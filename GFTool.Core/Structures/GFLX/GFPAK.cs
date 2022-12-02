using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Trinity.Core.Structures.GFLX
{
    public enum GFPakCompressionType : UInt16
    {
        NONE = 0,
        ZLIB,
        LZ4,
        OODLE
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct GFPakHeader
    {
        public const UInt64 MAGIC = 0x4B434150_584C4647; //GFLXPACK
        public const int SIZE = 0x18;
        public UInt64 magic;
        public UInt32 Version;
        public UInt32 Relocated;
        public UInt32 FileNumber;
        public UInt32 FolderNumber;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct GFPakFolderHeader
    {
        public const int SIZE = 0x10;
        public UInt64 Hash;
        public UInt32 ContentNumber;
        public UInt32 Reserved;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct GFPakFolderIndex
    {
        public const int SIZE = 0x10;
        public UInt64 Hash;
        public UInt32 Index;
        public UInt32 Reserved;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct GFPakFileHeader
    {
        public const int SIZE = 0x18;
        public UInt16 Level;
        public GFPakCompressionType CompressionType;
        public UInt32 BufferSize;
        public UInt32 FileSize;
        public UInt32 Reserved;
        public UInt64 FilePointer;
    }
}
