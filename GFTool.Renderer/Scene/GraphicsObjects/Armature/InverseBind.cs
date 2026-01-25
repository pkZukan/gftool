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
        private void ComputeInverseBindMatrices(bool useTrsklInverseBind)
        {
            if (Bones.Count == 0)
            {
                return;
            }

            // Some models ship TRSKL joint inverse bind matrices that don't match the rest pose hierarchy
            // (or use a different space/convention). When detected, fall back to computing inverse binds
            // from the rest pose so skinning behaves predictably.
            if (useTrsklInverseBind && !trsklInverseBindReliable)
            {
                useTrsklInverseBind = false;
                if (MessageHandler.Instance.DebugLogsEnabled)
                {
                    MessageHandler.Instance.AddMessage(
                        MessageType.WARNING,
                        "[Bind] TRSKL inverse binds flagged unreliable; falling back to rest-pose computed inverse binds.");
                }
            }

            var bindWorld = new Matrix4[Bones.Count];
            var computed = new bool[Bones.Count];
            for (int i = 0; i < Bones.Count; i++)
            {
                bindWorld[i] = ComputeBindWorld(i, useTrsklInverseBind, bindWorld, computed);
            }

            for (int i = 0; i < Bones.Count; i++)
            {
                if (useTrsklInverseBind && Bones[i].HasJointInverseBind)
                {
                    Bones[i].InverseBindWorld = Bones[i].JointInverseBindWorld;
                }
                else
                {
                    if (TryInvert(bindWorld[i], out var inv))
                    {
                        Bones[i].InverseBindWorld = inv;
                    }
                    else
                    {
                        WarnSingularInvert(
                            context: "InverseBindWorld",
                            boneIndex: i,
                            boneName: Bones[i].Name,
                            animation: null,
                            frame: 0.0f,
                            detail: null);
                        Bones[i].InverseBindWorld = Matrix4.Identity;
                    }
                }
            }

        }

        private Matrix4 ComputeBindWorld(int index, bool useTrsklInverseBind, Matrix4[] world, bool[] computed)
        {
            if (computed[index])
            {
                return world[index];
            }

            var bone = Bones[index];
            Matrix4 local;
            if (useTrsklInverseBind && bone.HasJointInverseBind)
            {
                if (!TryInvert(bone.JointInverseBindWorld, out local))
                {
                    WarnSingularInvert(
                        context: "JointInverseBindWorld",
                        boneIndex: index,
                        boneName: bone.Name,
                        animation: null,
                        frame: 0.0f,
                        detail: null);
                    local = Matrix4.Identity;
                }
            }
            else
            {
                local = bone.RestLocalMatrix;
            }

            if (bone.ParentIndex >= 0 && bone.ParentIndex < Bones.Count && bone.ParentIndex != index)
            {
                if (bone.UseSegmentScaleCompensate)
                {
                    var parent = Bones[bone.ParentIndex];
                    local *= Matrix4.CreateScale(
                        parent.RestScale.X != 0f ? 1f / parent.RestScale.X : 1f,
                        parent.RestScale.Y != 0f ? 1f / parent.RestScale.Y : 1f,
                        parent.RestScale.Z != 0f ? 1f / parent.RestScale.Z : 1f);
                }
                var parentWorld = ComputeBindWorld(bone.ParentIndex, useTrsklInverseBind, world, computed);
                world[index] = local * parentWorld;
            }
            else
            {
                world[index] = local;
            }

            computed[index] = true;
            return world[index];
        }

        private static Matrix4 CreateMatrixFromAxis(Vector3 axisX, Vector3 axisY, Vector3 axisZ, Vector3 axisW)
        {
            return CreateMatrixFromAxisRows(axisX, axisY, axisZ, axisW);
        }

        private static Matrix4 CreateMatrixFromAxisRows(Vector3 axisX, Vector3 axisY, Vector3 axisZ, Vector3 axisW)
        {
            // Axis x/y/z/w are rows; translation lives in axis w.
            return new Matrix4(
                axisX.X, axisX.Y, axisX.Z, 0f,
                axisY.X, axisY.Y, axisY.Z, 0f,
                axisZ.X, axisZ.Y, axisZ.Z, 0f,
                axisW.X, axisW.Y, axisW.Z, 1f);
        }

        private static Matrix4 CreateMatrixFromAxisColumns(Vector3 axisX, Vector3 axisY, Vector3 axisZ, Vector3 axisW)
        {
            // Axis x/y/z are columns; translation lives in axis w.
            return new Matrix4(
                axisX.X, axisY.X, axisZ.X, 0f,
                axisX.Y, axisY.Y, axisZ.Y, 0f,
                axisX.Z, axisY.Z, axisZ.Z, 0f,
                axisW.X, axisW.Y, axisW.Z, 1f);
        }

    }
}
