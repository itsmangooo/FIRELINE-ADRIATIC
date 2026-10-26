using System;
using System.Collections.Generic;
using Fireline.Shared;
using Fireline.Simulation;

namespace Fireline.Server
{
    /// <summary>One accepted authority substep with the held QA command and actual post-step contact; not network or rendered-frame data.</summary>
    [Serializable]
    public sealed class FlightRouteSample
    {
        public double seconds, brake, maximumCompressionM;
        public ulong tick, fireTick;
        public FlightState state;
        public FlightInput input;
        public int phase, supportedWheels;
    }

    /// <summary>Accelerated calm-air runway evidence through the real server seat/input/lease/advance path; never a production autopilot.</summary>
    [Serializable]
    public sealed class FlightRouteProof
    {
        public string profile = "DEBUG_AUTHORITY_ROUTE_V1";
        public double dt = FlightAuthority.StepSeconds, commandSeconds = .02;
        public FlightState initial;
        public FlightRouteSample[] samples;
        public double maximumHeightM, longestAirborneSeconds, stoppedSeconds, consumedFuelKg;
        public int acceptedCommands;
        public bool passed;
    }

    /// <summary>Bounded QA control strategy for the existing estimated body. Installed only in the explicit numerical proof.</summary>
    public sealed class FlightRoutePilot
    {
        /// <summary>Ground roll=0, climb/hold=1, approach=2, throttle-cut/brake=3; these are QA phases, not incident/gameplay state.</summary>
        public int Phase { get; private set; }
        /// <summary>Return full brake only after the approach's low-height throttle cut; no floor snapping or forced deceleration.</summary>
        public double Brake => Phase == 3 ? 1 : 0;

        /// <summary>Read actual authority state every 20 ms, choose bounded commands, and leave all physical state changes to the server.</summary>
        public FlightInput Command(FlightState state)
        {
            state.Validate();
            if (Phase == 0 && state.PositionM.Y > 3) Phase = 1;
            if (Phase == 1 && state.PositionM.Z > 1400) Phase = 2;
            if (Phase == 2 && state.PositionM.Y < 2.1) Phase = 3;
            double speed = state.VelocityMps.Length;
            double pitch = Math.Asin(Math.Max(-1, Math.Min(1, state.Attitude.Rotate(new FlightVector(0, 0, 1)).Y)));
            double targetVertical = Phase == 2 ? -1 : Math.Max(-1, Math.Min(2, (10 - state.PositionM.Y) * .4));
            double cl = FlightDynamics.Mass(state.FuelKg, state.WaterKg).Kg * DebugFlightProfile.GravityMps2
                / (.5 * DebugFlightProfile.ReferenceDensity * Math.Max(speed * speed, 100) * DebugFlightProfile.AreaM2);
            double alpha = (cl - DebugFlightProfile.LiftOffset - DebugFlightProfile.FlapLift) / DebugFlightProfile.LiftSlope;
            double targetPitch = alpha + Math.Atan2(targetVertical, Math.Max(speed, 1));
            double command = Phase == 0 ? (speed > 46 ? 1 : 0)
                : Math.Max(-1, Math.Min(1, 7.5 * alpha + 32 * (targetPitch - pitch) + 3 * state.AngularVelocityRadps.X));
            return new FlightInput { Gear = 1, Flaps = 1, Throttle = Phase == 3 ? 0 : 1, Pitch = Phase == 3 ? 0 : command };
        }
    }

    /// <summary>Qualify one bounded numerical route; normal live aircraft, transport and rendering are untouched.</summary>
    public static class FlightRouteVerification
    {
        /// <summary>
        /// Require a real five-second airborne segment, approach/contact and five settled seconds within every wheel's runway bounds.
        /// Fail closed on rejected commands, expiry, discarded time, faults, surface departure or the 120-second physical deadline.
        /// </summary>
        public static FlightRouteProof Run()
        {
            var host = new FlightAuthority(); var pilot = new FlightRoutePilot(); var wind = new WindField(415); wind.Set(0, 0, 0);
            if (host.Claim(1) != FlightResult.Accepted) throw new InvalidOperationException("Route seat claim failed.");
            var result = new FlightRouteProof { initial = host.State }; var rows = new List<FlightRouteSample>();
            var input = new FlightInput(); double brake = 0, airborne = 0, stopped = 0; bool seenAirborne = false, touched = false;
            uint serial = 0;
            for (int i = 0; i < 24000; i++)
            {
                double now = i * FlightAuthority.StepSeconds; ulong fireTick = (ulong)(i / 40);
                if (i % 4 == 0)
                {
                    input = pilot.Command(host.State); brake = pilot.Brake;
                    if (host.Submit(1, host.Epoch, ++serial, host.Tick, input, brake, now) != FlightResult.Accepted)
                        throw new InvalidOperationException("Route input rejected.");
                    result.acceptedCommands++;
                }
                if (host.Advance(FlightAuthority.StepSeconds, now, wind, fireTick) != 1 || host.Faulted
                    || host.Overruns != 0 || host.Expirations != 0 || host.Owner != 1)
                    throw new InvalidOperationException("Route authority did not accept exactly one substep.");
                var state = host.State;
                var contact = RunwayContact.Evaluate(state, input, brake, RunwayPlane.Debug);
                if (!contact.LeftMain.OnRunway || !contact.RightMain.OnRunway || !contact.Nose.OnRunway)
                    throw new InvalidOperationException("Route left the finite runway footprint.");
                airborne = contact.SupportedWheels == 0 && state.PositionM.Y > 5 ? airborne + result.dt : 0;
                result.longestAirborneSeconds = Math.Max(result.longestAirborneSeconds, airborne);
                seenAirborne |= airborne >= 5;
                if (seenAirborne && pilot.Phase >= 2 && contact.SupportedWheels >= 2) touched = true;
                stopped = touched && pilot.Phase == 3 && contact.SupportedWheels == 3 && state.VelocityMps.Length < .2
                    && Math.Abs(state.VelocityMps.Y) < .1 ? stopped + result.dt : 0;
                result.maximumHeightM = Math.Max(result.maximumHeightM, state.PositionM.Y);
                rows.Add(new FlightRouteSample { seconds = (i + 1) * result.dt, tick = host.Tick, fireTick = fireTick,
                    state = state, input = input, brake = brake, phase = pilot.Phase, supportedWheels = contact.SupportedWheels,
                    maximumCompressionM = contact.MaximumCompressionM });
                if (stopped >= 5)
                {
                    result.samples = rows.ToArray(); result.stoppedSeconds = stopped; result.consumedFuelKg = host.ConsumedFuelKg;
                    result.passed = true; return result;
                }
            }
            throw new InvalidOperationException("Route did not qualify before 120 physical seconds.");
        }
    }
}
