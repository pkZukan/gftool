#version 420 core

uniform sampler2D BaseColorMap;
uniform sampler2D LayerMaskMap;
uniform sampler2D NormalMap;
uniform sampler2D NormalMap1;
uniform sampler2D RoughnessMap;
uniform sampler2D AOMap;

uniform vec4 UVScaleOffset;
uniform vec4 UVScaleOffsetNormal;
uniform float UVRotation;
uniform float UVRotationNormal;
uniform int UVTransformMode;
uniform int UVIndexLayerMask;
uniform int UVIndexAO;
uniform float LayerMaskScale1;
uniform float LayerMaskScale2;
uniform float LayerMaskScale3;
uniform float LayerMaskScale4;

uniform bool EnableBaseColorMap;
uniform bool EnableLayerMaskMap;
uniform bool EnableNormalMap;
uniform bool EnableNormalMap1;
uniform bool EnableRoughnessMap;
uniform bool EnableAOMap;
uniform int NumMaterialLayer;
uniform bool EnableSSSMaskMap;
uniform bool EnableVertexColor;
uniform bool HasUv1;

uniform vec4 BaseColor;
uniform vec4 BaseColorLayer1;
uniform vec4 BaseColorLayer2;
uniform vec4 BaseColorLayer3;
uniform vec4 BaseColorLayer4;
uniform vec4 BaseColorLayer5;

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
in vec4 UV01;
in vec4 Color;
in vec3 Tangent;
in vec3 Bitangent;
in vec3 Binormal;

vec2 SelectUv(int index)
{
    if (index == 1)
    {
        return HasUv1 ? UV01.zw : UV01.xy;
    }
    return UV01.xy;
}

vec2 ApplyUvTransform(vec2 uv, vec4 srt, float rotation, int mode)
{
    // 0=SRT (scale+rotate+translate), 1=T (translate only)
    if (mode == 1)
    {
        return uv + srt.zw;
    }

    const vec2 pivot = vec2(0.5, 0.5);
    float c = cos(rotation);
    float s = sin(rotation);
    mat2 r = mat2(c, -s, s, c);
    vec2 local = (uv - pivot) * srt.xy;
    vec2 rotated = r * local;
    return rotated + pivot + srt.zw;
}

vec2 WrapUv01(vec2 uv)
{
    return fract(uv);
}

vec4 GetLayerMask(vec2 uv)
{
    vec4 mask = texture(LayerMaskMap, uv);

    float s1 = (LayerMaskScale1 == 0.0) ? 1.0 : LayerMaskScale1;
    float s2 = (LayerMaskScale2 == 0.0) ? 1.0 : LayerMaskScale2;
    float s3 = (LayerMaskScale3 == 0.0) ? 1.0 : LayerMaskScale3;
    float s4 = (LayerMaskScale4 == 0.0) ? 1.0 : LayerMaskScale4;

    if (NumMaterialLayer == 2)
    {
        mask.r *= s1;
        mask.gba = vec3(0.0);
    }
    else if (NumMaterialLayer == 3)
    {
        mask.rg *= vec2(s1, s2);
        mask.ba = vec2(0.0);
    }
    else if (NumMaterialLayer == 4)
    {
        mask.rgb *= vec3(s1, s2, s3);
        mask.a = 0.0;
    }
    else if (NumMaterialLayer >= 5)
    {
        mask *= vec4(s1, s2, s3, s4);
    }

    return clamp(mask, 0.0, 1.0);
}

void main()
{
    vec2 baseUv = vec2(SelectUv(0).x, 1.0 - SelectUv(0).y);
    vec2 uv = WrapUv01(ApplyUvTransform(baseUv, UVScaleOffset, UVRotation, UVTransformMode));
    vec2 uvNormal = WrapUv01(ApplyUvTransform(baseUv, UVScaleOffsetNormal, UVRotationNormal, UVTransformMode));

    bool useLayerMask = EnableLayerMaskMap && (NumMaterialLayer > 1);
    vec4 layerMask = vec4(0.0);
    if (useLayerMask)
    {
        vec2 layerBase = (UVIndexLayerMask == 1) ? vec2(SelectUv(1).x, 1.0 - SelectUv(1).y) : baseUv;
        vec2 uvLayer = WrapUv01(ApplyUvTransform(layerBase, UVScaleOffset, UVRotation, UVTransformMode));
        layerMask = GetLayerMask(uvLayer);
    }

    vec4 baseSample = EnableBaseColorMap ? texture(BaseColorMap, uv) : vec4(1.0);
    vec3 baseSampleLinear = baseSample.rgb;

    vec3 baseColor = baseSampleLinear * BaseColor.rgb;
    if (useLayerMask)
    {
        vec3 c = baseColor;
        float weightBase = clamp(1.0 - dot(vec4(1.0), layerMask), 0.0, 1.0);
        c *= weightBase;

        vec3 layer1 = baseSampleLinear * BaseColorLayer1.rgb;
        c = mix(c, layer1, layerMask.r);
        weightBase = mix(weightBase, 1.0, layerMask.r);

        if (NumMaterialLayer >= 3)
        {
            vec3 layer2 = baseSampleLinear * BaseColorLayer2.rgb;
            c = mix(c, layer2, layerMask.g);
            weightBase = mix(weightBase, 1.0, layerMask.g);
        }

        if (NumMaterialLayer >= 4)
        {
            vec3 layer3 = baseSampleLinear * BaseColorLayer3.rgb;
            c = mix(c, layer3, layerMask.b);
            weightBase = mix(weightBase, 1.0, layerMask.b);
        }

        if (NumMaterialLayer >= 5)
        {
            vec3 layer4 = baseSampleLinear * BaseColorLayer4.rgb;
            c = mix(c, layer4, layerMask.a);
            weightBase = mix(weightBase, 1.0, layerMask.a);
        }

        float inv = 1.0 / max(weightBase, 0.00001);
        baseColor = c * inv;
    }

    vec3 vertexColor = EnableVertexColor ? Color.rgb : vec3(1.0);
    vec3 albedo = baseColor * vertexColor;

    float roughness = EnableRoughnessMap ? texture(RoughnessMap, uv).r : 0.15;
    roughness = clamp(roughness, 0.02, 1.0);

    float ao = 1.0;
    if (EnableAOMap)
    {
        vec2 aoBase = (UVIndexAO == 1) ? vec2(SelectUv(1).x, 1.0 - SelectUv(1).y) : baseUv;
        vec2 uvAo = WrapUv01(ApplyUvTransform(aoBase, UVScaleOffset, UVRotation, UVTransformMode));
        ao = texture(AOMap, uvAo).r;
    }

    vec3 n = normalize(Normal);
    vec3 tangentNormal = vec3(0.0, 0.0, 1.0);
    if (EnableNormalMap && HasTangents)
    {
        vec4 nm = texture(NormalMap, uvNormal);
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
        vec4 nm1 = texture(NormalMap1, uvNormal);
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
    wrappedNdotL = clamp(wrappedNdotL, 0.0, 1.0);
    wrappedNdotL = smoothstep(0.0, 1.0, wrappedNdotL);
    float specPower = mix(32.0, 192.0, 1.0 - roughness);
    float iorScale = clamp(IOR - 1.0, 0.0, 1.0);
    specPower *= mix(1.0, 1.6, iorScale);
    float spec = pow(max(dot(n, halfDir), 0.0), specPower);
    float clearCoat = pow(max(dot(n, halfDir), 0.0), specPower * 2.0) * 0.35;
    spec *= (1.0 - roughness) * wrappedNdotL;
    clearCoat *= wrappedNdotL;

    vec3 emission = EmissionColor * EmissionStrength * clamp(ParallaxInside, 0.0, 1.0);
    vec3 color = AmbientColor * albedo + LightColor * wrappedNdotL * albedo + emission;
    vec3 specColor = vec3(0.8) * SpecularScale;
    color += (spec + clearCoat) * specColor;
    color *= ao;

    float alpha = clamp(AlbedoAlpha * LensOpacity, 0.0, 1.0);
    outColor = vec4(color, alpha);
}
