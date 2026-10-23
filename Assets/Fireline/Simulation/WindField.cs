using System;

namespace Fireline.Simulation
{
    [Serializable]
    public struct WindVector
    {
        public double X, Z;
        /// <summary>
        /// Store east X and north Z wind velocity in meters per second; no vertical component is modeled.
        /// </summary>
        public WindVector(double x, double z) { X = x; Z = z; }
    }

    /// <summary>Horizontal base velocities/gust amplitude in m/s and a monotonically increasing change revision.</summary>
    [Serializable]
    public struct WindState
    {
        public double EastMps, NorthMps, GustMps;
        public ulong Revision;
    }

    public sealed class WindField
    {
        /// <summary>
        /// Seed that fixes spatial/temporal gust noise for this field.
        /// </summary>
        public int Seed { get; }
        /// <summary>
        /// Current base wind/gust amplitude and authoritative change revision; sampling does not mutate it.
        /// </summary>
        public WindState State { get; private set; }
        /// <summary>
        /// Create a seeded wind field with zero initial velocity and revision until the server sets conditions.
        /// </summary>
        public WindField(int seed) { Seed = seed; }

        // Called at the authoritative step boundary, never from a client weather effect.
        /// <summary>
        /// At a server step boundary, validate base components within +/-40 m/s and gust amplitude within 0-10 m/s.
        /// Replace conditions and increment their revision; client weather visuals must not call this to alter authority.
        /// </summary>
        public void Set(double eastMps, double northMps, double gustMps)
        {
            SimMath.Range(eastMps, -40, 40, nameof(eastMps));
            SimMath.Range(northMps, -40, 40, nameof(northMps));
            SimMath.Range(gustMps, 0, 10, nameof(gustMps));
            State = new WindState { EastMps = eastMps, NorthMps = northMps, GustMps = gustMps, Revision = checked(State.Revision + 1) };
        }

        /// <summary>
        /// Return east/north velocity in m/s at meter coordinates and a simulation tick without changing state.
        /// Use 100 m noise blocks, interpolate 20-tick gust epochs, and apply a coarse capped height multiplier.
        /// This approximates gameplay wind; it does not resolve terrain airflow or guarantee cross-OS bit identity.
        /// </summary>
        public WindVector Sample(double eastM, double northM, double heightM, ulong tick)
        {
            SimMath.Range(eastM, -100000000, 100000000, nameof(eastM));
            SimMath.Range(northM, -100000000, 100000000, nameof(northM));
            SimMath.Range(heightM, -1000, 10000, nameof(heightM));
            int x = (int)Math.Floor(eastM / 100), z = (int)Math.Floor(northM / 100);
            ulong epoch = tick / 20;
            double blend = (tick % 20) / 20.0;
            // Blend this axis channel between adjacent seeded epochs, mapping noise to signed gust velocity in m/s.
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
