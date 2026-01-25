using GFTool.Core.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Trinity.Core.Cache;
using Trinity.Core.Compression;
using Trinity.Core.Math.Hash;
using Trinity.Core.Structures.GFLX;
using Trinity.Core.Utils;

namespace Trinity.Core.Assets
{
    public sealed class GfpakAssetProvider : IAssetProvider
    {
        private readonly string containerPath;
        private readonly FileStream stream;
        private readonly BinaryReader reader;
        private readonly Dictionary<ulong, int> absoluteHashToIndex;
        private readonly GFPakFileHeader[] fileHeaders;
        private readonly ulong[] absolutePathHashes;
        private readonly Dictionary<int, byte[]> decompressedCache = new Dictionary<int, byte[]>();
        private readonly object cacheLock = new object();

        public string DisplayName => Path.GetFileName(containerPath);

        public GfpakAssetProvider(string gfpakPath)
        {
            containerPath = gfpakPath;
            stream = new FileStream(gfpakPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            reader = new BinaryReader(stream);

            // Header (fixed size struct), then 3 offsets:
            // - embeddedFileOff
            // - embeddedFileHashOff (absolute path hashes)
            // - folder array offsets table
            var header = reader.ReadBytes(GFPakHeader.SIZE).ToStruct<GFPakHeader>();
            if (header.magic != GFPakHeader.MAGIC)
            {
                throw new InvalidDataException($"Invalid GFPAK magic 0x{header.magic:X16} (expected 0x{GFPakHeader.MAGIC:X16}).");
            }

            ulong embeddedFileOff = reader.ReadUInt64();
            ulong embeddedFileHashOff = reader.ReadUInt64();

            // Folder offsets (unused for serving by absolute hash, but parsed for entry enumeration)
            var folderOffsets = new long[header.FolderNumber];
            for (int i = 0; i < folderOffsets.Length; i++)
            {
                folderOffsets[i] = reader.ReadInt64();
            }

            // Absolute path hashes
            stream.Position = (long)embeddedFileHashOff;
            absolutePathHashes = new ulong[header.FileNumber];
            for (int i = 0; i < absolutePathHashes.Length; i++)
            {
                absolutePathHashes[i] = reader.ReadUInt64();
            }

            // File headers
            stream.Position = (long)embeddedFileOff;
            fileHeaders = new GFPakFileHeader[header.FileNumber];
            for (int i = 0; i < fileHeaders.Length; i++)
            {
                fileHeaders[i] = reader.ReadBytes(GFPakFileHeader.SIZE).ToStruct<GFPakFileHeader>();
            }

            absoluteHashToIndex = new Dictionary<ulong, int>(absolutePathHashes.Length);
            for (int i = 0; i < absolutePathHashes.Length; i++)
            {
                absoluteHashToIndex[absolutePathHashes[i]] = i;
            }

            // Optional: parse folders to warm cache relationships for UI enumeration, but not required for lookup.
            // Ensure the cache file is loaded if present.
            try
            {
                GFPakHashCache.Open();
            }
            catch
            {
                // Ignore cache load issues; browsing will fall back to hashes.
            }
        }

        public bool Exists(string path)
        {
            var hash = HashPath(path);
            return absoluteHashToIndex.ContainsKey(hash);
        }

        public Stream OpenRead(string path)
        {
            var bytes = ReadAllBytes(path);
            return new MemoryStream(bytes, writable: false);
        }

        public byte[] ReadAllBytes(string path)
        {
            var hash = HashPath(path);
            if (!absoluteHashToIndex.TryGetValue(hash, out int index))
            {
                throw new FileNotFoundException($"GFPAK entry not found: '{path}' (hash=0x{hash:X16})", path);
            }

            lock (cacheLock)
            {
                if (decompressedCache.TryGetValue(index, out var cached))
                {
                    return cached;
                }
            }

            var header = fileHeaders[index];
            byte[] compressed = ReadAt(header.FilePointer, checked((int)header.FileSize));
            byte[] data = DecompressIfNeeded(header, compressed);

            lock (cacheLock)
            {
                decompressedCache[index] = data;
            }

            return data;
        }

        public IEnumerable<AssetEntry> EnumerateEntries()
        {
            for (int i = 0; i < absolutePathHashes.Length; i++)
            {
                ulong hash = absolutePathHashes[i];
                string? path = null;
                try
                {
                    path = GFPakHashCache.GetName(hash);
                }
                catch
                {
                    path = null;
                }
                yield return new AssetEntry(hash, path);
            }
        }

        private static ulong HashPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return 0;
            }

            // GFPAK paths are hashed with forward slashes.
            string normalized = path.Trim().Replace('\\', '/');
            // Normalize accidental "./"
            if (normalized.StartsWith("./", StringComparison.Ordinal))
            {
                normalized = normalized.Substring(2);
            }
            return GFFNV.Hash(normalized);
        }

        private byte[] ReadAt(ulong offset, int length)
        {
            lock (stream)
            {
                stream.Position = (long)offset;
                return reader.ReadBytes(length);
            }
        }

        private static bool HasOodleDll()
        {
            // Oodle.cs DllImport uses "oo2core_8_win64" (no extension).
            return File.Exists("oo2core_8_win64.dll");
        }

        private static byte[] DecompressIfNeeded(GFPakFileHeader header, byte[] input)
        {
            switch (header.CompressionType)
            {
                case GFPakCompressionType.NONE:
                    return input;
                case GFPakCompressionType.LZ4:
                    return LZ4.Decompress(input, (int)header.BufferSize);
                case GFPakCompressionType.ZLIB:
                {
                    using var ms = new MemoryStream(input, writable: false);
                    using var zs = new ZLibStream(ms, CompressionMode.Decompress);
                    var outBytes = new byte[header.BufferSize];
                    int readTotal = 0;
                    while (readTotal < outBytes.Length)
                    {
                        int read = zs.Read(outBytes, readTotal, outBytes.Length - readTotal);
                        if (read == 0)
                        {
                            break;
                        }
                        readTotal += read;
                    }
                    return outBytes;
                }
                case GFPakCompressionType.OODLE:
                {
                    if (!HasOodleDll())
                    {
                        throw new DllNotFoundException("Missing Oodle library: oo2core_8_win64.dll (place it next to the executable).");
                    }

                    var decoded = Oodle.Decompress(input, (long)header.BufferSize);
                    if (decoded == null)
                    {
                        throw new InvalidDataException("Oodle decompression failed.");
                    }
                    return decoded;
                }
                default:
                    return input;
            }
        }

        public void Dispose()
        {
            reader.Dispose();
            stream.Dispose();
        }
    }
}
