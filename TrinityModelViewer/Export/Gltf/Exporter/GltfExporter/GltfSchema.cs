using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TrinityModelViewer.Export
{
    internal static partial class GltfExporter
    {
        private sealed class GltfRoot
        {
            [JsonPropertyName("asset")] public GltfAsset Asset { get; set; } = null!;
            [JsonPropertyName("scene")] public int Scene { get; set; }
            [JsonPropertyName("scenes")] public List<GltfScene> Scenes { get; set; } = new List<GltfScene>();
            [JsonPropertyName("nodes")] public List<GltfNode> Nodes { get; set; } = new List<GltfNode>();
            [JsonPropertyName("meshes")] public List<GltfMesh> Meshes { get; set; } = new List<GltfMesh>();
            [JsonPropertyName("materials")] public List<GltfMaterial> Materials { get; set; } = new List<GltfMaterial>();
            [JsonPropertyName("accessors")] public List<GltfAccessor> Accessors { get; set; } = new List<GltfAccessor>();
            [JsonPropertyName("bufferViews")] public List<GltfBufferView> BufferViews { get; set; } = new List<GltfBufferView>();
            [JsonPropertyName("buffers")] public List<GltfBuffer> Buffers { get; set; } = new List<GltfBuffer>();
            [JsonPropertyName("images")] public List<GltfImage> Images { get; set; } = new List<GltfImage>();
            [JsonPropertyName("textures")] public List<GltfTexture> Textures { get; set; } = new List<GltfTexture>();
            [JsonPropertyName("samplers")] public List<GltfSampler> Samplers { get; set; } = new List<GltfSampler>();
            [JsonPropertyName("skins")] public List<GltfSkin> Skins { get; set; } = new List<GltfSkin>();
            [JsonPropertyName("animations")] public List<GltfAnimation> Animations { get; set; } = new List<GltfAnimation>();
        }

        private sealed class GltfAsset
        {
            [JsonPropertyName("version")] public string Version { get; set; } = "2.0";
            [JsonPropertyName("generator")] public string? Generator { get; set; }
        }

        private sealed class GltfScene
        {
            [JsonPropertyName("nodes")] public List<int> Nodes { get; set; } = new List<int>();
        }

        private sealed class GltfNode
        {
            [JsonPropertyName("name")] public string? Name { get; set; }
            [JsonPropertyName("children")] public List<int>? Children { get; set; }
            [JsonPropertyName("mesh")] public int? Mesh { get; set; }
            [JsonPropertyName("skin")] public int? Skin { get; set; }
            [JsonPropertyName("translation")] public float[]? Translation { get; set; }
            [JsonPropertyName("rotation")] public float[]? Rotation { get; set; }
            [JsonPropertyName("scale")] public float[]? Scale { get; set; }
        }

        private sealed class GltfMesh
        {
            [JsonPropertyName("name")] public string? Name { get; set; }
            [JsonPropertyName("primitives")] public List<GltfPrimitive> Primitives { get; set; } = new List<GltfPrimitive>();
        }

        private sealed class GltfPrimitive
        {
            [JsonPropertyName("attributes")] public Dictionary<string, int> Attributes { get; set; } = new Dictionary<string, int>();
            [JsonPropertyName("indices")] public int? Indices { get; set; }
            [JsonPropertyName("material")] public int? Material { get; set; }
            [JsonPropertyName("mode")] public int Mode { get; set; } = 4; // TRIANGLES
            [JsonPropertyName("extras")] public Dictionary<string, object>? Extras { get; set; }
        }

        private sealed class GltfBuffer
        {
            [JsonPropertyName("uri")] public string Uri { get; set; } = string.Empty;
            [JsonPropertyName("byteLength")] public int ByteLength { get; set; }
        }

        private sealed class GltfBufferView
        {
            [JsonPropertyName("buffer")] public int Buffer { get; set; }
            [JsonPropertyName("byteOffset")] public int ByteOffset { get; set; }
            [JsonPropertyName("byteLength")] public int ByteLength { get; set; }
            [JsonPropertyName("target")] public int? Target { get; set; }
        }

        private sealed class GltfAccessor
        {
            [JsonPropertyName("bufferView")] public int BufferView { get; set; }
            [JsonPropertyName("byteOffset")] public int ByteOffset { get; set; }
            [JsonPropertyName("componentType")] public int ComponentType { get; set; }
            [JsonPropertyName("count")] public int Count { get; set; }
            [JsonPropertyName("type")] public string Type { get; set; } = string.Empty;
            [JsonPropertyName("min")] public float[]? Min { get; set; }
            [JsonPropertyName("max")] public float[]? Max { get; set; }
        }

        private sealed class GltfMaterial
        {
            [JsonPropertyName("name")] public string? Name { get; set; }
            [JsonPropertyName("pbrMetallicRoughness")] public GltfPbrMetallicRoughness? PbrMetallicRoughness { get; set; }
            [JsonPropertyName("normalTexture")] public GltfNormalTextureInfo? NormalTexture { get; set; }
            [JsonPropertyName("occlusionTexture")] public GltfOcclusionTextureInfo? OcclusionTexture { get; set; }
            [JsonPropertyName("alphaMode")] public string? AlphaMode { get; set; }
            [JsonPropertyName("doubleSided")] public bool? DoubleSided { get; set; }
        }

        private sealed class GltfPbrMetallicRoughness
        {
            [JsonPropertyName("baseColorFactor")] public float[]? BaseColorFactor { get; set; }
            [JsonPropertyName("baseColorTexture")] public GltfTextureInfo? BaseColorTexture { get; set; }
            [JsonPropertyName("metallicFactor")] public float MetallicFactor { get; set; }
            [JsonPropertyName("roughnessFactor")] public float RoughnessFactor { get; set; }
            [JsonPropertyName("metallicRoughnessTexture")] public GltfTextureInfo? MetallicRoughnessTexture { get; set; }
        }

        private class GltfTextureInfo
        {
            [JsonPropertyName("index")] public int Index { get; set; }
            [JsonPropertyName("texCoord")] public int? TexCoord { get; set; }
        }

        private sealed class GltfNormalTextureInfo : GltfTextureInfo
        {
            [JsonPropertyName("scale")] public float? Scale { get; set; }
        }

        private sealed class GltfOcclusionTextureInfo : GltfTextureInfo
        {
            [JsonPropertyName("strength")] public float? Strength { get; set; }
        }

        private sealed class GltfImage
        {
            [JsonPropertyName("uri")] public string Uri { get; set; } = string.Empty;
        }

        private sealed class GltfTexture
        {
            [JsonPropertyName("name")] public string? Name { get; set; }
            [JsonPropertyName("sampler")] public int? Sampler { get; set; }
            [JsonPropertyName("source")] public int Source { get; set; }
        }

        private sealed class GltfSampler
        {
            [JsonPropertyName("magFilter")] public int? MagFilter { get; set; }
            [JsonPropertyName("minFilter")] public int? MinFilter { get; set; }
            [JsonPropertyName("wrapS")] public int? WrapS { get; set; }
            [JsonPropertyName("wrapT")] public int? WrapT { get; set; }
        }

        private sealed class GltfSkin
        {
            [JsonPropertyName("inverseBindMatrices")] public int? InverseBindMatrices { get; set; }
            [JsonPropertyName("joints")] public List<int> Joints { get; set; } = new List<int>();
            [JsonPropertyName("skeleton")] public int? Skeleton { get; set; }
        }

        private sealed class GltfAnimation
        {
            [JsonPropertyName("name")] public string? Name { get; set; }
            [JsonPropertyName("samplers")] public List<GltfAnimationSampler> Samplers { get; set; } = new List<GltfAnimationSampler>();
            [JsonPropertyName("channels")] public List<GltfAnimationChannel> Channels { get; set; } = new List<GltfAnimationChannel>();
        }

        private sealed class GltfAnimationSampler
        {
            [JsonPropertyName("input")] public int Input { get; set; }
            [JsonPropertyName("output")] public int Output { get; set; }
            [JsonPropertyName("interpolation")] public string? Interpolation { get; set; }
        }

        private sealed class GltfAnimationChannel
        {
            [JsonPropertyName("sampler")] public int Sampler { get; set; }
            [JsonPropertyName("target")] public GltfAnimationChannelTarget Target { get; set; } = null!;
        }

        private sealed class GltfAnimationChannelTarget
        {
            [JsonPropertyName("node")] public int Node { get; set; }
            [JsonPropertyName("path")] public string Path { get; set; } = string.Empty;
        }
    }
}
