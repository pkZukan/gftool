using FlatSharp.Attributes;

namespace GFTool.Core.Flatbuffers.TR.Scene.Components;

[FlatBufferTable]
public class trinity_ModelInstancerComponent
{
    [FlatBufferItem(0)] public string FilePath { get; set; } = string.Empty;
}
