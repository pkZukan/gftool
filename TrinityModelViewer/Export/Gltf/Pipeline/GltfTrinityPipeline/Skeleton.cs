using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using OpenTK.Mathematics;
using Trinity.Core.Flatbuffers.TR.Model;
using Trinity.Core.Flatbuffers.Utils;
using Trinity.Core.Utils;


namespace TrinityModelViewer.Export
{
    internal static partial class GltfTrinityPipeline
    {
        private static string? TryResolveReferenceSkeletonPath(string referenceTrmdlPath, TRMDL referenceTrmdl, string referenceDir)
        {
            string? skeletonRelPath = referenceTrmdl.Skeleton?.PathName;
            if (!string.IsNullOrWhiteSpace(skeletonRelPath))
            {
                var direct = Path.Combine(referenceDir, skeletonRelPath);
                if (File.Exists(direct))
                {
                    return direct;
                }

                // Some files store "in-game rooted" paths (e.g. starting with / or \). Try resolving them against
                // ancestors of the TRMDL folder so `ik_chara/...` layouts can still be found when a model is copied out.
                var trimmed = skeletonRelPath.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                if (!string.IsNullOrWhiteSpace(trimmed))
                {
                    var candidate = TryResolveUnderAncestors(referenceDir, trimmed, maxDepth: 8);
                    if (!string.IsNullOrWhiteSpace(candidate))
                    {
                        return candidate;
                    }
                }
            }

            // Fallback: when the TRMDL doesn't have a local skeleton file (or it isn't present next to the model),
            // attempt to locate a known base skeleton relative to the model directory.
            var category = GuessBaseSkeletonCategory(
                referenceTrmdlPath,
                referenceTrmdl.Meshes != null && referenceTrmdl.Meshes.Length > 0 ? referenceTrmdl.Meshes[0].PathName : null,
                referenceTrmdl.Skeleton?.PathName);
            if (string.IsNullOrWhiteSpace(category))
            {
                return null;
            }

            return ResolveBaseTrsklPath(referenceDir, category!, localSkel: null);
        }

        private static string? TryResolveUnderAncestors(string baseDir, string relativePath, int maxDepth)
        {
            if (string.IsNullOrWhiteSpace(baseDir) || string.IsNullOrWhiteSpace(relativePath) || maxDepth < 0)
            {
                return null;
            }

            string current = baseDir;
            for (int i = 0; i <= maxDepth; i++)
            {
                var candidate = Path.GetFullPath(Path.Combine(current, relativePath));
                if (File.Exists(candidate))
                {
                    return candidate;
                }

                var parent = Path.GetDirectoryName(current);
                if (string.IsNullOrWhiteSpace(parent))
                {
                    break;
                }
                current = parent;
            }

            return null;
        }

        private static void TryCopyProtagBaseSkeleton(string referenceTrmdlPath, TRMDL referenceTrmdl, string? localSkeletonPath, string outputDir)
        {
            if (string.IsNullOrWhiteSpace(localSkeletonPath) || !File.Exists(localSkeletonPath))
            {
                return;
            }

            var category = GuessBaseSkeletonCategory(
                referenceTrmdlPath,
                referenceTrmdl.Meshes != null && referenceTrmdl.Meshes.Length > 0 ? referenceTrmdl.Meshes[0].PathName : null,
                referenceTrmdl.Skeleton?.PathName);
            if (!string.Equals(category, "Protag", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var localSkel = FlatBufferConverter.DeserializeFrom<TRSKL>(localSkeletonPath);
            var localDir = Path.GetDirectoryName(localSkeletonPath);
            if (localSkel == null || string.IsNullOrWhiteSpace(localDir))
            {
                return;
            }

            var basePath = ResolveBaseTrsklPath(localDir, category!, localSkel);
            if (string.IsNullOrWhiteSpace(basePath) || !File.Exists(basePath))
            {
                return;
            }

            // Always copy to the self-contained preview-friendly location.
            var dst = Path.Combine(outputDir, "model_pc_base", "model", "p0_base.trskl");
            var dstDir = Path.GetDirectoryName(dst);
            if (!string.IsNullOrWhiteSpace(dstDir))
            {
                Directory.CreateDirectory(dstDir);
            }

            try
            {
                File.Copy(basePath, dst, overwrite: true);
            }
            catch
            {
                // Ignore; preview can still work when the base skeleton is discoverable via other roots.
            }
        }

        private static Dictionary<string, int> BuildBoneNameToJointInfoIndex(TRSKL? skeleton)
        {
            var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            if (skeleton?.TransformNodes == null)
            {
                return map;
            }

            foreach (var node in skeleton.TransformNodes)
            {
                if (node == null || string.IsNullOrWhiteSpace(node.Name))
                {
                    continue;
                }

                int jointInfo = node.JointInfoIndex;
                if (jointInfo < 0)
                {
                    continue;
                }

                if (!map.ContainsKey(node.Name))
                {
                    map[node.Name] = jointInfo;
                }
            }

            return map;
        }

        private static TRSKL? TryLoadMergedReferenceSkeleton(string referenceTrmdlPath, TRMDL referenceTrmdl, string referenceDir, string localSkeletonPath)
        {
            if (string.IsNullOrWhiteSpace(localSkeletonPath) || !File.Exists(localSkeletonPath))
            {
                return null;
            }

            var localSkel = FlatBufferConverter.DeserializeFrom<TRSKL>(localSkeletonPath);
            if (localSkel?.TransformNodes == null || localSkel.TransformNodes.Length == 0)
            {
                return null;
            }

            var category = GuessBaseSkeletonCategory(
                referenceTrmdlPath,
                referenceTrmdl.Meshes != null && referenceTrmdl.Meshes.Length > 0 ? referenceTrmdl.Meshes[0].PathName : null,
                referenceTrmdl.Skeleton?.PathName);
            if (string.IsNullOrWhiteSpace(category))
            {
                return null;
            }

            var localDir = Path.GetDirectoryName(localSkeletonPath);
            if (string.IsNullOrWhiteSpace(localDir))
            {
                return null;
            }

            var baseSkelPath = ResolveBaseTrsklPath(localDir, category!, localSkel);
            if (string.IsNullOrWhiteSpace(baseSkelPath) || !File.Exists(baseSkelPath))
            {
                return null;
            }

            try
            {
                var baseSkel = FlatBufferConverter.DeserializeFrom<TRSKL>(baseSkelPath);
                if (baseSkel?.TransformNodes == null || baseSkel.TransformNodes.Length == 0)
                {
                    return null;
                }

                return MergeBaseAndLocalSkeletons(baseSkel, localSkel);
            }
            catch
            {
                return null;
            }
        }

        private static string? GuessBaseSkeletonCategory(string trmdlPath, string? meshPathName, string? skeletonPathName)
        {
            static string FileNameOrEmpty(string? path) => string.IsNullOrWhiteSpace(path) ? string.Empty : Path.GetFileName(path);
            static bool StartsWithAny(string file, params string[] prefixes) =>
                prefixes.Any(p => file.StartsWith(p, StringComparison.OrdinalIgnoreCase));

            string meshFile = FileNameOrEmpty(meshPathName);
            string skelFile = FileNameOrEmpty(skeletonPathName);

            if (string.IsNullOrWhiteSpace(meshFile) && string.IsNullOrWhiteSpace(skelFile))
            {
                return null;
            }

            if (StartsWithAny(meshFile, "p0", "p1", "p2") || StartsWithAny(skelFile, "p0", "p1", "p2"))
            {
                return "Protag";
            }

            string trmdlLower = (trmdlPath ?? string.Empty).Replace('\\', '/').ToLowerInvariant();
            if (trmdlLower.Contains("/ik_chara/") && (trmdlLower.Contains("/model_pc/") || trmdlLower.Contains("/model_pc_base/")))
            {
                return "Protag";
            }

            if (meshFile.StartsWith("bu_", StringComparison.OrdinalIgnoreCase)) return "CommonNPCbu";
            if (meshFile.StartsWith("dm_", StringComparison.OrdinalIgnoreCase)) return "CommonNPCdm";
            if (meshFile.StartsWith("df_", StringComparison.OrdinalIgnoreCase)) return "CommonNPCdf";
            if (meshFile.StartsWith("em_", StringComparison.OrdinalIgnoreCase)) return "CommonNPCem";
            if (meshFile.StartsWith("fm_", StringComparison.OrdinalIgnoreCase)) return "CommonNPCfm";
            if (meshFile.StartsWith("ff_", StringComparison.OrdinalIgnoreCase)) return "CommonNPCff";
            if (meshFile.StartsWith("gm_", StringComparison.OrdinalIgnoreCase)) return "CommonNPCgm";
            if (meshFile.StartsWith("gf_", StringComparison.OrdinalIgnoreCase)) return "CommonNPCgf";
            if (meshFile.StartsWith("rv_", StringComparison.OrdinalIgnoreCase)) return "CommonNPCrv";

            return null;
        }

        private static string? ResolveBaseTrsklPath(string modelDir, string category, TRSKL? localSkel)
        {
            string[] rels = category switch
            {
                "Protag" => new[]
                {
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
                        var baseSkel = FlatBufferConverter.DeserializeFrom<TRSKL>(full);
                        int influenceCount = CountInfluencingNodes(baseSkel);
                        if (influenceCount == expectedOffset)
                        {
                            return full;
                        }
                    }
                    catch
                    {
                        // Ignore.
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

        private static TRSKL MergeBaseAndLocalSkeletons(TRSKL baseSkel, TRSKL localSkel)
        {
            int baseNodeCount = baseSkel.TransformNodes?.Length ?? 0;
            int baseJointCount = baseSkel.JointInfos?.Length ?? 0;

            var mergedNodes = new List<TRTransformNode>(baseNodeCount + (localSkel.TransformNodes?.Length ?? 0));
            var mergedJoints = new List<TRJointInfo>(baseJointCount + (localSkel.JointInfos?.Length ?? 0));

            if (baseSkel.TransformNodes != null)
            {
                mergedNodes.AddRange(baseSkel.TransformNodes);
            }
            if (baseSkel.JointInfos != null)
            {
                mergedJoints.AddRange(baseSkel.JointInfos);
            }
            if (localSkel.JointInfos != null)
            {
                mergedJoints.AddRange(localSkel.JointInfos);
            }

            var baseIndexByName = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < mergedNodes.Count; i++)
            {
                var n = mergedNodes[i];
                if (n != null && !string.IsNullOrWhiteSpace(n.Name))
                {
                    baseIndexByName[n.Name] = i;
                }
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
                SkinningPaletteOffset = localSkel.SkinningPaletteOffset >= 0 ? localSkel.SkinningPaletteOffset : baseSkel.SkinningPaletteOffset,
                IsInteriorMap = baseSkel.IsInteriorMap || localSkel.IsInteriorMap
            };
        }
    }
}
