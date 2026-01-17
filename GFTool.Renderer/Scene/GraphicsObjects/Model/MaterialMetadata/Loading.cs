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
		        private static bool AreEquivalentMetadataValues(object original, object edited)
		        {
		            const float eps = 0.00001f;

		            if (original is int oi && edited is int ei)
		            {
		                return oi == ei;
		            }
		            if (original is float of && edited is float ef)
		            {
		                return MathF.Abs(of - ef) <= eps;
		            }
		            if (original is float of2 && edited is int ei2)
		            {
		                return MathF.Abs(of2 - ei2) <= eps;
		            }
		            if (original is int oi2 && edited is float ef2)
		            {
		                return MathF.Abs(oi2 - ef2) <= eps;
		            }
		            if (original is Vector3 ov3 && edited is Vector3 ev3)
		            {
		                return MathF.Abs(ov3.X - ev3.X) <= eps &&
		                       MathF.Abs(ov3.Y - ev3.Y) <= eps &&
		                       MathF.Abs(ov3.Z - ev3.Z) <= eps;
		            }
		            if (original is Vector4 ov4 && edited is Vector4 ev4)
		            {
		                return MathF.Abs(ov4.X - ev4.X) <= eps &&
		                       MathF.Abs(ov4.Y - ev4.Y) <= eps &&
		                       MathF.Abs(ov4.Z - ev4.Z) <= eps &&
		                       MathF.Abs(ov4.W - ev4.W) <= eps;
		            }

		            return Equals(original, edited);
		        }

		        private static string InferMetadataValueType(object value)
		        {
		            return value switch
		            {
		                int => "Int",
		                float => "Float",
		                Vector3 => "Vec3",
		                Vector4 => "Vec4",
		                _ => "Unknown"
		            };
		        }

	        public bool TrySetMaterialVariantParam(string name, int selectedIndex)
	        {
	            if (string.IsNullOrWhiteSpace(name))
	            {
	                return false;
	            }

	            var item = TryGetActiveMaterialParamMetadataItem();
	            if (item?.ParamList == null || item.ParamList.Length == 0)
	            {
	                return false;
	            }

	            var param = item.ParamList.FirstOrDefault(p => string.Equals(p?.Name, name, StringComparison.OrdinalIgnoreCase));
	            if (param == null || !param.UseNoAnime)
	            {
	                return false;
	            }

	            int variationCount = param.NoAnimeParam?.VariationCount ?? 0;
	            if (variationCount <= 0)
	            {
	                return false;
	            }

	            int clamped = ClampVariationIndex(selectedIndex, variationCount);
	            materialMetadataSelections[GetMaterialMetadataSelectionKey(name)] = clamped;
	            ApplyMaterialMetadataOverridesToRuntimeMaterials();
	            ApplyMaterialUniformOverridesToRuntimeMaterials();
	            return true;
	        }

        public bool TrySetMaterialSet(string setName)
        {
            if (string.IsNullOrWhiteSpace(setName))
            {
                return false;
            }

            var metadata = TryGetMaterialMetadataForCurrentMaterials();
            if (metadata == null)
            {
                return false;
            }

            string? resolved = null;
            var candidates = metadata.EnumerateTrmtrCandidatesForSet(setName).ToArray();
            if (candidates.Length == 0)
            {
                // Param-only sets: use the model's default TRMTR when available so switching away from a TRMTR-based
                // set (e.g. tex_00/tex_01) can return to the baseline material, then apply only metadata overrides.
                if (!string.IsNullOrWhiteSpace(defaultMaterialFilePath))
                {
                    var attempt = ResolveTrmtrPath(defaultMaterialFilePath, assetProvider);
                    if (assetProvider.Exists(attempt))
                    {
                        resolved = attempt;
                    }
                }
                else if (!string.IsNullOrWhiteSpace(currentMaterialFilePath) && assetProvider.Exists(currentMaterialFilePath))
                {
                    resolved = currentMaterialFilePath;
                }
            }
            else
            {
                foreach (var candidate in candidates)
                {
                    if (string.IsNullOrWhiteSpace(candidate) || !candidate.EndsWith(".trmtr", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var attempt = ResolveMaterialPathCandidate(currentMaterialFilePath, candidate);
                    if (string.IsNullOrWhiteSpace(attempt))
                    {
                        continue;
                    }

                    attempt = ResolveTrmtrPath(attempt, assetProvider);
                    if (assetProvider.Exists(attempt))
                    {
                        resolved = attempt;
                        break;
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(resolved))
            {
                return false;
            }

            if (MessageHandler.Instance.DebugLogsEnabled)
            {
                MessageHandler.Instance.AddMessage(MessageType.LOG, $"[TRMMT] Material set '{setName}' -> '{resolved}'");
            }

            ParseMaterial(resolved, preserveMaterialMetadata: true);
            currentMaterialSetName = setName;
            ApplyMaterialMetadataOverridesToRuntimeMaterials();
            ApplyMaterialUniformOverridesToRuntimeMaterials();
            return true;
        }

	        private TrmmtUnifiedMetadata? TryGetMaterialMetadataForCurrentMaterials()
	        {
	            EnsureMaterialMetadataLoaded();
	            return materialMetadata;
	        }

	        private void EnsureMaterialMetadataLoaded()
	        {
	            if (materialMetadata != null && !string.IsNullOrWhiteSpace(materialMetadataPath))
	            {
	                if (string.IsNullOrWhiteSpace(currentMaterialSetName))
	                {
	                    currentMaterialSetName = materialMetadata.InferSetNameFromCurrentTrmtr(currentMaterialFilePath);
	                }
	                return;
	            }

	            if (!string.IsNullOrWhiteSpace(preferredMaterialMetadataPath) && assetProvider.Exists(preferredMaterialMetadataPath))
	            {
	                try
	                {
	                    if (MessageHandler.Instance.DebugLogsEnabled)
	                    {
	                        MessageHandler.Instance.AddMessage(MessageType.LOG, $"[TRMMT] Using preferred metadata '{preferredMaterialMetadataPath}'");
	                    }
	                    var bytes = assetProvider.ReadAllBytes(preferredMaterialMetadataPath);
	                    materialMetadata = TrmmtUnifiedMetadata.TryParse(
	                        preferredMaterialMetadataPath,
	                        bytes,
	                        MessageHandler.Instance.DebugLogsEnabled,
	                        msg => MessageHandler.Instance.AddMessage(MessageType.LOG, msg));
	                    materialMetadataPath = materialMetadata != null ? preferredMaterialMetadataPath : null;
	                    if (string.IsNullOrWhiteSpace(currentMaterialSetName) && materialMetadata != null)
	                    {
	                        currentMaterialSetName = materialMetadata.InferSetNameFromCurrentTrmtr(currentMaterialFilePath);
	                    }
	                    return;
	                }
	                catch
	                {
	                    materialMetadata = null;
	                    materialMetadataPath = null;
	                }
	            }
	            else if (MessageHandler.Instance.DebugLogsEnabled && !string.IsNullOrWhiteSpace(preferredMaterialMetadataPath))
	            {
	                MessageHandler.Instance.AddMessage(MessageType.LOG, $"[TRMMT] Preferred metadata missing '{preferredMaterialMetadataPath}'");
	            }

	            if (string.IsNullOrWhiteSpace(currentMaterialFilePath))
	            {
	                return;
	            }

	            var trmmtPath = Path.ChangeExtension(currentMaterialFilePath, ".trmmt");
	            if (string.IsNullOrEmpty(trmmtPath) || !assetProvider.Exists(trmmtPath))
	            {
	                if (MessageHandler.Instance.DebugLogsEnabled)
	                {
	                    MessageHandler.Instance.AddMessage(MessageType.LOG, $"[TRMMT] No metadata next to material '{currentMaterialFilePath}' (wanted '{trmmtPath}')");
	                }
	                return;
	            }

	            if (materialMetadata != null && string.Equals(materialMetadataPath, trmmtPath, StringComparison.OrdinalIgnoreCase))
	            {
	                return;
	            }

	            try
	            {
	                if (MessageHandler.Instance.DebugLogsEnabled)
	                {
	                    MessageHandler.Instance.AddMessage(MessageType.LOG, $"[TRMMT] Using material-adjacent metadata '{trmmtPath}'");
	                }
	                var bytes = assetProvider.ReadAllBytes(trmmtPath);
	                materialMetadata = TrmmtUnifiedMetadata.TryParse(
	                    trmmtPath,
	                    bytes,
	                    MessageHandler.Instance.DebugLogsEnabled,
	                    msg => MessageHandler.Instance.AddMessage(MessageType.LOG, msg));
	                materialMetadataPath = materialMetadata != null ? trmmtPath : null;
	                if (string.IsNullOrWhiteSpace(currentMaterialSetName) && materialMetadata != null)
	                {
	                    currentMaterialSetName = materialMetadata.InferSetNameFromCurrentTrmtr(currentMaterialFilePath);
	                }
	                return;
	            }
            catch
            {
                materialMetadata = null;
                materialMetadataPath = null;
	                return;
		        }
	        }

	        private static string? ResolveMaterialPathCandidate(string? currentMaterialFilePath, string candidate)
	        {
	            if (string.IsNullOrWhiteSpace(candidate))
            {
                return null;
            }

            // Treat relative paths as being relative to the currently loaded TRMTR's directory.
            if (Path.IsPathRooted(candidate))
            {
                return candidate;
            }

            if (string.IsNullOrWhiteSpace(currentMaterialFilePath))
            {
                return candidate;
            }

            var dir = Path.GetDirectoryName(currentMaterialFilePath);
            if (string.IsNullOrWhiteSpace(dir))
            {
                return candidate;
            }

            return Path.Combine(dir, candidate);
        }
	    }
}
