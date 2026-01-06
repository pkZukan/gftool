#version 420 core

layout (location = 0) out vec4 outColor;

uniform vec4 color;

void main()
{
    outColor = color;
}
