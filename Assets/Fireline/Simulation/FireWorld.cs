using System;
using System.Collections.Generic;
using System.IO;

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
        /// <summary>
        /// Cells along one chunk edge, between 4 and 32; total cells per chunk are Size squared.
        /// </summary>
        public int Size { get; }
        /// <summary>
        /// Width/depth of each square cell in meters, used to convert areal density into kilograms.
        /// </summary>
        public double CellSizeM { get; }
        /// <summary>
        /// One authoritative fire tick represents 0.2 simulated seconds (5 Hz), independent of render frames.
        /// </summary>
        public double StepSeconds => 0.2;
        /// <summary>
        /// Completed global fire steps, including steps where some inactive chunks wait their turn.
        /// </summary>
        public ulong Tick { get; private set; }
        /// <summary>
        /// Server-owned wind sampler used by cell updates; no client particles feed back into this field.
        /// </summary>
        public WindField Wind { get; }
        /// <summary>
        /// Number of cells updated during the latest step, for checking the bounded work budget.
        /// </summary>
        public int LastVisitedCells { get; private set; }
        /// <summary>
        /// Sum of chunk update ages in simulated seconds; this is not a queued wall-time pause.
        /// </summary>
        public double BacklogSeconds { get; private set; }
        /// <summary>
        /// Cumulative kilograms admitted by validated water applications.
        /// </summary>
        public double AppliedWaterKg { get; private set; }
        /// <summary>
        /// Cumulative kilograms removed by the modeled evaporation term.
        /// </summary>
        public double EvaporatedWaterKg { get; private set; }
        /// <summary>
        /// Cumulative kilograms burned, summed from cell density loss times cell area.
        /// </summary>
        public double ConsumedFuelKg { get; private set; }

        /// <summary>
        /// Create an empty bounded SI grid with seeded wind; reject invalid side length or 5-100 m cell size.
        /// </summary>
        public FireWorld(int seed, int size = 16, double cellSizeM = 10)
        {
            if (size < 4 || size > 32) throw new ArgumentOutOfRangeException(nameof(size));
            SimMath.Range(cellSizeM, 5, 100, nameof(cellSizeM));
            Size = size; CellSizeM = cellSizeM; Wind = new WindField(seed);
        }

        /// <summary>
        /// Validate capacity, ID, fuel density in kg/m2 and moisture in [0,1], then allocate both cell buffers.
        /// Pack stable vegetation IDs from sector, signed-coordinate bit patterns and local cell index.
        /// At most 16 chunks and four active chunks are accepted; terrain is configured separately before stepping.
        /// </summary>
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

        /// <summary>
        /// Count active chunks so admission and activation enforce the same four-chunk work limit.
        /// </summary>
        private int ActiveCount() { int n = 0; foreach (var c in chunks.Values) if (c.Active) n++; return n; }
        /// <summary>
        /// Change a known chunk scheduling tier, refusing a fifth active chunk.
        /// Dynamic membership starvation bounds are not yet qualified; fixed-membership round-robin is the M01 proof.
        /// </summary>
        public void SetActive(ChunkId id, bool active)
        {
            var c = chunks[id];
            if (active && !c.Active && ActiveCount() >= MaximumActiveChunks) throw new InvalidOperationException("Active chunk budget exceeded.");
            c.Active = active;
        }
        /// <summary>
        /// Return a value copy of a known cell; editing it cannot mutate the world. Invalid IDs/indices throw.
        /// </summary>
        public FireCell GetCell(ChunkId id, int index) => chunks[id].Cells[index];
        /// <summary>
        /// Configure a known cell elevation in meters before the first step; reject later terrain edits or invalid height.
        /// </summary>
        public void SetElevation(ChunkId id, int index, double elevationM)
        {
            if (Tick != 0) throw new InvalidOperationException("Terrain configuration is immutable after stepping.");
            SimMath.Range(elevationM, -1000, 10000, nameof(elevationM));
            var c = chunks[id]; var cell = c.Cells[index]; cell.ElevationM = elevationM; c.Cells[index] = cell;
        }

        /// <summary>
        /// Attempt server-side ignition; return false for charred, exhausted, very moist or wet fuel.
        /// On success set burning heat/intensity, preserve existing charred vegetation, and increment chunk revision.
        /// </summary>
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
        /// <summary>
        /// Validate the entire nonempty weighted footprint before changing any cells; weights must sum to one.
        /// Distribute massKg once across cell area, cool/wet affected cells and update revisions/accounting.
        /// Water can extinguish and char vegetation without deleting remaining fuel; this is a trusted server operation.
        /// </summary>
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

        /// <summary>
        /// Advance one global 0.2 s tick, updating all active chunks plus one round-robin inactive chunk.
        /// Compute selected cells from old buffers before committing any chunk, then record changed-cell revisions.
        /// Inactive updates use accumulated simulated time; work is bounded, but coarse behavior is an approximation.
        /// </summary>
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

        /// <summary>
        /// Resolve local/adjacent cell coordinates, including negative chunk seams, within the same sector.
        /// Return false outside loaded chunks; absent space contributes no neighbor heat/smoke.
        /// </summary>
        private bool Neighbor(Chunk c, int x, int z, out FireCell result)
        {
            int dx = (int)Math.Floor(x / (double)Size), dz = (int)Math.Floor(z / (double)Size);
            if (chunks.TryGetValue(new ChunkId(c.Id.Sector, c.Id.X + dx, c.Id.Z + dz), out var other))
            { result = other.Cells[(z - dz * Size) * Size + x - dx * Size]; return true; }
            result = default; return false;
        }

        /// <summary>
        /// Compute one cell after dt simulated seconds using old neighbor buffers and sampled wind.
        /// Apply estimated wind/slope/fuel/moisture heating, evaporation and coarse smoke transport, then burn/suppress fuel.
        /// Return the next cell while accumulating kg accounting; charred vegetation never regrows. See the model comparison.
        /// </summary>
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

        /// <summary>
        /// Return a new outer array of the latest step deltas; entries/inner arrays remain mutable DTO references.
        /// This is not a transport queue or a deep-copy guarantee and does not include earlier manual mutations.
        /// </summary>
        public ChunkDelta[] CaptureDeltas() => deltas.ToArray();

        /// <summary>
        /// Clone every current cell buffer and include scheduling, wind and mass totals for a detached baseline.
        /// Schema/checksum describe this prototype; the snapshot codec/store supply separate validated persistence boundaries.
        /// </summary>
        public FireSnapshot CaptureSnapshot()
        {
            var snapshots = new List<ChunkSnapshot>();
            foreach (var c in chunks.Values) snapshots.Add(new ChunkSnapshot { Id = c.Id, Revision = c.Revision,
                LastSimulatedTick = c.LastTick, Active = c.Active, Cells = (FireCell[])c.Cells.Clone() });
            return new FireSnapshot { Seed = Wind.Seed, Size = Size, CellSizeM = CellSizeM, StepSeconds = StepSeconds,
                AppliedWaterKg = AppliedWaterKg, EvaporatedWaterKg = EvaporatedWaterKg, ConsumedFuelKg = ConsumedFuelKg, InactiveCursor = inactiveCursor,
                Tick = Tick, Wind = Wind.State, Chunks = snapshots.ToArray(), Checksum = Checksum() };
        }

        /// <summary>
        /// Validate and copy a complete schema-1 checkpoint into a new world, leaving any live authority untouched on failure.
        /// Preserve tick, wind/chunk revisions, inactive scheduling cursor, char and kg totals so the next step can continue.
        /// Recompute the canonical digest and mass balances; this validates data, not its origin or permission to replace a server.
        /// The caller must not mutate the supplied DTO concurrently; worlds remain single-owner, non-thread-safe objects.
        /// </summary>
        public static FireWorld Restore(FireSnapshot snapshot)
        {
            Require(snapshot != null && snapshot.Schema == 1, "Unsupported/missing snapshot.");
            Require(snapshot.Size >= 4 && snapshot.Size <= 32 && snapshot.StepSeconds == 0.2, "Invalid grid/step.");
            Require(Bounded(snapshot.CellSizeM, 5, 100), "Invalid cell size.");
            Require(snapshot.Chunks != null && snapshot.Chunks.Length <= MaximumChunks, "Invalid chunk count.");
            Require(snapshot.InactiveCursor >= 0 && snapshot.InactiveCursor < MaximumChunks, "Invalid scheduler cursor.");
            Require(Bounded(snapshot.Wind.EastMps, -40, 40) && Bounded(snapshot.Wind.NorthMps, -40, 40)
                && Bounded(snapshot.Wind.GustMps, 0, 10), "Invalid wind.");
            Require(snapshot.Wind.Revision != 0 || (snapshot.Wind.EastMps == 0 && snapshot.Wind.NorthMps == 0
                && snapshot.Wind.GustMps == 0), "Unversioned wind change.");
            Require(Bounded(snapshot.AppliedWaterKg, 0, double.MaxValue) && Bounded(snapshot.EvaporatedWaterKg, 0, double.MaxValue)
                && Bounded(snapshot.ConsumedFuelKg, 0, double.MaxValue), "Invalid mass totals.");
            var world = new FireWorld(snapshot.Seed, snapshot.Size, snapshot.CellSizeM);
            world.Tick = snapshot.Tick; world.inactiveCursor = snapshot.InactiveCursor;
            world.Wind.Restore(snapshot.Wind);
            world.AppliedWaterKg = snapshot.AppliedWaterKg; world.EvaporatedWaterKg = snapshot.EvaporatedWaterKg;
            world.ConsumedFuelKg = snapshot.ConsumedFuelKg;
            double initial = 0, remaining = 0, retained = 0;
            ChunkId previous = default; int active = 0;
            for (int n = 0; n < snapshot.Chunks.Length; n++)
            {
                var source = snapshot.Chunks[n];
                Require(source != null, "Missing chunk.");
                var id = source.Id;
                Require(id.Sector >= 0 && id.Sector <= ushort.MaxValue && Math.Abs((long)id.X) <= 10000
                    && Math.Abs((long)id.Z) <= 10000, "Invalid chunk ID.");
                Require(n == 0 || previous.CompareTo(id) < 0, "Chunks must be unique and canonically ordered."); previous = id;
                Require(source.LastSimulatedTick <= snapshot.Tick && source.Cells != null
                    && source.Cells.Length == snapshot.Size * snapshot.Size, "Invalid chunk tick/cell count.");
                if (source.Active) active++;
                Require(active <= MaximumActiveChunks, "Active chunk budget exceeded.");
                world.AddChunk(id, source.Active);
                var target = world.chunks[id]; target.Revision = source.Revision; target.LastTick = source.LastSimulatedTick;
                target.Cells = (FireCell[])source.Cells.Clone();
                for (int i = 0; i < target.Cells.Length; i++)
                {
                    var cell = target.Cells[i];
                    ulong expectedId = ((ulong)(ushort)id.Sector << 48) | ((ulong)(ushort)id.X << 32)
                        | ((ulong)(ushort)id.Z << 16) | (ushort)i;
                    Require(Enum.IsDefined(typeof(FuelKind), cell.Fuel) && Enum.IsDefined(typeof(BurnPhase), cell.Phase)
                        && Enum.IsDefined(typeof(VegetationPhase), cell.Vegetation), "Invalid cell enum.");
                    Require(Bounded(cell.InitialFuelKgPerM2, 0, 20) && Bounded(cell.RemainingFuelKgPerM2, 0, cell.InitialFuelKgPerM2)
                        && Bounded(cell.Moisture, 0, 1) && Bounded(cell.WaterKgPerM2, 0, double.MaxValue)
                        && Bounded(cell.Heat, 0, 1) && Bounded(cell.Intensity, 0, 1) && Bounded(cell.Smoke, 0, 1)
                        && Bounded(cell.ElevationM, -1000, 10000), "Invalid cell number.");
                    Require(cell.VegetationId == expectedId && cell.LastChangedTick <= snapshot.Tick, "Invalid cell identity/tick.");
                    Require(cell.Phase != BurnPhase.Charred || (cell.RemainingFuelKgPerM2 == 0 && cell.Heat == 0
                        && cell.Intensity == 0 && cell.Vegetation == VegetationPhase.Charred), "Invalid exhausted cell.");
                    Require(cell.Phase != BurnPhase.Extinguished || (cell.Intensity == 0 && cell.Vegetation == VegetationPhase.Charred),
                        "Invalid extinguished cell.");
                    double area = world.CellSizeM * world.CellSizeM;
                    initial += cell.InitialFuelKgPerM2 * area; remaining += cell.RemainingFuelKgPerM2 * area;
                    retained += cell.WaterKgPerM2 * area;
                }
                world.BacklogSeconds += (snapshot.Tick - source.LastSimulatedTick) * world.StepSeconds;
            }
            Require(Close(initial, remaining + world.ConsumedFuelKg) && Close(world.AppliedWaterKg, retained + world.EvaporatedWaterKg),
                "Snapshot mass accounting failed.");
            Require(string.Equals(world.Checksum(), snapshot.Checksum, StringComparison.Ordinal), "Snapshot digest mismatch.");
            return world;
        }

        /// <summary>Check finite inclusive numeric bounds without mutating a candidate checkpoint.</summary>
        private static bool Bounded(double value, double low, double high) => SimMath.Finite(value) && value >= low && value <= high;

        /// <summary>Allow small floating-point summation error in kg balances; reject non-finite sums.</summary>
        private static bool Close(double a, double b) => SimMath.Finite(a) && SimMath.Finite(b)
            && Math.Abs(a - b) <= 1e-6 + 1e-10 * Math.Max(Math.Abs(a), Math.Abs(b));

        /// <summary>Reject malformed checkpoint data with one consistent exception before a candidate world can be installed.</summary>
        private static void Require(bool condition, string message) { if (!condition) throw new InvalidDataException(message); }

        // Canonical sorted IDs and explicit IEEE-754 bits; a regression digest, not a cryptographic signature.
        /// <summary>
        /// Produce a sorted FNV-1a regression digest from explicit integer and IEEE-754 double bits.
        /// This detects state differences; it is neither cryptographic authentication nor universal cross-runtime determinism.
        /// </summary>
        public string Checksum()
        {
            ulong hash = 14695981039346656037UL;
            // Feed one unsigned 64-bit value least-significant byte first into the running FNV digest.
            void Add(ulong n) { unchecked { for (int i = 0; i < 8; i++) { hash ^= (byte)(n >> (8 * i)); hash *= 1099511628211UL; } } }
            // Preserve the exact double bit pattern when adding a simulation number to the digest.
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
