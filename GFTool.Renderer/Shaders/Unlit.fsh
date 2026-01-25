#version 420 core

uniform sampler2D BaseColorMap;
uniform sampler2D LayerMaskMap;
uniform sampler2D NormalMap;
uniform sampler2D RoughnessMap;
uniform sampler2D AOMap;
uniform sampler2D SSSMaskMap;

uniform vec4 UVScaleOffset;
uniform vec4 BaseColor;
uniform vec4 BaseColorLayer1;
uniform vec4 BaseColorLayer2;
uniform vec4 BaseColorLayer3;
uniform vec4 BaseColorLayer4;

uniform float LayerMaskScale1;
uniform float LayerMaskScale2;
uniform float LayerMaskScale3;
uniform float LayerMaskScale4;

uniform bool EnableBaseColorMap;
uniform bool EnableLayerMaskMap;
uniform bool EnableNormalMap;
uniform bool EnableRoughnessMap;
uniform bool EnableAOMap;
uniform int NumMaterialLayer;
uniform bool EnableSSSMaskMap;
uniform bool EnableVertexColor;
uniform bool BaseColorMultiply;
uniform int UVIndexLayerMask;

layout (location = 0) out vec4 gAlbedo;
layout (location = 1) out vec4 gNormal;
layout (location = 2) out vec4 gSpecular;
layout (location = 3) out vec4 gAO;

in vec3 FragPos;
in vec3 Normal;
in vec2 TexCoord;
in vec4 Color;

vec2 SelectUv(int index, vec2 uv0)
{
    // Unlit.vsh only provides UV0 today; keep the selector for consistency with IkCharacter
    return uv0;
}

void main()
{
    vec2 uv = vec2(TexCoord.x, 1.0 - TexCoord.y);
    uv = uv * UVScaleOffset.xy + UVScaleOffset.zw;

    bool useLayerMask = EnableLayerMaskMap && (NumMaterialLayer > 0);
    vec4 layerMask = vec4(0.0);
    if (useLayerMask)
    {
        layerMask = texture(LayerMaskMap, SelectUv(UVIndexLayerMask, uv));
        layerMask *= vec4(LayerMaskScale1, LayerMaskScale2, LayerMaskScale3, LayerMaskScale4);
        if (dot(BaseColorLayer2.rgb, BaseColorLayer2.rgb) < 0.000001) layerMask.g = 0.0;
        if (dot(BaseColorLayer3.rgb, BaseColorLayer3.rgb) < 0.000001) layerMask.b = 0.0;
        if (dot(BaseColorLayer4.rgb, BaseColorLayer4.rgb) < 0.000001) layerMask.a = 0.0;
    }

    float baseLayerWeight = 1.0;
    if (useLayerMask)
    {
        float layerSum = clamp(dot(vec4(1.0), layerMask), 0.0, 1.0);
        baseLayerWeight = clamp(1.0 - layerSum, 0.0, 1.0);
    }

    vec3 texColor = EnableBaseColorMap ? texture(BaseColorMap, uv).rgb : vec3(1.0);
    vec3 baseTint = BaseColorMultiply ? BaseColor.rgb : vec3(1.0);
    vec3 baseLayerColor = texColor * baseTint;
    vec3 layerColor = baseLayerColor;
    if (useLayerMask)
    {
        vec3 layer1 = texColor * BaseColorLayer1.rgb;
        vec3 layer2 = texColor * BaseColorLayer2.rgb;
        vec3 layer3 = texColor * BaseColorLayer3.rgb;
        vec3 layer4 = texColor * BaseColorLayer4.rgb;
        layerColor = baseLayerColor * baseLayerWeight +
                     layer1 * layerMask.r +
                     layer2 * layerMask.g +
                     layer3 * layerMask.b +
                     layer4 * layerMask.a;
    }

    vec3 vertexColor = EnableVertexColor ? Color.rgb : vec3(1.0);
    vec3 albedo = layerColor * vertexColor;
    float ao = EnableAOMap ? texture(AOMap, uv).r : 1.0;

    gAlbedo = vec4(albedo, 1.0);
    gNormal = vec4(normalize(Normal) * 0.5 + 0.5, 1.0);
    gSpecular = vec4(ao, 0.0, 0.0, 0.0);
    gAO = vec4(0.0, 0.0, 0.0, 1.0); // emission=0, shadingModel=PreLit
}
