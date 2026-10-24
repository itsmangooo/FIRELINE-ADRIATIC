using System;
using Fireline.Shared;
using Fireline.Simulation;

namespace Fireline.Server
{
    /// <summary>Debug single-aircraft authority. Connected identities are supplied by the transport adapter, never by payloads.</summary>
    public sealed class FlightAuthority
    {
        public const double StepSeconds = .005, InputLeaseSeconds = .5;
        public const int MaximumStepsPerUpdate = 8;
        /// <summary>Last fully validated state; no caller can install a client pose.</summary>
        public FlightState State { get; private set; }
        /// <summary>Completed physical steps, independent of fire ticks and Unity callback counts.</summary>
        public ulong Tick { get; private set; }
        /// <summary>Connected pilot identity, or zero when the seat is vacant.</summary>
        public int Owner { get; private set; }
        /// <summary>Seat generation; changes on assignment/release to invalidate packets from previous occupants.</summary>
        public uint Epoch { get; private set; }
        /// <summary>Last accepted sequence in this seat generation; zero is reserved.</summary>
        public uint Sequence { get; private set; }
        /// <summary>Wall time discarded by the bounded catch-up policy, in seconds; never counted as simulated time.</summary>
        public double DiscardedSeconds { get; private set; }
        /// <summary>Fatal envelope/solver failure; freezes the last valid state and releases the seat.</summary>
        public bool Faulted { get; private set; }
        /// <summary>Number of callback budgets that discarded time, including sleep/stall recovery.</summary>
        public int Overruns { get; private set; }
        /// <summary>Number of transitions from a live input to the neutral timeout fallback.</summary>
        public int Expirations { get; private set; }
        private FlightInput input = new FlightInput { Gear = 1 };
        private double brake = 1, deadline, accumulated, lastInputTime = double.NegativeInfinity;
        private bool leased;

        /// <summary>Start the estimated dry aircraft above the synthetic runway, without modifying the existing solver or art.</summary>
        public FlightAuthority() { State = new FlightState { PositionM = new FlightVector(0, 2.05, 0), Attitude = FlightRotation.Identity }; }

        /// <summary>Reserve a vacant seat for a live positive identity; repeated claim by the same pilot is idempotent.</summary>
        public FlightResult Claim(int identity)
        {
            if (identity <= 0 || Faulted) return FlightResult.Invalid;
            if (Owner == identity) return FlightResult.Accepted;
            if (Owner != 0) return FlightResult.NotOwner;
            Owner = identity; Epoch = checked(Epoch + 1); Sequence = 0; Neutral();
            return FlightResult.Accepted;
        }

        /// <summary>Release only the current pilot, including on disconnect; a seat-generation change invalidates queued old commands.</summary>
        public FlightResult Release(int identity, uint epoch)
        {
            if (identity <= 0 || identity != Owner || epoch != Epoch) return FlightResult.NotOwner;
            Owner = 0; Epoch = checked(Epoch + 1); Sequence = 0; Neutral(); return FlightResult.Accepted;
        }

        /// <summary>Accept finite bounded controls only from the seat owner, current generation, increasing sequence and recent observed tick.</summary>
        public FlightResult Submit(int identity, uint epoch, uint sequence, ulong observedTick, FlightInput command, double wheelBrake, double now)
        {
            if (Faulted) return FlightResult.Invalid;
            if (identity <= 0 || identity != Owner || epoch != Epoch) return FlightResult.NotOwner;
            if (sequence == 0 || sequence <= Sequence) return FlightResult.Sequence;
            if (observedTick > Tick || Tick - observedTick > 200) return FlightResult.Tick;
            if (double.IsNaN(now) || double.IsInfinity(now) || now < lastInputTime) return FlightResult.Invalid;
            try { command.Validate(); Range(wheelBrake, 0, 1); }
            catch (ArgumentException) { return FlightResult.Invalid; }
            // Bound accepted pilot updates to 50 Hz; rejected attempts never refresh the input lease or sequence.
            if (now - lastInputTime < .02 - 1e-9) return FlightResult.Rate;
            input = command; brake = wheelBrake; Sequence = sequence; lastInputTime = now;
            deadline = now + InputLeaseSeconds; leased = true; return FlightResult.Accepted;
        }

        /// <summary>Consume at most 40 ms in eight 5 ms steps; discard excess wall time instead of unbounded sleep catch-up.</summary>
        public int Advance(double elapsed, double now, WindField wind, ulong fireTick)
        {
            Range(elapsed, 0, double.MaxValue);
            Range(now, 0, double.MaxValue);
            if (wind == null) throw new ArgumentNullException(nameof(wind));
            if (Faulted) return 0;
            if (leased && now >= deadline) { Expirations++; Neutral(); }
            double available = accumulated + elapsed, budget = MaximumStepsPerUpdate * StepSeconds;
            if (available > budget) { DiscardedSeconds += available - budget; Overruns++; available = budget; }
            accumulated = available; int steps = 0;
            try
            {
                while (accumulated + 1e-12 >= StepSeconds && steps < MaximumStepsPerUpdate)
                {
                    var p = State.PositionM;
                    var sampled = wind.Sample(p.X, p.Z, p.Y, fireTick);
                    State = RunwayContact.Step(State, input, new FlightVector(sampled.X, 0, sampled.Z),
                        DebugFlightProfile.ReferenceDensity, StepSeconds, brake, RunwayPlane.Debug);
                    Tick++; steps++; accumulated = Math.Max(0, accumulated - StepSeconds);
                }
            }
            catch (ArgumentException) { Faulted = true; if (Owner != 0) Release(Owner, Epoch); }
            catch (InvalidOperationException) { Faulted = true; if (Owner != 0) Release(Owner, Epoch); }
            return steps;
        }

        /// <summary>Remove thrust/axes, extend gear and apply brakes when no fresh pilot command is available; this is no airborne autopilot.</summary>
        private void Neutral()
        {
            input = new FlightInput { Gear = 1 }; brake = 1; leased = false;
            lastInputTime = double.NegativeInfinity;
        }

        /// <summary>Require finite scalar boundary values without exposing the simulation assembly's internal helpers.</summary>
        private static void Range(double value, double low, double high)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < low || value > high)
                throw new ArgumentOutOfRangeException(nameof(value));
        }
    }
}
