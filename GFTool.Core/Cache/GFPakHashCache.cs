using Trinity.Core.Math.Hash;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Trinity.Core.Cache
{
    public class GFPakHashCache
    {
        private const string CachePath = "GFPAKHashCache.bin";
        private static Dictionary<ulong, string> Cache = new Dictionary<ulong, string>();

        public static int Count => Cache.Count;

        public static void Clear()
        {
            Cache.Clear();
        }

        public static void Open(string path = CachePath)
        {
            Cache = new Dictionary<ulong, string>();

            if (File.Exists(path))
            {
                using var br = new BinaryReader(File.OpenRead(path));
                var count = br.ReadUInt64();
                for (ulong i = 0; i < count; i++)
                {
                    var hash = br.ReadUInt64();
                    var name = br.ReadString();
                    Cache[hash] = name;
                }
            }
        }

        public static void Save(string path = CachePath)
        {
            // Use Create to truncate (avoids leaving trailing data from older/larger cache files).
            using var bw = new BinaryWriter(File.Create(path));
            bw.Write((UInt64)Cache.Count);
            foreach (KeyValuePair<ulong, string> pair in Cache)
            {
                bw.Write(pair.Key);
                bw.Write(pair.Value);
            }
        }

        public static void AddHashName(UInt64 hash, string name)
        {
            UInt64 hashCheck = GFFNV.Hash(name);
            if (hashCheck == hash)
            {
                Cache[hash] = name;
            }
        }

        public static void AddHashFromList(List<string> list)
        {
            foreach (string item in list)
            {
                if (string.IsNullOrWhiteSpace(item))
                {
                    continue;
                }

                // Hashlist lines are typically: "<hash> <path>", but may contain multiple spaces/tabs.
                var parts = item.Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 2)
                {
                    continue;
                }

                var hashText = parts[0];
                var path = parts[1];
                if (string.IsNullOrWhiteSpace(hashText) || string.IsNullOrWhiteSpace(path))
                {
                    continue;
                }

                // The provided hash is preferred as the source of truth since path normalization and casing can differ.
                // Fall back to hashing the path if parsing fails.
                if (TryParseHash(hashText, out ulong hash))
                {
                    Cache[hash] = path;
                }
                else
                {
                    Cache[GFFNV.Hash(path)] = path;
                }
            }
        }

        private static bool TryParseHash(string text, out ulong hash)
        {
            hash = 0;
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            text = text.Trim();
            if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                text = text.Substring(2);
            }

            return ulong.TryParse(text, System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out hash);
        }

        public static string? GetName(UInt64 hash)
        {
            string? str = null;
            Cache.TryGetValue(hash, out str);
            return str;
        }

    }
}
