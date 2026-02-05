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
        public void ResetPose()
        {
            GetEffectiveArmature()?.ResetPose();
        }

        public void SetArmatureOverride(Armature? overrideArmature)
        {
            armatureOverride = overrideArmature;
            ResolveRigidParentAttachments();
        }

        private Armature? GetEffectiveArmature()
        {
            return armatureOverride ?? armature;
        }


        private static void AppendBoneMesh(List<float> verts, Vector3 head, Vector3 tail)
        {
            var dir = tail - head;
            var len = dir.Length;
            if (len < 0.0001f)
            {
                return;
            }

            var basis = BuildBasis(dir / len);
            var radius = MathF.Max(0.01f, len * 0.07f);

            for (int i = 0; i < unitBoneVerts.Length; i += 3)
            {
                var local = new Vector3(unitBoneVerts[i], unitBoneVerts[i + 1], unitBoneVerts[i + 2]);
                local.X *= radius;
                local.Z *= radius;
                local.Y *= len;

                var world = head + basis * local;
                verts.Add(world.X);
                verts.Add(world.Y);
                verts.Add(world.Z);
            }
        }

        private static Matrix3 BuildBasis(Vector3 direction)
        {
            var up = MathF.Abs(Vector3.Dot(direction, Vector3.UnitY)) > 0.9f
                ? Vector3.UnitX
                : Vector3.UnitY;

            var x = Vector3.Normalize(Vector3.Cross(up, direction));
            var z = Vector3.Normalize(Vector3.Cross(direction, x));
            return new Matrix3(x.X, x.Y, x.Z,
                               direction.X, direction.Y, direction.Z,
                               z.X, z.Y, z.Z);
        }

        private static float[] BuildUnitBoneVerts()
        {
            var head = new Vector3(0f, 0f, 0f);
            var tail = new Vector3(0f, 1f, 0f);
            var a = new Vector3(1f, 0.5f, 0f);
            var b = new Vector3(-1f, 0.5f, 0f);
            var c = new Vector3(0f, 0.5f, 1f);
            var d = new Vector3(0f, 0.5f, -1f);

            return new[]
            {
                head.X, head.Y, head.Z, a.X, a.Y, a.Z, c.X, c.Y, c.Z,
                head.X, head.Y, head.Z, c.X, c.Y, c.Z, b.X, b.Y, b.Z,
                head.X, head.Y, head.Z, b.X, b.Y, b.Z, d.X, d.Y, d.Z,
                head.X, head.Y, head.Z, d.X, d.Y, d.Z, a.X, a.Y, a.Z,
                tail.X, tail.Y, tail.Z, c.X, c.Y, c.Z, a.X, a.Y, a.Z,
                tail.X, tail.Y, tail.Z, b.X, b.Y, b.Z, c.X, c.Y, c.Z,
                tail.X, tail.Y, tail.Z, d.X, d.Y, d.Z, b.X, b.Y, b.Z,
                tail.X, tail.Y, tail.Z, a.X, a.Y, a.Z, d.X, d.Y, d.Z,
            };
        }

        private void DrawOutline(Matrix4 view, Matrix4 proj, int indexCount, bool enableSkinning, int boneCount, Matrix4[] skinMatrices)
        {
            var outlineShader = ShaderPool.Instance.GetShader("Outline");
            if (outlineShader == null)
            {
                return;
            }

            outlineShader.Bind();
            outlineShader.SetMatrix4("model", modelMat);
            outlineShader.SetMatrix4("view", view);
            outlineShader.SetMatrix4("projection", proj);
            outlineShader.SetBoolIfExists("EnableSkinning", enableSkinning);
            outlineShader.SetIntIfExists("BoneCount", enableSkinning ? boneCount : 0);
            outlineShader.SetBoolIfExists("SwapBlendOrder", RenderOptions.SwapBlendOrder);
            if (enableSkinning)
            {
                outlineShader.SetMatrix4ArrayIfExists("Bones", skinMatrices, RenderOptions.TransposeSkinMatrices);
            }
            outlineShader.SetVector3("OutlineColor", RenderOptions.OutlineColor);
            outlineShader.SetFloat("OutlineAlpha", RenderOptions.OutlineAlpha);

            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
            GL.Disable(EnableCap.CullFace);
            GL.LineWidth(1.5f);
            GL.DrawElements(PrimitiveType.Triangles, indexCount, DrawElementsType.UnsignedInt, 0);
            GL.LineWidth(1.0f);
            GL.Enable(EnableCap.CullFace);
            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);

            outlineShader.Unbind();
        }

        public void SetSelectedSubmesh(int index)
        {
            selectedSubmeshIndex = index;
        }

        public void SetVisible(bool visible)
        {
            IsVisible = visible;
        }

        public void SetModelMatrix(Matrix4 matrix)
        {
            modelMat = matrix;
        }

        public bool IsSubmeshVisible(int submeshIndex)
        {
            if (submeshIndex < 0)
            {
                return false;
            }

            if (submeshVisible == null)
            {
                EnsureSubmeshVisibilitySize(Positions.Count);
            }

            return submeshVisible != null &&
                   submeshIndex < submeshVisible.Length &&
                   submeshVisible[submeshIndex];
        }

        public void SetSubmeshVisible(int submeshIndex, bool visible)
        {
            if (submeshIndex < 0)
            {
                return;
            }

            if (submeshVisible == null)
            {
                EnsureSubmeshVisibilitySize(Positions.Count);
            }

            if (submeshVisible == null || submeshIndex >= submeshVisible.Length)
            {
                return;
            }

            submeshVisible[submeshIndex] = visible;
            if (!visible && selectedSubmeshIndex == submeshIndex)
            {
                selectedSubmeshIndex = -1;
            }
        }

        private void EnsureSubmeshVisibilitySize(int count)
        {
            if (count < 0) count = 0;
            if (submeshVisible != null && submeshVisible.Length == count)
            {
                return;
            }

            submeshVisible = new bool[count];
            for (int i = 0; i < count; i++)
            {
                submeshVisible[i] = true;
            }
        }
	    }
}
