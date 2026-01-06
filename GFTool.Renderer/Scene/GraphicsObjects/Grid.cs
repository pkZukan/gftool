using GFTool.Renderer.Core.Graphics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System.Collections.Generic;

namespace GFTool.Renderer.Scene.GraphicsObjects
{
    public class Grid : RefObject
    {
        private const int GridSize = 20;
        private const float GridSpacing = 1.0f;

        private int vao;
        private int vbo;
        private int vertexCount;

        private Shader shader;

        public override void Setup()
        {
            shader = ShaderPool.Instance.GetShader("Grid");
            if (shader == null)
            {
                return;
            }

            var vertices = BuildGridVertices();
            vertexCount = vertices.Length;

            vao = GL.GenVertexArray();
            vbo = GL.GenBuffer();

            GL.BindVertexArray(vao);
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * Vector3.SizeInBytes, vertices, BufferUsageHint.StaticDraw);

            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, Vector3.SizeInBytes, 0);
            GL.EnableVertexAttribArray(0);

            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindVertexArray(0);

            base.Setup();
        }

        public override void Draw(Matrix4 view, Matrix4 proj)
        {
            if (shader == null || vertexCount == 0)
            {
                return;
            }

            shader.Bind();
            shader.SetMatrix4("model", Matrix4.Identity);
            shader.SetMatrix4("view", view);
            shader.SetMatrix4("projection", proj);
            shader.SetVector3("gridColor", new Vector3(0.35f, 0.35f, 0.35f));

            GL.BindVertexArray(vao);
            GL.DrawArrays(PrimitiveType.Lines, 0, vertexCount);
            GL.BindVertexArray(0);
        }

        private static Vector3[] BuildGridVertices()
        {
            var lines = new List<Vector3>();
            float half = GridSize * GridSpacing * 0.5f;

            for (int i = 0; i <= GridSize; i++)
            {
                float offset = -half + (i * GridSpacing);

                lines.Add(new Vector3(-half, 0, offset));
                lines.Add(new Vector3(half, 0, offset));

                lines.Add(new Vector3(offset, 0, -half));
                lines.Add(new Vector3(offset, 0, half));
            }

            return lines.ToArray();
        }
    }
}
