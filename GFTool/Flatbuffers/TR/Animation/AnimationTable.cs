using FlatSharp.Attributes;

namespace GFTool.Flatbuffers.TR.Animation
{
    [FlatBufferTable]
    public class AnimationEntry
    {
        [FlatBufferItem(0)] public string Name { get; set; } = string.Empty;
        [FlatBufferItem(1)] public string FileName { get; set; } = string.Empty ;
    }

    [FlatBufferTable]
    public class AnimationTable
    {
        [FlatBufferItem(0)] List<AnimationEntry> Entries { get; set; } = new List<AnimationEntry>();
    }
}
