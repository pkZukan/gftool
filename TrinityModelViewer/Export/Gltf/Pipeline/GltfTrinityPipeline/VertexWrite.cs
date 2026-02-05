using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using OpenTK.Mathematics;
using Trinity.Core.Flatbuffers.TR.Model;
using Trinity.Core.Flatbuffers.Utils;
using Trinity.Core.Utils;


namespace TrinityModelViewer.Export
{
    internal static partial class GltfTrinityPipeline
    {
        private static int GetStride(TRVertexDeclaration decl, int vertexElementSizeIndex)
        {
            if (decl.vertexElementSizes == null || decl.vertexElementSizes.Length == 0)
            {
                return 0;
            }

            if (vertexElementSizeIndex < 0 || vertexElementSizeIndex >= decl.vertexElementSizes.Length)
            {
                return 0;
            }

            return (int)decl.vertexElementSizes[vertexElementSizeIndex].Size;
        }

        private static void WriteVertexElement(
            byte[] dst,
            int stride,
            TRVertexElement el,
            TrinityPrimitive prim,
            int vertexCount)
        {
            bool hasNormals = prim.Normals != null && prim.Normals.Length == vertexCount;
            bool hasTangents = prim.Tangents != null && prim.Tangents.Length == vertexCount;
            bool hasColors = prim.Colors != null && prim.Colors.Length == vertexCount;
            bool hasUv0 = prim.Uv0 != null && prim.Uv0.Length == vertexCount;
            bool hasJoints = prim.JointIndices != null && prim.JointIndices.Length == vertexCount;
            bool hasWeights = prim.JointWeights != null && prim.JointWeights.Length == vertexCount;

            for (int v = 0; v < vertexCount; v++)
            {
                int offset = (v * stride) + (int)el.Offset;
                switch (el.Usage)
                {
                    case TRVertexUsage.POSITION:
                        WriteVector3(dst, offset, el.Format, prim.Positions[v]);
                        break;
                    case TRVertexUsage.NORMAL:
                        if (hasNormals)
                        {
                            WriteNormal(dst, offset, el.Format, prim.Normals[v]);
                        }
                        break;
                    case TRVertexUsage.TANGENT:
                        if (hasTangents)
                        {
                            WriteTangent(dst, offset, el.Format, prim.Tangents[v]);
                        }
                        break;
                    case TRVertexUsage.BINORMAL:
                        // Not currently exported from glTF; preserve template data.
                        break;
                    case TRVertexUsage.COLOR:
                        if (hasColors)
                        {
                            WriteColor(dst, offset, el.Format, prim.Colors[v]);
                        }
                        break;
                    case TRVertexUsage.TEX_COORD:
                        if (el.Layer == 0 && hasUv0)
                        {
                            WriteVector2(dst, offset, el.Format, prim.Uv0[v]);
                        }
                        break;
                    case TRVertexUsage.BLEND_INDEX:
                        if (el.Layer == 0 && hasJoints)
                        {
                            WriteBlendIndices(dst, offset, el.Format, prim.JointIndices[v]);
                        }
                        break;
                    case TRVertexUsage.BLEND_WEIGHTS:
                        if (el.Layer == 0 && hasWeights)
                        {
                            WriteBlendWeights(dst, offset, el.Format, prim.JointWeights[v]);
                        }
                        break;
                }
            }
        }

        private static void WriteVector2(byte[] dst, int offset, TRVertexFormat format, Vector2 value)
        {
            switch (format)
            {
                case TRVertexFormat.X32_Y32_FLOAT:
                    BinaryPrimitives.WriteSingleLittleEndian(dst.AsSpan(offset + 0, 4), value.X);
                    BinaryPrimitives.WriteSingleLittleEndian(dst.AsSpan(offset + 4, 4), value.Y);
                    break;
                case TRVertexFormat.W16_X16_Y16_Z16_FLOAT:
                    WriteHalf(dst, offset + 0, value.X);
                    WriteHalf(dst, offset + 2, value.Y);
                    break;
                case TRVertexFormat.W16_X16_Y16_Z16_UNSIGNED_NORMALIZED:
                    WriteUnorm16(dst, offset + 0, value.X);
                    WriteUnorm16(dst, offset + 2, value.Y);
                    break;
                case TRVertexFormat.R8_G8_B8_A8_UNSIGNED_NORMALIZED:
                case TRVertexFormat.W8_X8_Y8_Z8_UNSIGNED:
                    WriteUnorm8(dst, offset + 0, value.X);
                    WriteUnorm8(dst, offset + 1, value.Y);
                    break;
            }
        }

        private static void WriteVector3(byte[] dst, int offset, TRVertexFormat format, Vector3 value)
        {
            switch (format)
            {
                case TRVertexFormat.X32_Y32_Z32_FLOAT:
                    BinaryPrimitives.WriteSingleLittleEndian(dst.AsSpan(offset + 0, 4), value.X);
                    BinaryPrimitives.WriteSingleLittleEndian(dst.AsSpan(offset + 4, 4), value.Y);
                    BinaryPrimitives.WriteSingleLittleEndian(dst.AsSpan(offset + 8, 4), value.Z);
                    break;
                case TRVertexFormat.W32_X32_Y32_Z32_FLOAT:
                    // Stored as WXYZ.
                    BinaryPrimitives.WriteSingleLittleEndian(dst.AsSpan(offset + 0, 4), 0f);
                    BinaryPrimitives.WriteSingleLittleEndian(dst.AsSpan(offset + 4, 4), value.X);
                    BinaryPrimitives.WriteSingleLittleEndian(dst.AsSpan(offset + 8, 4), value.Y);
                    BinaryPrimitives.WriteSingleLittleEndian(dst.AsSpan(offset + 12, 4), value.Z);
                    break;
                case TRVertexFormat.W16_X16_Y16_Z16_FLOAT:
                    WriteHalf(dst, offset + 0, value.X);
                    WriteHalf(dst, offset + 2, value.Y);
                    WriteHalf(dst, offset + 4, value.Z);
                    break;
                case TRVertexFormat.W16_X16_Y16_Z16_UNSIGNED_NORMALIZED:
                    WriteUnorm16(dst, offset + 0, value.X);
                    WriteUnorm16(dst, offset + 2, value.Y);
                    WriteUnorm16(dst, offset + 4, value.Z);
                    break;
                case TRVertexFormat.R8_G8_B8_A8_UNSIGNED_NORMALIZED:
                case TRVertexFormat.W8_X8_Y8_Z8_UNSIGNED:
                    WriteUnorm8(dst, offset + 0, value.X);
                    WriteUnorm8(dst, offset + 1, value.Y);
                    WriteUnorm8(dst, offset + 2, value.Z);
                    break;
            }
        }

        private static void WriteNormal(byte[] dst, int offset, TRVertexFormat format, Vector3 n)
        {
            switch (format)
            {
                case TRVertexFormat.W16_X16_Y16_Z16_FLOAT:
                    WriteHalf(dst, offset + 0, n.X);
                    WriteHalf(dst, offset + 2, n.Y);
                    WriteHalf(dst, offset + 4, n.Z);
                    break;
                case TRVertexFormat.W16_X16_Y16_Z16_UNSIGNED_NORMALIZED:
                    WriteSnorm16(dst, offset + 0, n.X);
                    WriteSnorm16(dst, offset + 2, n.Y);
                    WriteSnorm16(dst, offset + 4, n.Z);
                    break;
                case TRVertexFormat.R8_G8_B8_A8_UNSIGNED_NORMALIZED:
                case TRVertexFormat.W8_X8_Y8_Z8_UNSIGNED:
                    WriteSnorm8(dst, offset + 0, n.X);
                    WriteSnorm8(dst, offset + 1, n.Y);
                    WriteSnorm8(dst, offset + 2, n.Z);
                    break;
                case TRVertexFormat.X32_Y32_Z32_FLOAT:
                    BinaryPrimitives.WriteSingleLittleEndian(dst.AsSpan(offset + 0, 4), n.X);
                    BinaryPrimitives.WriteSingleLittleEndian(dst.AsSpan(offset + 4, 4), n.Y);
                    BinaryPrimitives.WriteSingleLittleEndian(dst.AsSpan(offset + 8, 4), n.Z);
                    break;
            }
        }

        private static void WriteTangent(byte[] dst, int offset, TRVertexFormat format, Vector4 t)
        {
            switch (format)
            {
                case TRVertexFormat.W32_X32_Y32_Z32_FLOAT:
                    // Stored as WXYZ.
                    BinaryPrimitives.WriteSingleLittleEndian(dst.AsSpan(offset + 0, 4), t.W);
                    BinaryPrimitives.WriteSingleLittleEndian(dst.AsSpan(offset + 4, 4), t.X);
                    BinaryPrimitives.WriteSingleLittleEndian(dst.AsSpan(offset + 8, 4), t.Y);
                    BinaryPrimitives.WriteSingleLittleEndian(dst.AsSpan(offset + 12, 4), t.Z);
                    break;
                case TRVertexFormat.X32_Y32_Z32_FLOAT:
                    BinaryPrimitives.WriteSingleLittleEndian(dst.AsSpan(offset + 0, 4), t.X);
                    BinaryPrimitives.WriteSingleLittleEndian(dst.AsSpan(offset + 4, 4), t.Y);
                    BinaryPrimitives.WriteSingleLittleEndian(dst.AsSpan(offset + 8, 4), t.Z);
                    break;
                case TRVertexFormat.W16_X16_Y16_Z16_FLOAT:
                    WriteHalf(dst, offset + 0, t.X);
                    WriteHalf(dst, offset + 2, t.Y);
                    WriteHalf(dst, offset + 4, t.Z);
                    WriteHalf(dst, offset + 6, t.W);
                    break;
                case TRVertexFormat.W16_X16_Y16_Z16_UNSIGNED_NORMALIZED:
                    WriteSnorm16(dst, offset + 0, t.X);
                    WriteSnorm16(dst, offset + 2, t.Y);
                    WriteSnorm16(dst, offset + 4, t.Z);
                    WriteSnorm16(dst, offset + 6, t.W);
                    break;
                case TRVertexFormat.R8_G8_B8_A8_UNSIGNED_NORMALIZED:
                case TRVertexFormat.W8_X8_Y8_Z8_UNSIGNED:
                    WriteSnorm8(dst, offset + 0, t.X);
                    WriteSnorm8(dst, offset + 1, t.Y);
                    WriteSnorm8(dst, offset + 2, t.Z);
                    WriteSnorm8(dst, offset + 3, t.W);
                    break;
            }
        }

        private static void WriteColor(byte[] dst, int offset, TRVertexFormat format, Vector4 c)
        {
            switch (format)
            {
                case TRVertexFormat.R8_G8_B8_A8_UNSIGNED_NORMALIZED:
                case TRVertexFormat.W8_X8_Y8_Z8_UNSIGNED:
                    WriteUnorm8(dst, offset + 0, c.X);
                    WriteUnorm8(dst, offset + 1, c.Y);
                    WriteUnorm8(dst, offset + 2, c.Z);
                    WriteUnorm8(dst, offset + 3, c.W);
                    break;
                case TRVertexFormat.W16_X16_Y16_Z16_UNSIGNED_NORMALIZED:
                    WriteUnorm16(dst, offset + 0, c.X);
                    WriteUnorm16(dst, offset + 2, c.Y);
                    WriteUnorm16(dst, offset + 4, c.Z);
                    WriteUnorm16(dst, offset + 6, c.W);
                    break;
                case TRVertexFormat.W16_X16_Y16_Z16_FLOAT:
                    WriteHalf(dst, offset + 0, c.X);
                    WriteHalf(dst, offset + 2, c.Y);
                    WriteHalf(dst, offset + 4, c.Z);
                    WriteHalf(dst, offset + 6, c.W);
                    break;
            }
        }

        private static void WriteBlendIndices(byte[] dst, int offset, TRVertexFormat format, Vector4i joints)
        {
            switch (format)
            {
                case TRVertexFormat.W8_X8_Y8_Z8_UNSIGNED:
                    dst[offset + 0] = (byte)Math.Clamp(joints.X, 0, 255);
                    dst[offset + 1] = (byte)Math.Clamp(joints.Y, 0, 255);
                    dst[offset + 2] = (byte)Math.Clamp(joints.Z, 0, 255);
                    dst[offset + 3] = (byte)Math.Clamp(joints.W, 0, 255);
                    break;
                case TRVertexFormat.W16_X16_Y16_Z16_UNSIGNED_NORMALIZED:
                    BinaryPrimitives.WriteUInt16LittleEndian(dst.AsSpan(offset + 0, 2), (ushort)Math.Clamp(joints.X, 0, 65535));
                    BinaryPrimitives.WriteUInt16LittleEndian(dst.AsSpan(offset + 2, 2), (ushort)Math.Clamp(joints.Y, 0, 65535));
                    BinaryPrimitives.WriteUInt16LittleEndian(dst.AsSpan(offset + 4, 2), (ushort)Math.Clamp(joints.Z, 0, 65535));
                    BinaryPrimitives.WriteUInt16LittleEndian(dst.AsSpan(offset + 6, 2), (ushort)Math.Clamp(joints.W, 0, 65535));
                    break;
                case TRVertexFormat.W32_X32_Y32_Z32_UNSIGNED:
                    BinaryPrimitives.WriteUInt32LittleEndian(dst.AsSpan(offset + 0, 4), (uint)Math.Max(joints.X, 0));
                    BinaryPrimitives.WriteUInt32LittleEndian(dst.AsSpan(offset + 4, 4), (uint)Math.Max(joints.Y, 0));
                    BinaryPrimitives.WriteUInt32LittleEndian(dst.AsSpan(offset + 8, 4), (uint)Math.Max(joints.Z, 0));
                    BinaryPrimitives.WriteUInt32LittleEndian(dst.AsSpan(offset + 12, 4), (uint)Math.Max(joints.W, 0));
                    break;
            }
        }

        private static void WriteBlendWeights(byte[] dst, int offset, TRVertexFormat format, Vector4 weights)
        {
            switch (format)
            {
                case TRVertexFormat.W16_X16_Y16_Z16_UNSIGNED_NORMALIZED:
                    WriteUnorm16(dst, offset + 0, weights.X);
                    WriteUnorm16(dst, offset + 2, weights.Y);
                    WriteUnorm16(dst, offset + 4, weights.Z);
                    WriteUnorm16(dst, offset + 6, weights.W);
                    break;
                case TRVertexFormat.R8_G8_B8_A8_UNSIGNED_NORMALIZED:
                case TRVertexFormat.W8_X8_Y8_Z8_UNSIGNED:
                    WriteUnorm8(dst, offset + 0, weights.X);
                    WriteUnorm8(dst, offset + 1, weights.Y);
                    WriteUnorm8(dst, offset + 2, weights.Z);
                    WriteUnorm8(dst, offset + 3, weights.W);
                    break;
                case TRVertexFormat.W32_X32_Y32_Z32_FLOAT:
                    BinaryPrimitives.WriteSingleLittleEndian(dst.AsSpan(offset + 0, 4), weights.X);
                    BinaryPrimitives.WriteSingleLittleEndian(dst.AsSpan(offset + 4, 4), weights.Y);
                    BinaryPrimitives.WriteSingleLittleEndian(dst.AsSpan(offset + 8, 4), weights.Z);
                    BinaryPrimitives.WriteSingleLittleEndian(dst.AsSpan(offset + 12, 4), weights.W);
                    break;
            }
        }

        private static void WriteHalf(byte[] dst, int offset, float value)
        {
            ushort raw = BitConverter.HalfToUInt16Bits((global::System.Half)value);
            BinaryPrimitives.WriteUInt16LittleEndian(dst.AsSpan(offset, 2), raw);
        }

        private static void WriteUnorm16(byte[] dst, int offset, float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value)) value = 0;
            value = Math.Clamp(value, 0f, 1f);
            ushort q = (ushort)Math.Clamp((int)MathF.Round(value * 65535f), 0, 65535);
            BinaryPrimitives.WriteUInt16LittleEndian(dst.AsSpan(offset, 2), q);
        }

        private static void WriteSnorm16(byte[] dst, int offset, float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value)) value = 0;
            value = Math.Clamp(value, -1f, 1f);
            float unorm = (value * 0.5f) + 0.5f;
            ushort q = (ushort)Math.Clamp((int)MathF.Round(unorm * 65535f), 0, 65535);
            BinaryPrimitives.WriteUInt16LittleEndian(dst.AsSpan(offset, 2), q);
        }

        private static void WriteUnorm8(byte[] dst, int offset, float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value)) value = 0;
            value = Math.Clamp(value, 0f, 1f);
            dst[offset] = (byte)Math.Clamp((int)MathF.Round(value * 255f), 0, 255);
        }

        private static void WriteSnorm8(byte[] dst, int offset, float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value)) value = 0;
            value = Math.Clamp(value, -1f, 1f);
            float unorm = (value * 0.5f) + 0.5f;
            dst[offset] = (byte)Math.Clamp((int)MathF.Round(unorm * 255f), 0, 255);
        }
    }
}
