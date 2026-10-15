using GFTool.Core.Utils;
using GFTool.Renderer.Core;
using GFTool.Renderer.Core.Graphics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using Trinity.Core.Flatbuffers.TR.Model;

namespace GFTool.Renderer.Scene.GraphicsObjects
{
    public class Material : IDisposable
    {
        public string Name { get; set; }
        public IReadOnlyList<Texture> Textures => textures;

        private Shader shader;
        private List<Texture> textures;
        private readonly string shaderKey;
        private readonly bool isTransparent;

        private PathString modelpath;

        private List<(string Name, string Value)> ShaderParams;
        private TRFloatParameter[] floatParams;
        private TRVec2fParameter[] vec2Params;
        private TRVec3fParameter[] vec3Params;
        private TRVec4fParameter[] vec4Params;
        private TRSampler[] samplers;
        private readonly TRIntParameter[] intParams;
        private readonly TRVec4fParameter[] lightParams;
        private readonly Dictionary<int, int> glSamplers = new();
        private static readonly System.Diagnostics.Stopwatch clock = System.Diagnostics.Stopwatch.StartNew();

        public Material(PathString modelPath, TRMaterial trmat)
        {
            Name = trmat.Name ?? "Material";
            modelpath = modelPath;

            ShaderParams = new List<(string Name, string Value)>();
            floatParams = trmat.FloatParams ?? Array.Empty<TRFloatParameter>();
            vec2Params = trmat.Vec2fParams ?? Array.Empty<TRVec2fParameter>();
            vec3Params = trmat.Vec3fParams ?? Array.Empty<TRVec3fParameter>();
            vec4Params = trmat.Vec4fParams ?? Array.Empty<TRVec4fParameter>();
            samplers = trmat.Samplers ?? Array.Empty<TRSampler>();
            intParams = trmat.IntParams ?? Array.Empty<TRIntParameter>();
            lightParams = trmat.LightParams ?? Array.Empty<TRVec4fParameter>();
            textures = new List<Texture>();

            //I hope we dont actually have more than one shader per material
            var shaderName = trmat.Shader?.Length > 0 ? trmat.Shader[0].Name : string.Empty;
            shaderKey = MaterialShaderCatalog.Resolve(shaderName);
            shader = ShaderPool.Instance.GetShader(shaderKey);
            if (shader == null && !string.Equals(shaderKey, "Standard", StringComparison.OrdinalIgnoreCase))
            {
                shader = ShaderPool.Instance.GetShader("Standard");
            }
            isTransparent = MaterialShaderCatalog.IsTransparent(shaderKey, trmat.AlphaType, Name);

            if (trmat.Shader != null && trmat.Shader.Length > 0 && trmat.Shader[0].Values != null)
            {
                foreach (var param in trmat.Shader[0].Values)
                {
                    ShaderParams.Add((param.Name, param.Value));
                }
            }

            foreach (var tex in trmat.Textures ?? Array.Empty<TRTexture>())
            {
                textures.Add(new Texture(modelPath, tex));
            }
        }

        public void Dispose()
        {
            foreach (var tex in textures)
                tex.Dispose();
            foreach (var sampler in glSamplers.Values) GL.DeleteSampler(sampler);
            glSamplers.Clear();
        }

        public bool IsTransparent => isTransparent;
        public IReadOnlyList<(string Name, string Value)> ShaderParameters => ShaderParams;
        public IReadOnlyList<TRFloatParameter> FloatParameters => floatParams;
        public IReadOnlyList<TRVec2fParameter> Vec2Parameters => vec2Params;
        public IReadOnlyList<TRVec3fParameter> Vec3Parameters => vec3Params;
        public IReadOnlyList<TRVec4fParameter> Vec4Parameters => vec4Params;
        public IReadOnlyList<TRSampler> Samplers => samplers;
        public string ShaderName => shaderKey;

        public void Use(Matrix4 view, Matrix4 model, Matrix4 proj, bool hasVertexColors, bool hasTangents, bool hasBinormals)
        {
            var activeShader = GetActiveShader();
            if (activeShader == null) return;

            activeShader.Bind();
            var textureNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            bool newMaterial = activeShader.MaterialOwner != this;
            if (newMaterial)
            {
                activeShader.ResetMaterialUniforms();
                activeShader.MaterialOwner = this;
            }
            BindTextures(activeShader, textureNames);
            SetTextureFlags(activeShader, textureNames);
            // Reset source defaults on material changes; apply every draw because
            // presence/debug flags must still honor explicit material options.
            ApplyShaderParams(activeShader);
            if (!RenderOptions.EnableNormalMaps)
                foreach (var name in textureNames.Where(n => n.StartsWith("NormalMap", StringComparison.OrdinalIgnoreCase))) activeShader.SetBoolIfExists("Enable" + name, false);
            activeShader.SetBoolIfExists("EnableVertexColor", RenderOptions.EnableVertexColors && hasVertexColors);
            activeShader.SetBoolIfExists("VertexColorAvailable", hasVertexColors);
            activeShader.SetBoolIfExists("HasTangents", hasTangents);
            activeShader.SetBoolIfExists("HasBinormals", hasBinormals);
            activeShader.SetBoolIfExists("FlipNormalY", RenderOptions.FlipNormalY);
            activeShader.SetBoolIfExists("ReconstructNormalZ", RenderOptions.ReconstructNormalZ);
            activeShader.SetBoolIfExists("ForwardPass", RenderOptions.TransparentPass);
            activeShader.SetFloatIfExists("TIME", (float)clock.Elapsed.TotalSeconds);
            SetLightingUniforms(activeShader, view);
            activeShader.SetMatrix4("model", model);
            activeShader.SetMatrix4("view", view);
            activeShader.SetMatrix4("projection", proj);
        }

        public void ApplySkinning(bool enabled, int boneCount, Matrix4[] matrices)
        {
            var activeShader = GetActiveShader();
            if (activeShader == null)
            {
                return;
            }

            activeShader.Bind();
            activeShader.SetBoolIfExists("EnableSkinning", enabled);
            activeShader.SetIntIfExists("BoneCount", enabled ? boneCount : 0);
            activeShader.SetBoolIfExists("SwapBlendOrder", RenderOptions.SwapBlendOrder);
            if (enabled)
            {
                activeShader.SetMatrix4ArrayIfExists("Bones", matrices, RenderOptions.TransposeSkinMatrices);
            }
        }

        private Shader GetActiveShader()
        {
            if (RenderOptions.LegacyMode)
            {
                return ShaderPool.Instance.GetShader("Standard") ?? shader;
            }

            if (RenderOptions.TransparentPass && isTransparent && shaderKey == "EyeClearCoat")
            {
                var forwardShader = ShaderPool.Instance.GetShader("EyeClearCoatForward");
                if (forwardShader != null)
                {
                    return forwardShader;
                }
            }

            return shader;
        }

        private void ApplyShaderParams(Shader activeShader)
        {
            foreach (var param in ShaderParams)
            {
                var name = param.Name;
                var value = param.Value;

                activeShader.SetOption(name, value);
            }

            foreach (var param in floatParams)
            {
                activeShader.SetFloatIfExists(param.Name, param.Value);
            }

            foreach (var param in vec2Params)
            {
                activeShader.SetVector2IfExists(param.Name, new Vector2(param.Value.X, param.Value.Y));
            }

            foreach (var param in vec3Params)
            {
                activeShader.SetVector3IfExists(param.Name, new Vector3(param.Value.X, param.Value.Y, param.Value.Z));
            }

            foreach (var param in lightParams.Concat(vec4Params))
            {
                activeShader.SetVector4IfExists(param.Name, new Vector4(param.Value.X, param.Value.Y, param.Value.Z, param.Value.W));
            }
            foreach (var param in intParams) activeShader.SetIntIfExists(param.Name, param.Value);
        }

        private void BindTextures(Shader activeShader, HashSet<string> textureNames)
        {
            foreach (var binding in activeShader.SamplerBindings)
            {
                string name = binding.Key;
                int unit = binding.Value;
                var texture = textures.FirstOrDefault(t => string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase));
                GL.ActiveTexture(TextureUnit.Texture0 + unit);
                if (texture != null)
                {
                    texture.EnsureLoaded();
                    textureNames.Add(name);
                    GL.BindTexture(TextureTarget.Texture2D, texture.textureId);
                    int samplerId = texture.Slot <= int.MaxValue ? (int)texture.Slot : -1;
                    if (!glSamplers.TryGetValue(samplerId, out int handle))
                    {
                        handle = GL.GenSampler(); glSamplers[samplerId] = handle;
                        var source = samplerId >= 0 && samplerId < samplers.Length ? samplers[samplerId] : null;
                        GL.SamplerParameter(handle, SamplerParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
                        GL.SamplerParameter(handle, SamplerParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
                        GL.SamplerParameter(handle, SamplerParameterName.TextureWrapS, (int)Wrap(source?.RepeatU ?? UVWrapMode.CLAMP_TO_EDGE));
                        GL.SamplerParameter(handle, SamplerParameterName.TextureWrapT, (int)Wrap(source?.RepeatV ?? UVWrapMode.CLAMP_TO_EDGE));
                        var color = source?.BorderColor;
                        GL.SamplerParameter(handle, SamplerParameterName.TextureBorderColor, color == null ? new float[4] : new[] { color.R,color.G,color.B,color.A });
                    }
                    GL.BindSampler(unit, handle);
                }
                else { GL.BindTexture(TextureTarget.Texture2D, 0); GL.BindSampler(unit, 0); }
                activeShader.SetIntIfExists(name, unit);
                var sampler = texture != null && texture.Slot < samplers.Length ? samplers[(int)texture.Slot] : null;
                activeShader.SetIntIfExists($"sampler_repeat_u[{unit}]", (int)(sampler?.RepeatU ?? UVWrapMode.CLAMP_TO_EDGE));
                activeShader.SetIntIfExists($"sampler_repeat_v[{unit}]", (int)(sampler?.RepeatV ?? UVWrapMode.CLAMP_TO_EDGE));
                var border = sampler?.BorderColor;
                activeShader.SetVector4IfExists($"sampler_border_color[{unit}]", border == null ? Vector4.Zero : new Vector4(border.R,border.G,border.B,border.A));
                activeShader.SetBoolIfExists($"sampler_present[{unit}]", texture != null);
                activeShader.SetVector4IfExists($"sampler_default[{unit}]", name.Contains("Normal", StringComparison.OrdinalIgnoreCase) ? new Vector4(0.5f,0.5f,1,1)
                    : name.Contains("LayerMask", StringComparison.OrdinalIgnoreCase) || name.Contains("Emission", StringComparison.OrdinalIgnoreCase) || name.Contains("Parallax", StringComparison.OrdinalIgnoreCase)
                    || name.Contains("Reflection", StringComparison.OrdinalIgnoreCase) || name.Contains("Highlight", StringComparison.OrdinalIgnoreCase) || name.Contains("Displacement", StringComparison.OrdinalIgnoreCase) ? Vector4.Zero : Vector4.One);
            }
        }

        private static TextureWrapMode Wrap(UVWrapMode mode) => mode switch
        {
            UVWrapMode.WRAP => TextureWrapMode.Repeat,
            UVWrapMode.MIRROR => TextureWrapMode.MirroredRepeat,
            UVWrapMode.CLAMP_TO_BORDER or UVWrapMode.MIRROR_CLAMP_TO_BORDER => TextureWrapMode.ClampToBorder,
            _ => TextureWrapMode.ClampToEdge
        };

        private void SetTextureFlags(Shader activeShader, HashSet<string> textureNames)
        {
            foreach (var name in textureNames) activeShader.SetBoolIfExists("Enable" + name, true);
            activeShader.SetBoolIfExists("EnableBaseColorMap", textureNames.Contains("BaseColorMap"));
            activeShader.SetBoolIfExists("EnableLayerMaskMap", textureNames.Contains("LayerMaskMap"));
            activeShader.SetBoolIfExists("EnableNormalMap", RenderOptions.EnableNormalMaps && textureNames.Contains("NormalMap"));
            activeShader.SetBoolIfExists("EnableNormalMap1", RenderOptions.EnableNormalMaps && textureNames.Contains("NormalMap1"));
            activeShader.SetBoolIfExists("EnableNormalMap2", RenderOptions.EnableNormalMaps && textureNames.Contains("NormalMap2"));
            activeShader.SetBoolIfExists("EnableRoughnessMap", textureNames.Contains("RoughnessMap"));
            activeShader.SetBoolIfExists("EnableRoughnessMap1", textureNames.Contains("RoughnessMap1"));
            activeShader.SetBoolIfExists("EnableRoughnessMap2", textureNames.Contains("RoughnessMap2"));
            activeShader.SetBoolIfExists("EnableMetallicMap", textureNames.Contains("MetallicMap"));
            activeShader.SetBoolIfExists("EnableAOMap", RenderOptions.EnableAO && textureNames.Contains("AOMap"));
            activeShader.SetBoolIfExists("EnableDetailMaskMap", textureNames.Contains("DetailMaskMap"));
            activeShader.SetBoolIfExists("EnableSSSMaskMap", textureNames.Contains("SSSMaskMap"));
            activeShader.SetBoolIfExists("EnableHairFlowMap", textureNames.Contains("HairFlowMap"));
        }

        private void SetLightingUniforms(Shader activeShader, Matrix4 view)
        {
            Matrix4.Invert(view, out var inverseView);
            var cameraPos = inverseView.ExtractTranslation();
            activeShader.SetVector3IfExists("CameraPos", cameraPos);

            var lightDirection = RenderOptions.WorldLightDirection;
            activeShader.SetVector3IfExists("LightDirection", lightDirection);
            activeShader.SetVector3IfExists("LightColor", new Vector3(0.95f, 0.95f, 0.95f));
            activeShader.SetVector3IfExists("AmbientColor", new Vector3(0.18f, 0.18f, 0.18f));
            activeShader.SetBoolIfExists("TwoSidedDiffuse", true);
            activeShader.SetFloatIfExists("LightWrap", RenderOptions.LightWrap);
            activeShader.SetFloatIfExists("SpecularScale", RenderOptions.SpecularScale);
            activeShader.SetFloatIfExists("LensOpacity", RenderOptions.LensOpacity);
            activeShader.SetBoolIfExists("LegacyMode", RenderOptions.LegacyMode);
        }
    }
}
