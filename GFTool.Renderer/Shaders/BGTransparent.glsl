#version 420 core
#include "Common/Surface.glsl"

layout(binding = 0) uniform sampler2D BaseColorMap ;

uniform bool EnableBaseColorMap;
uniform bool EnableAlphaTest;
uniform int NumMaterialLayer;
uniform int BillboardType;
uniform bool EnableOpacityFalloff;
uniform int LayerMaskSource;
uniform int NumDetailLayer;
uniform int NumRequiredUV;
uniform int LayerBlendType;
uniform bool EnableDepthFade;
uniform bool EnableVertexColor;

uniform float EmissionIntensity;

uniform vec4 UVScaleOffset = vec4(1.0,1.0,0.0,0.0);
uniform vec4 EmissionColor = vec4(1.0);
vec4 sample_BaseColorMap(vec2 uv) { return sample_trinity(BaseColorMap,uv,0); }
vec4 sample_lod_BaseColorMap(vec2 uv,float lod) { return sample_trinity(BaseColorMap,uv,0); }


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


	vec4 base_col = vec4(1, 1, 1, 1);
	if (EnableBaseColorMap)
	{
		base_col = sample_BaseColorMap( uvs);
	}

	ALBEDO = base_col.rgb * COLOR.rgb;
	ALPHA = 1.0;

    ALPHA = sample_BaseColorMap(vec2(UV.x,1.0-UV.y)).a;
}
#endif
#ifdef VERTEX_STAGE
#include "Common/Vertex.glsl"
#endif
#ifdef FRAGMENT_STAGE
#include "Common/Fragment.glsl"
#endif
