using FlatSharp.Attributes;
using Trinity.Core.Flatbuffers.Utils;

namespace Trinity.Core.Flatbuffers.Gfx2
{
    [FlatBufferTable]
    public class IntParam
    {
        [FlatBufferItem(0)] public string Name { get; set; }
        [FlatBufferItem(1)] public int Value { get; set; }
    }

    [FlatBufferTable]
    public class FloatParam
    {
        [FlatBufferItem(0)] public string Name { get; set; }
        [FlatBufferItem(1)] public float Value { get; set; }
    }

    [FlatBufferTable]
    public class Vector2fParam
    {
        [FlatBufferItem(0)] public string Name { get; set; }
        [FlatBufferItem(1)] public Vector2f Value { get; set; }
    }

    [FlatBufferTable]
    public class Vector3fParam
    {
        [FlatBufferItem(0)] public string Name { get; set; }
        [FlatBufferItem(1)] public Vector3f Value { get; set; }
    }

    [FlatBufferTable]
    public class Vector4fParam
    {
        [FlatBufferItem(0)] public string Name { get; set; }
        [FlatBufferItem(1)] public Vector4f Value { get; set; }
    }

    [FlatBufferTable]
    public class TextureParam
    {
        [FlatBufferItem(0)] public string Name { get; set; }
        [FlatBufferItem(1)] public string FilePath { get; set; }
        [FlatBufferItem(2)] public int SamplerId { get; set; }
    }

    [FlatBufferTable]
    public class ShaderOption
    {
        [FlatBufferItem(0)] public string Name { get; set; }
        [FlatBufferItem(1)] public string Choice { get; set; }
    }

    [FlatBufferTable]
    public class Technique
    {
        [FlatBufferItem(0)] public string Name { get; set; }
        [FlatBufferItem(1)] public ShaderOption[] ShaderOptions { get; set; }
    }

    [FlatBufferTable]
    public class MaterialItem
    {
        [FlatBufferItem(0)] public string Name { get; set; }
        [FlatBufferItem(1)] public Technique[] TechniqueList { get; set; }
        [FlatBufferItem(2)] public TextureParam[] TextureParamList { get; set; }
        [FlatBufferItem(4)] public FloatParam[] FloatParamList { get; set; }
        [FlatBufferItem(5)] public Vector2fParam[] Vector2fParamList { get; set; }
        [FlatBufferItem(6)] public Vector3fParam[] Vector3fParamList { get; set; }
        [FlatBufferItem(7)] public Vector4fParam[] Vector4fParamList { get; set; }
        [FlatBufferItem(9)] public IntParam[] IntParamList { get; set; }
    }

    [FlatBufferTable]
    public class Material
    {
        [FlatBufferItem(0)] public uint Version { get; set; }
        [FlatBufferItem(1)] public MaterialItem[] ItemList { get; set; }
    }
}
