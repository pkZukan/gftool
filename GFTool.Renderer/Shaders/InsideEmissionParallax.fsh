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
uniform sampler2D SpecularMaskMap;
uniform sampler2D HighlightMaskMap;

uniform sampler2D ParallaxMap;
uniform sampler2D InsideEmissionParallaxIntensityMap;

uniform vec4 UVScaleOffset;
uniform vec4 UVScaleOffsetNormal;
uniform vec4 UVScaleOffsetInsideEmissionParallaxHeight;
uniform vec4 UVScaleOffsetInsideEmissionParallaxIntensity;

uniform int UVIndexLayerMask;
uniform int UVIndexAO;
uniform int UVIndexInsideEmissionParallaxHeight;
uniform int UVIndexInsideEmissionParallaxIntensity;
uniform bool HasUVIndexInsideEmissionParallaxHeight;
uniform bool HasUVIndexInsideEmissionParallaxIntensity;

uniform vec4 BaseColor;
uniform vec4 BaseColorLayer1;
uniform vec4 BaseColorLayer2;
uniform vec4 BaseColorLayer3;
uniform vec4 BaseColorLayer4;

uniform vec4 EmissionColor;
uniform vec4 EmissionColorLayer1;
uniform vec4 EmissionColorLayer2;
uniform vec4 EmissionColorLayer3;
uniform vec4 EmissionColorLayer4;
uniform float EmissionIntensity;
uniform float EmissionIntensityLayer1;
uniform float EmissionIntensityLayer2;
uniform float EmissionIntensityLayer3;
uniform float EmissionIntensityLayer4;
uniform bool EnableLerpBaseColorEmission;
uniform float LayerMaskScale1;
uniform float LayerMaskScale2;
uniform float LayerMaskScale3;
uniform float LayerMaskScale4;

uniform bool EnableBaseColorMap;
uniform bool EnableAlphaTest;
uniform bool BaseColorMultiply;
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
uniform bool EnableSpecularMaskMap;
uniform bool EnableHighlightMaskMap;
uniform bool EnableParallaxMap;
uniform int NumMaterialLayer;
uniform bool EnableVertexColor;
uniform bool LegacyMode;
uniform float AlphaTestThreshold;
uniform float DiscardValue;

uniform float InsideEmissionParallaxHeight;
uniform float InsideEmissionParallaxOffset;
uniform float InsideEmissionParallaxIntensity;
uniform float InsideEmissionParallaxIntensityMapRangeAvoidEdge;
uniform float InsideEmissionParallaxIntensityMapLatticeU;
uniform float InsideEmissionParallaxIntensityMapLatticeV;
uniform float InsideEmissionParallaxF0;
uniform float InsideEmissionParallaxF90;
uniform float EnableIEPTexture1;
uniform float EnableIEPTexture2;
uniform float EnableIEPTexture3;
uniform float EnableIEPTexture4;

uniform vec3 LightDirection;
uniform vec3 LightColor;
uniform vec3 AmbientColor;
uniform vec3 CameraPos;
uniform bool HasTangents;
uniform bool HasBinormals;
uniform bool HasUv1;
uniform bool FlipNormalY;
uniform bool ReconstructNormalZ;
uniform bool TwoSidedDiffuse;
uniform float LightWrap;
uniform float SpecularScale;
uniform int DebugShaderMode;

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
        if (!HasUv1)
        {
            return UV01.xy;
        }
        return UV01.zw;
    }
    return UV01.xy;
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

float FresnelSchlickScalar(float f0, float f90, float nv)
{
    float oneMinus = clamp(1.0 - nv, 0.0, 1.0);
    float t = oneMinus * oneMinus * oneMinus * oneMinus * oneMinus;
    return mix(f0, f90, t);
}

vec2 ApplyParallaxOffset(vec2 uvBase, vec2 uvHeight, vec3 viewDir, vec3 n, float heightScale, float heightOffset)
{
    if (!EnableParallaxMap)
    {
        return uvBase;
    }

    mat3 tbn = ComputeTbn(n, uvHeight);
    vec3 viewTs = normalize(transpose(tbn) * viewDir);
    float denom = max(viewTs.z, 0.0001);

    float heightSample = texture(ParallaxMap, uvHeight).r;
    float heightScaled = heightSample * heightScale + heightOffset;

    vec2 offset = (viewTs.xy / denom) * heightScaled;
    // Game shader adds the tangent space offset
    return uvBase + offset;
}

vec2 WrapUvIfOutside01(vec2 uv)
{
    if (any(lessThan(uv, vec2(0.0))) || any(greaterThan(uv, vec2(1.0))))
    {
        return fract(uv);
    }
    return uv;
}

vec2 ApplyIepIntensityWrap(vec2 uv, vec2 baseUv)
{
    float avoidEdge = InsideEmissionParallaxIntensityMapRangeAvoidEdge * 0.01;
    float revLatticeU = 1.0 / max(InsideEmissionParallaxIntensityMapLatticeU, 1.0);
    float revLatticeV = 1.0 / max(InsideEmissionParallaxIntensityMapLatticeV, 1.0);

    float uMin = baseUv.x - mod(baseUv.x, revLatticeU) + avoidEdge;
    float vMin = baseUv.y - mod(baseUv.y, revLatticeV) + avoidEdge;
    float uMod = revLatticeU - avoidEdge * 2.0;
    float vMod = revLatticeV - avoidEdge * 2.0;

    vec2 wrapped = uv;
    if (uMod > 0.00001)
    {
        wrapped.x = uMin + mod(wrapped.x - uMin, uMod);
    }
    if (vMod > 0.00001)
    {
        wrapped.y = vMin + mod(wrapped.y - vMin, vMod);
    }
    return wrapped;
}

void main()
{
    vec3 n = normalize(Normal);
    vec3 viewDir = normalize(CameraPos - FragPos);
    float nv = clamp(dot(n, viewDir), 0.0, 1.0);

    vec2 uv0 = vec2(SelectUv(0).x, 1.0 - SelectUv(0).y);
    vec2 uv1 = vec2(SelectUv(1).x, 1.0 - SelectUv(1).y);

    vec2 baseUv = vec2(SelectUv(0).x, 1.0 - SelectUv(0).y);
    vec2 uv = WrapUvIfOutside01(baseUv * UVScaleOffset.xy + UVScaleOffset.zw);

    vec2 uvNormal = WrapUvIfOutside01(baseUv * UVScaleOffsetNormal.xy + UVScaleOffsetNormal.zw);

    int iepHeightIndex = HasUVIndexInsideEmissionParallaxHeight ? UVIndexInsideEmissionParallaxHeight : -1;
    vec2 iepHeightBase = (iepHeightIndex == 1) ? uv1 : uv0;
    vec2 uvIepHeight = WrapUvIfOutside01(iepHeightBase * UVScaleOffsetInsideEmissionParallaxHeight.xy + UVScaleOffsetInsideEmissionParallaxHeight.zw);

    int iepIntensityIndex = HasUVIndexInsideEmissionParallaxIntensity ? UVIndexInsideEmissionParallaxIntensity : -1;
    vec2 iepIntensityBase = (iepIntensityIndex == 1) ? uv1 : uv0;
    vec2 uvIepIntensity = WrapUvIfOutside01(iepIntensityBase * UVScaleOffsetInsideEmissionParallaxIntensity.xy + UVScaleOffsetInsideEmissionParallaxIntensity.zw);

    vec2 uvParallax = WrapUvIfOutside01(ApplyParallaxOffset(uv, uvIepHeight, viewDir, n, InsideEmissionParallaxHeight, InsideEmissionParallaxOffset));
    vec2 uvNormalParallax = WrapUvIfOutside01(ApplyParallaxOffset(uvNormal, uvIepHeight, viewDir, n, InsideEmissionParallaxHeight, InsideEmissionParallaxOffset));

    // Intensity map uses the same height derived offset but with repeat range edge avoidance
    float iepHeightSample = texture(ParallaxMap, uvIepHeight).r * InsideEmissionParallaxHeight + InsideEmissionParallaxOffset;
    mat3 iepTbn = ComputeTbn(n, uvIepHeight);
    vec3 viewTs = normalize(transpose(iepTbn) * viewDir);
    float denom = max(viewTs.z, 0.0001);
    vec2 viewTsNorm = (viewTs.xy / denom);
    vec2 uvIntensityParallax = WrapUvIfOutside01(ApplyIepIntensityWrap(uvIepIntensity + viewTsNorm * iepHeightSample, uvIepIntensity));

    bool useLayerMask = EnableLayerMaskMap && (NumMaterialLayer > 0);
    vec4 layerMask = vec4(0.0);
    float baseLayerWeight = 1.0;
    if (useLayerMask)
    {
        vec2 layerBase = (UVIndexLayerMask == 1 ? uv1 : uv0);
        vec2 uvLayer = WrapUvIfOutside01(layerBase * UVScaleOffset.xy + UVScaleOffset.zw);
        layerMask = texture(LayerMaskMap, uvLayer);
        layerMask *= vec4(LayerMaskScale1, LayerMaskScale2, LayerMaskScale3, LayerMaskScale4);
        if (dot(BaseColorLayer2.rgb, BaseColorLayer2.rgb) < 0.000001) layerMask.g = 0.0;
        if (dot(BaseColorLayer3.rgb, BaseColorLayer3.rgb) < 0.000001) layerMask.b = 0.0;
        if (dot(BaseColorLayer4.rgb, BaseColorLayer4.rgb) < 0.000001) layerMask.a = 0.0;
        baseLayerWeight = clamp(1.0 - dot(vec4(1.0), layerMask), 0.0, 1.0);
    }

    if (DebugShaderMode != 0)
    {
        if (DebugShaderMode == 1)
        {
            vec3 iepDbg = texture(InsideEmissionParallaxIntensityMap, uvIntensityParallax).rgb;
            gAlbedo = vec4(iepDbg, 1.0);
            gNormal = vec4(vec3(0.5), 1.0);
            gSpecular = vec4(0.0);
            gAO = vec4(0.0, 0.0, 0.0, 1.0);
            return;
        }
        if (DebugShaderMode == 2)
        {
            // Visualize layer mask RGB
            gAlbedo = vec4(layerMask.rgb, 1.0);
            gNormal = vec4(vec3(0.5), 1.0);
            gSpecular = vec4(layerMask.a, 0.0, 0.0, 0.0);
            gAO = vec4(0.0, 0.0, 0.0, 1.0);
            return;
        }
        if (DebugShaderMode == 3)
        {
            // Visualize IEP UVs after parallax/wrap
            gAlbedo = vec4(vec3(fract(uvIntensityParallax), 0.0), 1.0);
            gNormal = vec4(vec3(0.5), 1.0);
            gSpecular = vec4(0.0);
            gAO = vec4(0.0, 0.0, 0.0, 1.0);
            return;
        }
        if (DebugShaderMode == 4)
        {
            // Visualize raw UV0 (Albedo view) and UV1 (Specular view)
            gAlbedo = vec4(vec3(fract(uv0), 0.0), 1.0);
            gNormal = vec4(vec3(0.5), 1.0);
            vec2 rawUv1 = vec2(UV01.z, 1.0 - UV01.w);
            gSpecular = vec4(vec3(fract(rawUv1), 0.0), 1.0);
            gAO = vec4(0.0, 0.0, 0.0, 1.0);
            return;
        }
    }

    vec4 baseSample = EnableBaseColorMap ? texture(BaseColorMap, uvParallax) : vec4(1.0);
    // Use BaseColor alpha for opacity
    float alpha = BaseColor.a;
    if (EnableAlphaTest && alpha < DiscardValue)
    {
        discard;
    }

    vec3 texColor = baseSample.rgb;
    vec3 baseTint = BaseColorMultiply ? BaseColor.rgb : vec3(1.0);
    vec3 baseColor0 = texColor * baseTint;
    vec3 baseLayerColor = baseColor0;
    vec3 layerColor = baseLayerColor;
    vec3 emission = EmissionColor.rgb * EmissionIntensity;

    if (useLayerMask)
    {
        float weightBase = baseLayerWeight;
        vec3 basePremult = baseColor0 * weightBase;
        if (EnableLerpBaseColorEmission)
        {
            basePremult *= max(1.0 - EmissionIntensity, 0.0);
        }

        vec3 emissionPremult = emission * weightBase;

        vec3 layer1 = baseColor0 * BaseColorLayer1.rgb;
        vec3 layer2 = baseColor0 * BaseColorLayer2.rgb;
        vec3 layer3 = baseColor0 * BaseColorLayer3.rgb;
        vec3 layer4 = baseColor0 * BaseColorLayer4.rgb;
        if (EnableLerpBaseColorEmission)
        {
            layer1 *= max(1.0 - EmissionIntensityLayer1, 0.0);
            layer2 *= max(1.0 - EmissionIntensityLayer2, 0.0);
            layer3 *= max(1.0 - EmissionIntensityLayer3, 0.0);
            layer4 *= max(1.0 - EmissionIntensityLayer4, 0.0);
        }

        basePremult = mix(basePremult, layer1, layerMask.r);
        emissionPremult = mix(emissionPremult, EmissionColorLayer1.rgb * EmissionIntensityLayer1, layerMask.r);
        weightBase = mix(weightBase, 1.0, layerMask.r);

        basePremult = mix(basePremult, layer2, layerMask.g);
        emissionPremult = mix(emissionPremult, EmissionColorLayer2.rgb * EmissionIntensityLayer2, layerMask.g);
        weightBase = mix(weightBase, 1.0, layerMask.g);

        basePremult = mix(basePremult, layer3, layerMask.b);
        emissionPremult = mix(emissionPremult, EmissionColorLayer3.rgb * EmissionIntensityLayer3, layerMask.b);
        weightBase = mix(weightBase, 1.0, layerMask.b);

        basePremult = mix(basePremult, layer4, layerMask.a);
        emissionPremult = mix(emissionPremult, EmissionColorLayer4.rgb * EmissionIntensityLayer4, layerMask.a);
        weightBase = mix(weightBase, 1.0, layerMask.a);

        float invWeight = 1.0 / max(weightBase, 0.00001);
        layerColor = basePremult * invWeight;
        emission = emissionPremult * invWeight;
    }

    vec3 vertexColor = EnableVertexColor ? Color.rgb : vec3(1.0);
    vec3 albedo = layerColor * vertexColor;

    float detailMask = EnableDetailMaskMap ? texture(DetailMaskMap, uvParallax).r : 0.0;
    albedo *= mix(1.0, 0.85, detailMask);

    float roughness = EnableRoughnessMap ? texture(RoughnessMap, uvParallax).r : 0.5;
    if (EnableRoughnessMap1 && useLayerMask)
    {
        float rough1 = texture(RoughnessMap1, uvParallax).r;
        roughness = mix(roughness, rough1, layerMask.g);
    }
    if (EnableRoughnessMap2 && useLayerMask)
    {
        float rough2 = texture(RoughnessMap2, uvParallax).r;
        roughness = mix(roughness, rough2, layerMask.b);
    }
    roughness = clamp(roughness, 0.04, 1.0);

    float metallic = EnableMetallicMap ? texture(MetallicMap, uvParallax).r : 0.0;
    float ao = 1.0;
    if (EnableAOMap)
    {
        vec2 aoBase = (UVIndexAO == 1 ? uv1 : uv0);
        vec2 uvAo = WrapUvIfOutside01(aoBase * UVScaleOffset.xy + UVScaleOffset.zw);
        ao = texture(AOMap, uvAo).r;
    }

    vec3 tangentNormal = vec3(0.0, 0.0, 1.0);
    if (EnableNormalMap)
    {
        vec4 nm = texture(NormalMap, uvNormalParallax);
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
        vec4 nm1 = texture(NormalMap1, uvNormalParallax);
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
        vec4 nm2 = texture(NormalMap2, uvNormalParallax);
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

    if ((EnableNormalMap || EnableNormalMap1 || EnableNormalMap2))
    {
        mat3 tbn = ComputeTbn(n, uvNormalParallax);
        n = normalize(tbn * tangentNormal);
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
    wrappedNdotL = mix(wrappedNdotL, 1.0, 0.08);

    float specPower = mix(16.0, 64.0, 1.0 - roughness);
    float spec = pow(max(dot(n, halfDir), 0.0), specPower);
    spec *= (1.0 - roughness);
    spec *= wrappedNdotL;

    vec3 diffuse = albedo * (1.0 - metallic);
    vec3 specColor = mix(vec3(0.04), albedo, metallic);
    vec3 color = AmbientColor * albedo + LightColor * wrappedNdotL * diffuse;

    float enableIepAny = max(max(EnableIEPTexture1, EnableIEPTexture2), max(EnableIEPTexture3, EnableIEPTexture4));
    if (enableIepAny > 0.0)
    {
        // uses RGB from InsideEmissionParallaxIntensityMap (dots are typically authored here)
        vec3 iep = texture(InsideEmissionParallaxIntensityMap, uvIntensityParallax).rgb;
        float fresnel = max(0.0, FresnelSchlickScalar(InsideEmissionParallaxF0, InsideEmissionParallaxF90, max(nv, 0.00001)));
        vec3 iepEmission = iep * (InsideEmissionParallaxIntensity * fresnel);

        float iepLayerPower = enableIepAny;
        if (useLayerMask && NumMaterialLayer > 1)
        {
            iepLayerPower = layerMask.r * EnableIEPTexture1 +
                            layerMask.g * EnableIEPTexture2 +
                            layerMask.b * EnableIEPTexture3 +
                            layerMask.a * EnableIEPTexture4;
        }

        emission += iepEmission * iepLayerPower;
    }

    float specMask = EnableSpecularMaskMap ? texture(SpecularMaskMap, uvParallax).r : 1.0;
    float highlightMask = EnableHighlightMaskMap ? texture(HighlightMaskMap, uvParallax).r : 0.0;
    float highlightBoost = mix(1.0, 2.0, highlightMask);
    vec3 specLit = spec * specColor * (SpecularScale * specMask * highlightBoost);
    vec3 finalShaded = color + specLit;

    gAlbedo = vec4(finalShaded, 1.0);
    gNormal = vec4(n * 0.5 + 0.5, 1.0);
    gSpecular = vec4(ao, 0.0, 0.0, 0.0);
    gAO = vec4(emission, 1.0); // emission, shadingModel=PreLit
}
