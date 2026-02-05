using GFTool.Core.Utils;
using GFTool.Renderer.Core;
using GFTool.Renderer.Core.Graphics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Globalization;
using System.Drawing;
using System.Linq;
using System.Runtime.Versioning;
using Trinity.Core.Flatbuffers.TR.Model;
using Trinity.Core.Flatbuffers.Utils;
using Trinity.Core.Assets;

namespace GFTool.Renderer.Scene.GraphicsObjects
{
    public partial class Material : IDisposable
    {
        private readonly object colorTableUniformOverrideLock = new object();
        private Dictionary<string, Vector4>? colorTableUniformOverrides;
        private string? colorTableUniformTextureKeyCached;
        private int colorTableUniformDivideCached;
        private int colorTableUniformIndex1Cached;
        private int colorTableUniformIndex2Cached;
        private int colorTableUniformIndex3Cached;
        private int colorTableUniformIndex4Cached;

        private string? colorTableCacheTextureKeyCached;

        private void TryApplyColorTableOverrides()
        {
            if (!string.Equals(shaderKey, "IkCharacter", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            // ColorTableMap is used by some character materials (notably faces) to populate BaseColorLayer/ShadowingColorLayer
            // based on indexed palette entries.
            static bool IsTruthy(string? value)
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    return false;
                }

                var trimmed = value.Trim();
                return string.Equals(trimmed, "true", StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(trimmed, "1", StringComparison.OrdinalIgnoreCase);
            }

            static bool TryGetBoolishOverride(object? value, out bool enabled)
            {
                switch (value)
                {
                    case null:
                        enabled = false;
                        return false;
                    case bool b:
                        enabled = b;
                        return true;
                    case int n:
                        enabled = n != 0;
                        return true;
                    case float f:
                        enabled = MathF.Abs(f) > 0.0001f;
                        return true;
                    case string s:
                        enabled = IsTruthy(s);
                        // Treat explicit false tokens as valid too.
                        if (!enabled)
                        {
                            var trimmed = s.Trim();
                            if (string.Equals(trimmed, "false", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(trimmed, "0", StringComparison.OrdinalIgnoreCase))
                            {
                                return true;
                            }
                            return false;
                        }
                        return true;
                }

                enabled = false;
                return false;
            }

            bool enableColorTableMap;
            if (TryGetUniformOverride("EnableColorTableMap", out var overrideValue) &&
                TryGetBoolishOverride(overrideValue, out var overrideEnabled))
            {
                enableColorTableMap = overrideEnabled;
            }
            else
            {
                enableColorTableMap =
                    ShaderParams.Any(p => string.Equals(p.Name, "EnableColorTableMap", StringComparison.OrdinalIgnoreCase) && IsTruthy(p.Value)) ||
                    floatParams.Any(p => string.Equals(p.Name, "EnableColorTableMap", StringComparison.OrdinalIgnoreCase) && MathF.Abs(p.Value) > 0.0001f);
            }

            if (!enableColorTableMap)
            {
                lock (colorTableUniformOverrideLock)
                {
                    colorTableUniformOverrides = null;
                    colorTableUniformTextureKeyCached = null;
                    colorTableUniformDivideCached = 0;
                    colorTableUniformIndex1Cached = 0;
                    colorTableUniformIndex2Cached = 0;
                    colorTableUniformIndex3Cached = 0;
                    colorTableUniformIndex4Cached = 0;
                }
                return;
            }

            // Bake ColorTableMap palette entries into BaseColorLayer*/ShadowingColorLayer* (reference import behavior).
            if (!TryGetShaderParamIntWithOverrides("ColorTableDivideNumber", out int divide) || divide <= 0)
            {
                lock (colorTableUniformOverrideLock)
                {
                    colorTableUniformOverrides = null;
                    colorTableUniformTextureKeyCached = null;
                }
                return;
            }

            if (!TryGetShaderParamIntWithOverrides("BaseColorIndex1", out int index1)) index1 = 0;
            if (!TryGetShaderParamIntWithOverrides("BaseColorIndex2", out int index2)) index2 = 0;
            if (!TryGetShaderParamIntWithOverrides("BaseColorIndex3", out int index3)) index3 = 0;
            if (!TryGetShaderParamIntWithOverrides("BaseColorIndex4", out int index4)) index4 = 0;

            if (!EnsureColorTableCache(divide))
            {
                lock (colorTableUniformOverrideLock)
                {
                    colorTableUniformOverrides = null;
                    colorTableUniformTextureKeyCached = null;
                }
                return;
            }

            var tableTex = textures.FirstOrDefault(t => string.Equals(t.Name, "ColorTableMap", StringComparison.OrdinalIgnoreCase));
            var textureKey = tableTex?.CacheKey ?? string.Empty;

            lock (colorTableUniformOverrideLock)
            {
                if (colorTableUniformOverrides != null &&
                    string.Equals(colorTableUniformTextureKeyCached, textureKey, StringComparison.Ordinal) &&
                    colorTableUniformDivideCached == divide &&
                    colorTableUniformIndex1Cached == index1 &&
                    colorTableUniformIndex2Cached == index2 &&
                    colorTableUniformIndex3Cached == index3 &&
                    colorTableUniformIndex4Cached == index4)
                {
                    return;
                }
            }

            var overrides = new Dictionary<string, Vector4>(StringComparer.OrdinalIgnoreCase);
            ApplyTableIndex(overrides, "BaseColorLayer1", colorTableBaseColorsCached!, index1);
            ApplyTableIndex(overrides, "BaseColorLayer2", colorTableBaseColorsCached!, index2);
            ApplyTableIndex(overrides, "BaseColorLayer3", colorTableBaseColorsCached!, index3);
            ApplyTableIndex(overrides, "BaseColorLayer4", colorTableBaseColorsCached!, index4);
            ApplyTableIndex(overrides, "ShadowingColorLayer1", colorTableShadowColorsCached!, index1);
            ApplyTableIndex(overrides, "ShadowingColorLayer2", colorTableShadowColorsCached!, index2);
            ApplyTableIndex(overrides, "ShadowingColorLayer3", colorTableShadowColorsCached!, index3);
            ApplyTableIndex(overrides, "ShadowingColorLayer4", colorTableShadowColorsCached!, index4);

            if (overrides.Count == 0)
            {
                lock (colorTableUniformOverrideLock)
                {
                    colorTableUniformOverrides = null;
                    colorTableUniformTextureKeyCached = null;
                }
                return;
            }
            lock (colorTableUniformOverrideLock)
            {
                colorTableUniformOverrides = overrides;
                colorTableUniformTextureKeyCached = textureKey;
                colorTableUniformDivideCached = divide;
                colorTableUniformIndex1Cached = index1;
                colorTableUniformIndex2Cached = index2;
                colorTableUniformIndex3Cached = index3;
                colorTableUniformIndex4Cached = index4;
            }
        }

        public void RefreshColorTableOverridesFromUniformOverrides()
        {
            TryApplyColorTableOverrides();
        }

        private bool EnsureColorTableCache(int divide)
        {
            if (!OperatingSystem.IsWindows())
            {
                return false;
            }

            return EnsureColorTableCacheWindows(divide);
        }

        [SupportedOSPlatform("windows")]
        private bool EnsureColorTableCacheWindows(int divide)
        {
            var tableTex = textures.FirstOrDefault(t => string.Equals(t.Name, "ColorTableMap", StringComparison.OrdinalIgnoreCase));
            if (tableTex == null)
            {
                return false;
            }
            var textureKey = tableTex.CacheKey;

            if (colorTableCacheReady && colorTableDivideCached == divide &&
                string.Equals(colorTableCacheTextureKeyCached, textureKey, StringComparison.Ordinal) &&
                colorTableBaseColorsCached != null && colorTableShadowColorsCached != null)
            {
                return true;
            }

            using Bitmap? bitmap = tableTex.LoadPreviewBitmap();
            if (bitmap == null)
            {
                return false;
            }

            int cols = Math.Min(bitmap.Width / 2, divide);
            int rows = bitmap.Height / 2;
            if (cols <= 0 || rows < 2)
            {
                return false;
            }

            var baseColors = new Vector3[cols];
            var shadowColors = new Vector3[cols];
            for (int col = 0; col < cols; col++)
            {
                baseColors[col] = SampleColorTableBlockLinearWindows(bitmap, col, row: 0);
                shadowColors[col] = SampleColorTableBlockLinearWindows(bitmap, col, row: 1);
            }

            colorTableBaseColorsCached = baseColors;
            colorTableShadowColorsCached = shadowColors;
            colorTableDivideCached = divide;
            colorTableCacheTextureKeyCached = textureKey;
            colorTableCacheReady = true;
            return true;
        }

        private bool TryGetShaderParamIntWithOverrides(string name, out int value)
        {
            if (TryGetUniformOverride(name, out var overrideValue))
            {
                switch (overrideValue)
                {
                    case int n:
                        value = n;
                        return true;
                    case float f:
                        value = (int)f;
                        return true;
                    case string s:
                        {
                            var trimmed = s.Trim();
                            if (int.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out var si))
                            {
                                value = si;
                                return true;
                            }

                            if (float.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out var sf))
                            {
                                value = (int)sf;
                                return true;
                            }

                            break;
                        }
                }
            }

            return TryGetShaderParamInt(name, out value);
        }

        private static void ApplyTableIndex(Dictionary<string, Vector4> overrides, string paramName, Vector3[] colors, int index1Based)
        {
            int idx = index1Based - 1;
            if (idx < 0 || idx >= colors.Length)
            {
                return;
            }

            var c = colors[idx];
            overrides[paramName] = new Vector4(c.X, c.Y, c.Z, 1.0f);
        }

        private void ApplyColorTableUniformOverrides(Shader activeShader)
        {
            Dictionary<string, Vector4>? overrides;
            lock (colorTableUniformOverrideLock)
            {
                overrides = colorTableUniformOverrides;
            }

            if (overrides == null || overrides.Count == 0)
            {
                return;
            }

            foreach (var kvp in overrides)
            {
                activeShader.SetVector4IfExists(kvp.Key, kvp.Value);
            }
        }

        [SupportedOSPlatform("windows")]
        private static Vector3 SampleColorTableBlockLinearWindows(Bitmap bitmap, int col, int row)
        {
            int startX = col * 2;
            int startY = row * 2;

            var c00 = bitmap.GetPixel(startX, startY);
            var c10 = bitmap.GetPixel(startX + 1, startY);
            var c01 = bitmap.GetPixel(startX, startY + 1);
            var c11 = bitmap.GetPixel(startX + 1, startY + 1);

            float sr = (c00.R + c10.R + c01.R + c11.R) / (4.0f * 255.0f);
            float sg = (c00.G + c10.G + c01.G + c11.G) / (4.0f * 255.0f);
            float sb = (c00.B + c10.B + c01.B + c11.B) / (4.0f * 255.0f);

            return new Vector3(SrgbToLinear(sr), SrgbToLinear(sg), SrgbToLinear(sb));
        }

        private static float SrgbToLinear(float c)
        {
            if (c <= 0.04045f)
            {
                return c / 12.92f;
            }

            return MathF.Pow((c + 0.055f) / 1.055f, 2.4f);
        }

    }
}
