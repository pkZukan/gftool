#version 420 core
#include "Common/Surface.glsl"



layout(binding = 0) uniform sampler2D BaseColorMap ;
layout(binding = 1) uniform sampler2D NormalMap ;
layout(binding = 2) uniform sampler2D RoughnessMap;
layout(binding = 3) uniform sampler2D AOMap;
layout(binding = 4) uniform sampler2D SSSMaskMap;

uniform bool EnableBaseColorMap;
uniform bool EnableNormalMap;
uniform bool EnableRoughnessMap;
uniform bool EnableAOMap;
uniform bool EnableSSSMaskMap;
uniform int NumMaterialLayer;
uniform bool EnableVertexSubLayer;
uniform bool EnableCustomLayer;
uniform int LightReceiveType;
vec4 sample_BaseColorMap(vec2 uv) { return sample_trinity(BaseColorMap,uv,0); }
vec4 sample_lod_BaseColorMap(vec2 uv,float lod) { return sample_trinity(BaseColorMap,uv,0); }
vec4 sample_NormalMap(vec2 uv) { return sample_trinity(NormalMap,uv,1); }
vec4 sample_lod_NormalMap(vec2 uv,float lod) { return sample_trinity(NormalMap,uv,1); }
vec4 sample_RoughnessMap(vec2 uv) { return sample_trinity(RoughnessMap,uv,2); }
vec4 sample_lod_RoughnessMap(vec2 uv,float lod) { return sample_trinity(RoughnessMap,uv,2); }
vec4 sample_AOMap(vec2 uv) { return sample_trinity(AOMap,uv,3); }
vec4 sample_lod_AOMap(vec2 uv,float lod) { return sample_trinity(AOMap,uv,3); }
vec4 sample_SSSMaskMap(vec2 uv) { return sample_trinity(SSSMaskMap,uv,4); }
vec4 sample_lod_SSSMaskMap(vec2 uv,float lod) { return sample_trinity(SSSMaskMap,uv,4); }


#ifdef VERTEX_STAGE
void vertex() {

}
#endif


#ifdef FRAGMENT_STAGE
void fragment() {
	vec2 uvs = vec2(UV.x, 1.0 - UV.y); 
	ALBEDO = sample_BaseColorMap( uvs).rgb;
	ALPHA = sample_BaseColorMap( uvs).a * 0.35; ALBEDO = vec3(0.0);
	NORMAL_MAP = sample_NormalMap( uvs).rgb;
}
#endif
#ifdef VERTEX_STAGE
#include "Common/Vertex.glsl"
#endif
#ifdef FRAGMENT_STAGE
#include "Common/Fragment.glsl"
#endif
