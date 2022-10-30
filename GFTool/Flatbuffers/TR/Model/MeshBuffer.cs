using FlatSharp.Attributes;

namespace GFTool.Flatbuffers.TR.Model
{
        [FlatBufferTable]
        public class Buffer
        {
            [FlatBufferItem(00)] public byte[] Bytes { get; set; }
    }

        [FlatBufferTable]
        public class MeshBuffer
        {
            [FlatBufferItem(00)] public Buffer PolygonBuffer { get; set; }
            [FlatBufferItem(01)] public Buffer VertexBuffer { get; set; }
        
        }

        [FlatBufferTable]
        public class TRMBF
        {
            [FlatBufferItem(00)] public int Field_00 { get; set; }
            [FlatBufferItem(01)] public MeshBuffer[] Buffers { get; set; }
        }
}
