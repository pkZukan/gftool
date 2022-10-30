using System.Runtime.InteropServices;


namespace GFTool.Structures.GFLX
{
    [StructLayout(LayoutKind.Sequential)]
    public struct BSEQHeader
    {
        public const UInt64 MAGIC = 0x44534553; //SESD
        public const int SIZE = 0x18;
        public UInt32 magic;
        public UInt32 MajorVersion;
        public UInt32 MinorVersion;
        public UInt32 FrameCount;
        public UInt32 GroupOptionCount;
        public UInt32 HashSizeCount;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct BSEQHashSizeEntry
    {
        public const int SIZE = 0xC;
        public UInt64 Hash;
        public UInt32 Size;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct BSEQGroupOption
    {
        public const int SIZE = 0xC;
        public UInt64 Hash;
        public UInt32 Value;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct BSEQCommandEntry
    {
        public UInt32 StartFrame;
        public UInt32 EndFrame;
        public UInt32 GroupNo;
        public BSEQGroupOption[] GroupOptions;
        public UInt64 Hash;
        public byte[] Buffer;
    }
}
