using System;

namespace Fireline.Simulation
{
    /// <summary>A monotonic fixed-step clock; no fire or flight model is implemented.</summary>
    public sealed class TickClock
    {
        public int TicksPerSecond { get; }
        public ulong Tick { get; private set; }
        public double ElapsedSeconds => Tick / (double)TicksPerSecond;

        public TickClock(int ticksPerSecond)
        {
            if (ticksPerSecond < 1 || ticksPerSecond > 240)
                throw new ArgumentOutOfRangeException(nameof(ticksPerSecond));

            TicksPerSecond = ticksPerSecond;
        }

        public void Step()
        {
            checked { Tick++; }
        }
    }
}
