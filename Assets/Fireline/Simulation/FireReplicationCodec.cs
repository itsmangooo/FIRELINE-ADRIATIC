using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace Fireline.Simulation
{
    /// <summary>Bounded prototype wire helpers: compressed baselines and revision-guarded byte-range deltas of canonical checkpoints.</summary>
    public static class FireReplicationCodec
    {
        public const int PartCharacters = 480, MaximumParts = 6000;
        private const uint DeltaMagic = 0x31445046;

        /// <summary>Build a sparse canonical-byte patch, including exact base digest and each chunk's previous/next revision.</summary>
        public static byte[] Delta(FireSnapshot before, FireSnapshot after)
        {
            byte[] old = FireSnapshotCodec.Encode(before), next = FireSnapshotCodec.Encode(after);
            if (old.Length != next.Length || before.Chunks.Length != after.Chunks.Length) return null;
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(DeltaMagic); writer.Write(Convert.ToUInt64(before.Checksum, 16)); writer.Write(after.Tick);
                writer.Write(after.Chunks.Length);
                for (int i = 0; i < after.Chunks.Length; i++)
                {
                    var a = before.Chunks[i]; var b = after.Chunks[i];
                    if (!a.Id.Equals(b.Id) || b.Revision < a.Revision) return null;
                    writer.Write(b.Id.Sector); writer.Write(b.Id.X); writer.Write(b.Id.Z);
                    writer.Write(a.Revision); writer.Write(b.Revision);
                }
                writer.Write(next.Length);
                var runs = new List<Tuple<int, int>>();
                for (int i = 0; i < next.Length;)
                {
                    if (old[i] == next[i]) { i++; continue; }
                    int start = i++;
                    while (i < next.Length && (old[i] != next[i] || (i + 4 < next.Length && old[i + 4] != next[i + 4]))) i++;
                    runs.Add(Tuple.Create(start, i - start));
                }
                writer.Write(runs.Count);
                foreach (var run in runs) { writer.Write(run.Item1); writer.Write(run.Item2); writer.Write(next, run.Item1, run.Item2); }
                return stream.Length <= FireSnapshotCodec.MaximumBytes ? stream.ToArray() : null;
            }
        }

        /// <summary>Apply a delta only to its exact acknowledged baseline; validate chunk revisions, bounds, target tick and full resulting state.</summary>
        public static FireSnapshot Apply(FireSnapshot baseline, byte[] delta)
        {
            if (baseline == null || delta == null || delta.Length > FireSnapshotCodec.MaximumBytes) throw new InvalidDataException("Missing/oversized delta.");
            try
            {
                using (var stream = new MemoryStream(delta, false))
                using (var reader = new BinaryReader(stream))
                {
                    if (reader.ReadUInt32() != DeltaMagic || reader.ReadUInt64() != Convert.ToUInt64(baseline.Checksum, 16))
                        throw new InvalidDataException("Delta base digest gap.");
                    ulong tick = reader.ReadUInt64(); int count = reader.ReadInt32();
                    if (tick < baseline.Tick || count != baseline.Chunks.Length) throw new InvalidDataException("Delta topology/tick gap.");
                    var revisions = new ulong[count];
                    for (int i = 0; i < count; i++)
                    {
                        var id = new ChunkId(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32());
                        ulong previous = reader.ReadUInt64(); revisions[i] = reader.ReadUInt64();
                        if (!id.Equals(baseline.Chunks[i].Id) || previous != baseline.Chunks[i].Revision || revisions[i] < previous)
                            throw new InvalidDataException("Chunk revision gap.");
                    }
                    byte[] bytes = FireSnapshotCodec.Encode(baseline); int length = reader.ReadInt32(); int runs = reader.ReadInt32();
                    if (length != bytes.Length || runs < 0 || runs > bytes.Length) throw new InvalidDataException("Invalid delta size.");
                    int end = 0;
                    for (int i = 0; i < runs; i++)
                    {
                        int offset = reader.ReadInt32(), size = reader.ReadInt32();
                        if (offset < end || size <= 0 || offset > bytes.Length - size) throw new InvalidDataException("Invalid delta range.");
                        byte[] part = reader.ReadBytes(size); if (part.Length != size) throw new EndOfStreamException();
                        Array.Copy(part, 0, bytes, offset, size); end = offset + size;
                    }
                    if (stream.Position != stream.Length) throw new InvalidDataException("Trailing delta bytes.");
                    var result = FireSnapshotCodec.Decode(bytes);
                    if (result.Tick != tick || !result.Chunks.Select(c => c.Revision).SequenceEqual(revisions))
                        throw new InvalidDataException("Delta target metadata mismatch.");
                    return result;
                }
            }
            catch (EndOfStreamException error) { throw new InvalidDataException("Truncated delta.", error); }
        }

        /// <summary>Compress validated protocol bytes before splitting RPC-sized base64 fragments; no asset or executable code is carried.</summary>
        public static string Pack(byte[] bytes)
        {
            if (bytes == null || bytes.Length > FireSnapshotCodec.MaximumBytes) throw new InvalidDataException("Wire byte budget.");
            using (var output = new MemoryStream())
            {
                using (var compressor = new DeflateStream(output, CompressionLevel.Fastest, true)) compressor.Write(bytes, 0, bytes.Length);
                string text = Convert.ToBase64String(output.ToArray());
                if (text.Length > PartCharacters * MaximumParts) throw new InvalidDataException("Wire fragment budget.");
                return text;
            }
        }

        /// <summary>Reject malformed base64 and bound decompression output, including deliberately compressed expansion bombs.</summary>
        public static byte[] Unpack(string text)
        {
            if (text == null || text.Length > PartCharacters * MaximumParts) throw new InvalidDataException("Wire text budget.");
            try
            {
                using (var input = new MemoryStream(Convert.FromBase64String(text)))
                using (var decompressor = new DeflateStream(input, CompressionMode.Decompress))
                using (var output = new MemoryStream())
                {
                    byte[] buffer = new byte[8192]; int count;
                    while ((count = decompressor.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        if (output.Length + count > FireSnapshotCodec.MaximumBytes) throw new InvalidDataException("Decompression budget.");
                        output.Write(buffer, 0, count);
                    }
                    return output.ToArray();
                }
            }
            catch (FormatException error) { throw new InvalidDataException("Invalid wire base64.", error); }
        }
    }

    /// <summary>One bounded in-progress transfer; accepts reordered identical fragments and rejects conflicting metadata/duplicates.</summary>
    public sealed class FireFrameAssembly
    {
        private readonly string[] parts;
        private readonly int length;
        private int received;

        /// <summary>Validate declared text/count before allocating fragment slots.</summary>
        public FireFrameAssembly(int count, int characters)
        {
            if (characters <= 0 || characters > FireReplicationCodec.PartCharacters * FireReplicationCodec.MaximumParts
                || count <= 0 || count > FireReplicationCodec.MaximumParts
                || count != (characters + FireReplicationCodec.PartCharacters - 1) / FireReplicationCodec.PartCharacters)
                throw new InvalidDataException("Invalid fragment counts.");
            parts = new string[count]; length = characters;
        }

        /// <summary>Store one exact-size fragment; return the complete joined text once every part has arrived.</summary>
        public string Add(int index, string value)
        {
            if (index < 0 || index >= parts.Length || value == null
                || value.Length != Math.Min(FireReplicationCodec.PartCharacters, length - index * FireReplicationCodec.PartCharacters))
                throw new InvalidDataException("Invalid fragment range.");
            if (parts[index] != null && parts[index] != value) throw new InvalidDataException("Conflicting fragment.");
            if (parts[index] == null) { parts[index] = value; received++; }
            return received == parts.Length ? string.Concat(parts) : null;
        }
    }
}
