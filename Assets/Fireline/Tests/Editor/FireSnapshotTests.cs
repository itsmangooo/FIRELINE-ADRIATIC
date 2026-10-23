using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Fireline.Server;
using Fireline.Simulation;
using NUnit.Framework;

namespace Fireline.Tests
{
    public sealed class FireSnapshotTests
    {
        /// <summary>Create active and coarse inactive chunks across a negative seam, with gusts and an exact water dose.</summary>
        private static FireWorld Fixture()
        {
            var world = new FireWorld(415, 4);
            var a = new ChunkId(1, -1, 0); var b = new ChunkId(1, 0, 0);
            world.AddChunk(a, true, 0.1); world.AddChunk(b, false);
            world.AddChunk(new ChunkId(1, 1, 0), false); world.AddChunk(new ChunkId(1, 2, 0), false);
            world.Wind.Set(8, 0, 2); world.Ignite(a, 7); world.Ignite(b, 5);
            world.ApplyWater(100, new[] { new WaterHit(b, 15, 1) });
            for (int i = 0; i < 301; i++) world.Step();
            return world;
        }

        /// <summary>Preserve all canonical state through encode/decode, then require identical future steps and delta revisions.</summary>
        [Test]
        public void RoundTripContinuesIdenticallyIncludingCharWindAndInactiveCursor()
        {
            var original = Fixture(); byte[] bytes = FireSnapshotCodec.Encode(original.CaptureSnapshot());
            var restored = FireWorld.Restore(FireSnapshotCodec.Decode(bytes));
            Assert.That(restored.Checksum(), Is.EqualTo(original.Checksum()));
            Assert.That(FireSnapshotCodec.Encode(restored.CaptureSnapshot()), Is.EqualTo(bytes));
            Assert.That(restored.CaptureSnapshot().Chunks.SelectMany(c => c.Cells).Any(c => c.Vegetation == VegetationPhase.Charred), Is.True);
            for (int i = 0; i < 100; i++)
            {
                if (i == 20) { original.Wind.Set(0, 8, 2); restored.Wind.Set(0, 8, 2); }
                original.Step(); restored.Step();
                Assert.That(restored.Checksum(), Is.EqualTo(original.Checksum()));
                Assert.That(restored.CaptureDeltas().Select(d => d.NextRevision), Is.EqualTo(original.CaptureDeltas().Select(d => d.NextRevision)));
            }
        }

        /// <summary>Accept a valid empty world and maximum-capacity world within the same explicit allocation/byte bounds.</summary>
        [Test]
        public void EmptyAndMaximumWorldsFitTheFormat()
        {
            var empty = new FireWorld(1); var encoded = FireSnapshotCodec.Encode(empty.CaptureSnapshot());
            Assert.That(encoded.Length, Is.EqualTo(FireSnapshotCodec.MinimumBytes));
            Assert.That(FireWorld.Restore(FireSnapshotCodec.Decode(encoded)).Checksum(), Is.EqualTo(empty.Checksum()));
            var maximum = new FireWorld(415, 32);
            for (int i = 0; i < 16; i++) maximum.AddChunk(new ChunkId(65535, i - 8, -10000), i < 4);
            byte[] bytes = FireSnapshotCodec.Encode(maximum.CaptureSnapshot());
            Assert.That(bytes.Length, Is.LessThan(FireSnapshotCodec.MaximumBytes));
            Assert.That(FireWorld.Restore(FireSnapshotCodec.Decode(bytes)).Checksum(), Is.EqualTo(maximum.Checksum()));
        }

        /// <summary>Reject byte corruption, truncation and excessive input before any decoded state can be admitted.</summary>
        [Test]
        public void InvalidByteEnvelopesAreRejected()
        {
            byte[] bytes = FireSnapshotCodec.Encode(Fixture().CaptureSnapshot());
            bytes[70] ^= 1;
            Assert.Throws<InvalidDataException>(() => FireSnapshotCodec.Decode(bytes));
            Assert.Throws<InvalidDataException>(() => FireSnapshotCodec.Decode(bytes.Take(100).ToArray()));
            Assert.Throws<InvalidDataException>(() => FireSnapshotCodec.Decode(new byte[FireSnapshotCodec.MaximumBytes + 1]));
            Assert.Throws<InvalidDataException>(() => FireSnapshotCodec.Decode(null));
        }

        /// <summary>Even with a matching byte hash, unsupported schema, impossible counts/flags and trailing bytes fail parsing.</summary>
        [Test]
        public void ValidHashCannotBypassSchemaCountsFlagsOrTrailingData()
        {
            byte[] bytes = FireSnapshotCodec.Encode(Fixture().CaptureSnapshot());
            // Header offsets are fixed by format v1: schema 4, side 12, chunk count 108, first active flag 140.
            foreach (var edit in new[] { new[] { 4, 2 }, new[] { 12, int.MaxValue }, new[] { 108, int.MaxValue } })
            {
                var changed = (byte[])bytes.Clone(); Array.Copy(BitConverter.GetBytes(edit[1]), 0, changed, edit[0], 4);
                Assert.Throws<InvalidDataException>(() => FireSnapshotCodec.Decode(Rehash(changed)));
            }
            var flag = (byte[])bytes.Clone(); flag[140] = 2;
            Assert.Throws<InvalidDataException>(() => FireSnapshotCodec.Decode(Rehash(flag)));
            var trailing = new byte[bytes.Length + 1]; Array.Copy(bytes, trailing, bytes.Length - 32);
            Assert.Throws<InvalidDataException>(() => FireSnapshotCodec.Decode(Rehash(trailing)));
            var truncated = bytes.Take(bytes.Length - 100).ToArray();
            Assert.Throws<InvalidDataException>(() => FireSnapshotCodec.Decode(Rehash(truncated)));
        }

        /// <summary>Reject malformed numeric/identity/accounting state, while keeping the already running world unchanged.</summary>
        [Test]
        public void InvalidSnapshotCandidatesCannotReplaceOrMutateAuthority()
        {
            var world = Fixture(); string before = world.Checksum();
            Action<FireSnapshot>[] corruptions = {
                s => s.Chunks[0].Cells[0].Heat = double.NaN,
                s => s.Chunks[0].Cells[0].VegetationId++,
                s => s.Chunks[0].Cells[0].Fuel = (FuelKind)99,
                s => s.Chunks[0].Cells[0].LastChangedTick = s.Tick + 1,
                s => s.Chunks[0].LastSimulatedTick = s.Tick + 1,
                s => s.Chunks[1].Id = s.Chunks[0].Id,
                s => s.AppliedWaterKg += 100,
                s => s.Wind.Revision = 0,
                s => s.Checksum = "0000000000000000",
                s => s.InactiveCursor = 16,
                s => s.StepSeconds = 0.1
            };
            foreach (var corrupt in corruptions)
            {
                var state = world.CaptureSnapshot(); corrupt(state);
                Assert.Throws<InvalidDataException>(() => FireWorld.Restore(state));
                Assert.That(world.Checksum(), Is.EqualTo(before));
            }
        }

        /// <summary>Mutating input/output DTO arrays after restoration must not alter the restored world's cell storage.</summary>
        [Test]
        public void RestoredWorldDoesNotShareSnapshotBuffers()
        {
            var snapshot = Fixture().CaptureSnapshot(); var restored = FireWorld.Restore(snapshot); string before = restored.Checksum();
            snapshot.Chunks[0].Cells[0].Heat = 100;
            restored.CaptureSnapshot().Chunks[0].Cells[0].Heat = 200;
            Assert.That(restored.Checksum(), Is.EqualTo(before));
        }

        /// <summary>Exercise real local file creation/replacement and confirm an invalid write preserves the previous checkpoint.</summary>
        [Test]
        public void FileStoreReplacesValidCheckpointsAndPreservesPreviousOnInvalidWrite()
        {
            string directory = Path.Combine(Path.GetTempPath(), "FirelineSnapshot-" + Guid.NewGuid().ToString("N"));
            string path = Path.Combine(directory, "world.fls");
            try
            {
                var store = new FileFireSnapshotStore(path); var world = Fixture();
                store.Write(world.CaptureSnapshot()); world.Step(); store.Write(world.CaptureSnapshot());
                Assert.That(FireWorld.Restore(store.Read()).Checksum(), Is.EqualTo(world.Checksum()));
                byte[] previous = File.ReadAllBytes(path); var invalid = world.CaptureSnapshot(); invalid.Checksum = "bad";
                Assert.Throws<InvalidDataException>(() => store.Write(invalid));
                Assert.That(File.ReadAllBytes(path), Is.EqualTo(previous));
                Assert.That(Directory.GetFiles(directory), Has.Length.EqualTo(1));
                File.WriteAllBytes(path, new byte[200]); Assert.Throws<InvalidDataException>(() => store.Read());
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
                if (Directory.Exists(directory)) Directory.Delete(directory);
            }
        }

        /// <summary>Recompute only the envelope hash so tests reach semantic/parser validation instead of failing at corruption detection.</summary>
        private static byte[] Rehash(byte[] bytes)
        {
            using (var sha = SHA256.Create())
            { byte[] hash = sha.ComputeHash(bytes, 0, bytes.Length - 32); Array.Copy(hash, 0, bytes, bytes.Length - 32, 32); }
            return bytes;
        }
    }
}
