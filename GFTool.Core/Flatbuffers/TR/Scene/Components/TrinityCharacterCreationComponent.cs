using FlatSharp.Attributes;

namespace GFTool.Core.Flatbuffers.TR.Scene.Components
{
    [FlatBufferTable]
    public class CCDataMasterEntry {
        [FlatBufferItem(0)]
        public string Part { get; set; }

        [FlatBufferItem(1)]
        public string File { get; set; }

        [FlatBufferItem(2)]
        public string Name { get; set; }
    }

    [FlatBufferTable]
    public class TrinityCharacterCreationComponent
    {
        [FlatBufferItem(0)]
        public string Name { get; set; }

        [FlatBufferItem(1)]
        public uint unk0 { get; set; }

        [FlatBufferItem(2)]
        public float unk1 { get; set; }

        [FlatBufferItem(3)]
        public float unk2 { get; set; }

        [FlatBufferItem(4)]
        public float unk3 { get; set; }

        [FlatBufferItem(5)]
        public uint unk4 { get; set; }

        [FlatBufferItem(6)]
        public float unk5 { get; set; }

        [FlatBufferItem(7)]
        public uint unk6 { get; set; }

        [FlatBufferItem(8)]
        public float unk7 { get; set; }

        [FlatBufferItem(9)]
        public uint unk8 { get; set; }

        [FlatBufferItem(10)]
        public float unk9 { get; set; }

        [FlatBufferItem(11)]
        public uint unk10 { get; set; }

        [FlatBufferItem(12)]
        public float unk11 { get; set; }

        [FlatBufferItem(13)]
        public uint unk12 { get; set; }

        [FlatBufferItem(14)]
        public uint unk13 { get; set; }

        [FlatBufferItem(15)]
        public CCDataMasterEntry[] ccdataMasterList { get; set; }

        [FlatBufferItem(16)]
        public uint[] unk14 { get; set; }
    }
}
