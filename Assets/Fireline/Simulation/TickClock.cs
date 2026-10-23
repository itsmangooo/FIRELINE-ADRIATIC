using System;

namespace Fireline.Simulation
{
    /// <summary>A monotonic fixed-step clock; no fire or flight model is implemented.</summary>
    public sealed class TickClock
    {
        /// <summary>
        /// Fixed callbacks per simulated second; this is a configured cadence, not measured CPU time.
        /// </summary>
        public int TicksPerSecond { get; }
        /// <summary>
        /// Number of completed clock steps; only Step can advance it.
        /// </summary>
        public ulong Tick { get; private set; }
        /// <summary>
        /// Convert completed steps into simulated seconds using the configured cadence.
        /// </summary>
        public double ElapsedSeconds => Tick / (double)TicksPerSecond;

        /// <summary>
        /// Create a fixed-rate clock between 1 and 240 ticks per second; reject unsupported rates.
        /// </summary>
        public TickClock(int ticksPerSecond)
        {
            if (ticksPerSecond < 1 || ticksPerSecond > 240)
                throw new ArgumentOutOfRangeException(nameof(ticksPerSecond));

            TicksPerSecond = ticksPerSecond;
        }

        /// <summary>
        /// Advance one monotonic tick; checked arithmetic throws rather than wrapping at ulong capacity.
        /// </summary>
        public void Step()
        {
            checked { Tick++; }
        }
    }
}
