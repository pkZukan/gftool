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
        private static bool IsPlaceholderMaskTexturePath(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            return path.Contains("sh_black_msk", StringComparison.OrdinalIgnoreCase) ||
                   path.Contains("sh_white_msk", StringComparison.OrdinalIgnoreCase) ||
                   path.Contains("sh_dummy", StringComparison.OrdinalIgnoreCase);
        }

        private static void ResetCommonUniformDefaults(Shader activeShader)
        {
            activeShader.SetBoolIfExists("BaseColorMultiply", false);
            activeShader.SetBoolIfExists("EnableAlphaTest", false);
            activeShader.SetBoolIfExists("EnableEyeOptions", false);
            activeShader.SetBoolIfExists("EnableHighlight", false);
            activeShader.SetBoolIfExists("EnableParallaxMap", false);
            activeShader.SetBoolIfExists("EnableHairSpecular", false);
            activeShader.SetBoolIfExists("EnableUVScaleOffsetNormal", false);
            activeShader.SetIntIfExists("EnableIrisRefraction", 0);
            activeShader.SetBoolIfExists("EnableEmissionLayer", false);
            activeShader.SetIntIfExists("NumMaterialLayer", 0);
            activeShader.SetIntIfExists("UVTransformMode", 0);
            activeShader.SetBoolIfExists("HasUVIndexLayerMask", false);
            activeShader.SetBoolIfExists("HasUVIndexAO", false);
            activeShader.SetBoolIfExists("HasUVIndexInsideEmissionParallaxHeight", false);
            activeShader.SetBoolIfExists("HasUVIndexInsideEmissionParallaxIntensity", false);
            activeShader.SetBoolIfExists("HasUv1", true);
            activeShader.SetBoolIfExists("EnableColorTableMap", false);
            activeShader.SetIntIfExists("ColorTableDivideNumber", 0);
            activeShader.SetFloatIfExists("BaseColorIndex1", 0.0f);
            activeShader.SetFloatIfExists("BaseColorIndex2", 0.0f);
            activeShader.SetFloatIfExists("BaseColorIndex3", 0.0f);
            activeShader.SetFloatIfExists("BaseColorIndex4", 0.0f);
            activeShader.SetFloatIfExists("BaseColorIndex6", 0.0f);
            activeShader.SetFloatIfExists("BaseColorIndex7", 0.0f);
            activeShader.SetFloatIfExists("BaseColorIndex8", 0.0f);
            activeShader.SetVector4IfExists("BaseColorLayer1", Vector4.One);
            activeShader.SetVector4IfExists("BaseColorLayer2", Vector4.One);
            activeShader.SetVector4IfExists("BaseColorLayer3", Vector4.One);
            activeShader.SetVector4IfExists("BaseColorLayer4", Vector4.One);
            activeShader.SetVector4IfExists("BaseColorLayer5", Vector4.One);
            activeShader.SetVector4IfExists("BaseColorLayer6", Vector4.One);
            activeShader.SetVector4IfExists("BaseColorLayer7", Vector4.One);
            activeShader.SetVector4IfExists("BaseColorLayer8", Vector4.One);
            activeShader.SetVector4IfExists("BaseColorLayer9", Vector4.One);
            activeShader.SetVector4IfExists("BaseColorLayer10", Vector4.One);
            activeShader.SetVector4IfExists("ShadowingColorLayer1", Vector4.One);
            activeShader.SetVector4IfExists("ShadowingColorLayer2", Vector4.One);
            activeShader.SetVector4IfExists("ShadowingColorLayer3", Vector4.One);
            activeShader.SetVector4IfExists("ShadowingColorLayer4", Vector4.One);

            activeShader.SetVector4IfExists("UVCenter0", new Vector4(0.5f, 0.5f, 0.0f, 0.0f));
            activeShader.SetVector4IfExists("UVCenter1", new Vector4(0.5f, 0.5f, 0.0f, 0.0f));
            activeShader.SetFloatIfExists("UVRotation", 0.0f);
            activeShader.SetFloatIfExists("UVRotationNormal", 0.0f);
            activeShader.SetFloatIfExists("UVRotation1", 0.0f);
            activeShader.SetFloatIfExists("UVRotation2", 0.0f);
            activeShader.SetFloatIfExists("UVRotation3", 0.0f);
            activeShader.SetFloatIfExists("UVRotation4", 0.0f);
            activeShader.SetVector4IfExists("UVScaleOffset5", new Vector4(1, 1, 0, 0));
            activeShader.SetIntIfExists("UVIndexLayer5", 0);
            activeShader.SetVector4IfExists("UVScaleOffsetLayerMask", new Vector4(1, 1, 0, 0));
            activeShader.SetVector4IfExists("UVCenterRotationLayerMask", new Vector4(0, 0, 0, 0));

            activeShader.SetVector4IfExists("UVScaleOffsetInsideEmissionParallaxHeight", new Vector4(1, 1, 0, 0));
            activeShader.SetVector4IfExists("UVScaleOffsetInsideEmissionParallaxIntensity", new Vector4(1, 1, 0, 0));
            activeShader.SetIntIfExists("UVIndexInsideEmissionParallaxHeight", 0);
            activeShader.SetIntIfExists("UVIndexInsideEmissionParallaxIntensity", 0);

            activeShader.SetFloatIfExists("InsideEmissionParallaxIntensityMapRangeAvoidEdge", 0.0f);
            activeShader.SetFloatIfExists("InsideEmissionParallaxIntensityMapLatticeU", 1.0f);
            activeShader.SetFloatIfExists("InsideEmissionParallaxIntensityMapLatticeV", 1.0f);
            activeShader.SetFloatIfExists("InsideEmissionParallaxHeight", 0.0f);
            activeShader.SetFloatIfExists("InsideEmissionParallaxOffset", 0.0f);
            activeShader.SetFloatIfExists("InsideEmissionParallaxIntensity", 1.0f);
            activeShader.SetFloatIfExists("InsideEmissionParallaxF0", 1.0f);
            activeShader.SetFloatIfExists("InsideEmissionParallaxF90", 0.0f);
            activeShader.SetFloatIfExists("EnableIEPTexture1", 1.0f);
            activeShader.SetFloatIfExists("EnableIEPTexture2", 1.0f);
            activeShader.SetFloatIfExists("EnableIEPTexture3", 1.0f);
            activeShader.SetFloatIfExists("EnableIEPTexture4", 1.0f);

            // EyeClearCoatForward uses an explicit alpha control; if a TRMTR omits it, the linked-program default is 0,
            // which makes the material fully transparent in the forward pass.
            activeShader.SetFloatIfExists("AlbedoAlpha", 1.0f);
            activeShader.SetFloatIfExists("IOR", 1.33f);
            activeShader.SetFloatIfExists("ParallaxInside", 0.0f);
            activeShader.SetFloatIfExists("EmissionStrength", 0.0f);

            activeShader.SetVector4IfExists("EmissionColor", Vector4.One);
            activeShader.SetVector4IfExists("EmissionColorLayer1", Vector4.Zero);
            activeShader.SetVector4IfExists("EmissionColorLayer2", Vector4.Zero);
            activeShader.SetVector4IfExists("EmissionColorLayer3", Vector4.Zero);
            activeShader.SetVector4IfExists("EmissionColorLayer4", Vector4.Zero);
            activeShader.SetVector4IfExists("EmissionColorLayer5", Vector4.Zero);
            activeShader.SetFloatIfExists("EmissionIntensity", 0.0f);
            activeShader.SetFloatIfExists("EmissionIntensityLayer1", 0.0f);
            activeShader.SetFloatIfExists("EmissionIntensityLayer2", 0.0f);
            activeShader.SetFloatIfExists("EmissionIntensityLayer3", 0.0f);
            activeShader.SetFloatIfExists("EmissionIntensityLayer4", 0.0f);
            activeShader.SetFloatIfExists("EmissionIntensityLayer5", 0.0f);
            activeShader.SetBoolIfExists("EnableLerpBaseColorEmission", false);

            activeShader.SetFloatIfExists("SpecularIntensity", 1.0f);
            activeShader.SetFloatIfExists("SpecularOffset", 0.0f);
            activeShader.SetFloatIfExists("SpecularContrast", 0.0f);
            activeShader.SetFloatIfExists("SpecularLayer1Offset", 0.0f);
            activeShader.SetFloatIfExists("SpecularLayer1Contrast", 0.0f);
            activeShader.SetFloatIfExists("SpecularLayer1Intensity", 1.0f);
            activeShader.SetFloatIfExists("SpecularLayer2Offset", 0.0f);
            activeShader.SetFloatIfExists("SpecularLayer2Contrast", 0.0f);
            activeShader.SetFloatIfExists("SpecularLayer2Intensity", 1.0f);
            activeShader.SetFloatIfExists("SpecularLayer3Offset", 0.0f);
            activeShader.SetFloatIfExists("SpecularLayer3Contrast", 0.0f);
            activeShader.SetFloatIfExists("SpecularLayer3Intensity", 1.0f);
            activeShader.SetFloatIfExists("SpecularLayer4Offset", 0.0f);
            activeShader.SetFloatIfExists("SpecularLayer4Contrast", 0.0f);
            activeShader.SetFloatIfExists("SpecularLayer4Intensity", 1.0f);
            activeShader.SetFloatIfExists("SpecularLayer5Offset", 0.0f);
            activeShader.SetFloatIfExists("SpecularLayer5Contrast", 0.0f);
            activeShader.SetFloatIfExists("SpecularLayer5Intensity", 1.0f);
            activeShader.SetFloatIfExists("Metallic", 0.0f);
            activeShader.SetFloatIfExists("MetallicLayer1", 0.0f);
            activeShader.SetFloatIfExists("MetallicLayer2", 0.0f);
            activeShader.SetFloatIfExists("MetallicLayer3", 0.0f);
            activeShader.SetFloatIfExists("MetallicLayer4", 0.0f);
            activeShader.SetFloatIfExists("MetallicLayer5", 0.0f);
            activeShader.SetFloatIfExists("Roughness", 0.5f);
            activeShader.SetFloatIfExists("MetallicLayer7", 0.0f);
            activeShader.SetFloatIfExists("MetallicLayer8", 0.0f);
            activeShader.SetFloatIfExists("RoughnessLayer7", 0.5f);
            activeShader.SetFloatIfExists("RoughnessLayer8", 0.5f);
            activeShader.SetFloatIfExists("ShadowingShift", 0.0f);
            activeShader.SetFloatIfExists("ShadowingContrast", 0.0f);
            activeShader.SetFloatIfExists("ShadowingBias", 1.0f);
            activeShader.SetFloatIfExists("ShadingBias", 1.0f);
            activeShader.SetFloatIfExists("HalfLambertBias", 0.0f);
            activeShader.SetFloatIfExists("ShadowStrength", 0.0f);
            activeShader.SetFloatIfExists("RimLightOffset", 0.0f);
            activeShader.SetFloatIfExists("RimLightContrast", 0.0f);
            activeShader.SetFloatIfExists("RimLightIntensity", 0.0f);
            activeShader.SetFloatIfExists("BackRimLightIntensity", 0.0f);
            activeShader.SetFloatIfExists("MidAreaShift", 0.0f);
            activeShader.SetFloatIfExists("MidAreaContrast", 0.0f);
            activeShader.SetFloatIfExists("MidAreaHueOffset", 0.0f);
            activeShader.SetFloatIfExists("DarkAreaShift", 0.0f);
            activeShader.SetFloatIfExists("DarkAreaContrast", 0.0f);
            activeShader.SetFloatIfExists("DarkAreaHueOffset", 0.0f);
            activeShader.SetFloatIfExists("HueShiftAreaValue", 0.0f);
            activeShader.SetFloatIfExists("HueShiftBias", 0.0f);
            activeShader.SetFloatIfExists("BaseColorDarkness", 0.0f);
            activeShader.SetFloatIfExists("SaturationPower", 1.0f);
            activeShader.SetFloatIfExists("ReflectionsBlur", 0.0f);
            activeShader.SetFloatIfExists("DiffusionLevels", 0.0f);
            activeShader.SetFloatIfExists("OcclusionStrength", 1.0f);
            activeShader.SetFloatIfExists("NormalHeight", 1.0f);

            activeShader.SetIntIfExists("EyelidType", 0);
            activeShader.SetBoolIfExists("RequireEyelidShadowMap", false);
            activeShader.SetFloatIfExists("EnableJewelMode", 0.0f);

            activeShader.SetBoolIfExists("EnableHolocastEffect", false);
            activeShader.SetVector4IfExists("HolocastColor", Vector4.Zero);
            activeShader.SetFloatIfExists("ScanlineSpeed", 0.0f);
            activeShader.SetFloatIfExists("ScanGradationTiling", 0.0f);
            activeShader.SetFloatIfExists("ScanlineTiling", 0.0f);
            activeShader.SetFloatIfExists("ScanlineDepth", 0.0f);

            activeShader.SetBoolIfExists("EnableMgUpwordNoise", false);
            activeShader.SetIntIfExists("MgUpwordNoiseProjectType", 0);
            activeShader.SetIntIfExists("MgUpwordNoiseBottomCap", 0);
            activeShader.SetIntIfExists("MgUpwordNoiseOutputType", 0);
            activeShader.SetFloatIfExists("NoiseScale", 0.0f);
            activeShader.SetFloatIfExists("NoiseScrollSpeed", 0.0f);
            activeShader.SetFloatIfExists("NoiseIntensity", 0.0f);
            activeShader.SetFloatIfExists("NoiseFadeStart", 0.0f);
            activeShader.SetFloatIfExists("NoiseFadeEnd", 0.0f);
            activeShader.SetVector4IfExists("NoiseBaseColor", Vector4.Zero);
            activeShader.SetFloatIfExists("NoiseLightMin", 0.0f);

            activeShader.SetBoolIfExists("EnableAuraEffect", false);
            activeShader.SetFloatIfExists("AuraIntensity", 0.0f);
            activeShader.SetFloatIfExists("AuraColorScrollSpeed", 0.0f);
            activeShader.SetFloatIfExists("AuraNoiseScrollSpeed", 0.0f);
            activeShader.SetFloatIfExists("AuraColorScale", 0.0f);
            activeShader.SetFloatIfExists("AuraNoiseScale", 0.0f);
            activeShader.SetFloatIfExists("AuraRimPower", 1.0f);
            activeShader.SetVector4IfExists("time_params", Vector4.Zero);

            activeShader.SetFloatIfExists("AlphaTestThreshold", 0.5f);
            activeShader.SetFloatIfExists("DiscardValue", 0.5f);

            activeShader.SetFloatIfExists("LayerMaskScale1", 1.0f);
            activeShader.SetFloatIfExists("LayerMaskScale2", 1.0f);
            activeShader.SetFloatIfExists("LayerMaskScale3", 1.0f);
            activeShader.SetFloatIfExists("LayerMaskScale4", 1.0f);
            activeShader.SetFloatIfExists("LayerMaskScale5", 1.0f);
            activeShader.SetFloatIfExists("LayerMaskScale6", 1.0f);
            activeShader.SetFloatIfExists("LayerMaskScale7", 1.0f);
            activeShader.SetFloatIfExists("LayerMaskScale8", 1.0f);
        }

        private static IReadOnlyList<string> GetTextureNameAliases(string name)
        {
            return name switch
            {
                "OcclusionMap" => new[] { "AOMap" },
                _ => Array.Empty<string>()
            };
        }

    }
}
