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
        private void ApplyShaderParams(Shader activeShader, UvSetOverride layerMaskUvOverride, UvSetOverride aoUvOverride, bool baseColorMapIsPlaceholder)
        {
            // Keep ColorTableMap-derived layer colors in sync with runtime edits.
            // When disabled, we fall back to the per-layer color params stored in the TRMTR.
            TryApplyColorTableOverrides();

            var baseColorMultiply = ShaderParams.Any(p =>
                string.Equals(p.Name, "BaseColorMultiply", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(p.Value, "true", StringComparison.OrdinalIgnoreCase));

            foreach (var param in ShaderParams)
            {
                var name = param.Name;
                var value = param.Value;

                if (string.Equals(name, "UVTransformMode", StringComparison.OrdinalIgnoreCase))
                {
                    // Common enum in TRMTR: SRT / T (translation-only)
                    // Some files also use "None" meaning default.
                    var v = value?.Trim();
                    if (string.Equals(v, "T", StringComparison.OrdinalIgnoreCase))
                    {
                        activeShader.SetIntIfExists(name, 1);
                        continue;
                    }
                    if (string.Equals(v, "SRT", StringComparison.OrdinalIgnoreCase))
                    {
                        activeShader.SetIntIfExists(name, 0);
                        continue;
                    }
                    if (string.Equals(v, "None", StringComparison.OrdinalIgnoreCase))
                    {
                        activeShader.SetIntIfExists(name, 0);
                        continue;
                    }
                }

                if (string.Equals(name, "EyelidType", StringComparison.OrdinalIgnoreCase))
                {
                    // Eye technique enum: None / Upper / Lower / All
                    var v = value?.Trim();
                    if (string.Equals(v, "None", StringComparison.OrdinalIgnoreCase))
                    {
                        activeShader.SetIntIfExists(name, 0);
                        continue;
                    }
                    if (string.Equals(v, "Upper", StringComparison.OrdinalIgnoreCase))
                    {
                        activeShader.SetIntIfExists(name, 1);
                        continue;
                    }
                    if (string.Equals(v, "Lower", StringComparison.OrdinalIgnoreCase))
                    {
                        activeShader.SetIntIfExists(name, 2);
                        continue;
                    }
                    if (string.Equals(v, "All", StringComparison.OrdinalIgnoreCase))
                    {
                        activeShader.SetIntIfExists(name, 3);
                        continue;
                    }
                }

                if (string.Equals(name, "EnableIrisRefraction", StringComparison.OrdinalIgnoreCase))
                {
                    // IkCharacter eye option enum: None / Ng / Ncc
                    var v = value?.Trim();
                    if (string.Equals(v, "None", StringComparison.OrdinalIgnoreCase))
                    {
                        activeShader.SetIntIfExists(name, 0);
                        continue;
                    }
                    if (string.Equals(v, "Ng", StringComparison.OrdinalIgnoreCase))
                    {
                        activeShader.SetIntIfExists(name, 1);
                        continue;
                    }
                    if (string.Equals(v, "Ncc", StringComparison.OrdinalIgnoreCase))
                    {
                        activeShader.SetIntIfExists(name, 2);
                        continue;
                    }
                }

                if (string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(value, "false", StringComparison.OrdinalIgnoreCase))
                {
                    activeShader.SetBoolIfExists(name, string.Equals(value, "true", StringComparison.OrdinalIgnoreCase));
                    continue;
                }

                if (int.TryParse(value, out int intValue))
                {
                    if (activeShader.TryGetUniformType(name, out var type) && type == ActiveUniformType.Float)
                    {
                        activeShader.SetFloatIfExists(name, intValue);
                    }
                    else
                    {
                        activeShader.SetIntIfExists(name, intValue);
                    }
                    continue;
                }

                if (float.TryParse(value, out float floatValue))
                {
                    if (activeShader.TryGetUniformType(name, out var type) && (type == ActiveUniformType.Int || type == ActiveUniformType.Bool))
                    {
                        activeShader.SetIntIfExists(name, (int)MathF.Round(floatValue));
                    }
                    else
                    {
                        activeShader.SetFloatIfExists(name, floatValue);
                    }
                }
            }

            foreach (var param in floatParams)
            {
                if (string.Equals(param.Name, "NumMaterialLayer", StringComparison.OrdinalIgnoreCase))
                {
                    activeShader.SetIntIfExists(param.Name, (int)MathF.Round(param.Value));
                }
                activeShader.SetFloatIfExists(param.Name, param.Value);
            }

            bool hasNumMaterialLayer = floatParams.Any(p => string.Equals(p.Name, "NumMaterialLayer", StringComparison.OrdinalIgnoreCase)) ||
                                       ShaderParams.Any(p => string.Equals(p.Name, "NumMaterialLayer", StringComparison.OrdinalIgnoreCase));

            if (!hasNumMaterialLayer &&
                vec4Params.Any(p => p.Name.StartsWith("BaseColorLayer", StringComparison.OrdinalIgnoreCase)))
            {
                activeShader.SetIntIfExists("NumMaterialLayer", 1);
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
                // Vector4f comes from Flatbuffers as W,X,Y,Z (FlatBufferItem order).
                // Shaders (and the UI) treat UVScaleOffset etc as (x,y,z,w) where x/y=scale and z/w=offset, so
                // we pass through in the stored order.
                activeShader.SetVector4IfExists(param.Name, new Vector4(param.Value.W, param.Value.X, param.Value.Y, param.Value.Z));
            }

            // Override per-layer colors with ColorTableMap-derived palette colors (when enabled).
            // Applied before user overrides so UI edits still win.
            ApplyColorTableUniformOverrides(activeShader);

            if (!vec4Params.Any(p => string.Equals(p.Name, "UVScaleOffset", StringComparison.OrdinalIgnoreCase)))
            {
                activeShader.SetVector4IfExists("UVScaleOffset", new Vector4(1, 1, 0, 0));
            }

            if (!vec4Params.Any(p => string.Equals(p.Name, "UVScaleOffsetNormal", StringComparison.OrdinalIgnoreCase)))
            {
                activeShader.SetVector4IfExists("UVScaleOffsetNormal", new Vector4(1, 1, 0, 0));
            }

            if (!vec4Params.Any(p => string.Equals(p.Name, "UVScaleOffsetInsideEmissionParallaxHeight", StringComparison.OrdinalIgnoreCase)))
            {
                activeShader.SetVector4IfExists("UVScaleOffsetInsideEmissionParallaxHeight", new Vector4(1, 1, 0, 0));
            }

            if (!vec4Params.Any(p => string.Equals(p.Name, "UVScaleOffsetInsideEmissionParallaxIntensity", StringComparison.OrdinalIgnoreCase)))
            {
                activeShader.SetVector4IfExists("UVScaleOffsetInsideEmissionParallaxIntensity", new Vector4(1, 1, 0, 0));
            }

            bool TryGetUvIndex(string name, out int value)
            {
                if (TryGetShaderParamInt(name, out value))
                {
                    return true;
                }

                foreach (var param in floatParams)
                {
                    if (!string.Equals(param.Name, name, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    value = (int)MathF.Round(param.Value);
                    return true;
                }

                value = 0;
                return false;
            }

            bool hasUvIndexLayerMask = TryGetUvIndex("UVIndexLayerMask", out int uvIndexLayerMask);
            activeShader.SetBoolIfExists("HasUVIndexLayerMask", hasUvIndexLayerMask);
            activeShader.SetIntIfExists("UVIndexLayerMask", hasUvIndexLayerMask ? uvIndexLayerMask : 0);

            bool hasUvIndexAo = TryGetUvIndex("UVIndexAO", out int uvIndexAo);
            activeShader.SetBoolIfExists("HasUVIndexAO", hasUvIndexAo);
            activeShader.SetIntIfExists("UVIndexAO", hasUvIndexAo ? uvIndexAo : 0);

            bool hasUvIndexIepHeight = TryGetUvIndex("UVIndexInsideEmissionParallaxHeight", out int uvIndexIepHeight);
            activeShader.SetBoolIfExists("HasUVIndexInsideEmissionParallaxHeight", hasUvIndexIepHeight);
            activeShader.SetIntIfExists("UVIndexInsideEmissionParallaxHeight", hasUvIndexIepHeight ? uvIndexIepHeight : -1);

            bool hasUvIndexIepIntensity = TryGetUvIndex("UVIndexInsideEmissionParallaxIntensity", out int uvIndexIepIntensity);
            activeShader.SetBoolIfExists("HasUVIndexInsideEmissionParallaxIntensity", hasUvIndexIepIntensity);
            activeShader.SetIntIfExists("UVIndexInsideEmissionParallaxIntensity", hasUvIndexIepIntensity ? uvIndexIepIntensity : -1);

            var effectiveLayerMaskOverride = layerMaskUvOverride != UvSetOverride.Material
                ? layerMaskUvOverride
                : RenderOptions.LayerMaskUvOverride;

            switch (effectiveLayerMaskOverride)
            {
                case UvSetOverride.Uv0:
                    activeShader.SetBoolIfExists("HasUVIndexLayerMask", true);
                    activeShader.SetIntIfExists("UVIndexLayerMask", 0);
                    break;
                case UvSetOverride.Uv1:
                    activeShader.SetBoolIfExists("HasUVIndexLayerMask", true);
                    activeShader.SetIntIfExists("UVIndexLayerMask", 1);
                    break;
            }

            var effectiveAoOverride = aoUvOverride != UvSetOverride.Material
                ? aoUvOverride
                : RenderOptions.AOUvOverride;

            switch (effectiveAoOverride)
            {
                case UvSetOverride.Uv0:
                    activeShader.SetIntIfExists("UVIndexAO", 0);
                    break;
                case UvSetOverride.Uv1:
                    activeShader.SetIntIfExists("UVIndexAO", 1);
                    break;
            }

            if (!vec4Params.Any(p => string.Equals(p.Name, "BaseColor", StringComparison.OrdinalIgnoreCase)))
            {
                activeShader.SetVector4IfExists("BaseColor", Vector4.One);
            }

            if (string.Equals(shaderKey, "FresnelBlend", StringComparison.OrdinalIgnoreCase))
            {
                bool HasFloatParam(string name) =>
                    floatParams.Any(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)) ||
                    ShaderParams.Any(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));

                bool HasVec4Param(string name) =>
                    vec4Params.Any(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));

                // Defaults that disable the fresnel layer unless explicitly configured by the material.
                if (!HasFloatParam("FresnelAlphaMin")) activeShader.SetFloatIfExists("FresnelAlphaMin", 0.0f);
                if (!HasFloatParam("FresnelAlphaMax")) activeShader.SetFloatIfExists("FresnelAlphaMax", 0.0f);
                if (!HasFloatParam("FresnelAngleBias")) activeShader.SetFloatIfExists("FresnelAngleBias", 0.0f);
                if (!HasFloatParam("FresnelPower")) activeShader.SetFloatIfExists("FresnelPower", 1.0f);

                if (!HasFloatParam("AOIntensity")) activeShader.SetFloatIfExists("AOIntensity", 1.0f);
                if (!HasFloatParam("AOIntensityFresnel")) activeShader.SetFloatIfExists("AOIntensityFresnel", 1.0f);

                if (!HasFloatParam("MetallicLayer5")) activeShader.SetFloatIfExists("MetallicLayer5", 0.0f);
                if (!HasFloatParam("RoughnessLayer5")) activeShader.SetFloatIfExists("RoughnessLayer5", 0.5f);

                // Fresnel layer colors default to white; with FresnelAlphaMin/Max defaulting to 0 this has no effect unless enabled.
                if (!HasVec4Param("BaseColorLayer5")) activeShader.SetVector4IfExists("BaseColorLayer5", Vector4.One);
                if (!HasVec4Param("BaseColorLayer6")) activeShader.SetVector4IfExists("BaseColorLayer6", Vector4.One);
                if (!HasVec4Param("BaseColorLayer7")) activeShader.SetVector4IfExists("BaseColorLayer7", Vector4.One);
                if (!HasVec4Param("BaseColorLayer8")) activeShader.SetVector4IfExists("BaseColorLayer8", Vector4.One);
                if (!HasVec4Param("BaseColorLayer9")) activeShader.SetVector4IfExists("BaseColorLayer9", Vector4.One);

                if (!HasVec4Param("EmissionColor")) activeShader.SetVector4IfExists("EmissionColor", Vector4.Zero);
                if (!HasVec4Param("EmissionColorLayer5")) activeShader.SetVector4IfExists("EmissionColorLayer5", Vector4.Zero);

                if (!HasFloatParam("EmissionIntensity")) activeShader.SetFloatIfExists("EmissionIntensity", 0.0f);
                if (!HasFloatParam("EmissionIntensityLayer1")) activeShader.SetFloatIfExists("EmissionIntensityLayer1", 0.0f);
                if (!HasFloatParam("EmissionIntensityLayer2")) activeShader.SetFloatIfExists("EmissionIntensityLayer2", 0.0f);
                if (!HasFloatParam("EmissionIntensityLayer3")) activeShader.SetFloatIfExists("EmissionIntensityLayer3", 0.0f);
                if (!HasFloatParam("EmissionIntensityLayer4")) activeShader.SetFloatIfExists("EmissionIntensityLayer4", 0.0f);
                if (!HasFloatParam("EmissionIntensityLayer5")) activeShader.SetFloatIfExists("EmissionIntensityLayer5", 0.0f);

                if (!HasFloatParam("DiscardValue")) activeShader.SetFloatIfExists("DiscardValue", 0.0f);
                if (!HasFloatParam("ParallaxHeight")) activeShader.SetFloatIfExists("ParallaxHeight", 0.0f);
            }

            if (string.Equals(shaderKey, "SSS", StringComparison.OrdinalIgnoreCase))
            {
                if (!vec4Params.Any(p => string.Equals(p.Name, "SubsurfaceColor", StringComparison.OrdinalIgnoreCase)))
                {
                    activeShader.SetVector4IfExists("SubsurfaceColor", new Vector4(1.0f, 0.9f, 0.9f, 1.0f));
                }

                bool HasFloatParam(string name) =>
                    floatParams.Any(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)) ||
                    ShaderParams.Any(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));

                if (!HasFloatParam("SSSScatterPower")) activeShader.SetFloatIfExists("SSSScatterPower", 2.0f);
                if (!HasFloatParam("SSSEmission")) activeShader.SetFloatIfExists("SSSEmission", 1.0f);
                if (!HasFloatParam("SSSMaskStrength")) activeShader.SetFloatIfExists("SSSMaskStrength", 1.0f);
            }

            // Only apply the BaseColor=Layer1 fallback when the BaseColorMap is a placeholder mask texture.
            // Many real materials (hair/eyebrow/etc) have non-white Layer1 values for runtime recolors; forcing it here tints them incorrectly.
            if (baseColorMultiply && baseColorMapIsPlaceholder)
            {
                var baseColor = vec4Params.FirstOrDefault(p => string.Equals(p.Name, "BaseColor", StringComparison.OrdinalIgnoreCase))?.Value;
                var baseColorLayer1 = vec4Params.FirstOrDefault(p => string.Equals(p.Name, "BaseColorLayer1", StringComparison.OrdinalIgnoreCase))?.Value;

                if (baseColor != null && baseColorLayer1 != null)
                {
                    var baseVec = new Vector4(baseColor.W, baseColor.X, baseColor.Y, baseColor.Z);
                    var layerVec = new Vector4(baseColorLayer1.W, baseColorLayer1.X, baseColorLayer1.Y, baseColorLayer1.Z);

                    var baseIsWhite = Math.Abs(baseVec.X - 1.0f) < 0.0001f &&
                                      Math.Abs(baseVec.Y - 1.0f) < 0.0001f &&
                                      Math.Abs(baseVec.Z - 1.0f) < 0.0001f;

                    var layerIsNonWhite = Math.Abs(layerVec.X - 1.0f) > 0.0001f ||
                                          Math.Abs(layerVec.Y - 1.0f) > 0.0001f ||
                                          Math.Abs(layerVec.Z - 1.0f) > 0.0001f;

                    if (baseIsWhite && layerIsNonWhite)
                    {
                        activeShader.SetVector4IfExists("BaseColor", layerVec);
                    }
                }
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
                "InsideEmissionParallax" => "InsideEmissionParallax",
                "Eye" => "Eye",
                "EyeClearCoat" => "EyeClearCoat",
                "Unlit" => "Unlit",
                "FresnelEffect" => "Standard",
                "FresnelBlend" => "FresnelBlend",
                // TODO Make more shaders.
                _ => name
            };
        }

        private void SetTextureFlags(Shader activeShader, HashSet<string> textureNames, bool layerMaskIsPlaceholder)
        {
            bool OptionEnabled(string name, bool defaultValue = true)
            {
                for (int i = 0; i < ShaderParams.Count; i++)
                {
                    if (!string.Equals(ShaderParams[i].Name, name, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var value = ShaderParams[i].Value?.Trim();
                    if (string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) || string.Equals(value, "1", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                    if (string.Equals(value, "false", StringComparison.OrdinalIgnoreCase) || string.Equals(value, "0", StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }

                    return defaultValue;
                }

                return defaultValue;
            }

            bool OptionEnabledByDefaultOff(string name) => OptionEnabled(name, defaultValue: false);

            // IkCharacter materials frequently omit an explicit `EnableLayerMaskMap` option even when a LayerMaskMap is present
            // (eyes rely on it heavily). Default to enabled when the texture exists.
            bool enableLayerMask = !layerMaskIsPlaceholder &&
                                   textureNames.Contains("LayerMaskMap") &&
                                   OptionEnabled("EnableLayerMaskMap", defaultValue: true);

            // Most materials expect these texture toggles to default ON when the texture exists.
            // Many TRMTRs omit explicit `Enable*Map` options, which previously caused textures (notably BaseColorMap)
            // to be ignored and resulted in flat/incorrect shading.
            activeShader.SetBoolIfExists("EnableBaseColorMap", textureNames.Contains("BaseColorMap") && OptionEnabled("EnableBaseColorMap", defaultValue: true));
            activeShader.SetBoolIfExists("EnableLayerMaskMap", enableLayerMask);
            activeShader.SetBoolIfExists("EnableNormalMap", RenderOptions.EnableNormalMaps && textureNames.Contains("NormalMap") && OptionEnabled("EnableNormalMap", defaultValue: true));
            activeShader.SetBoolIfExists("EnableNormalMap1", RenderOptions.EnableNormalMaps && textureNames.Contains("NormalMap1") && OptionEnabled("EnableNormalMap1", defaultValue: true));
            activeShader.SetBoolIfExists("EnableNormalMap2", RenderOptions.EnableNormalMaps && textureNames.Contains("NormalMap2") && OptionEnabled("EnableNormalMap2", defaultValue: true));
            activeShader.SetBoolIfExists("EnableRoughnessMap", textureNames.Contains("RoughnessMap") && OptionEnabled("EnableRoughnessMap", defaultValue: true));
            activeShader.SetBoolIfExists("EnableRoughnessMap1", textureNames.Contains("RoughnessMap1") && OptionEnabled("EnableRoughnessMap1", defaultValue: true));
            activeShader.SetBoolIfExists("EnableRoughnessMap2", textureNames.Contains("RoughnessMap2") && OptionEnabled("EnableRoughnessMap2", defaultValue: true));
            activeShader.SetBoolIfExists("EnableMetallicMap", textureNames.Contains("MetallicMap") && OptionEnabled("EnableMetallicMap", defaultValue: true));
            activeShader.SetBoolIfExists("EnableAOMap", RenderOptions.EnableAO && textureNames.Contains("AOMap") && OptionEnabled("EnableAOMap", defaultValue: true));
            activeShader.SetBoolIfExists("EnableDetailMaskMap", textureNames.Contains("DetailMaskMap") && OptionEnabled("EnableDetailMaskMap", defaultValue: true));
            activeShader.SetBoolIfExists("EnableSSSMaskMap", textureNames.Contains("SSSMaskMap") && OptionEnabled("EnableSSSMaskMap", defaultValue: true));
            activeShader.SetBoolIfExists("EnableHairFlowMap", textureNames.Contains("HairFlowMap") && OptionEnabled("EnableHairFlowMap", defaultValue: true));

            // These maps are frequently present without explicit shader options (notably on IkCharacter).
            // Default them to enabled when the texture exists to match common import behavior.
            activeShader.SetBoolIfExists("EnableSpecularMaskMap", textureNames.Contains("SpecularMaskMap") && OptionEnabled("EnableSpecularMaskMap", defaultValue: true));
            activeShader.SetBoolIfExists("EnableHighlightMaskMap", textureNames.Contains("HighlightMaskMap") && OptionEnabled("EnableHighlightMaskMap", defaultValue: true));
            // Some materials include a placeholder black DiscardMaskMap; only use it when explicitly enabled.
            activeShader.SetBoolIfExists("EnableDiscardMaskMap", textureNames.Contains("DiscardMaskMap") && OptionEnabledByDefaultOff("EnableDiscardMaskMap"));
            // IkCharacter full decomp always uses these maps when present; many TRMTRs omit explicit enable options.
            bool defaultIkCharacterOn = string.Equals(shaderKey, "IkCharacter", StringComparison.OrdinalIgnoreCase);
            activeShader.SetBoolIfExists("EnableShadowingColorMap",
                textureNames.Contains("ShadowingColorMap") && OptionEnabled("EnableShadowingColorMap", defaultValue: defaultIkCharacterOn));
            activeShader.SetBoolIfExists("EnableShadowingColorMaskMap",
                textureNames.Contains("ShadowingColorMaskMap") && OptionEnabled("EnableShadowingColorMaskMap", defaultValue: defaultIkCharacterOn));
            activeShader.SetBoolIfExists("EnableRimLightMaskMap", textureNames.Contains("RimLightMaskMap") && OptionEnabled("EnableRimLightMaskMap", defaultValue: true));
            activeShader.SetBoolIfExists("EnableOpacityMap1", textureNames.Contains("OpacityMap1"));

            activeShader.SetBoolIfExists("EnableEyeOptions", OptionEnabledByDefaultOff("EnableEyeOptions"));
            activeShader.SetBoolIfExists("EnableHighlight", OptionEnabledByDefaultOff("EnableHighlight"));

            if (defaultIkCharacterOn)
            {
                bool HasFloatParam(string name) =>
                    floatParams.Any(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)) ||
                    ShaderParams.Any(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));

                bool HasVec4Param(string name) =>
                    vec4Params.Any(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));

                bool HasShaderParam(string name) =>
                    ShaderParams.Any(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));

                if (!HasShaderParam("EnableEmissionFresnelLayer")) activeShader.SetBoolIfExists("EnableEmissionFresnelLayer", false);
                if (!HasShaderParam("EnableEmissionColorMap")) activeShader.SetBoolIfExists("EnableEmissionColorMap", false);

                if (!HasFloatParam("FresnelAlphaMin1")) activeShader.SetFloatIfExists("FresnelAlphaMin1", 0.0f);
                if (!HasFloatParam("FresnelAlphaMin2")) activeShader.SetFloatIfExists("FresnelAlphaMin2", 0.0f);
                if (!HasFloatParam("FresnelAlphaMin3")) activeShader.SetFloatIfExists("FresnelAlphaMin3", 0.0f);
                if (!HasFloatParam("FresnelAlphaMin4")) activeShader.SetFloatIfExists("FresnelAlphaMin4", 0.0f);
                if (!HasFloatParam("FresnelAlphaMin5")) activeShader.SetFloatIfExists("FresnelAlphaMin5", 0.0f);

                if (!HasFloatParam("FresnelAlphaMax1")) activeShader.SetFloatIfExists("FresnelAlphaMax1", 0.0f);
                if (!HasFloatParam("FresnelAlphaMax2")) activeShader.SetFloatIfExists("FresnelAlphaMax2", 0.0f);
                if (!HasFloatParam("FresnelAlphaMax3")) activeShader.SetFloatIfExists("FresnelAlphaMax3", 0.0f);
                if (!HasFloatParam("FresnelAlphaMax4")) activeShader.SetFloatIfExists("FresnelAlphaMax4", 0.0f);
                if (!HasFloatParam("FresnelAlphaMax5")) activeShader.SetFloatIfExists("FresnelAlphaMax5", 0.0f);

                if (!HasFloatParam("FresnelAngleBias1")) activeShader.SetFloatIfExists("FresnelAngleBias1", 0.0f);
                if (!HasFloatParam("FresnelAngleBias2")) activeShader.SetFloatIfExists("FresnelAngleBias2", 0.0f);
                if (!HasFloatParam("FresnelAngleBias3")) activeShader.SetFloatIfExists("FresnelAngleBias3", 0.0f);
                if (!HasFloatParam("FresnelAngleBias4")) activeShader.SetFloatIfExists("FresnelAngleBias4", 0.0f);
                if (!HasFloatParam("FresnelAngleBias5")) activeShader.SetFloatIfExists("FresnelAngleBias5", 0.0f);

                if (!HasFloatParam("FresnelPower1")) activeShader.SetFloatIfExists("FresnelPower1", 1.0f);
                if (!HasFloatParam("FresnelPower2")) activeShader.SetFloatIfExists("FresnelPower2", 1.0f);
                if (!HasFloatParam("FresnelPower3")) activeShader.SetFloatIfExists("FresnelPower3", 1.0f);
                if (!HasFloatParam("FresnelPower4")) activeShader.SetFloatIfExists("FresnelPower4", 1.0f);
                if (!HasFloatParam("FresnelPower5")) activeShader.SetFloatIfExists("FresnelPower5", 1.0f);

                if (!HasVec4Param("FresnelColor1")) activeShader.SetVector4IfExists("FresnelColor1", Vector4.Zero);
                if (!HasVec4Param("FresnelColor2")) activeShader.SetVector4IfExists("FresnelColor2", Vector4.Zero);
                if (!HasVec4Param("FresnelColor3")) activeShader.SetVector4IfExists("FresnelColor3", Vector4.Zero);
                if (!HasVec4Param("FresnelColor4")) activeShader.SetVector4IfExists("FresnelColor4", Vector4.Zero);
                if (!HasVec4Param("FresnelColor5")) activeShader.SetVector4IfExists("FresnelColor5", Vector4.Zero);
            }

            bool hasIepIntensityMap = textureNames.Contains("InsideEmissionParallaxIntensityMap");
            if (string.Equals(shaderKey, "InsideEmissionParallax", StringComparison.OrdinalIgnoreCase))
            {
                // IEP relies on ParallaxMap for the view-dependent offset; game defaults it effectively on.
                activeShader.SetBoolIfExists("EnableParallaxMap", textureNames.Contains("ParallaxMap") && OptionEnabled("EnableParallaxMap", defaultValue: true));

                if (!hasIepIntensityMap)
                {
                    activeShader.SetFloatIfExists("EnableIEPTexture1", 0.0f);
                    activeShader.SetFloatIfExists("EnableIEPTexture2", 0.0f);
                    activeShader.SetFloatIfExists("EnableIEPTexture3", 0.0f);
                    activeShader.SetFloatIfExists("EnableIEPTexture4", 0.0f);
                    return;
                }

                bool HasEnableIepParam(string name) =>
                    floatParams.Any(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)) ||
                    ShaderParams.Any(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));

                if (!HasEnableIepParam("EnableIEPTexture1")) activeShader.SetFloatIfExists("EnableIEPTexture1", 1.0f);
                if (!HasEnableIepParam("EnableIEPTexture2")) activeShader.SetFloatIfExists("EnableIEPTexture2", 1.0f);
                if (!HasEnableIepParam("EnableIEPTexture3")) activeShader.SetFloatIfExists("EnableIEPTexture3", 1.0f);
                if (!HasEnableIepParam("EnableIEPTexture4")) activeShader.SetFloatIfExists("EnableIEPTexture4", 1.0f);
            }
            else
            {
                activeShader.SetBoolIfExists("EnableParallaxMap", textureNames.Contains("ParallaxMap") && OptionEnabledByDefaultOff("EnableParallaxMap"));
            }
        }

        private bool TryGetShaderParamInt(string name, out int value)
        {
            value = 0;
            for (int i = 0; i < ShaderParams.Count; i++)
            {
                if (!string.Equals(ShaderParams[i].Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (int.TryParse(ShaderParams[i].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
                {
                    return true;
                }

                if (float.TryParse(ShaderParams[i].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out float floatValue))
                {
                    value = (int)MathF.Round(floatValue);
                    return true;
                }

                return false;
            }

            for (int i = 0; i < floatParams.Length; i++)
            {
                if (!string.Equals(floatParams[i].Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                value = (int)MathF.Round(floatParams[i].Value);
                return true;
            }

            return false;
        }

    }
}
