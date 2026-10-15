using FlatSharp.Attributes;
using Trinity.Core.Flatbuffers.Utils;

namespace Trinity.Core.Flatbuffers.TR.Model
{
    [FlatBufferEnum(typeof(uint))]
    public enum TRIndexFormat : uint
    {
        BYTE = 0,
        SHORT,
        INT,
        LONG,
        Count
    }

    [FlatBufferEnum(typeof(uint))]
    public enum TRVertexUsage : uint
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
        USER,
        USER_ID,
    }

    [FlatBufferEnum(typeof(uint))]
    public enum TRVertexFormat : uint
    {
        NONE = 0,
        R8_G8_B8_A8_UNSIGNED_NORMALIZED = 20,
        W8_X8_Y8_Z8_UNSIGNED = 22,
        X32_UNSIGNED = 36,
        X32_SIGNED = 37,
        // Observed in TRMSH/TRMBF: format code 0x34 (52) used for BLEND_INDEX as 4x uint32.
        // Tooling references this as "4UINTS32".
        W32_X32_Y32_Z32_UNSIGNED = 52,
        W16_X16_Y16_Z16_UNSIGNED_NORMALIZED = 39,
        W16_X16_Y16_Z16_FLOAT = 43,
        X32_Y32_FLOAT = 48,
        X32_Y32_Z32_FLOAT = 51,
        W32_X32_Y32_Z32_FLOAT = 54,
    }

    [FlatBufferTable]
    public class TRVertexElement
    {
        [FlatBufferItem(0, DefaultValue = -1)] public int vertexElementSizeIndex { get; set; } = -1;
        [FlatBufferItem(1)] public TRVertexUsage vertexUsage { get; set; }
        [FlatBufferItem(2)] public uint vertexElementLayer { get; set; }
        [FlatBufferItem(3)] public TRVertexFormat vertexFormat { get; set; }
        [FlatBufferItem(4)] public uint vertexElementOffset { get; set; }
    }

    [FlatBufferTable]
    public class TRVertexElementSize
    {
        [FlatBufferItem(0)] public uint elementSize { get; set; }
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
        [FlatBufferItem(0)] public uint indexCount { get; set; }
        [FlatBufferItem(1)] public uint indexOffset { get; set; }
        [FlatBufferItem(2)] public uint Field_02 { get; set; }
        [FlatBufferItem(3)] public string MaterialName { get; set; }
        [FlatBufferItem(4, DefaultValue = -1)] public int vertexDeclarationIndex { get; set; } = -1;
    }

    [FlatBufferTable]
    public class TRBoneWeight
    {
        [FlatBufferItem(0)] public uint RigIndex { get; set; }
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
        [FlatBufferItem(5)] public uint Field_05 { get; set; }
        [FlatBufferItem(6)] public uint Field_06 { get; set; }
        [FlatBufferItem(7)] public uint Field_07 { get; set; }
        [FlatBufferItem(8)] public uint Field_08 { get; set; }
        [FlatBufferItem(9)] public Sphere clipSphere { get; set; }
        [FlatBufferItem(10)] public TRBoneWeight[] boneWeight { get; set; }
        [FlatBufferItem(11)] public TRVisibilityShape[] VisibilityShapes { get; set; }
        [FlatBufferItem(12)] public string Field_12 { get; set; }
        [FlatBufferItem(13)] public uint Field_13 { get; set; }
        [FlatBufferItem(14)] public TRMorphShape[] MorphShapes { get; set; }
    }

    [FlatBufferTable]
    public partial class TRMSH
    {
        [FlatBufferItem(0)] public uint Version { get; set; }
        [FlatBufferItem(1)] public TRMesh[] Meshes { get; set; }
        [FlatBufferItem(2)] public string bufferFilePath { get; set; }

    }
}
