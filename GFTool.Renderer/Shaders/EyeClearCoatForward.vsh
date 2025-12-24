#version 420 core

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
