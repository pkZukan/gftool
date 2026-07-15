#version 420 core

uniform sampler2D BaseColorMap;
uniform sampler2D LayerMaskMap;
uniform sampler2D NormalMap;
uniform sampler2D RoughnessMap;
uniform sampler2D MetallicMap;
uniform sampler2D AOMap;

uniform vec4 BaseColor;
uniform vec4 BaseColorLayer1;
uniform vec4 BaseColorLayer2;
uniform vec4 BaseColorLayer3;
uniform vec4 BaseColorLayer4;
uniform vec4 IridescenceColor1;
uniform vec4 IridescenceColor2;
uniform vec4 IridescenceColor3;
uniform vec4 UVScaleOffset;
uniform float LayerMaskScale1;
uniform float LayerMaskScale2;
uniform float LayerMaskScale3;
uniform float LayerMaskScale4;
uniform float IridescenceBlend;
uniform float IridescenceCenter;
uniform float IridescencePower;

uniform bool EnableBaseColorMap;
uniform bool EnableLayerMaskMap;
uniform bool EnableNormalMap;
uniform bool EnableRoughnessMap;
uniform bool EnableMetallicMap;
uniform bool EnableAOMap;
uniform bool NumMaterialLayer;
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

void main()
{
    vec4 uvTransform = UVScaleOffset;
    if (dot(abs(uvTransform.xy), vec2(1.0)) < 0.0001)
        uvTransform.xy = vec2(1.0);
    vec2 uv = vec2(TexCoord.x, 1.0 - TexCoord.y) * uvTransform.xy + uvTransform.zw;

    vec3 albedo = (EnableBaseColorMap ? texture(BaseColorMap, uv).rgb : vec3(1.0)) * BaseColor.rgb;
    vec4 mask = EnableLayerMaskMap && NumMaterialLayer ? texture(LayerMaskMap, uv) : vec4(0.0);
    mask = clamp(mask * vec4(LayerMaskScale1, LayerMaskScale2, LayerMaskScale3, LayerMaskScale4), 0.0, 1.0);
    albedo = mix(albedo, clamp(BaseColorLayer1.rgb, 0.0, 1.0), mask.r);
    albedo = mix(albedo, clamp(BaseColorLayer2.rgb, 0.0, 1.0), mask.g);
    albedo = mix(albedo, clamp(BaseColorLayer3.rgb, 0.0, 1.0), mask.b);
    albedo = mix(albedo, clamp(BaseColorLayer4.rgb, 0.0, 1.0), mask.a);
    if (EnableVertexColor)
        albedo *= Color.rgb;

    vec3 n = normalize(Normal);
    if (EnableNormalMap && HasTangents)
    {
        vec4 sampleNormal = texture(NormalMap, uv);
        vec2 rg = sampleNormal.rg * 2.0 - 1.0;
        float z = ReconstructNormalZ ? sqrt(max(0.0, 1.0 - dot(rg, rg))) : sampleNormal.a * 2.0 - 1.0;
        vec3 tangentNormal = vec3(rg, z);
        if (FlipNormalY)
            tangentNormal.y = -tangentNormal.y;
        vec3 bitangent = HasBinormals ? normalize(Binormal) : normalize(Bitangent);
        if (dot(bitangent, bitangent) < 0.0001)
            bitangent = normalize(cross(n, normalize(Tangent)));
        n = normalize(mat3(normalize(Tangent), bitangent, n) * tangentNormal);
    }

    vec3 lightDir = normalize(-LightDirection);
    vec3 viewDir = normalize(CameraPos - FragPos);
    vec3 halfDir = normalize(lightDir + viewDir);
    float nDotL = dot(n, lightDir);
    nDotL = TwoSidedDiffuse ? abs(nDotL) : max(nDotL, 0.0);
    float diffuseTerm = clamp((nDotL + LightWrap) / (1.0 + LightWrap), 0.0, 1.0);
    float roughness = EnableRoughnessMap ? texture(RoughnessMap, uv).r : 0.35;
    roughness = clamp(roughness, 0.04, 1.0);
    float metallic = EnableMetallicMap ? texture(MetallicMap, uv).r : 0.0;
    float ao = EnableAOMap ? texture(AOMap, uv).r : 1.0;

    float rim = pow(1.0 - clamp(abs(dot(n, viewDir)), 0.0, 1.0), max(IridescencePower, 0.05));
    float center = clamp(IridescenceCenter, 0.05, 0.95);
    vec3 lowTint = mix(IridescenceColor1.rgb, IridescenceColor2.rgb, smoothstep(0.0, center, rim));
    vec3 highTint = mix(IridescenceColor2.rgb, IridescenceColor3.rgb, smoothstep(center, 1.0, rim));
    vec3 iridescence = mix(lowTint, highTint, step(center, rim));
    float iridescenceAmount = clamp(IridescenceBlend, 0.0, 1.0) * (0.25 + 0.75 * rim);
    albedo = mix(albedo, albedo * (0.65 + iridescence), iridescenceAmount);

    vec3 diffuse = albedo * (1.0 - metallic);
    vec3 color = AmbientColor * albedo + LightColor * diffuseTerm * diffuse;
    float specPower = mix(16.0, 96.0, 1.0 - roughness);
    float spec = pow(max(dot(n, halfDir), 0.0), specPower) * (1.0 - roughness);
    vec3 specColor = mix(vec3(0.04), albedo, metallic);

    gAlbedo = color;
    gNormal = n * 0.5 + 0.5;
    gSpecular = spec * specColor * SpecularScale;
    gAO = vec3(ao);
}
