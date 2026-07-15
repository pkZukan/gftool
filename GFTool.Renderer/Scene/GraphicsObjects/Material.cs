using GFTool.Core.Utils;
using GFTool.Renderer.Core;
using GFTool.Renderer.Core.Graphics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.IO;
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
        private readonly bool isThinRefraction;
        private readonly bool usesIkCharacterTechnique;
        private readonly bool usesTrinityVectorLayout;
        private readonly bool isPokemonMaterial;
        private readonly bool isSkinMaterial;
        private readonly bool enableEyeBaseSclera;
        private readonly Vector2 eyeUvClamp;
        private readonly int eyePointLightIndex;

        private PathString modelpath;

        private List<(string Name, string Value)> ShaderParams;
        private TRFloatParameter[] floatParams;
        private TRVec2fParameter[] vec2Params;
        private TRVec3fParameter[] vec3Params;
        private TRVec4fParameter[] vec4Params;
        private TRSampler[] samplers;
        private readonly Dictionary<string, Vector4> animatedVec4Params = new Dictionary<string, Vector4>(StringComparer.OrdinalIgnoreCase);
        private Material? skinToneSource;

        public Material(PathString modelPath, TRMaterial trmat, bool isPokemonMaterial)
        {
            Name = trmat.Name;
            modelpath = modelPath;
            this.isPokemonMaterial = isPokemonMaterial;

            ShaderParams = new List<(string Name, string Value)>();
            floatParams = trmat.FloatParams ?? Array.Empty<TRFloatParameter>();
            vec2Params = trmat.Vec2fParams ?? Array.Empty<TRVec2fParameter>();
            vec3Params = trmat.Vec3fParams ?? Array.Empty<TRVec3fParameter>();
            vec4Params = trmat.Vec4fParams ?? Array.Empty<TRVec4fParameter>();
            samplers = trmat.Samplers ?? Array.Empty<TRSampler>();
            textures = new List<Texture>();

            if (trmat.Shader != null && trmat.Shader.Length > 0 && trmat.Shader[0].Values != null)
            {
                foreach (var param in trmat.Shader[0].Values)
                {
                    ShaderParams.Add((param.Name, param.Value));
                }
            }

            //I hope we dont actually have more than one shader per material
            var shaderName = trmat.Shader?.Length > 0 ? trmat.Shader[0].Name : string.Empty;
            usesIkCharacterTechnique = IsLayeredCharacterTechnique(shaderName, trmat);
            // Vector4f is serialized as W, X, Y, Z for every IkCharacter material,
            // including materials without a meaningful layer mask (hair and skin tints).
            usesTrinityVectorLayout = shaderName.StartsWith("IkCharacter", StringComparison.OrdinalIgnoreCase) ||
                                      usesIkCharacterTechnique;
            isSkinMaterial = !isPokemonMaterial && (IsFaceSkin || IsBodySkin);
            shaderKey = ResolveShaderName(shaderName, trmat);
            shader = ShaderPool.Instance.GetShader(shaderKey);
            if (shader == null && !string.Equals(shaderKey, "Standard", StringComparison.OrdinalIgnoreCase))
            {
                shader = ShaderPool.Instance.GetShader(isPokemonMaterial ? "PokemonStandard" : "Standard");
            }
            isTransparent = isPokemonMaterial
                ? Name.Contains("eye_lens", StringComparison.OrdinalIgnoreCase)
                : string.Equals(shaderKey, "Transparent", StringComparison.OrdinalIgnoreCase) ||
                  string.Equals(shaderKey, "Fresnel", StringComparison.OrdinalIgnoreCase) ||
                  Name.Contains("eye_lens", StringComparison.OrdinalIgnoreCase) ||
                  string.Equals(shaderKey, "Fire", StringComparison.OrdinalIgnoreCase);
            isThinRefraction = string.Equals(shaderKey, "Transparent", StringComparison.OrdinalIgnoreCase) &&
                ShaderParams.Any(param => string.Equals(param.Name, "RefractionMode", StringComparison.OrdinalIgnoreCase) &&
                                          string.Equals(param.Value, "Thin", StringComparison.OrdinalIgnoreCase));

            foreach (var tex in trmat.Textures ?? Array.Empty<TRTexture>())
            {
                if (IsEnvironmentProbe(tex))
                {
                    DiagnosticLog.Write(
                        $"Environment probe skipped: material={Name}, name={tex.Name}, file={tex.File}, " +
                        "reason=current shaders have no samplerCube input");
                    continue;
                }
                textures.Add(new Texture(modelPath, tex));
            }

            eyeUvClamp = ResolveEyeUvClamp();
            eyePointLightIndex = ResolveEyePointLightIndex();

            if (string.Equals(shaderKey, "Fire", StringComparison.OrdinalIgnoreCase))
            {
                LogFireMaterialDebug();
            }

            AddAutoEyeMaskTexture(modelPath);
            enableEyeBaseSclera = !isPokemonMaterial && ShouldEnableEyeBaseSclera();
            LogMaterialSummary(shaderName);
            if (enableEyeBaseSclera)
            {
                DiagnosticLog.Write($"Eye base sclera inferred: material={Name}");
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
        public bool EnableEyeBaseSclera => enableEyeBaseSclera;
        public int EyePointLightIndex => eyePointLightIndex;

        private static bool IsEnvironmentProbe(TRTexture texture)
        {
            return texture != null &&
                (string.Equals(texture.Name, "LocalReflectionMap", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(texture.Name, "LocalSpecularProbe", StringComparison.OrdinalIgnoreCase));
        }

        public bool TryGetShaderVector4(string name, out Vector4 value)
        {
            var param = vec4Params.FirstOrDefault(p => p != null && string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
            if (param == null)
            {
                value = Vector4.Zero;
                return false;
            }

            value = ConvertVector4ForShader(param);
            return true;
        }

        public bool IsFaceSkin => Name.Contains("face_skin", StringComparison.OrdinalIgnoreCase);
        public bool IsBodySkin => Name.Contains("body_skin", StringComparison.OrdinalIgnoreCase);

        public void SetSkinToneSource(Material source)
        {
            if (!IsFaceSkin || source == null || !source.IsBodySkin)
            {
                return;
            }

            skinToneSource = source;
            if (source.TryGetEffectiveVector4("BaseColor", out var color))
            {
                DiagnosticLog.Write(
                    $"Skin tone harmonized: source={source.Name}, target={Name}, " +
                    $"rgba=({color.X}, {color.Y}, {color.Z}, {color.W})");
            }
        }

        public void SetAnimatedVector4(string name, Vector4 value)
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                animatedVec4Params[name] = value;
            }
        }

        public void ClearAnimationOverrides()
        {
            animatedVec4Params.Clear();
        }

        public void Use(Matrix4 view, Matrix4 model, Matrix4 proj, bool hasVertexColors, bool hasTangents, bool hasBinormals, bool hasUnitUvDomain, Vector3? eyePointLightPosition)
        {
            var activeShader = GetActiveShader();
            if (activeShader == null) return;

            activeShader.Bind();
            if (RenderOptions.TransparentPass)
            {
                GL.BlendFunc(
                    string.Equals(shaderKey, "Fire", StringComparison.OrdinalIgnoreCase) ? BlendingFactor.One : BlendingFactor.SrcAlpha,
                    BlendingFactor.OneMinusSrcAlpha);
            }
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
                ApplyTextureSampler(textures[i], hasUnitUvDomain);
                activeShader.SetIntIfExists(ResolveTextureUniformName(textures[i].Name), slot);
            }

            SetLayerParameterDefaults(activeShader);
            ApplyShaderParams(activeShader);
            activeShader.SetVector2IfExists("EyeUVClamp", hasUnitUvDomain ? eyeUvClamp : Vector2.Zero);
            activeShader.SetVector4IfExists("EyeHighlightColor", ResolveEyeHighlightColor());
            SetTextureFlags(activeShader, textureNames);
            var enableEyePointLight = eyePointLightPosition.HasValue &&
                IsEyeClearCoatShader() &&
                !textureNames.Contains("EyeMaskMap");
            activeShader.SetBoolIfExists("EnableEyePointLight", enableEyePointLight);
            if (enableEyePointLight)
            {
                activeShader.SetVector3IfExists("EyePointLightPosition", eyePointLightPosition!.Value);
            }
            activeShader.SetBoolIfExists("EnableEyeBaseSclera", enableEyeBaseSclera);
            activeShader.SetBoolIfExists(
                "ReplaceBaseColorWithLayers",
                !ResolveShaderBoolean("BaseColorMultiply", true));
            activeShader.SetBoolIfExists("ForceLayerPalette", !isPokemonMaterial && usesIkCharacterTechnique);
            activeShader.SetBoolIfExists(
                "UseTrinityMaterialUv",
                isPokemonMaterial ? usesTrinityVectorLayout : usesIkCharacterTechnique);
            activeShader.SetBoolIfExists("UseColorAtlasAoUv", !isPokemonMaterial && ShouldUseColorAtlasAoUv());
            var skinToneOverride = Vector4.One;
            var enableSkinToneOverride = !isPokemonMaterial &&
                skinToneSource != null &&
                skinToneSource.TryGetEffectiveVector4("BaseColor", out skinToneOverride);
            activeShader.SetBoolIfExists("EnableSkinToneOverride", enableSkinToneOverride);
            activeShader.SetVector4IfExists(
                "SkinToneOverride",
                enableSkinToneOverride ? skinToneOverride : Vector4.One);
            activeShader.SetBoolIfExists("IsSkinMaterial", isSkinMaterial);
            activeShader.SetBoolIfExists("EnableThinRefraction", !isPokemonMaterial && isThinRefraction);
            var forceVertexColor = string.Equals(shaderKey, "Fire", StringComparison.OrdinalIgnoreCase);
            activeShader.SetBoolIfExists("EnableVertexColor", (RenderOptions.EnableVertexColors || forceVertexColor) && hasVertexColors);
            activeShader.SetBoolIfExists("HasTangents", hasTangents);
            activeShader.SetBoolIfExists("HasBinormals", hasBinormals);
            activeShader.SetBoolIfExists("FlipNormalY", RenderOptions.FlipNormalY);
            activeShader.SetBoolIfExists("ReconstructNormalZ", RenderOptions.ReconstructNormalZ);
            SetLightingUniforms(activeShader, view);
            activeShader.SetMatrix4("model", model);
            activeShader.SetMatrix4("view", view);
            activeShader.SetMatrix4("projection", proj);
        }

        private void ApplyTextureSampler(Texture texture, bool hasUnitUvDomain)
        {
            var samplerIndex = (int)texture.Slot;
            var wrapS = TextureWrapMode.Repeat;
            var wrapT = TextureWrapMode.Repeat;
            if (samplerIndex >= 0 && samplerIndex < samplers.Length)
            {
                wrapS = ResolveTextureWrapMode(samplers[samplerIndex].RepeatU, hasUnitUvDomain);
                wrapT = ResolveTextureWrapMode(samplers[samplerIndex].RepeatV, hasUnitUvDomain);
            }

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)wrapS);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)wrapT);
        }

        private static TextureWrapMode ResolveTextureWrapMode(UVWrapMode wrapMode, bool hasUnitUvDomain)
        {
            // Some split body meshes intentionally use a second UV tile even
            // though their material sampler is marked Clamp. Clamping those
            // coordinates samples the texture border (often solid black).
            if (!hasUnitUvDomain && wrapMode == UVWrapMode.CLAMP)
            {
                return TextureWrapMode.Repeat;
            }

            return wrapMode switch
            {
                UVWrapMode.CLAMP => TextureWrapMode.ClampToEdge,
                UVWrapMode.MIRROR => TextureWrapMode.MirroredRepeat,
                UVWrapMode.MIRROR_ONCE => TextureWrapMode.ClampToEdge,
                _ => TextureWrapMode.Repeat
            };
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
            if (RenderOptions.TransparentPass &&
                isTransparent &&
                IsEyeClearCoatShader())
            {
                var forwardShader = ShaderPool.Instance.GetShader(
                    isPokemonMaterial ? "PokemonEyeClearCoatForward" : "EyeClearCoatForward");
                if (forwardShader != null)
                {
                    return forwardShader;
                }
            }

            if (RenderOptions.LegacyMode &&
                !IsEyeClearCoatShader() &&
                !string.Equals(shaderKey, "Fire", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(shaderKey, "Fresnel", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(shaderKey, "FresnelOpaque", StringComparison.OrdinalIgnoreCase))
            {
                return ShaderPool.Instance.GetShader(isPokemonMaterial ? "PokemonStandard" : "Standard") ?? shader;
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
                activeShader.SetVector4IfExists(param.Name, ConvertVector4ForShader(param));
            }

            foreach (var param in animatedVec4Params)
            {
                activeShader.SetVector4IfExists(param.Key, param.Value);
            }
        }

        private static void SetLayerParameterDefaults(Shader activeShader)
        {
            // Shader objects are shared by every material. Reset optional layer values so
            // a material that omits one cannot inherit it from the previously drawn mesh.
            activeShader.SetVector4IfExists("BaseColor", Vector4.One);
            activeShader.SetBoolIfExists("NumMaterialLayer", false);
            activeShader.SetBoolIfExists("BaseColorMultiply", false);
            activeShader.SetBoolIfExists("EnableLerpBaseColorEmission", false);
            activeShader.SetIntIfExists("ColorTableDivideNumber", 1);
            activeShader.SetIntIfExists("BaseColorIndex1", 1);
            activeShader.SetIntIfExists("BaseColorIndex2", 2);
            activeShader.SetIntIfExists("BaseColorIndex3", 3);
            activeShader.SetIntIfExists("BaseColorIndex4", 4);
            activeShader.SetFloatIfExists("LayerMaskScale1", 1.0f);
            activeShader.SetFloatIfExists("LayerMaskScale2", 1.0f);
            activeShader.SetFloatIfExists("LayerMaskScale3", 1.0f);
            activeShader.SetFloatIfExists("LayerMaskScale4", 1.0f);
        }

        private bool TryGetEffectiveVector4(string name, out Vector4 value)
        {
            if (animatedVec4Params.TryGetValue(name, out value))
            {
                return true;
            }

            return TryGetShaderVector4(name, out value);
        }

        private bool ResolveShaderBoolean(string name, bool defaultValue)
        {
            var value = ShaderParams
                .FirstOrDefault(param => string.Equals(param.Name, name, StringComparison.OrdinalIgnoreCase))
                .Value;
            if (bool.TryParse(value, out var boolValue))
            {
                return boolValue;
            }

            return int.TryParse(value, out var intValue) ? intValue != 0 : defaultValue;
        }

        private Vector4 ConvertVector4ForShader(TRVec4fParameter param)
        {
            if (usesTrinityVectorLayout ||
                string.Equals(shaderKey, "Fresnel", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(shaderKey, "FresnelOpaque", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(shaderKey, "Fire", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(shaderKey, "Unlit", StringComparison.OrdinalIgnoreCase) ||
                IsEyeClearCoatShader() ||
                string.Equals(shaderKey, "Transparent", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(shaderKey, "Iridescence", StringComparison.OrdinalIgnoreCase))
            {
                // Trinity's Vector4f struct is serialized in W, X, Y, Z order. These
                // shaders consume the four stored values as their X, Y, Z, W values.
                return new Vector4(param.Value.W, param.Value.X, param.Value.Y, param.Value.Z);
            }

            // Keep the established interpretation for other shaders until their
            // existing material reconstruction is migrated and re-verified.
            return new Vector4(param.Value.X, param.Value.Y, param.Value.Z, param.Value.W);
        }

        private Vector4 ResolveEyeHighlightColor()
        {
            var layer5 = vec4Params.FirstOrDefault(param => param != null &&
                string.Equals(param.Name, "EmissionColorLayer5", StringComparison.OrdinalIgnoreCase));
            return layer5 != null
                ? ConvertVector4ForShader(layer5)
                : new Vector4(1.0f, 0.97f, 1.0f, 1.0f);
        }

        private int ResolveEyePointLightIndex()
        {
            if (!IsEyeClearCoatShader())
            {
                return 0;
            }

            var value = ShaderParams
                .FirstOrDefault(param => string.Equals(param.Name, "PointLightIndex", StringComparison.OrdinalIgnoreCase))
                .Value;
            return int.TryParse(value, out var index) && index > 0 ? index : 0;
        }

        private Vector2 ResolveEyeUvClamp()
        {
            if (!IsEyeClearCoatShader())
            {
                return Vector2.Zero;
            }

            var uvTransformMode = ShaderParams
                .FirstOrDefault(param => string.Equals(param.Name, "UVTransformMode", StringComparison.OrdinalIgnoreCase))
                .Value;
            if (string.Equals(uvTransformMode, "T", StringComparison.OrdinalIgnoreCase))
            {
                return Vector2.Zero;
            }

            var textureSamplers = textures
                .Where(texture => texture.Slot < samplers.Length)
                .Select(texture => samplers[texture.Slot])
                .ToArray();
            if (textureSamplers.Length == 0)
            {
                return Vector2.Zero;
            }

            return new Vector2(
                textureSamplers.All(sampler => sampler.RepeatU == UVWrapMode.CLAMP) ? 1.0f : 0.0f,
                textureSamplers.All(sampler => sampler.RepeatV == UVWrapMode.CLAMP) ? 1.0f : 0.0f);
        }

        private string ResolveShaderName(string name, TRMaterial material)
        {
            if (string.Equals(material.Name, "fire", StringComparison.OrdinalIgnoreCase) ||
                IsLayeredUnlitEffect(name, material))
            {
                return "Fire";
            }

            if (string.IsNullOrEmpty(name))
            {
                return isPokemonMaterial ? "PokemonStandard" : "Standard";
            }

            if (isPokemonMaterial)
            {
                if (name.StartsWith("IkCharacter", StringComparison.OrdinalIgnoreCase))
                {
                    return material.Name.Contains("eye", StringComparison.OrdinalIgnoreCase)
                        ? "PokemonEyeClearCoat"
                        : "PokemonStandard";
                }

                return name switch
                {
                    "Opaque" => "PokemonStandard",
                    "Custom" => "PokemonStandard",
                    "Fabric" => "PokemonStandard",
                    "Eye" => "PokemonEyeClearCoat",
                    "EyeClearCoat" => "PokemonEyeClearCoat",
                    _ => name
                };
            }

            if (name.StartsWith("Transparent", StringComparison.OrdinalIgnoreCase))
            {
                return "Transparent";
            }

            if (name.StartsWith("IkCharacterTransparent", StringComparison.OrdinalIgnoreCase))
            {
                return "Transparent";
            }

            return name switch
            {
                "Opaque" => "Standard",
                "IkCharacter" => material.Name.Contains("eye", StringComparison.OrdinalIgnoreCase)
                    ? "EyeClearCoat"
                    : "Standard",
                "Custom" when IsLayeredCharacterTechnique(name, material) => "Standard",
                "Hair" => "Hair",
                "SSS" => "SSS",
                "Eye" => "EyeClearCoat",
                "EyeClearCoat" => "EyeClearCoat",
                "Fabric" => "Standard",
                "FresnelEffect" => "FresnelOpaque",
                "FresnelBlend" => "Fresnel",
                "Unlit" => "Unlit",
                "Fire" => "Fire",
                // TODO Make more shaders.
                _ => name
            };
        }

        private bool IsEyeClearCoatShader()
        {
            return string.Equals(shaderKey, "EyeClearCoat", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(shaderKey, "PokemonEyeClearCoat", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsLayeredCharacterTechnique(string shaderName, TRMaterial material)
        {
            if (shaderName.StartsWith("IkCharacter", StringComparison.OrdinalIgnoreCase))
            {
                return HasCharacterLayerInputs(material);
            }

            if (!string.Equals(shaderName, "Custom", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return HasCharacterLayerInputs(material);
        }

        private static bool HasCharacterLayerInputs(TRMaterial material)
        {
            var textures = material.Textures ?? Array.Empty<TRTexture>();
            bool hasBaseColor = textures.Any(texture =>
                texture != null && string.Equals(texture.Name, "BaseColorMap", StringComparison.OrdinalIgnoreCase));
            bool hasMeaningfulLayerMask = textures.Any(texture =>
                texture != null &&
                string.Equals(texture.Name, "LayerMaskMap", StringComparison.OrdinalIgnoreCase) &&
                IsMeaningfulLayerMaskTexture(texture.File));
            bool hasColorLayers = (material.Vec4fParams ?? Array.Empty<TRVec4fParameter>())
                .Any(param => param != null && param.Name.StartsWith("BaseColorLayer", StringComparison.OrdinalIgnoreCase));

            return hasBaseColor && hasMeaningfulLayerMask && hasColorLayers;
        }

        private static bool IsMeaningfulLayerMaskTexture(string? file)
        {
            if (string.IsNullOrWhiteSpace(file))
            {
                return false;
            }

            string normalized = file.Replace('\\', '/');
            if (normalized.Contains("/share/common/sh_black_msk/", StringComparison.OrdinalIgnoreCase) ||
                normalized.Contains("/share/common/sh_white_msk/", StringComparison.OrdinalIgnoreCase) ||
                normalized.Contains("/share/common/sh_dummy", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return true;
        }

        private bool IsLayeredUnlitEffect(string shaderName, TRMaterial material)
        {
            if (!string.Equals(shaderName, "Unlit", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            int layerCount = 0;
            var layerValue = ShaderParams
                .FirstOrDefault(param => string.Equals(param.Name, "NumMaterialLayer", StringComparison.OrdinalIgnoreCase))
                .Value;
            int.TryParse(layerValue, out layerCount);

            var textureNames = (material.Textures ?? Array.Empty<TRTexture>())
                .Select(texture => texture.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            bool hasLayerMask = textureNames.Contains("LayerMaskMap");
            bool hasMotionMap = textureNames.Contains("DisplacementMap") || textureNames.Contains("DistortionMap");
            bool hasEmission = floatParams.Any(param => param != null &&
                string.Equals(param.Name, "EmissionIntensity", StringComparison.OrdinalIgnoreCase));

            if (layerCount > 1 && hasLayerMask && hasMotionMap && hasEmission)
            {
                DiagnosticLog.Write($"Layered Unlit effect routed to Fire: material={material.Name}, layers={layerCount}");
                return true;
            }

            return false;
        }

        private void SetTextureFlags(Shader activeShader, HashSet<string> textureNames)
        {
            activeShader.SetBoolIfExists("EnableBaseColorMap", textureNames.Contains("BaseColorMap"));
            activeShader.SetBoolIfExists("EnableLayerMaskMap", textureNames.Contains("LayerMaskMap"));
            activeShader.SetBoolIfExists("EnableColorTableMap", textureNames.Contains("ColorTableMap"));
            activeShader.SetBoolIfExists("EnableEyeMaskMap", textureNames.Contains("EyeMaskMap"));
            activeShader.SetBoolIfExists("EnableFresnelMaskMap", textureNames.Contains("FresnelMaskMap"));
            activeShader.SetBoolIfExists("EnableNormalMap", RenderOptions.EnableNormalMaps && textureNames.Contains("NormalMap"));
            activeShader.SetBoolIfExists("EnableNormalMap1", RenderOptions.EnableNormalMaps && textureNames.Contains("NormalMap1"));
            activeShader.SetBoolIfExists("EnableNormalMap2", RenderOptions.EnableNormalMaps && textureNames.Contains("NormalMap2"));
            activeShader.SetBoolIfExists("EnableRoughnessMap", textureNames.Contains("RoughnessMap"));
            activeShader.SetBoolIfExists("EnableRoughnessMap1", textureNames.Contains("RoughnessMap1"));
            activeShader.SetBoolIfExists("EnableRoughnessMap2", textureNames.Contains("RoughnessMap2"));
            activeShader.SetBoolIfExists("EnableMetallicMap", textureNames.Contains("MetallicMap"));
            activeShader.SetBoolIfExists("EnableAOMap", RenderOptions.EnableAO &&
                (textureNames.Contains("AOMap") || textureNames.Contains("OcclusionMap")));
            activeShader.SetBoolIfExists("EnableDetailMaskMap", textureNames.Contains("DetailMaskMap"));
            activeShader.SetBoolIfExists("EnableSSSMaskMap", textureNames.Contains("SSSMaskMap"));
            activeShader.SetBoolIfExists("EnableHairFlowMap", textureNames.Contains("HairFlowMap"));
            activeShader.SetBoolIfExists("EnableDisplacementMap", textureNames.Contains("DisplacementMap"));
            activeShader.SetBoolIfExists("EnableDistortionMap", textureNames.Contains("DistortionMap") || textureNames.Contains("DisplacementMap"));
        }

        private bool ShouldUseColorAtlasAoUv()
        {
            if (!usesIkCharacterTechnique)
            {
                return false;
            }

            var ao = textures.FirstOrDefault(texture =>
                string.Equals(texture.Name, "AOMap", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(texture.Name, "OcclusionMap", StringComparison.OrdinalIgnoreCase));
            var baseColor = textures.FirstOrDefault(texture =>
                string.Equals(texture.Name, "BaseColorMap", StringComparison.OrdinalIgnoreCase));
            var normal = textures.FirstOrDefault(texture =>
                string.Equals(texture.Name, "NormalMap", StringComparison.OrdinalIgnoreCase));
            if (ao == null || baseColor == null || normal == null ||
                ao.Width <= 0 || ao.Height <= 0 ||
                baseColor.Width <= 0 || baseColor.Height <= 0 ||
                normal.Width <= 0 || normal.Height <= 0)
            {
                return false;
            }

            var aoAspect = ao.Width / (float)ao.Height;
            var colorAspect = baseColor.Width / (float)baseColor.Height;
            var normalAspect = normal.Width / (float)normal.Height;
            return Math.Abs(aoAspect - colorAspect) < Math.Abs(aoAspect - normalAspect);
        }

        private static string ResolveTextureUniformName(string textureName)
        {
            return textureName switch
            {
                // IkCharacter calls the same ambient-occlusion texture OcclusionMap.
                // The viewer's shared shaders expose it as AOMap.
                "OcclusionMap" => "AOMap",
                _ => textureName
            };
        }

        private void LogFireMaterialDebug()
        {
            var textureSummary = textures.Count == 0
                ? "<none>"
                : string.Join("; ", textures.Select(t => $"{t.Name}:{t.SourceFile}:slot{t.Slot}"));
            DiagnosticLog.Write($"Fire material inferred: name={Name}, shader={shaderKey}, textures={textureSummary}");

            foreach (var param in ShaderParams)
            {
                DiagnosticLog.Write($"  fire shader option: {param.Name}={param.Value}");
            }

            foreach (var param in floatParams.Where(p => p != null))
            {
                DiagnosticLog.Write($"  fire float param: {param.Name}={param.Value}");
            }

            foreach (var param in vec2Params.Where(p => p != null))
            {
                DiagnosticLog.Write($"  fire vec2 param: {param.Name}=({param.Value.X}, {param.Value.Y})");
            }

            foreach (var param in vec3Params.Where(p => p != null))
            {
                DiagnosticLog.Write($"  fire vec3 param: {param.Name}=({param.Value.X}, {param.Value.Y}, {param.Value.Z})");
            }

            foreach (var param in vec4Params.Where(p => p != null))
            {
                var decoded = ConvertVector4ForShader(param);
                DiagnosticLog.Write($"  fire vec4 param: {param.Name}, storedWXYZ=({param.Value.W}, {param.Value.X}, {param.Value.Y}, {param.Value.Z}), shader=({decoded.X}, {decoded.Y}, {decoded.Z}, {decoded.W})");
            }
        }

        private bool ShouldEnableEyeBaseSclera()
        {
            if (!IsEyeClearCoatShader() ||
                !Name.Contains("eye", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var baseColor = textures.FirstOrDefault(t => string.Equals(t.Name, "BaseColorMap", StringComparison.OrdinalIgnoreCase));
            var layerMask = textures.FirstOrDefault(t => string.Equals(t.Name, "LayerMaskMap", StringComparison.OrdinalIgnoreCase));
            if (baseColor == null || layerMask == null)
            {
                return false;
            }

            var baseStem = Path.GetFileNameWithoutExtension(baseColor.SourceFile) ?? string.Empty;
            var layerStem = Path.GetFileNameWithoutExtension(layerMask.SourceFile) ?? string.Empty;
            var baseIsEyeSpecific = baseStem.Contains("_eye", StringComparison.OrdinalIgnoreCase) ||
                baseStem.Contains("eye_", StringComparison.OrdinalIgnoreCase);
            var baseIsGenericAlbedo = baseStem.EndsWith("_alb", StringComparison.OrdinalIgnoreCase) && !baseIsEyeSpecific;
            var layerIsEyeMask = layerStem.Contains("_eye", StringComparison.OrdinalIgnoreCase) &&
                layerStem.EndsWith("_lym", StringComparison.OrdinalIgnoreCase);

            return baseIsGenericAlbedo && layerIsEyeMask;
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
            // IkCharacter has no meaningful legacy fallback: its albedo is intentionally
            // white and all palette data lives in LayerMaskMap. Preserve old Legacy mode
            // for existing material techniques, but never let it discard that palette.
            activeShader.SetBoolIfExists("LegacyMode", RenderOptions.LegacyMode &&
                !usesTrinityVectorLayout &&
                !string.Equals(shaderKey, "Fresnel", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(shaderKey, "FresnelOpaque", StringComparison.OrdinalIgnoreCase));
        }

        private void AddAutoEyeMaskTexture(PathString modelPath)
        {
            if (!IsEyeClearCoatShader() ||
                !Name.Contains("eye", StringComparison.OrdinalIgnoreCase) ||
                textures.Any(t => string.Equals(t.Name, "EyeMaskMap", StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            var maskSource = textures.FirstOrDefault(t => string.Equals(t.Name, "LayerMaskMap", StringComparison.OrdinalIgnoreCase))
                ?? textures.FirstOrDefault(t => string.Equals(t.Name, "BaseColorMap", StringComparison.OrdinalIgnoreCase));
            if (maskSource == null)
            {
                return;
            }

            var stem = Path.GetFileNameWithoutExtension(maskSource.SourceFile);
            var eyeIndex = stem.IndexOf("_eye", StringComparison.OrdinalIgnoreCase);
            if (eyeIndex <= 0)
            {
                return;
            }

            var prefix = stem.Substring(0, eyeIndex);
            var candidates = new List<string>();
            if (Name.StartsWith("l_", StringComparison.OrdinalIgnoreCase))
            {
                candidates.Add($"{prefix}_l_eye_msk.bntx");
            }
            else if (Name.StartsWith("r_", StringComparison.OrdinalIgnoreCase))
            {
                candidates.Add($"{prefix}_r_eye_msk.bntx");
            }

            candidates.Add($"{prefix}_eye_msk.bntx");

            foreach (var candidate in candidates.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (!File.Exists(modelPath.Combine(candidate)))
                {
                    continue;
                }

                textures.Add(new Texture(modelPath, "EyeMaskMap", candidate, 7));
                DiagnosticLog.Write($"Auto eye mask texture attached: material={Name}, file={candidate}");
                return;
            }
        }

        private void LogMaterialSummary(string originalShaderName)
        {
            var textureSummary = textures.Count == 0
                ? "<none>"
                : string.Join("; ", textures.Select(texture => $"{texture.Name}:{texture.SourceFile}:slot{texture.Slot}"));
            DiagnosticLog.Write(
                $"Material final: name={Name}, family={(isPokemonMaterial ? "Pokemon" : "Npc")}, sourceShader={originalShaderName}, shader={shaderKey}, " +
                $"layeredPalette={usesIkCharacterTechnique}, vectorLayout={(usesTrinityVectorLayout ? "WXYZ" : "XYZW")}, " +
                $"transparent={isTransparent}, textures={textureSummary}");

            var baseColor = vec4Params.FirstOrDefault(param => param != null &&
                string.Equals(param.Name, "BaseColor", StringComparison.OrdinalIgnoreCase));
            if (baseColor != null)
            {
                var decoded = ConvertVector4ForShader(baseColor);
                DiagnosticLog.Write(
                    $"Material BaseColor decoded: material={Name}, shaderRGBA=" +
                    $"({decoded.X}, {decoded.Y}, {decoded.Z}, {decoded.W})");
            }

            if (usesIkCharacterTechnique)
            {
                var layerColors = vec4Params
                    .Where(param => param != null && param.Name.StartsWith("BaseColorLayer", StringComparison.OrdinalIgnoreCase))
                    .OrderBy(param => param.Name, StringComparer.OrdinalIgnoreCase)
                    .Select(param =>
                    {
                        var decoded = ConvertVector4ForShader(param);
                        return $"{param.Name}=({decoded.X}, {decoded.Y}, {decoded.Z}, {decoded.W})";
                    });
                DiagnosticLog.Write(
                    $"Material character palette: material={Name}, baseColorMultiply={ResolveShaderBoolean("BaseColorMultiply", true)}, " +
                    $"layers={string.Join("; ", layerColors)}");
            }
        }
    }
}
