using FlatSharp.Attributes;

namespace GFTool.Core.Flatbuffers.TR.Scene.Components
{
    [FlatBufferTable]
    public class TrinityGrassCollisionComponent
    {
        [FlatBufferItem(0)]
        public string name { get; set; }

        [FlatBufferItem(1)]
        public float min { get; set; }

        [FlatBufferItem(2)]
        public float max { get; set; }
    }
}
