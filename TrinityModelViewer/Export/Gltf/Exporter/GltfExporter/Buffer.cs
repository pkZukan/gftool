using OpenTK.Mathematics;
using System;
using System.Collections.Generic;

namespace TrinityModelViewer.Export
{
    internal static partial class GltfExporter
    {
        private static int AddAccessorIndices(GltfRoot gltf, BinaryBufferBuilder buffer, uint[] indices)
        {
            uint max = 0;
            for (int i = 0; i < indices.Length; i++) max = Math.Max(max, indices[i]);

            if (max <= ushort.MaxValue)
            {
                var data = new ushort[indices.Length];
                for (int i = 0; i < indices.Length; i++) data[i] = (ushort)indices[i];
                return AddAccessorScalar(gltf, buffer, data, componentType: 5123, target: 34963);
            }

            return AddAccessorScalar(gltf, buffer, indices, componentType: 5125, target: 34963);
        }

        private static int AddAccessorScalar(GltfRoot gltf, BinaryBufferBuilder buffer, ushort[] values, int componentType, int target)
        {
            var bytes = new byte[values.Length * 2];
            Buffer.BlockCopy(values, 0, bytes, 0, bytes.Length);
            int view = AddBufferView(gltf, buffer, bytes, target);
            int accessorIndex = gltf.Accessors.Count;
            gltf.Accessors.Add(new GltfAccessor
            {
                BufferView = view,
                ByteOffset = 0,
                ComponentType = componentType,
                Count = values.Length,
                Type = "SCALAR"
            });
            return accessorIndex;
        }

        private static int AddAccessorScalar(GltfRoot gltf, BinaryBufferBuilder buffer, uint[] values, int componentType, int target)
        {
            var bytes = new byte[values.Length * 4];
            Buffer.BlockCopy(values, 0, bytes, 0, bytes.Length);
            int view = AddBufferView(gltf, buffer, bytes, target);
            int accessorIndex = gltf.Accessors.Count;
            gltf.Accessors.Add(new GltfAccessor
            {
                BufferView = view,
                ByteOffset = 0,
                ComponentType = componentType,
                Count = values.Length,
                Type = "SCALAR"
            });
            return accessorIndex;
        }

        private static int AddAccessorVec2(GltfRoot gltf, BinaryBufferBuilder buffer, Vector2[] values, int? target)
        {
            var bytes = new byte[values.Length * 8];
            int o = 0;
            for (int i = 0; i < values.Length; i++)
            {
                WriteFloat(bytes, ref o, values[i].X);
                WriteFloat(bytes, ref o, values[i].Y);
            }
            int view = AddBufferView(gltf, buffer, bytes, target);
            int accessorIndex = gltf.Accessors.Count;
            gltf.Accessors.Add(new GltfAccessor
            {
                BufferView = view,
                ByteOffset = 0,
                ComponentType = 5126,
                Count = values.Length,
                Type = "VEC2"
            });
            return accessorIndex;
        }

        private static int AddAccessorVec3(GltfRoot gltf, BinaryBufferBuilder buffer, Vector3[] values, int? target, bool includeMinMax = false)
        {
            var bytes = new byte[values.Length * 12];
            int o = 0;
            float minX = float.PositiveInfinity, minY = float.PositiveInfinity, minZ = float.PositiveInfinity;
            float maxX = float.NegativeInfinity, maxY = float.NegativeInfinity, maxZ = float.NegativeInfinity;
            for (int i = 0; i < values.Length; i++)
            {
                var v = values[i];
                if (includeMinMax)
                {
                    minX = Math.Min(minX, v.X); minY = Math.Min(minY, v.Y); minZ = Math.Min(minZ, v.Z);
                    maxX = Math.Max(maxX, v.X); maxY = Math.Max(maxY, v.Y); maxZ = Math.Max(maxZ, v.Z);
                }
                WriteFloat(bytes, ref o, v.X);
                WriteFloat(bytes, ref o, v.Y);
                WriteFloat(bytes, ref o, v.Z);
            }
            int view = AddBufferView(gltf, buffer, bytes, target);
            int accessorIndex = gltf.Accessors.Count;
            var acc = new GltfAccessor
            {
                BufferView = view,
                ByteOffset = 0,
                ComponentType = 5126,
                Count = values.Length,
                Type = "VEC3"
            };
            if (includeMinMax && values.Length > 0)
            {
                acc.Min = new[] { minX, minY, minZ };
                acc.Max = new[] { maxX, maxY, maxZ };
            }
            gltf.Accessors.Add(acc);
            return accessorIndex;
        }

        private static int AddAccessorVec4(GltfRoot gltf, BinaryBufferBuilder buffer, Vector4[] values, int? target)
        {
            var bytes = new byte[values.Length * 16];
            int o = 0;
            for (int i = 0; i < values.Length; i++)
            {
                var v = values[i];
                WriteFloat(bytes, ref o, v.X);
                WriteFloat(bytes, ref o, v.Y);
                WriteFloat(bytes, ref o, v.Z);
                WriteFloat(bytes, ref o, v.W);
            }
            int view = AddBufferView(gltf, buffer, bytes, target);
            int accessorIndex = gltf.Accessors.Count;
            gltf.Accessors.Add(new GltfAccessor
            {
                BufferView = view,
                ByteOffset = 0,
                ComponentType = 5126,
                Count = values.Length,
                Type = "VEC4"
            });
            return accessorIndex;
        }

        private static int AddAccessorUShort4(GltfRoot gltf, BinaryBufferBuilder buffer, Vector4[] values, int? target)
        {
            var bytes = new byte[values.Length * 8];
            int o = 0;
            for (int i = 0; i < values.Length; i++)
            {
                var v = values[i];
                WriteUShort(bytes, ref o, (ushort)Math.Clamp((int)MathF.Round(v.X), 0, ushort.MaxValue));
                WriteUShort(bytes, ref o, (ushort)Math.Clamp((int)MathF.Round(v.Y), 0, ushort.MaxValue));
                WriteUShort(bytes, ref o, (ushort)Math.Clamp((int)MathF.Round(v.Z), 0, ushort.MaxValue));
                WriteUShort(bytes, ref o, (ushort)Math.Clamp((int)MathF.Round(v.W), 0, ushort.MaxValue));
            }
            int view = AddBufferView(gltf, buffer, bytes, target);
            int accessorIndex = gltf.Accessors.Count;
            gltf.Accessors.Add(new GltfAccessor
            {
                BufferView = view,
                ByteOffset = 0,
                ComponentType = 5123,
                Count = values.Length,
                Type = "VEC4"
            });
            return accessorIndex;
        }

        private static int AddAccessorMat4(GltfRoot gltf, BinaryBufferBuilder buffer, Matrix4[] values)
        {
            var bytes = new byte[values.Length * 64];
            int o = 0;
            for (int i = 0; i < values.Length; i++)
            {
                // glTF matrices are column major. OpenTK.Matrix4 stores M11.. as row and col fields.
                // Values are written explicitly in column major order for clarity.
                var m = values[i];
                WriteFloat(bytes, ref o, m.M11); WriteFloat(bytes, ref o, m.M21); WriteFloat(bytes, ref o, m.M31); WriteFloat(bytes, ref o, m.M41);
                WriteFloat(bytes, ref o, m.M12); WriteFloat(bytes, ref o, m.M22); WriteFloat(bytes, ref o, m.M32); WriteFloat(bytes, ref o, m.M42);
                WriteFloat(bytes, ref o, m.M13); WriteFloat(bytes, ref o, m.M23); WriteFloat(bytes, ref o, m.M33); WriteFloat(bytes, ref o, m.M43);
                WriteFloat(bytes, ref o, m.M14); WriteFloat(bytes, ref o, m.M24); WriteFloat(bytes, ref o, m.M34); WriteFloat(bytes, ref o, m.M44);
            }

            int view = AddBufferView(gltf, buffer, bytes, target: null);
            int accessorIndex = gltf.Accessors.Count;
            gltf.Accessors.Add(new GltfAccessor
            {
                BufferView = view,
                ByteOffset = 0,
                ComponentType = 5126,
                Count = values.Length,
                Type = "MAT4"
            });
            return accessorIndex;
        }

        private static int AddAccessorScalarFloat(GltfRoot gltf, BinaryBufferBuilder buffer, float[] values)
        {
            var bytes = new byte[values.Length * 4];
            Buffer.BlockCopy(values, 0, bytes, 0, bytes.Length);
            int view = AddBufferView(gltf, buffer, bytes, target: null);
            int accessorIndex = gltf.Accessors.Count;
            gltf.Accessors.Add(new GltfAccessor
            {
                BufferView = view,
                ByteOffset = 0,
                ComponentType = 5126,
                Count = values.Length,
                Type = "SCALAR"
            });
            return accessorIndex;
        }

        private static int AddBufferView(GltfRoot gltf, BinaryBufferBuilder buffer, byte[] bytes, int? target)
        {
            var (offset, length) = buffer.Append(bytes, align: 4);
            int viewIndex = gltf.BufferViews.Count;
            gltf.BufferViews.Add(new GltfBufferView
            {
                Buffer = 0,
                ByteOffset = offset,
                ByteLength = length,
                Target = target
            });
            return viewIndex;
        }

        private static void WriteFloat(byte[] dst, ref int offset, float value)
        {
            var b = BitConverter.GetBytes(value);
            Buffer.BlockCopy(b, 0, dst, offset, 4);
            offset += 4;
        }

        private static void WriteUShort(byte[] dst, ref int offset, ushort value)
        {
            dst[offset++] = (byte)(value & 0xFF);
            dst[offset++] = (byte)((value >> 8) & 0xFF);
        }

        private sealed class BinaryBufferBuilder
        {
            private readonly List<byte> _data = new List<byte>(1024 * 1024);

            public (int offset, int length) Append(byte[] bytes, int align)
            {
                Align(align);
                int offset = _data.Count;
                _data.AddRange(bytes);
                return (offset, bytes.Length);
            }

            private void Align(int align)
            {
                int pad = (_data.Count % align) == 0 ? 0 : (align - (_data.Count % align));
                for (int i = 0; i < pad; i++) _data.Add(0);
            }

            public byte[] ToArray() => _data.ToArray();
        }
    }
}
