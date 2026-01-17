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
        private void UpdateRestParentMatrices()
        {
            for (int i = 0; i < Bones.Count; i++)
            {
                var bone = Bones[i];
                if (bone.ParentIndex >= 0 && bone.ParentIndex < Bones.Count && bone.ParentIndex != i)
                {
                    if (TryInvert(Bones[bone.ParentIndex].RestLocalMatrix, out var inv))
                    {
                        bone.RestInvParentMatrix = inv;
                    }
                    else
                    {
                        WarnSingularInvert(
                            context: "RestInvParentMatrix",
                            boneIndex: i,
                            boneName: bone.Name,
                            animation: null,
                            frame: 0.0f,
                            detail: $"parent={Bones[bone.ParentIndex].Name}({bone.ParentIndex})");
                        bone.RestInvParentMatrix = Matrix4.Identity;
                    }
                }
                else
                {
                    bone.RestInvParentMatrix = Matrix4.Identity;
                }
            }
        }

        private static bool TryInvert(Matrix4 matrix, out Matrix4 inverse)
        {
            try
            {
                inverse = Matrix4.Invert(matrix);
                return true;
            }
            catch
            {
                inverse = Matrix4.Identity;
                return false;
            }
        }

        private void WarnSingularInvert(string context, int boneIndex, string boneName, Animation? animation, float frame, string? detail)
        {
            if (!MessageHandler.Instance.DebugLogsEnabled)
            {
                return;
            }

            var animName = animation?.Name ?? "<none>";
            if (!string.Equals(lastSingularInvertAnimName, animName, StringComparison.Ordinal))
            {
                lastSingularInvertAnimName = animName;
                warnedSingularInvertBones.Clear();
            }

            if (!warnedSingularInvertBones.Add(boneIndex))
            {
                return;
            }

            MessageHandler.Instance.AddMessage(
                MessageType.WARNING,
                $"[Anim] Singular matrix invert ({context}) bone={boneName}({boneIndex}) anim='{animName}' frame={frame:0.###}" +
                (!string.IsNullOrWhiteSpace(detail) ? $" {detail}" : string.Empty));
        }

        public Vector3[] GetWorldPositions()
        {
            var worldMatrices = GetWorldMatrices();
            var positions = new Vector3[worldMatrices.Length];
            for (int i = 0; i < worldMatrices.Length; i++)
            {
                positions[i] = worldMatrices[i].ExtractTranslation();
            }
            return positions;
        }

        public Matrix4[] GetWorldMatrices()
        {
            if (activePoseWorld != null && activePoseWorld.Length == Bones.Count)
            {
                return (Matrix4[])activePoseWorld.Clone();
            }

            var world = new Matrix4[Bones.Count];
            var worldNoRot = new Matrix4[Bones.Count];
            var computed = new bool[Bones.Count];
            for (int i = 0; i < Bones.Count; i++)
            {
                world[i] = ComputeWorldMatrix(i, computed, world, worldNoRot);
            }
            return world;
        }

    }
}
