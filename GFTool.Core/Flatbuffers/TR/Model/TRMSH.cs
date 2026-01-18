using System;
using FlatSharp.Attributes;
using Trinity.Core.Flatbuffers.Utils;

namespace Trinity.Core.Flatbuffers.TR.Model
{
    // TRMSH (.trmsh) schema aligned to PokeDocs (SV/LA) naming and observed SV/ZA binaries.
    // Note: some PokeDocs variants include extra fields (visibility shapes, morph shapes). Those are not
    // present in the SV/ZA samples we use, so this binding models the common subset used by the tool.

    [FlatBufferEnum(typeof(int))]
    public enum TRIndexFormat
    {
        BYTE = 0,
        SHORT,
        INT,
        UINT64,
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
        USER,
        USER_ID,
    }

    [FlatBufferEnum(typeof(int))]
    public enum TRVertexFormat
    {
        NONE = 0,
        R8_G8_B8_A8_UNSIGNED_NORMALIZED = 20,
        W8_X8_Y8_Z8_UNSIGNED = 22,
        R32_UNSIGNED = 36,
        R32_SIGNED = 37,
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
        // `slot:int32 = -1` in PokeDocs; important: 0 is not the default, so preserve explicit 0 when writing.
        [FlatBufferItem(0, DefaultValue = -1)] public int Slot { get; set; } = -1;
        [FlatBufferItem(1)] public TRVertexUsage Usage { get; set; }
        [FlatBufferItem(2)] public int Layer { get; set; }
        [FlatBufferItem(3)] public TRVertexFormat Format { get; set; }
        [FlatBufferItem(4)] public int Offset { get; set; }
    }

    [FlatBufferTable]
    public class TRVertexElementSize
    {
        [FlatBufferItem(0)] public int Size { get; set; }
    }

    [FlatBufferTable]
    public class TRVertexDeclaration
    {
        [FlatBufferItem(0)] public TRVertexElement[] vertexElements { get; set; } = Array.Empty<TRVertexElement>();
        [FlatBufferItem(1)] public TRVertexElementSize[] vertexElementSizes { get; set; } = Array.Empty<TRVertexElementSize>();
    }

    [FlatBufferTable]
    public partial class TRMeshPart
    {
        [FlatBufferItem(0)] public int indexCount { get; set; }
        [FlatBufferItem(1)] public int indexOffset { get; set; }
        [FlatBufferItem(2)] public int unk3 { get; set; }
        [FlatBufferItem(3)] public string MaterialName { get; set; } = string.Empty;
        // `unk4:int32 = -1` in PokeDocs; important: 0 is not the default, so preserve explicit 0 when writing.
        [FlatBufferItem(4, DefaultValue = -1)] public int vertexDeclarationIndex { get; set; } = -1;
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
        [FlatBufferItem(0)] public string Name { get; set; } = string.Empty;
        [FlatBufferItem(1)] public TRBoundingBox boundingBox { get; set; } = new TRBoundingBox();
        [FlatBufferItem(2)] public TRIndexFormat IndexType { get; set; }
        [FlatBufferItem(3)] public TRVertexDeclaration[] vertexDeclaration { get; set; } = Array.Empty<TRVertexDeclaration>();
        [FlatBufferItem(4)] public TRMeshPart[] meshParts { get; set; } = Array.Empty<TRMeshPart>();
        [FlatBufferItem(5)] public int res0 { get; set; }
        [FlatBufferItem(6)] public int res1 { get; set; }
        [FlatBufferItem(7)] public int res2 { get; set; }
        [FlatBufferItem(8)] public int res3 { get; set; }
        [FlatBufferItem(9)] public Sphere clipSphere { get; set; } = new Sphere();
        [FlatBufferItem(10)] public TRBoneWeight[] boneWeight { get; set; } = Array.Empty<TRBoneWeight>();
        [FlatBufferItem(11)] public string MeshUnk7 { get; set; } = string.Empty;
        [FlatBufferItem(12)] public string MeshName { get; set; } = string.Empty;
    }

    [FlatBufferTable]
    public partial class TRMSH
    {
        [FlatBufferItem(0)] public int Version { get; set; }
        [FlatBufferItem(1)] public TRMesh[] Meshes { get; set; } = Array.Empty<TRMesh>();
        [FlatBufferItem(2)] public string bufferFilePath { get; set; } = string.Empty;

    }
}
