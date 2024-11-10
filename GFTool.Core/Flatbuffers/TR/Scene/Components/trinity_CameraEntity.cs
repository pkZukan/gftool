using FlatSharp.Attributes;
using Trinity.Core.Flatbuffers.Utils;

namespace GFTool.Core.Flatbuffers.TR.Scene.Components
{
    [FlatBufferTable]
    public class trinity_CameraEntity
    {
        [FlatBufferItem(0)]
        public string Name { get; set; }

        [FlatBufferItem(1)]
        public bool Activate { get; set; }

        [FlatBufferItem(2)]
        public Vector3f Position { get; set; }

        [FlatBufferItem(3)]
        public Vector3f Rotation { get; set; }

        [FlatBufferItem(4)]
        public float Distance { get; set; }

        [FlatBufferItem(5)]
        public float FovY { get; set; }

        [FlatBufferItem(6)]
        public float NearPlane { get; set; }

        [FlatBufferItem(7)]
        public float FarPlane { get; set; }

        [FlatBufferItem(8)]
        public byte ProjectionType { get; set; }

        [FlatBufferItem(9)]
        public byte CameraMode { get; set; }

        [FlatBufferItem(10)]
        public string TargetName { get; set; }

        [FlatBufferItem(11)]
        public bool UseRoll { get; set; }

        [FlatBufferItem(12)]
        public float Roll { get; set; }

        [FlatBufferItem(13)]
        public bool AttachTransform { get; set; }
    }
}
