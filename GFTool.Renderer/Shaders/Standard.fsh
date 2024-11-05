#version 420 core

uniform sampler2D BaseColorMap;
uniform sampler2D LayerMaskMap;
uniform sampler2D NormalMap;
uniform sampler2D NormalMap1;
uniform sampler2D NormalMap2;
uniform sampler2D RoughnessMap;
uniform sampler2D RoughnessMap1;
uniform sampler2D RoughnessMap2;
uniform sampler2D MetallicMap;
uniform sampler2D AOMap;
uniform sampler2D DetailMaskMap;

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

    vec2 norm = texture(NormalMap, uv).rg;
    norm = 2.0 * norm - 1.0;

    vec2 norm1 = texture(NormalMap1, uv).rg;
    norm1 = 2.0 * norm1 - 1.0;

    vec2 norm2 = texture(NormalMap2, uv).rg;
    norm2 = 2.0 * norm2 - 1.0;

    vec4 detailMap = texture(DetailMaskMap, TexCoord);

    vec4 metalMap = texture(MetallicMap, TexCoord);

    float rough = texture(RoughnessMap, uv).r;
    float rough1 = texture(RoughnessMap1, uv).r;
    float rough2 = texture(RoughnessMap2, uv).r;
    layerWeight = mix(layerWeight, 1.0f, layerMask.r);

    float ao = texture(AOMap, uv).r;

    gAlbedo = texture(BaseColorMap, uv).rgb;// * layerWeight;
    gNormal = normalize(Normal) * 0.5 + 0.5;
    gSpecular = vec3(0.5);  // Default medium specular for testing

    gAO = vec3(1.0);
}