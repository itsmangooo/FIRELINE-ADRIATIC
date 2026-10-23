using System;

namespace Fireline.Simulation
{
    public enum BurnPhase { Unburned, Heating, Burning, Smoldering, Extinguished, Charred }
    public enum VegetationPhase { Living, Burning, Charred }
    public enum FuelKind { Grass, Brush, PineLitter }

    [Serializable]
    public struct ChunkId : IComparable<ChunkId>, IEquatable<ChunkId>
    {
        public int Sector, X, Z;
        public ChunkId(int sector, int x, int z) { Sector = sector; X = x; Z = z; }
        public int CompareTo(ChunkId other)
        {
            int c = Sector.CompareTo(other.Sector);
            if (c == 0) c = X.CompareTo(other.X);
            return c == 0 ? Z.CompareTo(other.Z) : c;
        }
        public bool Equals(ChunkId other) => Sector == other.Sector && X == other.X && Z == other.Z;
        public override bool Equals(object other) => other is ChunkId id && Equals(id);
        public override int GetHashCode() { unchecked { return (Sector * 397 ^ X) * 397 ^ Z; } }
        public override string ToString() => $"{Sector}:{X}:{Z}";
    }

    [Serializable]
    public struct FireCell
    {
        public FuelKind Fuel;
        public double InitialFuelKgPerM2, RemainingFuelKgPerM2, Moisture, WaterKgPerM2;
        public double Heat, Intensity, Smoke, ElevationM;
        public BurnPhase Phase;
        public VegetationPhase Vegetation;
        public ulong VegetationId, LastChangedTick;
    }

    [Serializable]
    public struct WaterHit
    {
        public ChunkId Chunk;
        public int Cell;
        public double Weight;
        public WaterHit(ChunkId chunk, int cell, double weight) { Chunk = chunk; Cell = cell; Weight = weight; }
    }

    [Serializable]
    public sealed class ChunkSnapshot
    {
        public ChunkId Id;
        public ulong Revision, LastSimulatedTick;
        public bool Active;
        public FireCell[] Cells;
    }

    [Serializable]
    public sealed class ChunkDelta
    {
        public ChunkId Id;
        public ulong PreviousRevision, NextRevision, Tick, WindRevision;
        public int[] Indices;
        public FireCell[] Values;
    }

    [Serializable]
    public sealed class FireSnapshot
    {
        public int Schema = 1, Seed, Size;
        public double CellSizeM, StepSeconds;
        public double AppliedWaterKg, EvaporatedWaterKg, ConsumedFuelKg;
        public int InactiveCursor;
        public ulong Tick;
        public WindState Wind;
        public ChunkSnapshot[] Chunks;
        public string Checksum;
    }

    // M02 supplies a real serializer/store; this contract is not persistence evidence.
    public interface IFireSnapshotStore
    {
        void Write(FireSnapshot snapshot);
        FireSnapshot Read();
    }

    internal static class SimMath
    {
        public static double Clamp(double value, double low, double high) => Math.Max(low, Math.Min(high, value));
        public static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
        public static void Range(double value, double low, double high, string name)
        {
            if (!Finite(value) || value < low || value > high) throw new ArgumentOutOfRangeException(name);
        }
        public static uint Hash(int seed, int x, int z, ulong tick)
        {
            unchecked
            {
                uint h = (uint)seed ^ (uint)x * 374761393U ^ (uint)z * 668265263U ^ (uint)tick * 2246822519U ^ (uint)(tick >> 32);
                h = (h ^ (h >> 13)) * 1274126177U;
                return h ^ (h >> 16);
            }
        }
        public static double Noise(int seed, int x, int z, ulong tick) => Hash(seed, x, z, tick) / (double)uint.MaxValue;
    }
}
