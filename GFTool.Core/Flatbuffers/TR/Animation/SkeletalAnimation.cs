using FlatSharp;
using FlatSharp.Attributes;
using GFTool.Core.Flatbuffers.Converters;
using GFTool.Core.Flatbuffers.Utils;
using System.Text.Json.Serialization;

namespace GFTool.Core.Flatbuffers.TR.Animation
{
    [FlatBufferTable]
    public class FixedVectorTrack
    {
        [FlatBufferItem(0)]
        public Vector3 Value { get; set; }
    }

    [FlatBufferTable]
    public class FramedVectorTrack
    {
        [FlatBufferItem(0)]
        public IList<Vector3> Values { get; set; }
    }

    [FlatBufferTable]
    public class Keyed16VectorTrack
    {
        [FlatBufferItem(0)]
        public IList<UInt16> Keys { get; set; }
        [FlatBufferItem(1)]
        public IList<Vector3> Values { get; set; }
    }

    [FlatBufferTable]
    public class Keyed8VectorTrack
    {
        [FlatBufferItem(0)]
        public IList<Byte> Keys { get; set; }
        [FlatBufferItem(1)]
        public IList<Vector3> Values { get; set; }
    }
    [FlatBufferTable]
    public class FixedRotationTrack
    {
        [FlatBufferItem(0)]
        public PackedQuaternion Value { get; set; }
    }

    [FlatBufferTable]
    public class FramedRotationTrack
    {
        [FlatBufferItem(0)]
        public IList<PackedQuaternion> Values { get; set; }

    }

    [FlatBufferTable]
    public class Keyed16RotationTrack
    {
        [FlatBufferItem(0)]
        public IList<ushort> Keys { get; set; }
        [FlatBufferItem(1)]
        public IList<PackedQuaternion> Values { get; set; }
    }

    [FlatBufferTable]
    public class Keyed8RotationTrack
    {
        [FlatBufferItem(0)]
        public IList<byte> Keys { get; set; }
        [FlatBufferItem(1)]
        public IList<PackedQuaternion> Values { get; set; }
    }
    [FlatBufferTable]
    public class SkeletalTrack
    {
        [FlatBufferItem(0)]
        public string BoneName { get; set; }
        
        [JsonConverter(typeof(VectorTrackUnionConverter))]
        [FlatBufferItem(1)]
        public FlatBufferUnion<FixedVectorTrack, FramedVectorTrack, Keyed16VectorTrack, Keyed8VectorTrack> ScaleChannel { get; set; }

        [JsonConverter(typeof(RotationTrackUnionConverter))]
        [FlatBufferItem(3)]
        public FlatBufferUnion<FixedRotationTrack, FramedRotationTrack, Keyed16RotationTrack, Keyed8RotationTrack> RotationChannel { get; set; }

        [JsonConverter(typeof(VectorTrackUnionConverter))]
        [FlatBufferItem(5)]
        public FlatBufferUnion<FixedVectorTrack, FramedVectorTrack, Keyed16VectorTrack, Keyed8VectorTrack> TranslateChannel { get; set; }

    }

    [FlatBufferTable]
    public class PlaybackInfo
    {
        [FlatBufferItem(0)]
        public UInt32 IsLooped { get; set; }
        [FlatBufferItem(1)]
        public UInt32 FrameCount { get; set; }
        [FlatBufferItem(2)]
        public UInt32 FrameRate { get; set; }
    }

    [FlatBufferTable]
    public class BoneInit
    {
        [FlatBufferItem(0)]
        public UInt32 IsInit { get; set; }
        [FlatBufferItem(1)]
        public Transform BoneTransform { get; set; }
    }

    [FlatBufferTable]
    public class SkeletalAnimation
    {
        [FlatBufferItem(0)]
        public IList<SkeletalTrack> Tracks { get; set; }
        [FlatBufferItem(1)]
        public BoneInit Init { get; set; }
    }

    [FlatBufferTable]
    public class TRANM
    {
        [FlatBufferItem(0)]
        public PlaybackInfo Info { get; set; }
        [FlatBufferItem(1)]
        public SkeletalAnimation SkeletalAnimation { get; set; }
    }
}
