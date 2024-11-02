using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GFTool.Core.Utils
{
    public static class BinaryReaderHelper
    {
        public static void Peak(this BinaryReader br, Action func) 
        {
            var currInd = br.BaseStream.Position;
            func();
            br.BaseStream.Position = currInd;
        }

        public static string ReadPascalString(this BinaryReader br)
        {
            var len = br.ReadUInt16();
            return Encoding.UTF8.GetString(br.ReadBytes(len));
        }

        public static string ReadFixedString(this BinaryReader br, int size)
        {
            return Encoding.UTF8.GetString(br.ReadBytes(size)).Replace("\0", string.Empty);
        }
    }
}
