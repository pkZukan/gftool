using GFTool.Renderer.Core;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Linq;

namespace GFTool.Renderer.Scene.GraphicsObjects
{
    public partial class Model
    {
        private int gpuSetupIndex = -1;
        private bool gpuSetupComplete;

        internal bool IsGpuSetupComplete => gpuSetupComplete;

        internal void BeginGpuSetup()
        {
            if (gpuSetupComplete || gpuSetupIndex >= 0)
            {
                return;
            }

            int submeshCnt = Positions.Count;
            VAOs = new int[submeshCnt];
            VBOs = new int[submeshCnt];
            EBOs = new int[Indices.Count()];
            blendIndexOffsets = new int[submeshCnt];
            blendIndexByteSizes = new int[submeshCnt];
            EnsureSubmeshVisibilitySize(submeshCnt);
            gpuSetupIndex = 0;
        }

        internal bool StepGpuSetup()
        {
            if (gpuSetupComplete)
            {
                return true;
            }

            if (gpuSetupIndex < 0)
            {
                BeginGpuSetup();
            }

            if (VAOs == null || VBOs == null || EBOs == null)
            {
                return true;
            }

            if (gpuSetupIndex >= VAOs.Length)
            {
                FinalizeGpuSetup();
                return true;
            }

            SetupSubmeshGpu(gpuSetupIndex);
            gpuSetupIndex++;
            if (gpuSetupIndex >= VAOs.Length)
            {
                FinalizeGpuSetup();
                return true;
            }

            return false;
        }

        private void FinalizeGpuSetup()
        {
            gpuSetupComplete = true;
            gpuSetupIndex = VAOs?.Length ?? -1;

            // Grab any errors from setup.
            ErrorCode error;
            while ((error = GL.GetError()) != ErrorCode.NoError)
            {
                MessageHandler.Instance.AddMessage(MessageType.ERROR, $"Error in model \"{Name}\": {error}");
            }

            base.Setup();
        }

        private void SetupSubmeshGpu(int i)
        {
            // VAO
            GL.GenVertexArrays(1, out VAOs[i]);
            GL.BindVertexArray(VAOs[i]);

            // Sizes
            var vertSize = Positions[i].Length * Vector3.SizeInBytes;
            var normSize = Normals[i].Length * Vector3.SizeInBytes;
            var uvSize = UVs[i].Length * Vector2.SizeInBytes;
            var uv2Size = UVs2[i].Length * Vector2.SizeInBytes;
            var colorSize = Colors[i].Length * Vector4.SizeInBytes;
            var tangentSize = Tangents[i].Length * Vector4.SizeInBytes;
            var binormalSize = Binormals[i].Length * Vector3.SizeInBytes;
            var blendIndexSize = BlendIndicies[i].Length * Vector4.SizeInBytes;
            var blendWeightSize = BlendWeights[i].Length * Vector4.SizeInBytes;
            var totalSize = vertSize + normSize + uvSize + uv2Size + colorSize + tangentSize + binormalSize + blendIndexSize + blendWeightSize;

            blendIndexOffsets[i] = vertSize + normSize + uvSize + uv2Size + colorSize + tangentSize + binormalSize;
            blendIndexByteSizes[i] = blendIndexSize;

            // VBO
            GL.GenBuffers(1, out VBOs[i]);
            GL.BindBuffer(BufferTarget.ArrayBuffer, VBOs[i]);
            GL.BufferData(BufferTarget.ArrayBuffer, totalSize, IntPtr.Zero, BufferUsageHint.StaticDraw);

            // Upload vertex data to the buffer
            IntPtr offset = IntPtr.Zero;
            GL.BufferSubData(BufferTarget.ArrayBuffer, offset, vertSize, ToUnmanagedByteArray(Positions[i])); offset += vertSize;
            GL.BufferSubData(BufferTarget.ArrayBuffer, offset, normSize, ToUnmanagedByteArray(Normals[i])); offset += normSize;
            GL.BufferSubData(BufferTarget.ArrayBuffer, offset, uvSize, ToUnmanagedByteArray(UVs[i])); offset += uvSize;
            GL.BufferSubData(BufferTarget.ArrayBuffer, offset, uv2Size, ToUnmanagedByteArray(UVs2[i])); offset += uv2Size;
            GL.BufferSubData(BufferTarget.ArrayBuffer, offset, colorSize, ToUnmanagedByteArray(Colors[i])); offset += colorSize;
            GL.BufferSubData(BufferTarget.ArrayBuffer, offset, tangentSize, ToUnmanagedByteArray(Tangents[i])); offset += tangentSize;
            GL.BufferSubData(BufferTarget.ArrayBuffer, offset, binormalSize, ToUnmanagedByteArray(Binormals[i])); offset += binormalSize;
            GL.BufferSubData(BufferTarget.ArrayBuffer, offset, blendIndexSize, ToUnmanagedByteArray(BlendIndicies[i])); offset += blendIndexSize;
            GL.BufferSubData(BufferTarget.ArrayBuffer, offset, blendWeightSize, ToUnmanagedByteArray(BlendWeights[i])); offset += blendWeightSize;

            // EBO (indices)
            GL.GenBuffers(1, out EBOs[i]);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, EBOs[i]);
            GL.BufferData(BufferTarget.ElementArrayBuffer, Indices[i].Length * sizeof(uint), Indices[i].ToArray(), BufferUsageHint.StaticDraw);

            offset = IntPtr.Zero;

            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, Vector3.SizeInBytes, offset); offset += vertSize;
            GL.EnableVertexAttribArray(0);

            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, Vector3.SizeInBytes, offset); offset += normSize;
            GL.EnableVertexAttribArray(1);

            GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, Vector2.SizeInBytes, offset); offset += uvSize;
            GL.EnableVertexAttribArray(2);

            GL.VertexAttribPointer(8, 2, VertexAttribPointerType.Float, false, Vector2.SizeInBytes, offset); offset += uv2Size;
            GL.EnableVertexAttribArray(8);

            GL.VertexAttribPointer(3, 4, VertexAttribPointerType.Float, false, Vector4.SizeInBytes, offset); offset += colorSize;
            GL.EnableVertexAttribArray(3);

            GL.VertexAttribPointer(4, 4, VertexAttribPointerType.Float, false, Vector4.SizeInBytes, offset); offset += tangentSize;
            GL.EnableVertexAttribArray(4);

            GL.VertexAttribPointer(5, 3, VertexAttribPointerType.Float, false, Vector3.SizeInBytes, offset); offset += binormalSize;
            GL.EnableVertexAttribArray(5);

            GL.VertexAttribPointer(6, 4, VertexAttribPointerType.Float, false, Vector4.SizeInBytes, offset); offset += blendIndexSize;
            GL.EnableVertexAttribArray(6);

            GL.VertexAttribPointer(7, 4, VertexAttribPointerType.Float, false, Vector4.SizeInBytes, offset); offset += blendWeightSize;
            GL.EnableVertexAttribArray(7);

            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindVertexArray(0);
        }
    }
}
