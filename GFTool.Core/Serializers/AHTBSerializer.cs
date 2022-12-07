using GFTool.Core.Structures;
using GFTool.Core.Utils;
using Trinity.Core.Structures;
using Trinity.Core.Utils;

namespace Trinity.Core.Serializers
{
    public static class AHTBSerializer
    {
        private static ushort decryptU16(ushort val, ref ushort key)
        {
            val = (ushort)(val ^ key);
            key = (ushort)(((key << 3) | (key >> 13)) & 0xffff);
            return val;
        }

        private static ushort decryptVar(ushort val, ref ushort key) 
        {
            //TODO
            return 0;
        }

        public static Dictionary<UInt64, string> Deserialize(string file)
        {
            var tbl = new BinaryReader(File.OpenRead(file));
            var dat = new BinaryReader(File.OpenRead(file.Replace("tbl", "dat")));
            int i;

            Dictionary<UInt64, string> keyValuePairs = new Dictionary<ulong, string>();
            var header = tbl.ReadBytes(AHTBHeader.SIZE).ToStruct<AHTBHeader>();

            for (i = 0; i < header.Count; i++)
            {
                var hash = tbl.ReadUInt64();
                var length = tbl.ReadUInt16();
                var name = new string(tbl.ReadChars(length));
                keyValuePairs.Add(hash, name);
            }

            List<string> datStrings = new List<string>();
            var datHeader = dat.ReadBytes(0x10).ToStruct<TextHeader>();
            dat.BaseStream.Position = datHeader.sectData;
            var sectLen = dat.ReadUInt32();
            ushort key = 0x7C89;
            dat.ReadUInt32();
            for (i = 0; i < datHeader.textCount; i++) {
                var off = dat.ReadUInt32();
                var len = dat.ReadInt32();
                dat.Peak(() => {
                    dat.BaseStream.Position = off;
                    var enc = dat.ReadChars(len);
                    string str = "";
                    for (int j = 0; j < len / 2; j++) {
                        var val = decryptU16(dat.ReadUInt16(), ref key);
                        if (val == 0) break;
                        else if (val == '\n') str += "\\n";
                        else if (val == 0x10)
                            str += (char)decryptVar(val, ref key);
                        else {
                            // Check special characters...
                            if (val == 0xE07F)
                                str += (char)0x202F; // nbsp
                            else if (val == 0xE08D)
                                str += (char)0x2026; // …
                            else if (val == 0xE08E)
                                str += (char)0x2642; // ♂
                            else if (val == 0xE08F)
                                str += (char)0x2640; // ♀
                            else str += (char)val;
                        }
                    }
                    datStrings.Add(str);
                    key += 0x2983;
                });
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
