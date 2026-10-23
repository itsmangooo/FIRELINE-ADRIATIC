using System;
using System.Collections.Generic;
using System.Diagnostics;
using Fireline.Simulation;

namespace Fireline.Server
{
    [Serializable]
    public sealed class FireSample
    {
        public string scenario;
        public ulong tick;
        public double seconds, burnedAreaM2, fuelConsumedKg, waterRetainedKg, intensitySum, smokeSum, centroidEastM, centroidNorthM;
        public double stepMs;
        public int visitedCells;
    }

    [Serializable]
    public sealed class FireCaseResult
    {
        public string scenario, checksum;
        public FireSample[] samples;
        public FireSnapshot finalState;
    }

    [Serializable]
    public sealed class FireVerificationResult
    {
        public int schema = 1, seed = 415, steps = 600, gridSize = 32;
        public double stepSeconds = 0.2, cellSizeM = 10;
        public bool passed;
        public string runtime, limitations = "Synthetic gameplay approximation; no scientific calibration, graphics, client replication, persistence or capacity qualification.";
        public FireCaseResult[] cases;
        public double[] budgetStepMs;
        public int budgetCapacityCells, budgetMaximumVisitedCells, budgetGc0Collections;
        public double budgetMaximumBacklogSeconds;
    }

    public static class FireVerification
    {
        public static readonly ChunkId TestChunk = new ChunkId(1, 0, 0);
        public static FireWorld Fixture(double eastMps, double northMps, double moisture = 0.1, double slope = 0, double gust = 0)
        {
            var world = new FireWorld(415, 32);
            world.AddChunk(TestChunk, true, 2, moisture);
            for (int z = 0; z < 32; z++)
            for (int x = 0; x < 32; x++) world.SetElevation(TestChunk, z * 32 + x, slope * x * 10);
            world.Wind.Set(eastMps, northMps, gust);
            world.Ignite(TestChunk, 16 * 32 + 16);
            return world;
        }

        public static FireCaseResult RunCase(string name, double east, double north, bool rotate = false, bool suppress = false,
            double moisture = 0.1, double slope = 0, double gust = 0)
        {
            var world = Fixture(east, north, moisture, slope, gust);
            var service = new AuthoritativeFireService(world);
            var rows = new List<FireSample>();
            for (int i = 0; i < 600; i++)
            {
                if (rotate && i == 300) world.Wind.Set(0, 8, gust);
                if (suppress && i == 50)
                {
                    var hits = new List<WaterHit>();
                    for (int z = 13; z <= 19; z++)
                    for (int x = 13; x <= 19; x++) hits.Add(new WaterHit(TestChunk, z * 32 + x, 1.0 / 49));
                    service.ApplyManualTestWater(14700, hits.ToArray()); // 3 kg/m² over 4900 m².
                }
                long start = Stopwatch.GetTimestamp(); world.Step();
                double milliseconds = (Stopwatch.GetTimestamp() - start) * 1000.0 / Stopwatch.Frequency;
                var row = Measure(world, name); row.stepMs = milliseconds; row.visitedCells = world.LastVisitedCells;
                rows.Add(row);
            }
            return new FireCaseResult { scenario = name, checksum = world.Checksum(), samples = rows.ToArray(), finalState = world.CaptureSnapshot() };
        }

        public static FireSample Measure(FireWorld world, string name)
        {
            var row = new FireSample { scenario = name, tick = world.Tick, seconds = world.Tick * world.StepSeconds,
                fuelConsumedKg = world.ConsumedFuelKg };
            int burned = 0; double initialFuelKg = 0, remainingFuelKg = 0;
            for (int i = 0; i < world.Size * world.Size; i++)
            {
                var c = world.GetCell(TestChunk, i);
                if (!double.IsFinite(c.Heat) || !double.IsFinite(c.WaterKgPerM2) || !double.IsFinite(c.Smoke)
                    || c.RemainingFuelKgPerM2 < 0 || c.RemainingFuelKgPerM2 > c.InitialFuelKgPerM2
                    || c.Heat < 0 || c.Heat > 1 || c.Intensity < 0 || c.Intensity > 1 || c.Smoke < 0 || c.Smoke > 1)
                    throw new InvalidOperationException("Non-finite/out-of-bounds cell state.");
                row.waterRetainedKg += c.WaterKgPerM2 * world.CellSizeM * world.CellSizeM;
                initialFuelKg += c.InitialFuelKgPerM2 * world.CellSizeM * world.CellSizeM;
                remainingFuelKg += c.RemainingFuelKgPerM2 * world.CellSizeM * world.CellSizeM;
                row.intensitySum += c.Intensity; row.smokeSum += c.Smoke;
                if (c.Phase == BurnPhase.Burning || c.Phase == BurnPhase.Smoldering || c.Phase == BurnPhase.Extinguished || c.Phase == BurnPhase.Charred)
                {
                    burned++; row.centroidEastM += ((i % world.Size) - 16) * world.CellSizeM;
                    row.centroidNorthM += ((i / world.Size) - 16) * world.CellSizeM;
                }
            }
            row.burnedAreaM2 = burned * world.CellSizeM * world.CellSizeM;
            if (burned > 0) { row.centroidEastM /= burned; row.centroidNorthM /= burned; }
            if (Math.Abs(world.AppliedWaterKg - row.waterRetainedKg - world.EvaporatedWaterKg) > 1e-6)
                throw new InvalidOperationException("Water accounting failed.");
            if (Math.Abs(initialFuelKg - remainingFuelKg - world.ConsumedFuelKg) > 1e-6)
                throw new InvalidOperationException("Fuel accounting failed.");
            return row;
        }

        public static FireVerificationResult Run()
        {
            var east = RunCase("east", 8, 0);
            var north = RunCase("north", 0, 8);
            var repeat = RunCase("east-repeat", 8, 0);
            var rotated = RunCase("rotated", 8, 0, rotate: true);
            var wet = RunCase("suppressed", 8, 0, suppress: true);
            FireSample Last(FireCaseResult c) => c.samples[c.samples.Length - 1];
            var calm = RunCase("calm", 0, 0);
            var moisture = RunCase("moist", 8, 0, moisture: 0.7);
            var slope = RunCase("uphill", 0, 0, slope: 0.2);
            var gust = RunCase("gust", 8, 0, gust: 2);
            bool passed = east.checksum == repeat.checksum && Last(calm).burnedAreaM2 > 100 && Last(east).centroidEastM > 5 && Last(north).centroidNorthM > 5
                && Last(rotated).centroidNorthM > Last(east).centroidNorthM + 2
                && Last(wet).burnedAreaM2 < Last(east).burnedAreaM2
                && Last(moisture).burnedAreaM2 < Last(east).burnedAreaM2
                && Last(slope).centroidEastM > Last(calm).centroidEastM + 2 && gust.checksum != east.checksum;
            if (!passed) throw new InvalidOperationException("M01 scenario direction/suppression/determinism acceptance failed.");
            var result = new FireVerificationResult { passed = true, runtime = Environment.Version.ToString(),
                cases = new[] { calm, east, north, repeat, rotated, wet, moisture, slope, gust }, budgetCapacityCells = 16 * 32 * 32 };
            var budget = new FireWorld(415, 32);
            for (int i = 0; i < 16; i++)
            { var id = new ChunkId(1, i, 0); budget.AddChunk(id, i < 4); budget.Ignite(id, 16 * 32 + 16); }
            result.budgetStepMs = new double[120];
            int gc = GC.CollectionCount(0);
            for (int i = 0; i < result.budgetStepMs.Length; i++)
            {
                long start = Stopwatch.GetTimestamp(); budget.Step();
                result.budgetStepMs[i] = (Stopwatch.GetTimestamp() - start) * 1000.0 / Stopwatch.Frequency;
                result.budgetMaximumVisitedCells = Math.Max(result.budgetMaximumVisitedCells, budget.LastVisitedCells);
                result.budgetMaximumBacklogSeconds = Math.Max(result.budgetMaximumBacklogSeconds, budget.BacklogSeconds);
            }
            result.budgetGc0Collections = GC.CollectionCount(0) - gc;
            if (result.budgetMaximumVisitedCells > 5 * 32 * 32 || result.budgetMaximumBacklogSeconds >= 40)
                throw new InvalidOperationException("Bounded scheduler acceptance failed.");
            return result;
        }
    }
}
