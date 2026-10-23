using System;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Fireline.Simulation
{
    /// <summary>Bounded little-endian schema-1 checkpoint format; data only, with no Unity or executable object deserialization.</summary>
    public static class FireSnapshotCodec
    {
        public const int MinimumBytes = 144, MaximumBytes = 2 * 1024 * 1024;
        private const uint Magic = 0x31534c46; // ASCII FLS1 in little-endian order.
        private const int DigestBytes = 32;

        /// <summary>
        /// Validate a detached snapshot, encode explicit fields, and append SHA-256 to detect accidental byte corruption.
        /// The hash is not authentication: an untrusted sender can replace both payload and hash. Network admission is separate.
        /// </summary>
        public static byte[] Encode(FireSnapshot snapshot)
        {
            var state = FireWorld.Restore(snapshot).CaptureSnapshot();
            using (var stream = new MemoryStream())
            {
                using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
                {
                    writer.Write(Magic); writer.Write(state.Schema); writer.Write(state.Seed); writer.Write(state.Size);
                    writer.Write(state.CellSizeM); writer.Write(state.StepSeconds); writer.Write(state.Tick);
                    writer.Write(state.InactiveCursor); writer.Write(state.AppliedWaterKg); writer.Write(state.EvaporatedWaterKg);
                    writer.Write(state.ConsumedFuelKg); writer.Write(state.Wind.EastMps); writer.Write(state.Wind.NorthMps);
                    writer.Write(state.Wind.GustMps); writer.Write(state.Wind.Revision);
                    writer.Write(ulong.Parse(state.Checksum, NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                    writer.Write(state.Chunks.Length);
                    foreach (var chunk in state.Chunks)
                    {
                        writer.Write(chunk.Id.Sector); writer.Write(chunk.Id.X); writer.Write(chunk.Id.Z);
                        writer.Write(chunk.Revision); writer.Write(chunk.LastSimulatedTick); writer.Write(chunk.Active ? (byte)1 : (byte)0);
                        writer.Write(chunk.Cells.Length);
                        foreach (var cell in chunk.Cells) WriteCell(writer, cell);
                    }
                }
                byte[] payload = stream.ToArray();
                if (payload.Length + DigestBytes > MaximumBytes) throw new InvalidDataException("Checkpoint exceeds byte budget.");
                using (var sha = SHA256.Create())
                {
                    byte[] hash = sha.ComputeHash(payload);
                    stream.Write(hash, 0, hash.Length);
                }
                return stream.ToArray();
            }
        }

        /// <summary>
        /// Reject oversized, corrupt, truncated, unknown-schema or trailing data before returning a validated detached snapshot.
        /// Check chunk/cell counts before allocation; use fixed fields rather than type names or arbitrary serialized objects.
        /// Decoding never replaces the running authority; its owner must explicitly install a restored candidate.
        /// </summary>
        public static FireSnapshot Decode(byte[] bytes)
        {
            if (bytes == null || bytes.Length < MinimumBytes || bytes.Length > MaximumBytes) throw new InvalidDataException("Invalid checkpoint length.");
            int payloadLength = bytes.Length - DigestBytes;
            using (var sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(bytes, 0, payloadLength);
                for (int i = 0; i < DigestBytes; i++)
                    if (hash[i] != bytes[payloadLength + i]) throw new InvalidDataException("Checkpoint byte hash mismatch.");
            }
            try
            {
                using (var stream = new MemoryStream(bytes, 0, payloadLength, false))
                using (var reader = new BinaryReader(stream, Encoding.UTF8))
                {
                    if (reader.ReadUInt32() != Magic || reader.ReadInt32() != 1) throw new InvalidDataException("Unsupported checkpoint format.");
                    var state = new FireSnapshot { Seed = reader.ReadInt32(), Size = reader.ReadInt32(), CellSizeM = reader.ReadDouble(),
                        StepSeconds = reader.ReadDouble(), Tick = reader.ReadUInt64(), InactiveCursor = reader.ReadInt32(),
                        AppliedWaterKg = reader.ReadDouble(), EvaporatedWaterKg = reader.ReadDouble(), ConsumedFuelKg = reader.ReadDouble(),
                        Wind = new WindState { EastMps = reader.ReadDouble(), NorthMps = reader.ReadDouble(), GustMps = reader.ReadDouble(), Revision = reader.ReadUInt64() },
                        Checksum = reader.ReadUInt64().ToString("x16") };
                    int count = reader.ReadInt32();
                    if (count < 0 || count > FireWorld.MaximumChunks || state.Size < 4 || state.Size > 32)
                        throw new InvalidDataException("Invalid checkpoint allocation counts.");
                    state.Chunks = new ChunkSnapshot[count];
                    for (int n = 0; n < count; n++)
                    {
                        var chunk = new ChunkSnapshot { Id = new ChunkId(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32()),
                            Revision = reader.ReadUInt64(), LastSimulatedTick = reader.ReadUInt64() };
                        byte active = reader.ReadByte();
                        if (active > 1) throw new InvalidDataException("Invalid active flag.");
                        chunk.Active = active == 1;
                        int cells = reader.ReadInt32();
                        if (cells != state.Size * state.Size) throw new InvalidDataException("Invalid cell allocation count.");
                        chunk.Cells = new FireCell[cells];
                        for (int i = 0; i < cells; i++) chunk.Cells[i] = ReadCell(reader);
                        state.Chunks[n] = chunk;
                    }
                    if (stream.Position != payloadLength) throw new InvalidDataException("Trailing checkpoint data.");
                    return FireWorld.Restore(state).CaptureSnapshot();
                }
            }
            catch (EndOfStreamException error) { throw new InvalidDataException("Truncated checkpoint.", error); }
        }

        /// <summary>Write one cell in fixed field order; kg/m2, meters and dimensionless indices retain exact double bits.</summary>
        private static void WriteCell(BinaryWriter writer, FireCell cell)
        {
            writer.Write((int)cell.Fuel); writer.Write(cell.InitialFuelKgPerM2); writer.Write(cell.RemainingFuelKgPerM2);
            writer.Write(cell.Moisture); writer.Write(cell.WaterKgPerM2); writer.Write(cell.Heat); writer.Write(cell.Intensity);
            writer.Write(cell.Smoke); writer.Write(cell.ElevationM); writer.Write((int)cell.Phase); writer.Write((int)cell.Vegetation);
            writer.Write(cell.VegetationId); writer.Write(cell.LastChangedTick);
        }

        /// <summary>Read exactly one fixed-size cell; semantic bounds and stable identities are checked by Restore afterward.</summary>
        private static FireCell ReadCell(BinaryReader reader) => new FireCell { Fuel = (FuelKind)reader.ReadInt32(),
            InitialFuelKgPerM2 = reader.ReadDouble(), RemainingFuelKgPerM2 = reader.ReadDouble(), Moisture = reader.ReadDouble(),
            WaterKgPerM2 = reader.ReadDouble(), Heat = reader.ReadDouble(), Intensity = reader.ReadDouble(), Smoke = reader.ReadDouble(),
            ElevationM = reader.ReadDouble(), Phase = (BurnPhase)reader.ReadInt32(), Vegetation = (VegetationPhase)reader.ReadInt32(),
            VegetationId = reader.ReadUInt64(), LastChangedTick = reader.ReadUInt64() };
    }
}
