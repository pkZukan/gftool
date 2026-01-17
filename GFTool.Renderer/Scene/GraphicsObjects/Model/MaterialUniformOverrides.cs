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
	        private string? currentMaterialFilePath;
	        private string? currentMaterialSetName;
	        private string? defaultMaterialFilePath;
		        private TrmmtUnifiedMetadata? materialMetadata;
		        private string? materialMetadataPath;
		        private string? preferredMaterialMetadataPath;
		        private readonly Dictionary<string, int> materialMetadataSelections = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		        private readonly Dictionary<string, MaterialMetadataValueOverride> materialMetadataValueOverrides = new Dictionary<string, MaterialMetadataValueOverride>(StringComparer.OrdinalIgnoreCase);
		        // Persist user edits across material reloads, scoped per material set (i.e. per loaded TRMTR path).
		        // This prevents edits made in one set from leaking into another set that uses a different TRMTR.
		        private readonly Dictionary<string, Dictionary<string, Dictionary<string, object>>> materialUniformOverrideStateByMaterialFile =
		            new Dictionary<string, Dictionary<string, Dictionary<string, object>>>(StringComparer.OrdinalIgnoreCase);
		        private readonly HashSet<string> materialMetadataLastAppliedUniformNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

	        public string? CurrentMaterialSetName => currentMaterialSetName;
	        public string? CurrentMaterialFilePath => currentMaterialFilePath;
	        public string? DefaultMaterialFilePath => defaultMaterialFilePath;
		        public string? PreferredMaterialMetadataPath => preferredMaterialMetadataPath;
		        public string? LoadedMaterialMetadataPath => materialMetadataPath;
		        public bool HasMaterialMetadataSelectionOverrides => materialMetadataSelections.Count > 0;
		        public bool HasMaterialMetadataValueOverrides => materialMetadataValueOverrides.Count > 0;
		        public bool HasMaterialUniformOverrides =>
		            materialUniformOverrideStateByMaterialFile.Values.Any(byMat => byMat.Values.Any(v => v != null && v.Count > 0));

		        public KeyValuePair<string, int>[] GetMaterialMetadataSelectionsSnapshot()
		        {
		            return materialMetadataSelections.ToArray();
		        }

		        public MaterialMetadataValueOverride[] GetMaterialMetadataValueOverridesSnapshot()
		        {
		            return materialMetadataValueOverrides.Values.ToArray();
		        }

		        public bool TrySetMaterialUniformOverride(string materialName, string uniformName, object value)
		        {
		            if (string.IsNullOrWhiteSpace(materialName) || string.IsNullOrWhiteSpace(uniformName))
		            {
		                return false;
		            }

		            string scope = GetMaterialUniformOverrideScopeKey();
		            if (!materialUniformOverrideStateByMaterialFile.TryGetValue(scope, out var byMaterial))
		            {
		                byMaterial = new Dictionary<string, Dictionary<string, object>>(StringComparer.OrdinalIgnoreCase);
		                materialUniformOverrideStateByMaterialFile[scope] = byMaterial;
		            }

		            if (!byMaterial.TryGetValue(materialName, out var byUniform))
		            {
		                byUniform = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
		                byMaterial[materialName] = byUniform;
		            }

		            byUniform[uniformName] = value;
		            ApplyMaterialUniformOverridesToRuntimeMaterials(materialName);
		            return true;
		        }

		        public bool TryClearMaterialUniformOverride(string materialName, string uniformName)
		        {
		            if (string.IsNullOrWhiteSpace(materialName) || string.IsNullOrWhiteSpace(uniformName))
		            {
		                return false;
		            }

		            string scope = GetMaterialUniformOverrideScopeKey();
		            if (!materialUniformOverrideStateByMaterialFile.TryGetValue(scope, out var byMaterial) || byMaterial == null)
		            {
		                return false;
		            }

		            if (!byMaterial.TryGetValue(materialName, out var byUniform) || byUniform == null)
		            {
		                return false;
		            }

		            if (!byUniform.Remove(uniformName))
		            {
		                return false;
		            }

		            if (byUniform.Count == 0)
		            {
		                byMaterial.Remove(materialName);
		                if (byMaterial.Count == 0)
		                {
		                    materialUniformOverrideStateByMaterialFile.Remove(scope);
		                }
		            }

		            ApplyMaterialUniformOverridesToRuntimeMaterials(materialName);
		            return true;
		        }

		        private string GetMaterialUniformOverrideScopeKey()
		        {
		            if (!string.IsNullOrWhiteSpace(currentMaterialFilePath))
		            {
		                return currentMaterialFilePath!;
		            }

		            if (!string.IsNullOrWhiteSpace(currentMaterialSetName))
		            {
		                return currentMaterialSetName!;
		            }

		            return "<unknown>";
		        }

		        private void ApplyMaterialUniformOverridesToRuntimeMaterials(string? materialName = null)
		        {
		            if (materials == null || materials.Length == 0)
		            {
		                return;
		            }

		            string scope = GetMaterialUniformOverrideScopeKey();
		            if (!materialUniformOverrideStateByMaterialFile.TryGetValue(scope, out var byMaterial) || byMaterial == null || byMaterial.Count == 0)
		            {
		                return;
		            }

		            bool AnyAffectsColorTable(Dictionary<string, object>? map)
		            {
		                if (map == null || map.Count == 0)
		                {
		                    return false;
		                }

		                foreach (var key in map.Keys)
		                {
		                    if (key.StartsWith("BaseColorIndex", StringComparison.OrdinalIgnoreCase) ||
		                        string.Equals(key, "ColorTableDivideNumber", StringComparison.OrdinalIgnoreCase))
		                    {
		                        return true;
		                    }
		                }

		                return false;
		            }

		            foreach (var mat in materials)
		            {
		                if (mat == null || string.IsNullOrWhiteSpace(mat.Name))
		                {
		                    continue;
		                }

		                if (materialName != null && !string.Equals(mat.Name, materialName, StringComparison.OrdinalIgnoreCase))
		                {
		                    continue;
		                }

		                if (!byMaterial.TryGetValue(mat.Name, out var byUniform) || byUniform == null || byUniform.Count == 0)
		                {
		                    continue;
		                }

		                foreach (var kv in byUniform)
		                {
		                    mat.SetUniformOverride(kv.Key, kv.Value);
		                }

		                if (AnyAffectsColorTable(byUniform))
		                {
		                    mat.RefreshColorTableOverridesFromUniformOverrides();
		                }
		            }
		        }
	    }
}
