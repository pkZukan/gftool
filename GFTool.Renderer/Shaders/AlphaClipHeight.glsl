#version 420 core
#include "Common/Surface.glsl"

layout(binding = 0) uniform sampler2D BaseColorMap ;
layout(binding = 1) uniform sampler2D NormalMap;
layout(binding = 2) uniform sampler2D RoughnessMap;

uniform float NormalIntensity;
uniform float RoughnessIntensity;
uniform float Metallic;
uniform float VertexColorContrast;
uniform float HeightContrast;
uniform float DiscardValue;

uniform vec4 UVScaleOffset = vec4(1.0,1.0,0.0,0.0);

uniform int CastShadow;
uniform int ReceiveShadow;
uniform int CategoryLabel;
uniform int EnableGlobalWeather;
uniform int EnableDistanceDither;
vec4 sample_BaseColorMap(vec2 uv) { return sample_trinity(BaseColorMap,uv,0); }
vec4 sample_lod_BaseColorMap(vec2 uv,float lod) { return sample_trinity(BaseColorMap,uv,0); }
vec4 sample_NormalMap(vec2 uv) { return sample_trinity(NormalMap,uv,1); }
vec4 sample_lod_NormalMap(vec2 uv,float lod) { return sample_trinity(NormalMap,uv,1); }
vec4 sample_RoughnessMap(vec2 uv) { return sample_trinity(RoughnessMap,uv,2); }
vec4 sample_lod_RoughnessMap(vec2 uv,float lod) { return sample_trinity(RoughnessMap,uv,2); }


#ifdef VERTEX_STAGE
void vertex() 
{

}
#endif


#ifdef FRAGMENT_STAGE
void fragment() 
{
	vec2 uvs = vec2(UV.x, 1.0 - UV.y); 
	vec4 albedo = sample_BaseColorMap( uvs);
	ALBEDO = albedo.rgb;
	ALPHA = albedo.a;
	NORMAL_MAP = sample_NormalMap( uvs).rgb;

    ALPHA_SCISSOR_THRESHOLD = DiscardValue > 0.0 ? DiscardValue : 0.5;
}
#endif
#ifdef VERTEX_STAGE
#include "Common/Vertex.glsl"
#endif
#ifdef FRAGMENT_STAGE
#include "Common/Fragment.glsl"
#endif
