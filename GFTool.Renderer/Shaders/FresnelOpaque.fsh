#version 420 core

uniform sampler2D BaseColorMap;
uniform sampler2D AOMap;
uniform vec4 BaseColor;
uniform vec4 UVScaleOffset;
uniform bool EnableBaseColorMap;
uniform bool EnableAOMap;
uniform vec3 LightDirection;
uniform vec3 LightColor;
uniform vec3 AmbientColor;
uniform bool TwoSidedDiffuse;
uniform float LightWrap;

in vec3 FragPos;
in vec3 Normal;
in vec2 TexCoord;

layout (location = 0) out vec3 gAlbedo;
layout (location = 1) out vec3 gNormal;
layout (location = 2) out vec3 gSpecular;
layout (location = 3) out vec3 gAO;

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
    vec3 albedo = (EnableBaseColorMap ? texture(BaseColorMap, uv).rgb : vec3(1.0)) * BaseColor.rgb;
    vec3 normal = normalize(Normal);
    vec3 lightDir = normalize(-LightDirection);
    float nDotL = TwoSidedDiffuse ? abs(dot(normal, lightDir)) : max(dot(normal, lightDir), 0.0);
    float wrappedLight = clamp((nDotL + LightWrap) / (1.0 + LightWrap), 0.0, 1.0);
    wrappedLight = mix(smoothstep(0.0, 1.0, wrappedLight), 1.0, 0.08);

    gAlbedo = AmbientColor * albedo + LightColor * wrappedLight * albedo;
    gNormal = normal * 0.5 + 0.5;
    gSpecular = vec3(0.02);
    gAO = vec3(EnableAOMap ? texture(AOMap, uv).r : 1.0);
}
