using FlatSharp.Attributes;
using Trinity.Core.Flatbuffers.Utils;

namespace Trinity.Core.Flatbuffers.TR.Model
{
    [FlatBufferEnum(typeof(int))]
    public enum NodeType
    {
        Transform = 0,
        Joint = 1,
        Locator = 2,
    }

    [FlatBufferTable]
    public class Transform
    {
        [FlatBufferItem(0)] public Vector3f Scale { get; set; }
        [FlatBufferItem(1)] public Vector3f Rotation { get; set; }
        [FlatBufferItem(2)] public Vector3f Translation { get; set; }

    }
    [FlatBufferTable]
    public class TRBoneMetaData
    {
        [FlatBufferItem(0)] public bool LockTranslation { get; set; }
        [FlatBufferItem(1)] public bool Skinning { get; set; }
        [FlatBufferItem(2)] public BoneMatrix BoneMatrix { get; set; }

    }

    [FlatBufferTable]
    public class BoneMatrix
    {
        [FlatBufferItem(0)] public Vector3f X { get; set; }
        [FlatBufferItem(1)] public Vector3f Y { get; set; }
        [FlatBufferItem(2)] public Vector3f Z { get; set; }
        [FlatBufferItem(3)] public Vector3f W { get; set; }
    }

    [FlatBufferTable]
    public class TRInverseKinematicMetaData
    {
        [FlatBufferItem(0)] public string Name { get; set; }
        [FlatBufferItem(1)] public string Target { get; set; }
        [FlatBufferItem(2)] public string Pole { get; set; }
        [FlatBufferItem(3)] public string Type { get; set; }
        [FlatBufferItem(4)] public string Field_04 { get; set; }
        [FlatBufferItem(5)] public Vector3f Field_05 { get; set; }
        [FlatBufferItem(6)] public Vector4f Field_06 { get; set; }
    }

    [FlatBufferTable]
    public class TRTransformNode
    {
        [FlatBufferItem(0)] public string Name { get; set; }
        [FlatBufferItem(1)] public Transform Transform { get; set; }
        [FlatBufferItem(2)] public Vector3f ScalePivot { get; set; }
        [FlatBufferItem(3)] public Vector3f RotatePivot { get; set; }
        [FlatBufferItem(4)] public int ParentNodeIndex { get; set; }
        [FlatBufferItem(5)] public int BoneMetaDataIndex { get; set; }
        [FlatBufferItem(6)] public string LocatorMetaDataPath { get; set; }
        [FlatBufferItem(7)] public NodeType NodeType { get; set; }
    }
    [FlatBufferTable]
    public class TRSKL
    {
        [FlatBufferItem(0)] public int Version { get; set; }
        [FlatBufferItem(1)] public TRTransformNode[] TransformNodes { get; set; }
        [FlatBufferItem(2)] public TRBoneMetaData[] BoneMetaData { get; set; }
        [FlatBufferItem(3)] public TRInverseKinematicMetaData[] IKMetaData { get; set; }
    }
}
