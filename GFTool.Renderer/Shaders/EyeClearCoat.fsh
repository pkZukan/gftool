#version 420 core

uniform sampler2D BaseColorMap;
uniform sampler2D LayerMaskMap;
uniform sampler2D NormalMap;
uniform sampler2D NormalMap1;
uniform sampler2D RoughnessMap;
uniform sampler2D AOMap;

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
    vec4 layerMask = texture(LayerMaskMap, TexCoord);
    float layerWeight = clamp(1.0f - dot(vec4(1.0f), layerMask), 0.0f, 1.0f);

    vec2 norm = texture(NormalMap, TexCoord).rg;
    norm = 2.0 * norm - 1.0;

    vec2 norm1 = texture(NormalMap1, TexCoord).rg;
    norm1 = 2.0 * norm1 - 1.0;
    vec3 norm1Out = vec3(norm1, sqrt(1.0 - clamp(dot(norm1, norm1), 0.0, 1.0)));

    float rough = texture(RoughnessMap, TexCoord).r;
    layerWeight = mix(layerWeight, 1.0f, layerMask.r);

    float ao = texture(AOMap, TexCoord).r;

    gAlbedo = texture(BaseColorMap, TexCoord).rgb * layerWeight;
    gNormal = normalize(Normal) * 0.5 + 0.5;
    gSpecular = vec3(0.5);  // Default medium specular for testing

    gAO = vec3(1.0);
}