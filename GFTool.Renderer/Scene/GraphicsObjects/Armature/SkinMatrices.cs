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
        public Matrix4[] GetSkinMatrices(int maxBones)
        {
            int boneCount = Math.Min(Bones.Count, maxBones);
            var world = GetSkinWorldMatrices();
            EnsureSkinMatrixCache(ref skinMatricesCache, maxBones);
            var result = skinMatricesCache!;
            for (int i = 0; i < boneCount; i++)
            {
                // Row vector math: v' = v * invBind * poseWorld. Matrices are transposed on upload for GLSL.
                var bone = Bones[i];
                var invBind = GetActiveInverseBind(bone);
                result[i] = invBind * world[i];
            }
            for (int i = boneCount; i < maxBones; i++)
            {
                result[i] = Matrix4.Identity;
            }
            return result;
        }

        public Matrix4[] GetSkinMatricesForPalette(int[] palette, int maxBones, out int boneCount)
        {
            boneCount = 0;
            if (palette == null || palette.Length == 0)
            {
                EnsureSkinMatrixCache(ref paletteSkinMatricesCache, maxBones);
                var empty = paletteSkinMatricesCache!;
                for (int i = 0; i < maxBones; i++)
                {
                    empty[i] = Matrix4.Identity;
                }
                return empty;
            }

            var world = GetSkinWorldMatrices();
            boneCount = Math.Min(palette.Length, maxBones);
            EnsureSkinMatrixCache(ref paletteSkinMatricesCache, maxBones);
            var result = paletteSkinMatricesCache!;
            for (int i = 0; i < boneCount; i++)
            {
                int boneIndex = palette[i];
                if (boneIndex < 0 || boneIndex >= Bones.Count)
                {
                    result[i] = Matrix4.Identity;
                }
                else
                {
                    var bone = Bones[boneIndex];
                    var invBind = GetActiveInverseBind(bone);
                    result[i] = invBind * world[boneIndex];
                }
            }
            for (int i = boneCount; i < maxBones; i++)
            {
                result[i] = Matrix4.Identity;
            }
            return result;
        }

        public Matrix4[] GetSkinMatricesForJointInfo(int maxBones, out int boneCount)
        {
            boneCount = 0;
            if (jointInfoToNode == null || jointInfoToNode.Length == 0)
            {
                EnsureSkinMatrixCache(ref jointInfoSkinMatricesCache, maxBones);
                var empty = jointInfoSkinMatricesCache!;
                for (int i = 0; i < maxBones; i++)
                {
                    empty[i] = Matrix4.Identity;
                }
                return empty;
            }

            var world = GetSkinWorldMatrices();
            boneCount = Math.Min(jointInfoToNode.Length, maxBones);
            EnsureSkinMatrixCache(ref jointInfoSkinMatricesCache, maxBones);
            var result = jointInfoSkinMatricesCache!;
            for (int i = 0; i < boneCount; i++)
            {
                int boneIndex = jointInfoToNode[i];
                if (boneIndex < 0 || boneIndex >= Bones.Count)
                {
                    result[i] = Matrix4.Identity;
                    continue;
                }

                var bone = Bones[boneIndex];
                var invBind = GetActiveInverseBind(bone);
                result[i] = invBind * world[boneIndex];
            }
            for (int i = boneCount; i < maxBones; i++)
            {
                result[i] = Matrix4.Identity;
            }
            return result;
        }

        private static void EnsureSkinMatrixCache(ref Matrix4[]? cache, int maxBones)
        {
            if (cache == null || cache.Length != maxBones)
            {
                cache = new Matrix4[maxBones];
            }
        }

        private Matrix4 GetActiveInverseBind(Bone bone)
        {
            // Use TRSKL joint inverse binds only when the option is enabled and our convention check says they're reliable.
            if (RenderOptions.UseTrsklInverseBind && trsklInverseBindReliable && bone.HasJointInverseBind)
            {
                return bone.JointInverseBindWorld;
            }

            return bone.InverseBindWorld;
        }

        private Matrix4[] GetSkinWorldMatrices()
        {
            if (activePoseWorld != null && activePoseWorld.Length == Bones.Count)
            {
                // When an animation is active, skin directly from the computed pose world matrices to
                // avoid TRS decomposition and recomposition artifacts (especially with SSC and nonuniform scale).
                return activePoseWorld;
            }

            var world = new Matrix4[Bones.Count];
            var worldNoRot = new Matrix4[Bones.Count];
            var computed = new bool[Bones.Count];
            for (int i = 0; i < Bones.Count; i++)
            {
                world[i] = ComputeSkinWorldMatrix(i, computed, world, worldNoRot);
            }
            return world;
        }

        private Matrix4 ComputeSkinWorldMatrix(int index, bool[] computed, Matrix4[] world, Matrix4[] worldNoRot)
        {
            if (computed[index])
            {
                return world[index];
            }

            var bone = Bones[index];
            var local = BuildLocalMatrix(bone.Transform.Scale, bone.Transform.Rotation, bone.Transform.Position, bone.ScalePivot, bone.RotatePivot);
            var localNoRot = BuildLocalMatrix(bone.Transform.Scale, Quaternion.Identity, bone.Transform.Position, bone.ScalePivot, bone.RotatePivot);

            if (bone.ParentIndex >= 0 && bone.ParentIndex < Bones.Count && bone.ParentIndex != index)
            {
                if (bone.UseSegmentScaleCompensate)
                {
                    var parent = Bones[bone.ParentIndex];
                    local *= Matrix4.CreateScale(
                        parent.Transform.Scale.X != 0f ? 1f / parent.Transform.Scale.X : 1f,
                        parent.Transform.Scale.Y != 0f ? 1f / parent.Transform.Scale.Y : 1f,
                        parent.Transform.Scale.Z != 0f ? 1f / parent.Transform.Scale.Z : 1f);
                    localNoRot *= Matrix4.CreateScale(
                        parent.Transform.Scale.X != 0f ? 1f / parent.Transform.Scale.X : 1f,
                        parent.Transform.Scale.Y != 0f ? 1f / parent.Transform.Scale.Y : 1f,
                        parent.Transform.Scale.Z != 0f ? 1f / parent.Transform.Scale.Z : 1f);
                }
                var parentWorld = ComputeSkinWorldMatrix(bone.ParentIndex, computed, world, worldNoRot);
                var parentEffective = bone.IgnoreParentRotation ? worldNoRot[bone.ParentIndex] : parentWorld;
                world[index] = local * parentEffective;
                worldNoRot[index] = localNoRot * parentEffective;
            }
            else
            {
                world[index] = local;
                worldNoRot[index] = localNoRot;
            }

            computed[index] = true;
            return world[index];
        }

        private Matrix4 ComputeWorldMatrix(int index, bool[] computed, Matrix4[] world, Matrix4[] worldNoRot)
        {
            if (computed[index])
            {
                return world[index];
            }

            var bone = Bones[index];
            var local = BuildLocalMatrix(bone.Transform.Scale, bone.Transform.Rotation, bone.Transform.Position, bone.ScalePivot, bone.RotatePivot);
            var localNoRot = BuildLocalMatrix(bone.Transform.Scale, Quaternion.Identity, bone.Transform.Position, bone.ScalePivot, bone.RotatePivot);

            if (bone.ParentIndex >= 0 && bone.ParentIndex < Bones.Count && bone.ParentIndex != index)
            {
                if (bone.UseSegmentScaleCompensate)
                {
                    var parent = Bones[bone.ParentIndex];
                    local *= Matrix4.CreateScale(
                        parent.Transform.Scale.X != 0f ? 1f / parent.Transform.Scale.X : 1f,
                        parent.Transform.Scale.Y != 0f ? 1f / parent.Transform.Scale.Y : 1f,
                        parent.Transform.Scale.Z != 0f ? 1f / parent.Transform.Scale.Z : 1f);
                    localNoRot *= Matrix4.CreateScale(
                        parent.Transform.Scale.X != 0f ? 1f / parent.Transform.Scale.X : 1f,
                        parent.Transform.Scale.Y != 0f ? 1f / parent.Transform.Scale.Y : 1f,
                        parent.Transform.Scale.Z != 0f ? 1f / parent.Transform.Scale.Z : 1f);
                }
                var parentWorld = ComputeWorldMatrix(bone.ParentIndex, computed, world, worldNoRot);
                var parentEffective = bone.IgnoreParentRotation ? worldNoRot[bone.ParentIndex] : parentWorld;
                world[index] = local * parentEffective;
                worldNoRot[index] = localNoRot * parentEffective;
            }
            else
            {
                world[index] = local;
                worldNoRot[index] = localNoRot;
            }

            computed[index] = true;
            return world[index];
        }

    }
}
