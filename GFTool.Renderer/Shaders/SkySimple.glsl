#version 420 core
#include "Common/Surface.glsl"
#define UNSHADED



layout(binding = 0) uniform sampler2D NightStarsMap;
layout(binding = 1) uniform sampler2D NightStarsMaskMap;
layout(binding = 2) uniform sampler2D TopCloudsShapeMap;
layout(binding = 3) uniform sampler2D TopCloudsBreakupMap;
layout(binding = 4) uniform sampler2D MoonColorMap;
layout(binding = 5) uniform sampler2D MoonNormalMap ;

uniform vec4 ZenithColor = vec4(1.0f);
uniform vec4 HorizonColor = vec4(1.0f);
uniform vec4 CloudColor = vec4(1.0f);
uniform vec4 CloudColorRim = vec4(1.0f);
uniform vec4 SunTintColor = vec4(1.0f);
uniform vec4 FogColor = vec4(1.0f);

uniform float HorizonFalloff;
uniform float StarsIntensity;
uniform float StarsTiling;
uniform float StarsHorizonLift;
uniform float StarsMaskTiling;
uniform float StarsMaskSpeed;
uniform float CloudRim;
uniform float CloudsFadeOut;
uniform float TopCloudSpeed;
uniform float TopBreakupSpeed;
uniform float TopPlaneHeight;
uniform float TopPlaneHorizon;
uniform float TopPlaneTiling;
uniform float TopCloudCoverage;
uniform float SunRadius;
uniform float SunShine;
uniform float SunPitch;
uniform float SunYaw;
uniform float SunIntensity;
uniform float MoonScale;
uniform float MoonPhase;
uniform float MoonShadingRotation;
uniform float MoonPitch;
uniform float MoonYaw;
uniform float MoonIntensity;
uniform float FogIntensity;
uniform float FogOffset;
uniform float FogGradation;
vec4 sample_NightStarsMap(vec2 uv) { return sample_trinity(NightStarsMap,uv,0); }
vec4 sample_lod_NightStarsMap(vec2 uv,float lod) { return sample_trinity(NightStarsMap,uv,0); }
vec4 sample_NightStarsMaskMap(vec2 uv) { return sample_trinity(NightStarsMaskMap,uv,1); }
vec4 sample_lod_NightStarsMaskMap(vec2 uv,float lod) { return sample_trinity(NightStarsMaskMap,uv,1); }
vec4 sample_TopCloudsShapeMap(vec2 uv) { return sample_trinity(TopCloudsShapeMap,uv,2); }
vec4 sample_lod_TopCloudsShapeMap(vec2 uv,float lod) { return sample_trinity(TopCloudsShapeMap,uv,2); }
vec4 sample_TopCloudsBreakupMap(vec2 uv) { return sample_trinity(TopCloudsBreakupMap,uv,3); }
vec4 sample_lod_TopCloudsBreakupMap(vec2 uv,float lod) { return sample_trinity(TopCloudsBreakupMap,uv,3); }
vec4 sample_MoonColorMap(vec2 uv) { return sample_trinity(MoonColorMap,uv,4); }
vec4 sample_lod_MoonColorMap(vec2 uv,float lod) { return sample_trinity(MoonColorMap,uv,4); }
vec4 sample_MoonNormalMap(vec2 uv) { return sample_trinity(MoonNormalMap,uv,5); }
vec4 sample_lod_MoonNormalMap(vec2 uv,float lod) { return sample_trinity(MoonNormalMap,uv,5); }



#ifdef VERTEX_STAGE
void vertex() {

}
#endif


#ifdef FRAGMENT_STAGE
void fragment() {
	vec2 uvs = vec2(UV.x, 1.0 - UV.y); 

	float Moon_phase = MoonPhase * 2.0 * PI;

	vec3 moon_alb = sample_MoonColorMap( uvs).rgb;

	vec3 star_alb = sample_NightStarsMap( uvs).rgb;

	float horizon_falloff = smoothstep(0.0, 1.0, clamp(HorizonFalloff, 0.0, 1.0));
    vec4 sky_color = mix(HorizonColor, ZenithColor, horizon_falloff);

	ALBEDO = sky_color.rgb;
}
#endif
#ifdef VERTEX_STAGE
#include "Common/Vertex.glsl"
#endif
#ifdef FRAGMENT_STAGE
#include "Common/Fragment.glsl"
#endif
