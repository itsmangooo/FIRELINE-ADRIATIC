using System;
using System.Collections.Generic;
using Fireline.Server;
using Fireline.Simulation;
using NUnit.Framework;

namespace Fireline.Tests
{
    public sealed class FireSimulationTests
    {
        private static readonly ChunkId Id = new ChunkId(1, 0, 0);
        private static FireWorld Small(double fuel = 2)
        {
            var world = new FireWorld(415, 4); world.AddChunk(Id, true, fuel); return world;
        }

        [Test]
        public void ScenarioMatrixIsDeterministicDirectionalAndSuppressible()
        {
            var result = FireVerification.Run();
            Assert.That(result.passed, Is.True); Assert.That(result.cases.Length, Is.EqualTo(9));
        }

        [Test]
        public void WaterMassIsDistributedAndConservedAfterEvaporation()
        {
            var world = Small();
            world.ApplyWater(250, new[] { new WaterHit(Id, 0, 0.25), new WaterHit(Id, 1, 0.75) });
            Assert.That(world.GetCell(Id, 0).WaterKgPerM2, Is.EqualTo(0.625).Within(1e-12));
            for (int i = 0; i < 50; i++) world.Step();
            double retained = 0; for (int i = 0; i < 16; i++) retained += world.GetCell(Id, i).WaterKgPerM2 * 100;
            Assert.That(retained + world.EvaporatedWaterKg, Is.EqualTo(250).Within(1e-9));
        }

        [Test]
        public void InvalidWaterCoverageFailsAtomically()
        {
            var world = Small(); string before = world.Checksum();
            Assert.Throws<ArgumentException>(() => world.ApplyWater(100, new[] { new WaterHit(Id, 0, 0.5), new WaterHit(Id, 99, 0.5) }));
            Assert.Throws<ArgumentException>(() => world.ApplyWater(100, new[] { new WaterHit(Id, 0, 0.9) }));
            Assert.Throws<ArgumentOutOfRangeException>(() => world.ApplyWater(double.NaN, new[] { new WaterHit(Id, 0, 1) }));
            Assert.That(world.Checksum(), Is.EqualTo(before));
        }

        [Test]
        public void ClientWaterRequestCannotMutateAuthority()
        {
            var world = Small(); var service = new AuthoritativeFireService(world); string before = world.Checksum();
            Assert.That(service.TryApplyClientWater(100, new[] { new WaterHit(Id, 0, 1) }, out string reason), Is.False);
            Assert.That(reason, Is.Not.Empty); Assert.That(world.Checksum(), Is.EqualTo(before));
        }

        [Test]
        public void SmallDoseReducesIntensityAndStrongDoseExtinguishesWithoutDeletingFuel()
        {
            var world = Small(); world.Ignite(Id, 5);
            world.ApplyWater(10, new[] { new WaterHit(Id, 5, 1) });
            Assert.That(world.GetCell(Id, 5).Intensity, Is.InRange(0.1, 0.99));
            Assert.That(world.GetCell(Id, 5).Phase, Is.EqualTo(BurnPhase.Burning));
            world.ApplyWater(200, new[] { new WaterHit(Id, 5, 1) });
            Assert.That(world.GetCell(Id, 5).Phase, Is.EqualTo(BurnPhase.Extinguished));
            Assert.That(world.GetCell(Id, 5).Intensity, Is.Zero);
            Assert.That(world.GetCell(Id, 5).RemainingFuelKgPerM2, Is.EqualTo(2));
            Assert.That(world.Ignite(Id, 5), Is.False, "Wet fuel cannot immediately reignite.");
        }

        [Test]
        public void BurnConsumesFuelThroughSmolderingAndNeverRegrowsChar()
        {
            var world = Small(0.1); world.Ignite(Id, 5); bool smoldered = false;
            for (int i = 0; i < 500; i++) { world.Step(); smoldered |= world.GetCell(Id, 5).Phase == BurnPhase.Smoldering; }
            var cell = world.GetCell(Id, 5);
            Assert.That(smoldered, Is.True); Assert.That(cell.Phase, Is.EqualTo(BurnPhase.Charred));
            Assert.That(cell.RemainingFuelKgPerM2, Is.Zero); Assert.That(cell.Vegetation, Is.EqualTo(VegetationPhase.Charred));
            Assert.That(world.Ignite(Id, 5), Is.False);
            world.ApplyWater(100, new[] { new WaterHit(Id, 5, 1) }); world.Step();
            Assert.That(world.GetCell(Id, 5).Phase, Is.EqualTo(BurnPhase.Charred));
        }

        [Test]
        public void AdjacentNegativeCoordinateChunkReceivesHeatAcrossBoundary()
        {
            var world = Small(); var left = new ChunkId(1, -1, 0); world.AddChunk(left, true);
            world.Ignite(left, 7); for (int i = 0; i < 10; i++) world.Step();
            Assert.That(world.GetCell(Id, 4).Heat, Is.GreaterThan(0));
            Assert.That(world.GetCell(left, 7).VegetationId, Is.Not.EqualTo(world.GetCell(Id, 7).VegetationId));
        }

        [Test]
        public void InsertionOrderCannotChangeWorldResults()
        {
            FireWorld Create(bool reverse)
            {
                var world = new FireWorld(415, 4); var other = new ChunkId(1, 1, 0);
                world.AddChunk(reverse ? other : Id, true); world.AddChunk(reverse ? Id : other, true);
                world.Ignite(Id, 7); return world;
            }
            var a = Create(false); var b = Create(true);
            for (int i = 0; i < 100; i++) { a.Step(); b.Step(); Assert.That(a.Checksum(), Is.EqualTo(b.Checksum())); }
        }

        [Test]
        public void ChunkBudgetsBoundWorkAndInactiveChunksKeepProgressing()
        {
            var world = new FireWorld(415, 4); var ids = new List<ChunkId>();
            for (int i = 0; i < 16; i++) { var id = new ChunkId(1, i, 0); ids.Add(id); world.AddChunk(id, i < 4); world.Ignite(id, 5); }
            Assert.Throws<InvalidOperationException>(() => world.SetActive(ids[4], true));
            Assert.Throws<InvalidOperationException>(() => world.AddChunk(new ChunkId(1, 20, 0), false));
            for (int i = 0; i < 120; i++)
            {
                world.Step(); Assert.That(world.LastVisitedCells, Is.LessThanOrEqualTo(5 * 16));
                Assert.That(world.BacklogSeconds, Is.LessThan(40));
            }
            foreach (var id in ids) Assert.That(world.GetCell(id, 5).RemainingFuelKgPerM2, Is.LessThan(2));
        }

        [Test]
        public void SnapshotsAreDetachedAndDeltasHaveOrderedRevisions()
        {
            var world = Small(); world.Ignite(Id, 5); var old = world.CaptureSnapshot();
            string digest = world.Checksum(); old.Chunks[0].Cells[5].RemainingFuelKgPerM2 = 100;
            Assert.That(world.Checksum(), Is.EqualTo(digest)); world.Step();
            var delta = world.CaptureDeltas()[0]; Assert.That(delta.NextRevision, Is.EqualTo(delta.PreviousRevision + 1));
            Assert.That(delta.Tick, Is.EqualTo(world.Tick)); Assert.That(delta.Indices.Length, Is.EqualTo(delta.Values.Length));
            for (int i = 0; i < delta.Indices.Length; i++) Assert.That(delta.Values[i].LastChangedTick, Is.EqualTo(world.Tick));
        }

        [Test]
        public void SeededGustsAndWindChangesAreFiniteRepeatableAndVersioned()
        {
            var a = new WindField(415); var b = new WindField(415); a.Set(8, 0, 2); b.Set(8, 0, 2);
            double first = a.Sample(155, 155, 0, 0).X;
            for (ulong tick = 0; tick < 100; tick++)
                Assert.That(a.Sample(155, 155, 20, tick).X, Is.EqualTo(b.Sample(155, 155, 20, tick).X));
            Assert.That(a.Sample(155, 155, 0, 10).X, Is.Not.EqualTo(first));
            a.Set(0, 8, 0); Assert.That(a.State.Revision, Is.EqualTo(2UL));
            Assert.That(a.Sample(0, 0, 0, 100).Z, Is.EqualTo(8));
            Assert.Throws<ArgumentOutOfRangeException>(() => a.Set(double.PositiveInfinity, 0, 0));
        }

        [Test]
        public void SmokeHazardMovesDownwindWithoutClientParticles()
        {
            var world = Small(); world.Wind.Set(8, 0, 0); world.Ignite(Id, 5);
            for (int i = 0; i < 10; i++) world.Step();
            Assert.That(world.GetCell(Id, 6).Smoke, Is.GreaterThan(world.GetCell(Id, 4).Smoke));
        }

        [Test]
        public void FuelClassChangesNeighborHeatingUnderEquivalentInputs()
        {
            double Heat(FuelKind kind)
            {
                var world = new FireWorld(415, 4); world.AddChunk(Id, true, 2, 0.1, kind); world.Ignite(Id, 5);
                for (int i = 0; i < 20; i++) world.Step();
                return world.GetCell(Id, 6).Heat;
            }
            Assert.That(Heat(FuelKind.Grass), Is.GreaterThan(Heat(FuelKind.Brush)));
            Assert.That(Heat(FuelKind.Brush), Is.GreaterThan(Heat(FuelKind.PineLitter)));
        }

        [Test]
        public void DryingAllowsReignitionWithoutRestoringCharredVegetation()
        {
            var world = Small(); world.Ignite(Id, 5);
            world.ApplyWater(100, new[] { new WaterHit(Id, 5, 1) });
            Assert.That(world.Ignite(Id, 5), Is.False);
            for (int i = 0; i < 300; i++) world.Step();
            Assert.That(world.GetCell(Id, 5).WaterKgPerM2, Is.Zero.Within(1e-9));
            Assert.That(world.Ignite(Id, 5), Is.True);
            Assert.That(world.GetCell(Id, 5).Vegetation, Is.EqualTo(VegetationPhase.Charred));
        }
    }
}
