#version 420 core

uniform sampler2D BaseColorMap;
uniform sampler2D LayerMaskMap;
uniform sampler2D EyeMaskMap;
uniform sampler2D NormalMap;
uniform sampler2D NormalMap1;
uniform sampler2D RoughnessMap;
uniform sampler2D AOMap;

uniform vec4 BaseColor;
uniform vec4 BaseColorLayer1;
uniform vec4 BaseColorLayer2;
uniform vec4 BaseColorLayer3;
uniform vec4 BaseColorLayer4;
uniform vec4 EyeHighlightColor;
uniform float LayerMaskScale1;
uniform float LayerMaskScale2;
uniform float LayerMaskScale3;
uniform float LayerMaskScale4;
uniform vec4 UVScaleOffset;
uniform vec4 UVScaleOffset1;
uniform vec2 EyeUVClamp;
uniform bool EnableHighlight;
uniform bool EnableEyePointLight;
uniform float RoughnessHighlight;
uniform float RoughnessClearCoat;
uniform float MetallicHighlight;
uniform vec3 EyePointLightPosition;

uniform bool EnableBaseColorMap;
uniform bool EnableLayerMaskMap;
uniform bool EnableEyeMaskMap;
uniform bool EnableEyeBaseSclera;
uniform bool ReplaceBaseColorWithLayers;
uniform bool EnableNormalMap;
uniform bool EnableNormalMap1;
uniform bool EnableRoughnessMap;
uniform bool EnableAOMap;
uniform bool NumMaterialLayer;
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
uniform vec3 EmissionColor;
uniform float EmissionStrength;
uniform float ParallaxInside;
uniform float IOR;

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

vec3 linearToSrgb(vec3 color)
{
    vec3 low = color * 12.92;
    vec3 high = 1.055 * pow(max(color, vec3(0.0)), vec3(1.0 / 2.4)) - 0.055;
    return clamp(mix(low, high, step(vec3(0.0031308), color)), 0.0, 1.0);
}

vec3 composeEyeLayers(vec3 baseColor, vec4 layerMask)
{
    vec3 color = baseColor;
    vec3 layer1 = BaseColorLayer1.rgb;
    vec3 layer2 = BaseColorLayer2.rgb;
    vec3 layer3 = BaseColorLayer3.rgb;
    vec3 layer4 = BaseColorLayer4.rgb;
    color = mix(color, ReplaceBaseColorWithLayers ? layer1 : color * layer1, layerMask.r);
    color = mix(color, ReplaceBaseColorWithLayers ? layer2 : color * layer2, layerMask.g);
    color = mix(color, ReplaceBaseColorWithLayers ? layer3 : color * layer3, layerMask.b);
    color = mix(color, ReplaceBaseColorWithLayers ? layer4 : color * layer4, layerMask.a);
    return color;
}

vec2 transformedEyeUv(vec2 source, vec4 transform)
{
    if (dot(abs(transform.xy), vec2(1.0)) < 0.0001)
        transform.xy = vec2(1.0);

    vec2 uv = vec2(
        source.x * transform.x - transform.z,
        1.0 - (source.y * transform.y - transform.w));
    return mix(uv, clamp(uv, vec2(0.0), vec2(1.0)), EyeUVClamp);
}

void main()
{
    vec2 animatedUv = transformedEyeUv(TexCoord, UVScaleOffset);
    vec2 animatedUv1 = transformedEyeUv(TexCoord, UVScaleOffset1);
    vec2 baseUv = animatedUv;

    bool useLayerMask = EnableLayerMaskMap && NumMaterialLayer;
    vec4 layerMask = useLayerMask ? texture(LayerMaskMap, animatedUv) : vec4(0.0);
    vec4 scaledLayerMask = clamp(
        layerMask * vec4(LayerMaskScale1, LayerMaskScale2, LayerMaskScale3, LayerMaskScale4),
        0.0,
        1.0);

    vec3 baseColor = (EnableBaseColorMap ? texture(BaseColorMap, baseUv).rgb : vec3(1.0)) * BaseColor.rgb;
    if (useLayerMask)
    {
        baseColor = composeEyeLayers(baseColor, scaledLayerMask);
    }

    float eyeGlint = EnableHighlight && EnableEyeMaskMap
        ? smoothstep(0.08, 0.92, texture(EyeMaskMap, animatedUv).r)
        : 0.0;

    vec3 vertexColor = EnableVertexColor ? Color.rgb : vec3(1.0);
    vec3 albedo = EnableVertexColor
        ? mix(vertexColor, baseColor, EnableBaseColorMap ? 0.5 : 0.0)
        : baseColor;

    float roughness = EnableRoughnessMap ? texture(RoughnessMap, baseUv).r : 0.15;
    if (EnableHighlight)
        roughness = min(roughness, clamp(RoughnessHighlight, 0.02, 1.0));
    roughness = clamp(roughness, 0.02, 1.0);

    float ao = EnableAOMap ? texture(AOMap, baseUv).r : 1.0;

    vec3 n = normalize(Normal);
    vec3 tangentNormal = vec3(0.0, 0.0, 1.0);
    if (EnableNormalMap && HasTangents)
    {
        vec4 nm = texture(NormalMap, animatedUv);
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
        vec4 nm1 = texture(NormalMap1, animatedUv1);
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
    float specPower = mix(32.0, 192.0, 1.0 - roughness);
    float iorScale = clamp(IOR - 1.0, 0.0, 1.0);
    specPower *= mix(1.0, 1.6, iorScale);
    float spec = pow(max(dot(n, halfDir), 0.0), specPower);
    float clearCoat = pow(max(dot(n, halfDir), 0.0), specPower * 2.0) * mix(0.35, 0.62, clamp(MetallicHighlight, 0.0, 1.0));
    float pointHighlight = 0.0;
    vec3 pointVector = EyePointLightPosition - FragPos;
    if (EnableEyePointLight && dot(pointVector, pointVector) > 0.000001)
    {
        vec3 pointLightDir = normalize(pointVector);
        float pointRoughness = clamp(RoughnessClearCoat, 0.08, 1.0);
        float pointPower = max(8.0 / (pointRoughness * pointRoughness) - 2.0, 64.0);
        pointHighlight = pow(max(dot(n, pointLightDir), 0.0), pointPower);
        pointHighlight = smoothstep(0.02, 0.8, pointHighlight);
        pointHighlight *= mix(0.4, 1.0, clamp(MetallicHighlight, 0.0, 1.0));
    }

    vec3 emission = EmissionColor * EmissionStrength * clamp(ParallaxInside, 0.0, 1.0);
    vec3 color = AmbientColor * albedo + LightColor * wrappedNdotL * albedo + emission;
    color = mix(color, linearToSrgb(EyeHighlightColor.rgb), clamp(max(eyeGlint, pointHighlight), 0.0, 0.94));

    gAlbedo = color;
    gNormal = n * 0.5 + 0.5;
    gSpecular = (spec + clearCoat) * vec3(0.8) * SpecularScale;
    gAO = vec3(ao);
}
