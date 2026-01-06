namespace GFTool.Renderer.Core
{
    public static class RenderOptions
    {
        public static bool EnableNormalMaps { get; set; } = true;
        public static bool EnableAO { get; set; } = true;
        public static bool EnableVertexColors { get; set; } = true;
        public static bool FlipNormalY { get; set; } = true;
        public static bool ReconstructNormalZ { get; set; } = true;
        public static bool LegacyMode { get; set; } = false;
        public static OpenTK.Mathematics.Vector3 WorldLightDirection { get; set; } = new OpenTK.Mathematics.Vector3(-0.681f, -0.096f, -3.139f).Normalized();
        public static float LightWrap { get; set; } = 0.5f;
        public static float SpecularScale { get; set; } = 0.45f;
        public static float LensOpacity { get; set; } = 0.35f;
        public static bool TransparentPass { get; set; } = false;
        public static OpenTK.Mathematics.Vector3 OutlineColor { get; set; } = new OpenTK.Mathematics.Vector3(0.65f, 0.65f, 0.65f);
        public static float OutlineAlpha { get; set; } = 1.0f;
        public static bool OutlinePass { get; set; } = false;
        public static bool ShowSkeleton { get; set; } = false;
        public static bool UseTrsklInverseBind { get; set; } = true;
        public static bool SwapBlendOrder { get; set; } = false;
        public static bool AutoMapBlendIndices { get; set; } = true;
        public static bool MapBlendIndicesViaBoneMeta { get; set; } = false;
        public static bool TransposeSkinMatrices { get; set; } = false;
        public static bool MapBlendIndicesViaSkinningPalette { get; set; } = false;
        public static bool UseSkinningPaletteMatrices { get; set; } = false;
        public static bool MapBlendIndicesViaJointInfo { get; set; } = false;
        public static bool UseJointInfoMatrices { get; set; } = false;
    }
}
