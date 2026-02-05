using GFTool.Core.Utils;
using GFTool.Renderer.Core;
using GFTool.Renderer.Core.Graphics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Globalization;
using System.Drawing;
using System.Linq;
using Trinity.Core.Flatbuffers.TR.Model;
using Trinity.Core.Flatbuffers.Utils;
using Trinity.Core.Assets;

namespace GFTool.Renderer.Scene.GraphicsObjects
{
    public partial class Material : IDisposable
    {
        private static readonly HashSet<string> warnedMissingSkinningUniforms = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> warnedMissingSamplerBindings = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> warnedMissingEyeClearCoatForward = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> loggedEyeClearCoatParams = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private enum TransparentBlendMode
        {
            Alpha,
            PremultipliedAlpha,
            Additive
        }

        private static TransparentBlendMode? lastTransparentBlendMode;

        public string Name { get; set; }
        public IReadOnlyList<Texture> Textures => textures;

        private Shader shader;
        private List<Texture> textures;
        private readonly string shaderKey;
        private readonly bool isTransparent;
        private readonly TransparentBlendMode transparentBlendMode;

        private PathString modelpath;

        private List<(string Name, string Value)> ShaderParams;
        private TRFloatParameter[] floatParams;
        private TRVec2fParameter[] vec2Params;
        private TRVec3fParameter[] vec3Params;
        private TRVec4fParameter[] vec4Params;
        private TRSampler[] samplers;

        private static readonly HashSet<string> reservedOverrideUniformNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "model",
            "view",
            "projection",
            "Bones",
            "BoneCount",
            "EnableSkinning",
            "SwapBlendOrder"
        };

        private readonly object overrideLock = new object();
        private readonly Dictionary<string, object> uniformOverrides = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        private bool colorTableCacheReady;
        private int colorTableDivideCached;
        private Vector3[]? colorTableBaseColorsCached;
        private Vector3[]? colorTableShadowColorsCached;

        public Material(PathString modelPath, TRMaterial trmat, IAssetProvider? assetProvider = null)
        {
            Name = trmat.Name;
            modelpath = modelPath;

            ShaderParams = new List<(string Name, string Value)>();
            floatParams = trmat.FloatParams ?? Array.Empty<TRFloatParameter>();
            vec2Params = trmat.Vec2fParams ?? Array.Empty<TRVec2fParameter>();
            vec3Params = trmat.Vec3fParams ?? Array.Empty<TRVec3fParameter>();
            vec4Params = trmat.Vec4fParams ?? Array.Empty<TRVec4fParameter>();
            samplers = trmat.Samplers ?? Array.Empty<TRSampler>();
            textures = new List<Texture>();

            //I hope we dont actually have more than one shader per material
            var shaderName = trmat.Shader?.Length > 0 ? trmat.Shader[0].Name : string.Empty;
            shaderKey = ResolveShaderName(shaderName);
            // Shader compilation/linking requires a current GL context. Defer acquisition until first use
            // (or explicit warmup) so materials can be created off the render thread.
            shader = null!;

            string? techniqueName = null;
            if (trmat.Shader != null && trmat.Shader.Length > 0 && trmat.Shader[0].Values != null)
            {
                foreach (var param in trmat.Shader[0].Values)
                {
                    if (param == null)
                    {
                        continue;
                    }
                    if (string.Equals(param.Name, "__TechniqueName", StringComparison.OrdinalIgnoreCase))
                    {
                        techniqueName = param.Value;
                        break;
                    }
                }
            }

            bool isTransparentByTechnique = !string.IsNullOrWhiteSpace(techniqueName) &&
                                            techniqueName.Contains("Transparent", StringComparison.OrdinalIgnoreCase);

            isTransparent =
                Name.Contains("eye_lens", StringComparison.OrdinalIgnoreCase) ||
                isTransparentByTechnique ||
                string.Equals(shaderKey, "Transparent", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(shaderKey, "EyeClearCoatForward", StringComparison.OrdinalIgnoreCase);

            transparentBlendMode = isTransparentByTechnique ? TransparentBlendMode.PremultipliedAlpha : TransparentBlendMode.Alpha;

            if (trmat.Shader != null && trmat.Shader.Length > 0 && trmat.Shader[0].Values != null)
            {
                foreach (var param in trmat.Shader[0].Values)
                {
                    ShaderParams.Add((param.Name, param.Value));
                }
            }

            var samplersBySlot = new Dictionary<uint, TRSampler>();
            if (trmat.Samplers != null)
            {
                for (int i = 0; i < trmat.Samplers.Length; i++)
                {
                    samplersBySlot[(uint)i] = trmat.Samplers[i];
                }
            }

            foreach (var tex in trmat.Textures ?? Array.Empty<TRTexture>())
            {
                if (!samplersBySlot.TryGetValue(tex.Slot, out var sampler) && MessageHandler.Instance.DebugLogsEnabled)
                {
                    var key = $"{modelpath}::{Name}::{tex.Name}::{tex.Slot}";
                    if (warnedMissingSamplerBindings.Add(key))
                    {
                        MessageHandler.Instance.AddMessage(
                            MessageType.WARNING,
                            $"[Sampler] Missing sampler for mat='{Name}' tex='{tex.Name}' SamplerId={tex.Slot} (defaults to ClampToEdge)");
                    }
                }
                textures.Add(new Texture(modelPath, tex, sampler, assetProvider));
            }

            TryApplyColorTableOverrides();
            if (MessageHandler.Instance.DebugLogsEnabled &&
                string.Equals(shaderKey, "IkCharacter", StringComparison.OrdinalIgnoreCase))
            {
                bool TryGetVec4(string name, out Vector4 value)
                {
                    for (int i = 0; i < vec4Params.Length; i++)
                    {
                        if (!string.Equals(vec4Params[i].Name, name, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        var v = vec4Params[i].Value;
                        value = new Vector4(v.W, v.X, v.Y, v.Z);
                        return true;
                    }

                    value = default;
                    return false;
                }

                bool hasUvScaleOffset = TryGetVec4("UVScaleOffset", out var uvScaleOffset);
                bool nonIdentityUv = hasUvScaleOffset &&
                                     (Math.Abs(uvScaleOffset.X - 1.0f) > 0.0001f ||
                                      Math.Abs(uvScaleOffset.Y - 1.0f) > 0.0001f ||
                                      Math.Abs(uvScaleOffset.Z) > 0.0001f ||
                                      Math.Abs(uvScaleOffset.W) > 0.0001f);

                const TextureWrapMode mirrorClampToEdge = (TextureWrapMode)0x8743;
                bool hasMirroredSampler = false;
                for (int i = 0; i < textures.Count; i++)
                {
                    var wrapS = textures[i].WrapS;
                    var wrapT = textures[i].WrapT;
                    if (wrapS == TextureWrapMode.MirroredRepeat || wrapT == TextureWrapMode.MirroredRepeat ||
                        wrapS == mirrorClampToEdge || wrapT == mirrorClampToEdge)
                    {
                        hasMirroredSampler = true;
                        break;
                    }
                }

                if (nonIdentityUv || hasMirroredSampler)
                {
                    var uvLabel = hasUvScaleOffset ? $"({uvScaleOffset.X:0.###},{uvScaleOffset.Y:0.###},{uvScaleOffset.Z:0.###},{uvScaleOffset.W:0.###})" : "(missing)";
                    var samplerLabel = string.Join(", ", textures.Select(t => $"{t.Name}[{t.WrapS}/{t.WrapT}]"));
                    MessageHandler.Instance.AddMessage(
                        MessageType.LOG,
                        $"[UV] IkCharacter mat='{Name}' UVScaleOffset={uvLabel} samplers={samplerLabel}");
                }
            }
        }

        public void Dispose()
        {
            foreach (var tex in textures)
                tex.Dispose();
        }

        public bool IsTransparent => isTransparent;
        public IReadOnlyList<(string Name, string Value)> ShaderParameters => ShaderParams;
        public IReadOnlyList<TRFloatParameter> FloatParameters => floatParams;
        public IReadOnlyList<TRVec2fParameter> Vec2Parameters => vec2Params;
        public IReadOnlyList<TRVec3fParameter> Vec3Parameters => vec3Params;
        public IReadOnlyList<TRVec4fParameter> Vec4Parameters => vec4Params;
        public IReadOnlyList<TRSampler> Samplers => samplers;
        public string ShaderName => shaderKey;

    }
}
