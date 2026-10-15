using FlatSharp;
using FlatSharp.Attributes;
using Trinity.Core.Flatbuffers.Utils;
using Trinity.Core.Flatbuffers.TR.Model;
using TrackFlag = FlatSharp.FlatBufferUnion<Trinity.Core.Flatbuffers.TR.Animation.FixedBoolTrack, Trinity.Core.Flatbuffers.TR.Animation.DynamicBoolTrack, Trinity.Core.Flatbuffers.TR.Animation.Framed16BoolTrack, Trinity.Core.Flatbuffers.TR.Animation.Framed8BoolTrack>;
using BlendTrack = FlatSharp.FlatBufferUnion<Trinity.Core.Flatbuffers.TR.Animation.FixedBlendTrack, Trinity.Core.Flatbuffers.TR.Animation.DynamicBlendTrack, Trinity.Core.Flatbuffers.TR.Animation.Framed16BlendTrack, Trinity.Core.Flatbuffers.TR.Animation.Framed8BlendTrack>;

namespace Trinity.Core.Flatbuffers.TR.Animation
{
    [FlatBufferTable]
    public class FixedBoolTrack
    {
        [FlatBufferItem(0)] public bool Value { get; set; }
    }

    [FlatBufferTable]
    public class DynamicBoolTrack
    {
        [FlatBufferItem(0)] public IList<byte> Value { get; set; }
    }

    [FlatBufferTable]
    public class Framed16BoolTrack
    {
        [FlatBufferItem(0)] public IList<ushort> Frames { get; set; }
        [FlatBufferItem(1)] public IList<byte> Value { get; set; }
    }

    [FlatBufferTable]
    public class Framed8BoolTrack
    {
        [FlatBufferItem(0)] public IList<byte> Frames { get; set; }
        [FlatBufferItem(1)] public IList<byte> Value { get; set; }
    }

    [FlatBufferTable]
    public class TrackFlagsInfo
    {
        [FlatBufferItem(0)] public TrackFlag? Values { get; set; }
    }

    [FlatBufferTable]
    public class VisibilityShapeTimeline
    {
        [FlatBufferItem(0)] public float Time { get; set; }
        [FlatBufferItem(1)] public float Value { get; set; }
        [FlatBufferItem(2)] public TrackFlagsInfo Info { get; set; }
    }

    [FlatBufferTable]
    public class TrackMaterialValue
    {
        [FlatBufferItem(0)] public float Time { get; set; }
        [FlatBufferItem(1)] public float Value { get; set; }
        [FlatBufferItem(2)] public uint Config0 { get; set; }
        [FlatBufferItem(3)] public uint Config1 { get; set; }
        [FlatBufferItem(4)] public uint Config2 { get; set; }
    }

    [FlatBufferTable]
    public class TrackMaterialValueList
    {
        [FlatBufferItem(0)] public IList<TrackMaterialValue> Values { get; set; }
    }

    [FlatBufferTable]
    public class TrackMaterialInit
    {
        [FlatBufferItem(0)] public string Name { get; set; }
        [FlatBufferItem(1)] public TrackMaterialValueList List { get; set; }
    }

    [FlatBufferTable]
    public class TrackMaterialChannels
    {
        [FlatBufferItem(0)] public TrackMaterialValueList X { get; set; }
        [FlatBufferItem(1)] public TrackMaterialValueList Y { get; set; }
        [FlatBufferItem(2)] public TrackMaterialValueList Z { get; set; }
        [FlatBufferItem(3)] public TrackMaterialValueList W { get; set; }
    }

    [FlatBufferTable]
    public class TrackMaterialAnim
    {
        [FlatBufferItem(0)] public string Name { get; set; }
        [FlatBufferItem(1)] public TrackMaterialChannels List { get; set; }
    }

    [FlatBufferTable]
    public class TrackMaterialTimeline
    {
        [FlatBufferItem(0)] public PlaybackInfo Res0 { get; set; }
        [FlatBufferItem(1)] public uint Res1 { get; set; }
        [FlatBufferItem(2)] public IList<TrackMaterial> MaterialTrack { get; set; }
        [FlatBufferItem(3)] public byte Unk3 { get; set; }
        [FlatBufferItem(4)] public byte Unk4 { get; set; }
    }

    [FlatBufferTable]
    public class TrackMaterial
    {
        [FlatBufferItem(0)] public string Name { get; set; }
        [FlatBufferItem(1)] public IList<TrackMaterialInit> InitValues { get; set; }
        [FlatBufferItem(2)] public IList<TrackMaterialAnim> AnimValues { get; set; }
    }

    [FlatBufferTable]
    public class TrackBlendshapeInit
    {
        [FlatBufferItem(0)] public string MeshName { get; set; }
        [FlatBufferItem(1)] public IList<string> BlendshapeList { get; set; }
    }

    [FlatBufferTable]
    public class FixedBlendTrack
    {
        [FlatBufferItem(0)] public float Value { get; set; }
    }

    [FlatBufferTable]
    public class DynamicBlendTrack
    {
        [FlatBufferItem(0)] public IList<float> Value { get; set; }
    }

    [FlatBufferTable]
    public class Framed16BlendTrack
    {
        [FlatBufferItem(0)] public IList<ushort> Frames { get; set; }
        [FlatBufferItem(1)] public IList<float> Value { get; set; }
    }

    [FlatBufferTable]
    public class Framed8BlendTrack
    {
        [FlatBufferItem(0)] public IList<byte> Frames { get; set; }
        [FlatBufferItem(1)] public IList<float> Value { get; set; }
    }

    [FlatBufferTable]
    public class TrackBlendshapeAnim
    {
        [FlatBufferItem(0)] public uint Key { get; set; }
        [FlatBufferItem(1)] public IList<float> Values { get; set; }
    }

    [FlatBufferTable]
    public class TrackBlendShape
    {
        [FlatBufferItem(0)] public string Name { get; set; }
        [FlatBufferItem(1)] public IList<TrackBlendshapeInit> InitValues { get; set; }
        [FlatBufferItem(2)] public BlendTrack? Track { get; set; }
        [FlatBufferItem(4)] public uint BlendListIdx { get; set; }
    }

    [FlatBufferTable]
    public class blendtable
    {
        [FlatBufferItem(0)] public string Name { get; set; }
    }

    [FlatBufferTable]
    public class BlendShapeTimeline
    {
        [FlatBufferItem(0)] public PlaybackInfo Info { get; set; }
        [FlatBufferItem(1)] public uint Res1 { get; set; }
        [FlatBufferItem(2)] public IList<TrackBlendShape> BlendshapeTracks { get; set; }
        [FlatBufferItem(3)] public uint Res3 { get; set; }
        [FlatBufferItem(4)] public IList<blendtable> BlendList { get; set; }
    }

    [FlatBufferTable]
    public class MeshAnimeTrack
    {
        [FlatBufferItem(0)] public string TrackPath { get; set; }
        [FlatBufferItem(1)] public uint Res1 { get; set; }
        [FlatBufferItem(2)] public uint Res2 { get; set; }
        [FlatBufferItem(3)] public uint Res3 { get; set; }
        [FlatBufferItem(4)] public TrackMaterialTimeline MaterialAnimation { get; set; }
        [FlatBufferItem(5)] public VisibilityShapeTimeline VisibilityAnimation { get; set; }
        [FlatBufferItem(6)] public BlendShapeTimeline BlendshapeAnimation { get; set; }
    }

    [FlatBufferTable]
    public class TRACM
    {
        [FlatBufferItem(0)] public PlaybackInfo Info { get; set; }
        [FlatBufferItem(1)] public IList<MeshAnimeTrack> Tracks { get; set; }
        [FlatBufferItem(2)] public byte MaterialFrameMultiplier { get; set; }
        [FlatBufferItem(3)] public byte VisibilityFrameMultiplier { get; set; }
        [FlatBufferItem(4)] public byte BlendshapeFrameMultiplier { get; set; }
    }

}
