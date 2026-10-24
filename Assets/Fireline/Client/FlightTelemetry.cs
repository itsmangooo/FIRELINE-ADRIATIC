using System;
using System.Globalization;
using Fireline.Shared;
using Fireline.Simulation;

namespace Fireline.Client
{
    /// <summary>Read-only instruments derived from one validated server pose, its collocated sampled wind and the debug mass profile.</summary>
    public struct FlightInstruments
    {
        public double AirspeedMps, GroundSpeedMps, VerticalMps, CogAltitudeM, PitchDegrees, BankDegrees, HeadingDegrees;
        public double FuelKg, WaterKg, MassKg, CogZM, WindEastMps, WindNorthMps;
        public ulong Tick, FireTick, WindRevision;
    }

    /// <summary>Pure presentation calculations; never integrates aircraft state or decides fuel, weather, ground contact or pilot authority.</summary>
    public static class FlightTelemetry
    {
        /// <summary>
        /// Reject incompatible/nonfinite data, then compute air-relative 3D speed, horizontal ground speed and diagnostic attitude.
        /// Heading is north-zero/east-positive; bank is right-wing-down positive. Euler heading/bank are singular at vertical pitch.
        /// CoG altitude uses the server datum and is not radar altitude or height above arbitrary terrain. Wind is horizontal in M01.
        /// </summary>
        public static FlightInstruments Read(FlightStateRpc source)
        {
            if (source.Version != FlightWire.Version || source.Owner < 0 || source.Faulted > 1)
                throw new ArgumentException("Incompatible or invalid aircraft snapshot header.");
            var state = new FlightState { PositionM = new FlightVector(source.X, source.Y, source.Z),
                VelocityMps = new FlightVector(source.Vx, source.Vy, source.Vz),
                AngularVelocityRadps = new FlightVector(source.Wx, source.Wy, source.Wz),
                Attitude = new FlightRotation(source.Qx, source.Qy, source.Qz, source.Qw), FuelKg = source.FuelKg, WaterKg = source.WaterKg };
            state.Validate();
            var wind = new FlightVector(source.WindX, 0, source.WindZ); wind.Validate(100, "snapshot wind");
            var forward = state.Attitude.Rotate(new FlightVector(0, 0, 1));
            var right = state.Attitude.Rotate(new FlightVector(1, 0, 0));
            var up = state.Attitude.Rotate(new FlightVector(0, 1, 0));
            var mass = FlightDynamics.Mass(state.FuelKg, state.WaterKg);
            double heading = Math.Atan2(forward.X, forward.Z) * 180 / Math.PI;
            return new FlightInstruments { AirspeedMps = (state.VelocityMps - wind).Length,
                GroundSpeedMps = Math.Sqrt(source.Vx * source.Vx + source.Vz * source.Vz), VerticalMps = source.Vy,
                CogAltitudeM = source.Y, PitchDegrees = Math.Asin(Math.Max(-1, Math.Min(1, forward.Y))) * 180 / Math.PI,
                BankDegrees = Math.Atan2(-right.Y, up.Y) * 180 / Math.PI, HeadingDegrees = heading < 0 ? heading + 360 : heading,
                FuelKg = source.FuelKg, WaterKg = source.WaterKg, MassKg = mass.Kg, CogZM = mass.CgZ,
                WindEastMps = source.WindX, WindNorthMps = source.WindZ, Tick = source.Tick, FireTick = source.FireTick, WindRevision = source.WindRevision };
        }

        /// <summary>Format engineering units with an invariant locale so the visible text and archived text can be independently compared.</summary>
        public static string Format(FlightInstruments value) => string.Format(CultureInfo.InvariantCulture,
            "Air speed {0:F2} m/s\nGround (horizontal) {1:F2} m/s\nVertical {2:+0.00;-0.00;0.00} m/s / CoG {3:F2} m\n"
            + "Pitch {4:+0.0;-0.0;0.0} / Bank {5:+0.0;-0.0;0.0} deg\nHeading {6:000.0} deg (N=0 / E=90)\n"
            + "Wind E {7:+0.00;-0.00;0.00} / N {8:+0.00;-0.00;0.00} m/s\nFuel {9:F2} / Water {10:F2} kg\nMass {11:F2} kg / CoG Z {12:F3} m\nWind rev {13} / Fire tick {14}",
            value.AirspeedMps, value.GroundSpeedMps, value.VerticalMps, value.CogAltitudeM, value.PitchDegrees, value.BankDegrees,
            value.HeadingDegrees, value.WindEastMps, value.WindNorthMps, value.FuelKg, value.WaterKg, value.MassKg, value.CogZM, value.WindRevision, value.FireTick);
    }
}
