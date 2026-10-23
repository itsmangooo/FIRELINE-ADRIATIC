using System;
using System.IO;
using Fireline.Simulation;

namespace Fireline.Server
{
    /// <summary>Single-writer local checkpoint store; no database, event replay or automatic server-startup wiring is implied.</summary>
    public sealed class FileFireSnapshotStore : IFireSnapshotStore
    {
        private readonly string path;

        /// <summary>Use the operator-supplied file path, resolving it once; no save is read or written by construction.</summary>
        public FileFireSnapshotStore(string path) { this.path = Path.GetFullPath(path); }

        /// <summary>
        /// Validate/encode first, flush a unique temporary file in the same directory, then move/replace the checkpoint.
        /// Preserve the previous file if validation fails and remove this invocation's temporary file on normal failure.
        /// Single-writer and local-filesystem semantics are required; power-loss directory durability is not qualified.
        /// </summary>
        public void Write(FireSnapshot snapshot)
        {
            byte[] bytes = FireSnapshotCodec.Encode(snapshot);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                { stream.Write(bytes, 0, bytes.Length); stream.Flush(true); }
                if (File.Exists(path)) File.Replace(temporary, path, null);
                else File.Move(temporary, path);
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }

        /// <summary>
        /// Read at most the format's byte budget, reject file-size races/truncation, and return validated detached data.
        /// Missing/corrupt files throw; there is no silent reset to fresh vegetation or a default world.
        /// </summary>
        public FireSnapshot Read()
        {
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                if (stream.Length < FireSnapshotCodec.MinimumBytes || stream.Length > FireSnapshotCodec.MaximumBytes) throw new InvalidDataException("Invalid checkpoint file size.");
                byte[] bytes = new byte[(int)stream.Length];
                int offset = 0;
                while (offset < bytes.Length)
                {
                    int count = stream.Read(bytes, offset, bytes.Length - offset);
                    if (count == 0) throw new InvalidDataException("Checkpoint file truncated while reading.");
                    offset += count;
                }
                if (stream.ReadByte() != -1) throw new InvalidDataException("Checkpoint file grew while reading.");
                return FireSnapshotCodec.Decode(bytes);
            }
        }
    }
}
