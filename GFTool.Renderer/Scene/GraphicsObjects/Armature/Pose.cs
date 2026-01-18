using GFTool.Renderer.Core;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Trinity.Core.Flatbuffers.TR.Model;

namespace GFTool.Renderer.Scene.GraphicsObjects
{
    public partial class Armature
    {
        public bool IsVisibleBone(int index)
        {
            var bone = Bones[index];
            return bone.Skinning || bone.Children.Count > 0;
        }

        public void ResetPose()
        {
            foreach (var bone in Bones)
            {
                bone.ResetPose();
            }
            activePoseWorld = null;
        }

        public void ApplyAnimation(Animation animation, Animation? fallbackAnimation, float frame)
        {
            if (animation == null)
            {
                return;
            }

            if (!string.Equals(lastSingularInvertAnimName, animation.Name, StringComparison.Ordinal))
            {
                lastSingularInvertAnimName = animation.Name;
                warnedSingularInvertBones.Clear();
            }

            bool probeAlloc = RenderOptions.EnablePerfSpikeLog;
            long allocStart = 0;
            long allocAfterCompute = 0;
            if (probeAlloc)
            {
                try
                {
                    allocStart = GC.GetAllocatedBytesForCurrentThread();
                }
                catch
                {
                    allocStart = 0;
                }
            }

            EnsurePoseCaches();
            var poseWorld = poseWorldCache!;
            var poseWorldNoRot = poseWorldNoRotCache!;
            var poseLocal = poseLocalCache!;
            var computed = poseComputedCache!;
            Array.Clear(computed, 0, computed.Length);

            for (int i = 0; i < Bones.Count; i++)
            {
                poseWorld[i] = ComputePoseWorld(i, animation, fallbackAnimation, frame, poseWorld, poseWorldNoRot, poseLocal, computed);
            }

            // Keep the exact pose world matrices for skinning (avoids TRS extract/rebuild artifacts).
            activePoseWorld = poseWorld;

            if (probeAlloc && allocStart != 0)
            {
                try
                {
                    allocAfterCompute = GC.GetAllocatedBytesForCurrentThread();
                }
                catch
                {
                    allocAfterCompute = 0;
                }
            }

            for (int i = 0; i < Bones.Count; i++)
            {
                var bone = Bones[i];
                var local = poseLocal[i];

                bone.Transform.Position = local.ExtractTranslation();
                bone.Transform.Rotation = local.ExtractRotation();
                bone.Transform.Scale = local.ExtractScale();
            }

            if (probeAlloc && allocStart != 0)
            {
                long allocEnd = 0;
                try
                {
                    allocEnd = GC.GetAllocatedBytesForCurrentThread();
                }
                catch
                {
                    allocEnd = 0;
                }

                if (allocAfterCompute != 0 && allocEnd != 0)
                {
                    lastAllocPoseComputeBytes = allocAfterCompute - allocStart;
                    lastAllocWriteBackBytes = allocEnd - allocAfterCompute;
                }
                else
                {
                    lastAllocPoseComputeBytes = 0;
                    lastAllocWriteBackBytes = 0;
                }
            }
            else
            {
                lastAllocPoseComputeBytes = 0;
                lastAllocWriteBackBytes = 0;
            }
        }

        public long LastAllocPoseComputeBytes => lastAllocPoseComputeBytes;
        public long LastAllocWriteBackBytes => lastAllocWriteBackBytes;

        private void EnsurePoseCaches()
        {
            int count = Bones.Count;
            if (poseWorldCache == null || poseWorldCache.Length != count)
            {
                poseWorldCache = new Matrix4[count];
            }
            if (poseWorldNoRotCache == null || poseWorldNoRotCache.Length != count)
            {
                poseWorldNoRotCache = new Matrix4[count];
            }
            if (poseLocalCache == null || poseLocalCache.Length != count)
            {
                poseLocalCache = new Matrix4[count];
            }
            if (poseComputedCache == null || poseComputedCache.Length != count)
            {
                poseComputedCache = new bool[count];
            }
        }

        private Matrix4 ComputePoseWorld(
            int index,
            Animation animation,
            Animation? fallbackAnimation,
            float frame,
            Matrix4[] poseWorld,
            Matrix4[] poseWorldNoRot,
            Matrix4[] poseLocal,
            bool[] computed)
        {
            if (computed[index])
            {
                return poseWorld[index];
            }

            var bone = Bones[index];

            // Evaluate animation tracks relative to the skeleton rest local pose.
            // Inverse bind matrices are for skinning. Using them as an animation base pose can cause
            // visible flips and offsets when tracks are missing.
            var baseLoc = bone.RestPosition;
            var baseRot = bone.RestRotation;
            var baseScale = bone.RestScale;

            Vector3? scale = null;
            Quaternion? rotation = null;
            Vector3? translation = null;
            if (!animation.TryGetPose(bone.Name, frame, out var scaleA, out var rotationA, out var translationA))
            {
                scaleA = null;
                rotationA = null;
                translationA = null;
            }

            Vector3? scaleB = null;
            Quaternion? rotationB = null;
            Vector3? translationB = null;
            if (fallbackAnimation != null && fallbackAnimation.TryGetPose(bone.Name, frame, out var s, out var r, out var t))
            {
                scaleB = s;
                rotationB = r;
                translationB = t;
            }

            // Per-channel fallback: allows accessory-only animations to override only the bones/channels they key,
            // while pulling body motion from a base animation.
            scale = scaleA ?? scaleB;
            rotation = rotationA ?? rotationB;
            translation = translationA ?? translationB;

            var loc = translation ?? baseLoc;
            var rot = rotation ?? baseRot;
            // Animation scale tracks are local scale values; applying them in world space introduces shearing/stretching.
            var localScale = scale ?? baseScale;

            var matrix = BuildLocalMatrix(localScale, rot, loc, bone.ScalePivot, bone.RotatePivot);
            var matrixNoRot = BuildLocalMatrix(localScale, Quaternion.Identity, loc, bone.ScalePivot, bone.RotatePivot);

            Matrix4 world = matrix;
            if (bone.ParentIndex >= 0 && bone.ParentIndex < Bones.Count && bone.ParentIndex != index)
            {
                var parentWorld = ComputePoseWorld(bone.ParentIndex, animation, fallbackAnimation, frame, poseWorld, poseWorldNoRot, poseLocal, computed);
                if (bone.UseSegmentScaleCompensate)
                {
                    var parent = Bones[bone.ParentIndex];
                    Matrix4 parentLocal = parentWorld;
                    if (parent.ParentIndex >= 0 && parent.ParentIndex < Bones.Count && parent.ParentIndex != bone.ParentIndex)
                    {
                        var grandParentWorld = ComputePoseWorld(parent.ParentIndex, animation, fallbackAnimation, frame, poseWorld, poseWorldNoRot, poseLocal, computed);
                        if (TryInvert(grandParentWorld, out var invGrandParent))
                        {
                            parentLocal = parentWorld * invGrandParent;
                        }
                        else
                        {
                            WarnSingularInvert(
                                context: "SegmentScaleCompensate",
                                boneIndex: index,
                                boneName: bone.Name,
                                animation: animation,
                                frame: frame,
                                detail: $"parent={parent.Name}({bone.ParentIndex}) grandParent={Bones[parent.ParentIndex].Name}({parent.ParentIndex})");
                            parentLocal = parentWorld;
                        }
                    }

                    var parentScale = parentLocal.ExtractScale();
                    matrix *= Matrix4.CreateScale(
                        parentScale.X != 0f ? 1f / parentScale.X : 1f,
                        parentScale.Y != 0f ? 1f / parentScale.Y : 1f,
                        parentScale.Z != 0f ? 1f / parentScale.Z : 1f);
                    matrixNoRot *= Matrix4.CreateScale(
                        parentScale.X != 0f ? 1f / parentScale.X : 1f,
                        parentScale.Y != 0f ? 1f / parentScale.Y : 1f,
                        parentScale.Z != 0f ? 1f / parentScale.Z : 1f);
                }
                var parentEffective = bone.IgnoreParentRotation ? poseWorldNoRot[bone.ParentIndex] : parentWorld;
                world = matrix * parentEffective;
                poseWorldNoRot[index] = matrixNoRot * parentEffective;
            }
            else
            {
                poseWorldNoRot[index] = matrixNoRot;
            }

            computed[index] = true;
            poseWorld[index] = world;
            poseLocal[index] = matrix;
            return world;
        }

        public int GetVisibleParentIndex(int index)
        {
            var parent = Bones[index].ParentIndex;
            while (parent >= 0 && parent < Bones.Count && !IsVisibleBone(parent))
            {
                parent = Bones[parent].ParentIndex;
            }
            return parent;
        }

        public int MapJointInfoIndex(int jointInfoIndex)
        {
            if (jointInfoIndex < 0 || jointInfoIndex >= jointInfoToNode.Length)
            {
                return -1;
            }

            int mapped = jointInfoToNode[jointInfoIndex];
            return mapped >= 0 ? mapped : -1;
        }

        public int GetJointInfoIndex(int boneIndex)
        {
            if (boneIndex < 0 || boneIndex >= nodeToJointInfo.Length)
            {
                return -1;
            }

            return nodeToJointInfo[boneIndex];
        }

        public int MapBoneMetaIndex(int boneMetaIndex)
        {
            // BoneMeta mapping is not present in the TRSKL schema used by this viewer.
            return 0;
        }

        public int[] BuildSkinningPalette()
        {
            if (skinningPaletteOverride != null && skinningPaletteOverride.Length > 0)
            {
                return skinningPaletteOverride;
            }

            if (jointInfoToNode == null || jointInfoToNode.Length == 0)
            {
                return Array.Empty<int>();
            }

            // Joint info index space is treated as the palette. palette[jointId] maps to a node index.
            // Dual skeleton cases use `skinning_palette_offset`, but base and local are not merged here.
            var palette = new int[jointInfoToNode.Length];
            for (int i = 0; i < palette.Length; i++)
            {
                palette[i] = jointInfoToNode[i];
            }
            return palette;
        }

    }
}
