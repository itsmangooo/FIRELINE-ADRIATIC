using System;

namespace Fireline.Simulation
{
    [Serializable]
    public struct WindVector
    {
        public double X, Z;
        public WindVector(double x, double z) { X = x; Z = z; }
    }

    [Serializable]
    public struct WindState
    {
        public double EastMps, NorthMps, GustMps;
        public ulong Revision;
    }

    public sealed class WindField
    {
        public int Seed { get; }
        public WindState State { get; private set; }
        public WindField(int seed) { Seed = seed; }

        // Called at the authoritative step boundary, never from a client weather effect.
        public void Set(double eastMps, double northMps, double gustMps)
        {
            SimMath.Range(eastMps, -40, 40, nameof(eastMps));
            SimMath.Range(northMps, -40, 40, nameof(northMps));
            SimMath.Range(gustMps, 0, 10, nameof(gustMps));
            State = new WindState { EastMps = eastMps, NorthMps = northMps, GustMps = gustMps, Revision = checked(State.Revision + 1) };
        }

        public WindVector Sample(double eastM, double northM, double heightM, ulong tick)
        {
            SimMath.Range(eastM, -100000000, 100000000, nameof(eastM));
            SimMath.Range(northM, -100000000, 100000000, nameof(northM));
            SimMath.Range(heightM, -1000, 10000, nameof(heightM));
            int x = (int)Math.Floor(eastM / 100), z = (int)Math.Floor(northM / 100);
            ulong epoch = tick / 20;
            double blend = (tick % 20) / 20.0;
            double Gust(int channel)
            {
                double a = SimMath.Noise(Seed + channel, x, z, epoch);
                double b = SimMath.Noise(Seed + channel, x, z, epoch + 1);
                return State.GustMps * (2 * (a + (b - a) * blend) - 1);
            }
            // An explicit coarse height multiplier, not resolved terrain/orographic flow.
            double heightFactor = 1 + SimMath.Clamp(heightM, 0, 300) * 0.001;
            return new WindVector(State.EastMps * heightFactor + Gust(17), State.NorthMps * heightFactor + Gust(53));
        }
    }
}
