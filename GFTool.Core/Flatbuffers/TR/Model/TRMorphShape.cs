using FlatSharp.Attributes;

namespace Trinity.Core.Flatbuffers.TR.Model
{
    [FlatBufferEnum(typeof(uint))]
    public enum TRMorphUsage : uint
    {
        NONE = 0,
        POSITION = 1,
        MIN_LIMIT = 2,
        MAX_LIMIT = 3,
        USER_ID = 10
    }

    [FlatBufferTable]
    public class TRMorphElement
    {
        [FlatBufferItem(0)] public uint Field_00 { get; set; }
        [FlatBufferItem(1)] public TRMorphUsage Usage { get; set; }
        [FlatBufferItem(2)] public uint Field_02 { get; set; }
        [FlatBufferItem(3)] public TRVertexFormat Format { get; set; }
        [FlatBufferItem(4)] public uint Offset { get; set; }
    }

    [FlatBufferTable]
    public class TRMorphData
    {
        [FlatBufferItem(0)] public TRMorphElement[] Elements { get; set; }
        [FlatBufferItem(1)] public TRVertexElementSize[] Sizes { get; set; }
    }

    [FlatBufferTable]
    public class TRMorphMetadata
    {
        [FlatBufferItem(0)] public uint Index { get; set; }
        [FlatBufferItem(1)] public string Name { get; set; }
        [FlatBufferItem(2)] public byte Type { get; set; }
    }

    [FlatBufferTable]
    public class TRMorphShape
    {
        [FlatBufferItem(0)] public TRMorphData[] Data { get; set; }
        [FlatBufferItem(1)] public TRMorphMetadata[] Metadata { get; set; }
        [FlatBufferItem(2)] public string Name { get; set; }
    }

    [FlatBufferTable]
    public class TRVisibilityShape
    {
        [FlatBufferItem(0)] public uint Index { get; set; }
        [FlatBufferItem(1)] public string Name { get; set; }
    }
}
