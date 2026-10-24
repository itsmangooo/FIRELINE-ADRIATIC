using System;
using System.Collections.Generic;
using Fireline.Simulation;

namespace Fireline.Server
{
    /// <summary>One actual post-step state plus independently checkable resource and force observations.</summary>
    [Serializable]
    public sealed class FuelObservation
    {
        public double seconds, requestedKg, burnedKg, effectiveThrottle;
        public FlightState state;
        public FlightState beforeRebase;
        public FlightForces forces;
    }

    /// <summary>Explicit matched input/environment for either the unchanged frozen-fuel path or the new fuel-aware path.</summary>
    [Serializable]
    public sealed class FuelCase
    {
        public string name;
        public bool legacy;
        public double dt, densityKgM3 = 1.225, brake;
        public FlightInput input;
        public FlightState initial;
        public FuelObservation[] samples;
    }

    /// <summary>Actual accelerated numerical evidence; no manufacturer calibration, normal pilot flight or resource persistence is implied.</summary>
    [Serializable]
    public sealed class FuelProof
    {
        public string profile = DebugFuelProfile.Identifier;
        public double fullThrottleKgPerSecond = DebugFuelProfile.FullThrottleKgPerSecond;
        public FuelCase[] cases;
    }

    /// <summary>Run through the existing opt-in dedicated flight proof, preserving the original nine free-flight cases.</summary>
    public static class FuelVerification
    {
        /// <summary>Record every accepted step under fixed input for four physical seconds; the before path uses the actual unchanged kernel.</summary>
        private static FuelCase Case(string name, double fuel = 400, double throttle = 1, bool legacy = false,
            double water = 0, double dt = .005, bool runway = false)
        {
            var state = FlightVerification.Fixture(water); state.FuelKg = fuel;
            if (runway) { state.PositionM.Y = 2.05; state.VelocityMps = new FlightVector(); }
            var result = new FuelCase { name = name, legacy = legacy, dt = dt, initial = state,
                input = new FlightInput { Gear = 1, Throttle = throttle } };
            var samples = new List<FuelObservation>();
            for (int i = 0; i < (int)Math.Round(4 / dt); i++)
            {
                var previous = state;
                var step = legacy ? new FuelFlightStep { State = RunwayContact.Step(state, result.input, new FlightVector(),
                    result.densityKgM3, dt, result.brake, RunwayPlane.Debug), EffectiveThrottle = throttle }
                    : FuelFlight.Step(state, result.input, new FlightVector(), result.densityKgM3, dt, result.brake, RunwayPlane.Debug);
                state = step.State;
                var powered = result.input; powered.Throttle = step.EffectiveThrottle;
                var beforeRebase = RunwayContact.Step(previous, powered, new FlightVector(), result.densityKgM3, dt, result.brake, RunwayPlane.Debug);
                samples.Add(new FuelObservation { seconds = (i + 1) * dt, state = state, requestedKg = step.RequestedKg,
                    burnedKg = step.BurnedKg, effectiveThrottle = step.EffectiveThrottle,
                    beforeRebase = beforeRebase,
                    forces = FlightDynamics.Evaluate(state, powered, new FlightVector(), result.densityKgM3) });
            }
            result.samples = samples.ToArray(); return result;
        }

        /// <summary>Cover full/partial/off/empty/loaded and final-fuel depletion, finer-step convergence and actual runway contact.</summary>
        public static FuelProof Run() => new FuelProof { cases = new[] {
            Case("before_full", legacy: true), Case("full"), Case("half", throttle: .5), Case("off", throttle: 0),
            Case("before_starved", fuel: .000625, legacy: true), Case("starved", fuel: .000625),
            Case("empty", fuel: 0), Case("water", water: 4000), Case("fine", dt: .0025), Case("runway", runway: true) } };
    }
}
