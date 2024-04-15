using FlatSharp.Attributes;

namespace Trinity.Core.Flatbuffers.TR.Model
{
    [FlatBufferTable]
    public class TRBuffer
    {
        [FlatBufferItem(0)] public byte[] Bytes { get; set; }
    }

    [FlatBufferTable]
    public class TRMorphTarget
    {
        [FlatBufferItem(0)] public TRBuffer[] morphBuffers { get; set; }
    }

    [FlatBufferTable]
    public class TRModelBuffer
    {
        [FlatBufferItem(0)] public TRBuffer[] IndexBuffer { get; set; }
        [FlatBufferItem(1)] public TRBuffer[] VertexBuffer { get; set; }
        [FlatBufferItem(2)] public TRMorphTarget[] MorphTargets { get; set; }

    }

    [FlatBufferTable]
    public class TRMBF
    {
        [FlatBufferItem(0)] public int Field_00 { get; set; }
        [FlatBufferItem(1)] public TRModelBuffer[] TRMeshBuffers { get; set; }
    }
}
