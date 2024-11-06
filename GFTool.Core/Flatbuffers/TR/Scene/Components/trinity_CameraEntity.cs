using FlatSharp.Attributes;
using Trinity.Core.Flatbuffers.Utils;

namespace GFTool.Core.Flatbuffers.TR.Scene.Components
{
    [FlatBufferTable]
    public class trinity_CameraEntity
    {
        [FlatBufferItem(0)]
        public string camera_name { get; set; }

        [FlatBufferItem(1)]
        public uint res_1 { get; set; }

        [FlatBufferItem(2)]
        public Vector3f unk_2 { get; set; }

        [FlatBufferItem(3)]
        public Vector3f unk_3 { get; set; }

        [FlatBufferItem(4)]
        public float unk_4 { get; set; }

        [FlatBufferItem(5)]
        public float unk_5 { get; set; }

        [FlatBufferItem(6)]
        public float unk_6 { get; set; }

        [FlatBufferItem(7)]
        public float unk_7 { get; set; }

        [FlatBufferItem(8)]
        public uint res_8 { get; set; }

        [FlatBufferItem(9)]
        public byte unk_9 { get; set; }

        [FlatBufferItem(10)]
        public uint[] unk_10 { get; set; }

        [FlatBufferItem(11)]
        public uint res_11 { get; set; }

        [FlatBufferItem(12)]
        public uint res_12 { get; set; }

        [FlatBufferItem(13)]
        public byte res_13 { get; set; }
    }
}
