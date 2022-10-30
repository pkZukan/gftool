using GFTool.Structures;
using GFTool.Utils;

namespace GFTool.Serializers
{
    static class AHTBSerializer
    {
        public static Dictionary<UInt64, string> Deserialize(BinaryReader br)
        {
            Dictionary<UInt64, string> keyValuePairs = new Dictionary<ulong, string>();
            var header = br.ReadBytes(AHTBHeader.SIZE).ToStruct<AHTBHeader>();

            for (int i = 0; i < header.Count; i++)
            {
                var hash = br.ReadUInt64();
                var length = br.ReadUInt16();
                var name = br.ReadString();
                keyValuePairs.Add(hash, name);
            }

            return keyValuePairs;
        }

        public static void Serialize(BinaryWriter bw, Dictionary<UInt64, string> keyValuePairs)
        {
            AHTBHeader header = new AHTBHeader()
            {
                magic = AHTBHeader.MAGIC,
                Count = (uint)keyValuePairs.Count()
            };

            bw.Write(header.ToBytes());

            foreach (KeyValuePair<ulong, string> pair in keyValuePairs)
            {
                AHTBEntry entry = new AHTBEntry()
                {
                    Hash = pair.Key,
                    NameLength = (ushort)pair.Value.Length,
                    Name = pair.Value,
                };
                bw.Write(entry.ToBytes());
            }
        }
    }
}
