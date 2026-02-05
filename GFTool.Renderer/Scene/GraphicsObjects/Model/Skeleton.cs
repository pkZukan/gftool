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
        private void ParseArmature(string file)
        {
            var skel = LoadFlat<TRSKL>(file);
            var merge = assetProvider is DiskAssetProvider or Trinity.Core.Assets.OverlayDiskAssetProvider
                ? TryLoadAndMergeBaseSkeleton(skel, file, baseSkeletonCategoryHint)
                : null;
            armature = merge != null
                ? new Armature(merge.Value.Skeleton, file, merge.Value.SkinningPalette)
                : new Armature(skel, file);
            ApplyBlendIndexMapping(
                RenderOptions.MapBlendIndicesViaJointInfo,
                RenderOptions.MapBlendIndicesViaSkinningPalette,
                RenderOptions.MapBlendIndicesViaBoneMeta,
                RenderOptions.AutoMapBlendIndices);
        }

        private readonly struct MergedSkeletonResult
        {
            public TRSKL Skeleton { get; init; }
            public int[]? SkinningPalette { get; init; }
        }

        private MergedSkeletonResult? TryLoadAndMergeBaseSkeleton(TRSKL localSkel, string localSkelPath, string? category)
        {
            if (localSkel == null || string.IsNullOrWhiteSpace(localSkelPath) || string.IsNullOrWhiteSpace(category))
            {
                return null;
            }

            var localDir = Path.GetDirectoryName(localSkelPath);
            if (string.IsNullOrWhiteSpace(localDir))
            {
                return null;
            }

            try
            {
	                var basePath = ResolveBaseTrsklPath(localDir, category, localSkel);
	                if (string.IsNullOrWhiteSpace(basePath) || !File.Exists(basePath))
	                {
	                    return null;
	                }

                var baseSkel = LoadFlat<TRSKL>(basePath);
                var merged = MergeBaseAndLocalSkeletons(baseSkel, localSkel);
                int[]? palette = BuildConnectedSkinningPalette(baseSkel, localSkel);
                if (MessageHandler.Instance.DebugLogsEnabled)
                {
                    MessageHandler.Instance.AddMessage(
                        MessageType.LOG,
                        $"[TRSKL] baseMerge category={category} base='{basePath}' local='{localSkelPath}' nodes={baseSkel.TransformNodes.Length}+{localSkel.TransformNodes.Length} joints={baseSkel.JointInfos.Length}+{localSkel.JointInfos.Length}");
                }
                return new MergedSkeletonResult { Skeleton = merged, SkinningPalette = palette };
            }
            catch (Exception ex)
            {
                if (MessageHandler.Instance.DebugLogsEnabled)
                {
                    MessageHandler.Instance.AddMessage(
                        MessageType.WARNING,
                        $"[TRSKL] baseMerge failed category={category} local='{localSkelPath}': {ex.Message}");
                }
                return null;
            }
        }

        private static int CountInfluencingNodes(TRSKL skel)
        {
            if (skel.TransformNodes == null || skel.TransformNodes.Length == 0 ||
                skel.JointInfos == null || skel.JointInfos.Length == 0)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < skel.TransformNodes.Length; i++)
            {
                var node = skel.TransformNodes[i];
                if (node == null)
                {
                    continue;
                }

                int jointId = node.JointInfoIndex;
                if (jointId < 0 || jointId >= skel.JointInfos.Length)
                {
                    continue;
                }

                if (skel.JointInfos[jointId].InfluenceSkinning)
                {
                    count++;
                }
            }

            return count;
        }

	        private string? ResolveBaseTrsklPath(string modelDir, string category, TRSKL? localSkel)
	        {
            // Known base skeleton search paths (SVProtag renamed to Protag).
            string[] rels = category switch
            {
                "Protag" => new[]
                {
                    // glTF preview/export can place the base skeleton inside the output folder for self-contained roundtrips.
                    // Prefer that when available (avoids relying on ../../ traversal).
                    "model_pc_base/model/p0_base.trskl",
                    "../../model_pc_base/model/p0_base.trskl",
                    "../../../../p2/model/base/p2_base0001_00_default/p2_base0001_00_default.trskl",
                    "../../p2/p2_base0001_00_default/p2_base0001_00_default.trskl"
                },
                "CommonNPCbu" => new[] { "../../../model_cc_base/bu/bu_base.trskl", "../../base/cc_base0001_00_young_m/cc_base0001_00_young_m.trskl" },
                "CommonNPCdm" or "CommonNPCdf" => new[] { "../../../model_cc_base/dm/dm_base.trskl", "../../base/cc_base0001_00_young_m/cc_base0001_00_young_m.trskl" },
                "CommonNPCem" => new[] { "../../../model_cc_base/em/em_base.trskl", "../../base/cc_base0001_00_young_m/cc_base0001_00_young_m.trskl" },
                "CommonNPCfm" or "CommonNPCff" => new[] { "../../../model_cc_base/fm/fm_base.trskl", "../../base/cc_base0001_00_young_m/cc_base0001_00_young_m.trskl" },
                "CommonNPCgm" or "CommonNPCgf" => new[] { "../../../model_cc_base/gm/gm_base.trskl", "../../base/cc_base0001_00_young_m/cc_base0001_00_young_m.trskl" },
                "CommonNPCrv" => new[] { "../../../model_cc_base/rv/rv_base.trskl", "../../base/cc_base0001_00_young_m/cc_base0001_00_young_m.trskl" },
                _ => Array.Empty<string>()
            };

            // If the local skeleton specifies a skinning palette offset, prefer a base skeleton
            // whose influencing node count matches the expected offset. This helps player clothing
            // models that index into the connected skinning palette (indices can be > joint_info_list length).
	            int expectedOffset = localSkel?.SkinningPaletteOffset ?? -1;
            if (expectedOffset >= 0 && rels.Length > 1)
            {
                foreach (var rel in rels)
                {
                    var full = Path.GetFullPath(Path.Combine(modelDir, rel));
                    if (!File.Exists(full))
                    {
                        continue;
                    }

                    try
                    {
                        // Use flatc-parsed skeleton semantics: influencing nodes are those whose joint_info_list entry has influence_skinning=true.
                        var baseSkel = LoadFlat<TRSKL>(full);
                        int influenceCount = CountInfluencingNodes(baseSkel);
                        if (influenceCount == expectedOffset)
                        {
                            return full;
                        }
                    }
                    catch
                    {
                        // Ignore and fall back to the first existing path below.
                    }
                }
            }

            foreach (var rel in rels)
            {
                var full = Path.GetFullPath(Path.Combine(modelDir, rel));
                if (File.Exists(full))
                {
                    return full;
                }
            }

            return null;
        }

        private static int[]? BuildConnectedSkinningPalette(TRSKL baseSkel, TRSKL localSkel)
        {
            if (baseSkel.TransformNodes == null || localSkel.TransformNodes == null)
            {
                return null;
            }

            int baseNodeCount = baseSkel.TransformNodes.Length;
            int baseInfluenceCount = CountInfluencingNodes(baseSkel);
            int localInfluenceCount = CountInfluencingNodes(localSkel);
            int localStart = localSkel.SkinningPaletteOffset >= 0 ? localSkel.SkinningPaletteOffset : baseInfluenceCount;

            int paletteLen = Math.Max(baseInfluenceCount, localStart + localInfluenceCount);
            if (paletteLen <= 0)
            {
                return null;
            }

            var palette = new int[paletteLen];
            for (int i = 0; i < palette.Length; i++)
            {
                palette[i] = -1;
            }

            // Base skeleton palette indices start at 0 and are assigned in node order for influencing bones.
            if (baseSkel.JointInfos != null && baseSkel.JointInfos.Length > 0)
            {
                int skinIndex = 0;
                for (int i = 0; i < baseSkel.TransformNodes.Length; i++)
                {
                    var node = baseSkel.TransformNodes[i];
                    if (node == null)
                    {
                        continue;
                    }
                    int jointId = node.JointInfoIndex;
                    if (jointId < 0 || jointId >= baseSkel.JointInfos.Length)
                    {
                        continue;
                    }
                    if (!baseSkel.JointInfos[jointId].InfluenceSkinning)
                    {
                        continue;
                    }
                    if (skinIndex >= 0 && skinIndex < palette.Length)
                    {
                        palette[skinIndex] = i;
                    }
                    skinIndex++;
                }
            }

            // Local skeleton palette indices start at localStart (skinning_palette_offset) when provided.
            if (localSkel.JointInfos != null && localSkel.JointInfos.Length > 0)
            {
                int skinIndex = localStart;
                for (int i = 0; i < localSkel.TransformNodes.Length; i++)
                {
                    var node = localSkel.TransformNodes[i];
                    if (node == null)
                    {
                        continue;
                    }
                    int jointId = node.JointInfoIndex;
                    if (jointId < 0 || jointId >= localSkel.JointInfos.Length)
                    {
                        continue;
                    }
                    if (!localSkel.JointInfos[jointId].InfluenceSkinning)
                    {
                        continue;
                    }
                    int mergedNodeIndex = baseNodeCount + i;
                    if (skinIndex >= 0 && skinIndex < palette.Length)
                    {
                        palette[skinIndex] = mergedNodeIndex;
                    }
                    skinIndex++;
                }
            }

            return palette;
        }

        private static TRSKL MergeBaseAndLocalSkeletons(TRSKL baseSkel, TRSKL localSkel)
        {
            // Merge strategy for the TRSKL flavor with `node_list` and `joint_info_list`.
            // Local nodes and joint infos are appended after the base skeleton.
            // ParentNodeName is resolved to a base node index when present.
            // ParentNodeIndex is treated as local space and is offset by the base node count otherwise.
            // JointInfoIndex is offset by the base joint count.
            int baseNodeCount = baseSkel.TransformNodes?.Length ?? 0;
            int baseJointCount = baseSkel.JointInfos?.Length ?? 0;

            var mergedNodes = new List<TRTransformNode>(baseNodeCount + (localSkel.TransformNodes?.Length ?? 0));
            var mergedJoints = new List<TRJointInfo>(baseJointCount + (localSkel.JointInfos?.Length ?? 0));

            var baseIndexByName = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            if (baseSkel.TransformNodes != null)
            {
                for (int i = 0; i < baseSkel.TransformNodes.Length; i++)
                {
                    var n = baseSkel.TransformNodes[i];
                    mergedNodes.Add(n);
                    if (!string.IsNullOrWhiteSpace(n?.Name))
                    {
                        baseIndexByName[n.Name] = i;
                    }
                }
            }

            if (baseSkel.JointInfos != null)
            {
                mergedJoints.AddRange(baseSkel.JointInfos);
            }

            if (localSkel.JointInfos != null)
            {
                mergedJoints.AddRange(localSkel.JointInfos);
            }

            if (localSkel.TransformNodes != null)
            {
                for (int i = 0; i < localSkel.TransformNodes.Length; i++)
                {
                    var node = localSkel.TransformNodes[i];
                    if (node == null)
                    {
                        continue;
                    }

                    int parentIndex = node.ParentNodeIndex;
                    string parentName = node.ParentNodeName ?? string.Empty;
                    if (!string.IsNullOrWhiteSpace(parentName) && baseIndexByName.TryGetValue(parentName, out int baseParent))
                    {
                        parentIndex = baseParent;
                    }
                    else if (parentIndex >= 0)
                    {
                        parentIndex = parentIndex + baseNodeCount;
                    }

                    int jointIndex = node.JointInfoIndex;
                    if (jointIndex >= 0)
                    {
                        jointIndex = jointIndex + baseJointCount;
                    }

                    mergedNodes.Add(new TRTransformNode
                    {
                        Name = node.Name,
                        Transform = node.Transform,
                        ScalePivot = node.ScalePivot,
                        RotatePivot = node.RotatePivot,
                        ParentNodeIndex = parentIndex,
                        JointInfoIndex = jointIndex,
                        ParentNodeName = node.ParentNodeName,
                        Priority = node.Priority,
                        PriorityPass = node.PriorityPass,
                        IgnoreParentRotation = node.IgnoreParentRotation
                    });
                }
            }

            return new TRSKL
            {
                Version = baseSkel.Version != 0 ? baseSkel.Version : localSkel.Version,
                TransformNodes = mergedNodes.ToArray(),
                JointInfos = mergedJoints.ToArray(),
                HelperBones = baseSkel.HelperBones?.Length > 0 ? baseSkel.HelperBones : (localSkel.HelperBones ?? Array.Empty<TRHelperBoneInfo>()),
                // Preserve the local skeleton's intended palette offset when present; meshes can index into this connected palette.
                SkinningPaletteOffset = localSkel.SkinningPaletteOffset >= 0 ? localSkel.SkinningPaletteOffset : baseSkel.SkinningPaletteOffset,
                IsInteriorMap = baseSkel.IsInteriorMap || localSkel.IsInteriorMap
            };
        }

	    }
}
