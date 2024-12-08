#version 420 core

uniform sampler2D BaseColorMap;
uniform sampler2D LayerMaskMap;
uniform sampler2D NormalMap;
uniform sampler2D RoughnessMap;
uniform sampler2D AOMap;
uniform sampler2D SSSMaskMap;

uniform bool EnableBaseColorMap;
uniform bool EnableNormalMap;
uniform bool EnableRoughnessMap;
uniform bool EnableAOMap;
uniform bool NumMaterialLayer;
uniform bool EnableSSSMaskMap;

layout (location = 0) out vec3 gAlbedo;
layout (location = 1) out vec3 gNormal; 
layout (location = 2) out vec3 gSpecular;
layout (location = 3) out vec3 gAO;

in vec3 FragPos;
in vec3 Normal;
in vec2 TexCoord;

void main()
{
    //UV flip v
    vec2 uv = vec2(TexCoord.x, 1.0f - TexCoord.y);

    vec4 layerMask = texture(LayerMaskMap, uv);
    float layerWeight = clamp(1.0f - dot(vec4(1.0f), layerMask), 0.0f, 1.0f);

    vec3 normalOutput = normalize(Normal);
    vec2 normalXY = texture(NormalMap, uv).rg;
    normalXY = normalXY * 2.0 - 1.0; // Transform to [-1, 1]
    float normalZ = sqrt(max(0.0, 1.0 - dot(normalXY, normalXY)));
    normalOutput = normalize(vec3(normalXY, normalZ));

    float rough = texture(RoughnessMap, uv).r;
    layerWeight = mix(layerWeight, 1.0f, layerMask.r);

    float ao = texture(AOMap, uv).r;

    float sssMask = texture(SSSMaskMap, uv).r;

    gAlbedo = texture(BaseColorMap, uv).rgb * layerWeight;
    gNormal = normalOutput * 0.5 + 0.5;
    gSpecular = vec3(0.5);
    gAO = vec3(ao);
}