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
using System.Runtime.InteropServices;


namespace GFTool.Renderer.Scene.GraphicsObjects
{
	    public partial class Model : RefObject
	    {
        private class BlendIndexStats
        {
            public int VertexCount;
            public int MaxIndex;
        }

        private static float MapBlendIndex(float value, TRBoneWeight[] boneWeights)
        {
            int index = (int)MathF.Round(value);
            if (index >= 0 && index < boneWeights.Length)
            {
                int rigIndex = boneWeights[index].RigIndex;
                return rigIndex >= 0 ? rigIndex : value;
            }
            return value;
        }

        private static TRBuffer? GetVertexBuffer(TRBuffer[] buffers, int index)
        {
            if (buffers == null || index < 0 || index >= buffers.Length)
            {
                return null;
            }
            return buffers[index];
        }

        private static int GetStride(TRVertexDeclaration vertDesc, int sizeIndex)
        {
            if (vertDesc.vertexElementSizes == null || sizeIndex < 0 || sizeIndex >= vertDesc.vertexElementSizes.Length)
            {
                return 0;
            }
            return vertDesc.vertexElementSizes[sizeIndex].Size;
        }

        private static bool HasBytes(byte[] buffer, int offset, TRVertexFormat format)
        {
            int size = format switch
            {
                TRVertexFormat.X32_Y32_Z32_FLOAT => 12,
                TRVertexFormat.X32_Y32_FLOAT => 8,
                TRVertexFormat.W32_X32_Y32_Z32_FLOAT => 16,
                TRVertexFormat.W32_X32_Y32_Z32_UNSIGNED => 16,
                TRVertexFormat.W16_X16_Y16_Z16_FLOAT => 8,
                TRVertexFormat.W16_X16_Y16_Z16_UNSIGNED_NORMALIZED => 8,
                TRVertexFormat.R8_G8_B8_A8_UNSIGNED_NORMALIZED => 4,
                TRVertexFormat.W8_X8_Y8_Z8_UNSIGNED => 4,
                _ => 0
            };
            return size > 0 && offset >= 0 && offset + size <= buffer.Length;
        }

        private static void EnsureBlendStream(List<Vector4[]> streams, int index, int vertexCount)
        {
            while (streams.Count <= index)
            {
                streams.Add(new Vector4[vertexCount]);
            }
        }

        private static void EnsureUvStream(List<Vector2[]> streams, int index, int vertexCount)
        {
            while (streams.Count <= index)
            {
                streams.Add(new Vector2[vertexCount]);
            }
        }

        private static byte[] ToUnmanagedByteArray<T>(T[] data) where T : unmanaged
        {
            if (data == null || data.Length == 0)
            {
                return Array.Empty<byte>();
            }

            return MemoryMarshal.AsBytes(data.AsSpan()).ToArray();
        }

        private static void CollapseBlendStreams(
            List<Vector4[]> indexStreams,
            List<Vector4[]> weightStreams,
            int streamCount,
            out Vector4[] collapsedIndices,
            out Vector4[] collapsedWeights)
        {
            int vertexCount = indexStreams[0].Length;
            collapsedIndices = new Vector4[vertexCount];
            collapsedWeights = new Vector4[vertexCount];

            for (int v = 0; v < vertexCount; v++)
            {
                int maxInfluences = 4 * streamCount;
                Span<int> uniqueIndices = stackalloc int[maxInfluences];
                Span<float> uniqueWeights = stackalloc float[maxInfluences];
                int uniqueCount = 0;

                for (int s = 0; s < streamCount; s++)
                {
                    var idx = indexStreams[s][v];
                    var w = weightStreams[s][v];
                    AccumulateInfluence(uniqueIndices, uniqueWeights, ref uniqueCount, (int)MathF.Round(idx.X), w.X);
                    AccumulateInfluence(uniqueIndices, uniqueWeights, ref uniqueCount, (int)MathF.Round(idx.Y), w.Y);
                    AccumulateInfluence(uniqueIndices, uniqueWeights, ref uniqueCount, (int)MathF.Round(idx.Z), w.Z);
                    AccumulateInfluence(uniqueIndices, uniqueWeights, ref uniqueCount, (int)MathF.Round(idx.W), w.W);
                }

                if (uniqueCount == 0)
                {
                    collapsedIndices[v] = Vector4.Zero;
                    collapsedWeights[v] = Vector4.Zero;
                    continue;
                }

                Span<int> topIndices = stackalloc int[4];
                Span<float> topWeights = stackalloc float[4];

                for (int i = 0; i < uniqueCount; i++)
                {
                    float weight = uniqueWeights[i];
                    if (weight <= 0f)
                    {
                        continue;
                    }

                    int index = uniqueIndices[i];

                    if (weight > topWeights[0])
                    {
                        topWeights[3] = topWeights[2];
                        topIndices[3] = topIndices[2];
                        topWeights[2] = topWeights[1];
                        topIndices[2] = topIndices[1];
                        topWeights[1] = topWeights[0];
                        topIndices[1] = topIndices[0];
                        topWeights[0] = weight;
                        topIndices[0] = index;
                    }
                    else if (weight > topWeights[1])
                    {
                        topWeights[3] = topWeights[2];
                        topIndices[3] = topIndices[2];
                        topWeights[2] = topWeights[1];
                        topIndices[2] = topIndices[1];
                        topWeights[1] = weight;
                        topIndices[1] = index;
                    }
                    else if (weight > topWeights[2])
                    {
                        topWeights[3] = topWeights[2];
                        topIndices[3] = topIndices[2];
                        topWeights[2] = weight;
                        topIndices[2] = index;
                    }
                    else if (weight > topWeights[3])
                    {
                        topWeights[3] = weight;
                        topIndices[3] = index;
                    }
                }

                collapsedIndices[v] = new Vector4(topIndices[0], topIndices[1], topIndices[2], topIndices[3]);
                collapsedWeights[v] = new Vector4(topWeights[0], topWeights[1], topWeights[2], topWeights[3]);
            }
        }

        private static void AccumulateInfluence(
            Span<int> indices,
            Span<float> weights,
            ref int count,
            int index,
            float weight)
        {
            if (weight <= 0f)
            {
                return;
            }

            for (int i = 0; i < count; i++)
            {
                if (indices[i] == index)
                {
                    weights[i] += weight;
                    return;
                }
            }

            indices[count] = index;
            weights[count] = weight;
            count++;
        }

        private static Vector3 ReadVector3(byte[] buffer, int offset, TRVertexFormat format)
        {
            switch (format)
            {
                case TRVertexFormat.X32_Y32_Z32_FLOAT:
                    return new Vector3(BitConverter.ToSingle(buffer, offset), BitConverter.ToSingle(buffer, offset + 4), BitConverter.ToSingle(buffer, offset + 8));
                case TRVertexFormat.W32_X32_Y32_Z32_FLOAT:
                    return new Vector3(BitConverter.ToSingle(buffer, offset + 4), BitConverter.ToSingle(buffer, offset + 8), BitConverter.ToSingle(buffer, offset + 12));
                case TRVertexFormat.W16_X16_Y16_Z16_FLOAT:
                    return new Vector3(ReadHalf(buffer, offset), ReadHalf(buffer, offset + 2), ReadHalf(buffer, offset + 4));
                case TRVertexFormat.W16_X16_Y16_Z16_UNSIGNED_NORMALIZED:
                    return new Vector3(ReadUnorm16(buffer, offset), ReadUnorm16(buffer, offset + 2), ReadUnorm16(buffer, offset + 4));
                case TRVertexFormat.R8_G8_B8_A8_UNSIGNED_NORMALIZED:
                case TRVertexFormat.W8_X8_Y8_Z8_UNSIGNED:
                    return new Vector3(ReadUnorm8(buffer, offset), ReadUnorm8(buffer, offset + 1), ReadUnorm8(buffer, offset + 2));
                default:
                    return Vector3.Zero;
            }
        }

        private static Vector3 ReadNormal(byte[] buffer, int offset, TRVertexFormat format)
        {
            switch (format)
            {
                case TRVertexFormat.W16_X16_Y16_Z16_FLOAT:
                    return new Vector3(ReadHalf(buffer, offset), ReadHalf(buffer, offset + 2), ReadHalf(buffer, offset + 4));
                case TRVertexFormat.W16_X16_Y16_Z16_UNSIGNED_NORMALIZED:
                    return new Vector3(ReadSnorm16(buffer, offset), ReadSnorm16(buffer, offset + 2), ReadSnorm16(buffer, offset + 4));
                case TRVertexFormat.R8_G8_B8_A8_UNSIGNED_NORMALIZED:
                case TRVertexFormat.W8_X8_Y8_Z8_UNSIGNED:
                    return new Vector3(ReadSnorm8(buffer, offset), ReadSnorm8(buffer, offset + 1), ReadSnorm8(buffer, offset + 2));
                case TRVertexFormat.X32_Y32_Z32_FLOAT:
                    return new Vector3(BitConverter.ToSingle(buffer, offset), BitConverter.ToSingle(buffer, offset + 4), BitConverter.ToSingle(buffer, offset + 8));
                default:
                    return Vector3.UnitZ;
            }
        }

        private static Vector2 ReadVector2(byte[] buffer, int offset, TRVertexFormat format)
        {
            switch (format)
            {
                case TRVertexFormat.X32_Y32_FLOAT:
                    return new Vector2(BitConverter.ToSingle(buffer, offset), BitConverter.ToSingle(buffer, offset + 4));
                case TRVertexFormat.W16_X16_Y16_Z16_FLOAT:
                    return new Vector2(ReadHalf(buffer, offset), ReadHalf(buffer, offset + 2));
                case TRVertexFormat.W16_X16_Y16_Z16_UNSIGNED_NORMALIZED:
                    return new Vector2(ReadUnorm16(buffer, offset), ReadUnorm16(buffer, offset + 2));
                case TRVertexFormat.R8_G8_B8_A8_UNSIGNED_NORMALIZED:
                case TRVertexFormat.W8_X8_Y8_Z8_UNSIGNED:
                    return new Vector2(ReadUnorm8(buffer, offset), ReadUnorm8(buffer, offset + 1));
                default:
                    return Vector2.Zero;
            }
        }

        private static Vector4 ReadColor(byte[] buffer, int offset, TRVertexFormat format)
        {
            Vector4 color;
            switch (format)
            {
                case TRVertexFormat.R8_G8_B8_A8_UNSIGNED_NORMALIZED:
                case TRVertexFormat.W8_X8_Y8_Z8_UNSIGNED:
                    color = new Vector4(
                        ReadUnorm8(buffer, offset),
                        ReadUnorm8(buffer, offset + 1),
                        ReadUnorm8(buffer, offset + 2),
                        ReadUnorm8(buffer, offset + 3));
                    break;
                case TRVertexFormat.W16_X16_Y16_Z16_UNSIGNED_NORMALIZED:
                    color = new Vector4(
                        ReadUnorm16(buffer, offset),
                        ReadUnorm16(buffer, offset + 2),
                        ReadUnorm16(buffer, offset + 4),
                        ReadUnorm16(buffer, offset + 6));
                    break;
                case TRVertexFormat.W16_X16_Y16_Z16_FLOAT:
                    color = new Vector4(
                        ReadHalf(buffer, offset),
                        ReadHalf(buffer, offset + 2),
                        ReadHalf(buffer, offset + 4),
                        ReadHalf(buffer, offset + 6));
                    break;
                default:
                    color = Vector4.One;
                    break;
            }

            // Some external pipelines un-premultiply vertex colors by alpha.
            // Several Trinity meshes store RGB premultiplied, which otherwise tints surfaces (notably skin).
            if (color.W <= 0.000001f)
            {
                return new Vector4(color.X, color.Y, color.Z, 1.0f);
            }

            // Avoid amplifying colors when the stored RGB is already in straight-alpha form.
            // Premultiplied-alpha data will have RGB <= A (up to minor quantization error).
            const float epsilon = 0.0005f;
            bool premultiplied =
                color.X <= color.W + epsilon &&
                color.Y <= color.W + epsilon &&
                color.Z <= color.W + epsilon &&
                color.W < 0.9999f;

            if (!premultiplied)
            {
                return color;
            }

            float invA = 1.0f / color.W;
            return new Vector4(
                MathF.Min(color.X * invA, 1.0f),
                MathF.Min(color.Y * invA, 1.0f),
                MathF.Min(color.Z * invA, 1.0f),
                color.W);
        }

        private static Vector4 ReadTangent(byte[] buffer, int offset, TRVertexFormat format)
        {
            switch (format)
            {
                case TRVertexFormat.W32_X32_Y32_Z32_FLOAT:
                    return new Vector4(
                        BitConverter.ToSingle(buffer, offset + 4),
                        BitConverter.ToSingle(buffer, offset + 8),
                        BitConverter.ToSingle(buffer, offset + 12),
                        BitConverter.ToSingle(buffer, offset));
                case TRVertexFormat.X32_Y32_Z32_FLOAT:
                    return new Vector4(
                        BitConverter.ToSingle(buffer, offset),
                        BitConverter.ToSingle(buffer, offset + 4),
                        BitConverter.ToSingle(buffer, offset + 8),
                        1f);
                case TRVertexFormat.W16_X16_Y16_Z16_FLOAT:
                    return new Vector4(
                        ReadHalf(buffer, offset),
                        ReadHalf(buffer, offset + 2),
                        ReadHalf(buffer, offset + 4),
                        ReadHalf(buffer, offset + 6));
                case TRVertexFormat.W16_X16_Y16_Z16_UNSIGNED_NORMALIZED:
                    return new Vector4(
                        ReadSnorm16(buffer, offset),
                        ReadSnorm16(buffer, offset + 2),
                        ReadSnorm16(buffer, offset + 4),
                        ReadSnorm16(buffer, offset + 6));
                case TRVertexFormat.R8_G8_B8_A8_UNSIGNED_NORMALIZED:
                case TRVertexFormat.W8_X8_Y8_Z8_UNSIGNED:
                    return new Vector4(
                        ReadSnorm8(buffer, offset),
                        ReadSnorm8(buffer, offset + 1),
                        ReadSnorm8(buffer, offset + 2),
                        ReadSnorm8(buffer, offset + 3));
                default:
                    return new Vector4(1f, 0f, 0f, 1f);
            }
        }

        private static Vector4 ReadBlendIndices(byte[] buffer, int offset, TRVertexFormat format)
        {
            switch (format)
            {
                case TRVertexFormat.W8_X8_Y8_Z8_UNSIGNED:
                    // handle it via the shader-side `SwapBlendOrder` option (keeps this reader consistent
                    // with other formats and makes the behavior user-toggleable).
                    return new Vector4(
                        buffer[offset],
                        buffer[offset + 1],
                        buffer[offset + 2],
                        buffer[offset + 3]);
                case TRVertexFormat.W16_X16_Y16_Z16_UNSIGNED_NORMALIZED:
                    return new Vector4(
                        BitConverter.ToUInt16(buffer, offset),
                        BitConverter.ToUInt16(buffer, offset + 2),
                        BitConverter.ToUInt16(buffer, offset + 4),
                        BitConverter.ToUInt16(buffer, offset + 6));
                case TRVertexFormat.W32_X32_Y32_Z32_UNSIGNED:
                    return new Vector4(
                        BitConverter.ToUInt32(buffer, offset),
                        BitConverter.ToUInt32(buffer, offset + 4),
                        BitConverter.ToUInt32(buffer, offset + 8),
                        BitConverter.ToUInt32(buffer, offset + 12));
                case TRVertexFormat.W32_X32_Y32_Z32_FLOAT:
                    return new Vector4(
                        BitConverter.ToSingle(buffer, offset),
                        BitConverter.ToSingle(buffer, offset + 4),
                        BitConverter.ToSingle(buffer, offset + 8),
                        BitConverter.ToSingle(buffer, offset + 12));
                default:
                    return Vector4.Zero;
            }
        }

        private static Vector4 ReadBlendWeights(byte[] buffer, int offset, TRVertexFormat format)
        {
            switch (format)
            {
                case TRVertexFormat.W16_X16_Y16_Z16_UNSIGNED_NORMALIZED:
                    // Stored as four sequential UNORM16 values (XYZW). If a pipeline uses WXYZ ordering,
                    // rely on shader-side `SwapBlendOrder` to rotate channels.
                    return new Vector4(
                        ReadUnorm16(buffer, offset),
                        ReadUnorm16(buffer, offset + 2),
                        ReadUnorm16(buffer, offset + 4),
                        ReadUnorm16(buffer, offset + 6));
                case TRVertexFormat.R8_G8_B8_A8_UNSIGNED_NORMALIZED:
                    // RGBA order.
                    return new Vector4(
                        ReadUnorm8(buffer, offset),
                        ReadUnorm8(buffer, offset + 1),
                        ReadUnorm8(buffer, offset + 2),
                        ReadUnorm8(buffer, offset + 3));
                case TRVertexFormat.W8_X8_Y8_Z8_UNSIGNED:
                    return new Vector4(
                        ReadUnorm8(buffer, offset),
                        ReadUnorm8(buffer, offset + 1),
                        ReadUnorm8(buffer, offset + 2),
                        ReadUnorm8(buffer, offset + 3));
                case TRVertexFormat.W32_X32_Y32_Z32_FLOAT:
                    return new Vector4(
                        BitConverter.ToSingle(buffer, offset),
                        BitConverter.ToSingle(buffer, offset + 4),
                        BitConverter.ToSingle(buffer, offset + 8),
                        BitConverter.ToSingle(buffer, offset + 12));
                default:
                    return Vector4.Zero;
            }
        }

        private static float ReadHalf(byte[] buffer, int offset)
        {
            ushort raw = BitConverter.ToUInt16(buffer, offset);
            return (float)BitConverter.UInt16BitsToHalf(raw);
        }

        private static float ReadUnorm16(byte[] buffer, int offset)
        {
            return BitConverter.ToUInt16(buffer, offset) / 65535f;
        }

        private static float ReadSnorm16(byte[] buffer, int offset)
        {
            return (BitConverter.ToUInt16(buffer, offset) / 65535f) * 2f - 1f;
        }

        private static float ReadUnorm8(byte[] buffer, int offset)
        {
            return buffer[offset] / 255f;
        }

        private static float ReadSnorm8(byte[] buffer, int offset)
        {
            return (buffer[offset] / 255f) * 2f - 1f;
        }
	    }
}
