uniform int sampler_repeat_u[32];
uniform int sampler_repeat_v[32];
uniform vec4 sampler_border_color[32];
uniform bool sampler_present[32];
uniform vec4 sampler_default[32];

vec2 address_axis(float uv, int mode, float half_texel) {
    if (mode == 1) return vec2(fract(uv), 1.0);
    if (mode == 6) uv = 1.0 - abs(mod(uv, 2.0) - 1.0);
    if (mode == 2 || mode == 3 || mode == 4) uv = abs(uv);
    if (mode == 0 || mode == 2) uv = clamp(uv, 0.0, 1.0);
    float coverage = 1.0;
    if (mode == 0 || mode == 2 || mode == 4 || mode == 5)
        coverage = clamp(min(uv + half_texel, 1.0 + half_texel - uv) / (2.0 * half_texel), 0.0, 1.0);
    return vec2(clamp(uv, half_texel, 1.0 - half_texel), coverage);
}

vec4 sample_trinity(sampler2D tex, vec2 uv, int slot) {
    if (!sampler_present[slot]) return sampler_default[slot];
    vec2 half_texel = 0.5 / vec2(max(textureSize(tex, 0), ivec2(1)));
    vec2 u = address_axis(uv.x, sampler_repeat_u[slot], half_texel.x);
    vec2 v = address_axis(uv.y, sampler_repeat_v[slot], half_texel.y);
    return mix(sampler_border_color[slot], textureLod(tex, vec2(u.x, v.x), 0.0), u.y * v.y);
}
