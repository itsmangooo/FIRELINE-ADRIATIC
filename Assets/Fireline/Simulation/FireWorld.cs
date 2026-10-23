using System;
using System.Collections.Generic;

namespace Fireline.Simulation
{
    /// <summary>Bounded gameplay approximation on a synthetic SI grid. No Unity or client state.</summary>
    public sealed class FireWorld
    {
        private sealed class Chunk
        {
            public ChunkId Id;
            public FireCell[] Cells, Next;
            public bool Active;
            public ulong Revision, LastTick;
        }
        private readonly SortedDictionary<ChunkId, Chunk> chunks = new SortedDictionary<ChunkId, Chunk>();
        private readonly List<Chunk> scheduled = new List<Chunk>();
        private readonly List<Chunk> inactive = new List<Chunk>();
        private readonly List<ChunkDelta> deltas = new List<ChunkDelta>();
        private int inactiveCursor;
        public const int MaximumChunks = 16, MaximumActiveChunks = 4, InactiveChunksPerStep = 1;
        public int Size { get; }
        public double CellSizeM { get; }
        public double StepSeconds => 0.2;
        public ulong Tick { get; private set; }
        public WindField Wind { get; }
        public int LastVisitedCells { get; private set; }
        public double BacklogSeconds { get; private set; }
        public double AppliedWaterKg { get; private set; }
        public double EvaporatedWaterKg { get; private set; }
        public double ConsumedFuelKg { get; private set; }

        public FireWorld(int seed, int size = 16, double cellSizeM = 10)
        {
            if (size < 4 || size > 32) throw new ArgumentOutOfRangeException(nameof(size));
            SimMath.Range(cellSizeM, 5, 100, nameof(cellSizeM));
            Size = size; CellSizeM = cellSizeM; Wind = new WindField(seed);
        }

        public void AddChunk(ChunkId id, bool active, double fuelKgPerM2 = 2, double moisture = 0.1, FuelKind fuel = FuelKind.Brush)
        {
            if (chunks.Count >= MaximumChunks || chunks.ContainsKey(id)) throw new InvalidOperationException("Chunk capacity or duplicate ID.");
            if (Math.Abs((long)id.X) > 10000 || Math.Abs((long)id.Z) > 10000) throw new ArgumentOutOfRangeException(nameof(id));
            if (id.Sector < 0 || id.Sector > ushort.MaxValue) throw new ArgumentOutOfRangeException(nameof(id));
            if (!Enum.IsDefined(typeof(FuelKind), fuel)) throw new ArgumentOutOfRangeException(nameof(fuel));
            SimMath.Range(fuelKgPerM2, 0, 20, nameof(fuelKgPerM2));
            SimMath.Range(moisture, 0, 1, nameof(moisture));
            if (active && ActiveCount() >= MaximumActiveChunks) throw new InvalidOperationException("Active chunk budget exceeded.");
            var chunk = new Chunk { Id = id, Active = active, Cells = new FireCell[Size * Size], Next = new FireCell[Size * Size], LastTick = Tick };
            for (int i = 0; i < chunk.Cells.Length; i++)
                chunk.Cells[i] = new FireCell { InitialFuelKgPerM2 = fuelKgPerM2, RemainingFuelKgPerM2 = fuelKgPerM2,
                    Moisture = moisture, Fuel = fuel, VegetationId = ((ulong)(ushort)id.Sector << 48)
                        | ((ulong)(ushort)id.X << 32) | ((ulong)(ushort)id.Z << 16) | (ushort)i };
            chunks.Add(id, chunk);
        }

        private int ActiveCount() { int n = 0; foreach (var c in chunks.Values) if (c.Active) n++; return n; }
        public void SetActive(ChunkId id, bool active)
        {
            var c = chunks[id];
            if (active && !c.Active && ActiveCount() >= MaximumActiveChunks) throw new InvalidOperationException("Active chunk budget exceeded.");
            c.Active = active;
        }
        public FireCell GetCell(ChunkId id, int index) => chunks[id].Cells[index];
        public void SetElevation(ChunkId id, int index, double elevationM)
        {
            if (Tick != 0) throw new InvalidOperationException("Terrain configuration is immutable after stepping.");
            SimMath.Range(elevationM, -1000, 10000, nameof(elevationM));
            var c = chunks[id]; var cell = c.Cells[index]; cell.ElevationM = elevationM; c.Cells[index] = cell;
        }

        public bool Ignite(ChunkId id, int index)
        {
            var c = chunks[id]; var cell = c.Cells[index];
            if (cell.Phase == BurnPhase.Charred || cell.RemainingFuelKgPerM2 <= 0 || cell.Moisture >= 0.85 || cell.WaterKgPerM2 >= 0.75) return false;
            cell.Phase = BurnPhase.Burning;
            if (cell.Vegetation != VegetationPhase.Charred) cell.Vegetation = VegetationPhase.Burning;
            cell.Heat = 1; cell.Intensity = 1; cell.LastChangedTick = Tick;
            c.Cells[index] = cell; c.Revision++; return true;
        }

        // Validate the whole coverage before mutation. Mass is distributed, not multiplied by hits.
        public void ApplyWater(double massKg, WaterHit[] hits)
        {
            SimMath.Range(massKg, 0.000001, 100000, nameof(massKg));
            if (hits == null || hits.Length == 0 || hits.Length > 4096) throw new ArgumentException("Invalid coverage.");
            double weight = 0;
            foreach (var hit in hits)
            {
                SimMath.Range(hit.Weight, 0, 1, nameof(hit.Weight));
                if (!chunks.ContainsKey(hit.Chunk) || hit.Cell < 0 || hit.Cell >= Size * Size) throw new ArgumentException("Unknown coverage cell.");
                weight += hit.Weight;
            }
            if (Math.Abs(weight - 1) > 1e-9) throw new ArgumentException("Coverage weights must sum to one.");
            foreach (var hit in hits)
            {
                var c = chunks[hit.Chunk]; var cell = c.Cells[hit.Cell];
                double dose = massKg * (hit.Weight / weight) / (CellSizeM * CellSizeM);
                cell.WaterKgPerM2 += dose;
                cell.Heat *= Math.Exp(-dose / 0.5); cell.Intensity *= Math.Exp(-dose / 0.5);
                if ((cell.Phase == BurnPhase.Burning || cell.Phase == BurnPhase.Smoldering) && (cell.WaterKgPerM2 >= 0.75 || cell.Intensity < 0.15))
                { cell.Phase = BurnPhase.Extinguished; cell.Intensity = 0; cell.Vegetation = VegetationPhase.Charred; }
                cell.LastChangedTick = Tick; c.Cells[hit.Cell] = cell; c.Revision++;
            }
            AppliedWaterKg += massKg;
        }

        public void Step()
        {
            checked { Tick++; }
            scheduled.Clear(); inactive.Clear(); deltas.Clear(); LastVisitedCells = 0; BacklogSeconds = 0;
            foreach (var c in chunks.Values) { if (c.Active) scheduled.Add(c); else inactive.Add(c); }
            if (inactive.Count > 0)
            {
                inactiveCursor %= inactive.Count;
                scheduled.Add(inactive[inactiveCursor]); inactiveCursor = (inactiveCursor + 1) % inactive.Count;
            }
            // All selected chunks read the same previous world before any buffer is committed.
            foreach (var c in scheduled)
            {
                ulong steps = Tick - c.LastTick;
                double dt = steps * StepSeconds;
                for (int i = 0; i < c.Cells.Length; i++) c.Next[i] = Advance(c, i, dt);
                LastVisitedCells += c.Cells.Length;
            }
            foreach (var c in scheduled)
            {
                var indices = new List<int>(); var values = new List<FireCell>();
                for (int i = 0; i < c.Cells.Length; i++)
                {
                    var a = c.Cells[i]; var b = c.Next[i];
                    if (a.RemainingFuelKgPerM2 != b.RemainingFuelKgPerM2 || a.WaterKgPerM2 != b.WaterKgPerM2
                        || a.Heat != b.Heat || a.Intensity != b.Intensity || a.Smoke != b.Smoke || a.Phase != b.Phase || a.Vegetation != b.Vegetation)
                    { c.Next[i].LastChangedTick = Tick; indices.Add(i); values.Add(c.Next[i]); }
                }
                ulong previous = c.Revision;
                if (indices.Count > 0) c.Revision++;
                var old = c.Cells; c.Cells = c.Next; c.Next = old;
                c.LastTick = Tick;
                if (indices.Count > 0) deltas.Add(new ChunkDelta { Id = c.Id, PreviousRevision = previous, NextRevision = c.Revision,
                    Tick = Tick, WindRevision = Wind.State.Revision, Indices = indices.ToArray(), Values = values.ToArray() });
            }
            foreach (var c in chunks.Values) BacklogSeconds += (Tick - c.LastTick) * StepSeconds;
        }

        private bool Neighbor(Chunk c, int x, int z, out FireCell result)
        {
            int dx = (int)Math.Floor(x / (double)Size), dz = (int)Math.Floor(z / (double)Size);
            if (chunks.TryGetValue(new ChunkId(c.Id.Sector, c.Id.X + dx, c.Id.Z + dz), out var other))
            { result = other.Cells[(z - dz * Size) * Size + x - dx * Size]; return true; }
            result = default; return false;
        }

        private FireCell Advance(Chunk chunk, int index, double dt)
        {
            var cell = chunk.Cells[index];
            int x = index % Size, z = index / Size;
            double east = ((double)chunk.Id.X * Size + x + 0.5) * CellSizeM;
            double north = ((double)chunk.Id.Z * Size + z + 0.5) * CellSizeM;
            var wind = Wind.Sample(east, north, cell.ElevationM, Tick);
            double incoming = 0, upwind = 0, speed = Math.Abs(wind.X) + Math.Abs(wind.Z);
            for (int dz = -1; dz <= 1; dz++)
            for (int dx = -1; dx <= 1; dx++)
            {
                if ((dx == 0 && dz == 0) || !Neighbor(chunk, x + dx, z + dz, out var neighbor)) continue;
                double distance = Math.Sqrt(dx * dx + dz * dz) * CellSizeM;
                double projection = -(wind.X * dx + wind.Z * dz) * CellSizeM / distance;
                double slope = (cell.ElevationM - neighbor.ElevationM) / distance;
                double jitter = 0.9 + 0.2 * SimMath.Noise(Wind.Seed, chunk.Id.X * Size + x, chunk.Id.Z * Size + z, 0);
                double fuelFactor = SimMath.Clamp(cell.RemainingFuelKgPerM2 / 2, 0, 1);
                double kind = cell.Fuel == FuelKind.Grass ? 1.2 : cell.Fuel == FuelKind.PineLitter ? 0.8 : 1;
                double dry = (1 - cell.Moisture) * (1 - cell.Moisture) / (1 + cell.WaterKgPerM2);
                double rate = Math.Min(2, 0.8 * kind * fuelFactor * dry * jitter * Math.Exp(SimMath.Clamp(0.18 * projection + 3 * slope, -1.4, 1.4)));
                incoming += neighbor.Intensity * rate / distance;
                if (dz == 0 && dx == -Math.Sign(wind.X)) upwind += Math.Abs(wind.X) * neighbor.Smoke;
                if (dx == 0 && dz == -Math.Sign(wind.Z)) upwind += Math.Abs(wind.Z) * neighbor.Smoke;
            }
            double evaporation = Math.Min(cell.WaterKgPerM2, 0.02 * dt);
            cell.WaterKgPerM2 -= evaporation; EvaporatedWaterKg += evaporation * CellSizeM * CellSizeM;
            double alpha = Math.Min(0.5, speed * dt / CellSizeM);
            cell.Smoke = SimMath.Clamp((1 - alpha) * cell.Smoke + (speed > 0 ? alpha * upwind / speed : 0)
                + dt * (0.4 * cell.Intensity - 0.15 * cell.Smoke), 0, 1);
            if (cell.Phase == BurnPhase.Charred) return cell;
            double selfHeat = cell.Phase == BurnPhase.Burning ? 0.4 * (1 - cell.Moisture) / (1 + cell.WaterKgPerM2) : 0;
            cell.Heat = SimMath.Clamp(cell.Heat + dt * (incoming + selfHeat - 0.04 * cell.Heat - 0.8 * cell.WaterKgPerM2), 0, 1);
            if (cell.Phase == BurnPhase.Burning || cell.Phase == BurnPhase.Smoldering)
            {
                cell.Intensity = (cell.Phase == BurnPhase.Burning ? 1 : 0.15) * cell.Heat / (1 + cell.WaterKgPerM2);
                if (cell.Intensity < 0.05 || cell.WaterKgPerM2 >= 0.75)
                { cell.Phase = BurnPhase.Extinguished; cell.Intensity = 0; cell.Vegetation = VegetationPhase.Charred; }
                else
                {
                    double used = Math.Min(cell.RemainingFuelKgPerM2, dt * (cell.Phase == BurnPhase.Burning ? 0.04 : 0.01) * (0.2 + 0.8 * cell.Intensity));
                    cell.RemainingFuelKgPerM2 -= used; ConsumedFuelKg += used * CellSizeM * CellSizeM;
                    if (cell.RemainingFuelKgPerM2 <= 0) { cell.Phase = BurnPhase.Charred; cell.Intensity = cell.Heat = 0; cell.Vegetation = VegetationPhase.Charred; }
                    else if (cell.RemainingFuelKgPerM2 <= cell.InitialFuelKgPerM2 * 0.15) cell.Phase = BurnPhase.Smoldering;
                }
            }
            else if (cell.RemainingFuelKgPerM2 > 0 && cell.Moisture < 0.85 && cell.WaterKgPerM2 < 0.75)
            {
                if (cell.Heat >= 0.8)
                {
                    cell.Phase = BurnPhase.Burning; cell.Intensity = cell.Heat;
                    if (cell.Vegetation != VegetationPhase.Charred) cell.Vegetation = VegetationPhase.Burning;
                }
                else if (cell.Heat > 0.02 && cell.Phase != BurnPhase.Extinguished) cell.Phase = BurnPhase.Heating;
                else if (cell.Phase != BurnPhase.Extinguished) cell.Phase = BurnPhase.Unburned;
            }
            return cell;
        }

        public ChunkDelta[] CaptureDeltas() => deltas.ToArray();

        public FireSnapshot CaptureSnapshot()
        {
            var snapshots = new List<ChunkSnapshot>();
            foreach (var c in chunks.Values) snapshots.Add(new ChunkSnapshot { Id = c.Id, Revision = c.Revision,
                LastSimulatedTick = c.LastTick, Active = c.Active, Cells = (FireCell[])c.Cells.Clone() });
            return new FireSnapshot { Seed = Wind.Seed, Size = Size, CellSizeM = CellSizeM, StepSeconds = StepSeconds,
                AppliedWaterKg = AppliedWaterKg, EvaporatedWaterKg = EvaporatedWaterKg, ConsumedFuelKg = ConsumedFuelKg, InactiveCursor = inactiveCursor,
                Tick = Tick, Wind = Wind.State, Chunks = snapshots.ToArray(), Checksum = Checksum() };
        }

        // Canonical sorted IDs and explicit IEEE-754 bits; a regression digest, not a cryptographic signature.
        public string Checksum()
        {
            ulong hash = 14695981039346656037UL;
            void Add(ulong n) { unchecked { for (int i = 0; i < 8; i++) { hash ^= (byte)(n >> (8 * i)); hash *= 1099511628211UL; } } }
            void Number(double n) => Add(unchecked((ulong)BitConverter.DoubleToInt64Bits(n)));
            Add(Tick); Add(unchecked((ulong)Wind.Seed)); Add((ulong)Size); Number(CellSizeM);
            Add(Wind.State.Revision); Number(Wind.State.EastMps); Number(Wind.State.NorthMps); Number(Wind.State.GustMps);
            Add((ulong)inactiveCursor); Number(AppliedWaterKg); Number(EvaporatedWaterKg); Number(ConsumedFuelKg);
            foreach (var c in chunks.Values)
            {
                Add(unchecked((ulong)c.Id.Sector)); Add(unchecked((ulong)c.Id.X)); Add(unchecked((ulong)c.Id.Z));
                Add(c.Revision); Add(c.LastTick); Add(c.Active ? 1UL : 0UL);
                foreach (var v in c.Cells)
                {
                    Add((ulong)v.Fuel); Number(v.InitialFuelKgPerM2); Number(v.RemainingFuelKgPerM2); Number(v.Moisture);
                    Number(v.WaterKgPerM2); Number(v.Heat); Number(v.Intensity); Number(v.Smoke); Number(v.ElevationM);
                    Add((ulong)v.Phase); Add((ulong)v.Vegetation); Add(v.VegetationId); Add(v.LastChangedTick);
                }
            }
            return hash.ToString("x16");
        }
    }
}
