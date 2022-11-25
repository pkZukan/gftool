using FlatSharp.Attributes;

namespace GFTool.Core.Structures.GFLX
{
    [FlatBufferTable]
    public class TRPAK
    {
        [FlatBufferItem(0)]
        public uint? Unk0 { get; set; }

        [FlatBufferItem(1)]
        public byte CompressionType { get; set; }

        [FlatBufferItem(2)]
        public byte Unk1 { get; set; }

        [FlatBufferItem(3)]
        public byte UncompressedSize { get; set; }

        [FlatBufferItem(4)]
        public byte[] Data { get; set; } = Array.Empty<byte>();
    }
}
