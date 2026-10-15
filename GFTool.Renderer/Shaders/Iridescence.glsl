#version 420 core
#include "Common/Surface.glsl"

layout(binding = 0) uniform sampler2D RoughnessMap;
uniform float Roughness = 0.5;
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
layout(binding = 5) uniform sampler2D BaseColorMap;
uniform vec4 BaseColor = vec4(1.0);
layout(binding = 6) uniform sampler2D AOMap;

uniform float DiscardValue;
uniform float EmissionIntensity;
uniform float EmissionIntensityLayer1;
uniform float EmissionIntensityLayer2;
uniform float EmissionIntensityLayer3;
uniform float EmissionIntensityLayer4;
uniform float IridescenceAOBlend;
uniform float IridescenceBlend;
uniform float IridescenceBlend1;
uniform float IridescenceBlend2;
uniform float IridescenceBlend3;
uniform float IridescenceBlend4;
uniform float IridescenceCenter;
uniform float IridescenceEnd;
uniform float IridescenceFresnel;
uniform float IridescencePower;
uniform float LayerMaskScale1;
uniform float LayerMaskScale2;
uniform float LayerMaskScale3;
uniform float LayerMaskScale4;
uniform float MetallicLayer1;
uniform float MetallicLayer2;
uniform float MetallicLayer3;
uniform float MetallicLayer4;
uniform float NormalHeight;
uniform float RoughnessLayer1;
uniform float RoughnessLayer2;
uniform float RoughnessLayer3;
uniform float RoughnessLayer4;


uniform vec4 BaseColorLayer1 = vec4(1.0);
uniform vec4 BaseColorLayer2 = vec4(1.0);
uniform vec4 BaseColorLayer3 = vec4(1.0);
uniform vec4 BaseColorLayer4 = vec4(1.0);
uniform vec4 EmissionColor = vec4(1.0);
uniform vec4 EmissionColorLayer1 = vec4(1.0);
uniform vec4 EmissionColorLayer2 = vec4(1.0);
uniform vec4 EmissionColorLayer3 = vec4(1.0);
uniform vec4 EmissionColorLayer4 = vec4(1.0);
uniform vec4 IridescenceColor1;
uniform vec4 IridescenceColor2;
uniform vec4 IridescenceColor3;
uniform vec4 UVScaleOffset = vec4(1.0,1.0,0.0,0.0);
uniform vec4 UVScaleOffsetNormal = vec4(1.0,1.0,0.0,0.0);


uniform int CastShadow;
uniform int CategoryLabel;
uniform int EnableGlobalWeather;
uniform int EnableJewel;
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
    float edge = pow(clamp(1.0-dot(NORMAL,VIEW),0.0,1.0),2.0); ALBEDO = color.rgb * mix(vec3(1.0), vec3(0.5+0.5*sin(edge*6.28),0.5+0.5*sin(edge*6.28+2.09),0.5+0.5*sin(edge*6.28+4.18)),0.5); ALPHA = color.a;
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
