#version 420 core

uniform sampler2D BaseColorMap;
uniform sampler2D LayerMaskMap;
uniform sampler2D FresnelMaskMap;
uniform sampler2D NormalMap;
uniform sampler2D AOMap;

uniform vec4 BaseColor;
uniform vec4 BaseColorLayer1;
uniform vec4 BaseColorLayer2;
uniform vec4 BaseColorLayer3;
uniform vec4 BaseColorLayer4;
uniform vec4 UVScaleOffset;
uniform float LayerMaskScale1;
uniform float LayerMaskScale2;
uniform float LayerMaskScale3;
uniform float LayerMaskScale4;
uniform float FresnelAlphaMin;
uniform float FresnelAlphaMax;
uniform float FresnelAngleBias;
uniform float FresnelPower;

uniform bool EnableBaseColorMap;
uniform bool EnableLayerMaskMap;
uniform bool EnableFresnelMaskMap;
uniform bool EnableAOMap;
uniform bool NumMaterialLayer;
uniform bool EnableVertexColor;

uniform vec3 CameraPos;

in vec3 FragPos;
in vec3 Normal;
in vec2 TexCoord;
in vec4 Color;

layout (location = 0) out vec4 outColor;

vec2 transformedUv(vec2 source, vec4 transform)
{
    if (dot(abs(transform.xy), vec2(1.0)) < 0.0001)
        transform.xy = vec2(1.0);
    return vec2(
        source.x * transform.x - transform.z,
        1.0 - (source.y * transform.y - transform.w));
}

void main()
{
    vec2 uv = transformedUv(TexCoord, UVScaleOffset);
    vec4 baseSample = EnableBaseColorMap ? texture(BaseColorMap, uv) : vec4(1.0);
    vec3 color = baseSample.rgb * BaseColor.rgb;
    if (EnableLayerMaskMap && NumMaterialLayer)
    {
        vec4 mask = texture(LayerMaskMap, uv) * vec4(
            LayerMaskScale1, LayerMaskScale2, LayerMaskScale3, LayerMaskScale4);
        color = mix(color, BaseColorLayer1.rgb, clamp(mask.r, 0.0, 1.0));
        color = mix(color, BaseColorLayer2.rgb, clamp(mask.g, 0.0, 1.0));
        color = mix(color, BaseColorLayer3.rgb, clamp(mask.b, 0.0, 1.0));
        color = mix(color, BaseColorLayer4.rgb, clamp(mask.a, 0.0, 1.0));
    }
    if (EnableVertexColor)
        color *= Color.rgb;

    vec3 viewDir = normalize(CameraPos - FragPos);
    float rim = pow(1.0 - clamp(abs(dot(normalize(Normal), viewDir)), 0.0, 1.0), max(FresnelPower, 0.05));
    rim = clamp(rim + FresnelAngleBias, 0.0, 1.0);
    // Trinity stores the base opacity separately from the rim contribution.
    // In particular, FresnelEffect uses AlphaMin=0.8 and AlphaMax=0 for the
    // ice horns: interpolating those values would fade the rim to zero.
    float alpha = (FresnelAlphaMin + FresnelAlphaMax * rim) * baseSample.a;
    if (EnableFresnelMaskMap)
        alpha *= texture(FresnelMaskMap, uv).a;
    if (EnableAOMap)
        alpha *= texture(AOMap, uv).r;
    if (alpha <= 0.003)
        discard;

    outColor = vec4(color, clamp(alpha, 0.0, 1.0));
}
