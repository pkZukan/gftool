#version 420 core

uniform sampler2D BaseColorMap;
uniform sampler2D LayerMaskMap;
uniform sampler2D NormalMap;
uniform sampler2D RoughnessMap;
uniform sampler2D AOMap;
uniform sampler2D SSSMaskMap;

uniform bool EnableBaseColorMap;
uniform bool EnableLayerMaskMap;
uniform bool EnableNormalMap;
uniform bool EnableRoughnessMap;
uniform bool EnableAOMap;
uniform bool NumMaterialLayer;
uniform bool EnableSSSMaskMap;
uniform bool EnableVertexColor;
uniform vec4 BaseColor;
uniform vec4 BaseColorLayer1;
uniform vec4 BaseColorLayer2;
uniform vec4 BaseColorLayer3;
uniform vec4 BaseColorLayer4;
uniform vec4 UVScaleOffset;

layout (location = 0) out vec3 gAlbedo;
layout (location = 1) out vec3 gNormal;
layout (location = 2) out vec3 gSpecular;
layout (location = 3) out vec3 gAO;

in vec3 FragPos;
in vec3 Normal;
in vec2 TexCoord;
in vec4 Color;

void main()
{
    vec2 uvScale = dot(abs(UVScaleOffset.xy), vec2(1.0)) < 0.0001
        ? vec2(1.0)
        : UVScaleOffset.xy;
    vec2 uv = vec2(TexCoord.x, 1.0f - TexCoord.y) * uvScale + UVScaleOffset.zw;

    bool useLayerMask = EnableLayerMaskMap && NumMaterialLayer;
    vec4 layerMask = vec4(0.0);
    if (useLayerMask)
    {
        layerMask = texture(LayerMaskMap, uv);
    }

    vec3 baseColor = (EnableBaseColorMap ? texture(BaseColorMap, uv).rgb : vec3(1.0)) * BaseColor.rgb;
    if (useLayerMask)
    {
        baseColor = mix(baseColor, BaseColorLayer1.rgb, layerMask.r);
        baseColor = mix(baseColor, BaseColorLayer2.rgb, layerMask.g);
        baseColor = mix(baseColor, BaseColorLayer3.rgb, layerMask.b);
        baseColor = mix(baseColor, BaseColorLayer4.rgb, layerMask.a);
    }
    vec3 vertexColor = EnableVertexColor ? Color.rgb : vec3(1.0);
    vec3 albedo = EnableVertexColor
        ? mix(vertexColor, baseColor, EnableBaseColorMap ? 0.5 : 0.0)
        : baseColor;
    float ao = EnableAOMap ? texture(AOMap, uv).r : 1.0;

    gAlbedo = albedo;
    gNormal = normalize(Normal) * 0.5 + 0.5;
    gSpecular = vec3(0.0);
    gAO = vec3(ao);
}
