#version 420 core

uniform sampler2D BaseColorMap;
uniform sampler2D LayerMaskMap;
uniform sampler2D NormalMap;
uniform sampler2D RoughnessMap;
uniform sampler2D AOMap;
uniform sampler2D MetallicMap;

uniform vec4 BaseColor;
uniform vec4 BaseColorLayer1;
uniform vec4 BaseColorLayer2;
uniform vec4 BaseColorLayer3;
uniform vec4 BaseColorLayer4;
uniform float LayerMaskScale1;
uniform float LayerMaskScale2;
uniform float LayerMaskScale3;
uniform float LayerMaskScale4;
uniform vec4 UVScaleOffset;

uniform bool EnableBaseColorMap;
uniform bool EnableLayerMaskMap;
uniform bool EnableNormalMap;
uniform bool EnableRoughnessMap;
uniform bool EnableAOMap;
uniform bool NumMaterialLayer;
uniform bool EnableSSSMaskMap;
uniform bool EnableMetallicMap;
uniform bool EnableVertexColor;
uniform bool EnableThinRefraction;

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

layout (location = 0) out vec4 outColor;

in vec3 FragPos;
in vec3 Normal;
in vec2 TexCoord;
in vec4 Color;
in vec3 Tangent;
in vec3 Bitangent;
in vec3 Binormal;

void main()
{
    vec4 uvTransform = UVScaleOffset;
    if (dot(abs(uvTransform.xy), vec2(1.0)) < 0.0001)
        uvTransform.xy = vec2(1.0);
    vec2 uv = vec2(TexCoord.x, 1.0f - TexCoord.y) * uvTransform.xy + uvTransform.zw;
    bool useLayerMask = EnableLayerMaskMap && NumMaterialLayer;
    vec4 layerMask = useLayerMask ? texture(LayerMaskMap, uv) : vec4(0.0);
    layerMask = clamp(
        layerMask * vec4(LayerMaskScale1, LayerMaskScale2, LayerMaskScale3, LayerMaskScale4),
        0.0,
        1.0);

    vec4 baseSample = (EnableBaseColorMap ? texture(BaseColorMap, uv) : vec4(1.0)) * BaseColor;
    vec3 baseColor = baseSample.rgb;
    if (useLayerMask)
    {
        baseColor = mix(baseColor, clamp(BaseColorLayer1.rgb, 0.0, 1.0), layerMask.r);
        baseColor = mix(baseColor, clamp(BaseColorLayer2.rgb, 0.0, 1.0), layerMask.g);
        baseColor = mix(baseColor, clamp(BaseColorLayer3.rgb, 0.0, 1.0), layerMask.b);
        baseColor = mix(baseColor, clamp(BaseColorLayer4.rgb, 0.0, 1.0), layerMask.a);
    }
    vec3 vertexColor = EnableVertexColor ? Color.rgb : vec3(1.0);
    vec3 albedo = baseColor * vertexColor;

    float roughness = EnableRoughnessMap ? texture(RoughnessMap, uv).r : 0.35;
    roughness = clamp(roughness, 0.04, 1.0);
    float metallic = EnableMetallicMap ? texture(MetallicMap, uv).r : 0.0;
    float ao = EnableAOMap ? texture(AOMap, uv).r : 1.0;

    vec3 n = normalize(Normal);
    if (EnableNormalMap && HasTangents)
    {
        vec4 nm = texture(NormalMap, uv);
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

    vec3 diffuse = albedo * (1.0 - metallic);
    vec3 specColor = mix(vec3(0.04), albedo, metallic);
    vec3 color = AmbientColor * albedo + LightColor * wrappedNdotL * diffuse;

    color += spec * specColor * SpecularScale;
    color *= ao;

    float alpha = baseSample.a * (EnableVertexColor ? Color.a : 1.0);
    if (EnableThinRefraction)
    {
        float fresnel = pow(1.0 - clamp(abs(dot(n, viewDir)), 0.0, 1.0), 3.0);
        alpha = mix(0.035, 0.16, fresnel) * alpha;
        color = mix(vec3(1.0), color, 0.2);
    }
    if (alpha <= 0.003)
        discard;
    outColor = vec4(color, clamp(alpha, 0.0, 1.0));
}
