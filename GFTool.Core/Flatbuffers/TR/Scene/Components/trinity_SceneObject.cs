using FlatSharp.Attributes;
using Trinity.Core.Flatbuffers.Utils;

namespace GFTool.Core.Flatbuffers.TR.Scene.Components
{
    [FlatBufferTable]
    public class ObjectLayer
    {
        [FlatBufferItem(0)]
        public int Priority { get; set; }

        [FlatBufferItem(1)]
        public string Name { get; set; }

        [FlatBufferItem(2)]
        public trinity_Transform Srt { get; set; } = new trinity_Transform();
    }

    [FlatBufferTable]
    public class trinity_SceneObject
    {
        [FlatBufferItem(0)]
        public string Name { get; set; }

        [FlatBufferItem(1)]
        public trinity_Transform Srt { get; set; } = new trinity_Transform();

        [FlatBufferItem(2)]
        public bool KeepWorldSrt { get; set; }

        [FlatBufferItem(3)]
        public bool AttachTransform { get; set; }

        [FlatBufferItem(4)]
        public string AttachJointName { get; set; }

        [FlatBufferItem(5)]
        public bool Scriptable { get; set; }

        [FlatBufferItem(6)]
        public byte Priority { get; set; }

        [FlatBufferItem(7)]
        public ObjectLayer[] Layers { get; set; }

        [FlatBufferItem(8)]
        public string[] TagList { get; set; }
    }
}
