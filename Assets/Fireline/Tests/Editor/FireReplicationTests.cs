using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Fireline.Shared;
using Fireline.Simulation;
using NUnit.Framework;

namespace Fireline.Tests
{
    public sealed class FireReplicationTests
    {
        /// <summary>Create a tiny seeded authoritative world with mutable wind/fire for protocol tests.</summary>
        private static FireWorld World()
        {
            var world = new FireWorld(415, 4); var id = new ChunkId(1, 0, 0);
            world.AddChunk(id, true); world.Ignite(id, 5); world.Wind.Set(8, 0, 2); return world;
        }

        /// <summary>Round-trip a sparse delta after wind, water and fire changes, retaining exact checkpoint bytes and revision metadata.</summary>
        [Test]
        public void DeltaPreservesExactAuthoritativeState()
        {
            var world = World(); var baseline = world.CaptureSnapshot();
            world.Wind.Set(0, 8, 2); world.ApplyWater(100, new[] { new WaterHit(new ChunkId(1, 0, 0), 5, 1) });
            for (int i = 0; i < 20; i++) world.Step();
            var target = world.CaptureSnapshot(); var patch = FireReplicationCodec.Delta(baseline, target);
            var result = FireReplicationCodec.Apply(baseline, FireReplicationCodec.Unpack(FireReplicationCodec.Pack(patch)));
            Assert.That(FireSnapshotCodec.Encode(result), Is.EqualTo(FireSnapshotCodec.Encode(target)));
            Assert.That(baseline.Tick, Is.Zero, "Applying must not mutate the acknowledged baseline.");
        }

        /// <summary>Skipped, reordered and replayed patches cannot apply to a different acknowledged digest.</summary>
        [Test]
        public void DeltaGapAndReplayLeaveValidStateUntouched()
        {
            var world = World(); var a = world.CaptureSnapshot(); world.Step(); var b = world.CaptureSnapshot();
            world.Step(); var c = world.CaptureSnapshot(); var patch = FireReplicationCodec.Delta(b, c);
            Assert.Throws<InvalidDataException>(() => FireReplicationCodec.Apply(a, patch));
            var result = FireReplicationCodec.Apply(b, patch);
            Assert.Throws<InvalidDataException>(() => FireReplicationCodec.Apply(result, patch));
            Assert.That(a.Checksum, Is.Not.EqualTo(c.Checksum));
        }

        /// <summary>Even an exact base digest cannot override a mismatched per-chunk revision guard or malformed range data.</summary>
        [Test]
        public void DeltaRejectsRevisionCorruptionTruncationAndTrailingBytes()
        {
            var world = World(); var baseline = world.CaptureSnapshot(); world.Step();
            byte[] patch = FireReplicationCodec.Delta(baseline, world.CaptureSnapshot());
            var changed = (byte[])patch.Clone(); changed[36] ^= 1; // First previous-revision byte in format FPD1.
            Assert.Throws<InvalidDataException>(() => FireReplicationCodec.Apply(baseline, changed));
            Assert.Throws<InvalidDataException>(() => FireReplicationCodec.Apply(baseline, patch.Take(patch.Length - 1).ToArray()));
            Assert.Throws<InvalidDataException>(() => FireReplicationCodec.Apply(baseline, patch.Concat(new byte[] { 0 }).ToArray()));
            Assert.Throws<InvalidDataException>(() => FireReplicationCodec.Apply(null, patch));
        }

        /// <summary>Adding chunks forces a fresh baseline rather than pretending the old topology is a compatible patch base.</summary>
        [Test]
        public void TopologyChangeRequiresBaseline()
        {
            var world = World(); var before = world.CaptureSnapshot(); world.AddChunk(new ChunkId(1, 1, 0), false);
            Assert.That(FireReplicationCodec.Delta(before, world.CaptureSnapshot()), Is.Null);
        }

        /// <summary>Reordered and duplicate identical fragments assemble once; conflicting values and invalid bounds are rejected.</summary>
        [Test]
        public void FragmentAssemblyIsBoundedAndOrderIndependent()
        {
            string text = new string('x', 480) + new string('y', 480) + "end";
            var assembly = new FireFrameAssembly(3, text.Length);
            Assert.That(assembly.Add(2, "end"), Is.Null); Assert.That(assembly.Add(0, text.Substring(0, 480)), Is.Null);
            Assert.That(assembly.Add(0, text.Substring(0, 480)), Is.Null);
            Assert.Throws<InvalidDataException>(() => assembly.Add(0, new string('z', 480)));
            Assert.That(assembly.Add(1, text.Substring(480, 480)), Is.EqualTo(text));
            Assert.Throws<InvalidDataException>(() => new FireFrameAssembly(6001, 6001 * 480));
            Assert.Throws<InvalidDataException>(() => new FireFrameAssembly(1, int.MaxValue));
            Assert.Throws<InvalidDataException>(() => new FireFrameAssembly(1, 5).Add(2, "hello"));
        }

        /// <summary>Malformed text and compressed expansion cannot allocate an unbounded checkpoint.</summary>
        [Test]
        public void CompressionRejectsInvalidAndExpandingPayloads()
        {
            Assert.Throws<InvalidDataException>(() => FireReplicationCodec.Unpack("not-base64"));
            using (var stream = new MemoryStream())
            {
                using (var compressor = new DeflateStream(stream, CompressionLevel.Fastest, true))
                { byte[] oversized = new byte[FireSnapshotCodec.MaximumBytes + 1]; compressor.Write(oversized, 0, oversized.Length); }
                Assert.Throws<InvalidDataException>(() => FireReplicationCodec.Unpack(Convert.ToBase64String(stream.ToArray())));
            }
        }

        /// <summary>Parse explicit load/save/proof paths and forbid mixing accelerated M01 execution with live replication state.</summary>
        [Test]
        public void LiveCheckpointOptionsAreExplicitAndConflictWithAcceleratedProof()
        {
            var options = LaunchOptions.Parse(new[] { "--fireline-world-load", "old.fls", "--fireline-world-save", "new.fls", "--fireline-replication-proof", "proof.json" });
            Assert.That(options.WorldLoadPath, Is.EqualTo("old.fls")); Assert.That(options.WorldSavePath, Is.EqualTo("new.fls"));
            Assert.Throws<ArgumentException>(() => LaunchOptions.Parse(new[] { "--fireline-world-load", "" }));
            Assert.Throws<ArgumentException>(() => LaunchOptions.Parse(new[] { "--fireline-sim-proof", "old.json", "--fireline-replication-proof", "new.json" }));
        }
    }
}
