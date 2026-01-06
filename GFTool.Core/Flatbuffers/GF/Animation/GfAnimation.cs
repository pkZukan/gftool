using System.Collections.Generic;
using FlatSharp.Attributes;
using Trinity.Core.Flatbuffers.Utils;

namespace Trinity.Core.Flatbuffers.GF.Animation
{
    [FlatBufferTable]
    public class Animation
    {
        [FlatBufferItem(0)]
        public Info Info { get; set; } = new Info();

        [FlatBufferItem(1)]
        public BoneAnimation Skeleton { get; set; } = new BoneAnimation();
    }

    [FlatBufferTable]
    public class Info
    {
        [FlatBufferItem(0)] public uint DoesLoop { get; set; }
        [FlatBufferItem(1)] public uint KeyFrames { get; set; }
        [FlatBufferItem(2)] public uint FrameRate { get; set; }
    }

    [FlatBufferTable]
    public class BoneAnimation
    {
        [FlatBufferItem(0)]
        public IList<BoneTrack> Tracks { get; set; } = new List<BoneTrack>();

        [FlatBufferItem(1)]
        public BoneInit? InitData { get; set; }
    }

    [FlatBufferTable]
    public class BoneInit
    {
        [FlatBufferItem(0)] public uint IsInit { get; set; }
        [FlatBufferItem(1)] public Transform Transform { get; set; } = new Transform();
    }

    [FlatBufferTable]
    public class BoneTrack
    {
        [FlatBufferItem(0)] public string Name { get; set; } = string.Empty;
        [FlatBufferItem(1)] public FlatSharp.FlatBufferUnion<FixedVectorTrack, DynamicVectorTrack, Framed16VectorTrack, Framed8VectorTrack> Scale { get; set; }
        [FlatBufferItem(3)] public FlatSharp.FlatBufferUnion<FixedRotationTrack, DynamicRotationTrack, Framed16RotationTrack, Framed8RotationTrack> Rotate { get; set; }
        [FlatBufferItem(5)] public FlatSharp.FlatBufferUnion<FixedVectorTrack, DynamicVectorTrack, Framed16VectorTrack, Framed8VectorTrack> Translate { get; set; }
    }

    [FlatBufferTable]
    public class FixedVectorTrack
    {
        [FlatBufferItem(0)] public Vector3f Co { get; set; } = new Vector3f();
    }

    [FlatBufferTable]
    public class DynamicVectorTrack
    {
        [FlatBufferItem(0)] public IList<Vector3f> Co { get; set; } = new List<Vector3f>();
    }

    [FlatBufferTable]
    public class Framed16VectorTrack
    {
        [FlatBufferItem(0)] public IList<ushort> Frames { get; set; } = new List<ushort>();
        [FlatBufferItem(1)] public IList<Vector3f> Co { get; set; } = new List<Vector3f>();
    }

    [FlatBufferTable]
    public class Framed8VectorTrack
    {
        [FlatBufferItem(0)] public IList<byte> Frames { get; set; } = new List<byte>();
        [FlatBufferItem(1)] public IList<Vector3f> Co { get; set; } = new List<Vector3f>();
    }

    [FlatBufferTable]
    public class FixedRotationTrack
    {
        [FlatBufferItem(0)] public PackedQuaternion Co { get; set; } = new PackedQuaternion();
    }

    [FlatBufferTable]
    public class DynamicRotationTrack
    {
        [FlatBufferItem(0)] public IList<PackedQuaternion> Co { get; set; } = new List<PackedQuaternion>();
    }

    [FlatBufferTable]
    public class Framed16RotationTrack
    {
        [FlatBufferItem(0)] public IList<ushort> Frames { get; set; } = new List<ushort>();
        [FlatBufferItem(1)] public IList<PackedQuaternion> Co { get; set; } = new List<PackedQuaternion>();
    }

    [FlatBufferTable]
    public class Framed8RotationTrack
    {
        [FlatBufferItem(0)] public IList<byte> Frames { get; set; } = new List<byte>();
        [FlatBufferItem(1)] public IList<PackedQuaternion> Co { get; set; } = new List<PackedQuaternion>();
    }
}
