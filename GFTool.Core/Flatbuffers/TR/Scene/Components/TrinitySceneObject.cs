using FlatSharp.Attributes;

namespace GFTool.Core.Flatbuffers.TR.Scene.Components
{
    [FlatBufferTable]
    public class TrinitySceneObject
    {
        [FlatBufferItem(0)]
        public string object_name { get; set; }

        [FlatBufferItem(1)]
        public SRT object_SRT { get; set; }

        [FlatBufferItem(2)]
        public uint res_2 { get; set; }

        [FlatBufferItem(3)]
        public uint res_3 { get; set; }

        [FlatBufferItem(4)]
        public uint[] unk_4 { get; set; }

        [FlatBufferItem(5)]
        public byte unk_5 { get; set; }

        [FlatBufferItem(6)]
        public uint res_6 { get; set; }

        [FlatBufferItem(7)]
        public uint[] unk_7 { get; set; }

        [FlatBufferItem(8)]
        public string[] unk_8 { get; set; }
    }
}
