#version 420 core
#include "Common/Surface.glsl"

layout(binding = 0) uniform sampler2D FlowMap;
layout(binding = 1) uniform sampler2D FoamMaskMap;
layout(binding = 2) uniform sampler2D NormalMap ;
layout(binding = 3) uniform sampler2D NormalMap1 ;

uniform bool EnableBaseColorMap;
uniform bool EnableBaseColorMap1;
uniform bool EnableNormalMap;
uniform bool EnableNormalMap1;
uniform bool EnableFlowMap;
uniform bool EnableParallaxMap;
uniform bool EnableMetallicMap;
uniform bool EnableRoughnessMap;
uniform bool EnableEmissionColorMap;
uniform bool EnableVertexFoamMask;
uniform bool EnableAlphaTest;
uniform bool EnableVertexAnimationMask;
uniform int NumMaterialLayer;
uniform int BillboardType;
uniform int WindReceiverType;
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

uniform vec4 BaseColor = vec4(1.0);
uniform vec4 UVScaleOffset = vec4(1.0,1.0,0.0,0.0);
uniform vec4 EmissionColor = vec4(1.0);
vec4 sample_FlowMap(vec2 uv) { return sample_trinity(FlowMap,uv,0); }
vec4 sample_lod_FlowMap(vec2 uv,float lod) { return sample_trinity(FlowMap,uv,0); }
vec4 sample_FoamMaskMap(vec2 uv) { return sample_trinity(FoamMaskMap,uv,1); }
vec4 sample_lod_FoamMaskMap(vec2 uv,float lod) { return sample_trinity(FoamMaskMap,uv,1); }
vec4 sample_NormalMap(vec2 uv) { return sample_trinity(NormalMap,uv,2); }
vec4 sample_lod_NormalMap(vec2 uv,float lod) { return sample_trinity(NormalMap,uv,2); }
vec4 sample_NormalMap1(vec2 uv) { return sample_trinity(NormalMap1,uv,3); }
vec4 sample_lod_NormalMap1(vec2 uv,float lod) { return sample_trinity(NormalMap1,uv,3); }


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


	vec4 albedo = BaseColor;


	vec3 flowMap = vec3(1);
	vec3 flowMask = vec3(1);
	if(EnableFlowMap)
	{
		flowMap = sample_FlowMap( uvs).rgb;
		flowMask = sample_FoamMaskMap( uvs).rgb;
	}


	vec3 norm = vec3(1,1,1);
	if(EnableNormalMap)
		norm *= sample_NormalMap( uvs).rgb;
	if(EnableNormalMap1)
		norm *= sample_NormalMap1( uvs).rgb;

	ALBEDO = albedo.rgb;
	ALPHA = albedo.a;
	NORMAL_MAP = norm;
}
#endif
#ifdef VERTEX_STAGE
#include "Common/Vertex.glsl"
#endif
#ifdef FRAGMENT_STAGE
#include "Common/Fragment.glsl"
#endif
