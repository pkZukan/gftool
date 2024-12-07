using FlatSharp.Attributes;
using Trinity.Core.Flatbuffers.Utils;

namespace Trinity.Core.Flatbuffers.TR.Model
{
    [FlatBufferEnum(typeof(int))]
    public enum TRIndexFormat
    {
        BYTE = 0,
        SHORT,
        INT,
        Count
    }

    [FlatBufferEnum(typeof(int))]
    public enum TRVertexUsage
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
    public enum TRVertexFormat
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
    public class TRVertexElement
    {
        [FlatBufferItem(0)] public int vertexElementSizeIndex { get; set; }
        [FlatBufferItem(1)] public TRVertexUsage vertexUsage { get; set; }
        [FlatBufferItem(2)] public int vertexElementLayer { get; set; }
        [FlatBufferItem(3)] public TRVertexFormat vertexFormat { get; set; }
        [FlatBufferItem(4)] public int vertexElementOffset { get; set; }
    }

    [FlatBufferTable]
    public class TRVertexElementSize
    {
        [FlatBufferItem(0)] public int elementSize { get; set; }
    }

    [FlatBufferTable]
    public class TRVertexDeclaration
    {
        [FlatBufferItem(0)] public TRVertexElement[] vertexElements { get; set; }
        [FlatBufferItem(1)] public TRVertexElementSize[] vertexElementSizes { get; set; }
    }

    [FlatBufferTable]
    public partial class TRMeshPart
    {
        [FlatBufferItem(0)] public int indexCount { get; set; }
        [FlatBufferItem(1)] public int indexOffset { get; set; }
        [FlatBufferItem(2)] public int Field_02 { get; set; }
        [FlatBufferItem(3)] public string MaterialName { get; set; }
        [FlatBufferItem(4)] public int vertexDeclarationIndex { get; set; }
    }
    
    [FlatBufferTable]
    public class TRBoneWeight
    {
        [FlatBufferItem(0)] public int RigIndex { get; set; }
        [FlatBufferItem(1)] public float RigWeight { get; set; }
    }

    [FlatBufferTable]
    public partial class TRMesh
    {
        [FlatBufferItem(0)] public string Name { get; set; }
        [FlatBufferItem(1)] public TRBoundingBox boundingBox { get; set; }
        [FlatBufferItem(2)] public TRIndexFormat IndexType { get; set; }
        [FlatBufferItem(3)] public TRVertexDeclaration[] vertexDeclaration { get; set; }
        [FlatBufferItem(4)] public TRMeshPart[] meshParts { get; set; }
        [FlatBufferItem(5)] public int Field_05 { get; set; }
        [FlatBufferItem(6)] public int Field_06 { get; set; }
        [FlatBufferItem(7)] public int Field_07 { get; set; }
        [FlatBufferItem(8)] public int Field_08 { get; set; }
        [FlatBufferItem(9)] public Sphere clipSphere { get; set; }
        [FlatBufferItem(10)] public TRBoneWeight[] boneWeight { get; set; }
        [FlatBufferItem(11)] public string Field_11 { get; set; }
        [FlatBufferItem(12)] public string Field_12 { get; set; }
    }

    [FlatBufferTable]
    public partial class TRMSH
    {
        [FlatBufferItem(0)] public int Version { get; set; }
        [FlatBufferItem(1)] public TRMesh[] Meshes { get; set; }
        [FlatBufferItem(2)] public string bufferFilePath { get; set; }

    }
}
