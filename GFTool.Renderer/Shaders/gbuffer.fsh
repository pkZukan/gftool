#version 420 core

layout (location = 0) in vec2 inTexCoord;
layout (location = 0) out vec3 outColor;

// GBuffer layout (viewer, to style deferred encoding):
// albedoTexture: rgb = base/prelit color, a = roughness (or a repurposed scalar for special shaders)
// normalTexture: rgb = world normal encoded [0,1], a = reflectance (or repurposed scalar)
// specularTexture: r = AO, g = metallic (or repurposed scalar), b/a unused
// aoTexture: rgb = emission, a = shading model id (0=PBR, 1=PreLit)
uniform sampler2D albedoTexture;
uniform sampler2D normalTexture;
uniform sampler2D specularTexture;
uniform sampler2D aoTexture;
uniform sampler2D ssaoTexture;
uniform sampler2D depthTexture;

uniform vec3 LightDirection;
uniform vec3 LightColor;
uniform vec3 AmbientColor;
uniform float LightWrap;
uniform vec3 CameraPos;
uniform mat4 InvView;
uniform mat4 InvProjection;

uniform float CameraNear;
uniform float CameraFar;

uniform bool useAlbedo;
uniform bool useNormal;
uniform bool useSpecular;
uniform bool useAO;
uniform bool useSSAO;
uniform bool useToon;
uniform bool useLegacy;
uniform bool useDepth;

vec3 GammaEncode(vec3 linearColor)
{
    // Our material shaders sample color textures as sRGB (decoded to linear), then write linear values into the gbuffer
    // Apply gamma for display on the final pass so the overall image brightness better
    const float gamma = 2.2;
    return pow(max(linearColor, vec3(0.0)), vec3(1.0 / gamma));
}

float LinearizeDepth(float depth)
{
    float z = depth * 2.0 - 1.0;
    float nearPlane = max(CameraNear, 0.0001);
    float farPlane = max(CameraFar, nearPlane + 0.001);
    return (2.0 * nearPlane * farPlane) / (farPlane + nearPlane - z * (farPlane - nearPlane));
}

vec3 ReconstructWorldPos(vec2 uv, float depth)
{
    // Reconstruct view space position from depth, then transform to world
    vec4 ndc = vec4(uv * 2.0 - 1.0, depth * 2.0 - 1.0, 1.0);
    vec4 viewPos = InvProjection * ndc;
    viewPos.xyz /= max(viewPos.w, 0.00001);
    vec4 worldPos = InvView * vec4(viewPos.xyz, 1.0);
    return worldPos.xyz;
}

float WrapNdotL(float nDotL, float wrap)
{
    float w = max(wrap, 0.0);
    return clamp((nDotL + w) / (1.0 + w), 0.0, 1.0);
}

float D_GGX(float a2, float NdotH)
{
    float denom = (NdotH * NdotH) * (a2 - 1.0) + 1.0;
    return a2 / (3.14159265 * denom * denom);
}

float G_SchlickGGX(float k, float NdotV)
{
    return NdotV / (NdotV * (1.0 - k) + k);
}

float G_Smith(float k, float NdotV, float NdotL)
{
    return G_SchlickGGX(k, NdotV) * G_SchlickGGX(k, NdotL);
}

vec3 F_Schlick(vec3 F0, float VdotH)
{
    return F0 + (1.0 - F0) * pow(clamp(1.0 - VdotH, 0.0, 1.0), 5.0);
}

void main()
{
    vec4 g0 = texture(albedoTexture, inTexCoord);
    vec4 g1 = texture(normalTexture, inTexCoord);
    vec4 g2 = texture(specularTexture, inTexCoord);
    vec4 g3 = texture(aoTexture, inTexCoord);

    vec3 baseColor = g0.rgb;
    float roughness = g0.a;
    vec3 normal = normalize(g1.rgb * 2.0 - 1.0);
    float reflectance = g1.a;
    float ao = g2.r;
    float metallic = g2.g;
    vec3 emission = g3.rgb;
    float shadingModel = g3.a;

    float ssao = useSSAO ? texture(ssaoTexture, inTexCoord).r : 1.0;
    float depth = texture(depthTexture, inTexCoord).r;

    bool useAll = useAlbedo && useNormal && useSpecular && useAO;

    if (useDepth)
    {
        // Depth texture is nonlinear; linearize to view space depth then normalize for visualization
        float z = depth * 2.0 - 1.0;
        float nearPlane = max(CameraNear, 0.0001);
        float farPlane = max(CameraFar, nearPlane + 0.001);
        float linear = (2.0 * nearPlane * farPlane) / (farPlane + nearPlane - z * (farPlane - nearPlane));
        float normalized = clamp((linear - nearPlane) / (farPlane - nearPlane), 0.0, 1.0);
        // Near=white, far=black with a gentle curve
        outColor = vec3(pow(1.0 - normalized, 2.2));
        return;
    }

    if(useLegacy)
    {
        float aoSoft = mix(1.0, ao, 0.7);
        float ssaoSoft = mix(1.0, ssao, 0.7);

        // Legacy display still runs the same PreLit handling so IkCharacter doesn't look flat when
        // the user selects legacy shading
        float aoApply = (shadingModel >= 1.5) ? 1.0 : aoSoft;
        vec3 linear = baseColor * aoApply * ssaoSoft + emission;

        if (shadingModel >= 1.5 && depth < 1.0)
        {
            vec3 worldPos = ReconstructWorldPos(inTexCoord, depth);
            vec3 viewDir = normalize(CameraPos - worldPos);
            vec3 lightDir = normalize(-LightDirection);
            vec3 halfDir = normalize(viewDir + lightDir);

            float giGain = clamp(roughness, 0.0, 2.0);
            float shadowStrength = clamp(reflectance, 0.0, 2.0);
            float rimMask = clamp(metallic, 0.0, 1.0);

            float nDotL = max(dot(normal, lightDir), 0.0);
            float wrappedNdotL = WrapNdotL(nDotL, LightWrap);
            float nv = max(dot(normal, viewDir), 0.0);

            float fresnel = pow(1.0 - nv, 2.0);
            vec3 rim = (rimMask * fresnel) * LightColor * 0.35;

            float sparkle = pow(max(dot(normal, halfDir), 0.0), 96.0) * wrappedNdotL;
            vec3 sparkleTerm = sparkle * LightColor * (0.15 + 0.35 * shadowStrength);

            float giFactor = (1.0 - wrappedNdotL) * (0.15 + 0.35 * giGain);
            vec3 giTerm = baseColor * AmbientColor * giFactor;

            float addedOcclusion = aoSoft * ssaoSoft;
            linear += (rim + sparkleTerm + giTerm) * addedOcclusion;
        }

        outColor = GammaEncode(linear);
    }
    else if(useToon)
    {
        float nDotL = max(dot(normal, normalize(-LightDirection)), 0.0);
        float steps = 3.0;
        float toon = floor(nDotL * steps) / (steps - 1.0);
        vec3 lit = AmbientColor + LightColor * toon;
        float aoSoft = mix(1.0, ao, 0.75);
        float ssaoSoft = mix(1.0, ssao, 0.8);
        vec3 linear = baseColor * lit * aoSoft * ssaoSoft + emission;
        outColor = GammaEncode(linear);
    }
    else if(useAll)
    {
        // Shading model:
        // 0: PBR attributes (deferred lighting computed here)
        // 1: Pre lit baseColor (shader already baked lighting)
        float aoSoft = mix(1.0, ao, 0.65);
        float ssaoSoft = mix(1.0, ssao, 0.7);

        vec3 linear = vec3(0.0);
        if (shadingModel >= 0.5)
        {
            // PreLit content:
            // shadingModel >= 1.5: color already includes AO, only apply SSAO (IkCharacter ZA uses AO during shading)
            // otherwise: apply both AO and SSAO (most legacy/prelit shaders)
            float aoApply = (shadingModel >= 1.5) ? 1.0 : aoSoft;
            linear = baseColor * aoApply * ssaoSoft + emission;

            // IkCharacter ZA: the material shader bakes most lighting into `final_shaded_color` but stores a few
            // scalars in the GBuffer for later passes ( uses them in follow up deferred steps)
            // We approximate those missing passes here to recover eye pop and the subtle \"glow\" the game has
            if (shadingModel >= 1.5)
            {
                if (depth < 1.0)
                {
                    vec3 worldPos = ReconstructWorldPos(inTexCoord, depth);
                    vec3 viewDir = normalize(CameraPos - worldPos);
                    vec3 lightDir = normalize(-LightDirection);
                    vec3 halfDir = normalize(viewDir + lightDir);

                    // Repurposed channels from IkCharacter's deferred encoding:
                    // g0.a: ShadowingGIGain (acts like a GI/ambient gain)
                    // g1.a: ShadowStrength (acts like an extra highlight/shadow control)
                    // g2.g: rim light mask
                    float giGain = clamp(roughness, 0.0, 2.0);
                    float shadowStrength = clamp(reflectance, 0.0, 2.0);
                    float rimMask = clamp(metallic, 0.0, 1.0);

                    float nDotL = max(dot(normal, lightDir), 0.0);
                    float wrappedNdotL = WrapNdotL(nDotL, LightWrap);
                    float nv = max(dot(normal, viewDir), 0.0);

                    float fresnel = pow(1.0 - nv, 2.0);
                    vec3 rim = (rimMask * fresnel) * LightColor * 0.35;

                    // Small view dependent sparkle term (helps eyes/eyebrows without requiring per material textures here)
                    float sparkle = pow(max(dot(normal, halfDir), 0.0), 96.0) * wrappedNdotL;
                    vec3 sparkleTerm = sparkle * LightColor * (0.15 + 0.35 * shadowStrength);

                    // GI gain: lightly boost the prelit term where direct light is weaker
                    float giFactor = (1.0 - wrappedNdotL) * (0.15 + 0.35 * giGain);
                    vec3 giTerm = baseColor * AmbientColor * giFactor;

                    // Use material AO for the added terms (baseColor already includes it)
                    float addedOcclusion = aoSoft * ssaoSoft;
                    linear += (rim + sparkleTerm + giTerm) * addedOcclusion;
                }
            }
        }
        else
        {
            if (depth >= 1.0)
            {
                outColor = GammaEncode(vec3(0.0));
                return;
            }

            vec3 worldPos = ReconstructWorldPos(inTexCoord, depth);
            vec3 viewDir = normalize(CameraPos - worldPos);
            vec3 lightDir = normalize(-LightDirection);
            vec3 halfDir = normalize(viewDir + lightDir);

            float NdotL = max(dot(normal, lightDir), 0.0);
            float NdotV = clamp(dot(normal, viewDir), 0.0001, 1.0);
            float NdotH = clamp(dot(normal, halfDir), 0.0, 1.0);
            float VdotH = clamp(dot(viewDir, halfDir), 0.0, 1.0);

            float wrappedNdotL = WrapNdotL(NdotL, LightWrap);

            float a = clamp(roughness, 0.04, 1.0);
            float a2 = a * a;
            float k = (a + 1.0);
            k = (k * k) / 8.0;

            float refl = max(reflectance, 0.0);
            vec3 F0 = mix(vec3(0.04 * refl), baseColor, metallic);
            vec3 F = F_Schlick(F0, VdotH);
            float D = D_GGX(a2, NdotH);
            float G = G_Smith(k, NdotV, NdotL);
            vec3 specularBRDF = (D * G) * F / max(4.0 * NdotV * NdotL, 0.0001);

            vec3 diffuse = baseColor * (1.0 - metallic);
            vec3 lit = AmbientColor + LightColor * wrappedNdotL;
            vec3 diffuseLit = diffuse * lit;
            vec3 specularLit = specularBRDF * LightColor * NdotL;

            linear = (diffuseLit + specularLit) * aoSoft * ssaoSoft + emission;
        }

        outColor = GammaEncode(linear);
    }
    else
    {
        if (useAlbedo)
        {
            outColor = GammaEncode(baseColor);
            return;
        }
        if (useNormal)
        {
            outColor = normal * 0.5 + 0.5;
            return;
        }
        if (useSpecular)
        {
            if (depth >= 1.0)
            {
                outColor = vec3(0.0);
                return;
            }

            // Specular debug:
            // PreLit: show the pre packed scalar in `g2.g` (metallic slot). Our legacy IkCharacter highlight shader
            // uses this to store a spec intensity visualization
            // PBR: compute and display the specular lighting term
            if (shadingModel >= 0.5)
            {
                outColor = GammaEncode(vec3(clamp(metallic, 0.0, 1.0)));
                return;
            }

            vec3 worldPos = ReconstructWorldPos(inTexCoord, depth);
            vec3 viewDir = normalize(CameraPos - worldPos);
            vec3 lightDir = normalize(-LightDirection);
            vec3 halfDir = normalize(viewDir + lightDir);

            float NdotL = max(dot(normal, lightDir), 0.0);
            float NdotV = clamp(dot(normal, viewDir), 0.0001, 1.0);
            float NdotH = clamp(dot(normal, halfDir), 0.0, 1.0);
            float VdotH = clamp(dot(viewDir, halfDir), 0.0, 1.0);

            float a = clamp(roughness, 0.04, 1.0);
            float a2 = a * a;
            float k = (a + 1.0);
            k = (k * k) / 8.0;

            float refl = max(reflectance, 0.0);
            vec3 F0 = mix(vec3(0.04 * refl), baseColor, metallic);
            vec3 F = F_Schlick(F0, VdotH);
            float D = D_GGX(a2, NdotH);
            float G = G_Smith(k, NdotV, NdotL);
            vec3 specularBRDF = (D * G) * F / max(4.0 * NdotV * NdotL, 0.0001);

            float aoSoft = mix(1.0, ao, 0.65);
            float ssaoSoft = mix(1.0, ssao, 0.7);
            vec3 specularLit = specularBRDF * LightColor * NdotL;
            outColor = GammaEncode(specularLit * aoSoft * ssaoSoft);
            return;
        }
        if (useAO)
        {
            outColor = vec3(ao);
            return;
        }

        outColor = vec3(0.0);
    }
}
