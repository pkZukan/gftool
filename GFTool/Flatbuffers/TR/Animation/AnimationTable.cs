using FlatSharp.Attributes;

namespace GFTool.Flatbuffers.TR.Animation
{
    [FlatBufferTable]
    public class AnimationEntry
    {
        [FlatBufferItem(00)] public string Name { get; set; } = string.Empty;
        [FlatBufferItem(01)] public string FileName { get; set; } = string.Empty ;
    }

    [FlatBufferTable]
    public class AnimationTable
    {
        [FlatBufferItem(00)] List<AnimationEntry> Entries { get; set; } = new List<AnimationEntry>();
    }
}
