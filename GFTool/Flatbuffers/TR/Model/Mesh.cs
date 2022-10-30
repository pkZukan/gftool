using FlatSharp.Attributes;
using GFTool.Flatbuffers.Utils;

namespace GFTool.Flatbuffers.TR.Model
{
    [FlatBufferEnum(typeof(int))]
    public enum PolygonType
    {
        X8_Y8_Z8_UNSIGNED = 0,
        X16_Y16_Z16_UNSIGNED = 1,
        X32_Y32_Z32_UNSIGNED = 2,
        X64_Y64_Z64_UNSIGNED = 3,
    }

    [FlatBufferEnum(typeof(int))]
    public enum VertexAttributeIndex
    {
        NONE = 0,
        POSITION,
        NORMAL,
        TANGENT,
        BINORMAL,
        COLOR,
        TEX_COORD,
        BLEND_INDEX,
        BLEND_WEIGHTS,
    }
    [FlatBufferEnum(typeof(int))]
    public enum VertexType
    {
        NONE = 0,
        R8_G8_B8_A8_UNSIGNED_NORMALIZED = 20,
        W8_X8_Y8_Z8_UNSIGNED = 22,
        W16_X16_Y16_Z16_UNSIGNED_NORMALIZED = 39,
        W16_X16_Y16_Z16_FLOAT = 43,
        X32_Y32_FLOAT = 48,
        X32_Y32_Z32_FLOAT = 51,
        W32_X32_Y32_Z32_FLOAT = 54,
    }
    [FlatBufferTable]
    public class VertexAttribute
    {
        [FlatBufferItem(0)] public int Field_00 { get; set; }
        [FlatBufferItem(1)] public VertexAttributeIndex Attribute { get; set; }
        [FlatBufferItem(2)] public int AttributeLayer { get; set; }
        [FlatBufferItem(3)] public VertexType Type { get; set; }
        [FlatBufferItem(4)] public int Pointer { get; set; }
    }

    [FlatBufferTable]
    public class VertexSize
    {
        [FlatBufferItem(0)] public int Size { get; set; }
    }

    [FlatBufferTable]
    public class VertexAttributeLayout
    {
        [FlatBufferItem(0)] public VertexAttribute[] Attributes { get; set; }
        [FlatBufferItem(1)] public VertexSize[] Sizes { get; set; }
    }

    [FlatBufferTable]
    public class MaterialAttribute
    {
        [FlatBufferItem(0)] public int PolygonCount { get; set; }
        [FlatBufferItem(1)] public int PolygonPointer { get; set; }
        [FlatBufferItem(2)] public int Field_02 { get; set; }
        [FlatBufferItem(3)] public string MaterialName { get; set; }
        [FlatBufferItem(4)] public int Field_04 { get; set; }
    }
    
    [FlatBufferTable]
    public class BoneWeights
    {
        [FlatBufferItem(0)] public int RigIndex { get; set; }
        [FlatBufferItem(1)] public float RigWeight { get; set; }
    }

    [FlatBufferTable]
    public class Shape
    {
        [FlatBufferItem(0)] public string Name { get; set; }
        [FlatBufferItem(1)] public BoundingBox Bounds { get; set; }
        [FlatBufferItem(2)] public PolygonType PolygonType { get; set; }
        [FlatBufferItem(3)] public VertexAttributeLayout[] VertexAttributes { get; set; }
        [FlatBufferItem(4)] public MaterialAttribute[] MaterialAttributes { get; set; }
        [FlatBufferItem(5)] public int Field_05 { get; set; }
        [FlatBufferItem(6)] public int Field_06 { get; set; }
        [FlatBufferItem(7)] public int Field_07 { get; set; }
        [FlatBufferItem(8)] public int Field_08 { get; set; }
        [FlatBufferItem(9)] public Sphere ClipSphere { get; set; }
        [FlatBufferItem(10)] public BoneWeights[] Weights { get; set; }
        [FlatBufferItem(11)] public string Field_11 { get; set; }
        [FlatBufferItem(12)] public string Field_12 { get; set; }
    }

    [FlatBufferTable]
    public class Mesh
    {
        [FlatBufferItem(0)] public int Field_00 { get; set; }
        [FlatBufferItem(1)] public Shape[] Shapes { get; set; }
        [FlatBufferItem(2)] public string BufferPathName { get; set; }

    }
}
