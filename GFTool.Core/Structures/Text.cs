using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace GFTool.Core.Structures
{
    [StructLayout(LayoutKind.Sequential)]
    public struct TextHeader
    {
        public UInt16 textSect;
        public UInt16 textCount;
        public UInt32 totalLength;
        public UInt32 initKey;
        public Int32 sectData;

    }
}
