const float PI = 3.14159265358979323846;
uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;
uniform float TIME;
vec2 UV, UV2;
vec4 COLOR;
vec3 VERTEX, NORMAL, TANGENT, BINORMAL, VIEW;
#define MODEL_MATRIX model
#ifdef FRAGMENT_STAGE
#define FRAGCOORD gl_FragCoord
#else
#define FRAGCOORD vec4(0.0)
#endif
vec3 ALBEDO = vec3(1.0), EMISSION = vec3(0.0), NORMAL_MAP = vec3(0.5, 0.5, 1.0);
float ALPHA = 1.0, ALPHA_SCISSOR_THRESHOLD = 0.0, AO = 1.0;
float METALLIC = 0.0, ROUGHNESS = 0.5, SPECULAR = 0.5, NORMAL_MAP_DEPTH = 1.0;
vec3 DIFFUSE_LIGHT = vec3(0.0), SPECULAR_LIGHT = vec3(0.0);
vec3 LIGHT, LIGHT_COLOR;
bool LIGHT_IS_DIRECTIONAL = true;
float ATTENUATION = 1.0;
#include "Sampling.glsl"
