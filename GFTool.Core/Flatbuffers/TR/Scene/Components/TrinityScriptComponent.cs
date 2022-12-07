using FlatSharp.Attributes;

namespace GFTool.Core.Flatbuffers.TR.Scene.Components
{
    [FlatBufferTable]
    public class TrinityScriptComponent
    {
        [FlatBufferItem(0)]
        public string script_file { get; set; }

        [FlatBufferItem(1)]
        public string script_hash { get; set; }

        [FlatBufferItem(2)]
        public uint res_2 { get; set; }

        [FlatBufferItem(3)]
        public uint res_3 { get; set; }

        [FlatBufferItem(4)]
        public uint res_4 { get; set; }

        [FlatBufferItem(5)]
        public string class_name { get; set; }
    }
}
