using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GFTool.Renderer.Scene.GraphicsObjects;
using OpenTK.Mathematics;

namespace TrinityModelViewer.Export
{
    internal static class TrmmtBinaryPatcher
    {
        // Schema-based vtable indices for the TRMMT "metadata/variation" flavor (SV/ZA) used by the viewer.
        // This patcher updates only selection indices and per-variation parameter values, preserving all other bytes.

        private const int Root_ItemList = 2;

        private const int Item_Name = 0;
        private const int Item_ParamList = 3;

        private const int MetaParam_Name = 0;
        private const int MetaParam_OverrideDefaultValue = 8;
        private const int MetaParam_UseNoAnime = 9;
        private const int MetaParam_NoAnimeParam = 10;

        private const int NoAnime_VariationCount = 0;
        private const int NoAnime_MaterialList = 1;

        private const int MetaMaterial_Name = 0;
        private const int MetaMaterial_FloatList = 1;
        private const int MetaMaterial_Float3List = 2;
        private const int MetaMaterial_Float4List = 3;
        private const int MetaMaterial_IntList = 4;

        private const int ParamTable_Name = 0;
        private const int ParamTable_Values = 1;

        public static void ExportEditedTrmmtPreserveAllFields(string sourceTrmmtPath, Model model, string outputTrmmtPath)
        {
            if (string.IsNullOrWhiteSpace(sourceTrmmtPath)) throw new ArgumentException("Missing source TRMMT path.", nameof(sourceTrmmtPath));
            if (model == null) throw new ArgumentNullException(nameof(model));
            if (string.IsNullOrWhiteSpace(outputTrmmtPath)) throw new ArgumentException("Missing output TRMMT path.", nameof(outputTrmmtPath));
            if (!File.Exists(sourceTrmmtPath)) throw new FileNotFoundException("Source TRMMT not found.", sourceTrmmtPath);

            var data = File.ReadAllBytes(sourceTrmmtPath);
            var fb = new FlatBufferBinary(data);

            bool changed = PatchTrmmtInPlace(fb, model);

            Directory.CreateDirectory(Path.GetDirectoryName(outputTrmmtPath) ?? ".");
            if (!changed)
            {
                if (!string.Equals(Path.GetFullPath(sourceTrmmtPath), Path.GetFullPath(outputTrmmtPath), StringComparison.OrdinalIgnoreCase))
                {
                    File.Copy(sourceTrmmtPath, outputTrmmtPath, overwrite: true);
                }
                return;
            }

            File.WriteAllBytes(outputTrmmtPath, fb.Buffer);
        }

        private static bool PatchTrmmtInPlace(FlatBufferBinary fb, Model model)
        {
            var selectionMap = BuildSelectionMap(model.GetMaterialMetadataSelectionsSnapshot());
            var overrideGroups = BuildOverrideGroups(model.GetMaterialMetadataValueOverridesSnapshot());

            if (selectionMap.Count == 0 && overrideGroups.Count == 0)
            {
                return false;
            }

            int root = fb.GetRootTableOffset();
            int itemListField = fb.GetFieldAbsoluteOffset(root, Root_ItemList);
            if (itemListField == 0)
            {
                return false;
            }

            int itemVec = fb.GetVectorDataStartFromUOffsetField(itemListField, out int itemCount);
            if (itemVec == 0 || itemCount <= 0)
            {
                return false;
            }

            bool anyPatched = false;

            for (int itemIndex = 0; itemIndex < itemCount; itemIndex++)
            {
                int item = fb.GetVectorElementTableOffset(itemVec, itemIndex);
                if (item == 0)
                {
                    continue;
                }

                int paramListField = fb.GetFieldAbsoluteOffset(item, Item_ParamList);
                if (paramListField == 0)
                {
                    // Not the "variation metadata" flavor.
                    continue;
                }

                string setName = ReadSetName(fb, item, itemIndex);

                int paramVec = fb.GetVectorDataStartFromUOffsetField(paramListField, out int paramCount);
                if (paramVec == 0 || paramCount <= 0)
                {
                    continue;
                }

                for (int p = 0; p < paramCount; p++)
                {
                    int param = fb.GetVectorElementTableOffset(paramVec, p);
                    if (param == 0)
                    {
                        continue;
                    }

                    string paramName = ReadStringField(fb, param, MetaParam_Name);
                    if (string.IsNullOrWhiteSpace(paramName))
                    {
                        continue;
                    }

                    bool useNoAnime = ReadBoolField(fb, param, MetaParam_UseNoAnime);
                    if (!useNoAnime)
                    {
                        continue;
                    }

                    int noAnimeField = fb.GetFieldAbsoluteOffset(param, MetaParam_NoAnimeParam);
                    int noAnime = noAnimeField == 0 ? 0 : fb.DerefUOffset(noAnimeField);
                    if (noAnime == 0)
                    {
                        continue;
                    }

                    int variationCount = ReadInt32Field(fb, noAnime, NoAnime_VariationCount);
                    if (variationCount <= 0)
                    {
                        continue;
                    }

                    // Persist selected index for this set/param into OverrideDefaultValue.
                    if (selectionMap.TryGetValue((setName, paramName), out int desiredSelection))
                    {
                        desiredSelection = Math.Clamp(desiredSelection, 0, variationCount - 1);
                        int selField = fb.GetFieldAbsoluteOffset(param, MetaParam_OverrideDefaultValue);
                        if (selField != 0)
                        {
                            fb.WriteInt32(selField, desiredSelection);
                            anyPatched = true;
                        }
                    }

                    // Persist any value overrides by writing the overridden variation values back into the arrays.
                    if (!overrideGroups.TryGetValue((setName, paramName), out var overridesForParam) || overridesForParam.Count == 0)
                    {
                        continue;
                    }

                    int materialListField = fb.GetFieldAbsoluteOffset(noAnime, NoAnime_MaterialList);
                    if (materialListField == 0)
                    {
                        continue;
                    }

                    int matVec = fb.GetVectorDataStartFromUOffsetField(materialListField, out int matCount);
                    if (matVec == 0 || matCount <= 0)
                    {
                        continue;
                    }

                    // Index overrides by material+uniform for quick lookup.
                    var perMaterial = overridesForParam
                        .GroupBy(o => o.MaterialName ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                        .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

                    for (int m = 0; m < matCount; m++)
                    {
                        int mat = fb.GetVectorElementTableOffset(matVec, m);
                        if (mat == 0)
                        {
                            continue;
                        }

                        string matName = ReadStringField(fb, mat, MetaMaterial_Name);
                        if (string.IsNullOrWhiteSpace(matName) || !perMaterial.TryGetValue(matName, out var overridesForMaterial))
                        {
                            continue;
                        }

                        anyPatched |= PatchFloatList(fb, mat, MetaMaterial_FloatList, overridesForMaterial);
                        anyPatched |= PatchIntList(fb, mat, MetaMaterial_IntList, overridesForMaterial);
                        anyPatched |= PatchVec3List(fb, mat, MetaMaterial_Float3List, overridesForMaterial);
                        anyPatched |= PatchVec4List(fb, mat, MetaMaterial_Float4List, overridesForMaterial);
                    }
                }
            }

            return anyPatched;
        }

        private static bool PatchFloatList(FlatBufferBinary fb, int mat, int fieldIndex, List<Model.MaterialMetadataValueOverride> overrides)
        {
            int listField = fb.GetFieldAbsoluteOffset(mat, fieldIndex);
            if (listField == 0)
            {
                return false;
            }

            int vec = fb.GetVectorDataStartFromUOffsetField(listField, out int count);
            if (vec == 0 || count <= 0)
            {
                return false;
            }

            bool patched = false;
            for (int i = 0; i < count; i++)
            {
                int p = fb.GetVectorElementTableOffset(vec, i);
                if (p == 0)
                {
                    continue;
                }

                string name = ReadStringField(fb, p, ParamTable_Name);
                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                foreach (var ov in overrides.Where(o => string.Equals(o.UniformName, name, StringComparison.OrdinalIgnoreCase) && o.Value is float or int))
                {
                    int valuesField = fb.GetFieldAbsoluteOffset(p, ParamTable_Values);
                    if (valuesField == 0)
                    {
                        continue;
                    }

                    int dataStart = fb.GetVectorDataStartFromUOffsetField(valuesField, out int len);
                    if (dataStart == 0 || len <= 0)
                    {
                        continue;
                    }

                    int idx = Math.Clamp(ov.VariationIndex, 0, len - 1);
                    float value = ov.Value is int ni ? ni : (float)ov.Value;
                    fb.WriteSingle(dataStart + (idx * 4), value);
                    patched = true;
                }
            }

            return patched;
        }

        private static bool PatchIntList(FlatBufferBinary fb, int mat, int fieldIndex, List<Model.MaterialMetadataValueOverride> overrides)
        {
            int listField = fb.GetFieldAbsoluteOffset(mat, fieldIndex);
            if (listField == 0)
            {
                return false;
            }

            int vec = fb.GetVectorDataStartFromUOffsetField(listField, out int count);
            if (vec == 0 || count <= 0)
            {
                return false;
            }

            bool patched = false;
            for (int i = 0; i < count; i++)
            {
                int p = fb.GetVectorElementTableOffset(vec, i);
                if (p == 0)
                {
                    continue;
                }

                string name = ReadStringField(fb, p, ParamTable_Name);
                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                foreach (var ov in overrides.Where(o => string.Equals(o.UniformName, name, StringComparison.OrdinalIgnoreCase) && o.Value is int or float))
                {
                    int valuesField = fb.GetFieldAbsoluteOffset(p, ParamTable_Values);
                    if (valuesField == 0)
                    {
                        continue;
                    }

                    int dataStart = fb.GetVectorDataStartFromUOffsetField(valuesField, out int len);
                    if (dataStart == 0 || len <= 0)
                    {
                        continue;
                    }

                    int idx = Math.Clamp(ov.VariationIndex, 0, len - 1);
                    int value = ov.Value is float nf ? (int)MathF.Round(nf) : (int)ov.Value;
                    fb.WriteInt32(dataStart + (idx * 4), value);
                    patched = true;
                }
            }

            return patched;
        }

        private static bool PatchVec3List(FlatBufferBinary fb, int mat, int fieldIndex, List<Model.MaterialMetadataValueOverride> overrides)
        {
            int listField = fb.GetFieldAbsoluteOffset(mat, fieldIndex);
            if (listField == 0)
            {
                return false;
            }

            int vec = fb.GetVectorDataStartFromUOffsetField(listField, out int count);
            if (vec == 0 || count <= 0)
            {
                return false;
            }

            bool patched = false;
            for (int i = 0; i < count; i++)
            {
                int p = fb.GetVectorElementTableOffset(vec, i);
                if (p == 0)
                {
                    continue;
                }

                string name = ReadStringField(fb, p, ParamTable_Name);
                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                foreach (var ov in overrides.Where(o => string.Equals(o.UniformName, name, StringComparison.OrdinalIgnoreCase) && o.Value is Vector3))
                {
                    int valuesField = fb.GetFieldAbsoluteOffset(p, ParamTable_Values);
                    if (valuesField == 0)
                    {
                        continue;
                    }

                    int dataStart = fb.GetVectorDataStartFromUOffsetField(valuesField, out int len);
                    if (dataStart == 0 || len <= 0)
                    {
                        continue;
                    }

                    int idx = Math.Clamp(ov.VariationIndex, 0, len - 1);
                    int elem = dataStart + (idx * 12);
                    var v = (Vector3)ov.Value;
                    fb.WriteSingle(elem + 0, v.X);
                    fb.WriteSingle(elem + 4, v.Y);
                    fb.WriteSingle(elem + 8, v.Z);
                    patched = true;
                }
            }

            return patched;
        }

        private static bool PatchVec4List(FlatBufferBinary fb, int mat, int fieldIndex, List<Model.MaterialMetadataValueOverride> overrides)
        {
            int listField = fb.GetFieldAbsoluteOffset(mat, fieldIndex);
            if (listField == 0)
            {
                return false;
            }

            int vec = fb.GetVectorDataStartFromUOffsetField(listField, out int count);
            if (vec == 0 || count <= 0)
            {
                return false;
            }

            bool patched = false;
            for (int i = 0; i < count; i++)
            {
                int p = fb.GetVectorElementTableOffset(vec, i);
                if (p == 0)
                {
                    continue;
                }

                string name = ReadStringField(fb, p, ParamTable_Name);
                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                foreach (var ov in overrides.Where(o => string.Equals(o.UniformName, name, StringComparison.OrdinalIgnoreCase) && o.Value is Vector4))
                {
                    int valuesField = fb.GetFieldAbsoluteOffset(p, ParamTable_Values);
                    if (valuesField == 0)
                    {
                        continue;
                    }

                    int dataStart = fb.GetVectorDataStartFromUOffsetField(valuesField, out int len);
                    if (dataStart == 0 || len <= 0)
                    {
                        continue;
                    }

                    int idx = Math.Clamp(ov.VariationIndex, 0, len - 1);
                    int elem = dataStart + (idx * 16);
                    var v = (Vector4)ov.Value;
                    // Vector4f struct is W,X,Y,Z on disk; we store values in OpenTK as X,Y,Z,W.
                    fb.WriteSingle(elem + 0, v.X);
                    fb.WriteSingle(elem + 4, v.Y);
                    fb.WriteSingle(elem + 8, v.Z);
                    fb.WriteSingle(elem + 12, v.W);
                    patched = true;
                }
            }

            return patched;
        }

        private static Dictionary<(string SetName, string ParamName), int> BuildSelectionMap(KeyValuePair<string, int>[] selections)
        {
            var map = new Dictionary<(string, string), int>();
            if (selections == null || selections.Length == 0)
            {
                return map;
            }

            foreach (var kv in selections)
            {
                if (TryParseSelectionKey(kv.Key, out var setName, out var paramName))
                {
                    map[(NormalizeSetName(setName), paramName)] = kv.Value;
                }
            }

            return map;
        }

        private static Dictionary<(string SetName, string ParamName), List<Model.MaterialMetadataValueOverride>> BuildOverrideGroups(Model.MaterialMetadataValueOverride[] overrides)
        {
            var map = new Dictionary<(string, string), List<Model.MaterialMetadataValueOverride>>();
            if (overrides == null || overrides.Length == 0)
            {
                return map;
            }

            foreach (var ov in overrides)
            {
                if (string.IsNullOrWhiteSpace(ov.SetName) || string.IsNullOrWhiteSpace(ov.MetadataParamName))
                {
                    continue;
                }

                var key = (NormalizeSetName(ov.SetName), ov.MetadataParamName);
                if (!map.TryGetValue(key, out var list))
                {
                    list = new List<Model.MaterialMetadataValueOverride>();
                    map[key] = list;
                }
                list.Add(ov);
            }

            return map;
        }

        private static bool TryParseSelectionKey(string key, out string setName, out string paramName)
        {
            setName = string.Empty;
            paramName = string.Empty;
            if (string.IsNullOrWhiteSpace(key))
            {
                return false;
            }

            int sep = key.IndexOf("::", StringComparison.Ordinal);
            if (sep <= 0 || sep + 2 >= key.Length)
            {
                return false;
            }

            setName = key.Substring(0, sep);
            paramName = key.Substring(sep + 2);
            return !string.IsNullOrWhiteSpace(setName) && !string.IsNullOrWhiteSpace(paramName);
        }

        private static string ReadSetName(FlatBufferBinary fb, int itemTable, int itemIndex)
        {
            string name = ReadStringField(fb, itemTable, Item_Name);
            if (!string.IsNullOrWhiteSpace(name))
            {
                return name;
            }

            return itemIndex == 0 ? "default" : $"set_{itemIndex}";
        }

        private static string NormalizeSetName(string setName)
        {
            if (string.IsNullOrWhiteSpace(setName))
            {
                return "default";
            }

            // Older UI state used "<default>" when no set name exists.
            if (string.Equals(setName, "<default>", StringComparison.OrdinalIgnoreCase))
            {
                return "default";
            }

            return setName;
        }

        private static string ReadStringField(FlatBufferBinary fb, int table, int fieldIndex)
        {
            int field = fb.GetFieldAbsoluteOffset(table, fieldIndex);
            return field == 0 ? string.Empty : fb.ReadStringAtUOffsetField(field);
        }

        private static bool ReadBoolField(FlatBufferBinary fb, int table, int fieldIndex)
        {
            int field = fb.GetFieldAbsoluteOffset(table, fieldIndex);
            return field != 0 && fb.ReadBool(field);
        }

        private static int ReadInt32Field(FlatBufferBinary fb, int table, int fieldIndex)
        {
            int field = fb.GetFieldAbsoluteOffset(table, fieldIndex);
            return field == 0 ? 0 : fb.ReadInt32(field);
        }
    }
}
