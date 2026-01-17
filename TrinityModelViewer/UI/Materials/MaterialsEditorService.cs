using OpenTK.Mathematics;
using System;
using System.Globalization;
using System.Linq;

namespace TrinityModelViewer.UI.Materials
{
    internal sealed class MaterialsEditorService
    {
        public bool TryParseUniformValueForVariationsGrid(string type, string rawValue, out object? parsedValue, out string? errorText)
        {
            parsedValue = null;
            errorText = null;

            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return true;
            }

            bool TryParseFloat(string s, out float f) =>
                float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out f);

            bool TryParseInt(string s, out int n) =>
                int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out n);

            bool TryParseVector(string s, int count, out float[] values)
            {
                values = Array.Empty<float>();
                var parts = s.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != count)
                {
                    return false;
                }
                var parsed = new float[count];
                for (int i = 0; i < count; i++)
                {
                    if (!TryParseFloat(parts[i], out parsed[i]))
                    {
                        return false;
                    }
                }
                values = parsed;
                return true;
            }

            if (string.Equals(type, "Float", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryParseFloat(rawValue, out var f))
                {
                    errorText = "Invalid float value.";
                    return false;
                }
                parsedValue = f;
                return true;
            }

            if (string.Equals(type, "Int", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryParseInt(rawValue, out var n))
                {
                    errorText = "Invalid int value.";
                    return false;
                }
                parsedValue = n;
                return true;
            }

            if (string.Equals(type, "Vec3", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryParseVector(rawValue, 3, out var v))
                {
                    errorText = "Invalid Vec3. Use: x, y, z";
                    return false;
                }
                parsedValue = new Vector3(v[0], v[1], v[2]);
                return true;
            }

            if (string.Equals(type, "Vec4", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryParseVector(rawValue, 4, out var v))
                {
                    errorText = "Invalid Vec4. Use: x, y, z, w";
                    return false;
                }
                parsedValue = new Vector4(v[0], v[1], v[2], v[3]);
                return true;
            }

            if (TryParseInt(rawValue, out var intValue))
            {
                parsedValue = intValue;
                return true;
            }

            if (TryParseFloat(rawValue, out var floatValue))
            {
                parsedValue = floatValue;
                return true;
            }

            parsedValue = rawValue;
            return true;
        }

        public bool TryParseUniformValue(string type, string rawValue, out object? parsedValue, out string? errorText)
        {
            parsedValue = null;
            errorText = null;

            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return true;
            }

            bool TryParseFloat(string s, out float f) =>
                float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out f);

            bool TryParseInt(string s, out int n) =>
                int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out n);

            bool TryParseVector(string s, int count, out float[] values)
            {
                values = Array.Empty<float>();
                var parts = s.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != count)
                {
                    return false;
                }
                var parsed = new float[count];
                for (int i = 0; i < count; i++)
                {
                    if (!TryParseFloat(parts[i], out parsed[i]))
                    {
                        return false;
                    }
                }
                values = parsed;
                return true;
            }

            if (string.Equals(type, "Float", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryParseFloat(rawValue, out var f))
                {
                    errorText = "Invalid float value.";
                    return false;
                }
                parsedValue = f;
                return true;
            }

            if (string.Equals(type, "Int", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryParseInt(rawValue, out var n))
                {
                    errorText = "Invalid int value.";
                    return false;
                }
                parsedValue = n;
                return true;
            }

            if (string.Equals(type, "Vec2", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryParseVector(rawValue, 2, out var v))
                {
                    errorText = "Invalid Vec2. Use: x, y";
                    return false;
                }
                parsedValue = new Vector2(v[0], v[1]);
                return true;
            }

            if (string.Equals(type, "Vec3", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryParseVector(rawValue, 3, out var v))
                {
                    errorText = "Invalid Vec3. Use: x, y, z";
                    return false;
                }
                parsedValue = new Vector3(v[0], v[1], v[2]);
                return true;
            }

            if (string.Equals(type, "Vec4", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryParseVector(rawValue, 4, out var v))
                {
                    errorText = "Invalid Vec4. Use: x, y, z, w";
                    return false;
                }
                parsedValue = new Vector4(v[0], v[1], v[2], v[3]);
                return true;
            }

            if (string.Equals(type, "Option", StringComparison.OrdinalIgnoreCase))
            {
                if (bool.TryParse(rawValue, out var b))
                {
                    parsedValue = b;
                    return true;
                }

                if (TryParseInt(rawValue, out var n))
                {
                    parsedValue = n;
                    return true;
                }

                if (TryParseFloat(rawValue, out var f))
                {
                    parsedValue = f;
                    return true;
                }

                parsedValue = rawValue;
                return true;
            }

            if (bool.TryParse(rawValue, out var boolValue))
            {
                parsedValue = boolValue;
                return true;
            }

            if (TryParseInt(rawValue, out var intValue))
            {
                parsedValue = intValue;
                return true;
            }

            if (TryParseFloat(rawValue, out var floatValue))
            {
                parsedValue = floatValue;
                return true;
            }

            parsedValue = rawValue;
            return true;
        }

        public bool IsLikelyBoolOption(string name, string value)
        {
            if (value.Equals("true", StringComparison.OrdinalIgnoreCase) || value.Equals("false", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if ((value == "0" || value == "1") &&
                (name.StartsWith("Enable", StringComparison.OrdinalIgnoreCase) ||
                 name.StartsWith("Use", StringComparison.OrdinalIgnoreCase) ||
                 name.StartsWith("Has", StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            return false;
        }

        public bool IsColorVec4Param(string name, string type)
        {
            if (!string.Equals(type, "Vec4", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return name.StartsWith("BaseColorLayer", StringComparison.OrdinalIgnoreCase) ||
                   name.StartsWith("ShadowingColorLayer", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(name, "ShadowingColor", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(name, "BaseColor", StringComparison.OrdinalIgnoreCase) ||
                   name.StartsWith("EmissionColor", StringComparison.OrdinalIgnoreCase) ||
                   name.StartsWith("SubsurfaceColor", StringComparison.OrdinalIgnoreCase);
        }

        public bool TryParseVec4(string text, out Vector4 value)
        {
            value = default;
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            var parts = text.Split(',')
                .Select(p => p.Trim())
                .Where(p => p.Length > 0)
                .ToArray();
            if (parts.Length != 4)
            {
                return false;
            }

            if (!float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float x)) return false;
            if (!float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float y)) return false;
            if (!float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float z)) return false;
            if (!float.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out float w)) return false;

            value = new Vector4(x, y, z, w);
            return true;
        }

        public int ClampToByte(float value)
        {
            if (float.IsNaN(value))
            {
                return 0;
            }

            value = Math.Clamp(value, 0f, 1f);
            return (int)MathF.Round(value * 255f);
        }

        public string FormatOverrideValue(string type, object value)
        {
            switch (value)
            {
                case bool b:
                    return b ? "True" : "False";
                case int i:
                    return i.ToString(CultureInfo.InvariantCulture);
                case float f:
                    return f.ToString("0.####", CultureInfo.InvariantCulture);
                case Vector2 v2:
                    return $"{v2.X.ToString("0.####", CultureInfo.InvariantCulture)}, {v2.Y.ToString("0.####", CultureInfo.InvariantCulture)}";
                case Vector3 v3:
                    return $"{v3.X.ToString("0.####", CultureInfo.InvariantCulture)}, {v3.Y.ToString("0.####", CultureInfo.InvariantCulture)}, {v3.Z.ToString("0.####", CultureInfo.InvariantCulture)}";
                case Vector4 v4:
                    return $"{v4.X.ToString("0.####", CultureInfo.InvariantCulture)}, {v4.Y.ToString("0.####", CultureInfo.InvariantCulture)}, {v4.Z.ToString("0.####", CultureInfo.InvariantCulture)}, {v4.W.ToString("0.####", CultureInfo.InvariantCulture)}";
                default:
                    return value.ToString() ?? string.Empty;
            }
        }
    }
}
