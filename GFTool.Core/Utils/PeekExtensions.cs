using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GFTool.Core.Utils
{
    static class PeekExtensions
    {
        public static UInt64 PeekUInt64(this BinaryReader br)
        {
            long location = br.BaseStream.Position;
            ulong value = br.ReadUInt64();
            br.BaseStream.Position = location;
            return value;
        }
        public static UInt32 PeekUInt32(this BinaryReader br)
        {
            long location = br.BaseStream.Position;
            uint value = br.ReadUInt32();
            br.BaseStream.Position = location;
            return value;
        }
    }
}
