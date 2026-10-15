#version 420 core
#include "Common/Surface.glsl"



layout(binding = 0) uniform sampler2D BaseColorMap ;
layout(binding = 1) uniform sampler2D LayerMaskMap;
layout(binding = 2) uniform sampler2D NormalMap ;
layout(binding = 3) uniform sampler2D NormalMap1 ;
layout(binding = 4) uniform sampler2D AOMap;

uniform bool EnableBaseColorMap;
uniform bool EnableNormalMap;
uniform bool EnableParallaxMap;
uniform bool EnableMetallicMap;
uniform bool EnableRoughnessMap;
uniform bool EnableEmissionColorMap;
uniform bool EnableAOMap;
uniform bool EnableAlphaTest;
uniform int NumMaterialLayer;
vec4 sample_BaseColorMap(vec2 uv) { return sample_trinity(BaseColorMap,uv,0); }
vec4 sample_lod_BaseColorMap(vec2 uv,float lod) { return sample_trinity(BaseColorMap,uv,0); }
vec4 sample_LayerMaskMap(vec2 uv) { return sample_trinity(LayerMaskMap,uv,1); }
vec4 sample_lod_LayerMaskMap(vec2 uv,float lod) { return sample_trinity(LayerMaskMap,uv,1); }
vec4 sample_NormalMap(vec2 uv) { return sample_trinity(NormalMap,uv,2); }
vec4 sample_lod_NormalMap(vec2 uv,float lod) { return sample_trinity(NormalMap,uv,2); }
vec4 sample_NormalMap1(vec2 uv) { return sample_trinity(NormalMap1,uv,3); }
vec4 sample_lod_NormalMap1(vec2 uv,float lod) { return sample_trinity(NormalMap1,uv,3); }
vec4 sample_AOMap(vec2 uv) { return sample_trinity(AOMap,uv,4); }
vec4 sample_lod_AOMap(vec2 uv,float lod) { return sample_trinity(AOMap,uv,4); }


#ifdef VERTEX_STAGE
void vertex() {

}
#endif


#ifdef FRAGMENT_STAGE
void fragment() {
	vec2 uvs = vec2(UV.x, 1.0 - UV.y); 
	float ao_factor = 1.0;
	if(EnableAOMap) ao_factor = sample_AOMap( uvs).r; 
	ALBEDO = sample_BaseColorMap( uvs).rgb * ao_factor;
	NORMAL_MAP = sample_NormalMap( uvs).rgb;

    ALPHA = sample_BaseColorMap(vec2(UV.x,1.0-UV.y)).a;
}
#endif
#ifdef VERTEX_STAGE
#include "Common/Vertex.glsl"
#endif
#ifdef FRAGMENT_STAGE
#include "Common/Fragment.glsl"
#endif
