using System.Runtime.InteropServices;


namespace Trinity.Core.Structures
{
    [StructLayout(LayoutKind.Sequential)]
    public struct AHTBHeader
    {
        public const UInt32 MAGIC = 0x42544841; //AHTB
        public const int SIZE = 0x8;
        public UInt32 magic;
        public UInt32 Count;
    }

    [StructLayout(LayoutKind.Sequential)]

    public struct AHTBEntry
    {
        public UInt64 Hash;
        public UInt16 NameLength;
        public string Name;
    }
}
