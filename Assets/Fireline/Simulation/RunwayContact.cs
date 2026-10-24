using System;

namespace Fireline.Simulation
{
    /// <summary>Finite, level, stationary debug runway rectangle; not terrain, sea contact or surveyed Zemunik geography.</summary>
    [Serializable]
    public struct RunwayPlane
    {
        public double HeightM, HalfWidthM, StartZM, EndZM;
        /// <summary>Return the explicitly synthetic 45 m by 3000 m north-oriented test surface at zero elevation.</summary>
        public static RunwayPlane Debug => new RunwayPlane { HalfWidthM = 22.5, StartZM = -500, EndZM = 2500 };
        /// <summary>Reject invalid geometry before testing wheel coordinates; no silent fallback to an infinite floor.</summary>
        public void Validate()
        {
            SimMath.Range(HeightM, -10000, 10000, nameof(HeightM));
            SimMath.Range(HalfWidthM, .1, 10000, nameof(HalfWidthM));
            SimMath.Range(StartZM, -1e6, 1e6, nameof(StartZM)); SimMath.Range(EndZM, -1e6, 1e6, nameof(EndZM));
            if (EndZM <= StartZM) throw new ArgumentException("Runway must have positive length.");
        }
        /// <summary>Test the actual contact point against the finite inclusive runway edges in world X/Z meters.</summary>
        public bool Contains(FlightVector point) => Math.Abs(point.X) <= HalfWidthM && point.Z >= StartZM && point.Z <= EndZM;
    }

    /// <summary>CALIBRATE estimates for a generic debug tricycle gear, deliberately independent of the imported CL-415 mesh.</summary>
    public static class DebugRunwayProfile
    {
        public const string Identifier = "DEBUG_RUNWAY_CONTACT_V1";
        public const double SpringNPerM = 300000, DamperNsPerM = 50000, MaximumNormalN = 250000;
        public const double MaximumCompressionM = .75, MaximumStepSeconds = .005;
        public const double RollingFriction = .02, BrakedFriction = .6, LateralFriction = .65;
        public const double RollingDampingNsPerM = 800, BrakeDampingNsPerM = 50000, LateralDampingNsPerM = 20000;
        public const double MaximumSteerRad = .35;
        /// <summary>Return the two main and one nose tire points relative to the dry-body origin, in body meters.</summary>
        public static FlightVector Wheel(int index)
        {
            switch (index)
            {
                case 0: return new FlightVector(-3, -2, -3);
                case 1: return new FlightVector(3, -2, -3);
                case 2: return new FlightVector(0, -2, 6);
                default: throw new ArgumentOutOfRangeException(nameof(index));
            }
        }
    }

    /// <summary>One pre-step wheel observation: world force/velocity and body CoG lever, including actual Coulomb limits.</summary>
    [Serializable]
    public struct WheelContact
    {
        public FlightVector PointM, VelocityMps, WorldN, BodyLeverM;
        public double CompressionM, NormalN, LongitudinalN, LateralN, FrictionPowerW, FrictionEllipse;
        public bool OnRunway;
    }

    /// <summary>Combined pre-step ground reaction; friction power is nonpositive, while spring forces can return stored energy.</summary>
    [Serializable]
    public struct ContactForces
    {
        public FlightVector WorldN, BodyMomentNm;
        public WheelContact LeftMain, RightMain, Nose;
        public double NormalN, SpringEnergyJ, FrictionPowerW, MaximumCompressionM;
        public int SupportedWheels;
    }

    /// <summary>Pure, bounded 200 Hz-or-faster contact adapter; installation and pilot authority remain server responsibilities.</summary>
    public static class RunwayContact
    {
        /// <summary>
        /// Evaluate unilateral spring/damper loads and regularized Coulomb-limited tire forces at three actual wheel points.
        /// Brake acts on the mains; rudder command steers the nose. Reject overload/unsupported attitude instead of projecting a body onto the floor.
        /// </summary>
        public static ContactForces Evaluate(FlightState state, FlightInput input, double brake, RunwayPlane plane)
        {
            state.Validate(); input.Validate(); plane.Validate(); SimMath.Range(brake, 0, 1, nameof(brake));
            var result = new ContactForces();
            var mass = FlightDynamics.Mass(state.FuelKg, state.WaterKg);
            if (plane.Contains(state.PositionM) && state.PositionM.Y < plane.HeightM + .5)
                throw new InvalidOperationException("RUNWAY_ENVELOPE: unsupported belly/terrain impact.");
            if (input.Gear <= .99) return result;
            for (int i = 0; i < 3; i++)
            {
                var lever = DebugRunwayProfile.Wheel(i) - new FlightVector(0, 0, mass.CgZ);
                var point = state.PositionM + state.Attitude.Rotate(lever);
                var velocity = state.VelocityMps + state.Attitude.Rotate(FlightVector.Cross(state.AngularVelocityRadps, lever));
                var wheel = new WheelContact { PointM = point, VelocityMps = velocity, BodyLeverM = lever, OnRunway = plane.Contains(point) };
                double compression = Math.Max(0, plane.HeightM - point.Y);
                if (wheel.OnRunway && point.Y <= plane.HeightM)
                {
                    if (compression > DebugRunwayProfile.MaximumCompressionM || state.Attitude.Rotate(new FlightVector(0, 1, 0)).Y < .5)
                        throw new InvalidOperationException("RUNWAY_ENVELOPE: gear compression/attitude exceeded.");
                    wheel.CompressionM = compression;
                    wheel.NormalN = SimMath.Clamp(DebugRunwayProfile.SpringNPerM * compression - DebugRunwayProfile.DamperNsPerM * velocity.Y,
                        0, DebugRunwayProfile.MaximumNormalN);
                    if (wheel.NormalN > 0)
                    {
                        double steer = i == 2 ? input.Yaw * DebugRunwayProfile.MaximumSteerRad : 0;
                        var forward = state.Attitude.Rotate(new FlightVector(Math.Sin(steer), 0, Math.Cos(steer)));
                        forward.Y = 0; forward *= 1 / forward.Length;
                        var right = FlightVector.Cross(new FlightVector(0, 1, 0), forward);
                        double braking = i < 2 ? brake : 0;
                        double mu = DebugRunwayProfile.RollingFriction + (DebugRunwayProfile.BrakedFriction - DebugRunwayProfile.RollingFriction) * braking;
                        double longitudinal = -(DebugRunwayProfile.RollingDampingNsPerM + DebugRunwayProfile.BrakeDampingNsPerM * braking) * FlightVector.Dot(velocity, forward);
                        double lateral = -DebugRunwayProfile.LateralDampingNsPerM * FlightVector.Dot(velocity, right);
                        // Normalize demands before dividing by the load; tiny positive loads must not overflow the squared norm.
                        double demand = Math.Sqrt(Math.Pow(longitudinal / mu, 2)
                            + Math.Pow(lateral / DebugRunwayProfile.LateralFriction, 2));
                        double scale = demand > 0 ? Math.Min(1, wheel.NormalN / demand) : 1;
                        wheel.LongitudinalN = longitudinal * scale; wheel.LateralN = lateral * scale;
                        wheel.FrictionEllipse = demand > 0 ? Math.Min(1, demand / wheel.NormalN) : 0;
                        var friction = forward * wheel.LongitudinalN + right * wheel.LateralN;
                        wheel.FrictionPowerW = FlightVector.Dot(friction, velocity);
                        wheel.WorldN = friction + new FlightVector(0, wheel.NormalN, 0);
                        result.WorldN += wheel.WorldN;
                        result.BodyMomentNm += FlightVector.Cross(lever, state.Attitude.InverseRotate(wheel.WorldN));
                        result.NormalN += wheel.NormalN; result.SupportedWheels++;
                        result.FrictionPowerW += wheel.FrictionPowerW;
                    }
                    result.SpringEnergyJ += .5 * DebugRunwayProfile.SpringNPerM * compression * compression;
                    result.MaximumCompressionM = Math.Max(result.MaximumCompressionM, compression);
                }
                if (i == 0) result.LeftMain = wheel;
                else if (i == 1) result.RightMain = wheel;
                else result.Nose = wheel;
            }
            return result;
        }

        /// <summary>
        /// Add evaluated contact reactions to the unchanged aerodynamic/gravity forces, integrate once and validate the candidate envelope.
        /// The caller's value state remains unchanged if either the current or candidate state is rejected. No floor snapping or hidden mass change occurs.
        /// </summary>
        public static FlightState Step(FlightState current, FlightInput input, FlightVector windMps, double densityKgM3, double seconds, double brake, RunwayPlane plane)
        {
            SimMath.Range(seconds, 1e-6, DebugRunwayProfile.MaximumStepSeconds, nameof(seconds));
            var reaction = Evaluate(current, input, brake, plane);
            var force = FlightDynamics.Evaluate(current, input, windMps, densityKgM3);
            force.WorldN += reaction.WorldN; force.BodyMomentNm += reaction.BodyMomentNm;
            var candidate = FlightDynamics.Integrate(current, force, seconds);
            Evaluate(candidate, input, brake, plane);
            return candidate;
        }
    }
}
