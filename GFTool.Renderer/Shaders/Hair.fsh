#version 420 core

uniform sampler2D BaseColorMap;
uniform sampler2D LayerMaskMap;
uniform sampler2D NormalMap;
uniform sampler2D RoughnessMap;
uniform sampler2D AOMap;
uniform sampler2D HairFlowMap;

uniform bool EnableBaseColorMap;
uniform bool EnableLayerMaskMap;
uniform bool EnableNormalMap;
uniform bool EnableRoughnessMap;
uniform bool EnableAOMap;
uniform int NumMaterialLayer;
uniform bool EnableSSSMaskMap;
uniform bool EnableHairFlowMap;
uniform bool EnableVertexColor;

uniform vec3 LightDirection;
uniform vec3 LightColor;
uniform vec3 AmbientColor;
uniform vec3 CameraPos;
uniform bool HasTangents;
uniform bool HasBinormals;
uniform bool FlipNormalY;
uniform bool ReconstructNormalZ;
uniform bool TwoSidedDiffuse;
uniform float LightWrap;
uniform float SpecularScale;

layout (location = 0) out vec4 gAlbedo;
layout (location = 1) out vec4 gNormal;
layout (location = 2) out vec4 gSpecular;
layout (location = 3) out vec4 gAO;

in vec3 FragPos;
in vec3 Normal;
in vec2 TexCoord;
in vec4 Color;
in vec3 Tangent;
in vec3 Bitangent;
in vec3 Binormal;

void main()
{
    vec2 uv = vec2(TexCoord.x, 1.0 - TexCoord.y);
    bool useLayerMask = EnableLayerMaskMap && (NumMaterialLayer > 0);
    vec4 layerMask = vec4(0.0);
    if (useLayerMask)
    {
        layerMask = texture(LayerMaskMap, uv);
    }
    float layerWeight = 1.0;
    if (useLayerMask)
    {
        layerWeight = clamp(1.0 - dot(vec4(1.0), layerMask), 0.0, 1.0);
        layerWeight = mix(layerWeight, 1.0, layerMask.r);
    }

    vec3 baseColor = EnableBaseColorMap ? texture(BaseColorMap, uv).rgb : vec3(0.8);
    vec3 vertexColor = EnableVertexColor ? Color.rgb : vec3(1.0);
    vec3 albedo = baseColor * vertexColor;
    albedo *= layerWeight;

    float roughness = EnableRoughnessMap ? texture(RoughnessMap, uv).r : 0.6;
    roughness = clamp(roughness, 0.08, 1.0);

    float ao = EnableAOMap ? texture(AOMap, uv).r : 1.0;

    float flow = EnableHairFlowMap ? texture(HairFlowMap, uv).r : 0.5;

    vec3 n = normalize(Normal);
    if (EnableNormalMap && HasTangents)
    {
        vec4 nm = texture(NormalMap, uv);
        vec2 rg = nm.rg * 2.0 - 1.0;
        vec3 tangentNormal;
        if (ReconstructNormalZ)
        {
            float nz = sqrt(max(0.0, 1.0 - dot(rg, rg)));
            tangentNormal = vec3(rg, nz);
        }
        else
        {
            tangentNormal = vec3(nm.r, nm.g, nm.a) * 2.0 - 1.0;
        }
        if (FlipNormalY)
            tangentNormal.y = -tangentNormal.y;
        vec3 bitangent = HasBinormals ? normalize(Binormal) : normalize(Bitangent);
        if (dot(bitangent, bitangent) < 0.0001)
        {
            bitangent = normalize(cross(n, normalize(Tangent)));
        }
        mat3 tbn = mat3(normalize(Tangent), bitangent, n);
        n = normalize(tbn * tangentNormal);
    }

    // Hair is shaded in the deferred lighting pass. We store base attributes here
    // Use flow as a cheap proxy for reflectance (gives nicer hair highlights without a dedicated anisotropic BRDF)
    float reflectance = clamp(mix(0.35, 1.0, flow), 0.0, 1.0);

    gAlbedo = vec4(albedo, roughness);
    gNormal = vec4(n * 0.5 + 0.5, reflectance);
    gSpecular = vec4(ao, 0.0, 0.0, 0.0); // AO=ao, metallic=0
    gAO = vec4(0.0, 0.0, 0.0, 0.0);      // emission=0, shadingModel=PBR
}
