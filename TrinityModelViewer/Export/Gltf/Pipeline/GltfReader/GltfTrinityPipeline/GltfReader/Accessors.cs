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
        private static partial class GltfReader
        {
            private static byte[][] LoadBuffers(JsonElement root, string baseDir)
            {
                var buffers = GetArray(root, "buffers");
                var result = new byte[buffers.Count][];
                for (int i = 0; i < buffers.Count; i++)
                {
                    var b = buffers[i];
                    var uri = TryGetString(b, "uri");
                    if (string.IsNullOrWhiteSpace(uri))
                    {
                        result[i] = Array.Empty<byte>();
                        continue;
                    }

                    if (uri.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                    {
                        // data:application/octet-stream;base64,....
                        int comma = uri.IndexOf(',');
                        if (comma < 0)
                        {
                            result[i] = Array.Empty<byte>();
                            continue;
                        }
                        var base64 = uri.Substring(comma + 1);
                        result[i] = Convert.FromBase64String(base64);
                        continue;
                    }

                    var path = Path.IsPathRooted(uri) ? uri : Path.Combine(baseDir, uri);
                    result[i] = File.Exists(path) ? File.ReadAllBytes(path) : Array.Empty<byte>();
                }
                return result;
            }

            private static Vector3[] ReadVec3(GltfDocument doc, int accessorIndex)
            {
                var a = GetAccessor(doc.Json.RootElement, accessorIndex);
                if (a.ComponentType != 5126 || a.Type != "VEC3")
                {
                    return Array.Empty<Vector3>();
                }

                var span = GetAccessorSpan(doc, a, out int stride);
                if (span.Length == 0)
                {
                    return Array.Empty<Vector3>();
                }

                var result = new Vector3[a.Count];
                for (int i = 0; i < a.Count; i++)
                {
                    int o = i * stride;
                    result[i] = new Vector3(
                        BinaryPrimitives.ReadSingleLittleEndian(span.Slice(o + 0, 4)),
                        BinaryPrimitives.ReadSingleLittleEndian(span.Slice(o + 4, 4)),
                        BinaryPrimitives.ReadSingleLittleEndian(span.Slice(o + 8, 4)));
                }
                return result;
            }

            private static Vector2[] ReadVec2(GltfDocument doc, int accessorIndex)
            {
                var a = GetAccessor(doc.Json.RootElement, accessorIndex);
                if (a.ComponentType != 5126 || a.Type != "VEC2")
                {
                    return Array.Empty<Vector2>();
                }

                var span = GetAccessorSpan(doc, a, out int stride);
                if (span.Length == 0)
                {
                    return Array.Empty<Vector2>();
                }

                var result = new Vector2[a.Count];
                for (int i = 0; i < a.Count; i++)
                {
                    int o = i * stride;
                    result[i] = new Vector2(
                        BinaryPrimitives.ReadSingleLittleEndian(span.Slice(o + 0, 4)),
                        BinaryPrimitives.ReadSingleLittleEndian(span.Slice(o + 4, 4)));
                }
                return result;
            }

            private static Vector4[] ReadVec4(GltfDocument doc, int accessorIndex)
            {
                var a = GetAccessor(doc.Json.RootElement, accessorIndex);
                if (a.ComponentType != 5126 || a.Type != "VEC4")
                {
                    return Array.Empty<Vector4>();
                }

                var span = GetAccessorSpan(doc, a, out int stride);
                if (span.Length == 0)
                {
                    return Array.Empty<Vector4>();
                }

                var result = new Vector4[a.Count];
                for (int i = 0; i < a.Count; i++)
                {
                    int o = i * stride;
                    result[i] = new Vector4(
                        BinaryPrimitives.ReadSingleLittleEndian(span.Slice(o + 0, 4)),
                        BinaryPrimitives.ReadSingleLittleEndian(span.Slice(o + 4, 4)),
                        BinaryPrimitives.ReadSingleLittleEndian(span.Slice(o + 8, 4)),
                        BinaryPrimitives.ReadSingleLittleEndian(span.Slice(o + 12, 4)));
                }
                return result;
            }

            private static Vector4[] ReadColorVec4(GltfDocument doc, int accessorIndex)
            {
                var a = GetAccessor(doc.Json.RootElement, accessorIndex);
                if (a.Type != "VEC3" && a.Type != "VEC4")
                {
                    return Array.Empty<Vector4>();
                }

                var span = GetAccessorSpan(doc, a, out int stride);
                if (span.Length == 0)
                {
                    return Array.Empty<Vector4>();
                }

                int compSize = a.ComponentType switch
                {
                    5126 => 4,
                    5121 => 1,
                    5123 => 2,
                    _ => 0
                };
                if (compSize == 0)
                {
                    return Array.Empty<Vector4>();
                }

                int comps = a.Type == "VEC3" ? 3 : 4;
                var result = new Vector4[a.Count];
                for (int i = 0; i < a.Count; i++)
                {
                    int o = i * stride;
                    float r = ReadColorComponent(span, o + (0 * compSize), a.ComponentType);
                    float g = ReadColorComponent(span, o + (1 * compSize), a.ComponentType);
                    float b = ReadColorComponent(span, o + (2 * compSize), a.ComponentType);
                    float aVal = comps == 4 ? ReadColorComponent(span, o + (3 * compSize), a.ComponentType) : 1f;
                    result[i] = new Vector4(r, g, b, aVal);
                }
                return result;
            }

            private static float ReadColorComponent(ReadOnlySpan<byte> span, int byteOffset, int componentType)
            {
                return componentType switch
                {
                    5126 => BinaryPrimitives.ReadSingleLittleEndian(span.Slice(byteOffset, 4)),
                    5121 => span[byteOffset] / 255f,
                    5123 => BinaryPrimitives.ReadUInt16LittleEndian(span.Slice(byteOffset, 2)) / 65535f,
                    _ => 0f
                };
            }

            private static Vector4i[] ReadVec4UShort(GltfDocument doc, int accessorIndex)
            {
                var a = GetAccessor(doc.Json.RootElement, accessorIndex);
                if (a.Type != "VEC4")
                {
                    return Array.Empty<Vector4i>();
                }

                var span = GetAccessorSpan(doc, a, out int stride);
                if (span.Length == 0)
                {
                    return Array.Empty<Vector4i>();
                }

                var result = new Vector4i[a.Count];
                if (a.ComponentType == 5121) // UNSIGNED_BYTE
                {
                    for (int i = 0; i < a.Count; i++)
                    {
                        int o = i * stride;
                        result[i] = new Vector4i(span[o + 0], span[o + 1], span[o + 2], span[o + 3]);
                    }
                    return result;
                }

                if (a.ComponentType != 5123) // UNSIGNED_SHORT
                {
                    return Array.Empty<Vector4i>();
                }

                for (int i = 0; i < a.Count; i++)
                {
                    int o = i * stride;
                    result[i] = new Vector4i(
                        BinaryPrimitives.ReadUInt16LittleEndian(span.Slice(o + 0, 2)),
                        BinaryPrimitives.ReadUInt16LittleEndian(span.Slice(o + 2, 2)),
                        BinaryPrimitives.ReadUInt16LittleEndian(span.Slice(o + 4, 2)),
                        BinaryPrimitives.ReadUInt16LittleEndian(span.Slice(o + 6, 2)));
                }
                return result;
            }

            private static Vector4[] ReadWeights(GltfDocument doc, int accessorIndex)
            {
                var a = GetAccessor(doc.Json.RootElement, accessorIndex);
                if (a.Type != "VEC4")
                {
                    return Array.Empty<Vector4>();
                }

                var span = GetAccessorSpan(doc, a, out int stride);
                if (span.Length == 0)
                {
                    return Array.Empty<Vector4>();
                }

                var result = new Vector4[a.Count];
                if (a.ComponentType == 5126) // FLOAT
                {
                    for (int i = 0; i < a.Count; i++)
                    {
                        int o = i * stride;
                        result[i] = new Vector4(
                            BinaryPrimitives.ReadSingleLittleEndian(span.Slice(o + 0, 4)),
                            BinaryPrimitives.ReadSingleLittleEndian(span.Slice(o + 4, 4)),
                            BinaryPrimitives.ReadSingleLittleEndian(span.Slice(o + 8, 4)),
                            BinaryPrimitives.ReadSingleLittleEndian(span.Slice(o + 12, 4)));
                    }
                    return result;
                }

                // Normalized integer weights.
                if (a.ComponentType == 5121) // UNSIGNED_BYTE
                {
                    for (int i = 0; i < a.Count; i++)
                    {
                        int o = i * stride;
                        result[i] = new Vector4(span[o + 0] / 255f, span[o + 1] / 255f, span[o + 2] / 255f, span[o + 3] / 255f);
                    }
                    return result;
                }

                if (a.ComponentType == 5123) // UNSIGNED_SHORT
                {
                    for (int i = 0; i < a.Count; i++)
                    {
                        int o = i * stride;
                        result[i] = new Vector4(
                            BinaryPrimitives.ReadUInt16LittleEndian(span.Slice(o + 0, 2)) / 65535f,
                            BinaryPrimitives.ReadUInt16LittleEndian(span.Slice(o + 2, 2)) / 65535f,
                            BinaryPrimitives.ReadUInt16LittleEndian(span.Slice(o + 4, 2)) / 65535f,
                            BinaryPrimitives.ReadUInt16LittleEndian(span.Slice(o + 6, 2)) / 65535f);
                    }
                    return result;
                }

                return Array.Empty<Vector4>();
            }

            private static uint[] ReadIndices(GltfDocument doc, int accessorIndex)
            {
                var a = GetAccessor(doc.Json.RootElement, accessorIndex);
                if (a.Type != "SCALAR")
                {
                    return Array.Empty<uint>();
                }

                var span = GetAccessorSpan(doc, a, out int stride);
                if (span.Length == 0)
                {
                    return Array.Empty<uint>();
                }

                var result = new uint[a.Count];
                if (a.ComponentType == 5123) // UNSIGNED_SHORT
                {
                    for (int i = 0; i < a.Count; i++)
                    {
                        result[i] = BinaryPrimitives.ReadUInt16LittleEndian(span.Slice(i * stride, 2));
                    }
                    return result;
                }

                if (a.ComponentType == 5125) // UNSIGNED_INT
                {
                    for (int i = 0; i < a.Count; i++)
                    {
                        result[i] = BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(i * stride, 4));
                    }
                    return result;
                }

                if (a.ComponentType == 5121) // UNSIGNED_BYTE
                {
                    for (int i = 0; i < a.Count; i++)
                    {
                        result[i] = span[i * stride];
                    }
                    return result;
                }

                return Array.Empty<uint>();
            }

            private readonly struct AccessorInfo
            {
                public AccessorInfo(int bufferView, int byteOffset, int componentType, int count, string type)
                {
                    BufferView = bufferView;
                    ByteOffset = byteOffset;
                    ComponentType = componentType;
                    Count = count;
                    Type = type;
                }

                public int BufferView { get; }
                public int ByteOffset { get; }
                public int ComponentType { get; }
                public int Count { get; }
                public string Type { get; }
            }

            private static AccessorInfo GetAccessor(JsonElement root, int accessorIndex)
            {
                var accessors = GetArray(root, "accessors");
                if (accessorIndex < 0 || accessorIndex >= accessors.Count)
                {
                    return default;
                }

                var a = accessors[accessorIndex];
                int bufferView = TryGetInt(a, "bufferView", out int bv) ? bv : -1;
                int byteOffset = TryGetInt(a, "byteOffset", out int bo) ? bo : 0;
                int componentType = TryGetInt(a, "componentType", out int ct) ? ct : 0;
                int count = TryGetInt(a, "count", out int c) ? c : 0;
                string type = TryGetString(a, "type") ?? string.Empty;
                return new AccessorInfo(bufferView, byteOffset, componentType, count, type);
            }

            private static ReadOnlySpan<byte> GetAccessorSpan(GltfDocument doc, AccessorInfo accessor, out int stride)
            {
                stride = 0;
                if (accessor.BufferView < 0)
                {
                    return ReadOnlySpan<byte>.Empty;
                }

                var root = doc.Json.RootElement;
                var bufferViews = GetArray(root, "bufferViews");
                if (accessor.BufferView >= bufferViews.Count)
                {
                    return ReadOnlySpan<byte>.Empty;
                }

                var view = bufferViews[accessor.BufferView];
                int bufferIndex = TryGetInt(view, "buffer", out int bi) ? bi : 0;
                int viewOffset = TryGetInt(view, "byteOffset", out int vo) ? vo : 0;
                int viewLength = TryGetInt(view, "byteLength", out int vl) ? vl : 0;
                int viewStride = TryGetInt(view, "byteStride", out int vs) ? vs : 0;

                if (bufferIndex < 0 || bufferIndex >= doc.Buffers.Length)
                {
                    return ReadOnlySpan<byte>.Empty;
                }

                stride = viewStride;
                if (stride <= 0)
                {
                    stride = accessor.Type switch
                    {
                        "SCALAR" => ComponentTypeSize(accessor.ComponentType),
                        "VEC2" => ComponentTypeSize(accessor.ComponentType) * 2,
                        "VEC3" => ComponentTypeSize(accessor.ComponentType) * 3,
                        "VEC4" => ComponentTypeSize(accessor.ComponentType) * 4,
                        _ => 0
                    };
                }

                int start = viewOffset + accessor.ByteOffset;
                if (start < 0 || viewLength <= 0)
                {
                    return ReadOnlySpan<byte>.Empty;
                }

                var buffer = doc.Buffers[bufferIndex];
                if (start >= buffer.Length)
                {
                    return ReadOnlySpan<byte>.Empty;
                }

                int available = Math.Min(viewLength - accessor.ByteOffset, buffer.Length - start);
                if (available <= 0)
                {
                    return ReadOnlySpan<byte>.Empty;
                }

                return buffer.AsSpan(start, available);
            }

            private static int ComponentTypeSize(int componentType)
            {
                return componentType switch
                {
                    5120 => 1, // BYTE
                    5121 => 1, // UNSIGNED_BYTE
                    5122 => 2, // SHORT
                    5123 => 2, // UNSIGNED_SHORT
                    5125 => 4, // UNSIGNED_INT
                    5126 => 4, // FLOAT
                    _ => 0
                };
            }

            private static List<JsonElement> GetArray(JsonElement parent, string name)
            {
                if (!parent.TryGetProperty(name, out var arr) || arr.ValueKind != JsonValueKind.Array)
                {
                    return new List<JsonElement>();
                }

                return arr.EnumerateArray().ToList();
            }

            private static List<int> GetIntArray(JsonElement parent, string name)
            {
                if (!parent.TryGetProperty(name, out var arr) || arr.ValueKind != JsonValueKind.Array)
                {
                    return new List<int>();
                }
                var list = new List<int>();
                foreach (var el in arr.EnumerateArray())
                {
                    if (el.ValueKind == JsonValueKind.Number && el.TryGetInt32(out int v))
                    {
                        list.Add(v);
                    }
                }
                return list;
            }

            private static bool TryGetInt(JsonElement obj, string name, out int value)
            {
                value = 0;
                if (!obj.TryGetProperty(name, out var p) || p.ValueKind != JsonValueKind.Number)
                {
                    return false;
                }
                return p.TryGetInt32(out value);
            }

            private static bool TryGetInt(JsonElement obj, string name, out int? value)
            {
                value = null;
                if (!obj.TryGetProperty(name, out var p) || p.ValueKind != JsonValueKind.Number)
                {
                    return false;
                }
                if (!p.TryGetInt32(out int v))
                {
                    return false;
                }
                value = v;
                return true;
            }

            private static string? TryGetString(JsonElement obj, string name)
            {
                if (!obj.TryGetProperty(name, out var p) || p.ValueKind != JsonValueKind.String)
                {
                    return null;
                }
                return p.GetString();
            }
        }
    }
}
