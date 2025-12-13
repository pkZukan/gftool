using FlatSharp.Attributes;

namespace GFTool.Core.Flatbuffers.TR.UI
{
    [FlatBufferTable]
    public class ViewChunk
    {
        [FlatBufferItem(0)] public string Type { get; set; }
        [FlatBufferItem(1)] public byte[] Data { get; set; }
        [FlatBufferItem(2)] public ViewChunk[] Children { get; set; }

    }

    [FlatBufferTable]
    public class TRUIV
    {
        [FlatBufferItem(0)] public ViewChunk[] Chunks { get; set; }
    }
}
