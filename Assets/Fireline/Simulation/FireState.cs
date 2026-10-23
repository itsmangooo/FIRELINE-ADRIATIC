using System;

namespace Fireline.Simulation
{
    // Combustion state and vegetation state are separate: extinguished fuel can reignite while vegetation stays charred.
    public enum BurnPhase { Unburned, Heating, Burning, Smoldering, Extinguished, Charred }
    public enum VegetationPhase { Living, Burning, Charred }
    public enum FuelKind { Grass, Brush, PineLitter }

    [Serializable]
    public struct ChunkId : IComparable<ChunkId>, IEquatable<ChunkId>
    {
        public int Sector, X, Z;
        /// <summary>
        /// Store sector and signed chunk coordinates; validation occurs when the world accepts the chunk.
        /// </summary>
        public ChunkId(int sector, int x, int z) { Sector = sector; X = x; Z = z; }
        /// <summary>
        /// Sort by sector, then east X, then north Z so traversal does not depend on insertion order.
        /// </summary>
        public int CompareTo(ChunkId other)
        {
            int c = Sector.CompareTo(other.Sector);
            if (c == 0) c = X.CompareTo(other.X);
            return c == 0 ? Z.CompareTo(other.Z) : c;
        }
        /// <summary>
        /// Two chunk IDs are equal only when all three coordinates match.
        /// </summary>
        public bool Equals(ChunkId other) => Sector == other.Sector && X == other.X && Z == other.Z;
        /// <summary>
        /// Support boxed equality while rejecting objects that are not a chunk ID.
        /// </summary>
        public override bool Equals(object other) => other is ChunkId id && Equals(id);
        /// <summary>
        /// Combine all coordinates for dictionary lookup; unchecked overflow is intentional and is not a state digest.
        /// </summary>
        public override int GetHashCode() { unchecked { return (Sector * 397 ^ X) * 397 ^ Z; } }
        /// <summary>
        /// Return a compact sector:X:Z label for diagnostics, not a persistence/network encoding.
        /// </summary>
        public override string ToString() => $"{Sector}:{X}:{Z}";
    }

    /// <summary>One value-type SI cell; normalized heat/intensity/hazard are gameplay indices, not temperature or emissions.</summary>
    [Serializable]
    public struct FireCell
    {
        public FuelKind Fuel;
        // Fuel and water are kg per square meter; moisture is a dimensionless fraction.
        public double InitialFuelKgPerM2, RemainingFuelKgPerM2, Moisture, WaterKgPerM2;
        // Heat/intensity/smoke are bounded [0,1]; elevation is meters, not a visual transform.
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
        /// <summary>
        /// Describe one cell and its fraction of a water dose; ApplyWater validates all hits before mutation.
        /// </summary>
        public WaterHit(ChunkId chunk, int cell, double weight) { Chunk = chunk; Cell = cell; Weight = weight; }
    }

    /// <summary>One chunk baseline, including its revision, last simulated tick, scheduling tier and copied cells.</summary>
    [Serializable]
    public sealed class ChunkSnapshot
    {
        public ChunkId Id;
        public ulong Revision, LastSimulatedTick;
        public bool Active;
        public FireCell[] Cells;
    }

    /// <summary>Latest-step changed cells and revision precondition; parallel Indices/Values refer to the same local cells.</summary>
    [Serializable]
    public sealed class ChunkDelta
    {
        public ChunkId Id;
        public ulong PreviousRevision, NextRevision, Tick, WindRevision;
        public int[] Indices;
        public FireCell[] Values;
    }

    /// <summary>Schema-1 detached world data and regression digest; this DTO alone does not implement durable restoration.</summary>
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
        /// <summary>
        /// Contract for a future store to write a complete snapshot; M01 provides no storage implementation.
        /// </summary>
        void Write(FireSnapshot snapshot);
        /// <summary>
        /// Contract for a future store to load a snapshot; restoring/validating a world belongs to later work.
        /// </summary>
        FireSnapshot Read();
    }

    internal static class SimMath
    {
        /// <summary>
        /// Restrict a value to inclusive bounds; callers must validate finiteness when accepting external input.
        /// </summary>
        public static double Clamp(double value, double low, double high) => Math.Max(low, Math.Min(high, value));
        /// <summary>
        /// Return false for NaN and either infinity so invalid numbers cannot enter bounded simulation parameters.
        /// </summary>
        public static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
        /// <summary>
        /// Throw using the parameter name unless the value is finite and within inclusive bounds.
        /// </summary>
        public static void Range(double value, double low, double high, string name)
        {
            if (!Finite(value) || value < low || value > high) throw new ArgumentOutOfRangeException(name);
        }
        /// <summary>
        /// Mix seed, signed grid coordinates and both halves of the tick into repeatable 32-bit noise.
        /// Overflow is intentional; this is deterministic gameplay noise, not a cryptographic hash.
        /// </summary>
        public static uint Hash(int seed, int x, int z, ulong tick)
        {
            unchecked
            {
                uint h = (uint)seed ^ (uint)x * 374761393U ^ (uint)z * 668265263U ^ (uint)tick * 2246822519U ^ (uint)(tick >> 32);
                h = (h ^ (h >> 13)) * 1274126177U;
                return h ^ (h >> 16);
            }
        }
        /// <summary>
        /// Normalize the integer noise hash to the inclusive dimensionless range [0, 1].
        /// </summary>
        public static double Noise(int seed, int x, int z, ulong tick) => Hash(seed, x, z, tick) / (double)uint.MaxValue;
    }
}
