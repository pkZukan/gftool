#version 420 core

uniform sampler2D BaseColorMap;
uniform sampler2D LayerMaskMap;
uniform sampler2D NormalMap;
uniform sampler2D RoughnessMap;
uniform sampler2D MetallicMap;
uniform sampler2D AOMap;
uniform sampler2D HighlightMaskMap;
uniform sampler2D ParallaxMap;

uniform vec4 UVScaleOffset;
uniform vec4 UVScaleOffsetNormal;
uniform int UVTransformMode;

uniform vec4 BaseColor;
uniform vec4 BaseColorLayer1;
uniform vec4 BaseColorLayer2;
uniform vec4 BaseColorLayer3;
uniform vec4 BaseColorLayer4;
uniform vec4 BaseColorLayer5;
uniform vec4 BaseColorLayer6;
uniform vec4 BaseColorLayer7;
uniform vec4 BaseColorLayer8;
uniform vec4 BaseColorLayer9;

uniform vec4 EmissionColor;
uniform vec4 EmissionColorLayer5;
uniform float EmissionIntensity;
uniform float EmissionIntensityLayer1;
uniform float EmissionIntensityLayer2;
uniform float EmissionIntensityLayer3;
uniform float EmissionIntensityLayer4;
uniform float EmissionIntensityLayer5;

uniform float Metallic;
uniform float Roughness;
uniform float MetallicLayer5;
uniform float RoughnessLayer5;

uniform float AOIntensity;
uniform float AOIntensityFresnel;

uniform float FresnelAlphaMin;
uniform float FresnelAlphaMax;
uniform float FresnelAngleBias;
uniform float FresnelPower;

uniform bool EnableBaseColorMap;
uniform bool EnableAlphaTest;
uniform bool BaseColorMultiply;
uniform bool EnableLayerMaskMap;
uniform bool EnableNormalMap;
uniform bool EnableRoughnessMap;
uniform bool EnableMetallicMap;
uniform bool EnableAOMap;
uniform bool EnableHighlightMaskMap;
uniform bool EnableParallaxMap;
uniform float ParallaxHeight;
uniform int NumMaterialLayer;
uniform bool EnableVertexColor;
uniform bool FlipNormalY;
uniform bool ReconstructNormalZ;
uniform bool TwoSidedDiffuse;
uniform float LightWrap;
uniform float SpecularScale;
uniform float AlphaTestThreshold;
uniform float DiscardValue;
uniform float NormalHeight;
uniform int UVIndexLayerMask;
uniform int UVIndexAO;

uniform vec3 LightDirection;
uniform vec3 LightColor;
uniform vec3 AmbientColor;
uniform vec3 CameraPos;
uniform bool HasTangents;
uniform bool HasBinormals;

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

vec2 WrapUvIfOutside01(vec2 uv)
{
    if (any(lessThan(uv, vec2(0.0))) || any(greaterThan(uv, vec2(1.0))))
    {
        return fract(uv);
    }
    return uv;
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

mat3 ComputeTbn(vec3 n, vec2 uv)
{
    if (HasTangents)
    {
        vec3 bitangent = HasBinormals ? normalize(Binormal) : normalize(Bitangent);
        if (dot(bitangent, bitangent) < 0.0001)
        {
            bitangent = normalize(cross(n, normalize(Tangent)));
        }
        return mat3(normalize(Tangent), bitangent, n);
    }

    return CotangentFrame(n, FragPos, uv);
}

float FresnelGeneral(float f0, float f90, float u, float exponent)
{
    float t = pow(clamp(1.0 - u, 0.0, 1.0), max(exponent, 0.00001));
    return mix(f0, f90, t);
}

float D_GGX(float a2, float NdotH)
{
    float denom = (NdotH * NdotH) * (a2 - 1.0) + 1.0;
    return a2 / (3.14159265 * denom * denom);
}

float G_SchlickGGX(float k, float NdotV)
{
    return NdotV / (NdotV * (1.0 - k) + k);
}

float G_Smith(float k, float NdotV, float NdotL)
{
    return G_SchlickGGX(k, NdotV) * G_SchlickGGX(k, NdotL);
}

vec3 F_Schlick(vec3 F0, float VdotH)
{
    return F0 + (1.0 - F0) * pow(clamp(1.0 - VdotH, 0.0, 1.0), 5.0);
}

vec2 ApplyParallaxOffset(vec2 uvBase, vec2 uvHeight, vec3 viewDir, vec3 n, float heightScale)
{
    if (!EnableParallaxMap || !HasTangents)
    {
        return uvBase;
    }

    mat3 tbn = ComputeTbn(n, uvHeight);
    vec3 viewTs = normalize(transpose(tbn) * viewDir);
    float denom = max(viewTs.z, 0.0001);
    float heightSample = texture(ParallaxMap, uvHeight).r;
    float heightScaled = heightSample * heightScale;
    vec2 offset = (viewTs.xy / denom) * heightScaled;
    return uvBase + offset;
}

void main()
{
    vec3 n = normalize(Normal);
    vec3 viewDir = normalize(CameraPos - FragPos);
    float nv = clamp(dot(n, viewDir), 0.00001, 1.0);

    vec2 baseUv = vec2(SelectUv(0).x, 1.0 - SelectUv(0).y);
    vec2 uvBase = WrapUvIfOutside01(ApplyUvTransform(baseUv, UVScaleOffset, UVTransformMode));
    vec2 uvNormalBase = WrapUvIfOutside01(ApplyUvTransform(baseUv, UVScaleOffsetNormal, UVTransformMode));

    bool useLayerMask = EnableLayerMaskMap && (NumMaterialLayer > 0);
    vec4 layerMask = vec4(0.0);
    float baseLayerWeight = 1.0;
    if (useLayerMask)
    {
        vec2 layerBase = (UVIndexLayerMask == 1) ? vec2(SelectUv(1).x, 1.0 - SelectUv(1).y) : baseUv;
        vec2 uvLayer = WrapUvIfOutside01(ApplyUvTransform(layerBase, UVScaleOffset, UVTransformMode));
        layerMask = texture(LayerMaskMap, uvLayer);
        if (dot(BaseColorLayer2.rgb, BaseColorLayer2.rgb) < 0.000001) layerMask.g = 0.0;
        if (dot(BaseColorLayer3.rgb, BaseColorLayer3.rgb) < 0.000001) layerMask.b = 0.0;
        if (dot(BaseColorLayer4.rgb, BaseColorLayer4.rgb) < 0.000001) layerMask.a = 0.0;
        baseLayerWeight = clamp(1.0 - dot(vec4(1.0), layerMask), 0.0, 1.0);
    }

    vec2 uv = WrapUvIfOutside01(ApplyParallaxOffset(uvBase, uvBase, viewDir, n, ParallaxHeight));
    vec2 uvNormal = uvNormalBase;

    vec4 baseSample = EnableBaseColorMap ? texture(BaseColorMap, uv) : vec4(1.0);
    if (EnableAlphaTest && EnableBaseColorMap)
    {
        float alpha = baseSample.a * BaseColor.a;
        if (alpha < (DiscardValue > 0.0 ? DiscardValue : AlphaTestThreshold))
        {
            discard;
        }
    }

    vec3 texColor = baseSample.rgb;
    vec3 baseTint = BaseColorMultiply ? BaseColor.rgb : vec3(1.0);
    vec3 baseColor0 = texColor * baseTint;
    vec3 baseColor = baseColor0;
    if (useLayerMask)
    {
        vec3 layer1 = texColor * BaseColorLayer1.rgb;
        vec3 layer2 = texColor * BaseColorLayer2.rgb;
        vec3 layer3 = texColor * BaseColorLayer3.rgb;
        vec3 layer4 = texColor * BaseColorLayer4.rgb;
        baseColor = baseColor0 * baseLayerWeight +
                    layer1 * layerMask.r +
                    layer2 * layerMask.g +
                    layer3 * layerMask.b +
                    layer4 * layerMask.a;
    }

    vec3 vertexColor = EnableVertexColor ? Color.rgb : vec3(1.0);
    baseColor *= vertexColor;

    float roughness = EnableRoughnessMap ? texture(RoughnessMap, uv).r : Roughness;
    roughness = clamp(roughness, 0.04, 1.0);

    float metallic = EnableMetallicMap ? texture(MetallicMap, uv).r : Metallic;
    metallic = clamp(metallic, 0.0, 1.0);

    float ao = 1.0;
    if (EnableAOMap)
    {
        vec2 aoBase = (UVIndexAO == 1) ? vec2(SelectUv(1).x, 1.0 - SelectUv(1).y) : baseUv;
        vec2 uvAo = WrapUvIfOutside01(ApplyUvTransform(aoBase, UVScaleOffset, UVTransformMode));
        ao = texture(AOMap, uvAo).r;
    }

    vec3 emission = EmissionColor.rgb * EmissionIntensity;

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
        mat3 tbn = ComputeTbn(n, uvNormal);
        n = normalize(tbn * tangentNormal);
        nv = clamp(dot(n, viewDir), 0.00001, 1.0);
    }

    float fresnelBias = FresnelAngleBias;
    if (EnableVertexColor)
    {
        fresnelBias *= Color.r;
    }
    float layerMaskFresnel = FresnelGeneral(FresnelAlphaMin, FresnelAlphaMax, max(nv - fresnelBias, 0.0), FresnelPower);

    vec3 baseColorFresnel = BaseColorLayer5.rgb;
    vec3 emissionFresnel = BaseColorLayer5.rgb * EmissionIntensityLayer1;
    if (useLayerMask)
    {
        float weightBaseFresnel = baseLayerWeight;
        baseColorFresnel *= weightBaseFresnel;
        float invBaseEmission = max(1.0 - EmissionIntensity, 0.0);
        baseColorFresnel *= invBaseEmission;

        vec3 c1 = BaseColorLayer6.rgb * max(1.0 - EmissionIntensityLayer1, 0.0);
        vec3 e1 = BaseColorLayer6.rgb * EmissionIntensityLayer1;
        baseColorFresnel = mix(baseColorFresnel, c1, layerMask.r);
        emissionFresnel = mix(emissionFresnel, e1, layerMask.r);
        weightBaseFresnel = mix(weightBaseFresnel, 1.0, layerMask.r);

        if (NumMaterialLayer > 2)
        {
            vec3 c2 = BaseColorLayer7.rgb * max(1.0 - EmissionIntensityLayer2, 0.0);
            vec3 e2 = BaseColorLayer7.rgb * EmissionIntensityLayer2;
            baseColorFresnel = mix(baseColorFresnel, c2, layerMask.g);
            emissionFresnel = mix(emissionFresnel, e2, layerMask.g);
            weightBaseFresnel = mix(weightBaseFresnel, 1.0, layerMask.g);
        }
        if (NumMaterialLayer > 3)
        {
            vec3 c3 = BaseColorLayer8.rgb * max(1.0 - EmissionIntensityLayer3, 0.0);
            vec3 e3 = BaseColorLayer8.rgb * EmissionIntensityLayer3;
            baseColorFresnel = mix(baseColorFresnel, c3, layerMask.b);
            emissionFresnel = mix(emissionFresnel, e3, layerMask.b);
            weightBaseFresnel = mix(weightBaseFresnel, 1.0, layerMask.b);
        }
        if (NumMaterialLayer > 4)
        {
            vec3 c4 = BaseColorLayer9.rgb * max(1.0 - EmissionIntensityLayer4, 0.0);
            vec3 e4 = BaseColorLayer9.rgb * EmissionIntensityLayer4;
            baseColorFresnel = mix(baseColorFresnel, c4, layerMask.a);
            emissionFresnel = mix(emissionFresnel, e4, layerMask.a);
            weightBaseFresnel = mix(weightBaseFresnel, 1.0, layerMask.a);
        }

        float inv = 1.0 / max(weightBaseFresnel, 0.00001);
        baseColorFresnel *= inv;
        emissionFresnel *= inv;
    }
    else
    {
        baseColorFresnel *= max(1.0 - EmissionIntensity, 0.0);
    }

    baseColor = mix(baseColor, baseColorFresnel, layerMaskFresnel);
    emission = mix(emission, emissionFresnel, layerMaskFresnel);
    metallic = mix(metallic, MetallicLayer5, layerMaskFresnel);
    roughness = mix(roughness, RoughnessLayer5, layerMaskFresnel);

    float invAo = 1.0 - ao;
    invAo *= mix(AOIntensity, AOIntensityFresnel, layerMaskFresnel);
    ao = 1.0 - invAo;

    // Highlight layer (as emission) from
    if (EnableHighlightMaskMap)
    {
        float highlightMask = texture(HighlightMaskMap, uv).r;
        vec3 baseHighlight = EmissionColorLayer5.rgb * max(1.0 - EmissionIntensityLayer5, 0.0);
        vec3 emissionHighlight = EmissionColorLayer5.rgb * EmissionIntensityLayer5;
        baseColor = mix(baseColor, baseHighlight, highlightMask);
        emission = mix(emission, emissionHighlight, highlightMask);
    }

    vec3 lightDir = normalize(-LightDirection);
    vec3 halfDir = normalize(lightDir + viewDir);

    float nDotL = dot(n, lightDir);
    if (TwoSidedDiffuse)
        nDotL = abs(nDotL);
    else
        nDotL = max(nDotL, 0.0);

    float wrappedNdotL = (nDotL + LightWrap) / (1.0 + LightWrap);
    wrappedNdotL = clamp(wrappedNdotL, 0.0, 1.0);
    wrappedNdotL = smoothstep(0.0, 1.0, wrappedNdotL);

    // GGX microfacet (no IBL)
    float a = max(roughness * roughness, 0.002);
    float a2 = a * a;
    float NdotV = nv;
    float NdotL = clamp(nDotL, 0.0, 1.0);
    float NdotH = clamp(dot(n, halfDir), 0.0, 1.0);
    float VdotH = clamp(dot(viewDir, halfDir), 0.0, 1.0);

    vec3 F0 = mix(vec3(0.04), baseColor, metallic);
    vec3 F = F_Schlick(F0, VdotH);
    float k = (a + 1.0) * (a + 1.0) / 8.0;
    float D = D_GGX(a2, NdotH);
    float G = G_Smith(k, NdotV, NdotL);
    vec3 spec = (D * G) * F / max(4.0 * NdotV * NdotL, 0.0001);
    spec *= NdotL;

    // Deferred attributes (PBR shading computed in `gbuffer.fsh`)
    gAlbedo = vec4(baseColor, roughness);
    gNormal = vec4(n * 0.5 + 0.5, 1.0);
    gSpecular = vec4(ao, metallic, 0.0, 0.0);
    gAO = vec4(emission, 0.0); // emission, shadingModel=PBR
}
