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

        public Material(PathString modelPath, TRMaterial trmat)
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
            shader = ShaderPool.Instance.GetShader(shaderKey);
            if (shader == null && !string.Equals(shaderKey, "Standard", StringComparison.OrdinalIgnoreCase))
            {
                shader = ShaderPool.Instance.GetShader("Standard");
            }
            isTransparent = Name.Contains("eye_lens", StringComparison.OrdinalIgnoreCase);

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
            var usedSlots = new HashSet<int>();
            var textureNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int nextSlot = 0;
            for (int i = 0; i < textures.Count; i++)
            {
                textures[i].EnsureLoaded();
                textureNames.Add(textures[i].Name);
                int slot = (int)textures[i].Slot;
                if (slot < 0 || slot > 31 || usedSlots.Contains(slot))
                {
                    while (usedSlots.Contains(nextSlot) && nextSlot < 32) nextSlot++;
                    slot = Math.Min(nextSlot, 31);
                }
                usedSlots.Add(slot);

                GL.ActiveTexture(TextureUnit.Texture0 + slot);
                GL.BindTexture(TextureTarget.Texture2D, textures[i].textureId);
                activeShader.SetInt(textures[i].Name, slot);
            }

            ApplyShaderParams(activeShader);
            SetTextureFlags(activeShader, textureNames);
            activeShader.SetBool("EnableVertexColor", RenderOptions.EnableVertexColors && hasVertexColors);
            activeShader.SetBool("HasTangents", hasTangents);
            activeShader.SetBool("HasBinormals", hasBinormals);
            activeShader.SetBool("FlipNormalY", RenderOptions.FlipNormalY);
            activeShader.SetBool("ReconstructNormalZ", RenderOptions.ReconstructNormalZ);
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

            if (RenderOptions.TransparentPass && isTransparent)
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

                if (string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(value, "false", StringComparison.OrdinalIgnoreCase))
                {
                    activeShader.SetBoolIfExists(name, string.Equals(value, "true", StringComparison.OrdinalIgnoreCase));
                    continue;
                }

                if (int.TryParse(value, out int intValue))
                {
                    activeShader.SetIntIfExists(name, intValue);
                    continue;
                }

                if (float.TryParse(value, out float floatValue))
                {
                    activeShader.SetFloatIfExists(name, floatValue);
                }
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

            foreach (var param in vec4Params)
            {
                activeShader.SetVector4IfExists(param.Name, new Vector4(param.Value.X, param.Value.Y, param.Value.Z, param.Value.W));
            }
        }

        private static string ResolveShaderName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return "Standard";
            }

            return name switch
            {
                "Opaque" => "Standard",
                "Transparent" => "Transparent",
                "Hair" => "Hair",
                "SSS" => "SSS",
                "EyeClearCoat" => "EyeClearCoat",
                "Unlit" => "Unlit",
                // TODO Make more shaders.
                _ => name
            };
        }

        private void SetTextureFlags(Shader activeShader, HashSet<string> textureNames)
        {
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
