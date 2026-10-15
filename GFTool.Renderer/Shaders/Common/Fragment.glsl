in vec3 FragPos, Normal, Tangent, Bitangent, Binormal;
in vec2 TexCoord;
in vec4 Color;
uniform vec3 CameraPos, LightDirection, LightColor, AmbientColor;
uniform bool ForwardPass, HasTangents, HasBinormals, FlipNormalY, ReconstructNormalZ, LegacyMode;
uniform bool VertexColorAvailable;
uniform float LightWrap, SpecularScale;
layout(location=0) out vec4 gAlbedo;
layout(location=1) out vec3 gNormal;
layout(location=2) out vec3 gSpecular;
layout(location=3) out vec3 gAO;
vec3 safe_normalize(vec3 v, vec3 fallback) { return dot(v,v) > 0.000001 ? normalize(v) : fallback; }
void main() {
    UV=TexCoord; UV2=TexCoord; COLOR=VertexColorAvailable?Color:vec4(1.0);
    VERTEX=(view*vec4(FragPos,1)).xyz;
    NORMAL=safe_normalize(Normal,vec3(0,1,0));
    TANGENT=safe_normalize(Tangent,vec3(1,0,0));
    BINORMAL=HasBinormals?safe_normalize(Binormal,Bitangent):safe_normalize(Bitangent,vec3(0,0,1));
    VIEW=safe_normalize(CameraPos-FragPos,vec3(0,0,1));
    fragment();
    if (ALPHA < ALPHA_SCISSOR_THRESHOLD || (ForwardPass && ALPHA < 0.001)) discard;
    vec3 n=NORMAL;
    if (HasTangents) {
        vec2 xy=(NORMAL_MAP.rg*2.0-1.0)*NORMAL_MAP_DEPTH;
        if (FlipNormalY) xy.y=-xy.y;
        vec3 tn=vec3(xy,sqrt(max(1.0-dot(xy,xy),0.00001)));
        n=safe_normalize(mat3(TANGENT,BINORMAL,NORMAL)*tn,NORMAL);
    }
    NORMAL=n;
    LIGHT=safe_normalize(-LightDirection,vec3(0,1,0)); LIGHT_COLOR=LightColor*PI;
    vec3 diffuse, specular=vec3(0);
#ifdef CUSTOM_LIGHT
    light();
    diffuse=ALBEDO*(AmbientColor+DIFFUSE_LIGHT);
    specular=SPECULAR_LIGHT;
#elif defined(UNSHADED)
    diffuse=ALBEDO;
#else
    float ndl=clamp((dot(n,LIGHT)+LightWrap)/(1.0+LightWrap),0.0,1.0);
    diffuse=ALBEDO*(AmbientColor+LightColor*ndl*(1.0-clamp(METALLIC,0.0,1.0)));
    vec3 halfway=safe_normalize(LIGHT+VIEW,LIGHT);
    float rough=clamp(ROUGHNESS,0.04,1.0);
    float highlight=pow(max(dot(n,halfway),0.0),mix(96.0,8.0,rough))*(1.0-rough)*ndl;
    specular=mix(vec3(0.08*SPECULAR),ALBEDO,clamp(METALLIC,0.0,1.0))*highlight*SpecularScale;
#endif
    if (LegacyMode) { diffuse=ALBEDO; specular=vec3(0); }
    gAlbedo=vec4(diffuse+EMISSION,clamp(ALPHA,0.0,1.0));
    gNormal=n*0.5+0.5; gSpecular=specular; gAO=vec3(clamp(AO,0.0,1.0));
    if (ForwardPass) gAlbedo.rgb=(diffuse+specular)*mix(1.0,AO,0.65)+EMISSION;
}
