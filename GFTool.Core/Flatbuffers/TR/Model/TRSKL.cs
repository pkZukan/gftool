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
        [FlatBufferItem(0)] public Vector3 Scale { get; set; }
        [FlatBufferItem(1)] public Vector3 Rotation { get; set; }
        [FlatBufferItem(2)] public Vector3 Translation { get; set; }

    }
    [FlatBufferTable]
    public class TRBoneMetaData
    {
        [FlatBufferItem(0)] public byte LockTranslation { get; set; }
        [FlatBufferItem(1)] public byte Field_01 { get; set; }
        [FlatBufferItem(2)] public BoneMatrix BoneMatrix { get; set; }

    }

    [FlatBufferTable]
    public class BoneMatrix
    {
        [FlatBufferItem(0)] public Vector3 X { get; set; }
        [FlatBufferItem(1)] public Vector3 Y { get; set; }
        [FlatBufferItem(2)] public Vector3 Z { get; set; }
        [FlatBufferItem(3)] public Vector3 W { get; set; }
    }

    [FlatBufferTable]
    public class TRInverseKinematicMetaData
    {
        [FlatBufferItem(0)] public string Name { get; set; }
        [FlatBufferItem(1)] public string Target { get; set; }
        [FlatBufferItem(2)] public string Pole { get; set; }
        [FlatBufferItem(3)] public string Type { get; set; }
        [FlatBufferItem(4)] public string Field_04 { get; set; }
        [FlatBufferItem(5)] public Vector3 Field_05 { get; set; }
        [FlatBufferItem(6)] public Vector4 Field_06 { get; set; }
    }

    [FlatBufferTable]
    public class TRTransformNode
    {
        [FlatBufferItem(0)] public string Name { get; set; }
        [FlatBufferItem(1)] public Transform Transform { get; set; }
        [FlatBufferItem(2)] public Vector3 ScalePivot { get; set; }
        [FlatBufferItem(3)] public Vector3 RotatePivot { get; set; }
        [FlatBufferItem(4)] public int ParentNodeIndex { get; set; }
        [FlatBufferItem(5)] public int BoneMetaDataIndex { get; set; }
        [FlatBufferItem(6)] public string LocatorMetaDataPath { get; set; }
        [FlatBufferItem(7)] public NodeType NodeType { get; set; }
    }
    [FlatBufferTable]
    public class TRSKL
    {
        [FlatBufferItem(0)] public int Field_00 { get; set; }
        [FlatBufferItem(1)] public TRTransformNode[] TransformNodes { get; set; }
        [FlatBufferItem(2)] public TRBoneMetaData[] BoneMetaData { get; set; }
        [FlatBufferItem(3)] public TRInverseKinematicMetaData[] IKMetaData { get; set; }
    }
}
