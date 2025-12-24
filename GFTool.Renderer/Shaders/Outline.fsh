#version 420 core

uniform vec3 OutlineColor;
uniform float OutlineAlpha;

layout (location = 0) out vec4 outColor;

void main()
{
    outColor = vec4(OutlineColor, OutlineAlpha);
}
