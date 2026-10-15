#version 420 core
#include "Common/Surface.glsl"

layout(binding = 0) uniform sampler2D BaseColorMap ;
layout(binding = 1) uniform sampler2D NormalMap ;
layout(binding = 2) uniform sampler2D PackedMap;
layout(binding = 3) uniform sampler2D EmissionColorMap;
layout(binding = 4) uniform sampler2D MetallicMap;
layout(binding = 5) uniform sampler2D RoughnessMap;
layout(binding = 6) uniform sampler2D AOMap;
layout(binding = 7) uniform sampler2D WindowGradientMap;

uniform bool EnableBaseColorMap;
uniform bool EnableNormalMap;
uniform bool EnableParallaxMap;
uniform bool EnableMetallicMap;
uniform bool EnableRoughnessMap;
uniform bool EnableEmissionColorMap;
uniform bool EnableAOMap;
uniform bool EnablePackedMap;
uniform bool EnableVertexBaseColor;

uniform int CastShadow;
uniform int ReceiveShadow;
uniform int CategoryLabel;
uniform int EnableGlobalWeather;
uniform int EnableDistanceDither;

uniform float EmissionIntensity;
uniform float VertexColorIntensity;

uniform vec4 UVScaleOffset = vec4(1.0,1.0,0.0,0.0);
uniform vec4 EmissionColor = vec4(1.0);
vec4 sample_BaseColorMap(vec2 uv) { return sample_trinity(BaseColorMap,uv,0); }
vec4 sample_lod_BaseColorMap(vec2 uv,float lod) { return sample_trinity(BaseColorMap,uv,0); }
vec4 sample_NormalMap(vec2 uv) { return sample_trinity(NormalMap,uv,1); }
vec4 sample_lod_NormalMap(vec2 uv,float lod) { return sample_trinity(NormalMap,uv,1); }
vec4 sample_PackedMap(vec2 uv) { return sample_trinity(PackedMap,uv,2); }
vec4 sample_lod_PackedMap(vec2 uv,float lod) { return sample_trinity(PackedMap,uv,2); }
vec4 sample_EmissionColorMap(vec2 uv) { return sample_trinity(EmissionColorMap,uv,3); }
vec4 sample_lod_EmissionColorMap(vec2 uv,float lod) { return sample_trinity(EmissionColorMap,uv,3); }
vec4 sample_MetallicMap(vec2 uv) { return sample_trinity(MetallicMap,uv,4); }
vec4 sample_lod_MetallicMap(vec2 uv,float lod) { return sample_trinity(MetallicMap,uv,4); }
vec4 sample_RoughnessMap(vec2 uv) { return sample_trinity(RoughnessMap,uv,5); }
vec4 sample_lod_RoughnessMap(vec2 uv,float lod) { return sample_trinity(RoughnessMap,uv,5); }
vec4 sample_AOMap(vec2 uv) { return sample_trinity(AOMap,uv,6); }
vec4 sample_lod_AOMap(vec2 uv,float lod) { return sample_trinity(AOMap,uv,6); }
vec4 sample_WindowGradientMap(vec2 uv) { return sample_trinity(WindowGradientMap,uv,7); }
vec4 sample_lod_WindowGradientMap(vec2 uv,float lod) { return sample_trinity(WindowGradientMap,uv,7); }


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
	vec2 base_uv = get_uv(UV);
	vec2 uvs = base_uv;


	if (EnableParallaxMap) {
		vec3 view_dir = normalize(normalize(-VERTEX) * mat3(TANGENT, BINORMAL, NORMAL));
		float height = sample_PackedMap( base_uv).a;
		uvs = base_uv + view_dir.xy * (height - 0.5) * 0.04;
	}


	vec4 base_col = vec4(1);
	base_col = sample_BaseColorMap( uvs);


	vec4 norm = vec4(1);
	if(EnableNormalMap)
		norm *= sample_NormalMap( uvs);


	vec4 packed_value = vec4(1);
	if(EnablePackedMap)
		packed_value *= sample_PackedMap( uvs);


	vec3 emission = vec3(1);
	if(EnableEmissionColorMap)
		emission *= (sample_EmissionColorMap( uvs).rgb * EmissionIntensity);


	float metallic = 0.0;
    if (EnableMetallicMap) {
        metallic = sample_MetallicMap( uvs).r;
    }


    float ao = 1.0;
    if (EnableAOMap)
        ao *= pow(sample_AOMap( uvs).r, 0.001);


	float roughness = 1.0;
	if (EnableRoughnessMap)
		roughness = sample_RoughnessMap( uvs).r;

	float specular = 0.5;

	ALBEDO = base_col.rgb;
	if (EnableVertexBaseColor)
		ALBEDO *= COLOR.rgb * VertexColorIntensity;


	NORMAL_MAP = norm.rgb;


	METALLIC = metallic;
	ROUGHNESS = roughness;
	AO = ao;

	if (EnableDistanceDither > 0) {
		float dither = fract(sin(dot(FRAGCOORD.xy, vec2(12.9898, 78.233))) * 43758.5453);
		ALPHA = base_col.a;
		ALPHA_SCISSOR_THRESHOLD = dither;
	}
}
#endif
#ifdef VERTEX_STAGE
#include "Common/Vertex.glsl"
#endif
#ifdef FRAGMENT_STAGE
#include "Common/Fragment.glsl"
#endif
