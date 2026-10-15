using FlatSharp.Attributes;

namespace GFTool.Core.Flatbuffers.TR.Scene;

[FlatBufferTable]
public class InstanceBuffer
{
    [FlatBufferItem(0, Required = true)] public byte[] Data { get; set; } = Array.Empty<byte>();
}

[FlatBufferTable]
public class TRINS
{
    [FlatBufferItem(0, Required = true)] public string TypeName { get; set; } = string.Empty;
    [FlatBufferItem(1)] public uint Version { get; set; }
    [FlatBufferItem(2)] public uint Count { get; set; }
    [FlatBufferItem(3)] public uint Reserved { get; set; }
    [FlatBufferItem(4, Required = true)] public InstanceBuffer Buffer { get; set; } = new();
}
