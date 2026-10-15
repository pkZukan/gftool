#version 420 core
#include "Common/Surface.glsl"
#define CUSTOM_LIGHT

layout(binding = 0) uniform sampler2D BaseColorMap ;
layout(binding = 1) uniform sampler2D NormalMap ;
layout(binding = 2) uniform sampler2D OcclusionMap ;
layout(binding = 3) uniform sampler2D SpecularMaskMap ;
layout(binding = 4) uniform sampler2D ShadowingColorMap ;
layout(binding = 5) uniform sampler2D ShadowingColorMaskMap ;
layout(binding = 6) uniform sampler2D RimLightMaskMap ;
layout(binding = 7) uniform sampler2D LocalReflectionMap ;
layout(binding = 8) uniform sampler2D LayerMaskMap ;
layout(binding = 9) uniform sampler2D ParallaxMap ;
layout(binding = 10) uniform sampler2D HighlightMaskMap ;
layout(binding = 11) uniform sampler2D EyelidShadowMaskMap ;

uniform bool EnableBaseColorMap;
uniform bool EnableNormalMap;
uniform bool EnableAOMap;
uniform bool EnableAlphaTest;
uniform bool BaseColorMultiply;
uniform bool EnableHairSpecular;
uniform bool EnableEmissionLayer;
uniform bool EnableLerpBaseColorEmission;
uniform bool EnableEyeOptions;
uniform bool EnableHighlight;
uniform bool EnableParallaxMap;
uniform bool RequireEyelidShadowMap;
uniform bool EnableUVScaleOffsetNormal;

uniform int NumMaterialLayer;

uniform bool EnableSSSMaskMap;

uniform float SaturationPower = 1.0;
uniform float BaseColorDarkness;
uniform float NormalHeight = 1.0;
uniform float Metallic;
uniform float OcclusionStrength = 1.0;
uniform float SpecularOffset;
uniform float SpecularIntensity;
uniform float SpecularContrast;
uniform float ShadowStrength;
uniform float EmissionIntensity;
uniform float DiscardValue;
uniform float ShadingBias;
uniform float ShadowingColorMaskMapValue;
uniform float HueShiftBias;
uniform float ShadowingBias;
uniform float HalfLambertBias;
uniform float ShadowingGIGain;
uniform float RimLightOffset;
uniform float RimLightContrast;
uniform float RimLightIntensity;
uniform float BackRimLightIntensity;
uniform float ReflectionsBlur;
uniform float DiffusionLevels;
uniform float LayerMaskScale1 = 1.0;
uniform float SpecularLayer1Intensity;
uniform float SpecularLayer1Offset;
uniform float SpecularLayer1Contrast;
uniform float MetallicLayer1;
uniform float LayerMaskScale2 = 1.0;
uniform float SpecularLayer2Intensity;
uniform float SpecularLayer2Offset;
uniform float SpecularLayer2Contrast;
uniform float MetallicLayer2;
uniform float LayerMaskScale3 = 1.0;
uniform float LayerMaskScale4 = 1.0;
uniform float SpecularLayer4Intensity;
uniform float SpecularLayer4Offset;
uniform float SpecularLayer4Contrast;
uniform float MetallicLayer4;
uniform float MidAreaShift;
uniform float MidAreaContrast;
uniform float MidAreaHueOffset;
uniform float DarkAreaShift;
uniform float DarkAreaContrast;
uniform float DarkAreaHueOffset;
uniform float HueShiftAreaValue;
uniform float ShadowingShift;
uniform float ShadowingContrast;
uniform float ParallaxHeight;
uniform float ParallaxIOR;
uniform float EmissionIntensityLayer5;
uniform float UVRotation2;


uniform vec4 BaseColor = vec4(1.0);
uniform vec4 EmissionColor = vec4(1.0);
uniform vec4 UVCenter0;
uniform vec4 UVScaleOffset = vec4(1.0, 1.0, 0.0, 0.0);
uniform vec4 UVScaleOffsetNormal = vec4(1.0, 1.0, 0.0, 0.0);
uniform vec4 ShadowingColor;
uniform vec4 BaseColorLayer1 = vec4(1.0);
uniform vec4 BaseColorLayer2 = vec4(1.0);
uniform vec4 BaseColorLayer3 = vec4(1.0);
uniform vec4 BaseColorLayer4 = vec4(1.0);
uniform vec4 ShadowingColorLayer1;
uniform vec4 ShadowingColorLayer2;
uniform vec4 ShadowingColorLayer3;
uniform vec4 ShadowingColorLayer4;


uniform int CastShadow;
uniform int ReceiveShadow;
uniform int CategoryLabel;
uniform int EnableGlobalWeather;
uniform int EnableDistanceDither;
uniform int UVIndexAO;
uniform int EnableAdditionalLight;
uniform int UVIndexLayerMask;

uniform float SpecularLayer3Intensity = 0.0;
uniform float SpecularLayer3Offset = 0.0;
uniform float SpecularLayer3Contrast = 0.0;
uniform float MetallicLayer3 = 0.0;
uniform vec4 EmissionColorLayer5 = vec4(1.0);
uniform vec4 UVCenter1 = vec4(1.0);
uniform vec4 UVScaleOffset1 = vec4(1.0, 1.0, 0.0, 0.0);
uniform vec4 BaseColorLayer6 = vec4(1.0);
uniform vec4 UVScaleOffset2 = vec4(1.0, 1.0, 0.0, 0.0);
uniform int EyelidType = 0;
uniform int EnableIrisRefraction = 0;
uniform int NumRequiredUV = 0;
uniform float EmissionIntensityLayer1 = 0.0;
uniform float EmissionIntensityLayer2 = 0.0;
uniform float EmissionIntensityLayer3 = 0.0;
uniform float EmissionIntensityLayer4 = 0.0;
uniform float AuraColorScale = 0.0;
uniform float AuraColorScrollSpeed = 0.0;
uniform float AuraNoiseScale = 0.0;
uniform float AuraNoiseScrollSpeed = 0.0;
uniform float AuraIntensity = 0.0;
uniform float AuraRimPower = 0.0;
uniform vec4 EmissionColorLayer1 = vec4(1.0);
uniform vec4 EmissionColorLayer2 = vec4(1.0);
uniform vec4 EmissionColorLayer3 = vec4(1.0);
uniform vec4 EmissionColorLayer4 = vec4(1.0);
uniform bool EnableAuraEffect = false;
vec4 sample_BaseColorMap(vec2 uv) { return sample_trinity(BaseColorMap,uv,0); }
vec4 sample_lod_BaseColorMap(vec2 uv,float lod) { return sample_trinity(BaseColorMap,uv,0); }
vec4 sample_NormalMap(vec2 uv) { return sample_trinity(NormalMap,uv,1); }
vec4 sample_lod_NormalMap(vec2 uv,float lod) { return sample_trinity(NormalMap,uv,1); }
vec4 sample_OcclusionMap(vec2 uv) { return sample_trinity(OcclusionMap,uv,2); }
vec4 sample_lod_OcclusionMap(vec2 uv,float lod) { return sample_trinity(OcclusionMap,uv,2); }
vec4 sample_SpecularMaskMap(vec2 uv) { return sample_trinity(SpecularMaskMap,uv,3); }
vec4 sample_lod_SpecularMaskMap(vec2 uv,float lod) { return sample_trinity(SpecularMaskMap,uv,3); }
vec4 sample_ShadowingColorMap(vec2 uv) { return sample_trinity(ShadowingColorMap,uv,4); }
vec4 sample_lod_ShadowingColorMap(vec2 uv,float lod) { return sample_trinity(ShadowingColorMap,uv,4); }
vec4 sample_ShadowingColorMaskMap(vec2 uv) { return sample_trinity(ShadowingColorMaskMap,uv,5); }
vec4 sample_lod_ShadowingColorMaskMap(vec2 uv,float lod) { return sample_trinity(ShadowingColorMaskMap,uv,5); }
vec4 sample_RimLightMaskMap(vec2 uv) { return sample_trinity(RimLightMaskMap,uv,6); }
vec4 sample_lod_RimLightMaskMap(vec2 uv,float lod) { return sample_trinity(RimLightMaskMap,uv,6); }
vec4 sample_LocalReflectionMap(vec2 uv) { return sample_trinity(LocalReflectionMap,uv,7); }
vec4 sample_lod_LocalReflectionMap(vec2 uv,float lod) { return sample_trinity(LocalReflectionMap,uv,7); }
vec4 sample_LayerMaskMap(vec2 uv) { return sample_trinity(LayerMaskMap,uv,8); }
vec4 sample_lod_LayerMaskMap(vec2 uv,float lod) { return sample_trinity(LayerMaskMap,uv,8); }
vec4 sample_ParallaxMap(vec2 uv) { return sample_trinity(ParallaxMap,uv,9); }
vec4 sample_lod_ParallaxMap(vec2 uv,float lod) { return sample_trinity(ParallaxMap,uv,9); }
vec4 sample_HighlightMaskMap(vec2 uv) { return sample_trinity(HighlightMaskMap,uv,10); }
vec4 sample_lod_HighlightMaskMap(vec2 uv,float lod) { return sample_trinity(HighlightMaskMap,uv,10); }
vec4 sample_EyelidShadowMaskMap(vec2 uv) { return sample_trinity(EyelidShadowMaskMap,uv,11); }
vec4 sample_lod_EyelidShadowMaskMap(vec2 uv,float lod) { return sample_trinity(EyelidShadowMaskMap,uv,11); }




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
    return mix(mix(mix(mix(base, a, weights.r), b, weights.g), c, weights.b), d, weights.a);
}

vec3 shadow_tint;
float surface_specular;
float surface_gloss;
float rim_mask;
float surface_offset;
float surface_contrast;
float surface_mask;
float surface_ao;
float surface_emission;

float adjust_contrast(float value, float contrast) {
    return clamp((value - 0.5) * (1.0 + 2.0 * contrast) + 0.5, 0.0, 1.0);
}

vec2 get_uv(vec2 uv, int slot) {
    vec4 transform = slot == 1 && EnableUVScaleOffsetNormal ? UVScaleOffsetNormal : UVScaleOffset;
    vec2 center = slot == 1 && EnableUVScaleOffsetNormal ? vec2(0.0) : UVCenter0.xy;
    if (EnableEyeOptions && slot == 10) { transform = UVScaleOffset1; center = UVCenter1.xy; }
    if (EnableEyeOptions && slot == 11) {
        transform = UVScaleOffset2;
        float angle = radians(UVRotation2);
        vec2 v = uv - vec2(0.5);
        uv = vec2(cos(angle)*v.x - sin(angle)*v.y, sin(angle)*v.x + cos(angle)*v.y) + vec2(0.5);
    }
    return transform_uv(uv, transform, center);
}
float blend_values(float base, vec4 layers, vec4 weights) {
    return mix(mix(mix(mix(base, layers.r, weights.r), layers.g, weights.g), layers.b, weights.b), layers.a, weights.a);
}
vec3 hue_rotate(vec3 color, float degrees) {
    vec3 axis = normalize(vec3(1.0));
    float angle = radians(degrees);
    return max(color * cos(angle) + cross(axis, color) * sin(angle) + axis * dot(axis, color) * (1.0 - cos(angle)), vec3(0.0));
}
#ifdef FRAGMENT_STAGE
void fragment() {
    vec2 surface_uv = UV;
    if (EnableEyeOptions && EnableParallaxMap) {
        vec3 view_tangent = VIEW * mat3(normalize(TANGENT), normalize(BINORMAL), normalize(NORMAL));
        float height = sample_trinity(ParallaxMap, get_uv(UV, 9), 9).r;
        float refraction = EnableIrisRefraction != 0 ? 1.0 / max(ParallaxIOR, 1.0) : 1.0;
        surface_uv -= view_tangent.xy / max(abs(view_tangent.z), 0.2) * height * ParallaxHeight * refraction;
    }
    vec2 layer_uv = NumRequiredUV > 1 && UVIndexLayerMask == 1 ? UV2 : surface_uv;
    vec4 weights = vec4(0.0);
    if (NumMaterialLayer > 1) weights = layer_weights(sample_trinity(LayerMaskMap, get_uv(layer_uv, 8), 8));
    vec4 texel = vec4(1.0);
    if (EnableBaseColorMap) texel = sample_trinity(BaseColorMap, get_uv(surface_uv, 0), 0);
    float total = max(1.0 - dot(weights, vec4(1.0)), 0.0);
    vec4 base = BaseColor * texel;
    base.rgb *= total;
    for (int i = 0; i < 4; i++) {
        vec4 tint = i == 0 ? BaseColorLayer1 : (i == 1 ? BaseColorLayer2 : (i == 2 ? BaseColorLayer3 : BaseColorLayer4));
        if (BaseColorMultiply) tint *= texel;
        base.rgb = mix(base.rgb, tint.rgb, weights[i]);
    }
    float luminance = dot(base.rgb, vec3(0.2126, 0.7152, 0.0722));
    base.rgb = max(mix(vec3(luminance), base.rgb, SaturationPower) * (1.0 - BaseColorDarkness), vec3(0.0));
    if (EnableAlphaTest && base.a < DiscardValue) discard;

    shadow_tint = blend_colors(vec4(ShadowingColor.rgb * total, ShadowingColor.a), ShadowingColorLayer1, ShadowingColorLayer2, ShadowingColorLayer3, ShadowingColorLayer4, weights).rgb;
    float shadow_mask = clamp(sample_trinity(ShadowingColorMaskMap, get_uv(surface_uv, 5), 5).r * ShadowingColorMaskMapValue, 0.0, 1.0);
    shadow_tint = mix(shadow_tint, sample_trinity(ShadowingColorMap, get_uv(surface_uv, 4), 4).rgb, shadow_mask);
    if (EnableEyeOptions && RequireEyelidShadowMap) {
        float eyelid = sample_trinity(EyelidShadowMaskMap, get_uv(UV, 11), 11).r;
        vec3 tint = mix(vec3(1.0), BaseColorLayer6.rgb, eyelid);
        base.rgb *= tint;
        shadow_tint *= tint;
    }
    vec2 ao_uv = NumRequiredUV > 1 && UVIndexAO == 1 ? UV2 : surface_uv;
    AO = EnableAOMap ? pow(max(sample_trinity(OcclusionMap, get_uv(ao_uv, 2), 2).r, 0.00001), max(OcclusionStrength, 0.0)) : 1.0;
    if (EnableNormalMap) {
        vec2 xy = sample_trinity(NormalMap, get_uv(UV, 1), 1).rg * 2.0 - 1.0;
        NORMAL_MAP = vec3(xy, sqrt(max(1.0 - dot(xy, xy), 0.0))) * 0.5 + 0.5;
        NORMAL_MAP_DEPTH = NormalHeight;
    }
    surface_ao = AO;
    vec3 spec_mask = sample_trinity(SpecularMaskMap, get_uv(surface_uv, 3), 3).rgb;
    float intensity = blend_values(SpecularIntensity, vec4(SpecularLayer1Intensity, SpecularLayer2Intensity, SpecularLayer3Intensity, SpecularLayer4Intensity), weights);
    float offset = blend_values(SpecularOffset, vec4(SpecularLayer1Offset, SpecularLayer2Offset, SpecularLayer3Offset, SpecularLayer4Offset), weights);
    float contrast = blend_values(SpecularContrast, vec4(SpecularLayer1Contrast, SpecularLayer2Contrast, SpecularLayer3Contrast, SpecularLayer4Contrast), weights);
    surface_gloss = clamp((spec_mask.r - 0.5) * (1.0 + contrast) + 0.5 + offset, 0.0, 1.0);
    surface_specular = max(intensity, 0.0);
    surface_offset = offset;
    surface_contrast = contrast;
    surface_mask = spec_mask.r;
    METALLIC = clamp(blend_values(Metallic, vec4(MetallicLayer1, MetallicLayer2, MetallicLayer3, MetallicLayer4), weights), 0.0, 1.0);
    ROUGHNESS = 1.0; 
    SPECULAR = clamp(surface_specular / 0.08, 0.0, 1.0);
    rim_mask = sample_trinity(RimLightMaskMap, get_uv(surface_uv, 6), 6).r;
    vec3 emission = EmissionColor.rgb * EmissionIntensity;
    if (EnableEmissionLayer) {
        vec3 layer_emission = blend_colors(vec4(emission, 1.0), EmissionColorLayer1 * EmissionIntensityLayer1, EmissionColorLayer2 * EmissionIntensityLayer2, EmissionColorLayer3 * EmissionIntensityLayer3, EmissionColorLayer4 * EmissionIntensityLayer4, weights).rgb;
        emission = layer_emission;
    }
    surface_emission = EnableEmissionLayer ? blend_values(EmissionIntensity,
        vec4(EmissionIntensityLayer1, EmissionIntensityLayer2, EmissionIntensityLayer3, EmissionIntensityLayer4), weights) : EmissionIntensity;
    if (EnableLerpBaseColorEmission) {
        base.rgb *= max(1.0 - surface_emission, 0.0);
        shadow_tint *= max(1.0 - surface_emission, 0.0);
    }
    if (EnableEyeOptions && EnableHighlight) {
        float highlight = sample_trinity(HighlightMaskMap, get_uv(UV, 10), 10).r;
        base.rgb = mix(base.rgb, EmissionColorLayer5.rgb * EmissionIntensityLayer5, highlight);
        shadow_tint = mix(shadow_tint, EmissionColorLayer5.rgb * EmissionIntensityLayer5, highlight);
    }

    vec3 reflected = reflect(-VIEW, NORMAL);
    vec2 reflection_uv = vec2(atan(reflected.z, reflected.x) / (2.0 * PI) + 0.5, acos(clamp(reflected.y, -1.0, 1.0)) / PI);
    emission += sample_trinity(LocalReflectionMap, reflection_uv, 7).rgb * METALLIC * surface_specular;
    if (EnableAuraEffect) {
        float wave = 0.5 + 0.5 * sin(UV.y * AuraColorScale * 2.0 * PI + TIME * AuraColorScrollSpeed);
        float noise = 0.5 + 0.5 * sin(dot(UV, vec2(12.9898, 78.233)) * AuraNoiseScale + TIME * AuraNoiseScrollSpeed);
        float edge = pow(clamp(1.0 - dot(NORMAL, VIEW), 0.0, 1.0), max(AuraRimPower, 0.001));
        emission += EmissionColor.rgb * wave * noise * edge * AuraIntensity;
    }
    ALBEDO = base.rgb;
    EMISSION = emission;
}
#endif

#ifdef FRAGMENT_STAGE
void light() {
    float light_enabled = LIGHT_IS_DIRECTIONAL || EnableAdditionalLight != 0 ? 1.0 : 0.0;
    float ndotl = dot(NORMAL, LIGHT);
    float diffuse = ndotl * 0.5 + 0.5;
    diffuse = clamp(mix(diffuse, diffuse * diffuse, HalfLambertBias), 0.0, 1.0);
    float attenuation = ReceiveShadow > 0 ? ATTENUATION : 1.0;
    float shadow = clamp(attenuation + ShadowStrength * ShadowStrength, 0.0, 1.0);
    vec3 light_color = LIGHT_COLOR / PI * light_enabled;
    float shading_width = max(ShadingBias * ShadingBias * ShadowingBias, 0.00001);
    float shaded_weight = clamp((0.5 * ShadowingBias + 0.5 * shading_width - diffuse) / shading_width, 0.0, 1.0);
    vec3 shaded = ALBEDO * clamp(shadow_tint + vec3(surface_ao), vec3(0.0), vec3(1.0));
    float brightness = max(light_color.r, max(light_color.g, light_color.b)) * diffuse * surface_ao * shadow;
    float mid_area = adjust_contrast(1.0 - smoothstep(MidAreaShift, 1.0 + MidAreaShift, brightness), MidAreaContrast);
    float dark_area = adjust_contrast(1.0 - smoothstep(DarkAreaShift, 1.0 + DarkAreaShift, brightness), DarkAreaContrast);
    vec3 mid_tint = mix(shaded, hue_rotate(shaded, MidAreaHueOffset), mid_area);
    vec3 dark_tint = hue_rotate(shaded, DarkAreaHueOffset);
    float gradient = adjust_contrast(smoothstep(ShadowingShift, 1.0 + ShadowingShift, attenuation * (ndotl * 0.5 + 0.5)), ShadowingContrast);
    vec3 tint = mix(mix(dark_tint, mid_tint, dark_area), mix(mid_tint, dark_tint, dark_area), gradient);
    tint *= mix(1.0, 0.5, HueShiftAreaValue * mid_area);
    shaded = mix(shaded, mix(shaded, tint, HueShiftBias), clamp(light_color, vec3(0.0), vec3(1.0)));
    shaded *= mix(vec3(1.0), shadow_tint, shaded_weight);


    DIFFUSE_LIGHT += shaded / max(ALBEDO, vec3(0.00001)) * light_color * shadow;

    float fresnel = 1.0 - clamp(dot(normalize(NORMAL), normalize(VIEW)), 0.0, 1.0);
    float rim = adjust_contrast(smoothstep(RimLightOffset, 1.0, fresnel), RimLightContrast);
    float rim_front = rim * smoothstep(-0.4, 0.0, ndotl + fresnel - 1.0) * RimLightIntensity * ndotl * surface_ao;
    float rim_back = rim * smoothstep(-0.4, 0.0, -ndotl + fresnel - 1.0);
    float back_weight = rim_back * BackRimLightIntensity;
    rim_back = mix(back_weight, rim_back, back_weight) * RimLightIntensity;
    rim = mix(rim_front, rim_back, clamp(-ndotl, 0.0, 1.0)) * rim_mask * max(1.0 - surface_emission, 0.0);
    vec3 halfway = normalize(LIGHT + VIEW);
    float spec = pow(clamp(dot(NORMAL, halfway), 0.0, 1.0), 8.0);
    spec = adjust_contrast(smoothstep(surface_offset, 1.0 + surface_offset, spec), surface_contrast);
    vec3 specular = light_color * shadow * (spec * surface_specular * surface_mask * surface_ao);

    specular = clamp(max(specular, vec3(rim)), vec3(0.0), vec3(1.0));
    SPECULAR_LIGHT += specular * clamp(light_color * shadow, vec3(0.0), vec3(1.0)) * light_color * shadow;
}
#endif


#ifdef VERTEX_STAGE
void vertex() {}
#endif
#ifdef VERTEX_STAGE
#include "Common/Vertex.glsl"
#endif
#ifdef FRAGMENT_STAGE
#include "Common/Fragment.glsl"
#endif
