#version 420 core
#include "Common/Surface.glsl"

layout(binding = 0) uniform sampler2D RoughnessMap;
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

uniform float DepthFadeDistance;
uniform float DiscardValue;
uniform float DissolveEdgeIntensity;
uniform float DissolveEdgeWidth;
uniform float EmissionIntensity;
uniform float FresnelAlphaMax;
uniform float FresnelAlphaMin;
uniform float Metallic;
uniform float NormalHeight;
uniform float RimIntensity;
uniform float RimPower;
uniform float Roughness = 0.5;
uniform float UVRotation;

uniform vec4 BaseColor = vec4(1.0);
uniform vec4 BaseColorLayer1 = vec4(1.0);
uniform vec4 DissolveEdgeColor;
uniform vec4 EmissionColor = vec4(1.0);
uniform vec4 RimColor;
uniform vec4 UVScaleOffset = vec4(1.0,1.0,0.0,0.0);

uniform int AlphaProcess;
uniform int CastShadow;
uniform int CategoryLabel;
uniform int ColorProcess;
uniform int EnableGlobalWeather;
uniform int ReceiveShadow;
uniform int Texture0AlphaChannel;
uniform int Texture0ColorChannel;
uniform int Texture1AlphaChannel;
uniform int Texture1AlphaOp;
uniform int Texture1ColorChannel;
uniform int Texture1ColorOp;
uniform int Texture2AlphaChannel;
uniform int Texture2AlphaOp;
uniform int Texture2ColorChannel;
uniform int Texture2ColorOp;
uniform int VertexAlphaChannel;
uniform int VertexAlphaOp;
uniform int VertexColorChannel;
uniform int VertexColorOp;
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
