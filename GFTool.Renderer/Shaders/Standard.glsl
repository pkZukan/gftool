#version 420 core
#include "Common/Surface.glsl"



layout(binding = 0) uniform sampler2D BaseColorMap ;
layout(binding = 1) uniform sampler2D BaseColorMap1 ;
layout(binding = 2) uniform sampler2D NormalMap ;
layout(binding = 3) uniform sampler2D NormalMap1 ;
layout(binding = 4) uniform sampler2D NormalMap2 ;
layout(binding = 5) uniform sampler2D MetallicMap;
layout(binding = 6) uniform sampler2D RoughnessMap;
layout(binding = 7) uniform sampler2D RoughnessMap1;
layout(binding = 8) uniform sampler2D AOMap;
layout(binding = 9) uniform sampler2D AOMap1;
layout(binding = 10) uniform sampler2D AOMap2;
layout(binding = 11) uniform sampler2D LayerMaskMap;
layout(binding = 12) uniform sampler2D ParallaxMap;
layout(binding = 13) uniform sampler2D EmissionColorMap;
layout(binding = 14) uniform sampler2D LightMap;
layout(binding = 15) uniform sampler2D WindMaskMap;

uniform bool EnableBaseColorMap;
uniform bool EnableNormalMap;
uniform bool EnableParallaxMap;
uniform bool EnableMetallicMap;
uniform bool EnableRoughnessMap;
uniform bool EnableEmissionColorMap;
uniform bool EnableAOMap;
uniform bool EnableAlphaTest;
uniform bool EnableLightMap;
uniform int NumMaterialLayer;
uniform int BillboardType;
uniform int WindReceiverType;
uniform bool EnableWindMaskMap;
uniform bool EnableBaseColorMap1;
uniform bool EnableNormalMap1;
uniform bool EnableNormalMap2;
uniform bool EnableRoughnessMap1;
uniform bool EnableAOMap1;
uniform bool EnableAOMap2;
uniform int LayerMaskSource;
uniform int LayerMaskSwizzle;
uniform int LayerBaseMaskSource;
uniform bool EnableVertexBaseColor;
uniform int NumDetailLayer;
uniform bool EnableDepthFade;
uniform bool ReceiveShadow;
uniform bool EnableDeferredRendering;
uniform int InstancingType;
uniform bool EnableGrassCollisionMap;
uniform int NumRequiredUV;
uniform bool EnableLerpBaseColorEmission;
uniform int LayerBlendType;
uniform bool EnableTriplanar;
uniform bool EnablePackedMap;
uniform bool EnableDisplacementMap;


uniform float EmissionIntensity;


uniform vec4 UVScaleOffset = vec4(1.0,1.0,0.0,0.0);
uniform vec4 EmissionColor = vec4(1.0);
vec4 sample_BaseColorMap(vec2 uv) { return sample_trinity(BaseColorMap,uv,0); }
vec4 sample_lod_BaseColorMap(vec2 uv,float lod) { return sample_trinity(BaseColorMap,uv,0); }
vec4 sample_BaseColorMap1(vec2 uv) { return sample_trinity(BaseColorMap1,uv,1); }
vec4 sample_lod_BaseColorMap1(vec2 uv,float lod) { return sample_trinity(BaseColorMap1,uv,1); }
vec4 sample_NormalMap(vec2 uv) { return sample_trinity(NormalMap,uv,2); }
vec4 sample_lod_NormalMap(vec2 uv,float lod) { return sample_trinity(NormalMap,uv,2); }
vec4 sample_NormalMap1(vec2 uv) { return sample_trinity(NormalMap1,uv,3); }
vec4 sample_lod_NormalMap1(vec2 uv,float lod) { return sample_trinity(NormalMap1,uv,3); }
vec4 sample_NormalMap2(vec2 uv) { return sample_trinity(NormalMap2,uv,4); }
vec4 sample_lod_NormalMap2(vec2 uv,float lod) { return sample_trinity(NormalMap2,uv,4); }
vec4 sample_MetallicMap(vec2 uv) { return sample_trinity(MetallicMap,uv,5); }
vec4 sample_lod_MetallicMap(vec2 uv,float lod) { return sample_trinity(MetallicMap,uv,5); }
vec4 sample_RoughnessMap(vec2 uv) { return sample_trinity(RoughnessMap,uv,6); }
vec4 sample_lod_RoughnessMap(vec2 uv,float lod) { return sample_trinity(RoughnessMap,uv,6); }
vec4 sample_RoughnessMap1(vec2 uv) { return sample_trinity(RoughnessMap1,uv,7); }
vec4 sample_lod_RoughnessMap1(vec2 uv,float lod) { return sample_trinity(RoughnessMap1,uv,7); }
vec4 sample_AOMap(vec2 uv) { return sample_trinity(AOMap,uv,8); }
vec4 sample_lod_AOMap(vec2 uv,float lod) { return sample_trinity(AOMap,uv,8); }
vec4 sample_AOMap1(vec2 uv) { return sample_trinity(AOMap1,uv,9); }
vec4 sample_lod_AOMap1(vec2 uv,float lod) { return sample_trinity(AOMap1,uv,9); }
vec4 sample_AOMap2(vec2 uv) { return sample_trinity(AOMap2,uv,10); }
vec4 sample_lod_AOMap2(vec2 uv,float lod) { return sample_trinity(AOMap2,uv,10); }
vec4 sample_LayerMaskMap(vec2 uv) { return sample_trinity(LayerMaskMap,uv,11); }
vec4 sample_lod_LayerMaskMap(vec2 uv,float lod) { return sample_trinity(LayerMaskMap,uv,11); }
vec4 sample_ParallaxMap(vec2 uv) { return sample_trinity(ParallaxMap,uv,12); }
vec4 sample_lod_ParallaxMap(vec2 uv,float lod) { return sample_trinity(ParallaxMap,uv,12); }
vec4 sample_EmissionColorMap(vec2 uv) { return sample_trinity(EmissionColorMap,uv,13); }
vec4 sample_lod_EmissionColorMap(vec2 uv,float lod) { return sample_trinity(EmissionColorMap,uv,13); }
vec4 sample_LightMap(vec2 uv) { return sample_trinity(LightMap,uv,14); }
vec4 sample_lod_LightMap(vec2 uv,float lod) { return sample_trinity(LightMap,uv,14); }
vec4 sample_WindMaskMap(vec2 uv) { return sample_trinity(WindMaskMap,uv,15); }
vec4 sample_lod_WindMaskMap(vec2 uv,float lod) { return sample_trinity(WindMaskMap,uv,15); }



const float PARALLAX_DEPTH = 0.04;
const float WIND_AMPLITUDE = 0.025;

vec2 get_uv(vec2 uv) 
{
	vec2 transformed = UVScaleOffset.xy * (uv - UVScaleOffset.zw);
	return vec2(transformed.x, 1.0 - transformed.y);
}

#ifdef VERTEX_STAGE
void vertex() 
{
	if (WindReceiverType != 0) 
	{
		float mask = 1.0;
		if (EnableWindMaskMap)
			mask = sample_lod_WindMaskMap( get_uv(UV), 0.0).r;

		vec3 world_position = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
		float sway = sin(TIME * 1.7 + dot(world_position.xz, vec2(0.7, 0.5)));
		VERTEX.x += sway * mask * WIND_AMPLITUDE;
	}
}
#endif


vec3 decode_normal(vec3 encoded) 
{
	vec2 xy = encoded.xy * 2.0 - 1.0;
	return normalize(vec3(xy, sqrt(max(1.0 - dot(xy, xy), 0.0001))));
}


vec3 blend_detail_normal(vec3 base, vec3 detail) 
{
	return normalize(vec3(base.xy + detail.xy, base.z * detail.z));
}

#ifdef FRAGMENT_STAGE
void fragment() 
{
	vec2 uvs = get_uv(UV);
	if (EnableParallaxMap) {
		vec3 view_tangent = VIEW * mat3(normalize(TANGENT), normalize(BINORMAL), normalize(NORMAL));
		float height = sample_ParallaxMap( uvs).r - 0.5;

		uvs -= vec2(view_tangent.x, -view_tangent.y) * height * PARALLAX_DEPTH
			/ max(abs(view_tangent.z), 0.2);
	}



	vec4 mask = vec4(0.0);
	if (NumMaterialLayer > 1 || NumDetailLayer > 0) 
	{
		if (LayerMaskSource == 1) 
		{
			mask = COLOR;
		} 
		else 
		{
			mask = sample_LayerMaskMap( uvs);
		}
	}

	if (LayerMaskSwizzle == 1)
		mask = vec4(mask.r);
	else if (LayerMaskSwizzle == 2)
		mask = vec4(mask.g);
	else if (LayerMaskSwizzle == 3)
		mask = vec4(mask.b);
	else if (LayerMaskSwizzle == 4)
		mask = vec4(mask.a);
	mask = clamp(mask, vec4(0.0), vec4(1.0));
	float layer_weight = NumMaterialLayer > 1 ? mask.r : 0.0;

	vec4 base = vec4(1.0);
	if (EnableBaseColorMap) {
		base = sample_BaseColorMap( uvs);
	}

	if (LayerBaseMaskSource == 1) 
		layer_weight *= base.a; 
	if (EnableBaseColorMap1 && NumMaterialLayer > 1) 
	{
		vec4 layer = sample_BaseColorMap1( uvs);

		if (LayerBlendType == 1) { base.rgb += layer.rgb * layer_weight; }
		else if (LayerBlendType == 2) { base.rgb *= mix(vec3(1.0), layer.rgb, layer_weight); }
		else { base = mix(base, layer, layer_weight); }
	}
	if (EnableVertexBaseColor) 
	{
		base *= COLOR;
	}
	if (EnableAlphaTest && base.a < 0.5) 
	{
		discard;
	}

	vec3 normal = vec3(0.0, 0.0, 1.0);
	if (EnableNormalMap)
		normal = decode_normal(sample_NormalMap( uvs).rgb); 

	if (EnableNormalMap1) 
	{
		vec3 layer_normal = decode_normal(sample_NormalMap1( uvs).rgb);
		if (NumDetailLayer > 0) 
		{
			normal = blend_detail_normal(normal, normalize(mix(vec3(0.0, 0.0, 1.0), layer_normal, mask.r)));
		} 
		else if (NumMaterialLayer > 1) 
		{
			normal = normalize(mix(normal, layer_normal, layer_weight));
		}
	}
	if (EnableNormalMap2) {
		vec3 layer_normal = decode_normal(sample_NormalMap2( uvs).rgb);
		if (NumDetailLayer > 1)
		{
			normal = blend_detail_normal(normal, normalize(mix(vec3(0.0, 0.0, 1.0), layer_normal, mask.g)));
		} 
		else if (NumMaterialLayer > 2) 
		{
			normal = normalize(mix(normal, layer_normal, mask.g));
		}
	}

	float metallic = 0.0;
	if (EnableMetallicMap)
		metallic = sample_MetallicMap( uvs).r;

	float roughness = 1.0;
	if (EnableRoughnessMap)
		roughness = sample_RoughnessMap( uvs).r; 

	if (EnableRoughnessMap1 && (NumMaterialLayer > 1 || NumDetailLayer > 0)) 
	{
		float weight = NumDetailLayer > 0 ? mask.r : layer_weight;
		roughness = mix(roughness, sample_RoughnessMap1( uvs).r, weight);
	}
	float ao = 1.0;
	if (EnableAOMap)
		ao = sample_AOMap( uvs).r;
	if (EnableAOMap1) 
	{
		float layer_ao = sample_AOMap1( uvs).r;
		if (NumDetailLayer > 0)
			ao *= mix(1.0, layer_ao, mask.r);
		else if (NumMaterialLayer > 1)
			ao = mix(ao, layer_ao, layer_weight);
	}
	if (EnableAOMap2) 
	{
		float layer_ao = sample_AOMap2( uvs).r;
		if (NumDetailLayer > 1)
			ao *= mix(1.0, layer_ao, mask.g);
		else if (NumMaterialLayer > 2)
			ao = mix(ao, layer_ao, mask.g);
	}

	vec3 emission = vec3(0.0);
	if (EnableEmissionColorMap) 
	{
		vec4 emission_sample = sample_EmissionColorMap( uvs);
		vec3 tint = EmissionColor.rgb;
		if (EnableLerpBaseColorEmission) 
		{
			tint = mix(base.rgb, tint, emission_sample.a); 
		}
		emission = emission_sample.rgb * tint * EmissionIntensity;
	}
	if (EnableLightMap) 
	{
		vec2 light_uv = NumRequiredUV > 1 ? vec2(UV2.x, 1.0 - UV2.y) : uvs;
		emission += base.rgb * sample_LightMap( light_uv).rgb * ao;
	}

	ALBEDO = base.rgb;
	NORMAL_MAP = normal * 0.5 + 0.5;
	METALLIC = clamp(metallic, 0.0, 1.0);
	ROUGHNESS = clamp(roughness, 0.0, 1.0);
	SPECULAR = 0.5;
	EMISSION = emission;
	AO = clamp(ao, 0.0, 1.0);
}
#endif
#ifdef VERTEX_STAGE
#include "Common/Vertex.glsl"
#endif
#ifdef FRAGMENT_STAGE
#include "Common/Fragment.glsl"
#endif
