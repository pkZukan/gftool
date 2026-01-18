using FlatSharp.Attributes;

namespace GFTool.Core.Flatbuffers.TR.Scene.Components
{
    [FlatBufferTable]
    public class PropertySheetSomeTable3
    {
        [FlatBufferItem(0)]
        public ulong unk0 { get; set; }

        [FlatBufferItem(1)]
        public bool unk1 { get; set; }
    }

    [FlatBufferTable]
    public class PropertySheetProperty
    {
        [FlatBufferItem(0)]
        public string? name { get; set; }

        [FlatBufferItem(1)]
        public bool value { get; set; }

        [FlatBufferItem(2)]
        public PropertySheetSomeTable3? unk1 { get; set; }
    }

    [FlatBufferTable]
    public class PropertySheetEntry
    {
        [FlatBufferItem(0)]
        public PropertySheetProperty[]? properties { get; set; }
    }

    [FlatBufferTable]
    public class trinity_PropertySheet
    {
        [FlatBufferItem(0)]
        public string? name { get; set; }

        [FlatBufferItem(1)]
        public string? template { get; set; }

        [FlatBufferItem(2)]
        public PropertySheetEntry[]? entries { get; set; }
    }
}
