#version 420 core

#ifdef VERTEX_STAGE
layout (location = 0) in vec3 aPos;
layout (location = 1) in vec3 aNormal;
layout (location = 2) in vec2 aTexCoord;
layout (location = 3) in vec4 aColor;
layout (location = 4) in vec4 aTangent;
layout (location = 5) in vec3 aBinormal;
layout (location = 6) in vec4 aBlendIndices;
layout (location = 7) in vec4 aBlendWeights;

uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;
uniform bool EnableSkinning;
uniform bool SwapBlendOrder;
uniform mat4 Bones[192];
uniform int BoneCount;

out vec3 FragPos;
out vec3 Normal;
out vec2 TexCoord;
out vec4 Color;
out vec3 Tangent;
out vec3 Bitangent;
out vec3 Binormal;

void main()
{
    vec4 localPos = vec4(aPos, 1.0);
    vec3 localNormal = aNormal;
    vec3 localTangent = aTangent.xyz;
    vec3 localBinormal = aBinormal;
    if (EnableSkinning && BoneCount > 0)
    {
        vec4 weights = aBlendWeights;
        ivec4 boneIds = ivec4(aBlendIndices + 0.5);
        if (SwapBlendOrder)
        {
            weights = weights.wxyz;
            boneIds = ivec4(boneIds.w, boneIds.x, boneIds.y, boneIds.z);
        }
        float total = weights.x + weights.y + weights.z + weights.w;
        if (total > 0.0)
        {
            weights /= total;
        }
        boneIds = clamp(boneIds, ivec4(0), ivec4(BoneCount - 1));
        mat4 skinMat = weights.x * Bones[boneIds.x]
                     + weights.y * Bones[boneIds.y]
                     + weights.z * Bones[boneIds.z]
                     + weights.w * Bones[boneIds.w];
        localPos = skinMat * vec4(aPos, 1.0);
        mat3 skinMat3 = mat3(skinMat);
        localNormal = normalize(skinMat3 * aNormal);
        localTangent = normalize(skinMat3 * aTangent.xyz);
        localBinormal = normalize(skinMat3 * aBinormal);
    }

    FragPos = vec3(model * localPos);

    mat3 normalMatrix = transpose(inverse(mat3(model)));
    Normal = normalize(normalMatrix * localNormal);
    vec3 tangent = normalize(normalMatrix * localTangent);
    float handedness = (aTangent.w < 0.0) ? -1.0 : 1.0;
    Tangent = tangent;
    Bitangent = normalize(cross(Normal, tangent) * handedness);
    Binormal = normalize(normalMatrix * localBinormal);

    TexCoord = aTexCoord;
    Color = aColor;

    gl_Position = projection * view * vec4(FragPos, 1.0);
}
#endif

#ifdef FRAGMENT_STAGE
layout(binding = 0) uniform sampler2D BaseColorMap;
layout(binding = 1) uniform sampler2D LayerMaskMap;
layout(binding = 2) uniform sampler2D NormalMap;
layout(binding = 3) uniform sampler2D NormalMap1;
layout(binding = 4) uniform sampler2D RoughnessMap;
layout(binding = 5) uniform sampler2D AOMap;

uniform bool EnableBaseColorMap;
uniform bool EnableLayerMaskMap;
uniform bool EnableNormalMap;
uniform bool EnableNormalMap1;
uniform bool EnableRoughnessMap;
uniform bool EnableAOMap;
uniform bool NumMaterialLayer;
uniform bool EnableSSSMaskMap;
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
uniform vec3 EmissionColor;
uniform float EmissionStrength;
uniform float ParallaxInside;
uniform float IOR;
uniform float LensOpacity;
uniform float AlbedoAlpha;

layout (location = 0) out vec4 outColor;

in vec3 FragPos;
in vec3 Normal;
in vec2 TexCoord;
in vec4 Color;
in vec3 Tangent;
in vec3 Bitangent;
in vec3 Binormal;

void main()
{
    vec2 uv = vec2(TexCoord.x, 1.0f - TexCoord.y);

    bool useLayerMask = EnableLayerMaskMap && NumMaterialLayer;
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

    vec3 baseColor = EnableBaseColorMap ? texture(BaseColorMap, uv).rgb : vec3(1.0);
    vec3 vertexColor = EnableVertexColor ? Color.rgb : vec3(1.0);
    vec3 albedo = EnableVertexColor
        ? mix(vertexColor, baseColor, EnableBaseColorMap ? 0.5 : 0.0)
        : baseColor;
    albedo *= layerWeight;

    float roughness = EnableRoughnessMap ? texture(RoughnessMap, uv).r : 0.15;
    roughness = clamp(roughness, 0.02, 1.0);

    float ao = EnableAOMap ? texture(AOMap, uv).r : 1.0;

    vec3 n = normalize(Normal);
    vec3 tangentNormal = vec3(0.0, 0.0, 1.0);
    if (EnableNormalMap && HasTangents)
    {
        vec4 nm = texture(NormalMap, uv);
        vec2 rg = nm.rg * 2.0 - 1.0;
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
    }
    if (EnableNormalMap1 && useLayerMask && HasTangents)
    {
        vec4 nm1 = texture(NormalMap1, uv);
        vec2 rg1 = nm1.rg * 2.0 - 1.0;
        vec3 n1;
        if (ReconstructNormalZ)
        {
            float nz1 = sqrt(max(0.0, 1.0 - dot(rg1, rg1)));
            n1 = vec3(rg1, nz1);
        }
        else
        {
            n1 = vec3(nm1.r, nm1.g, nm1.a) * 2.0 - 1.0;
        }
        if (FlipNormalY)
            n1.y = -n1.y;
        tangentNormal = normalize(mix(tangentNormal, n1, layerMask.g));
    }
    if ((EnableNormalMap || EnableNormalMap1) && HasTangents)
    {
        vec3 bitangent = HasBinormals ? normalize(Binormal) : normalize(Bitangent);
        if (dot(bitangent, bitangent) < 0.0001)
        {
            bitangent = normalize(cross(n, normalize(Tangent)));
        }
        mat3 tbn = mat3(normalize(Tangent), bitangent, n);
        n = normalize(tbn * tangentNormal);
    }

    vec3 lightDir = normalize(-LightDirection);
    vec3 viewDir = normalize(CameraPos - FragPos);
    vec3 halfDir = normalize(lightDir + viewDir);

    float nDotL = dot(n, lightDir);
    if (TwoSidedDiffuse)
        nDotL = abs(nDotL);
    else
        nDotL = max(nDotL, 0.0);
    float wrappedNdotL = (nDotL + LightWrap) / (1.0 + LightWrap);
    float specPower = mix(32.0, 192.0, 1.0 - roughness);
    float iorScale = clamp(IOR - 1.0, 0.0, 1.0);
    specPower *= mix(1.0, 1.6, iorScale);
    float spec = pow(max(dot(n, halfDir), 0.0), specPower);
    float clearCoat = pow(max(dot(n, halfDir), 0.0), specPower * 2.0) * 0.35;

    vec3 emission = EmissionColor * EmissionStrength * clamp(ParallaxInside, 0.0, 1.0);
    vec3 color = AmbientColor * albedo + LightColor * wrappedNdotL * albedo + emission;
    vec3 specColor = vec3(0.8) * SpecularScale;
    color += (spec + clearCoat) * specColor;
    color *= ao;

    float alpha = clamp(AlbedoAlpha * LensOpacity, 0.0, 1.0);
    outColor = vec4(color, alpha);
}
#endif
