#version 420 core

layout (location = 0) in vec3 aPos;
layout (location = 6) in vec4 aBlendIndices;
layout (location = 7) in vec4 aBlendWeights;

uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;
uniform bool EnableSkinning;
uniform bool SwapBlendOrder;
uniform mat4 Bones[192];
uniform int BoneCount;

void main()
{
    vec4 localPos = vec4(aPos, 1.0);
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
    }

    gl_Position = projection * view * model * localPos;
}
