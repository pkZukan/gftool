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

uniform vec4 BaseColorLayer1;
uniform vec4 BaseColorLayer2;
uniform vec4 BaseColorLayer3;
uniform vec4 BaseColorLayer4;
uniform vec4 BaseColor;
uniform vec4 UVScaleOffset;
uniform vec4 UVScaleOffsetNormal;
uniform float LayerMaskScale1;
uniform float LayerMaskScale2;
uniform float LayerMaskScale3;
uniform float LayerMaskScale4;

uniform bool EnableBaseColorMap;
uniform bool EnableLayerMaskMap;
uniform bool EnableNormalMap;
uniform bool EnableNormalMap1;
uniform bool EnableNormalMap2;
uniform bool EnableRoughnessMap;
uniform bool EnableRoughnessMap1;
uniform bool EnableRoughnessMap2;
uniform bool EnableMetallicMap;
uniform bool EnableAOMap;
uniform bool EnableDetailMaskMap;
uniform bool NumMaterialLayer;
uniform bool EnableVertexColor;
uniform bool LegacyMode;
uniform bool EnableLerpBaseColorEmission;
uniform bool BaseColorMultiply;
uniform bool EnableUVScaleOffsetNormal;
uniform bool UseTrinityMaterialUv;

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

layout (location = 0) out vec3 gAlbedo;
layout (location = 1) out vec3 gNormal;
layout (location = 2) out vec3 gSpecular;
layout (location = 3) out vec3 gAO;

in vec3 FragPos;
in vec3 Normal;
in vec2 TexCoord;
in vec4 Color;
in vec3 Tangent;
in vec3 Bitangent;
in vec3 Binormal;

vec2 transformedUv(vec2 source, vec4 transform)
{
    if (dot(abs(transform.xy), vec2(1.0)) < 0.0001)
        transform.xy = vec2(1.0);
    return vec2(
        source.x * transform.x - transform.z,
        1.0 - (source.y * transform.y - transform.w));
}

vec3 applyPokemonPalette(
    vec3 sourceColor,
    vec4 weights,
    vec3 layer1,
    vec3 layer2,
    vec3 layer3,
    vec3 layer4)
{
    vec3 color = sourceColor;
    vec3 target1 = BaseColorMultiply ? sourceColor * layer1 : layer1;
    vec3 target2 = BaseColorMultiply ? sourceColor * layer2 : layer2;
    vec3 target3 = BaseColorMultiply ? sourceColor * layer3 : layer3;
    vec3 target4 = BaseColorMultiply ? sourceColor * layer4 : layer4;
    color = mix(color, target1, weights.r);
    color = mix(color, target2, weights.g);
    color = mix(color, target3, weights.b);
    color = mix(color, target4, weights.a);
    return color;
}

void main()
{
    vec2 legacyUv = vec2(TexCoord.x, 1.0 - TexCoord.y);
    vec2 uv = UseTrinityMaterialUv
        ? transformedUv(TexCoord, UVScaleOffset)
        : legacyUv;
    vec2 normalUv = UseTrinityMaterialUv && EnableUVScaleOffsetNormal
        ? transformedUv(TexCoord, UVScaleOffsetNormal)
        : uv;

    if (LegacyMode)
    {
        vec3 baseColor = (EnableBaseColorMap ? texture(BaseColorMap, uv).rgb : vec3(1.0)) * BaseColor.rgb;
        gAlbedo = baseColor;
        gNormal = normalize(Normal) * 0.5 + 0.5;
        gSpecular = vec3(0.0);
        gAO = vec3(1.0);
        return;
    }

    bool useLayerMask = EnableLayerMaskMap && NumMaterialLayer;
    vec4 layerMask = vec4(0.0);
    if (useLayerMask)
    {
        layerMask = texture(LayerMaskMap, uv);
    }
    vec4 scaledLayerMask = clamp(
        layerMask * vec4(LayerMaskScale1, LayerMaskScale2, LayerMaskScale3, LayerMaskScale4),
        0.0,
        1.0);

    vec3 baseColor = (EnableBaseColorMap ? texture(BaseColorMap, uv).rgb : vec3(1.0)) * BaseColor.rgb;
    vec3 layerColor = baseColor;
    if (useLayerMask)
    {
        layerColor = applyPokemonPalette(
            layerColor,
            scaledLayerMask,
            BaseColorLayer1.rgb,
            BaseColorLayer2.rgb,
            BaseColorLayer3.rgb,
            BaseColorLayer4.rgb);
    }
    vec3 vertexColor = EnableVertexColor ? Color.rgb : vec3(1.0);
    vec3 albedo = layerColor * vertexColor;

    float detailMask = EnableDetailMaskMap ? texture(DetailMaskMap, normalUv).r : 0.0;
    albedo *= mix(1.0, 0.85, detailMask);

    float roughness = EnableRoughnessMap ? texture(RoughnessMap, normalUv).r : 0.5;
    if (EnableRoughnessMap1 && useLayerMask)
    {
        float rough1 = texture(RoughnessMap1, normalUv).r;
        roughness = mix(roughness, rough1, layerMask.g);
    }
    if (EnableRoughnessMap2 && useLayerMask)
    {
        float rough2 = texture(RoughnessMap2, normalUv).r;
        roughness = mix(roughness, rough2, layerMask.b);
    }
    roughness = clamp(roughness, 0.04, 1.0);

    float metallic = EnableMetallicMap ? texture(MetallicMap, normalUv).r : 0.0;
    float ao = EnableAOMap ? texture(AOMap, normalUv).r : 1.0;

    vec3 n = normalize(Normal);
    vec3 tangentNormal = vec3(0.0, 0.0, 1.0);
    if (EnableNormalMap && HasTangents)
    {
        vec4 nm = texture(NormalMap, normalUv);
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
        vec4 nm1 = texture(NormalMap1, normalUv);
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
    if (EnableNormalMap2 && useLayerMask && HasTangents)
    {
        vec4 nm2 = texture(NormalMap2, normalUv);
        vec2 rg2 = nm2.rg * 2.0 - 1.0;
        vec3 n2;
        if (ReconstructNormalZ)
        {
            float nz2 = sqrt(max(0.0, 1.0 - dot(rg2, rg2)));
            n2 = vec3(rg2, nz2);
        }
        else
        {
            n2 = vec3(nm2.r, nm2.g, nm2.a) * 2.0 - 1.0;
        }
        if (FlipNormalY)
            n2.y = -n2.y;
        tangentNormal = normalize(mix(tangentNormal, n2, layerMask.b));
    }

    if ((EnableNormalMap || EnableNormalMap1 || EnableNormalMap2) && HasTangents)
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

    // Softer lighting: wrap diffuse + smooth curve so it doesn't look like a hard flashlight.
    float wrappedNdotL = (nDotL + LightWrap) / (1.0 + LightWrap);
    wrappedNdotL = clamp(wrappedNdotL, 0.0, 1.0);
    wrappedNdotL = smoothstep(0.0, 1.0, wrappedNdotL);
    wrappedNdotL = mix(wrappedNdotL, 1.0, 0.08); // lift deep shadows a bit

    // Softer spec: lower peak sharpness and tie it to the light term.
    float specPower = mix(16.0, 64.0, 1.0 - roughness);
    float spec = pow(max(dot(n, halfDir), 0.0), specPower);
    spec *= (1.0 - roughness);
    spec *= wrappedNdotL;

    vec3 diffuse = albedo * (1.0 - metallic);
    vec3 specColor = mix(vec3(0.04), albedo, metallic);

    vec3 color = AmbientColor * albedo + LightColor * wrappedNdotL * diffuse;

    gAlbedo = color;
    gNormal = n * 0.5 + 0.5;
    gSpecular = spec * specColor * SpecularScale;
    gAO = vec3(ao);
}
