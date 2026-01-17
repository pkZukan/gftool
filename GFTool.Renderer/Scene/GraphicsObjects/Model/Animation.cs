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
	        public void ApplyAnimation(Animation animation, float frame, Animation? fallbackAnimation = null)
	        {
	            if (animation != null &&
	                !string.Equals(lastAnimSkinDebugName, animation.Name, StringComparison.OrdinalIgnoreCase))
	            {
	                lastAnimSkinDebugName = animation.Name;
	                if (!RenderOptions.DeterministicSkinningAndAnimation)
	                {
	                    TryImproveBlendIndexRemapForAnimation(animation);
	                }
	                if (MessageHandler.Instance.DebugLogsEnabled)
	                {
	                    LogSkinningDebugForAnimation(animation);
	                }
	            }

	            var effectiveArmature = GetEffectiveArmature();
	            effectiveArmature?.ApplyAnimation(animation, fallbackAnimation, frame);
            if (RenderOptions.EnablePerfSpikeLog && effectiveArmature != null)
            {
                lastAnimAllocPoseComputeBytes = effectiveArmature.LastAllocPoseComputeBytes;
                lastAnimAllocWriteBackBytes = effectiveArmature.LastAllocWriteBackBytes;
            }
            else
            {
                lastAnimAllocPoseComputeBytes = 0;
                lastAnimAllocWriteBackBytes = 0;
	            }
	        }

	        public long LastAnimAllocPoseComputeBytes => lastAnimAllocPoseComputeBytes;
	        public long LastAnimAllocWriteBackBytes => lastAnimAllocWriteBackBytes;

	        private void TryImproveBlendIndexRemapForAnimation(Animation animation)
	        {
	            if (animation == null ||
	                armature == null ||
	                blendIndexRemapModes == null ||
	                BlendIndiciesOriginal == null ||
	                !RenderOptions.AutoMapBlendIndices)
	            {
	                return;
	            }

	            // Only adjust mappings when the current mapping yields almost no animated influences, but an alternate
	            // mapping (with equal OOR/nonInfluencer score) produces significantly more bones that have tracks.
	            // This is intended to be a safe tie-breaker and should not change already-correct models.
	            const float minAnimatedInfluenceRatioToAccept = 0.03f;
	            const float minAnimatedInfluenceRatioGain = 0.10f;
	            const float minAnimatedInfluenceGainFactor = 2.0f;

	            // Consider bones animated if they have a track, OR if any ancestor has a track (parent motion affects children).
	            // This reduces false "0 influence" detections where only a parent bone is keyed.
	            var animatedBones = BuildAnimatedBoneMask(animation, armature);

	            var skinPalette = armature.BuildSkinningPalette();
	            int submeshCount = Math.Min(blendIndexRemapModes.Length, BlendIndiciesOriginal.Count);
	            for (int submeshIndex = 0; submeshIndex < submeshCount; submeshIndex++)
	            {
	                if (submeshIndex >= HasSkinning.Count || !HasSkinning[submeshIndex])
	                {
	                    continue;
	                }

	                var source = BlendIndiciesOriginal[submeshIndex];
	                if (source == null || source.Length == 0)
	                {
	                    continue;
	                }

	                var weights = submeshIndex < BlendWeights.Count ? BlendWeights[submeshIndex] : null;
	                var boneWeights = submeshIndex < BlendBoneWeights.Count ? BlendBoneWeights[submeshIndex] : null;

	                int maxIndexUsed = GetMaxIndexUsed(source, weights);
	                bool canRemapViaBoneWeights = boneWeights != null &&
	                                              boneWeights.Length > 0 &&
	                                              maxIndexUsed >= 0 &&
	                                              maxIndexUsed < boneWeights.Length;
	                bool canMapJointInfo = armature.JointInfoCount > 0;
	                bool canMapSkinPalette = skinPalette.Length > 0;
	                bool canMapBoneMeta = armature.BoneMetaCount > 0;

	                var currentMode = blendIndexRemapModes[submeshIndex];
	                var currentScore = ScoreBlendIndexMapping(source, weights, currentMode, boneWeights, skinPalette);
	                float currentAnimated = ScoreAnimatedInfluence(source, weights, currentMode, boneWeights, skinPalette, animatedBones);
	                float currentTotal = ScoreTotalInfluence(weights, source.Length);
	                float currentRatio = currentTotal > 0.0001f ? currentAnimated / currentTotal : 0.0f;

	                if (currentRatio >= minAnimatedInfluenceRatioToAccept)
	                {
	                    continue;
	                }

	                BlendIndexRemapMode bestMode = currentMode;
	                float bestAnimated = currentAnimated;
	                float bestRatio = currentRatio;

	                void Consider(BlendIndexRemapMode mode, bool enabled)
	                {
	                    if (!enabled)
	                    {
	                        return;
	                    }

	                    var score = ScoreBlendIndexMapping(source, weights, mode, boneWeights, skinPalette);
	                    if (score != currentScore)
	                    {
	                        return;
	                    }

	                    float animated = ScoreAnimatedInfluence(source, weights, mode, boneWeights, skinPalette, animatedBones);
	                    float ratio = currentTotal > 0.0001f ? animated / currentTotal : 0.0f;
	                    if (ratio > bestRatio)
	                    {
	                        bestMode = mode;
	                        bestAnimated = animated;
	                        bestRatio = ratio;
	                    }
	                }

	                Consider(BlendIndexRemapMode.None, true);
	                Consider(BlendIndexRemapMode.BoneWeights, canRemapViaBoneWeights);
	                Consider(BlendIndexRemapMode.JointInfo, canMapJointInfo);
	                Consider(BlendIndexRemapMode.SkinningPalette, canMapSkinPalette);
	                Consider(BlendIndexRemapMode.BoneMeta, canMapBoneMeta);

	                if (bestMode == currentMode)
	                {
	                    continue;
	                }

	                float ratioGain = bestRatio - currentRatio;
	                float gainFactor = currentAnimated > 0.0001f ? bestAnimated / currentAnimated : float.PositiveInfinity;
	                if (ratioGain < minAnimatedInfluenceRatioGain || gainFactor < minAnimatedInfluenceGainFactor)
	                {
	                    continue;
	                }

	                // Avoid aggressive remaps on facial meshes; if these break they are very obvious.
	                string meshName = submeshIndex < BlendMeshNames.Count ? BlendMeshNames[submeshIndex] : string.Empty;
	                if (!string.IsNullOrWhiteSpace(meshName) &&
	                    (meshName.IndexOf("face", StringComparison.OrdinalIgnoreCase) >= 0 ||
	                     meshName.IndexOf("eye", StringComparison.OrdinalIgnoreCase) >= 0 ||
	                     meshName.IndexOf("mouth", StringComparison.OrdinalIgnoreCase) >= 0 ||
	                     meshName.IndexOf("teeth", StringComparison.OrdinalIgnoreCase) >= 0 ||
	                     meshName.IndexOf("tongue", StringComparison.OrdinalIgnoreCase) >= 0))
	                {
	                    continue;
	                }

	                blendIndexRemapModes[submeshIndex] = bestMode;
	                ApplyBlendIndexRemapForSubmesh(submeshIndex, bestMode, boneWeights, skinPalette);

	                if (MessageHandler.Instance.DebugLogsEnabled)
	                {
	                    MessageHandler.Instance.AddMessage(
	                        MessageType.LOG,
	                        $"[SkinAnim] Remap adjusted mesh={meshName} anim='{animation.Name}' mode={currentMode}->{bestMode} animatedRatio={currentRatio:0.###}->{bestRatio:0.###}");
	                }
	            }
	        }

	        private static bool[] BuildAnimatedBoneMask(Animation animation, Armature armature)
	        {
	            int boneCount = armature.Bones.Count;
	            var animated = new bool[boneCount];
	            var queue = new Queue<int>();
	            var indexByBone = new Dictionary<Armature.Bone, int>(boneCount);

	            for (int i = 0; i < boneCount; i++)
	            {
	                indexByBone[armature.Bones[i]] = i;
	                var name = armature.Bones[i].Name;
	                if (string.IsNullOrWhiteSpace(name))
	                {
	                    continue;
	                }

	                if (animation.HasTrack(name))
	                {
	                    animated[i] = true;
	                    queue.Enqueue(i);
	                }
	            }

	            // Propagate "animated" to descendants so meshes bound to non-keyed child bones still count as animated.
	            while (queue.Count > 0)
	            {
	                int parent = queue.Dequeue();
	                var children = armature.Bones[parent].Children;
	                if (children == null)
	                {
	                    continue;
	                }

	                for (int c = 0; c < children.Count; c++)
	                {
	                    var childBone = children[c];
	                    if (childBone == null || !indexByBone.TryGetValue(childBone, out int child) || animated[child])
	                    {
	                        continue;
	                    }

	                    animated[child] = true;
	                    queue.Enqueue(child);
	                }
	            }

	            return animated;
	        }

	        private void ApplyBlendIndexRemapForSubmesh(int submeshIndex, BlendIndexRemapMode mode, TRBoneWeight[]? boneWeights, int[] skinPalette)
	        {
	            if (armature == null ||
	                submeshIndex < 0 ||
	                submeshIndex >= BlendIndiciesOriginal.Count)
		            {
		                return;
		            }

	            var source = BlendIndiciesOriginal[submeshIndex];
	            if (source == null || source.Length == 0)
	            {
	                return;
	            }

	            var mapped = new Vector4[source.Length];
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

	            BlendIndicies[submeshIndex] = mapped;
	            UpdateBlendIndicesBuffer(submeshIndex);

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

	        private float ScoreAnimatedInfluence(
	            Vector4[] indices,
	            Vector4[]? weights,
	            BlendIndexRemapMode mode,
	            TRBoneWeight[]? boneWeights,
	            int[] skinPalette,
	            bool[] animatedBones)
	        {
	            if (armature == null || indices == null || indices.Length == 0)
	            {
	                return 0.0f;
	            }

	            float sum = 0.0f;
	            int sampleCount = Math.Min(indices.Length, 2048);
	            for (int v = 0; v < sampleCount; v++)
	            {
	                var idx = indices[v];
	                var w = weights != null && v < weights.Length ? weights[v] : Vector4.One;
	                Acc(idx.X, w.X);
	                Acc(idx.Y, w.Y);
	                Acc(idx.Z, w.Z);
	                Acc(idx.W, w.W);
	            }
	            return sum;

	            void Acc(float indexValue, float weightValue)
	            {
	                if (weightValue <= 0.0001f)
	                {
	                    return;
	                }

	                int mapped = MapBlendIndexComponent(indexValue, mode, boneWeights, skinPalette);
	                if (mapped < 0 || mapped >= armature.Bones.Count)
	                {
	                    return;
	                }

	                if (mapped >= 0 && mapped < animatedBones.Length && animatedBones[mapped])
	                {
	                    sum += weightValue;
	                }
	            }
	        }

	        private static float ScoreTotalInfluence(Vector4[]? weights, int vertexCount)
	        {
	            int sampleCount = Math.Min(vertexCount, 2048);
	            float sum = 0.0f;
	            for (int v = 0; v < sampleCount; v++)
	            {
	                var w = weights != null && v < weights.Length ? weights[v] : Vector4.One;
	                sum += w.X + w.Y + w.Z + w.W;
	            }
	            return sum;
	        }

	        private void LogSkinningDebugForAnimation(Animation animation)
	        {
	            var effectiveArmature = GetEffectiveArmature();
	            if (effectiveArmature == null || BlendIndicies.Count == 0)
            {
                return;
            }

            for (int submeshIndex = 0; submeshIndex < BlendIndicies.Count; submeshIndex++)
            {
                string meshName = submeshIndex < BlendMeshNames.Count ? BlendMeshNames[submeshIndex] : string.Empty;
                if (string.IsNullOrWhiteSpace(meshName))
                {
                    continue;
                }

                if (meshName.IndexOf("glasses", StringComparison.OrdinalIgnoreCase) < 0 &&
                    meshName.IndexOf("eye_default", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                int drivingIndex = FindPrimaryInfluenceIndex(submeshIndex);
                string drivingBoneName = drivingIndex >= 0 && drivingIndex < effectiveArmature.Bones.Count
                    ? effectiveArmature.Bones[drivingIndex].Name
                    : $"<idx:{drivingIndex}>";

                bool hasTrack = drivingIndex >= 0 && drivingIndex < effectiveArmature.Bones.Count &&
                                animation.HasTrack(effectiveArmature.Bones[drivingIndex].Name);

                var mode = blendIndexRemapModes != null && submeshIndex < blendIndexRemapModes.Length
                    ? blendIndexRemapModes[submeshIndex]
                    : BlendIndexRemapMode.None;

                MessageHandler.Instance.AddMessage(
                    MessageType.LOG,
                    $"[AnimSkin] anim='{animation.Name}' mesh={meshName} submesh={submeshIndex} mode={mode} driveBone={drivingBoneName} hasTrack={hasTrack}");

                if (!hasTrack && meshName.IndexOf("glasses", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    var candidates = animation.TrackNames
                        .Where(n => !string.IsNullOrWhiteSpace(n) &&
                                    (n.IndexOf("glass", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                     n.IndexOf("look", StringComparison.OrdinalIgnoreCase) >= 0))
                        .Take(12)
                        .ToArray();

                    if (candidates.Length > 0)
                    {
                        MessageHandler.Instance.AddMessage(
                            MessageType.LOG,
                            $"[AnimSkin] anim='{animation.Name}' glassesRelatedTracks={string.Join(", ", candidates)}");
                    }
                }
            }
        }

        private int FindPrimaryInfluenceIndex(int submeshIndex)
        {
            if (submeshIndex < 0 || submeshIndex >= BlendIndicies.Count)
            {
                return -1;
            }

            var indices = BlendIndicies[submeshIndex];
            var weights = submeshIndex < BlendWeights.Count ? BlendWeights[submeshIndex] : null;
            if (indices == null || indices.Length == 0)
            {
                return -1;
            }

            // Sample a limited number of verts and pick the most common "strongest influence" bone.
            var counts = new Dictionary<int, int>();
            int sampleCount = Math.Min(indices.Length, 256);
            for (int v = 0; v < sampleCount; v++)
            {
                var idx = indices[v];
                var w = weights != null && v < weights.Length ? weights[v] : Vector4.One;

                float maxW = w.X;
                float idxF = idx.X;

                if (w.Y > maxW) { maxW = w.Y; idxF = idx.Y; }
                if (w.Z > maxW) { maxW = w.Z; idxF = idx.Z; }
                if (w.W > maxW) { maxW = w.W; idxF = idx.W; }

                if (maxW <= 0.0001f)
                {
                    continue;
                }

                int boneIndex = (int)MathF.Round(idxF);
                counts.TryGetValue(boneIndex, out int curr);
                counts[boneIndex] = curr + 1;
            }

            int bestIndex = -1;
            int bestCount = -1;
            foreach (var kv in counts)
            {
                if (kv.Value > bestCount)
                {
                    bestCount = kv.Value;
                    bestIndex = kv.Key;
                }
            }

            return bestIndex;
        }
	    }
}
