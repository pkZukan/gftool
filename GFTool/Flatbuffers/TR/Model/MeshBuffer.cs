using FlatSharp.Attributes;

namespace GFTool.Flatbuffers.TR.Model
{
        [FlatBufferTable]
        public class Buffer
        {
            [FlatBufferItem(0)] public byte[] Bytes { get; set; }
    }

        [FlatBufferTable]
        public class MeshBuffer
        {
            [FlatBufferItem(0)] public Buffer PolygonBuffer { get; set; }
            [FlatBufferItem(1)] public Buffer VertexBuffer { get; set; }
        
        }

        [FlatBufferTable]
        public class TRMBF
        {
            [FlatBufferItem(0)] public int Field_00 { get; set; }
            [FlatBufferItem(1)] public MeshBuffer[] Buffers { get; set; }
        }
}
