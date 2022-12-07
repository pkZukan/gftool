using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
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
    }
}
