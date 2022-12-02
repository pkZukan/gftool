using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Trinity.Core.Structures.TR
{
    [StructLayout(LayoutKind.Sequential)]
    public struct OneFileHeader
    {
        public const int SIZE = 16;
        public UInt64 magic;
        public Int64 offset;
    }
}
