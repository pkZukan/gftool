using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GFTool.Math.Hash
{
    public static class GFFNV
    {
        public const UInt64 FNV_PRIME = 0x00000000100000001B3;
        public const UInt64 FNV_BASIS = 0xCBF29CE484222645;

        public static UInt64 Hash(string str, UInt64 basis = FNV_BASIS)
        {
            byte[] buf = Encoding.UTF8.GetBytes(str);
            UInt64 result = basis;
            foreach (byte b in buf)
            {
                result *= FNV_PRIME;
                result ^= b;
                result &= 0xFFFFFFFFFFFFFFFF;
            }
            return result;
        }

        public static UInt64 HashBackwards(string str)
        {
            return 0;
        }
    }
}
