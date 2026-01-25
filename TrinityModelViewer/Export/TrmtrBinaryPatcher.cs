using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GFTool.Renderer.Scene.GraphicsObjects;
using OpenTK.Mathematics;

namespace TrinityModelViewer.Export
{
    internal static class TrmtrBinaryPatcher
    {
        private const int Material_ItemList = 1;

        private const int MaterialItem_Name = 0;
        private const int MaterialItem_ShaderList = 1;
        private const int MaterialItem_FloatParams = 4;
        private const int MaterialItem_Float4Params = 7;
        private const int MaterialItem_IntParams = 9;

        private const int Param_Name = 0;
        private const int Param_Value = 1;

        private const int Shader_StringParams = 1;

        private const int StringParam_Name = 0;
        private const int StringParam_Value = 1;

        public static bool PatchTrmtrInPlace(string trmtrPath, Model model)
        {
            if (string.IsNullOrWhiteSpace(trmtrPath)) throw new ArgumentException("Missing TRMTR path.", nameof(trmtrPath));
            if (model == null) throw new ArgumentNullException(nameof(model));
            if (!File.Exists(trmtrPath)) throw new FileNotFoundException("TRMTR not found.", trmtrPath);

            var runtimeByName = model.GetMaterials()
                .Where(m => m != null && !string.IsNullOrWhiteSpace(m.Name) && m.HasUniformOverrides)
                .ToDictionary(m => m.Name, StringComparer.OrdinalIgnoreCase);

            if (runtimeByName.Count == 0)
            {
                return false;
            }

            var data = File.ReadAllBytes(trmtrPath);
            var fb = new FlatBufferBinary(data);

            int root = fb.GetRootTableOffset();
            int itemListField = fb.GetFieldAbsoluteOffset(root, Material_ItemList);
            if (itemListField == 0)
            {
                throw new InvalidOperationException("TRMTR missing item_list.");
            }

            int itemVec = fb.GetVectorDataStartFromUOffsetField(itemListField, out int itemCount);
            if (itemVec == 0 || itemCount <= 0)
            {
                throw new InvalidOperationException("TRMTR item_list empty.");
            }

            for (int i = 0; i < itemCount; i++)
            {
                int item = fb.GetVectorElementTableOffset(itemVec, i);
                if (item == 0)
                {
                    continue;
                }

                int nameField = fb.GetFieldAbsoluteOffset(item, MaterialItem_Name);
                if (nameField == 0)
                {
                    continue;
                }

                string matName = fb.ReadStringAtUOffsetField(nameField);
                if (string.IsNullOrWhiteSpace(matName) || !runtimeByName.TryGetValue(matName, out var runtime))
                {
                    continue;
                }

                var overrides = runtime.GetUniformOverridesSnapshot();
                if (overrides.Length == 0)
                {
                    continue;
                }

                var byName = overrides.ToDictionary(k => k.Key, v => v.Value, StringComparer.OrdinalIgnoreCase);

                PatchShaderStringParams(fb, item, byName);
                PatchFloatList(fb, item, MaterialItem_FloatParams, byName);
                PatchIntList(fb, item, MaterialItem_IntParams, byName);
                PatchFloat4List(fb, item, MaterialItem_Float4Params, byName);
            }

            File.WriteAllBytes(trmtrPath, fb.Buffer);
            return true;
        }

        public static void ExportEditedTrmtrPreserveAllFields(string sourceTrmtrPath, Model model, string outputTrmtrPath)
        {
            if (string.IsNullOrWhiteSpace(sourceTrmtrPath)) throw new ArgumentException("Missing source TRMTR path.", nameof(sourceTrmtrPath));
            if (model == null) throw new ArgumentNullException(nameof(model));
            if (string.IsNullOrWhiteSpace(outputTrmtrPath)) throw new ArgumentException("Missing output TRMTR path.", nameof(outputTrmtrPath));
            if (!File.Exists(sourceTrmtrPath)) throw new FileNotFoundException("Source TRMTR not found.", sourceTrmtrPath);

            var data = File.ReadAllBytes(sourceTrmtrPath);
            var fb = new FlatBufferBinary(data);

            var runtimeByName = model.GetMaterials()
                .Where(m => m != null && !string.IsNullOrWhiteSpace(m.Name) && m.HasUniformOverrides)
                .ToDictionary(m => m.Name, StringComparer.OrdinalIgnoreCase);

            if (runtimeByName.Count == 0)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(outputTrmtrPath) ?? ".");
                if (!string.Equals(Path.GetFullPath(sourceTrmtrPath), Path.GetFullPath(outputTrmtrPath), StringComparison.OrdinalIgnoreCase))
                {
                    File.Copy(sourceTrmtrPath, outputTrmtrPath, overwrite: true);
                }
                return;
            }

            int root = fb.GetRootTableOffset();
            int itemListField = fb.GetFieldAbsoluteOffset(root, Material_ItemList);
            if (itemListField == 0)
            {
                throw new InvalidOperationException("TRMTR missing item_list.");
            }

            int itemVec = fb.GetVectorDataStartFromUOffsetField(itemListField, out int itemCount);
            if (itemVec == 0 || itemCount <= 0)
            {
                throw new InvalidOperationException("TRMTR item_list empty.");
            }

            for (int i = 0; i < itemCount; i++)
            {
                int item = fb.GetVectorElementTableOffset(itemVec, i);
                if (item == 0)
                {
                    continue;
                }

                int nameField = fb.GetFieldAbsoluteOffset(item, MaterialItem_Name);
                if (nameField == 0)
                {
                    continue;
                }

                string matName = fb.ReadStringAtUOffsetField(nameField);
                if (string.IsNullOrWhiteSpace(matName) || !runtimeByName.TryGetValue(matName, out var runtime))
                {
                    continue;
                }

                var overrides = runtime.GetUniformOverridesSnapshot();
                if (overrides.Length == 0)
                {
                    continue;
                }

                var byName = overrides.ToDictionary(k => k.Key, v => v.Value, StringComparer.OrdinalIgnoreCase);

                PatchShaderStringParams(fb, item, byName);
                PatchFloatList(fb, item, MaterialItem_FloatParams, byName);
                PatchIntList(fb, item, MaterialItem_IntParams, byName);
                PatchFloat4List(fb, item, MaterialItem_Float4Params, byName);
            }

            Directory.CreateDirectory(Path.GetDirectoryName(outputTrmtrPath) ?? ".");
            File.WriteAllBytes(outputTrmtrPath, fb.Buffer);
        }

        private static void PatchShaderStringParams(FlatBufferBinary fb, int materialItem, Dictionary<string, object> byName)
        {
            int shaderListField = fb.GetFieldAbsoluteOffset(materialItem, MaterialItem_ShaderList);
            if (shaderListField == 0)
            {
                return;
            }

            int shaderVec = fb.GetVectorDataStartFromUOffsetField(shaderListField, out int shaderCount);
            if (shaderVec == 0 || shaderCount <= 0)
            {
                return;
            }

            for (int i = 0; i < shaderCount; i++)
            {
                int shader = fb.GetVectorElementTableOffset(shaderVec, i);
                if (shader == 0)
                {
                    continue;
                }

                int paramListField = fb.GetFieldAbsoluteOffset(shader, Shader_StringParams);
                if (paramListField == 0)
                {
                    continue;
                }

                int paramVec = fb.GetVectorDataStartFromUOffsetField(paramListField, out int paramCount);
                if (paramVec == 0 || paramCount <= 0)
                {
                    continue;
                }

                for (int o = 0; o < paramCount; o++)
                {
                    int p = fb.GetVectorElementTableOffset(paramVec, o);
                    if (p == 0)
                    {
                        continue;
                    }

                    int nameField = fb.GetFieldAbsoluteOffset(p, StringParam_Name);
                    if (nameField == 0)
                    {
                        continue;
                    }

                    string name = fb.ReadStringAtUOffsetField(nameField);
                    if (string.IsNullOrWhiteSpace(name) || !byName.TryGetValue(name, out var overrideValue))
                    {
                        continue;
                    }

                    int valueField = fb.GetFieldAbsoluteOffset(p, StringParam_Value);
                    if (valueField == 0)
                    {
                        continue;
                    }

                    // FlatBuffer strings have no spare capacity; we can only overwrite in-place if it fits.
                    string existingValue = fb.ReadStringAtUOffsetField(valueField);
                    string desired = ConvertOptionChoiceToString(overrideValue, existingValue);
                    fb.TryOverwriteStringAtUOffsetField(valueField, desired);
                }
            }
        }

        private static string ConvertOptionChoiceToString(object value, string existingChoice)
        {
            if (value == null)
            {
                return string.Empty;
            }

            switch (value)
            {
                case string s:
                    // If both the existing and desired values look like bools, treat it as a bool so we can
                    // fall back to 0/1 when "False" doesn't fit in-place over "True".
                    if ((string.Equals(existingChoice, "true", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(existingChoice, "false", StringComparison.OrdinalIgnoreCase)) &&
                        (string.Equals(s, "true", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(s, "false", StringComparison.OrdinalIgnoreCase)))
                    {
                        return ConvertOptionChoiceToString(string.Equals(s, "true", StringComparison.OrdinalIgnoreCase), existingChoice);
                    }
                    return s;
                case bool b:
                    // Preserve the file's original convention where possible:
                    // - "True"/"False" (or lowercase variants) stay as True/False.
                    // - otherwise default to "0"/"1" (1-char so in-place patching is likely to fit).
                    if (string.Equals(existingChoice, "true", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(existingChoice, "false", StringComparison.OrdinalIgnoreCase))
                    {
                        bool upper = existingChoice.Length > 0 && char.IsUpper(existingChoice[0]);
                        string desired = upper
                            ? (b ? "True" : "False")
                            : (b ? "true" : "false");

                        // FlatBuffer strings have no spare capacity; if the new value is longer than the existing one
                        if (desired.Length <= existingChoice.Length)
                        {
                            return desired;
                        }
                        return b ? "1" : "0";
                    }
                    return b ? "1" : "0";
                case int i:
                    return i.ToString(System.Globalization.CultureInfo.InvariantCulture);
                case float f:
                    return f.ToString("0.####", System.Globalization.CultureInfo.InvariantCulture);
                case double d:
                    return d.ToString("0.####", System.Globalization.CultureInfo.InvariantCulture);
                default:
                    return value.ToString() ?? string.Empty;
            }
        }

        private static void PatchFloatList(FlatBufferBinary fb, int materialItem, int listFieldIndex, Dictionary<string, object> byName)
        {
            int listField = fb.GetFieldAbsoluteOffset(materialItem, listFieldIndex);
            if (listField == 0)
            {
                return;
            }

            int vec = fb.GetVectorDataStartFromUOffsetField(listField, out int count);
            if (vec == 0 || count <= 0)
            {
                return;
            }

            for (int i = 0; i < count; i++)
            {
                int param = fb.GetVectorElementTableOffset(vec, i);
                if (param == 0)
                {
                    continue;
                }

                int nameField = fb.GetFieldAbsoluteOffset(param, Param_Name);
                if (nameField == 0)
                {
                    continue;
                }

                string name = fb.ReadStringAtUOffsetField(nameField);
                if (string.IsNullOrWhiteSpace(name) || !byName.TryGetValue(name, out var value) || !TryConvertFloat(value, out float f))
                {
                    continue;
                }

                int valueField = fb.GetFieldAbsoluteOffset(param, Param_Value);
                if (valueField != 0)
                {
                    fb.WriteSingle(valueField, f);
                }
            }
        }

        private static void PatchIntList(FlatBufferBinary fb, int materialItem, int listFieldIndex, Dictionary<string, object> byName)
        {
            int listField = fb.GetFieldAbsoluteOffset(materialItem, listFieldIndex);
            if (listField == 0)
            {
                return;
            }

            int vec = fb.GetVectorDataStartFromUOffsetField(listField, out int count);
            if (vec == 0 || count <= 0)
            {
                return;
            }

            for (int i = 0; i < count; i++)
            {
                int param = fb.GetVectorElementTableOffset(vec, i);
                if (param == 0)
                {
                    continue;
                }

                int nameField = fb.GetFieldAbsoluteOffset(param, Param_Name);
                if (nameField == 0)
                {
                    continue;
                }

                string name = fb.ReadStringAtUOffsetField(nameField);
                if (string.IsNullOrWhiteSpace(name) || !byName.TryGetValue(name, out var value) || !TryConvertInt(value, out int n))
                {
                    continue;
                }

                int valueField = fb.GetFieldAbsoluteOffset(param, Param_Value);
                if (valueField != 0)
                {
                    fb.WriteInt32(valueField, n);
                }
            }
        }

        private static void PatchFloat4List(FlatBufferBinary fb, int materialItem, int listFieldIndex, Dictionary<string, object> byName)
        {
            int listField = fb.GetFieldAbsoluteOffset(materialItem, listFieldIndex);
            if (listField == 0)
            {
                return;
            }

            int vec = fb.GetVectorDataStartFromUOffsetField(listField, out int count);
            if (vec == 0 || count <= 0)
            {
                return;
            }

            for (int i = 0; i < count; i++)
            {
                int param = fb.GetVectorElementTableOffset(vec, i);
                if (param == 0)
                {
                    continue;
                }

                int nameField = fb.GetFieldAbsoluteOffset(param, Param_Name);
                if (nameField == 0)
                {
                    continue;
                }

                string name = fb.ReadStringAtUOffsetField(nameField);
                if (string.IsNullOrWhiteSpace(name) || !byName.TryGetValue(name, out var value) || value is not Vector4 v)
                {
                    continue;
                }

                int valueField = fb.GetFieldAbsoluteOffset(param, Param_Value);
                if (valueField != 0)
                {
                    fb.WriteSingle(valueField + 0, v.X);
                    fb.WriteSingle(valueField + 4, v.Y);
                    fb.WriteSingle(valueField + 8, v.Z);
                    fb.WriteSingle(valueField + 12, v.W);
                }
            }
        }

        private static bool TryConvertFloat(object value, out float f)
        {
            switch (value)
            {
                case bool b:
                    f = b ? 1.0f : 0.0f;
                    return true;
                case float ff:
                    f = ff;
                    return true;
                case double dd:
                    f = (float)dd;
                    return true;
                case int ii:
                    f = ii;
                    return true;
                case string s when float.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var parsed):
                    f = parsed;
                    return true;
                default:
                    f = 0;
                    return false;
            }
        }

        private static bool TryConvertInt(object value, out int n)
        {
            switch (value)
            {
                case bool b:
                    n = b ? 1 : 0;
                    return true;
                case int ii:
                    n = ii;
                    return true;
                case float ff:
                    n = (int)MathF.Round(ff);
                    return true;
                case double dd:
                    n = (int)Math.Round(dd);
                    return true;
                case string s when int.TryParse(s, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var parsed):
                    n = parsed;
                    return true;
                default:
                    n = 0;
                    return false;
            }
        }
    }
}
