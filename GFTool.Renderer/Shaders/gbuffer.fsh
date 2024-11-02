#version 420 core

layout (location = 0) in vec2 inTexCoord;
layout (location = 0) out vec3 outColor;

uniform sampler2D albedoTexture;
uniform sampler2D normalTexture;
uniform sampler2D specularTexture;
uniform sampler2D aoTexture;

uniform bool useAlbedo;
uniform bool useNormal;
uniform bool useSpecular;
uniform bool useAO;

void main()
{
    vec3 albedo = texture(albedoTexture, inTexCoord).rgb;
    vec3 normal = texture(normalTexture, inTexCoord).rgb;
    vec3 specular = texture(specularTexture, inTexCoord).rgb;
    float ao = texture(aoTexture, inTexCoord).r;

    bool useAll = useAlbedo && useNormal && useSpecular && useAO;

    if(useAll)
    {
        // Mult albedo and ao
        outColor = albedo * ao;
        outColor += specular * 0.5; // Adjust the factor as needed   
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