#version 420 core
#include "Common/Surface.glsl"
#define UNSHADED

layout(binding = 0) uniform sampler2D BaseColorMap ;
layout(binding = 1) uniform sampler2D LayerMaskMap ;
layout(binding = 2) uniform sampler2D DisplacementMap ;

uniform bool EnableBaseColorMap;
uniform bool EnableAlphaTest;
uniform int NumMaterialLayer;
uniform bool EnableDisplacementMap;
uniform int NumRequiredUV;

uniform float DisplacementHeight;
uniform float DiscardValue;
uniform float LayerMaskScale1 = 1.0;
uniform float LayerMaskScale2 = 1.0;
uniform float LayerMaskScale3 = 1.0;
uniform float LayerMaskScale4 = 1.0;
uniform float EmissionIntensity = 1.0;


uniform vec4 BaseColor = vec4(1.0);
uniform vec4 UVScaleOffset = vec4(1.0, 1.0, 0.0, 0.0);
uniform vec4 BaseColorLayer1 = vec4(1.0);
uniform vec4 BaseColorLayer2 = vec4(1.0);
uniform vec4 BaseColorLayer3 = vec4(1.0);
uniform vec4 BaseColorLayer4 = vec4(1.0);
uniform vec4 UVScaleOffset3 = vec4(1.0, 1.0, 0.0, 0.0);

uniform int CastShadow;
uniform int CategoryLabel;
uniform int EnableJewel;
uniform int EnableGlobalWeather;
uniform int EnableDirt;
uniform int UVIndexLayer3;
vec4 sample_BaseColorMap(vec2 uv) { return sample_trinity(BaseColorMap,uv,0); }
vec4 sample_lod_BaseColorMap(vec2 uv,float lod) { return sample_trinity(BaseColorMap,uv,0); }
vec4 sample_LayerMaskMap(vec2 uv) { return sample_trinity(LayerMaskMap,uv,1); }
vec4 sample_lod_LayerMaskMap(vec2 uv,float lod) { return sample_trinity(LayerMaskMap,uv,1); }
vec4 sample_DisplacementMap(vec2 uv) { return sample_trinity(DisplacementMap,uv,2); }
vec4 sample_lod_DisplacementMap(vec2 uv,float lod) { return sample_trinity(DisplacementMap,uv,2); }



vec2 transform_uv(vec2 uv, vec4 scale_offset, vec2 center) {
    vec2 result = (uv - center - scale_offset.zw) * scale_offset.xy + center;
    return vec2(result.x, 1.0 - result.y);
}
vec4 layer_weights(vec4 mask) {
    vec4 weights = clamp(mask * vec4(LayerMaskScale1, LayerMaskScale2, LayerMaskScale3, LayerMaskScale4), vec4(0.0), vec4(1.0));
    if (NumMaterialLayer < 2) weights.r = 0.0;
    if (NumMaterialLayer < 3) weights.g = 0.0;
    if (NumMaterialLayer < 4) weights.b = 0.0;
    if (NumMaterialLayer < 5) weights.a = 0.0;
    return weights;
}
vec4 blend_colors(vec4 base, vec4 a, vec4 b, vec4 c, vec4 d, vec4 weights) {
    float total = max(1.0 - dot(weights, vec4(1.0)), 0.0);
    vec4 tint = vec4(base.rgb * total, base.a);
    for (int i = 0; i < 4; i++) {
        vec4 layer = i == 0 ? a : (i == 1 ? b : (i == 2 ? c : d));
        tint.rgb = mix(tint.rgb, layer.rgb * base.rgb, weights[i]);
        total = mix(total, 1.0, weights[i]);
    }
    tint.rgb /= max(total, 0.00001);
    return tint;
}

#ifdef VERTEX_STAGE
void vertex() {
    if (EnableDisplacementMap) {
        vec2 source_uv = NumRequiredUV > 1 && UVIndexLayer3 != 0 ? UV2 : UV;
        vec2 displacement_uv = transform_uv(source_uv, UVScaleOffset3, vec2(0.0));
        float height = sample_trinity(DisplacementMap, displacement_uv, 2).r;
        VERTEX += NORMAL * (sin(height) * COLOR.r * DisplacementHeight);
    }
}
#endif


#ifdef FRAGMENT_STAGE
void fragment() {
    vec2 uv = transform_uv(UV, UVScaleOffset, vec2(0.0));
    vec4 texel = vec4(1.0);
    if (EnableBaseColorMap) texel = sample_trinity(BaseColorMap, uv, 0);
    vec4 weights = vec4(0.0);
    if (NumMaterialLayer > 1) weights = layer_weights(sample_trinity(LayerMaskMap, uv, 1));
    vec4 tint = blend_colors(BaseColor, BaseColorLayer1, BaseColorLayer2, BaseColorLayer3, BaseColorLayer4, weights);
    vec4 color = texel * tint;
    if (EnableAlphaTest && color.a < DiscardValue) discard;
    ALBEDO = color.rgb * EmissionIntensity;
}
#endif
#ifdef VERTEX_STAGE
#include "Common/Vertex.glsl"
#endif
#ifdef FRAGMENT_STAGE
#include "Common/Fragment.glsl"
#endif
