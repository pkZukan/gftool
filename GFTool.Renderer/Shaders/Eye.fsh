#version 420 core

uniform sampler2D BaseColorMap;
uniform sampler2D LayerMaskMap;
uniform sampler2D NormalMap;

uniform vec4 UVScaleOffset;
uniform vec4 UVScaleOffsetNormal;
uniform float UVRotation;
uniform float UVRotationNormal;
uniform int UVTransformMode;
uniform bool HasUVIndexLayerMask;

uniform vec4 BaseColor;
uniform vec4 BaseColorLayer1;
uniform vec4 BaseColorLayer2;
uniform vec4 BaseColorLayer3;
uniform vec4 BaseColorLayer4;
uniform vec4 BaseColorLayer5;

uniform vec4 EmissionColor;
uniform vec4 EmissionColorLayer1;
uniform vec4 EmissionColorLayer2;
uniform vec4 EmissionColorLayer3;
uniform vec4 EmissionColorLayer4;
uniform vec4 EmissionColorLayer5;

uniform float EmissionIntensity;
uniform float EmissionIntensityLayer1;
uniform float EmissionIntensityLayer2;
uniform float EmissionIntensityLayer3;
uniform float EmissionIntensityLayer4;
uniform float EmissionIntensityLayer5;

uniform bool EnableBaseColorMap;
uniform bool EnableNormalMap;
uniform bool EnableLayerMaskMap;
uniform bool EnableVertexColor;
uniform float Roughness;
uniform float RoughnessLayer1;
uniform float RoughnessLayer2;
uniform float RoughnessLayer3;
uniform float RoughnessLayer4;
uniform float RoughnessLayer5;
uniform int NumMaterialLayer;
uniform float NormalHeight;
uniform int UVIndexLayerMask;
uniform float LayerMaskScale1;
uniform float LayerMaskScale2;
uniform float LayerMaskScale3;
uniform float LayerMaskScale4;

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
in vec4 UV01;
in vec4 Color;
in vec3 Tangent;
in vec3 Bitangent;
in vec3 Binormal;

vec2 SelectUv(int index)
{
    return index == 1 ? UV01.zw : UV01.xy;
}

vec2 ApplyUvTransform(vec2 uv, vec4 srt, float rotation, int mode)
{
    // 0=SRT (scale+translate), 1=T (translate only)
    if (mode == 1)
    {
        return uv + srt.zw;
    }

    // Eye uses UV rotation about the center pivot
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
    // Eye textures frequently use atlases/animation frames and some meshes provide UVs outside [0,1]
    // Many TRMTR samplers for these maps are CLAMP, so we explicitly wrap into [0,1] here
    return fract(uv);
}

vec4 GetLayerMask(vec2 uv)
{
    vec4 mask = texture(LayerMaskMap, uv);
    if (NumMaterialLayer == 2)
    {
        mask.r *= LayerMaskScale1;
        mask.gba = vec3(0.0);
    }
    else if (NumMaterialLayer == 3)
    {
        mask.rg *= vec2(LayerMaskScale1, LayerMaskScale2);
        mask.ba = vec2(0.0);
    }
    else if (NumMaterialLayer == 4)
    {
        mask.rgb *= vec3(LayerMaskScale1, LayerMaskScale2, LayerMaskScale3);
        mask.a = 0.0;
    }
    else if (NumMaterialLayer >= 5)
    {
        mask *= vec4(LayerMaskScale1, LayerMaskScale2, LayerMaskScale3, LayerMaskScale4);
    }
    return clamp(mask, 0.0, 1.0);
}

void LerpBaseColorAndEmission(inout vec3 baseColor, float intensity)
{
    float inv = max(1.0 - intensity, 0.0);
    baseColor *= inv;
}

mat3 CotangentFrame(vec3 n, vec3 p, vec2 uv)
{
    vec3 dp1 = dFdx(p);
    vec3 dp2 = dFdy(p);
    vec2 duv1 = dFdx(uv);
    vec2 duv2 = dFdy(uv);

    vec3 dp2perp = cross(dp2, n);
    vec3 dp1perp = cross(n, dp1);
    vec3 t = dp2perp * duv1.x + dp1perp * duv2.x;
    vec3 b = dp2perp * duv1.y + dp1perp * duv2.y;

    float invmax = inversesqrt(max(dot(t, t), dot(b, b)));
    return mat3(t * invmax, b * invmax, n);
}

void main()
{
    vec2 baseUv = vec2(SelectUv(0).x, 1.0 - SelectUv(0).y);
    vec2 uv = WrapUv01(ApplyUvTransform(baseUv, UVScaleOffset, UVRotation, UVTransformMode));
    vec2 uvNormal = WrapUv01(ApplyUvTransform(baseUv, UVScaleOffsetNormal, UVRotationNormal, UVTransformMode));

    vec4 baseSample = EnableBaseColorMap ? texture(BaseColorMap, uv) : vec4(1.0);
    // Color textures are created as sRGB so sampling yields linear RGB
    vec3 baseSampleLinear = baseSample.rgb;
    vec3 baseColor0 = baseSampleLinear * BaseColor.rgb;
    vec3 emission0 = EmissionColor.rgb * EmissionIntensity;
    LerpBaseColorAndEmission(baseColor0, EmissionIntensity);

    float roughness0 = Roughness * baseSample.a;

    vec3 baseColor = baseColor0;
    vec3 emission = emission0;
    float roughness = roughness0;
    float debugMaskR = 0.0;

    bool useLayerMask = EnableLayerMaskMap && NumMaterialLayer > 1;
    if (useLayerMask)
    {
        // LayerMaskMap UVs are not always consistent across assets:
        // Some use UV0 vs UV1
        // Some expect unflipped V vs flipped V
        // Try all candidates and pick the one with strongest overall mask coverage
        vec2 uv0_raw = SelectUv(0);
        vec2 uv1_raw = SelectUv(1);
        vec2 uv0_flip = vec2(uv0_raw.x, 1.0 - uv0_raw.y);
        vec2 uv1_flip = vec2(uv1_raw.x, 1.0 - uv1_raw.y);

        vec4 m0f = GetLayerMask(WrapUv01(ApplyUvTransform(uv0_flip, UVScaleOffset, UVRotation, UVTransformMode)));
        vec4 m0r = GetLayerMask(WrapUv01(ApplyUvTransform(uv0_raw, UVScaleOffset, UVRotation, UVTransformMode)));
        vec4 m1f = GetLayerMask(WrapUv01(ApplyUvTransform(uv1_flip, UVScaleOffset, UVRotation, UVTransformMode)));
        vec4 m1r = GetLayerMask(WrapUv01(ApplyUvTransform(uv1_raw, UVScaleOffset, UVRotation, UVTransformMode)));

        float s0f = dot(m0f, vec4(1.0));
        float s0r = dot(m0r, vec4(1.0));
        float s1f = dot(m1f, vec4(1.0));
        float s1r = dot(m1r, vec4(1.0));

        vec4 mask = m0f;
        float best = s0f;

        if (s0r > best) { mask = m0r; best = s0r; }
        if (s1f > best) { mask = m1f; best = s1f; }
        if (s1r > best) { mask = m1r; best = s1r; }

        // If the material explicitly declares UVIndexLayerMask, restrict selection to that UV set,
        // but still auto pick flipped vs raw V
        if (HasUVIndexLayerMask && UVIndexLayerMask == 0)
        {
            mask = (s0r > s0f) ? m0r : m0f;
        }
        else if (HasUVIndexLayerMask && UVIndexLayerMask == 1)
        {
            mask = (s1r > s1f) ? m1r : m1f;
        }

        float weightBase = clamp(1.0 - dot(vec4(1.0), mask), 0.0, 1.0);
        baseColor = baseColor0 * weightBase;
        emission = emission0 * weightBase;
        roughness = roughness0 * weightBase;

        // Layer 1 (mask.r)
        vec3 layer1Color = baseSampleLinear * BaseColorLayer1.rgb;
        vec3 layer1Emission = EmissionColorLayer1.rgb * EmissionIntensityLayer1;
        LerpBaseColorAndEmission(layer1Color, EmissionIntensityLayer1);
        baseColor = mix(baseColor, layer1Color, mask.r);
        emission = mix(emission, layer1Emission, mask.r);
        roughness = mix(roughness, RoughnessLayer1, mask.r);
        weightBase = mix(weightBase, 1.0, mask.r);

        // Layer 2 (mask.g)
        if (NumMaterialLayer >= 3)
        {
            vec3 layer2Color = baseSampleLinear * BaseColorLayer2.rgb;
            vec3 layer2Emission = EmissionColorLayer2.rgb * EmissionIntensityLayer2;
            LerpBaseColorAndEmission(layer2Color, EmissionIntensityLayer2);
            baseColor = mix(baseColor, layer2Color, mask.g);
            emission = mix(emission, layer2Emission, mask.g);
            roughness = mix(roughness, RoughnessLayer2, mask.g);
            weightBase = mix(weightBase, 1.0, mask.g);
        }

        // Layer 3 (mask.b)
        if (NumMaterialLayer >= 4)
        {
            vec3 layer3Color = baseSampleLinear * BaseColorLayer3.rgb;
            vec3 layer3Emission = EmissionColorLayer3.rgb * EmissionIntensityLayer3;
            LerpBaseColorAndEmission(layer3Color, EmissionIntensityLayer3);
            baseColor = mix(baseColor, layer3Color, mask.b);
            emission = mix(emission, layer3Emission, mask.b);
            roughness = mix(roughness, RoughnessLayer3, mask.b);
            weightBase = mix(weightBase, 1.0, mask.b);
        }

        // Layer 4 (mask.a)
        if (NumMaterialLayer >= 5)
        {
            vec3 layer4Color = baseSampleLinear * BaseColorLayer4.rgb;
            vec3 layer4Emission = EmissionColorLayer4.rgb * EmissionIntensityLayer4;
            LerpBaseColorAndEmission(layer4Color, EmissionIntensityLayer4);
            baseColor = mix(baseColor, layer4Color, mask.a);
            emission = mix(emission, layer4Emission, mask.a);
            roughness = mix(roughness, RoughnessLayer4, mask.a);
            weightBase = mix(weightBase, 1.0, mask.a);
        }

        float inv = 1.0 / max(weightBase, 0.00001);
        baseColor *= inv;
        emission *= inv;
        roughness *= inv;

        debugMaskR = mask.r;
    }

    vec3 vertexColor = EnableVertexColor ? Color.rgb : vec3(1.0);
    vec3 albedo = baseColor * vertexColor;

    vec3 n = normalize(Normal);
    if (EnableNormalMap)
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
        {
            tangentNormal.y = -tangentNormal.y;
        }
        tangentNormal.xy *= max(NormalHeight, 0.0);

        mat3 tbn;
        if (HasTangents)
        {
            vec3 bitangent = HasBinormals ? normalize(Binormal) : normalize(Bitangent);
            if (dot(bitangent, bitangent) < 0.0001)
            {
                bitangent = normalize(cross(n, normalize(Tangent)));
            }
            tbn = mat3(normalize(Tangent), bitangent, n);
        }
        else
        {
            tbn = CotangentFrame(n, FragPos, uvNormal);
        }
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

    roughness = clamp(roughness, 0.02, 1.0);
    float specPower = mix(64.0, 256.0, 1.0 - roughness);
    float spec = pow(max(dot(n, halfDir), 0.0), specPower);
    spec *= (1.0 - roughness) * wrappedNdotL;

    vec3 baseLit = AmbientColor * albedo + LightColor * wrappedNdotL * albedo;
    vec3 specLit = spec * vec3(0.9) * SpecularScale;
    vec3 finalShaded = baseLit + specLit;

    gAlbedo = vec4(finalShaded, 1.0);
    gNormal = vec4(n * 0.5 + 0.5, 1.0);
    gSpecular = vec4(1.0, 0.0, 0.0, 0.0); // AO=1, metallic=0
    gAO = vec4(emission, 1.0);            // emission, shadingModel=PreLit
}
