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
        public void DrawSkeleton(Matrix4 view, Matrix4 proj)
        {
            if (!IsVisible)
            {
                return;
            }

            if (armature == null || armature.Bones.Count == 0)
            {
                return;
            }

            var shader = ShaderPool.Instance.GetShader("Lines");
            if (shader == null)
            {
                return;
            }

            var positions = armature.GetWorldPositions();
            var boneVerts = new List<float>();

            for (int i = 0; i < positions.Length; i++)
            {
                if (!armature.IsVisibleBone(i))
                {
                    continue;
                }

                var head = positions[i];
                bool added = false;

                // Draw to each visible child (better for hands/fingers where the first child is often a helper/roll node).
                var bone = armature.Bones[i];
                foreach (var child in bone.Children)
                {
                    int childIndex = armature.Bones.IndexOf(child);
                    if (childIndex < 0 || childIndex >= positions.Length)
                    {
                        continue;
                    }
                    if (!armature.IsVisibleBone(childIndex))
                    {
                        continue;
                    }

                    var tail = positions[childIndex];
                    if ((tail - head).LengthSquared < 0.0001f)
                    {
                        continue;
                    }

                    boneVerts.Add(head.X);
                    boneVerts.Add(head.Y);
                    boneVerts.Add(head.Z);
                    boneVerts.Add(tail.X);
                    boneVerts.Add(tail.Y);
                    boneVerts.Add(tail.Z);
                    added = true;
                }

                // Leaf bone: extension along the parent to child direction keeps the line stable.
                if (!added)
                {
                    int parent = armature.GetVisibleParentIndex(i);
                    Vector3 dir = Vector3.UnitY;
                    if (parent >= 0 && parent < positions.Length)
                    {
                        var d = head - positions[parent];
                        if (d.LengthSquared > 0.000001f)
                        {
                            dir = Vector3.Normalize(d);
                        }
                    }

                    var tail = head + dir * 0.05f;
                    boneVerts.Add(head.X);
                    boneVerts.Add(head.Y);
                    boneVerts.Add(head.Z);
                    boneVerts.Add(tail.X);
                    boneVerts.Add(tail.Y);
                    boneVerts.Add(tail.Z);
                }
            }

            if (boneVerts.Count == 0)
            {
                return;
            }

            if (skeletonVao == 0)
            {
                skeletonVao = GL.GenVertexArray();
                skeletonVbo = GL.GenBuffer();
                GL.BindVertexArray(skeletonVao);
                GL.BindBuffer(BufferTarget.ArrayBuffer, skeletonVbo);
                GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
                GL.EnableVertexAttribArray(0);
                GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
                GL.BindVertexArray(0);
            }

            GL.BindVertexArray(skeletonVao);
            GL.BindBuffer(BufferTarget.ArrayBuffer, skeletonVbo);
            GL.BufferData(BufferTarget.ArrayBuffer, boneVerts.Count * sizeof(float), boneVerts.ToArray(), BufferUsageHint.DynamicDraw);

            shader.Bind();
            shader.SetMatrix4("model", modelMat);
            shader.SetMatrix4("view", view);
            shader.SetMatrix4("projection", proj);
            shader.SetVector4("color", new Vector4(1.0f, 0.85f, 0.1f, 1.0f));

            GL.DrawArrays(PrimitiveType.Lines, 0, boneVerts.Count / 3);

            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindVertexArray(0);
        }
	    }
}
