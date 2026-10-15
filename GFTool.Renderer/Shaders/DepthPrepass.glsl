#version 420 core
#include "Common/Surface.glsl"

layout(binding = 0) uniform sampler2D BaseColorMap ;
layout(binding = 1) uniform sampler2D NormalMap ;

uniform float VertexColorIntensity;
uniform float DiscardValue;
uniform float NormalHeight;
uniform float Metallic;
uniform float Roughness = 0.5;
uniform float EmissionIntensity;

uniform vec4 UVScaleOffset = vec4(1.0,1.0,0.0,0.0);
vec4 sample_BaseColorMap(vec2 uv) { return sample_trinity(BaseColorMap,uv,0); }
vec4 sample_lod_BaseColorMap(vec2 uv,float lod) { return sample_trinity(BaseColorMap,uv,0); }
vec4 sample_NormalMap(vec2 uv) { return sample_trinity(NormalMap,uv,1); }
vec4 sample_lod_NormalMap(vec2 uv,float lod) { return sample_trinity(NormalMap,uv,1); }

#ifdef VERTEX_STAGE
void vertex() {

}
#endif


#ifdef FRAGMENT_STAGE
void fragment() {
	vec2 uvs = vec2(UV.x, 1.0 - UV.y); 
	vec4 albedo = sample_BaseColorMap( uvs);
	ALBEDO = albedo.rgb;
	ALPHA = albedo.a;

    ALPHA_SCISSOR_THRESHOLD = DiscardValue > 0.0 ? DiscardValue : 0.5;
}
#endif
#ifdef VERTEX_STAGE
#include "Common/Vertex.glsl"
#endif
#ifdef FRAGMENT_STAGE
#include "Common/Fragment.glsl"
#endif
