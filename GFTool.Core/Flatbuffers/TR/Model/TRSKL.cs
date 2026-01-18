using System;
using FlatSharp.Attributes;
using Trinity.Core.Flatbuffers.Utils;

namespace Trinity.Core.Flatbuffers.TR.Model
{
    // TRSKL (.trskl) schema for the Titan/Ikkaku “node_list + joint_info_list” flavor observed in SV/ZA samples.
    // PokeDocs documents other TRSKL flavors; treat this as a local variant driven by binary evidence.

    [FlatBufferTable]
    public class Matrix4x3f
    {
        [FlatBufferItem(0)] public Vector3f AxisX { get; set; } = new Vector3f();
        [FlatBufferItem(1)] public Vector3f AxisY { get; set; } = new Vector3f();
        [FlatBufferItem(2)] public Vector3f AxisZ { get; set; } = new Vector3f();
        [FlatBufferItem(3)] public Vector3f AxisW { get; set; } = new Vector3f();
    }

    [FlatBufferTable]
    public class SRT
    {
        [FlatBufferItem(0)] public Vector3f Scale { get; set; } = new Vector3f();
        [FlatBufferItem(1)] public Vector3f Rotate { get; set; } = new Vector3f();
        [FlatBufferItem(2)] public Vector3f Translate { get; set; } = new Vector3f();
    }

    [FlatBufferTable]
    public class TRTransformNode
    {
        [FlatBufferItem(0)] public string Name { get; set; } = string.Empty;
        [FlatBufferItem(1)] public SRT Transform { get; set; } = new SRT();
        [FlatBufferItem(2)] public Vector3f ScalePivot { get; set; } = new Vector3f();
        [FlatBufferItem(3)] public Vector3f RotatePivot { get; set; } = new Vector3f();
        [FlatBufferItem(4, DefaultValue = -1)] public int ParentNodeIndex { get; set; } = -1;
        [FlatBufferItem(5, DefaultValue = -1)] public int JointInfoIndex { get; set; } = -1;
        [FlatBufferItem(6)] public string ParentNodeName { get; set; } = string.Empty;
        [FlatBufferItem(7)] public uint Priority { get; set; } = 0;
        [FlatBufferItem(8)] public bool PriorityPass { get; set; } = false;
        [FlatBufferItem(9)] public bool IgnoreParentRotation { get; set; } = false;
    }

    [FlatBufferTable]
    public class TRJointInfo
    {
        [FlatBufferItem(0)] public bool SegmentScaleCompensate { get; set; } = false;
        [FlatBufferItem(1)] public bool InfluenceSkinning { get; set; } = true;
        [FlatBufferItem(2)] public Matrix4x3f InverseBindPoseMatrix { get; set; } = new Matrix4x3f();
    }

    [FlatBufferTable]
    public class TRHelperBoneInfo
    {
        [FlatBufferItem(0)] public string Output { get; set; } = string.Empty;
        [FlatBufferItem(1)] public string Target { get; set; } = string.Empty;
        [FlatBufferItem(2)] public string Reference { get; set; } = string.Empty;
        [FlatBufferItem(3)] public string Type { get; set; } = string.Empty;
        [FlatBufferItem(4)] public string UpType { get; set; } = string.Empty;
        [FlatBufferItem(5)] public Vector3f Weight { get; set; } = new Vector3f();
        [FlatBufferItem(6)] public Vector4f Adjust { get; set; } = new Vector4f();
    }

    [FlatBufferTable]
    public class TRSKL
    {
        [FlatBufferItem(0)] public uint Version { get; set; } = 2;
        [FlatBufferItem(1)] public TRTransformNode[] TransformNodes { get; set; } = Array.Empty<TRTransformNode>();
        [FlatBufferItem(2)] public TRJointInfo[] JointInfos { get; set; } = Array.Empty<TRJointInfo>();
        [FlatBufferItem(3)] public TRHelperBoneInfo[] HelperBones { get; set; } = Array.Empty<TRHelperBoneInfo>();
        [FlatBufferItem(4, DefaultValue = -1)] public int SkinningPaletteOffset { get; set; } = -1;
        [FlatBufferItem(5)] public bool IsInteriorMap { get; set; } = false;
    }
}
