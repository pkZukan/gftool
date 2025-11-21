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

        public static void Open(string path = CachePath)
        {
            Cache = new Dictionary<ulong, string>();

            if (File.Exists(path)) 
            {
                BinaryReader br = new BinaryReader(File.OpenRead(path));
                var count = br.ReadUInt64();
                for (ulong i = 0; i < count; i++)
                {
                    var hash = br.ReadUInt64();
                    var name = br.ReadString();
                    Cache.TryAdd(hash, name);
                }
            }
        }

        public static void Save(string path = CachePath)
        {
            BinaryWriter bw = new BinaryWriter(File.OpenWrite(path));
            bw.Write((UInt64)Cache.Count);
            foreach(KeyValuePair<ulong, string> pair in Cache)
            {
                bw.Write(pair.Key);
                bw.Write(pair.Value);
            }
            bw.Close();
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
                var keypair = item.TrimEnd().Split(' ');
                if (keypair.Length == 2)
                {
                    Cache.TryAdd(GFFNV.Hash(keypair[1]), keypair[1]);
                }
            }
        }

        public static string? GetName(UInt64 hash)
        {
            string? str = null;
            Cache.TryGetValue(hash, out str);
            return str;
        }

    }
}
