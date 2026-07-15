#version 420 core

uniform sampler2D BaseColorMap;
uniform sampler2D LayerMaskMap;
uniform sampler2D DisplacementMap;
uniform sampler2D DistortionMap;

uniform bool EnableBaseColorMap;
uniform bool EnableLayerMaskMap;
uniform bool EnableDisplacementMap;
uniform bool EnableDistortionMap;
uniform bool EnableVertexColor;

uniform vec4 BaseColor;
uniform vec4 BaseColorLayer1;
uniform vec4 BaseColorLayer2;
uniform vec4 BaseColorLayer3;
uniform vec4 BaseColorLayer4;
uniform vec4 EmissionColor;
uniform vec4 UVScaleOffset;
uniform vec4 UVScaleOffset3;
uniform int NumRequiredUV;
uniform int UVIndexLayer3;
uniform float LayerMaskScale1;
uniform float LayerMaskScale2;
uniform float LayerMaskScale3;
uniform float LayerMaskScale4;
uniform float EmissionIntensity;
uniform float DisplacementHeight;
uniform float DiscardValue;

layout (location = 0) out vec4 outColor;

in vec3 FragPos;
in vec3 Normal;
in vec2 TexCoord0;
in vec2 TexCoord1;
in vec4 Color;

vec4 safeUvTransform(vec4 transform)
{
    if (dot(abs(transform.xy), vec2(1.0)) < 0.0001)
    {
        return vec4(1.0, 1.0, transform.zw);
    }
    return transform;
}

vec2 transformedUv(vec2 source, vec4 transform)
{
    vec2 uv = vec2(source.x, 1.0 - source.y);
    vec4 st = safeUvTransform(transform);
    return uv * st.xy + st.zw;
}

vec3 toneMapFire(vec3 hdr)
{
    return 1.0 - exp(-max(hdr, vec3(0.0)) * 0.90);
}

float maxComponent(vec4 value)
{
    return max(max(value.r, value.g), max(value.b, value.a));
}

void main()
{
    vec2 baseUv = transformedUv(TexCoord0, UVScaleOffset);
    vec2 layer3Source = NumRequiredUV > 1 && UVIndexLayer3 > 0 ? TexCoord1 : TexCoord0;
    vec2 layer3Uv = transformedUv(layer3Source, UVScaleOffset3);

    vec4 displacement = vec4(0.5, 0.5, 0.5, 1.0);
    if (EnableDisplacementMap)
    {
        displacement = texture(DisplacementMap, layer3Uv);
    }
    else if (EnableDistortionMap)
    {
        displacement = texture(DistortionMap, layer3Uv);
    }

    if ((EnableDisplacementMap || EnableDistortionMap) && abs(DisplacementHeight) > 0.0001)
    {
        float secondaryNoise = EnableDisplacementMap
            ? texture(DisplacementMap, layer3Uv + vec2(0.173, 0.317)).r
            : texture(DistortionMap, layer3Uv + vec2(0.173, 0.317)).r;
        vec2 flow = vec2(displacement.r, secondaryNoise) * 2.0 - 1.0;
        float strength = clamp(abs(DisplacementHeight) * 0.12, 0.0, 0.025);
        baseUv += flow * strength;
    }

    vec4 baseSample = EnableBaseColorMap ? texture(BaseColorMap, baseUv) : vec4(1.0);
    vec4 layerMask = EnableLayerMaskMap ? texture(LayerMaskMap, baseUv) : vec4(1.0, 0.0, 0.0, 0.0);
    vec4 maskScale = vec4(LayerMaskScale1, LayerMaskScale2, LayerMaskScale3, LayerMaskScale4);
    if (maxComponent(abs(maskScale)) < 0.0001)
    {
        maskScale = vec4(1.0);
    }
    layerMask = clamp(layerMask * max(maskScale, vec4(0.0)), 0.0, 1.0);

    vec3 baseTint = maxComponent(abs(BaseColor)) > 0.0001 ? BaseColor.rgb : vec3(1.0);
    vec4 layerActivity = vec4(
        step(0.001, length(BaseColorLayer1.rgb - baseTint)),
        step(0.001, length(BaseColorLayer2.rgb - baseTint)),
        step(0.001, length(BaseColorLayer3.rgb - baseTint)),
        step(0.001, length(BaseColorLayer4.rgb - baseTint))
    );
    vec4 effectiveLayerMask = layerMask * layerActivity;
    vec3 hdrColor = baseSample.rgb * baseTint;
    hdrColor = mix(hdrColor, BaseColorLayer1.rgb, effectiveLayerMask.r);
    hdrColor = mix(hdrColor, BaseColorLayer2.rgb, effectiveLayerMask.g);
    hdrColor = mix(hdrColor, BaseColorLayer3.rgb, effectiveLayerMask.b);
    hdrColor = mix(hdrColor, BaseColorLayer4.rgb, effectiveLayerMask.a);

    if (maxComponent(abs(BaseColorLayer1)) + maxComponent(abs(BaseColorLayer2)) < 0.0001)
    {
        hdrColor = mix(vec3(2.8, 0.12, 0.02), vec3(2.2, 0.72, 0.12), layerMask.g);
    }

    vec3 emissionTint = maxComponent(abs(EmissionColor)) > 0.0001 ? max(EmissionColor.rgb, vec3(0.0)) : vec3(1.0);
    float exposure = 0.82 + max(EmissionIntensity, 0.0) * 0.12;
    vec3 color = toneMapFire(hdrColor * emissionTint * exposure);

    if (EnableVertexColor)
    {
        color *= mix(vec3(1.0), clamp(Color.rgb, 0.0, 2.0), 0.18);
    }

    float coverage = EnableLayerMaskMap ? maxComponent(effectiveLayerMask) : baseSample.a;
    float threshold = max(DiscardValue, 0.012);
    float edge = smoothstep(threshold, min(0.30, threshold + 0.12), coverage);
    float alpha = edge * mix(0.38, 0.92, coverage) * baseSample.a;
    if (EnableVertexColor)
    {
        alpha *= clamp(Color.a, 0.0, 1.0);
    }
    if (alpha <= threshold)
    {
        discard;
    }

    alpha = clamp(alpha, 0.0, 1.0);
    outColor = vec4(color * alpha, alpha);
}
