using FlatSharp.Attributes;
using Trinity.Core.Flatbuffers.Utils;

namespace Trinity.Core.Flatbuffers.TR.Model
{
    [FlatBufferEnum(typeof(uint))]
    public enum UVWrapMode : uint
    {
        WRAP = 0,
        CLAMP = 1,
        MIRROR = 6,
        MIRROR_ONCE = 7
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
    public class TRSampler
    {
        [FlatBufferItem(0)] public uint State0 { get; set; } = 0;
        [FlatBufferItem(1)] public uint State1 { get; set; } = 0;
        [FlatBufferItem(2)] public uint State2 { get; set; } = 0;
        [FlatBufferItem(3)] public uint State3 { get; set; } = 0;
        [FlatBufferItem(4)] public uint State4 { get; set; } = 0;
        [FlatBufferItem(5)] public uint State5 { get; set; } = 0;
        [FlatBufferItem(6)] public uint State6 { get; set; } = 0;
        [FlatBufferItem(7)] public uint State7 { get; set; } = 0;
        [FlatBufferItem(8)] public uint State8 { get; set; } = 0;
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
        [FlatBufferItem(5)] public TRVec2fParameter[] Vec2fParams { get; set; }
        [FlatBufferItem(6)] public TRVec3fParameter[] Vec3fParams { get; set; }
        [FlatBufferItem(7)] public TRVec4fParameter[] Vec4fParams { get; set; }
    }

    [FlatBufferTable]
    public class TRMTR
    {
        [FlatBufferItem(0)] public int Field_00 { get; set; }
        [FlatBufferItem(1)] public TRMaterial[] Materials { get; set; }
    }
}
