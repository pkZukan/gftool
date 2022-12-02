using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Trinity.Core.Structures.BN
{
    [StructLayout(LayoutKind.Sequential)]
    public struct BNHeader
    {
        public UInt64 Magic;
        public UInt32 Version;
        public UInt16 ByteOrder;
        public Byte Alignment;
        public Byte AlignmentSize;
        public UInt32 NamePointer;
        public UInt16 Relocated;
        public UInt16 BlockPointer;
        public UInt32 RelocationTablePointer;
        public UInt32 FileSize;
    }
}
