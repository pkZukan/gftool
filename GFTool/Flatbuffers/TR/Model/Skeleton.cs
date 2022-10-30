using FlatSharp.Attributes;
using GFTool.Flatbuffers.Utils;

namespace GFTool.Flatbuffers.TR.Model
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
        [FlatBufferItem(00)] public Vector3 Scale { get; set; }
        [FlatBufferItem(01)] public Vector3 Rotate { get; set; }
        [FlatBufferItem(02)] public Vector3 Translate { get; set; }

    }
    [FlatBufferTable]
    public class Bone
    {
        [FlatBufferItem(00)] public byte LockTranslation { get; set; }
        [FlatBufferItem(01)] public byte Field_01 { get; set; }
        [FlatBufferItem(02)] public BoneMatrix BoneMatrix { get; set; }

    }
    [FlatBufferTable]
    public class BoneMatrix
    {
        [FlatBufferItem(00)] public Vector3 X { get; set; }
        [FlatBufferItem(01)] public Vector3 Y { get; set; }
        [FlatBufferItem(02)] public Vector3 Z { get; set; }
        [FlatBufferItem(03)] public Vector3 W { get; set; }
    }
    [FlatBufferTable]
    public class IK
    {
        [FlatBufferItem(00)] public string Name { get; set; }
        [FlatBufferItem(01)] public string Target { get; set; }
        [FlatBufferItem(02)] public string Pole { get; set; }
        [FlatBufferItem(03)] public string Type { get; set; }
        [FlatBufferItem(04)] public string Field_04 { get; set; }
        [FlatBufferItem(05)] public Vector3 Field_05 { get; set; }
        [FlatBufferItem(06)] public Vector4 Field_06 { get; set; }
    }
    [FlatBufferTable]
    public class TransformNode
    {
        [FlatBufferItem(00)] public string Name { get; set; }
        [FlatBufferItem(01)] public Transform Transform { get; set; }
        [FlatBufferItem(02)] public Vector3 ScalePivot { get; set; }
        [FlatBufferItem(03)] public Vector3 RotatePivot { get; set; }
        [FlatBufferItem(04)] public int ParentIndex { get; set; }
        [FlatBufferItem(05)] public int BoneIndex { get; set; }
        [FlatBufferItem(06)] public string LocatorBone { get; set; }
        [FlatBufferItem(07)] public NodeType NodeType { get; set; }
    }
    [FlatBufferTable]
    public class TRSKL
    {
        [FlatBufferItem(00)] public int Field_00 { get; set; }
        [FlatBufferItem(01)] public TransformNode[] Bones { get; set; }
        [FlatBufferItem(02)] public Bone[] Params { get; set; }
        [FlatBufferItem(03)] public IK[] InverseKinematics { get; set; }
    }
}
