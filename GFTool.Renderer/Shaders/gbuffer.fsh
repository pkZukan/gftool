#version 420 core

layout (location = 0) in vec2 inTexCoord;
layout (location = 0) out vec3 outColor;

uniform sampler2D albedoTexture;
uniform sampler2D normalTexture;
uniform sampler2D specularTexture;
uniform sampler2D aoTexture;
uniform sampler2D ssaoTexture;

uniform vec3 LightDirection;
uniform vec3 LightColor;
uniform vec3 AmbientColor;

uniform bool useAlbedo;
uniform bool useNormal;
uniform bool useSpecular;
uniform bool useAO;
uniform bool useSSAO;
uniform bool useToon;
uniform bool useLegacy;

void main()
{
    vec3 albedo = texture(albedoTexture, inTexCoord).rgb;
    vec3 normal = texture(normalTexture, inTexCoord).rgb;
    vec3 specular = texture(specularTexture, inTexCoord).rgb;
    float ao = texture(aoTexture, inTexCoord).r;
    float ssao = useSSAO ? texture(ssaoTexture, inTexCoord).r : 1.0;

    bool useAll = useAlbedo && useNormal && useSpecular && useAO;

    if(useLegacy)
    {
        float aoSoft = mix(1.0, ao, 0.7);
        outColor = albedo * aoSoft;
        outColor += specular * 0.35;
    }
    else if(useToon)
    {
        vec3 n = normalize(normal * 2.0 - 1.0);
        float nDotL = max(dot(n, normalize(-LightDirection)), 0.0);
        float steps = 3.0;
        float toon = floor(nDotL * steps) / (steps - 1.0);
        vec3 lit = AmbientColor + LightColor * toon;
        float aoSoft = mix(1.0, ao, 0.75);
        float ssaoSoft = mix(1.0, ssao, 0.8);
        outColor = albedo * lit * aoSoft * ssaoSoft;
    }
    else if(useAll)
    {
        // Keep AO/SSAO from crushing the image too hard.
        float aoSoft = mix(1.0, ao, 0.65);
        float ssaoSoft = mix(1.0, ssao, 0.7);
        outColor = albedo * aoSoft * ssaoSoft;
        outColor += specular * 0.25;
    }
    else
    {
        if(useAlbedo)
            outColor = albedo;
        if(useNormal)
            outColor = normal;
        if(useSpecular)
            outColor = specular;
        if(useAO)
            outColor *= ao;
    }
}
