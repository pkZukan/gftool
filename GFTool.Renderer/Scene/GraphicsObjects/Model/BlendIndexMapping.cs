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
        private void ApplyBlendIndexMapping(bool useJointInfo, bool useSkinPalette, bool useBoneMeta, bool autoMap)
        {
            if (armature == null)
            {
                return;
            }

            // When auto-mapping, build the palette even if the explicit palette toggle is off so heuristics
            // can pick it when the mesh indices are in connected skinning-palette space.
            var skinPalette = (useSkinPalette || autoMap) ? armature.BuildSkinningPalette() : Array.Empty<int>();
            blendIndexRemapModes = new BlendIndexRemapMode[BlendIndiciesOriginal.Count];

            for (int i = 0; i < BlendIndiciesOriginal.Count; i++)
            {
                var source = BlendIndiciesOriginal[i];
                var sourceWeights = i < BlendWeights.Count ? BlendWeights[i] : null;
                var boneWeights = i < BlendBoneWeights.Count ? BlendBoneWeights[i] : null;
                var mapped = new Vector4[source.Length];
                int maxIndexBefore = GetMaxIndexUsed(source, sourceWeights);

                if (MessageHandler.Instance.DebugLogsEnabled && boneWeights != null && boneWeights.Length > 0 && boneWeights.Length <= 4)
                {
                    var meshName = i < BlendMeshNames.Count ? BlendMeshNames[i] : $"Submesh {i}";
                    string entries = string.Join(", ", boneWeights.Select((bw, idx) => $"[{idx}]=({bw.RigIndex},{bw.RigWeight:0.###})"));
                    MessageHandler.Instance.AddMessage(MessageType.LOG, $"[Skin] BoneWeights mesh={meshName} len={boneWeights.Length} {entries}");
                }

                bool canRemapViaBoneWeights = false;
                if (boneWeights != null && boneWeights.Length > 0 && maxIndexBefore >= 0 && maxIndexBefore < boneWeights.Length)
                {
                    int outOfRangeWeights = 0;
                    int sampleCount = Math.Min(source.Length, 512);
                    for (int v = 0; v < sampleCount; v++)
                    {
                        var idx = source[v];
                        var w = sourceWeights != null && v < sourceWeights.Length ? sourceWeights[v] : Vector4.One;
                        CountOutOfRangeIfWeighted(boneWeights, idx.X, w.X, ref outOfRangeWeights);
                        CountOutOfRangeIfWeighted(boneWeights, idx.Y, w.Y, ref outOfRangeWeights);
                        CountOutOfRangeIfWeighted(boneWeights, idx.Z, w.Z, ref outOfRangeWeights);
                        CountOutOfRangeIfWeighted(boneWeights, idx.W, w.W, ref outOfRangeWeights);
                    }
                    canRemapViaBoneWeights = outOfRangeWeights == 0;
                }

                var mode = SelectBlendIndexRemapMode(
                    i,
                    canRemapViaBoneWeights,
                    boneWeights,
                    maxIndexBefore,
                    useJointInfo,
                    useSkinPalette,
                    useBoneMeta,
                    autoMap,
                    skinPalette);
                blendIndexRemapModes[i] = mode;

                for (int v = 0; v < source.Length; v++)
                {
                    var idx = source[v];
                    if (mode == BlendIndexRemapMode.BoneWeights && boneWeights != null)
                    {
                        idx = new Vector4(
                            MapBlendIndexComponent(idx.X, BlendIndexRemapMode.BoneWeights, boneWeights, skinPalette),
                            MapBlendIndexComponent(idx.Y, BlendIndexRemapMode.BoneWeights, boneWeights, skinPalette),
                            MapBlendIndexComponent(idx.Z, BlendIndexRemapMode.BoneWeights, boneWeights, skinPalette),
                            MapBlendIndexComponent(idx.W, BlendIndexRemapMode.BoneWeights, boneWeights, skinPalette));
                    }

                    if (mode == BlendIndexRemapMode.JointInfo)
                    {
                        mapped[v] = new Vector4(
                            (int)MathF.Round(idx.X) >= 0 && (int)MathF.Round(idx.X) < armature.JointInfoCount ? armature.MapJointInfoIndex((int)MathF.Round(idx.X)) : idx.X,
                            (int)MathF.Round(idx.Y) >= 0 && (int)MathF.Round(idx.Y) < armature.JointInfoCount ? armature.MapJointInfoIndex((int)MathF.Round(idx.Y)) : idx.Y,
                            (int)MathF.Round(idx.Z) >= 0 && (int)MathF.Round(idx.Z) < armature.JointInfoCount ? armature.MapJointInfoIndex((int)MathF.Round(idx.Z)) : idx.Z,
                            (int)MathF.Round(idx.W) >= 0 && (int)MathF.Round(idx.W) < armature.JointInfoCount ? armature.MapJointInfoIndex((int)MathF.Round(idx.W)) : idx.W);
                    }
                    else if (mode == BlendIndexRemapMode.SkinningPalette)
                    {
                        int ix = (int)MathF.Round(idx.X);
                        int iy = (int)MathF.Round(idx.Y);
                        int iz = (int)MathF.Round(idx.Z);
                        int iw = (int)MathF.Round(idx.W);
                        mapped[v] = new Vector4(
                            TryMapPalette(ix, idx.X),
                            TryMapPalette(iy, idx.Y),
                            TryMapPalette(iz, idx.Z),
                            TryMapPalette(iw, idx.W));
                    }
                    else if (mode == BlendIndexRemapMode.BoneMeta)
                    {
                        mapped[v] = new Vector4(
                            (int)MathF.Round(idx.X) >= 0 && (int)MathF.Round(idx.X) < armature.BoneMetaCount ? armature.MapBoneMetaIndex((int)MathF.Round(idx.X)) : idx.X,
                            (int)MathF.Round(idx.Y) >= 0 && (int)MathF.Round(idx.Y) < armature.BoneMetaCount ? armature.MapBoneMetaIndex((int)MathF.Round(idx.Y)) : idx.Y,
                            (int)MathF.Round(idx.Z) >= 0 && (int)MathF.Round(idx.Z) < armature.BoneMetaCount ? armature.MapBoneMetaIndex((int)MathF.Round(idx.Z)) : idx.Z,
                            (int)MathF.Round(idx.W) >= 0 && (int)MathF.Round(idx.W) < armature.BoneMetaCount ? armature.MapBoneMetaIndex((int)MathF.Round(idx.W)) : idx.W);
                    }
                    else
                    {
                        mapped[v] = idx;
                    }
                }

                BlendIndicies[i] = mapped;
                UpdateBlendIndicesBuffer(i);

                if (MessageHandler.Instance.DebugLogsEnabled)
                {
                    int maxIndexAfter = GetMaxIndexUsed(mapped, sourceWeights);
                    string meshName = i < BlendMeshNames.Count ? BlendMeshNames[i] : $"Submesh {i}";
                    MessageHandler.Instance.AddMessage(
                        MessageType.LOG,
                        $"[Skin] Remap result mesh={meshName} maxIndexAfter={maxIndexAfter}");

                    if (meshName.IndexOf("eye", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        meshName.IndexOf("mouth", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        meshName.IndexOf("teeth", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        meshName.IndexOf("tongue", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        meshName.IndexOf("face", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        meshName.IndexOf("body_mesh_shape", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        LogInfluenceSummary(meshName, mapped, sourceWeights);
                    }
                }
            }

            float TryMapPalette(int paletteIndex, float original)
            {
                if (paletteIndex < 0 || paletteIndex >= skinPalette.Length)
                {
                    return original;
                }

                int mappedIndex = skinPalette[paletteIndex];
                return mappedIndex >= 0 ? mappedIndex : original;
            }
        }

        private void LogInfluenceSummary(string meshName, Vector4[] mappedIndices, Vector4[]? weights)
        {
            if (armature == null || mappedIndices == null || mappedIndices.Length == 0)
            {
                return;
            }

            int sampleCount = Math.Min(mappedIndices.Length, weights?.Length ?? mappedIndices.Length);
            sampleCount = Math.Min(sampleCount, 2048);
            if (sampleCount <= 0)
            {
                return;
            }

            var totals = new Dictionary<int, double>();
            int invalid = 0;

            for (int v = 0; v < sampleCount; v++)
            {
                var idx = mappedIndices[v];
                var w = weights != null && v < weights.Length ? weights[v] : Vector4.One;

                Acc(idx.X, w.X);
                Acc(idx.Y, w.Y);
                Acc(idx.Z, w.Z);
                Acc(idx.W, w.W);
            }

            if (totals.Count == 0)
            {
                return;
            }

            var top = totals
                .OrderByDescending(kv => kv.Value)
                .Take(8)
                .Select(kv =>
                {
                    string name = kv.Key >= 0 && kv.Key < armature.Bones.Count ? armature.Bones[kv.Key].Name : "<out-of-range>";
                    return $"{name}({kv.Key})={kv.Value:0.###}";
                });

            MessageHandler.Instance.AddMessage(
                MessageType.LOG,
                $"[Skin] InfluenceSummary mesh={meshName} samples={sampleCount} invalid={invalid} top={string.Join(", ", top)}");

            void Acc(float indexValue, float weightValue)
            {
                if (weightValue <= 0.0001f)
                {
                    return;
                }

                int index = (int)MathF.Round(indexValue);
                if (index < 0 || index >= armature.Bones.Count)
                {
                    invalid++;
                    return;
                }

                totals.TryGetValue(index, out var current);
                totals[index] = current + weightValue;
            }
        }

        private BlendIndexRemapMode SelectBlendIndexRemapMode(
            int submeshIndex,
            bool canRemapViaBoneWeights,
            TRBoneWeight[]? boneWeights,
            int maxIndexBefore,
            bool useJointInfo,
            bool useSkinPalette,
            bool useBoneMeta,
            bool autoMap,
            int[] skinPalette)
        {
            if (armature == null)
            {
                return BlendIndexRemapMode.None;
            }

            bool canMapJointInfo = useJointInfo && armature.JointInfoCount > 0;
            // When auto-mapping (or deterministic), allow the connected skinning palette to participate even
            // if the explicit palette toggle is off. This keeps default behavior safe while enabling
            // dual-skeleton meshes that index into a connected palette to remap correctly.
            bool canMapSkinPalette = skinPalette.Length > 0 && (useSkinPalette || autoMap || RenderOptions.DeterministicSkinningAndAnimation);
            bool canMapBoneMeta = useBoneMeta && armature.BoneMetaCount > 0;

            if (RenderOptions.DeterministicSkinningAndAnimation)
            {
                // Deterministic fixed-priority mapping:
                // - Prefer joint-info index space when indices are in-range.
                // - Else prefer per-submesh boneWeight table only when indices are in-range.
                // - Else prefer skinning palette / bone meta only when indices are in-range.
                // - Otherwise keep indices as-is (node space).
                if (canMapJointInfo && maxIndexBefore >= 0 && maxIndexBefore < armature.JointInfoCount)
                {
                    return BlendIndexRemapMode.JointInfo;
                }

                if (canRemapViaBoneWeights && boneWeights != null && maxIndexBefore >= 0 && maxIndexBefore < boneWeights.Length)
                {
                    return BlendIndexRemapMode.BoneWeights;
                }

                if (canMapSkinPalette && maxIndexBefore >= 0 && maxIndexBefore < skinPalette.Length)
                {
                    return BlendIndexRemapMode.SkinningPalette;
                }

                if (canMapBoneMeta && maxIndexBefore >= 0 && maxIndexBefore < armature.BoneMetaCount)
                {
                    return BlendIndexRemapMode.BoneMeta;
                }

                return BlendIndexRemapMode.None;
            }

            if (!autoMap)
            {
                if (canRemapViaBoneWeights && boneWeights != null) return BlendIndexRemapMode.BoneWeights;
                if (canMapJointInfo) return BlendIndexRemapMode.JointInfo;
                if (canMapSkinPalette) return BlendIndexRemapMode.SkinningPalette;
                if (canMapBoneMeta) return BlendIndexRemapMode.BoneMeta;
                return BlendIndexRemapMode.None;
            }

            // Heuristic note: some meshes store blend indices in joint-info space. Bind pose can still look OK
            // regardless of mapping, so prefer joint-info as a tie breaker, but don't early-return here since it
            // can mis-map accessory meshes (e.g. glasses) whose indices are already node-indexed.
            bool preferJointInfo = false;
            if (canMapJointInfo &&
                maxIndexBefore >= 0 &&
                maxIndexBefore < armature.JointInfoCount)
            {
                bool mappingIsIdentity = true;
                int sampleMax = Math.Min(Math.Max(maxIndexBefore, 0), Math.Min(armature.JointInfoCount - 1, 96));
                for (int i = 0; i <= sampleMax; i++)
                {
                    if (armature.MapJointInfoIndex(i) != i)
                    {
                        mappingIsIdentity = false;
                        break;
                    }
                }

                preferJointInfo = !mappingIsIdentity;
            }

            int boneWeightLen = boneWeights?.Length ?? 0;
            var meshNameForHeuristics = submeshIndex < BlendMeshNames.Count ? BlendMeshNames[submeshIndex] : string.Empty;
            bool isEyeMesh = !string.IsNullOrWhiteSpace(meshNameForHeuristics) &&
                             meshNameForHeuristics.IndexOf("eye", StringComparison.OrdinalIgnoreCase) >= 0;
            bool isTongueLike = !string.IsNullOrWhiteSpace(meshNameForHeuristics) &&
                                meshNameForHeuristics.IndexOf("tongue", StringComparison.OrdinalIgnoreCase) >= 0;
            bool isGlassesLike = !string.IsNullOrWhiteSpace(meshNameForHeuristics) &&
                                 (meshNameForHeuristics.IndexOf("glasses", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                  meshNameForHeuristics.IndexOf("nose_pad", StringComparison.OrdinalIgnoreCase) >= 0);
            // Only treat small meshes as "accessories" when their blend indices appear to be indexing
            // into the small joint list (i.e., maxIndexUsed is within the joint list length).
            // Many Pokémon submeshes (arms/tails/etc) only reference a handful of joints but still use
            // joint-info index space, so they should not be excluded from joint-info remapping.
            bool looksLikeAccessory = boneWeightLen > 0 &&
                                      boneWeightLen <= 4 &&
                                      maxIndexBefore >= 0 &&
                                      maxIndexBefore < boneWeightLen &&
                                      !isEyeMesh &&
                                      !isGlassesLike;

            // Auto mode tries each applicable mapping and picks the one with the fewest
            // out of range and non influencer indices (weights ignore unused channels).
            var source = BlendIndiciesOriginal[submeshIndex];
            var weights = submeshIndex < BlendWeights.Count ? BlendWeights[submeshIndex] : null;

            // Some meshes provide a tiny boneWeight[] table (the intended influence set) but store vertex blend indices
            // directly as rig/node indices (not as 0..boneWeightLen-1). In these cases, joint-info remapping can
            // "validly" map indices (no OOR/nonInfluencer), yet still choose the wrong joints and break animation
            // (commonly seen in tongues / small mouth parts).
            bool looksLikeDirectRigIndexSpace = false;
            if (isTongueLike &&
                preferJointInfo &&
                canMapJointInfo &&
                source != null &&
                boneWeights != null &&
                boneWeights.Length > 0 &&
                boneWeights.Length <= 8)
            {
                // boneWeights[].RigIndex values often look like joint-info indices rather than node indices.
                // Compare vertex indices against the *mapped* joint-info indices inferred from boneWeights to decide
                // if the stream is already in node index space. If it is, suppress the joint-info tie-breaker.
                var expectedNodeIndexSet = new HashSet<int>();
                var rawRigIndexSet = new HashSet<int>();
                for (int i = 0; i < boneWeights.Length; i++)
                {
                    int rigIndex = boneWeights[i].RigIndex;
                    if (rigIndex < 0)
                    {
                        continue;
                    }

                    rawRigIndexSet.Add(rigIndex);
                    if (rigIndex < armature.JointInfoCount)
                    {
                        int mapped = armature.MapJointInfoIndex(rigIndex);
                        if (mapped >= 0)
                        {
                            expectedNodeIndexSet.Add(mapped);
                        }
                    }
                }

                if (expectedNodeIndexSet.Count > 0)
                {
                    int hit = 0;
                    int total = 0;
                    int sampleCount = Math.Min(source.Length, 1024);
                    for (int v = 0; v < sampleCount; v++)
                    {
                        var idx = source[v];
                        var w = weights != null && v < weights.Length ? weights[v] : Vector4.One;
                        Acc(idx.X, w.X);
                        Acc(idx.Y, w.Y);
                        Acc(idx.Z, w.Z);
                        Acc(idx.W, w.W);
                    }

                    float ratio = total > 0 ? (float)hit / total : 0.0f;
                    looksLikeDirectRigIndexSpace = ratio >= 0.80f;

                    if (MessageHandler.Instance.DebugLogsEnabled && looksLikeDirectRigIndexSpace)
                    {
                        MessageHandler.Instance.AddMessage(
                            MessageType.LOG,
                            $"[Skin] Remap hint mesh={meshNameForHeuristics} directRigIndexSpace=True hitRatio={ratio:0.###} expectedNodes=[{string.Join(",", expectedNodeIndexSet.OrderBy(x => x))}] rawRig=[{string.Join(",", rawRigIndexSet.OrderBy(x => x))}]");
                    }

                    void Acc(float indexValue, float weightValue)
                    {
                        if (weightValue <= 0.0001f)
                        {
                            return;
                        }

                        total++;
                        int index = (int)MathF.Round(indexValue);
                        if (expectedNodeIndexSet.Contains(index))
                        {
                            hit++;
                        }
                    }
                }
            }
            if (preferJointInfo &&
                !isGlassesLike &&
                !string.IsNullOrWhiteSpace(Name) &&
                Name.StartsWith("pm", StringComparison.OrdinalIgnoreCase))
            {
                // Pokémon meshes frequently have very small joint sets (1-4) but still store indices in joint-info space.
                // Treat them as normal skinned meshes so tie-breakers can prefer joint-info remapping.
                looksLikeAccessory = false;
            }

            (int outOfRange, int nonInfluencer) bestScore = ScoreBlendIndexMapping(source, weights, BlendIndexRemapMode.None, boneWeights, skinPalette);
            BlendIndexRemapMode bestMode = BlendIndexRemapMode.None;

            void consider(BlendIndexRemapMode candidate)
            {
                var score = ScoreBlendIndexMapping(source, weights, candidate, boneWeights, skinPalette);
                if (score.outOfRange < bestScore.outOfRange ||
                    (score.outOfRange == bestScore.outOfRange && score.nonInfluencer < bestScore.nonInfluencer))
                {
                    bestScore = score;
                    bestMode = candidate;
                    return;
                }

                // If scores are identical, apply a small set of tie-breakers.
                // Most Pokémon meshes store indices in joint-info index space even if they only use a few joints.
                // In those cases, preferring joint-info prevents remapping via a per-submesh boneWeight table that
                // may represent something else (and can "look fine" in bind pose but break facial animation).
                if (score == bestScore)
                {
                    if (preferJointInfo &&
                        !looksLikeAccessory &&
                        candidate == BlendIndexRemapMode.JointInfo &&
                        bestMode == BlendIndexRemapMode.BoneWeights)
                    {
                        bestMode = candidate;
                    }
                    else if (looksLikeAccessory &&
                             candidate == BlendIndexRemapMode.BoneWeights &&
                             bestMode == BlendIndexRemapMode.JointInfo)
                    {
                        bestMode = candidate;
                    }
                }
            }

            if (canRemapViaBoneWeights && boneWeights != null) consider(BlendIndexRemapMode.BoneWeights);
            if (canMapJointInfo) consider(BlendIndexRemapMode.JointInfo);
            if (canMapSkinPalette) consider(BlendIndexRemapMode.SkinningPalette);
            if (canMapBoneMeta) consider(BlendIndexRemapMode.BoneMeta);

            // If the mesh has a boneWeight[] table and the observed indices fit inside it,
            // prefer that mapping when it scores identically. This avoids "perfect tie" cases
            // where joint-info remapping can incorrectly reinterpret already-correct rig indices.
            if (bestMode != BlendIndexRemapMode.BoneWeights &&
                canRemapViaBoneWeights &&
                boneWeights != null &&
                maxIndexBefore >= 0 &&
                maxIndexBefore < boneWeights.Length)
            {
                var boneWeightScore = ScoreBlendIndexMapping(source, weights, BlendIndexRemapMode.BoneWeights, boneWeights, skinPalette);
                if (boneWeightScore == bestScore)
                {
                    // If we already determined this mesh likely uses joint-info index space, don't force BoneWeights.
                    // Facial meshes can have valid-looking indices that still map to the wrong joints via the table.
                    if (!(preferJointInfo && !looksLikeAccessory))
                    {
                        bestMode = BlendIndexRemapMode.BoneWeights;
                    }
                }
            }

            // Tie breaker prefers mappings over None when scores are identical, since "None" can
            // look correct in bind pose even if indices are in the wrong index space.
            if (bestMode == BlendIndexRemapMode.None)
            {
                var jointScore = canMapJointInfo ? ScoreBlendIndexMapping(source, weights, BlendIndexRemapMode.JointInfo, boneWeights, skinPalette) : (int.MaxValue, int.MaxValue);
                if (preferJointInfo && canMapJointInfo && !looksLikeAccessory && !looksLikeDirectRigIndexSpace && jointScore == bestScore)
                {
                    bestMode = BlendIndexRemapMode.JointInfo;
                }
                else if (canMapSkinPalette)
                {
                    var palScore = ScoreBlendIndexMapping(source, weights, BlendIndexRemapMode.SkinningPalette, boneWeights, skinPalette);
                    if (palScore == bestScore)
                    {
                        bestMode = BlendIndexRemapMode.SkinningPalette;
                    }
                }

                // If scores can't distinguish, fall back to joint-info for "full" skinned meshes.
                // Small accessory meshes (often with 0-2 boneWeight entries) may already be in node index space
                // and should stay in None mode unless we have a strong reason to remap.
                if (bestMode == BlendIndexRemapMode.None && preferJointInfo && canMapJointInfo && !looksLikeAccessory && !looksLikeDirectRigIndexSpace)
                {
                    bestMode = BlendIndexRemapMode.JointInfo;
                }
            }

            if (MessageHandler.Instance.DebugLogsEnabled)
            {
                string meshName = submeshIndex < BlendMeshNames.Count ? BlendMeshNames[submeshIndex] : $"Submesh {submeshIndex}";
                MessageHandler.Instance.AddMessage(
                    MessageType.LOG,
                    $"[Skin] Remap pick mesh={meshName} maxIndexUsed={maxIndexBefore} boneWeights={(boneWeights?.Length ?? 0)} jointInfo={armature.JointInfoCount} palette={skinPalette.Length} boneMeta={armature.BoneMetaCount} mode={bestMode} score=(oor={bestScore.outOfRange}, nonInfluencer={bestScore.nonInfluencer}) preferJointInfo={preferJointInfo}");
            }

            return bestMode;
        }

        private (int outOfRange, int nonInfluencer) ScoreBlendIndexMapping(
            Vector4[] indices,
            Vector4[]? weights,
            BlendIndexRemapMode mode,
            TRBoneWeight[]? boneWeights,
            int[] skinPalette)
        {
            if (armature == null || indices == null || indices.Length == 0)
            {
                return (0, 0);
            }

            int outOfRange = 0;
            int nonInfluencer = 0;
            int sampleCount = Math.Min(indices.Length, 2048);

            for (int v = 0; v < sampleCount; v++)
            {
                var idx = indices[v];
                var w = weights != null && v < weights.Length ? weights[v] : Vector4.One;

                ScoreComponent(idx.X, w.X);
                ScoreComponent(idx.Y, w.Y);
                ScoreComponent(idx.Z, w.Z);
                ScoreComponent(idx.W, w.W);
            }

            return (outOfRange, nonInfluencer);

            void ScoreComponent(float value, float weight)
            {
                if (weight <= 0.0001f)
                {
                    return;
                }

                int mapped = MapBlendIndexComponent(value, mode, boneWeights, skinPalette);
                if (mapped < 0 || mapped >= armature.Bones.Count)
                {
                    outOfRange++;
                    return;
                }

                if (!armature.Bones[mapped].Skinning)
                {
                    nonInfluencer++;
                }
            }
        }

        private int MapBlendIndexComponent(float value, BlendIndexRemapMode mode, TRBoneWeight[]? boneWeights, int[] skinPalette)
        {
            if (armature == null)
            {
                return 0;
            }

            int index = (int)MathF.Round(value);
            if (index < 0)
            {
                return index;
            }

            switch (mode)
            {
                case BlendIndexRemapMode.BoneWeights:
                    if (boneWeights == null || index >= boneWeights.Length)
                    {
                        return index;
                    }
                    {
                        int rigIndex = boneWeights[index].RigIndex;
                        // Map them through the joint-info table when available so animation deforms the expected major bones.
                        if (shaderGame == ShaderGame.ZA &&
                            rigIndex >= 0 &&
                            rigIndex < armature.JointInfoCount)
                        {
                            int mapped = armature.MapJointInfoIndex(rigIndex);
                            if (mapped >= 0)
                            {
                                return mapped;
                            }
                        }
                        return rigIndex;
                    }
                case BlendIndexRemapMode.JointInfo:
                    if (index >= armature.JointInfoCount)
                    {
                        return index;
                    }
                    return armature.MapJointInfoIndex(index);
                case BlendIndexRemapMode.SkinningPalette:
                    if (skinPalette == null || index >= skinPalette.Length)
                    {
                        return index;
                    }
                    {
                        int mapped = skinPalette[index];
                        // Connected palettes can have unmapped slots (-1). Keep the original palette index in that case.
                        return mapped >= 0 ? mapped : index;
                    }
                case BlendIndexRemapMode.BoneMeta:
                    if (index >= armature.BoneMetaCount)
                    {
                        return index;
                    }
                    return armature.MapBoneMetaIndex(index);
                default:
                    return index;
            }
        }

        private void UpdateBlendIndicesBuffer(int submeshIndex)
        {
            if (VBOs == null || submeshIndex < 0 || submeshIndex >= VBOs.Length ||
                blendIndexOffsets == null || blendIndexByteSizes == null ||
                submeshIndex >= blendIndexOffsets.Length || submeshIndex >= blendIndexByteSizes.Length)
            {
                return;
            }

            var indices = BlendIndicies[submeshIndex];
            GL.BindBuffer(BufferTarget.ArrayBuffer, VBOs[submeshIndex]);
            GL.BufferSubData(BufferTarget.ArrayBuffer, (IntPtr)blendIndexOffsets[submeshIndex], blendIndexByteSizes[submeshIndex], ToUnmanagedByteArray(indices));
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        }

        private static int GetMaxIndex(Vector4[] indices)
        {
            int maxIndex = 0;
            for (int v = 0; v < indices.Length; v++)
            {
                var idx = indices[v];
                maxIndex = Math.Max(maxIndex, (int)MathF.Max(MathF.Max(idx.X, idx.Y), MathF.Max(idx.Z, idx.W)));
            }
            return maxIndex;
        }

        private static int GetMaxIndexUsed(Vector4[] indices, Vector4[]? weights)
        {
            if (indices == null || indices.Length == 0)
            {
                return 0;
            }

            if (weights == null || weights.Length == 0)
            {
                return GetMaxIndex(indices);
            }

            int maxIndex = 0;
            int count = Math.Min(indices.Length, weights.Length);
            for (int v = 0; v < count; v++)
            {
                var idx = indices[v];
                var w = weights[v];
                if (w.X > 0.0001f) maxIndex = Math.Max(maxIndex, (int)MathF.Round(idx.X));
                if (w.Y > 0.0001f) maxIndex = Math.Max(maxIndex, (int)MathF.Round(idx.Y));
                if (w.Z > 0.0001f) maxIndex = Math.Max(maxIndex, (int)MathF.Round(idx.Z));
                if (w.W > 0.0001f) maxIndex = Math.Max(maxIndex, (int)MathF.Round(idx.W));
            }

            return maxIndex;
        }

        private static void CountOutOfRange(TRBoneWeight[] boneWeights, int index, ref int outOfRange)
        {
            if (index < 0 || index >= boneWeights.Length)
            {
                outOfRange++;
            }
        }

        private static void CountOutOfRangeIfWeighted(TRBoneWeight[] boneWeights, float indexValue, float weight, ref int outOfRange)
        {
            if (weight <= 0.0001f)
            {
                return;
            }

            CountOutOfRange(boneWeights, (int)MathF.Round(indexValue), ref outOfRange);
        }
	    }
}
