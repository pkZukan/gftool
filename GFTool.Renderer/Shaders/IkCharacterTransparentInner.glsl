#version 420 core
#include "Common/Surface.glsl"

layout(binding = 0) uniform sampler2D RoughnessMap;
uniform float Roughness = 0.5;
uniform int NumMaterialLayer;
layout(binding = 1) uniform sampler2D NormalMap;
layout(binding = 2) uniform sampler2D MetallicMap;
layout(binding = 3) uniform sampler2D LayerMaskMap;
uniform bool EnableRoughnessMap;
uniform bool EnableNormalMap;
uniform bool EnableMetallicMap;
uniform bool EnableEmissionColorMap;
uniform bool EnableBaseColorMap;
uniform bool EnableAOMap;
uniform bool EnableAlphaTest;
layout(binding = 4) uniform sampler2D EmissionColorMap;
layout(binding = 5) uniform sampler2D BaseColorMap;
layout(binding = 6) uniform sampler2D AOMap;

uniform float BackRimLightIntensity;
uniform float BaseColorDarkness;
uniform float DarkAreaContrast;
uniform float DarkAreaHueOffset;
uniform float DarkAreaShift;
uniform float DiffusionLevels;
uniform float DiscardValue;
uniform float EmissionIntensity;
uniform float EmissionIntensityLayer1;
uniform float EmissionIntensityLayer2;
uniform float EmissionIntensityLayer3;
uniform float EmissionIntensityLayer4;
uniform float FresnelAlphaMax;
uniform float FresnelAlphaMin;
uniform float FresnelAngleBias;
uniform float HalfLambertBias;
uniform float HueShiftAreaValue;
uniform float HueShiftBias;
uniform float LayerMaskScale1;
uniform float LayerMaskScale2;
uniform float LayerMaskScale3;
uniform float LayerMaskScale4;
uniform float Metallic;
uniform float MetallicLayer1;
uniform float MetallicLayer2;
uniform float MetallicLayer3;
uniform float MetallicLayer4;
uniform float MidAreaContrast;
uniform float MidAreaHueOffset;
uniform float MidAreaShift;
uniform float NormalHeight;
uniform float OcclusionStrength;
uniform float ReflectionsBlur;
uniform float RimLightContrast;
uniform float RimLightIntensity;
uniform float RimLightOffset;
uniform float SaturationPower;
uniform float ShadingBias;
uniform float ShadowStrength;
uniform float ShadowingBias;
uniform float ShadowingColorMaskMapValue;
uniform float ShadowingContrast;
uniform float ShadowingGIGain;
uniform float ShadowingShift;
uniform float SpecularContrast;
uniform float SpecularIntensity;
uniform float SpecularLayer1Contrast;
uniform float SpecularLayer1Intensity;
uniform float SpecularLayer1Offset;
uniform float SpecularLayer2Contrast;
uniform float SpecularLayer2Intensity;
uniform float SpecularLayer2Offset;
uniform float SpecularLayer3Contrast;
uniform float SpecularLayer3Intensity;
uniform float SpecularLayer3Offset;
uniform float SpecularLayer4Contrast;
uniform float SpecularLayer4Intensity;
uniform float SpecularLayer4Offset;
uniform float SpecularOffset;


uniform vec4 BaseColor = vec4(1.0);
uniform vec4 BaseColorLayer1 = vec4(1.0);
uniform vec4 BaseColorLayer2 = vec4(1.0);
uniform vec4 BaseColorLayer3 = vec4(1.0);
uniform vec4 BaseColorLayer4 = vec4(1.0);
uniform vec4 EmissionColor = vec4(1.0);
uniform vec4 EmissionColorLayer1 = vec4(1.0);
uniform vec4 EmissionColorLayer2 = vec4(1.0);
uniform vec4 EmissionColorLayer3 = vec4(1.0);
uniform vec4 EmissionColorLayer4 = vec4(1.0);
uniform vec4 ShadowingColor;
uniform vec4 ShadowingColorLayer1;
uniform vec4 ShadowingColorLayer2;
uniform vec4 ShadowingColorLayer3;
uniform vec4 ShadowingColorLayer4;
uniform vec4 UVScaleOffset = vec4(1.0,1.0,0.0,0.0);


uniform int CastShadow;
uniform int CategoryLabel;
uniform int EnableAdditionalLight;
uniform int EnableDistanceDither;
uniform int EnableGlobalWeather;
uniform int ReceiveShadow;
uniform int UVIndexAO;
uniform int UVIndexLayerMask;
vec4 sample_RoughnessMap(vec2 uv) { return sample_trinity(RoughnessMap,uv,0); }
vec4 sample_lod_RoughnessMap(vec2 uv,float lod) { return sample_trinity(RoughnessMap,uv,0); }
vec4 sample_NormalMap(vec2 uv) { return sample_trinity(NormalMap,uv,1); }
vec4 sample_lod_NormalMap(vec2 uv,float lod) { return sample_trinity(NormalMap,uv,1); }
vec4 sample_MetallicMap(vec2 uv) { return sample_trinity(MetallicMap,uv,2); }
vec4 sample_lod_MetallicMap(vec2 uv,float lod) { return sample_trinity(MetallicMap,uv,2); }
vec4 sample_LayerMaskMap(vec2 uv) { return sample_trinity(LayerMaskMap,uv,3); }
vec4 sample_lod_LayerMaskMap(vec2 uv,float lod) { return sample_trinity(LayerMaskMap,uv,3); }
vec4 sample_EmissionColorMap(vec2 uv) { return sample_trinity(EmissionColorMap,uv,4); }
vec4 sample_lod_EmissionColorMap(vec2 uv,float lod) { return sample_trinity(EmissionColorMap,uv,4); }
vec4 sample_BaseColorMap(vec2 uv) { return sample_trinity(BaseColorMap,uv,5); }
vec4 sample_lod_BaseColorMap(vec2 uv,float lod) { return sample_trinity(BaseColorMap,uv,5); }
vec4 sample_AOMap(vec2 uv) { return sample_trinity(AOMap,uv,6); }
vec4 sample_lod_AOMap(vec2 uv,float lod) { return sample_trinity(AOMap,uv,6); }














#ifdef VERTEX_STAGE
void vertex()
{

}
#endif


#ifdef FRAGMENT_STAGE
void fragment() {
    vec2 uv = UVScaleOffset.xy * (UV - UVScaleOffset.zw); uv.y = 1.0 - uv.y;
    vec4 color = BaseColor * sample_BaseColorMap( uv);
    ALBEDO = color.rgb; ALPHA = color.a;
    if (EnableAlphaTest && ALPHA < DiscardValue) discard;
    if (EnableNormalMap) NORMAL_MAP = sample_NormalMap( uv).rgb;
    METALLIC = EnableMetallicMap ? sample_MetallicMap( uv).r : Metallic;
    ROUGHNESS = EnableRoughnessMap ? sample_RoughnessMap( uv).r : Roughness;
    AO = EnableAOMap ? sample_AOMap( uv).r : 1.0;
    EMISSION = EmissionColor.rgb * EmissionIntensity;
    if (EnableEmissionColorMap) EMISSION *= sample_EmissionColorMap( uv).rgb;
}
#endif
#ifdef VERTEX_STAGE
#include "Common/Vertex.glsl"
#endif
#ifdef FRAGMENT_STAGE
#include "Common/Fragment.glsl"
#endif
