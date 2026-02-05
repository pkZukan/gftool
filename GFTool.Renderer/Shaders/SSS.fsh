#version 420 core

uniform sampler2D BaseColorMap;
uniform sampler2D LayerMaskMap;
uniform sampler2D NormalMap;
uniform sampler2D RoughnessMap;
uniform sampler2D AOMap;
uniform sampler2D SSSMaskMap;

uniform vec4 UVScaleOffset;
uniform vec4 UVScaleOffsetNormal;

// SSS uses an explicit subsurface tint + controls rather than whitening the base color
uniform vec4 SubsurfaceColor;
uniform float SSSScatterPower;
uniform float SSSEmission;
uniform float SSSMaskStrength;

uniform bool EnableBaseColorMap;
uniform bool EnableLayerMaskMap;
uniform bool EnableNormalMap;
uniform bool EnableRoughnessMap;
uniform bool EnableAOMap;
uniform int NumMaterialLayer;
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
uniform int UVIndexLayerMask;
uniform int UVIndexAO;
uniform int UVTransformMode;

layout (location = 0) out vec4 gAlbedo;
layout (location = 1) out vec4 gNormal;
layout (location = 2) out vec4 gSpecular;
layout (location = 3) out vec4 gAO;

in vec3 FragPos;
in vec3 Normal;
in vec2 TexCoord;
in vec4 UV01;
in vec4 Color;
in vec3 Tangent;
in vec3 Bitangent;
in vec3 Binormal;

vec2 SelectUv(int index)
{
    if (index == 1)
    {
        return UV01.zw;
    }
    return UV01.xy;
}

vec2 ApplyUvTransform(vec2 uv, vec4 srt, int mode)
{
    if (mode == 1)
    {
        return uv + srt.zw;
    }
    return uv * srt.xy + srt.zw;
}

void main()
{
    vec2 baseUv = vec2(SelectUv(0).x, 1.0 - SelectUv(0).y);
    vec2 uv = ApplyUvTransform(baseUv, UVScaleOffset, UVTransformMode);
    vec2 uvNormal = ApplyUvTransform(baseUv, UVScaleOffsetNormal, UVTransformMode);

    bool useLayerMask = EnableLayerMaskMap && (NumMaterialLayer > 0);
    vec4 layerMask = vec4(0.0);
    if (useLayerMask)
    {
        vec2 layerBase = (UVIndexLayerMask == 1) ? vec2(SelectUv(1).x, 1.0 - SelectUv(1).y) : baseUv;
        vec2 uvLayer = ApplyUvTransform(layerBase, UVScaleOffset, UVTransformMode);
        layerMask = texture(LayerMaskMap, uvLayer);
    }

    float layerWeight = 1.0;
    if (useLayerMask)
    {
        layerWeight = clamp(1.0 - dot(vec4(1.0), layerMask), 0.0, 1.0);
        layerWeight = mix(layerWeight, 1.0, layerMask.r);
    }

    vec3 baseColor = EnableBaseColorMap ? texture(BaseColorMap, uv).rgb : vec3(1.0);
    vec3 vertexColor = EnableVertexColor ? Color.rgb : vec3(1.0);
    vec3 albedo = baseColor * vertexColor;
    albedo *= layerWeight;

    float roughness = EnableRoughnessMap ? texture(RoughnessMap, uv).r : 0.5;
    roughness = clamp(roughness, 0.04, 1.0);

    float ao = 1.0;
    if (EnableAOMap)
    {
        vec2 aoBase = (UVIndexAO == 1) ? vec2(SelectUv(1).x, 1.0 - SelectUv(1).y) : baseUv;
        vec2 uvAo = ApplyUvTransform(aoBase, UVScaleOffset, UVTransformMode);
        ao = texture(AOMap, uvAo).r;
    }
    float sssMask = EnableSSSMaskMap ? texture(SSSMaskMap, uv).r : 0.0;
    sssMask = clamp(sssMask * SSSMaskStrength, 0.0, 1.0);

    vec3 n = normalize(Normal);
    if (EnableNormalMap && HasTangents)
    {
        vec4 nm = texture(NormalMap, uvNormal);
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

    vec3 lightDir = normalize(-LightDirection);
    vec3 viewDir = normalize(CameraPos - FragPos);
    vec3 halfDir = normalize(lightDir + viewDir);

    float nDotL = dot(n, lightDir);
    if (TwoSidedDiffuse)
        nDotL = abs(nDotL);
    else
        nDotL = max(nDotL, 0.0);
    float wrappedNdotL = (nDotL + LightWrap) / (1.0 + LightWrap);
    float specPower = mix(16.0, 96.0, 1.0 - roughness);
    float spec = pow(max(dot(n, halfDir), 0.0), specPower);

    vec3 specColor = vec3(0.04);

    vec3 color = AmbientColor * albedo + LightColor * wrappedNdotL * albedo;

    // Approximate the game's SSS as an additive, warm tinted scattered light term (keeps base saturation)
    // This is intentionally lightweight (no IBL/shadows), but it avoids the "washed out" whitening behavior
    float nl01 = clamp(nDotL, 0.0, 1.0);
    float scatterPower = max(SSSScatterPower, 0.0001);
    float scatter = pow(1.0 - nl01, scatterPower);
    vec3 subsurface = albedo * SubsurfaceColor.rgb;
    vec3 sss = LightColor * scatter * (sssMask * SSSEmission) * subsurface;
    color += sss;

    // Pre lit shading (SSS is baked here); deferred pass only applies AO/SSAO and adds emission
    gAlbedo = vec4(color, 1.0);
    gNormal = vec4(n * 0.5 + 0.5, 1.0);
    gSpecular = vec4(ao, 0.0, 0.0, 0.0); // AO=ao, metallic=0
    gAO = vec4(0.0, 0.0, 0.0, 1.0);      // emission=0, shadingModel=PreLit
}
