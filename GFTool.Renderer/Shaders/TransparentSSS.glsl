#version 420 core
#include "Common/Surface.glsl"

layout(binding = 0) uniform sampler2D RoughnessMap;
uniform int NumMaterialLayer;
layout(binding = 1) uniform sampler2D NormalMap;
layout(binding = 2) uniform sampler2D MetallicMap;
uniform float Metallic;
layout(binding = 3) uniform sampler2D LayerMaskMap;
uniform bool EnableRoughnessMap;
uniform bool EnableNormalMap;
uniform bool EnableMetallicMap;
uniform bool EnableEmissionColorMap;
uniform bool EnableBaseColorMap;
uniform bool EnableAOMap;
uniform bool EnableAlphaTest;
layout(binding = 4) uniform sampler2D EmissionColorMap;
uniform float DiscardValue;
layout(binding = 5) uniform sampler2D BaseColorMap;
layout(binding = 6) uniform sampler2D AOMap;

uniform float EmissionIntensity;
uniform float EmissionIntensityLayer1;
uniform float EmissionIntensityLayer2;
uniform float EmissionIntensityLayer3;
uniform float EmissionIntensityLayer4;
uniform float LayerMaskScale1;
uniform float LayerMaskScale2;
uniform float LayerMaskScale3;
uniform float LayerMaskScale4;
uniform float NormalHeight;
uniform float Reflectance;
uniform float Roughness = 0.5;
uniform float RoughnessLayer1;
uniform float RoughnessLayer2;
uniform float RoughnessLayer3;
uniform float RoughnessLayer4;
uniform float SSSMaskOffset;
uniform float SSSMaskScale;

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
uniform vec4 SubsurfaceColor = vec4(1.0);
uniform vec4 SubsurfaceColorLayer1 = vec4(1.0);
uniform vec4 SubsurfaceColorLayer2 = vec4(1.0);
uniform vec4 SubsurfaceColorLayer3 = vec4(1.0);
uniform vec4 SubsurfaceColorLayer4 = vec4(1.0);
uniform vec4 UVScaleOffset = vec4(1.0,1.0,0.0,0.0);
uniform vec4 UVScaleOffsetNormal = vec4(1.0,1.0,0.0,0.0);

uniform int CastShadow;
uniform int CategoryLabel;
uniform int EnableAdditionalLight;
uniform int EnableGlobalWeather;
uniform int EnableJewel;
uniform int ReceiveShadow;
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
