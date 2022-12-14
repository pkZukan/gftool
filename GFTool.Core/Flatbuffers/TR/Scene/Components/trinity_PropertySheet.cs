using FlatSharp.Attributes;

namespace GFTool.Core.Flatbuffers.TR.Scene.Components
{
    [FlatBufferTable]
    public class UnkTable2
    {
        [FlatBufferItem(0)]
        public UInt64 unk0 { get; set; }

        [FlatBufferItem(1)]
        public bool unk1 { get; set; }
    }

    [FlatBufferTable]
    public class Property
    {
        [FlatBufferItem(0)]
        public string name { get; set; }

        [FlatBufferItem(1)]
        public bool value { get; set; }

        [FlatBufferItem(2)]
        public UnkTable2 unk0 { get; set; }
    }

    [FlatBufferTable]
    public class UnkTable {
        [FlatBufferItem(0)]
        public Property[] properties { get; set; }
    }

    [FlatBufferTable]
    public class trinity_PropertySheet
    {
        [FlatBufferItem(0)]
        public string property_name { get; set; }

        [FlatBufferItem(1)]
        public string property_template { get; set; }

        [FlatBufferItem(2)]
        public UnkTable[] unk0 { get; set; }
    }
}
