#version 420 core

#ifdef VERTEX_STAGE
layout (location = 0) in vec3 inPosition;
layout (location = 1) in vec2 inTexCoord;

layout (location = 0) out vec2 outTexCoord;

void main()
{
    outTexCoord = inTexCoord;
    gl_Position = vec4(inPosition, 1.0);
}
#endif

#ifdef FRAGMENT_STAGE
layout (location = 0) in vec2 inTexCoord;
layout (location = 0) out float outAO;

layout(binding = 0) uniform sampler2D ssaoTexture;
uniform vec2 texelSize;

void main()
{
    float result = 0.0;
    float count = 0.0;

    for (int x = -2; x <= 2; x++)
    {
        for (int y = -2; y <= 2; y++)
        {
            vec2 offset = vec2(float(x), float(y)) * texelSize;
            result += texture(ssaoTexture, inTexCoord + offset).r;
            count += 1.0;
        }
    }

    outAO = result / count;
}
#endif
