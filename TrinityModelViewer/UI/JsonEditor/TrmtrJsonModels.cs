using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TrinityModelViewer
{
    internal sealed class TrmtrJsonDocument
    {
        [JsonPropertyName("version")] public uint Version { get; set; }
        [JsonPropertyName("items")] public List<TrmtrJsonMaterialItem> Items { get; set; } = new List<TrmtrJsonMaterialItem>();
    }

    internal sealed class TrmtrJsonMaterialItem
    {
        [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
        [JsonPropertyName("techniques")] public List<TrmtrJsonTechnique> Techniques { get; set; } = new List<TrmtrJsonTechnique>();
        [JsonPropertyName("textures")] public List<TrmtrJsonTextureParam> Textures { get; set; } = new List<TrmtrJsonTextureParam>();
        [JsonPropertyName("floats")] public List<TrmtrJsonFloatParam> Floats { get; set; } = new List<TrmtrJsonFloatParam>();
        [JsonPropertyName("ints")] public List<TrmtrJsonIntParam> Ints { get; set; } = new List<TrmtrJsonIntParam>();
        [JsonPropertyName("vec2")] public List<TrmtrJsonVec2Param> Vec2 { get; set; } = new List<TrmtrJsonVec2Param>();
        [JsonPropertyName("vec3")] public List<TrmtrJsonVec3Param> Vec3 { get; set; } = new List<TrmtrJsonVec3Param>();
        [JsonPropertyName("vec4")] public List<TrmtrJsonVec4Param> Vec4 { get; set; } = new List<TrmtrJsonVec4Param>();
    }

    internal sealed class TrmtrJsonTechnique
    {
        [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
        [JsonPropertyName("shaderOptions")] public List<TrmtrJsonShaderOption> ShaderOptions { get; set; } = new List<TrmtrJsonShaderOption>();
    }

    internal sealed class TrmtrJsonShaderOption
    {
        [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
        [JsonPropertyName("choice")] public string Choice { get; set; } = string.Empty;
    }

    internal sealed class TrmtrJsonTextureParam
    {
        [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
        [JsonPropertyName("filePath")] public string FilePath { get; set; } = string.Empty;
        [JsonPropertyName("samplerId")] public int SamplerId { get; set; }
    }

    internal sealed class TrmtrJsonFloatParam
    {
        [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
        [JsonPropertyName("value")] public float Value { get; set; }
    }

    internal sealed class TrmtrJsonIntParam
    {
        [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
        [JsonPropertyName("value")] public int Value { get; set; }
    }

    internal sealed class TrmtrJsonVec2Param
    {
        [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
        [JsonPropertyName("value")] public TrmtrJsonVec2 Value { get; set; } = new TrmtrJsonVec2();
    }

    internal sealed class TrmtrJsonVec3Param
    {
        [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
        [JsonPropertyName("value")] public TrmtrJsonVec3 Value { get; set; } = new TrmtrJsonVec3();
    }

    internal sealed class TrmtrJsonVec4Param
    {
        [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
        [JsonPropertyName("value")] public TrmtrJsonVec4 Value { get; set; } = new TrmtrJsonVec4();
    }

    // Shader-order vectors (x,y,z,w).
    internal sealed class TrmtrJsonVec2
    {
        [JsonPropertyName("x")] public float X { get; set; }
        [JsonPropertyName("y")] public float Y { get; set; }
    }

    internal sealed class TrmtrJsonVec3
    {
        [JsonPropertyName("x")] public float X { get; set; }
        [JsonPropertyName("y")] public float Y { get; set; }
        [JsonPropertyName("z")] public float Z { get; set; }
    }

    internal sealed class TrmtrJsonVec4
    {
        [JsonPropertyName("x")] public float X { get; set; }
        [JsonPropertyName("y")] public float Y { get; set; }
        [JsonPropertyName("z")] public float Z { get; set; }
        [JsonPropertyName("w")] public float W { get; set; }
    }
}
