using FlatSharp.Attributes;

namespace GFTool.Core.Flatbuffers.TR.Scene.Components
{
    [FlatBufferTable]
    public class trinity_ParticleComponent
    {
        [FlatBufferItem(0)]
        public string particle_file { get; set; }

        [FlatBufferItem(1)]
        public uint[] unk_1 { get; set; }

        [FlatBufferItem(2)]
        public uint res_2 { get; set; }

        [FlatBufferItem(3)]
        public string particle_name { get; set; }

        [FlatBufferItem(4)]
        public string particle_parent { get; set; }
    }
}
