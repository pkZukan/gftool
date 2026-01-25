#version 420 core

uniform sampler2D BaseColorMap;
uniform sampler2D LayerMaskMap;
uniform sampler2D NormalMap;
uniform sampler2D RoughnessMap;
uniform sampler2D MetallicMap;
uniform sampler2D AOMap;
uniform sampler2D DetailMaskMap;
uniform sampler2D SpecularMaskMap;
uniform sampler2D HighlightMaskMap;
uniform sampler2D DiscardMaskMap;

uniform sampler2D ShadowingColorMap;
uniform sampler2D ShadowingColorMaskMap;
uniform sampler2D RimLightMaskMap;
uniform sampler2D ParallaxMap;
uniform sampler2D EyelidShadowMaskMap;

uniform vec4 UVScaleOffset;
uniform vec4 UVScaleOffsetNormal;
uniform int UVTransformMode;
uniform vec4 UVCenter0;
uniform float UVRotation;
uniform float UVRotationNormal;
uniform vec4 BaseColor;
uniform vec4 BaseColorLayer1;
uniform vec4 BaseColorLayer2;
uniform vec4 BaseColorLayer3;
uniform vec4 BaseColorLayer4;
uniform vec4 ShadowingColor;
uniform vec4 ShadowingColorLayer1;
uniform vec4 ShadowingColorLayer2;
uniform vec4 ShadowingColorLayer3;
uniform vec4 ShadowingColorLayer4;
uniform vec4 EmissionColorLayer5;
uniform float EmissionIntensityLayer5;

uniform float LayerMaskScale1;
uniform float LayerMaskScale2;
uniform float LayerMaskScale3;
uniform float LayerMaskScale4;

uniform bool EnableBaseColorMap;
uniform bool EnableAlphaTest;
uniform bool BaseColorMultiply;
uniform bool EnableLayerMaskMap;
uniform bool EnableNormalMap;
uniform bool EnableRoughnessMap;
uniform bool EnableMetallicMap;
uniform bool EnableAOMap;
uniform bool EnableDetailMaskMap;
uniform bool EnableSpecularMaskMap;
uniform bool EnableHighlightMaskMap;
uniform bool EnableDiscardMaskMap;
uniform bool EnableShadowingColorMap;
uniform bool EnableShadowingColorMaskMap;
uniform bool EnableRimLightMaskMap;

uniform bool EnableEyeOptions;
uniform bool EnableHighlight;
uniform bool EnableParallaxMap;
uniform bool RequireEyelidShadowMap;
uniform bool EnableUVScaleOffsetNormal;

uniform int NumMaterialLayer;
uniform bool EnableVertexColor;
uniform bool LegacyMode;
uniform bool EnableHairSpecular;

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
uniform float SpecularIntensity;
uniform float SpecularOffset;
uniform float SpecularContrast;
uniform float SpecularLayer1Offset;
uniform float SpecularLayer1Contrast;
uniform float SpecularLayer1Intensity;
uniform float SpecularLayer2Offset;
uniform float SpecularLayer2Contrast;
uniform float SpecularLayer2Intensity;
uniform float SpecularLayer3Offset;
uniform float SpecularLayer3Contrast;
uniform float SpecularLayer3Intensity;
uniform float SpecularLayer4Offset;
uniform float SpecularLayer4Contrast;
uniform float SpecularLayer4Intensity;
uniform float OcclusionStrength;
uniform float AlphaTestThreshold;
uniform float DiscardValue;
uniform float ParallaxHeight;
uniform float HalfLambertBias;
uniform float ShadowingShift;
uniform float ShadowingContrast;
uniform float ShadowStrength;
uniform float RimLightOffset;
uniform float RimLightContrast;
uniform float RimLightIntensity;
uniform float BackRimLightIntensity;

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

uniform int UVIndexLayerMask;
uniform int UVIndexAO;
uniform bool HasUVIndexLayerMask;
uniform bool HasUVIndexAO;

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

vec2 ApplyUvTransformPivot(vec2 uv, vec4 srt, float rotation, int mode, vec2 pivot)
{
    if (mode == 1)
    {
        return uv + srt.zw;
    }

    float c = cos(rotation);
    float s = sin(rotation);
    mat2 r = mat2(c, -s, s, c);
    vec2 local = (uv - pivot) * srt.xy;
    vec2 rotated = r * local;
    return rotated + pivot + srt.zw;
}

vec2 FlipV(vec2 uv)
{
    return vec2(uv.x, 1.0 - uv.y);
}

vec2 TransformUvFd(vec2 uv, vec4 scaleOffset, vec2 center)
{
    vec2 stuv = scaleOffset.xy * (uv - center - scaleOffset.zw) + center;
    return FlipV(stuv);
}

vec2 TransformUvFd(vec2 uv, vec4 scaleOffset, float rotationRad, vec2 center)
{
    float s = sin(rotationRad);
    float c = cos(rotationRad);
    mat2 rotationMat = mat2(c, s, -s, c);
    vec2 srtuv = scaleOffset.xy * ((uv - center) * rotationMat - scaleOffset.zw) + center;
    return FlipV(srtuv);
}

vec2 ApplyUvTransformFd(vec2 uv, vec4 scaleOffset, float rotationRad, int mode, vec2 center)
{
    // 0=SRT, 1=T (translate only)
    if (mode == 1)
    {
        return FlipV(uv - scaleOffset.zw);
    }

    // convention: subtract offset, apply rotation around center, flip V after transform
    if (abs(rotationRad) > 0.000001)
    {
        return TransformUvFd(uv, scaleOffset, rotationRad, center);
    }
    return TransformUvFd(uv, scaleOffset, center);
}

vec2 WrapUvIfOutside01(vec2 uv)
{
    // Sampler wrap modes are loaded from the TRMTR sampler list; avoid forcing repeat here
    return uv;
}

float SGCheapContrast(float inputValue, float contrast)
{
    return clamp(mix(0.0 - contrast, contrast + 1.0, inputValue), 0.0, 1.0);
}

float SGSpecularParam(float specularOffset, float phongSpecular, float specularContrast, float specularIntensity)
{
    float specular = smoothstep(0.0 + specularOffset, 1.0 + specularOffset, phongSpecular);
    specular = SGCheapContrast(specular, specularContrast);
    return specular * specularIntensity;
}

void main()
{
    bool isEye = EnableEyeOptions || RequireEyelidShadowMap;
    vec2 rawUv0 = UV01.xy;
    vec2 rawUv1 = UV01.zw;
    vec2 rawUv1Safe = HasUv1 ? rawUv1 : rawUv0;

    bool useEyeCenter = EnableParallaxMap || isEye;
    vec2 uv = WrapUvIfOutside01(ApplyUvTransformFd(rawUv0, UVScaleOffset, UVRotation, UVTransformMode, useEyeCenter ? UVCenter0.xy : vec2(0.0)));

    vec2 uvNormal = uv;
    if (EnableUVScaleOffsetNormal)
    {
        // convention: normal UV uses UVScaleOffsetNormal without rotation/mode and flips V after transform
        uvNormal = WrapUvIfOutside01(TransformUvFd(rawUv0, UVScaleOffsetNormal, vec2(0.0)));
    }

    vec2 uvParallax = uv;
    vec2 uvNormalParallax = uvNormal;
    if (isEye && EnableParallaxMap)
    {
        vec3 nBase = normalize(Normal);
        vec3 viewDir = normalize(CameraPos - FragPos);

        mat3 tbn;
        if (HasTangents)
        {
            vec3 t = normalize(Tangent);
            vec3 b = HasBinormals ? normalize(Binormal) : normalize(Bitangent);
            if (dot(b, b) < 0.0001)
            {
                b = normalize(cross(nBase, t));
            }
            tbn = mat3(t, b, nBase);
        }
        else
        {
            tbn = CotangentFrame(nBase, FragPos, uvNormal);
        }

        vec3 parallaxRay = -normalize(transpose(tbn) * viewDir);
        float denomZ = max(abs(parallaxRay.z), 0.0001);

        vec2 primaryUvDdx = dFdx(uvParallax);
        vec2 primaryUvDdy = dFdy(uvParallax);

        const float parallaxMinStep = 2.0;
        const float parallaxMaxStep = 12.0;
        float stepBias = clamp(abs(dot(viewDir, nBase)), 0.0, 1.0);
        float steps = mix(parallaxMaxStep, parallaxMinStep, stepBias);
        float stepSize = 1.0 / steps;

        vec2 stepUv = parallaxRay.xy * vec2(-1.0, 1.0) / denomZ * ParallaxHeight * stepSize;
        stepUv *= normalize(max(abs(primaryUvDdx + primaryUvDdy), vec2(0.00001)));
        stepUv *= 1.0 - pow(1.0 - stepBias, 5.0);

        vec2 cur = vec2(1.0);      // x: height map, y: ray height
        vec2 prev = vec2(1.0, 1.1); // avoid divide by zero
        vec2 offset = vec2(0.0);

        int stepCount = int(floor(steps) + 2.0);
        for (int i = 0; i < stepCount; i++)
        {
            cur.x = texture(ParallaxMap, uvParallax + offset).r;
            if (cur.x >= cur.y)
            {
                float dh0 = cur.x - cur.y;
                float dh1 = prev.x - prev.y;
                float ratio = dh0 / max(dh0 - dh1, 0.00001);
                offset -= stepUv * ratio;
                break;
            }

            prev = cur;
            cur.y -= stepSize;
            offset += stepUv;
        }

        uvParallax += offset;
        uvNormalParallax += offset;
    }

    bool useLayerMask = EnableLayerMaskMap && (NumMaterialLayer > 0);
    vec4 layerMask = vec4(0.0);
    float baseLayerWeight = 1.0;
    if (useLayerMask)
    {
        int index = HasUVIndexLayerMask ? UVIndexLayerMask : -1;
        vec2 maskUv = uvParallax;
        if (index >= 0)
        {
            // When the TRMTR specifies a UV index override for the layer mask, still apply the material UV transform
            // so masks authored in the same transformed UV space don't collapse to edges
            vec2 rawMaskUv = (index == 0) ? rawUv0 : rawUv1Safe;
            maskUv = WrapUvIfOutside01(ApplyUvTransformFd(rawMaskUv, UVScaleOffset, UVRotation, UVTransformMode, useEyeCenter ? UVCenter0.xy : vec2(0.0)));
        }
        layerMask = texture(LayerMaskMap, maskUv);
        layerMask *= vec4(LayerMaskScale1, LayerMaskScale2, LayerMaskScale3, LayerMaskScale4);
        if (dot(BaseColorLayer2.rgb, BaseColorLayer2.rgb) < 0.000001) layerMask.g = 0.0;
        if (dot(BaseColorLayer3.rgb, BaseColorLayer3.rgb) < 0.000001) layerMask.b = 0.0;
        if (dot(BaseColorLayer4.rgb, BaseColorLayer4.rgb) < 0.000001) layerMask.a = 0.0;

        float layerSum = clamp(dot(vec4(1.0), layerMask), 0.0, 1.0);
        baseLayerWeight = clamp(1.0 - layerSum, 0.0, 1.0);
    }

    vec4 baseSample = EnableBaseColorMap ? texture(BaseColorMap, uvParallax) : vec4(1.0);
    if (EnableAlphaTest)
    {
        float maskValue = EnableDiscardMaskMap ? texture(DiscardMaskMap, uvParallax).r : baseSample.a;
        float threshold = EnableDiscardMaskMap ? DiscardValue : AlphaTestThreshold;
        if (maskValue < threshold)
        {
            discard;
        }
    }

    vec3 baseSampleRgb = baseSample.rgb;

    // Layer blending tweaks
    // don't multiply vertex color into base albedo (Color is used for other masks in the full technique)
    // use sequential `mix()` blending instead of weighted sums (the game treats mask channels as true blend weights)
    vec3 baseColorRgb = BaseColor.rgb * baseSampleRgb;

    vec3 layer1 = BaseColorLayer1.rgb;
    vec3 layer2 = BaseColorLayer2.rgb;
    vec3 layer3 = BaseColorLayer3.rgb;
    vec3 layer4 = BaseColorLayer4.rgb;
    if (BaseColorMultiply)
    {
        layer1 *= baseSampleRgb;
        layer2 *= baseSampleRgb;
        layer3 *= baseSampleRgb;
        layer4 *= baseSampleRgb;
    }

    if (useLayerMask)
    {
        baseColorRgb *= baseLayerWeight;
        baseColorRgb = mix(baseColorRgb, layer1, layerMask.r);
        baseColorRgb = mix(baseColorRgb, layer2, layerMask.g);
        baseColorRgb = mix(baseColorRgb, layer3, layerMask.b);
        baseColorRgb = mix(baseColorRgb, layer4, layerMask.a);
    }

    vec3 shadowingColorRgb = ShadowingColor.rgb;
    if (useLayerMask)
    {
        shadowingColorRgb *= baseLayerWeight;
        shadowingColorRgb = mix(shadowingColorRgb, ShadowingColorLayer1.rgb, layerMask.r);
        shadowingColorRgb = mix(shadowingColorRgb, ShadowingColorLayer2.rgb, layerMask.g);
        shadowingColorRgb = mix(shadowingColorRgb, ShadowingColorLayer3.rgb, layerMask.b);
        shadowingColorRgb = mix(shadowingColorRgb, ShadowingColorLayer4.rgb, layerMask.a);
    }

    vec3 albedo = baseColorRgb;

    if (isEye && RequireEyelidShadowMap)
    {
        float eyelidShadow = texture(EyelidShadowMaskMap, uvParallax).r;
        float eyelidFactor = mix(1.0, 0.65, clamp(eyelidShadow, 0.0, 1.0));
        albedo *= eyelidFactor;
        shadowingColorRgb *= eyelidFactor;
    }

    float highlightMaskSample = EnableHighlightMaskMap ? texture(HighlightMaskMap, uvParallax).r : 0.0;
    vec3 highlightAdd = vec3(0.0);
    if (EnableHighlight && EnableHighlightMaskMap)
    {
        float highlightMask = clamp(highlightMaskSample, 0.0, 1.0);
        vec3 highlight = EmissionColorLayer5.rgb * EmissionIntensityLayer5;
        if (isEye)
        {
            // For eyes, treat highlight as an unlit additive term (closer to how glints behave in game)
            highlightAdd = highlight * highlightMask;
        }
        else
        {
            albedo = mix(albedo, highlight, highlightMask);
            shadowingColorRgb = mix(shadowingColorRgb, highlight, highlightMask);
        }
    }

    int aoIndex = HasUVIndexAO ? UVIndexAO : -1;
    vec2 aoUv = (aoIndex < 0) ? uvParallax : FlipV((aoIndex == 0) ? rawUv0 : rawUv1Safe);
    float aoSample = EnableAOMap ? texture(AOMap, aoUv).r : 1.0;
    float occStrength = (OcclusionStrength <= 0.0) ? 1.0 : OcclusionStrength;
    float ao = pow(clamp(aoSample, 0.0, 1.0), occStrength);
    float aoOut = isEye ? 1.0 : ao;

    vec3 n = normalize(Normal);
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

    if (EnableNormalMap)
    {
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

    float halfLambert = nDotL * 0.5 + 0.5;
    float biasedHalfLambert = mix(halfLambert, halfLambert * halfLambert, clamp(HalfLambertBias, 0.0, 1.0));
    float wrappedNdotL = biasedHalfLambert;
    if (LightWrap > 0.0)
    {
        float lw = (nDotL + LightWrap) / (1.0 + LightWrap);
        wrappedNdotL = clamp(lw, 0.0, 1.0);
        wrappedNdotL = smoothstep(0.0, 1.0, wrappedNdotL);
    }

    float roughness = EnableRoughnessMap ? texture(RoughnessMap, uv).r : 0.35;
    roughness = clamp(roughness, 0.04, 1.0);
    float metallic = EnableMetallicMap ? texture(MetallicMap, uv).r : 0.0;

    float specPower = mix(16.0, 96.0, 1.0 - roughness);
    if (EnableHairSpecular)
    {
        specPower = mix(32.0, 256.0, 1.0 - roughness);
    }
    float phongSpec = pow(max(dot(n, halfDir), 0.0), specPower);
    phongSpec *= (1.0 - roughness);
    phongSpec *= wrappedNdotL;

    float specMask = EnableSpecularMaskMap ? texture(SpecularMaskMap, uvParallax).r : 1.0;
    if (isEye)
    {
        // Eye materials often rely more on highlight/glint masks than explicit specular masks
        specMask = max(specMask, clamp(highlightMaskSample, 0.0, 1.0));
    }
    float specularOffset = SpecularOffset;
    float specularContrast = SpecularContrast;
    float specularIntensity = SpecularIntensity;
    if (useLayerMask)
    {
        vec3 spec0 = vec3(SpecularOffset, SpecularContrast, SpecularIntensity);
        vec3 spec1 = vec3(SpecularLayer1Offset, SpecularLayer1Contrast, SpecularLayer1Intensity);
        vec3 spec2 = vec3(SpecularLayer2Offset, SpecularLayer2Contrast, SpecularLayer2Intensity);
        vec3 spec3 = vec3(SpecularLayer3Offset, SpecularLayer3Contrast, SpecularLayer3Intensity);
        vec3 spec4 = vec3(SpecularLayer4Offset, SpecularLayer4Contrast, SpecularLayer4Intensity);
        vec3 mixed = mix(spec0, spec1, layerMask.r);
        mixed = mix(mixed, spec2, layerMask.g);
        mixed = mix(mixed, spec3, layerMask.b);
        mixed = mix(mixed, spec4, layerMask.a);
        specularOffset = mixed.x;
        specularContrast = mixed.y;
        specularIntensity = mixed.z;
    }
    float spec = SGSpecularParam(specularOffset, phongSpec, specularContrast, specularIntensity);

    vec3 diffuse = albedo * (1.0 - metallic);
    vec3 specColor = mix(vec3(0.04), albedo, metallic);
    vec3 color = AmbientColor * albedo + LightColor * wrappedNdotL * diffuse;

    if (EnableShadowingColorMap || EnableShadowingColorMaskMap)
    {
        vec3 shadowTex = EnableShadowingColorMap ? texture(ShadowingColorMap, uv).rgb : vec3(1.0);
        float shadowMask = EnableShadowingColorMaskMap ? texture(ShadowingColorMaskMap, uv).r : 1.0;
        vec3 shadowTint = mix(vec3(1.0), shadowingColorRgb * shadowTex, shadowMask);

        float shadowStrength = clamp(1.0 - wrappedNdotL + ShadowingShift, 0.0, 1.0);
        float contrast = clamp(ShadowingContrast, 0.0, 1.0);
        shadowStrength = pow(shadowStrength, mix(1.0, 3.5, contrast));
        color = mix(color, color * shadowTint, shadowStrength);
    }

    float specBoost = EnableHairSpecular ? 1.25 : 1.0;
    float shadowScale = clamp(1.0 + ShadowStrength, 0.0, 2.0);
    vec3 specTerm = (spec * specColor) * (SpecularScale * specMask * specBoost) * shadowScale;
    color += specTerm;

    vec3 eyeSpecTerm = vec3(0.0);
    if (isEye)
    {
        // Extra clearcoat style sparkle for eyes (brings back the "pop" without needing probe/IBL)
        float eyeSparkle = pow(max(dot(n, halfDir), 0.0), 384.0) * wrappedNdotL;
        float sparkleMask = 0.15 + 0.85 * clamp(highlightMaskSample, 0.0, 1.0);
        eyeSpecTerm = LightColor * eyeSparkle * sparkleMask * 0.8;
        color += eyeSpecTerm;
    }

    if (EnableRimLightMaskMap)
    {
        float rimMask = texture(RimLightMaskMap, uv).r;
        float rimBase = 1.0 - max(dot(n, viewDir), 0.0);
        float rim = clamp(rimBase + RimLightOffset, 0.0, 1.0);
        float rimContrast = clamp(RimLightContrast, 0.0, 1.0);
        rim = pow(rim, mix(1.0, 6.0, rimContrast));
        rim *= RimLightIntensity;

        float backRim = pow(clamp(-dot(n, lightDir), 0.0, 1.0), 2.0) * BackRimLightIntensity;
        float rimTerm = (rim + backRim) * rimMask;
        color += rimTerm * vec3(1.0);
    }

    // Store spec intensity for Specular debug view when running in pre lit mode
    vec3 specViewColor = specTerm + eyeSpecTerm;
    float specView = clamp(max(specViewColor.r, max(specViewColor.g, specViewColor.b)) * 4.0, 0.0, 1.0);
    color += highlightAdd;

    if (LegacyMode)
    {
        gAlbedo = vec4(albedo, 0.0);
        gNormal = vec4(normalize(Normal) * 0.5 + 0.5, 0.0);
        gSpecular = vec4(aoOut, specView, 0.0, 0.0);
        gAO = vec4(0.0, 0.0, 0.0, 1.0);
        return;
    }

    gAlbedo = vec4(color, 0.0);
    gNormal = vec4(n * 0.5 + 0.5, clamp(specMask, 0.0, 1.0));
    gSpecular = vec4(aoOut, specView, 0.0, 0.0);
    gAO = vec4(0.0, 0.0, 0.0, 1.0);
}
