namespace GFTool.Renderer.Core.Graphics;

public static class MaterialShaderCatalog
{
    public static string Resolve(string? name) => string.IsNullOrEmpty(name) || name.Equals("Opaque", StringComparison.OrdinalIgnoreCase) ? "Standard" : name;

    public static bool IsTransparent(string shader, string? alphaType, string? materialName)
    {
        // Material render state takes precedence. Name fallbacks support materials with missing alpha state.
        if (!string.IsNullOrWhiteSpace(alphaType))
            return alphaType.Equals("Transparent", StringComparison.OrdinalIgnoreCase)
                || alphaType.Equals("Blend", StringComparison.OrdinalIgnoreCase);
        return shader.Contains("Transparent", StringComparison.OrdinalIgnoreCase)
            || shader.Contains("Fresnel", StringComparison.OrdinalIgnoreCase)
            || shader.Contains("Effect", StringComparison.OrdinalIgnoreCase)
            || shader.Contains("Jewel", StringComparison.OrdinalIgnoreCase)
            || shader is "Water" or "Decal" or "Shadow" or "EyeClearCoat"
            || materialName?.Contains("eye_lens", StringComparison.OrdinalIgnoreCase) == true;
    }

    public static int? OptionValue(string name, string value)
    {
        if (bool.TryParse(value, out var boolean)) return boolean ? 1 : 0;
        if (int.TryParse(value, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var number)) return number;
        return (name, value) switch
        {
            ("LayerMaskSource", "Texture") => 0, ("LayerMaskSource", "VertexColor") => 1,
            ("LayerMaskSwizzle", "RGBA") => 0, ("LayerMaskSwizzle", "RRRR") => 1,
            ("LayerMaskSwizzle", "GGGG") => 2, ("LayerMaskSwizzle", "BBBB") => 3, ("LayerMaskSwizzle", "AAAA") => 4,
            ("LayerBaseMaskSource", "One") => 0, ("LayerBaseMaskSource", "BaseColorAlpha") => 1,
            ("LayerBlendType", "Mix") => 0, ("LayerBlendType", "Add") => 1, ("LayerBlendType", "Multiply") => 2,
            ("WindReceiverType", "None") => 0, ("WindReceiverType", "SimpleLeaf") => 1,
            ("EyelidType", "None") => 0, ("EnableIrisRefraction", "None") => 0,
            ("EnableIrisRefraction", "Ng") => 1, ("EnableIrisRefraction", "Ncc") => 2,
            _ => null
        };
    }
}
