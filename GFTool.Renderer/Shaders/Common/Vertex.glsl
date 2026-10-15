layout(location=0) in vec3 aPos;
layout(location=1) in vec3 aNormal;
layout(location=2) in vec2 aTexCoord;
layout(location=3) in vec4 aColor;
layout(location=4) in vec4 aTangent;
layout(location=5) in vec3 aBinormal;
layout(location=6) in vec4 aBlendIndices;
layout(location=7) in vec4 aBlendWeights;
uniform bool EnableSkinning, SwapBlendOrder;
uniform mat4 Bones[192];
uniform int BoneCount;
out vec3 FragPos, Normal, Tangent, Bitangent, Binormal;
out vec2 TexCoord;
out vec4 Color;

vec3 safe_normalize(vec3 v, vec3 fallback) { return dot(v,v) > 0.000001 ? normalize(v) : fallback; }
void main() {
    vec4 position = vec4(aPos, 1.0);
    mat3 skin = mat3(1.0);
    float total = dot(aBlendWeights, vec4(1.0));
    if (EnableSkinning && BoneCount > 0 && total > 0.00001) {
        vec4 weights = aBlendWeights / total;
        ivec4 ids = ivec4(aBlendIndices + 0.5);
        if (SwapBlendOrder) { weights = weights.wxyz; ids = ids.wxyz; }
        ids = clamp(ids, ivec4(0), ivec4(min(BoneCount,192)-1));
        mat4 matrix = weights.x*Bones[ids.x] + weights.y*Bones[ids.y] + weights.z*Bones[ids.z] + weights.w*Bones[ids.w];
        position = matrix * position;
        skin = mat3(matrix);
    }
    VERTEX = position.xyz;
    NORMAL = safe_normalize(skin*aNormal, vec3(0,1,0));
    TANGENT = safe_normalize(skin*aTangent.xyz, vec3(1,0,0));
    BINORMAL = safe_normalize(skin*aBinormal, cross(NORMAL,TANGENT));
    UV = aTexCoord; UV2 = aTexCoord; COLOR = aColor;
    vertex();
    FragPos = (model * vec4(VERTEX,1.0)).xyz;
    mat3 normalMatrix = transpose(inverse(mat3(model)));
    Normal = safe_normalize(normalMatrix*NORMAL, vec3(0,1,0));
    Tangent = safe_normalize(mat3(model)*TANGENT, vec3(1,0,0));
    Tangent = safe_normalize(Tangent - Normal*dot(Normal,Tangent), vec3(1,0,0));
    Bitangent = safe_normalize(cross(Normal,Tangent), vec3(0,0,1)) * (aTangent.w < 0.0 ? -1.0 : 1.0);
    Binormal = safe_normalize(mat3(model)*BINORMAL, Bitangent);
    TexCoord=UV; Color=COLOR;
    gl_Position=projection*view*vec4(FragPos,1.0);
}
