using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace GFTool.Core.Structures.TR
{
    [StructLayout(LayoutKind.Sequential)]
    public struct OneFileHeader
    {
        public UInt64 magic;
        public UInt64 offset;
    }
}
