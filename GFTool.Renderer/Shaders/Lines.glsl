#version 420 core

#ifdef VERTEX_STAGE
layout (location = 0) in vec3 aPos;

uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;

void main()
{
    gl_Position = projection * view * model * vec4(aPos, 1.0);
}
#endif

#ifdef FRAGMENT_STAGE
layout (location = 0) out vec4 outColor;

uniform vec4 color;

void main()
{
    outColor = color;
}
#endif
