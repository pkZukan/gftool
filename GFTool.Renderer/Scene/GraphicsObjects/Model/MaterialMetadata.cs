using GFTool.Core.Utils;
using GFTool.Renderer.Core;
using GFTool.Renderer.Core.Graphics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using Trinity.Core.Flatbuffers.TR.Model;
using Trinity.Core.Utils;
using System.IO;
using System;
using Trinity.Core.Assets;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;


namespace GFTool.Renderer.Scene.GraphicsObjects
{
	    public partial class Model : RefObject
	    {
		        private static string GetMaterialMetadataAppliedKey(string materialName, string uniformName)
		        {
		            return $"{materialName}::{uniformName}";
		        }

		        private static bool TrySplitMaterialMetadataAppliedKey(string key, out string materialName, out string uniformName)
		        {
		            materialName = string.Empty;
		            uniformName = string.Empty;
		            if (string.IsNullOrWhiteSpace(key))
		            {
		                return false;
		            }

		            int sep = key.IndexOf("::", StringComparison.Ordinal);
		            if (sep <= 0 || sep + 2 >= key.Length)
		            {
		                return false;
		            }

		            materialName = key.Substring(0, sep);
		            uniformName = key.Substring(sep + 2);
		            return !string.IsNullOrWhiteSpace(materialName) && !string.IsNullOrWhiteSpace(uniformName);
		        }

		        private void ClearMaterialMetadataOverridesFromRuntimeMaterials()
		        {
		            if (materials == null || materials.Length == 0 || materialMetadataLastAppliedUniformNames.Count == 0)
		            {
		                materialMetadataLastAppliedUniformNames.Clear();
		                return;
		            }

		            foreach (var key in materialMetadataLastAppliedUniformNames)
		            {
		                if (!TrySplitMaterialMetadataAppliedKey(key, out var materialName, out var uniformName))
		                {
		                    continue;
		                }

		                if (!materialMap.TryGetValue(materialName, out var mat) || mat == null)
		                {
		                    continue;
		                }

		                mat.ClearUniformOverride(uniformName);
		            }

		            materialMetadataLastAppliedUniformNames.Clear();
		        }

		        private TrmmtMetaItem? TryGetActiveMaterialParamMetadataItem()
		        {
		            var metadata = TryGetMaterialMetadataForCurrentMaterials();
		            return metadata?.FindParamItem(currentMaterialSetName, currentMaterialFilePath);
		        }

		        private static int ClampVariationIndex(int index, int variationCount)
		        {
		            if (variationCount <= 0)
		            {
		                return 0;
		            }

		            return Math.Clamp(index, 0, variationCount - 1);
		        }

		        private int GetSelectedVariationIndex(TrmmtMetaParam param)
		        {
		            if (param == null || string.IsNullOrWhiteSpace(param.Name))
		            {
		                return 0;
		            }

		            int variationCount = param.NoAnimeParam?.VariationCount ?? 0;
		            int fallback = param.OverrideDefaultValue >= 0 ? param.OverrideDefaultValue : 0;
		            int selected = materialMetadataSelections.TryGetValue(GetMaterialMetadataSelectionKey(param.Name), out var v) ? v : fallback;
		            return ClampVariationIndex(selected, variationCount);
		        }

		        private void ApplyMaterialMetadataOverridesToRuntimeMaterials()
		        {
		            if (materials == null || materials.Length == 0)
		            {
		                return;
		            }

		            ClearMaterialMetadataOverridesFromRuntimeMaterials();

		            var item = TryGetActiveMaterialParamMetadataItem();
		            if (item?.ParamList == null || item.ParamList.Length == 0)
		            {
		                return;
		            }

		            bool AnyAffectsColorTable(string uniformName)
		            {
		                if (string.IsNullOrWhiteSpace(uniformName))
		                {
		                    return false;
		                }

		                return uniformName.StartsWith("BaseColorIndex", StringComparison.OrdinalIgnoreCase) ||
		                       string.Equals(uniformName, "ColorTableDivideNumber", StringComparison.OrdinalIgnoreCase) ||
		                       uniformName.StartsWith("BaseColorLayer", StringComparison.OrdinalIgnoreCase) ||
		                       uniformName.StartsWith("ShadowingColorLayer", StringComparison.OrdinalIgnoreCase);
		            }

		            var touchedColorTable = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		            foreach (var param in item.ParamList)
		            {
		                if (param == null || !param.UseNoAnime || string.IsNullOrWhiteSpace(param.Name))
		                {
		                    continue;
		                }

		                var noAnime = param.NoAnimeParam;
		                if (noAnime?.MaterialList == null || noAnime.MaterialList.Length == 0)
		                {
		                    continue;
		                }

		                int variationIndex = GetSelectedVariationIndex(param);

		                foreach (var mat in noAnime.MaterialList)
		                {
		                    if (mat == null || string.IsNullOrWhiteSpace(mat.MaterialName))
		                    {
		                        continue;
		                    }

		                    if (!materialMap.TryGetValue(mat.MaterialName, out var runtime) || runtime == null)
		                    {
		                        continue;
		                    }

		                    void ApplyUniform(string uniformName, object baseValue)
		                    {
		                        int varIndexForKey = variationIndex;
		                        string key = GetMaterialMetadataValueOverrideKey(param.Name, mat.MaterialName, uniformName, varIndexForKey);
		                        object value = baseValue;
		                        if (materialMetadataValueOverrides.TryGetValue(key, out var overrideEntry))
		                        {
		                            value = overrideEntry.Value;
		                        }

		                        runtime.SetUniformOverride(uniformName, value);
		                        materialMetadataLastAppliedUniformNames.Add(GetMaterialMetadataAppliedKey(mat.MaterialName, uniformName));

		                        if (AnyAffectsColorTable(uniformName))
		                        {
		                            touchedColorTable.Add(mat.MaterialName);
		                        }
		                    }

		                    foreach (var fp in mat.FloatParamList ?? Array.Empty<TrmmtMetaFloatParams>())
		                    {
		                        if (fp == null || string.IsNullOrWhiteSpace(fp.Name) || fp.Values == null || fp.Values.Length == 0)
		                        {
		                            continue;
		                        }
		                        float v = fp.Values[Math.Clamp(variationIndex, 0, fp.Values.Length - 1)];
		                        ApplyUniform(fp.Name, v);
		                    }

		                    foreach (var ip in mat.IntParamList ?? Array.Empty<TrmmtMetaIntParams>())
		                    {
		                        if (ip == null || string.IsNullOrWhiteSpace(ip.Name) || ip.Values == null || ip.Values.Length == 0)
		                        {
		                            continue;
		                        }
		                        int v = ip.Values[Math.Clamp(variationIndex, 0, ip.Values.Length - 1)];
		                        ApplyUniform(ip.Name, v);
		                    }

		                    foreach (var v3p in mat.Float3ParamList ?? Array.Empty<TrmmtMetaFloat3Params>())
		                    {
		                        if (v3p == null || string.IsNullOrWhiteSpace(v3p.Name) || v3p.Values == null || v3p.Values.Length == 0)
		                        {
		                            continue;
		                        }
		                        var v3 = v3p.Values[Math.Clamp(variationIndex, 0, v3p.Values.Length - 1)];
		                        ApplyUniform(v3p.Name, new Vector3(v3.X, v3.Y, v3.Z));
		                    }

		                    foreach (var v4p in mat.Float4ParamList ?? Array.Empty<TrmmtMetaFloat4Params>())
		                    {
		                        if (v4p == null || string.IsNullOrWhiteSpace(v4p.Name) || v4p.Values == null || v4p.Values.Length == 0)
		                        {
		                            continue;
		                        }
		                        var v4 = v4p.Values[Math.Clamp(variationIndex, 0, v4p.Values.Length - 1)];
		                        ApplyUniform(v4p.Name, new Vector4(v4.W, v4.X, v4.Y, v4.Z));
		                    }
		                }
		            }

		            foreach (var matName in touchedColorTable)
		            {
		                if (materialMap.TryGetValue(matName, out var mat) && mat != null)
		                {
		                    mat.RefreshColorTableOverridesFromUniformOverrides();
		                }
		            }
		        }

	        private string GetEffectiveMaterialSetNameForSelections()
	        {
	            return !string.IsNullOrWhiteSpace(currentMaterialSetName) ? currentMaterialSetName! : "<default>";
	        }

		        private string GetMaterialMetadataSelectionKey(string paramName)
		        {
		            return $"{GetEffectiveMaterialSetNameForSelections()}::{paramName}";
		        }

		        private string GetMaterialMetadataValueOverrideKey(string metadataParamName, string materialName, string uniformName, int variationIndex)
		        {
		            return $"{GetEffectiveMaterialSetNameForSelections()}::{metadataParamName}::{materialName}::{uniformName}::{variationIndex}";
		        }

	        public readonly struct MaterialVariantParam
	        {
	            public string Name { get; init; }
	            public int VariationCount { get; init; }
	            public int SelectedIndex { get; init; }
	        }

		        public readonly struct MaterialMetadataOverrideEntry
		        {
		            public string Name { get; init; }
		            public string Type { get; init; }
		            public object Value { get; init; }
		            public string? MetadataParamName { get; init; }
		            public string? MetadataMaterialName { get; init; }
		            public int MetadataVariationIndex { get; init; }
		        }

		        public readonly struct MaterialMetadataValueOverride
		        {
		            public string SetName { get; init; }
		            public string MetadataParamName { get; init; }
		            public string MaterialName { get; init; }
		            public string UniformName { get; init; }
		            public int VariationIndex { get; init; }
		            public string Type { get; init; }
		            public object Value { get; init; }
		        }

		        public IReadOnlyList<string> GetMaterialSetNames()
		        {
		            var meta = TryGetMaterialMetadataForCurrentMaterials();
		            if (meta == null)
	            {
	                return Array.Empty<string>();
	            }

		            var names = meta.GetSetNames();
		            return names.Count > 0 ? names : Array.Empty<string>();
	        }

		        public IReadOnlyList<MaterialVariantParam> GetMaterialVariantParams()
		        {
		            var item = TryGetActiveMaterialParamMetadataItem();
		            if (item?.ParamList == null || item.ParamList.Length == 0)
		            {
		                return Array.Empty<MaterialVariantParam>();
		            }

		            var list = new List<MaterialVariantParam>();
		            foreach (var param in item.ParamList)
		            {
		                if (param == null || !param.UseNoAnime || string.IsNullOrWhiteSpace(param.Name))
		                {
		                    continue;
		                }

		                int variationCount = param.NoAnimeParam?.VariationCount ?? 0;
		                if (variationCount <= 0)
		                {
		                    continue;
		                }

		                list.Add(new MaterialVariantParam
		                {
		                    Name = param.Name,
		                    VariationCount = variationCount,
		                    SelectedIndex = GetSelectedVariationIndex(param)
		                });
		            }

		            return list.Count > 0 ? list : Array.Empty<MaterialVariantParam>();
		        }

		        public IReadOnlyList<MaterialMetadataOverrideEntry> GetMaterialMetadataOverrideEntriesForMaterial(string materialName)
		        {
		            if (string.IsNullOrWhiteSpace(materialName))
		            {
		                return Array.Empty<MaterialMetadataOverrideEntry>();
		            }

		            var item = TryGetActiveMaterialParamMetadataItem();
		            if (item?.ParamList == null || item.ParamList.Length == 0)
		            {
		                return Array.Empty<MaterialMetadataOverrideEntry>();
		            }

		            var results = new List<MaterialMetadataOverrideEntry>();

		            foreach (var param in item.ParamList)
		            {
		                if (param == null || !param.UseNoAnime || string.IsNullOrWhiteSpace(param.Name))
		                {
		                    continue;
		                }

		                var noAnime = param.NoAnimeParam;
		                if (noAnime?.MaterialList == null || noAnime.MaterialList.Length == 0)
		                {
		                    continue;
		                }

		                int variationIndex = GetSelectedVariationIndex(param);

		                foreach (var mat in noAnime.MaterialList)
		                {
		                    if (mat == null || string.IsNullOrWhiteSpace(mat.MaterialName))
		                    {
		                        continue;
		                    }

		                    if (!MatchesMaterial(mat.MaterialName, materialName))
		                    {
		                        continue;
		                    }

		                    foreach (var fp in mat.FloatParamList ?? Array.Empty<TrmmtMetaFloatParams>())
		                    {
		                        if (fp == null || string.IsNullOrWhiteSpace(fp.Name) || fp.Values == null || fp.Values.Length == 0)
		                        {
		                            continue;
		                        }

		                        results.Add(new MaterialMetadataOverrideEntry
		                        {
		                            Name = fp.Name,
		                            Type = "Float",
		                            Value = fp.Values[Math.Clamp(variationIndex, 0, fp.Values.Length - 1)],
		                            MetadataParamName = param.Name,
		                            MetadataMaterialName = mat.MaterialName,
		                            MetadataVariationIndex = variationIndex
		                        });
		                    }

		                    foreach (var ip in mat.IntParamList ?? Array.Empty<TrmmtMetaIntParams>())
		                    {
		                        if (ip == null || string.IsNullOrWhiteSpace(ip.Name) || ip.Values == null || ip.Values.Length == 0)
		                        {
		                            continue;
		                        }

		                        results.Add(new MaterialMetadataOverrideEntry
		                        {
		                            Name = ip.Name,
		                            Type = "Int",
		                            Value = ip.Values[Math.Clamp(variationIndex, 0, ip.Values.Length - 1)],
		                            MetadataParamName = param.Name,
		                            MetadataMaterialName = mat.MaterialName,
		                            MetadataVariationIndex = variationIndex
		                        });
		                    }

		                    foreach (var v3p in mat.Float3ParamList ?? Array.Empty<TrmmtMetaFloat3Params>())
		                    {
		                        if (v3p == null || string.IsNullOrWhiteSpace(v3p.Name) || v3p.Values == null || v3p.Values.Length == 0)
		                        {
		                            continue;
		                        }

		                        var v3 = v3p.Values[Math.Clamp(variationIndex, 0, v3p.Values.Length - 1)];
		                        results.Add(new MaterialMetadataOverrideEntry
		                        {
		                            Name = v3p.Name,
		                            Type = "Vec3",
		                            Value = new Vector3(v3.X, v3.Y, v3.Z),
		                            MetadataParamName = param.Name,
		                            MetadataMaterialName = mat.MaterialName,
		                            MetadataVariationIndex = variationIndex
		                        });
		                    }

		                    foreach (var v4p in mat.Float4ParamList ?? Array.Empty<TrmmtMetaFloat4Params>())
		                    {
		                        if (v4p == null || string.IsNullOrWhiteSpace(v4p.Name) || v4p.Values == null || v4p.Values.Length == 0)
		                        {
		                            continue;
		                        }

		                        var v4 = v4p.Values[Math.Clamp(variationIndex, 0, v4p.Values.Length - 1)];
		                        results.Add(new MaterialMetadataOverrideEntry
		                        {
		                            Name = v4p.Name,
		                            Type = "Vec4",
		                            Value = new Vector4(v4.W, v4.X, v4.Y, v4.Z),
		                            MetadataParamName = param.Name,
		                            MetadataMaterialName = mat.MaterialName,
		                            MetadataVariationIndex = variationIndex
		                        });
		                    }
		                }
		            }

		            return results.Count > 0 ? results : Array.Empty<MaterialMetadataOverrideEntry>();
		        }

		        public bool TrySetMaterialMetadataValueOverride(string metadataParamName, string materialName, string uniformName, object value)
		        {
		            if (string.IsNullOrWhiteSpace(metadataParamName) ||
		                string.IsNullOrWhiteSpace(materialName) ||
		                string.IsNullOrWhiteSpace(uniformName))
		            {
		                return false;
		            }

		            var item = TryGetActiveMaterialParamMetadataItem();
		            if (item?.ParamList == null || item.ParamList.Length == 0)
		            {
		                return false;
		            }

		            var param = item.ParamList.FirstOrDefault(p => string.Equals(p?.Name, metadataParamName, StringComparison.OrdinalIgnoreCase));
		            if (param == null || !param.UseNoAnime || param.NoAnimeParam?.MaterialList == null)
		            {
		                return false;
		            }

		            int variationIndex = GetSelectedVariationIndex(param);

		            var mat = param.NoAnimeParam.MaterialList.FirstOrDefault(m => m != null && MatchesMaterial(m.MaterialName, materialName));
		            if (mat == null)
		            {
		                return false;
		            }

		            object? baseValue = null;
		            if (mat.FloatParamList != null)
		            {
		                var fp = mat.FloatParamList.FirstOrDefault(p => p != null && string.Equals(p.Name, uniformName, StringComparison.OrdinalIgnoreCase));
		                if (fp != null && fp.Values != null && fp.Values.Length > 0)
		                {
		                    baseValue = fp.Values[Math.Clamp(variationIndex, 0, fp.Values.Length - 1)];
		                }
		            }
		            if (baseValue == null && mat.IntParamList != null)
		            {
		                var ip = mat.IntParamList.FirstOrDefault(p => p != null && string.Equals(p.Name, uniformName, StringComparison.OrdinalIgnoreCase));
		                if (ip != null && ip.Values != null && ip.Values.Length > 0)
		                {
		                    baseValue = ip.Values[Math.Clamp(variationIndex, 0, ip.Values.Length - 1)];
		                }
		            }
		            if (baseValue == null && mat.Float3ParamList != null)
		            {
		                var v3p = mat.Float3ParamList.FirstOrDefault(p => p != null && string.Equals(p.Name, uniformName, StringComparison.OrdinalIgnoreCase));
		                if (v3p != null && v3p.Values != null && v3p.Values.Length > 0)
		                {
		                    var v3 = v3p.Values[Math.Clamp(variationIndex, 0, v3p.Values.Length - 1)];
		                    baseValue = new Vector3(v3.X, v3.Y, v3.Z);
		                }
		            }
		            if (baseValue == null && mat.Float4ParamList != null)
		            {
		                var v4p = mat.Float4ParamList.FirstOrDefault(p => p != null && string.Equals(p.Name, uniformName, StringComparison.OrdinalIgnoreCase));
		                if (v4p != null && v4p.Values != null && v4p.Values.Length > 0)
		                {
		                    var v4 = v4p.Values[Math.Clamp(variationIndex, 0, v4p.Values.Length - 1)];
		                    baseValue = new Vector4(v4.W, v4.X, v4.Y, v4.Z);
		                }
		            }

		            if (baseValue == null)
		            {
		                return false;
		            }

		            string key = GetMaterialMetadataValueOverrideKey(metadataParamName, mat.MaterialName, uniformName, variationIndex);
		            if (AreEquivalentMetadataValues(baseValue, value))
		            {
		                if (materialMetadataValueOverrides.Remove(key))
		                {
		                    ApplyMaterialMetadataOverridesToRuntimeMaterials();
		                    ApplyMaterialUniformOverridesToRuntimeMaterials(mat.MaterialName);
		                    return true;
		                }
		                return false;
		            }

		            materialMetadataValueOverrides[key] = new MaterialMetadataValueOverride
		            {
		                SetName = GetEffectiveMaterialSetNameForSelections(),
		                MetadataParamName = metadataParamName,
		                MaterialName = mat.MaterialName,
		                UniformName = uniformName,
		                VariationIndex = variationIndex,
		                Type = InferMetadataValueType(value),
		                Value = value
		            };

		            ApplyMaterialMetadataOverridesToRuntimeMaterials();
		            ApplyMaterialUniformOverridesToRuntimeMaterials(mat.MaterialName);
		            return true;
		        }

		        public bool ClearMaterialMetadataValueOverride(string metadataParamName, string materialName, string uniformName)
		        {
		            if (string.IsNullOrWhiteSpace(metadataParamName) ||
		                string.IsNullOrWhiteSpace(materialName) ||
		                string.IsNullOrWhiteSpace(uniformName))
		            {
		                return false;
		            }

		            var item = TryGetActiveMaterialParamMetadataItem();
		            if (item?.ParamList == null || item.ParamList.Length == 0)
		            {
		                return false;
		            }

		            var param = item.ParamList.FirstOrDefault(p => string.Equals(p?.Name, metadataParamName, StringComparison.OrdinalIgnoreCase));
		            if (param == null || !param.UseNoAnime)
		            {
		                return false;
		            }

		            int variationIndex = GetSelectedVariationIndex(param);
		            string key = GetMaterialMetadataValueOverrideKey(metadataParamName, materialName, uniformName, variationIndex);
		            if (!materialMetadataValueOverrides.Remove(key))
		            {
		                return false;
		            }

		            ApplyMaterialMetadataOverridesToRuntimeMaterials();
		            ApplyMaterialUniformOverridesToRuntimeMaterials(materialName);
		            return true;
		        }
	    }
}
