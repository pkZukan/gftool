using FlatSharp.Attributes;
using Trinity.Core.Flatbuffers.Utils;

namespace Trinity.Core.Flatbuffers.TR.Model
{
    [FlatBufferEnum(typeof(uint))]
    public enum UVWrapMode : uint
    {
        CLAMP = 0,
        WRAP = 1,
        MIRROR_CLAMP = 2,
        MIRROR_CLAMP_TO_EDGE = 3,
        MIRROR_CLAMP_TO_BORDER = 4,
        CLAMP_TO_BORDER = 5,
        MIRROR = 6,
        CLAMP_TO_EDGE = 7
    };

    [FlatBufferTable]
    public class TRFloatParameter
    {
        [FlatBufferItem(0)] public string Name { get; set; }
        [FlatBufferItem(1)] public float Value { get; set; }
    }

    [FlatBufferTable]
    public class TRVec2fParameter
    {
        [FlatBufferItem(0)] public string Name { get; set; }
        [FlatBufferItem(1)] public Vector2f Value { get; set; }
    }

    [FlatBufferTable]
    public class TRVec3fParameter
    {
        [FlatBufferItem(0)] public string Name { get; set; }
        [FlatBufferItem(1)] public Vector3f Value { get; set; }
    }

    [FlatBufferTable]
    public class TRVec4fParameter
    {
        [FlatBufferItem(0)] public string Name { get; set; }
        [FlatBufferItem(1)] public Vector4f Value { get; set; }
    }

    [FlatBufferTable]
    public class TRStringParameter
    {
        [FlatBufferItem(0)] public string Name { get; set; }
        [FlatBufferItem(1)] public string Value { get; set; }
    }

    [FlatBufferTable]
    public class TRIntParameter
    {
        [FlatBufferItem(0)] public string Name { get; set; }
        [FlatBufferItem(1, DefaultValue = -1)] public int Value { get; set; } = -1;
    }

    [FlatBufferTable]
    public class TRMaterialByteExtra
    {
        [FlatBufferItem(0, DefaultValue = (sbyte)-1)] public sbyte Value { get; set; } = -1;
    }

    [FlatBufferTable]
    public class TRMaterialIntExtra
    {
        [FlatBufferItem(0)] public uint Reserved { get; set; }
        [FlatBufferItem(1, DefaultValue = -1)] public int Value { get; set; } = -1;
    }

    [FlatBufferTable]
    public class TRSampler
    {
        [FlatBufferItem(0)] public uint State0 { get; set; } = 0;
        [FlatBufferItem(1)] public uint State1 { get; set; } = 0;
        [FlatBufferItem(2)] public uint State2 { get; set; } = 0;
        [FlatBufferItem(3)] public uint State3 { get; set; } = 0;
        [FlatBufferItem(4)] public float State4 { get; set; }
        [FlatBufferItem(5)] public float State5 { get; set; }
        [FlatBufferItem(6)] public float State6 { get; set; }
        [FlatBufferItem(7)] public float State7 { get; set; }
        [FlatBufferItem(8)] public float State8 { get; set; }
        [FlatBufferItem(9)] public UVWrapMode RepeatU { get; set; }
        [FlatBufferItem(10)] public UVWrapMode RepeatV { get; set; }
        [FlatBufferItem(11)] public UVWrapMode RepeatW { get; set; }
        [FlatBufferItem(12)] public RGBA BorderColor { get; set; }
    }

    [FlatBufferTable]
    public class TRTexture
    {
        [FlatBufferItem(0)] public string Name { get; set; }
        [FlatBufferItem(1)] public string File { get; set; }
        [FlatBufferItem(2)] public uint Slot { get; set; }
    }

    [FlatBufferTable]
    public class TRMaterialShader
    {
        [FlatBufferItem(0)] public string Name { get; set; }
        [FlatBufferItem(1)] public TRStringParameter[] Values { get; set; }
    }

    [FlatBufferTable]
    public class TRMaterial
    {
        [FlatBufferItem(0)] public string Name { get; set; }
        [FlatBufferItem(1)] public TRMaterialShader[] Shader { get; set; }
        [FlatBufferItem(2)] public TRTexture[] Textures { get; set; }
        [FlatBufferItem(3)] public TRSampler[] Samplers { get; set; }
        [FlatBufferItem(4)] public TRFloatParameter[] FloatParams { get; set; }
        [FlatBufferItem(5)] public string Field_05 { get; set; }
        // RGBA and Vector4f have the same four-float layout (R=X, G=Y, B=Z, A=W).
        [FlatBufferItem(6)] public TRVec4fParameter[] LightParams { get; set; }
        [FlatBufferItem(7)] public TRVec4fParameter[] Vec4fParams { get; set; }
        [FlatBufferItem(8)] public string Field_08 { get; set; }
        [FlatBufferItem(9)] public TRIntParameter[] IntParams { get; set; }
        [FlatBufferItem(10)] public string Field_10 { get; set; }
        [FlatBufferItem(11)] public string Field_11 { get; set; }
        [FlatBufferItem(12)] public string Field_12 { get; set; }
        [FlatBufferItem(13)] public TRMaterialByteExtra ByteExtra { get; set; }
        [FlatBufferItem(14)] public TRMaterialIntExtra IntExtra { get; set; }
        [FlatBufferItem(15)] public string AlphaType { get; set; }

        // Used by the Gfx2 renderer adapter; these are not TRMTR fields.
        public TRVec2fParameter[] Vec2fParams { get; set; }
        public TRVec3fParameter[] Vec3fParams { get; set; }
    }

    [FlatBufferTable]
    public class TRMTR
    {
        [FlatBufferItem(0)] public uint Field_00 { get; set; }
        [FlatBufferItem(1)] public TRMaterial[] Materials { get; set; }
    }
}
