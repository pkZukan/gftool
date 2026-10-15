#version 420 core
#include "Common/Surface.glsl"

layout(binding = 0) uniform sampler2D BaseColorMap1 ;
layout(binding = 1) uniform sampler2D NormalMap ;
layout(binding = 2) uniform sampler2D NormalMap1 ;
layout(binding = 3) uniform sampler2D PackedMap;
layout(binding = 4) uniform sampler2D GrayAlbedoMap;
layout(binding = 5) uniform sampler2D EmissionMaskMap;

uniform bool EnableVertexBaseColor;
uniform bool EnableVertexColor;
uniform bool NumMaterialLayer;
uniform bool EnableTilingBlend;
uniform bool EnableLightMap;
uniform bool EnableGrayAlbedoMap;
uniform bool EnableEmissionMaskMap;
uniform bool EnableAlphaTest;

uniform float EmissionIntensity;
uniform float VertexColorIntensity;
uniform float MetallicIntensity;

uniform vec4 UVScaleOffset = vec4(1.0,1.0,0.0,0.0);
uniform vec4 EmissionColor = vec4(1.0);
vec4 sample_BaseColorMap1(vec2 uv) { return sample_trinity(BaseColorMap1,uv,0); }
vec4 sample_lod_BaseColorMap1(vec2 uv,float lod) { return sample_trinity(BaseColorMap1,uv,0); }
vec4 sample_NormalMap(vec2 uv) { return sample_trinity(NormalMap,uv,1); }
vec4 sample_lod_NormalMap(vec2 uv,float lod) { return sample_trinity(NormalMap,uv,1); }
vec4 sample_NormalMap1(vec2 uv) { return sample_trinity(NormalMap1,uv,2); }
vec4 sample_lod_NormalMap1(vec2 uv,float lod) { return sample_trinity(NormalMap1,uv,2); }
vec4 sample_PackedMap(vec2 uv) { return sample_trinity(PackedMap,uv,3); }
vec4 sample_lod_PackedMap(vec2 uv,float lod) { return sample_trinity(PackedMap,uv,3); }
vec4 sample_GrayAlbedoMap(vec2 uv) { return sample_trinity(GrayAlbedoMap,uv,4); }
vec4 sample_lod_GrayAlbedoMap(vec2 uv,float lod) { return sample_trinity(GrayAlbedoMap,uv,4); }
vec4 sample_EmissionMaskMap(vec2 uv) { return sample_trinity(EmissionMaskMap,uv,5); }
vec4 sample_lod_EmissionMaskMap(vec2 uv,float lod) { return sample_trinity(EmissionMaskMap,uv,5); }


#ifdef VERTEX_STAGE
void vertex() {

}
#endif


vec2 xform_uvs(vec2 uvs, vec4 scale_off)
{
	vec2 v = scale_off.xy * (uvs - scale_off.zw);
	return vec2(v.x, 1.0 - v.y);
}

vec2 get_uv(vec2 uvs) 
{
    return xform_uvs(uvs, UVScaleOffset);
}

#ifdef FRAGMENT_STAGE
void fragment() {
	vec2 uvs = get_uv(UV);


	vec4 baseColMap1 = sample_BaseColorMap1( uvs);


	vec3 norm = vec3(1,1,1);
		norm *= sample_NormalMap( uvs).rgb;
		norm *= sample_NormalMap1( uvs).rgb;


	vec3 packed_value = sample_PackedMap( uvs).rgb;


	vec3 emission = sample_EmissionMaskMap( uvs).rgb;
	emission *= EmissionIntensity;

	float specular = 0.5;

	ALBEDO = baseColMap1.rgb * packed_value;
	if(EnableVertexBaseColor)
		ALBEDO *= (COLOR.rgb * VertexColorIntensity);
	NORMAL_MAP = norm;
	SPECULAR = specular;
	EMISSION = emission;
}
#endif
#ifdef VERTEX_STAGE
#include "Common/Vertex.glsl"
#endif
#ifdef FRAGMENT_STAGE
#include "Common/Fragment.glsl"
#endif
