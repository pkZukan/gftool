#version 420 core

layout (location = 0) in vec2 inTexCoord;
layout (location = 0) out float outAO;

uniform sampler2D normalTexture;
uniform sampler2D depthTexture;

uniform vec2 texelSize;
uniform float radius;
uniform float bias;
uniform float nearPlane;
uniform float farPlane;

float LinearizeDepth(float depth)
{
    float z = depth * 2.0 - 1.0;
    return (2.0 * nearPlane * farPlane) / (farPlane + nearPlane - z * (farPlane - nearPlane));
}

void main()
{
    float depth = texture(depthTexture, inTexCoord).r;
    if (depth >= 1.0)
    {
        outAO = 1.0;
        return;
    }

    float centerDepth = LinearizeDepth(depth);
    vec3 n = texture(normalTexture, inTexCoord).rgb * 2.0 - 1.0;

    vec2 offsets[8] = vec2[](
        vec2(1.0, 0.0),
        vec2(-1.0, 0.0),
        vec2(0.0, 1.0),
        vec2(0.0, -1.0),
        vec2(1.0, 1.0),
        vec2(-1.0, 1.0),
        vec2(1.0, -1.0),
        vec2(-1.0, -1.0)
    );

    float occlusion = 0.0;
    for (int i = 0; i < 8; i++)
    {
        vec2 sampleUV = inTexCoord + offsets[i] * texelSize * radius;
        float sampleDepth = texture(depthTexture, sampleUV).r;
        if (sampleDepth >= 1.0)
            continue;

        float sampleLinear = LinearizeDepth(sampleDepth);
        float depthDelta = centerDepth - sampleLinear;
        float rangeWeight = smoothstep(0.0, radius * 0.5, depthDelta);
        occlusion += step(bias, depthDelta) * rangeWeight;
    }

    float ao = 1.0 - (occlusion / 8.0);
    outAO = clamp(ao, 0.0, 1.0);
}
