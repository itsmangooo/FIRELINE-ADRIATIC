using System;

namespace Fireline.Simulation
{
    /// <summary>Explicit gameplay estimates for the existing generic debug body, not CL-415 engine performance data.</summary>
    public static class DebugFuelProfile
    {
        public const string Identifier = "DEBUG_FUEL_V1";
        public const double InitialFuelKg = 400, FullThrottleKgPerSecond = .25;
    }

    /// <summary>One accepted resource/physics transaction; callers install the state and accounting together.</summary>
    [Serializable]
    public struct FuelFlightStep
    {
        public FlightState State;
        public double RequestedKg, BurnedKg, EffectiveThrottle;
    }

    /// <summary>Pure fuel-aware adapter around the unchanged debug flight/contact kernel; no client can install its result.</summary>
    public static class FuelFlight
    {
        /// <summary>
        /// Consume fuel only for the accepted physical step. An empty tank produces no thrust; a partial final step uses
        /// proportional average throttle. Zero throttle has no idle burn in this deliberately limited engine model.
        /// Validate the complete contact/resource candidate before returning; exceptions cannot consume the caller's fuel.
        /// </summary>
        public static FuelFlightStep Step(FlightState current, FlightInput requested, FlightVector windMps,
            double densityKgM3, double seconds, double brake, RunwayPlane plane)
        {
            current.Validate(); requested.Validate();
            SimMath.Range(seconds, 1e-6, DebugRunwayProfile.MaximumStepSeconds, nameof(seconds));
            double demand = DebugFuelProfile.FullThrottleKgPerSecond * requested.Throttle * seconds;
            double burned = Math.Min(current.FuelKg, demand);
            var powered = requested;
            powered.Throttle = demand > 0 ? requested.Throttle * (burned / demand) : 0;
            var candidate = RunwayContact.Step(current, powered, windMps, densityKgM3, seconds, brake, plane);
            candidate = RebaseFuel(candidate, current.FuelKg - burned);
            RunwayContact.Evaluate(candidate, powered, brake, plane);
            return new FuelFlightStep { State = candidate, RequestedKg = demand, BurnedKg = burned, EffectiveThrottle = powered.Throttle };
        }

        /// <summary>
        /// Move the CoG reference when fuel leaves, preserving the dry-body origin and its instantaneous velocity.
        /// Attitude/angular rate and water are unchanged. Exhaust leaves with local rigid-body velocity: no extra rocket
        /// recoil or engine spin dynamics are modeled. The existing force law already represents propeller thrust.
        /// </summary>
        public static FlightState RebaseFuel(FlightState current, double remainingKg)
        {
            current.Validate();
            SimMath.Range(remainingKg, 0, current.FuelKg, nameof(remainingKg));
            double previousCg = FlightDynamics.Mass(current.FuelKg, current.WaterKg).CgZ;
            double nextCg = FlightDynamics.Mass(remainingKg, current.WaterKg).CgZ;
            var shift = new FlightVector(0, 0, nextCg - previousCg);
            var candidate = current;
            candidate.FuelKg = remainingKg;
            candidate.PositionM += current.Attitude.Rotate(shift);
            candidate.VelocityMps += current.Attitude.Rotate(FlightVector.Cross(current.AngularVelocityRadps, shift));
            candidate.Validate(); return candidate;
        }
    }
}
