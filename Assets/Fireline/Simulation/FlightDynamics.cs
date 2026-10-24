using System;

namespace Fireline.Simulation
{
    /// <summary>All constants are CALIBRATE debug estimates, not verified CL-415 specifications or a production aircraft definition.</summary>
    public static class DebugFlightProfile
    {
        public const string Identifier = "DEBUG_FLIGHT_ONLY_V1";
        public const double DryMassKg = 10000, FuelCapacityKg = 1000, WaterCapacityKg = 4000, MaximumMassKg = 16000;
        public const double AreaM2 = 100, SpanM = 28, ChordM = 3.5, StaticThrustN = 40000;
        public const double FuelZ = -0.8, WaterZ = 1.2, ReferenceDensity = 1.225, GravityMps2 = 9.80665;
        public const double LiftOffset = 0.2, LiftSlope = 4.5, FlapLift = 0.4, StallRadians = 14 * Math.PI / 180;
        public const double BaseDrag = 0.035, InducedDrag = 0.06, FlapDrag = 0.08, GearDrag = 0.025;
    }

    /// <summary>Validated normalized commands: positive pitch raises nose, roll lowers right wing, yaw turns nose right.</summary>
    [Serializable]
    public struct FlightInput
    {
        public double Throttle, Pitch, Roll, Yaw, Flaps, Gear;
        /// <summary>Reject NaN, infinity and out-of-range pilot commands; never clamp unauthorized input into acceptance.</summary>
        public void Validate()
        {
            SimMath.Range(Throttle, 0, 1, nameof(Throttle)); SimMath.Range(Pitch, -1, 1, nameof(Pitch));
            SimMath.Range(Roll, -1, 1, nameof(Roll)); SimMath.Range(Yaw, -1, 1, nameof(Yaw));
            SimMath.Range(Flaps, 0, 1, nameof(Flaps)); SimMath.Range(Gear, 0, 1, nameof(Gear));
        }
    }

    /// <summary>World-space CoG position/velocity and body-space angular velocity, plus frozen-per-step fuel/water masses.</summary>
    [Serializable]
    public struct FlightState
    {
        public FlightVector PositionM, VelocityMps, AngularVelocityRadps;
        public FlightRotation Attitude;
        public double FuelKg, WaterKg;
        /// <summary>Reject invalid state before force evaluation and again before returning a candidate; no in-place mutation occurs.</summary>
        public void Validate()
        {
            PositionM.Validate(1e7, nameof(PositionM)); VelocityMps.Validate(500, nameof(VelocityMps));
            AngularVelocityRadps.Validate(10, nameof(AngularVelocityRadps)); Attitude.Validate();
            FlightDynamics.Mass(FuelKg, WaterKg);
        }
    }

    /// <summary>Total mass, centerline CoG offset and diagonal inertia about that CoG; all components are kg or kg m².</summary>
    [Serializable]
    public struct FlightMass
    {
        public double Kg, CgZ;
        public FlightVector InertiaKgM2;
    }

    /// <summary>One immutable force evaluation: world newtons, body newton-meters and scalar aerodynamic telemetry.</summary>
    [Serializable]
    public struct FlightForces
    {
        public FlightVector WorldN, BodyMomentNm, AerodynamicWorldN;
        public FlightMass Mass;
        public double AirspeedMps, AlphaRad, BetaRad, DynamicPressurePa, LiftN, DragN, ThrustN, LiftCoefficient;
    }

    /// <summary>Pure prototype 6DOF solver. Only a server adapter may install its result; this does not authorize pilot RPCs.</summary>
    public static class FlightDynamics
    {
        /// <summary>Combine dry body with centerline fuel/water point masses using exact CoG and parallel-axis contributions.</summary>
        public static FlightMass Mass(double fuelKg, double waterKg)
        {
            SimMath.Range(fuelKg, 0, DebugFlightProfile.FuelCapacityKg, nameof(fuelKg));
            SimMath.Range(waterKg, 0, DebugFlightProfile.WaterCapacityKg, nameof(waterKg));
            double mass = DebugFlightProfile.DryMassKg + fuelKg + waterKg;
            SimMath.Range(mass, DebugFlightProfile.DryMassKg, DebugFlightProfile.MaximumMassKg, "total mass");
            double cg = (fuelKg * DebugFlightProfile.FuelZ + waterKg * DebugFlightProfile.WaterZ) / mass;
            double shift = DebugFlightProfile.DryMassKg * cg * cg
                + fuelKg * Math.Pow(DebugFlightProfile.FuelZ - cg, 2) + waterKg * Math.Pow(DebugFlightProfile.WaterZ - cg, 2);
            return new FlightMass { Kg = mass, CgZ = cg, InertiaKgM2 = new FlightVector(50000 + shift, 100000 + shift, 120000) };
        }

        /// <summary>Estimated piecewise lift: linear until ±14 degrees, then continuous falloff to 15% of the stall-boundary value.</summary>
        public static double LiftCurve(double alphaRad, double flaps)
        {
            SimMath.Range(alphaRad, -Math.PI, Math.PI, nameof(alphaRad)); SimMath.Range(flaps, 0, 1, nameof(flaps));
            double edge = SimMath.Clamp(alphaRad, -DebugFlightProfile.StallRadians, DebugFlightProfile.StallRadians);
            double decay = Math.Max(0.15, 1 - Math.Max(0, Math.Abs(alphaRad) - DebugFlightProfile.StallRadians) / 0.6);
            return (DebugFlightProfile.LiftOffset + DebugFlightProfile.LiftSlope * edge + DebugFlightProfile.FlapLift * flaps) * decay;
        }

        /// <summary>Compute lift/drag/thrust/gravity and estimated stability/control moments using air-relative velocity; no state is changed.</summary>
        public static FlightForces Evaluate(FlightState state, FlightInput input, FlightVector windMps, double densityKgM3)
        {
            state.Validate(); input.Validate(); windMps.Validate(100, nameof(windMps)); SimMath.Range(densityKgM3, 0, 1.5, nameof(densityKgM3));
            var mass = Mass(state.FuelKg, state.WaterKg); var relative = state.VelocityMps - windMps;
            double speed = relative.Length; var air = state.Attitude.InverseRotate(relative);
            double alpha = speed > 1e-9 ? Math.Atan2(-air.Y, air.Z) : 0;
            double beta = speed > 1e-9 ? Math.Atan2(air.X, Math.Sqrt(air.Y * air.Y + air.Z * air.Z)) : 0;
            double pressure = 0.5 * densityKgM3 * speed * speed, qs = pressure * DebugFlightProfile.AreaM2;
            double cl = air.Z > 0 ? LiftCurve(alpha, input.Flaps) * Math.Pow(Math.Cos(beta), 2) : 0;
            double cd = air.Z >= 0 ? DebugFlightProfile.BaseDrag + DebugFlightProfile.InducedDrag * cl * cl
                + DebugFlightProfile.FlapDrag * input.Flaps + DebugFlightProfile.GearDrag * input.Gear
                + 1.2 * Math.Pow(Math.Sin(alpha), 2) + 0.3 * Math.Pow(Math.Sin(beta), 2) : 1;
            var drag = speed > 1e-9 ? relative * (-qs * cd / speed) : new FlightVector();
            var right = state.Attitude.Rotate(new FlightVector(1, 0, 0)); var liftAxis = FlightVector.Cross(relative, right);
            var lift = liftAxis.Length > 1e-9 ? liftAxis * (qs * cl / liftAxis.Length) : new FlightVector();
            var side = state.Attitude.Rotate(new FlightVector(-qs * 0.6 * beta, 0, 0));
            double thrust = DebugFlightProfile.StaticThrustN * input.Throttle * densityKgM3 / DebugFlightProfile.ReferenceDensity
                * Math.Max(0.25, 1 - speed / 150);
            var aerodynamic = lift + drag + side; var w = state.AngularVelocityRadps;
            double denominator = 2 * Math.Max(speed, 1);
            // Conventional nose-up and right-bank controls map to negative body X/Z angular moments.
            double cm = -0.6 * alpha + 0.08 * input.Pitch + 0.4 * w.X * DebugFlightProfile.ChordM / denominator;
            var moment = new FlightVector(-qs * DebugFlightProfile.ChordM * cm,
                qs * DebugFlightProfile.SpanM * (0.04 * input.Yaw + 0.15 * beta - 0.2 * w.Y * DebugFlightProfile.SpanM / denominator),
                -qs * DebugFlightProfile.SpanM * (0.05 * input.Roll + 0.2 * w.Z * DebugFlightProfile.SpanM / denominator));
            moment += FlightVector.Cross(new FlightVector(0, 0, -mass.CgZ), state.Attitude.InverseRotate(aerodynamic));
            return new FlightForces { Mass = mass, AirspeedMps = speed, AlphaRad = alpha, BetaRad = beta,
                DynamicPressurePa = pressure, LiftN = qs * cl, DragN = qs * cd, ThrustN = thrust, LiftCoefficient = cl,
                AerodynamicWorldN = aerodynamic, BodyMomentNm = moment,
                WorldN = aerodynamic + state.Attitude.Rotate(new FlightVector(0, 0, thrust)) + new FlightVector(0, -mass.Kg * DebugFlightProfile.GravityMps2, 0) };
        }

        /// <summary>Advance one 0–20 ms semi-implicit step with full gyroscopic term and normalized body-local quaternion increment.</summary>
        public static FlightState Step(FlightState current, FlightInput input, FlightVector windMps, double densityKgM3, double seconds)
        {
            SimMath.Range(seconds, 1e-6, 0.02, nameof(seconds)); var force = Evaluate(current, input, windMps, densityKgM3);
            var w = current.AngularVelocityRadps; var inertia = force.Mass.InertiaKgM2;
            var gyroscopic = FlightVector.Cross(w, new FlightVector(inertia.X * w.X, inertia.Y * w.Y, inertia.Z * w.Z));
            var torque = force.BodyMomentNm - gyroscopic;
            var candidate = current;
            candidate.VelocityMps += force.WorldN * (seconds / force.Mass.Kg);
            candidate.PositionM += candidate.VelocityMps * seconds;
            candidate.AngularVelocityRadps += new FlightVector(torque.X / inertia.X, torque.Y / inertia.Y, torque.Z / inertia.Z) * seconds;
            candidate.Attitude = (current.Attitude * FlightRotation.FromRotationVector(candidate.AngularVelocityRadps * seconds)).Normalized();
            candidate.Validate(); return candidate;
        }
    }
}
