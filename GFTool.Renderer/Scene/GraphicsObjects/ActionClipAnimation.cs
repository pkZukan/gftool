using OpenTK.Mathematics;
using System.Text;

namespace GFTool.Renderer.Scene.GraphicsObjects
{
    public sealed class ActionClipAnimation
    {
        public sealed class VisibilityTrack
        {
            private readonly byte kind;
            private readonly bool fixedValue;
            private readonly byte[] packedValues;
            private readonly ushort[] frames16;
            private readonly byte[] frames8;
            private readonly int frameCount;

            internal VisibilityTrack(
                string targetName,
                byte kind,
                bool fixedValue,
                byte[] packedValues,
                ushort[] frames16,
                byte[] frames8,
                int frameCount)
            {
                TargetName = targetName;
                this.kind = kind;
                this.fixedValue = fixedValue;
                this.packedValues = packedValues;
                this.frames16 = frames16;
                this.frames8 = frames8;
                this.frameCount = frameCount;
            }

            public string TargetName { get; }
            public byte Kind => kind;

            public bool Sample(float frame)
            {
                return kind switch
                {
                    1 => fixedValue,
                    2 => ReadPackedBit(Math.Clamp((int)MathF.Floor(frame), 0, Math.Max(0, frameCount - 1))),
                    3 => ReadPackedBit(FindKeyIndex(frames16, frame)),
                    4 => ReadPackedBit(FindKeyIndex(frames8, frame)),
                    _ => fixedValue
                };
            }

            private bool ReadPackedBit(int index)
            {
                if (index < 0 || packedValues.Length == 0)
                {
                    return fixedValue;
                }

                int byteIndex = index / 8;
                int bitIndex = index % 8;
                if (byteIndex >= packedValues.Length)
                {
                    return ReadPackedBit(packedValues.Length * 8 - 1);
                }

                return ((packedValues[byteIndex] >> bitIndex) & 1) != 0;
            }

            private static int FindKeyIndex<T>(IReadOnlyList<T> frames, float frame) where T : struct
            {
                if (frames.Count == 0)
                {
                    return -1;
                }

                int result = 0;
                for (int i = 1; i < frames.Count; i++)
                {
                    float key = frames[i] switch
                    {
                        byte value => value,
                        ushort value => value,
                        _ => 0f
                    };
                    if (key > frame)
                    {
                        break;
                    }
                    result = i;
                }
                return result;
            }
        }

        public sealed class Vector4ParameterTrack
        {
            private readonly FloatCurve[] components;
            private readonly bool stepAtlasOffsets;

            internal Vector4ParameterTrack(string targetName, string materialName, string parameterName, FloatCurve[] components)
            {
                TargetName = targetName;
                MaterialName = materialName;
                ParameterName = parameterName;
                this.components = components;
                stepAtlasOffsets = parameterName.StartsWith("UVScaleOffset", StringComparison.OrdinalIgnoreCase) &&
                    (components[2].LooksLikeAtlasSelector() || components[3].LooksLikeAtlasSelector());
            }

            public string TargetName { get; }
            public string MaterialName { get; }
            public string ParameterName { get; }

            public Vector4 Sample(float frame, Vector4 fallback)
            {
                return new Vector4(
                    components[0].Sample(frame, fallback.X),
                    components[1].Sample(frame, fallback.Y),
                    stepAtlasOffsets ? components[2].SampleStep(frame, fallback.Z) : components[2].Sample(frame, fallback.Z),
                    stepAtlasOffsets ? components[3].SampleStep(frame, fallback.W) : components[3].Sample(frame, fallback.W));
            }
        }

        internal sealed class FloatCurve
        {
            private readonly Keyframe[] keys;

            internal FloatCurve(IEnumerable<Keyframe> keys)
            {
                this.keys = keys.OrderBy(key => key.Frame).ToArray();
            }

            public float Sample(float frame, float fallback)
            {
                if (keys.Length == 0)
                {
                    return fallback;
                }
                if (frame <= keys[0].Frame)
                {
                    return keys[0].Value;
                }
                if (frame >= keys[^1].Frame)
                {
                    return keys[^1].Value;
                }

                for (int i = 0; i < keys.Length - 1; i++)
                {
                    var left = keys[i];
                    var right = keys[i + 1];
                    if (frame < left.Frame || frame > right.Frame)
                    {
                        continue;
                    }

                    float span = right.Frame - left.Frame;
                    if (span <= 0.00001f)
                    {
                        return right.Value;
                    }
                    float amount = Math.Clamp((frame - left.Frame) / span, 0f, 1f);
                    return MathHelper.Lerp(left.Value, right.Value, amount);
                }

                return keys[^1].Value;
            }

            public float SampleStep(float frame, float fallback)
            {
                if (keys.Length == 0) return fallback;
                float value = keys[0].Value;
                for (int i = 1; i < keys.Length; i++)
                {
                    if (keys[i].Frame > frame) break;
                    value = keys[i].Value;
                }
                return value;
            }

            public bool LooksLikeAtlasSelector()
            {
                if (keys.Length < 2) return false;
                float min = keys.Min(key => key.Value);
                float max = keys.Max(key => key.Value);
                if (max - min < 0.2f) return false;

                for (int divisions = 2; divisions <= 8; divisions++)
                {
                    if (keys.All(key => MathF.Abs(key.Value * divisions - MathF.Round(key.Value * divisions)) < 0.001f))
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        internal readonly record struct Keyframe(float Frame, float Value);

        private ActionClipAnimation(
            string sourcePath,
            int frameCount,
            int frameRate,
            IReadOnlyList<string> targetNames,
            IReadOnlyList<VisibilityTrack> visibilityTracks,
            IReadOnlyList<Vector4ParameterTrack> vector4Tracks)
        {
            SourcePath = sourcePath;
            FrameCount = frameCount;
            FrameRate = frameRate;
            TargetNames = targetNames;
            VisibilityTracks = visibilityTracks;
            Vector4Tracks = vector4Tracks;
        }

        public string SourcePath { get; }
        public int FrameCount { get; }
        public int FrameRate { get; }
        public IReadOnlyList<string> TargetNames { get; }
        public IReadOnlyList<VisibilityTrack> VisibilityTracks { get; }
        public IReadOnlyList<Vector4ParameterTrack> Vector4Tracks { get; }

        public static ActionClipAnimation Load(string path)
        {
            var reader = new FlatBufferReader(File.ReadAllBytes(path));
            int root = reader.RootTable;
            int info = reader.GetTable(root, 0);
            int frameCount = info != 0 ? reader.GetInt32(info, 1, 0) : 0;
            int frameRate = info != 0 ? reader.GetInt32(info, 2, 0) : 0;

            var targetNames = new List<string>();
            var visibilityTracks = new List<VisibilityTrack>();
            var vector4Tracks = new List<Vector4ParameterTrack>();

            if (reader.TryGetVector(root, 1, out int targetVector, out int targetCount))
            {
                for (int i = 0; i < targetCount; i++)
                {
                    int target = reader.GetTableVectorElement(targetVector, i);
                    if (target == 0)
                    {
                        continue;
                    }

                    string targetName = reader.GetString(target, 0) ?? string.Empty;
                    if (!string.IsNullOrWhiteSpace(targetName))
                    {
                        targetNames.Add(targetName);
                    }

                    ParseVisibilityTrack(reader, target, targetName, frameCount, visibilityTracks);
                    ParseMaterialTracks(reader, target, targetName, vector4Tracks);
                }
            }

            return new ActionClipAnimation(
                path,
                frameCount,
                frameRate,
                targetNames.Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
                visibilityTracks,
                vector4Tracks);
        }

        private static void ParseVisibilityTrack(
            FlatBufferReader reader,
            int target,
            string targetName,
            int frameCount,
            List<VisibilityTrack> tracks)
        {
            int container = reader.GetTable(target, 5);
            int union = container != 0 ? reader.GetTable(container, 2) : 0;
            if (union == 0)
            {
                return;
            }

            byte kind = reader.GetByte(union, 0, 0);
            int value = reader.GetTable(union, 1);
            if (kind == 0 || value == 0)
            {
                return;
            }

            bool fixedValue = false;
            byte[] packedValues = Array.Empty<byte>();
            ushort[] frames16 = Array.Empty<ushort>();
            byte[] frames8 = Array.Empty<byte>();

            switch (kind)
            {
                case 1:
                    fixedValue = reader.GetByte(value, 0, 0) != 0;
                    break;
                case 2:
                    packedValues = reader.GetByteVector(value, 0);
                    break;
                case 3:
                    frames16 = reader.GetUInt16Vector(value, 0);
                    packedValues = reader.GetByteVector(value, 1);
                    break;
                case 4:
                    frames8 = reader.GetByteVector(value, 0);
                    packedValues = reader.GetByteVector(value, 1);
                    break;
                default:
                    return;
            }

            tracks.Add(new VisibilityTrack(targetName, kind, fixedValue, packedValues, frames16, frames8, frameCount));
        }

        private static void ParseMaterialTracks(
            FlatBufferReader reader,
            int target,
            string targetName,
            List<Vector4ParameterTrack> tracks)
        {
            int container = reader.GetTable(target, 4);
            if (container == 0 || !reader.TryGetVector(container, 2, out int materialVector, out int materialCount))
            {
                return;
            }

            for (int i = 0; i < materialCount; i++)
            {
                int material = reader.GetTableVectorElement(materialVector, i);
                if (material == 0)
                {
                    continue;
                }

                string materialName = reader.GetString(material, 0) ?? string.Empty;
                if (!reader.TryGetVector(material, 2, out int parameterVector, out int parameterCount))
                {
                    continue;
                }

                for (int p = 0; p < parameterCount; p++)
                {
                    int parameter = reader.GetTableVectorElement(parameterVector, p);
                    if (parameter == 0)
                    {
                        continue;
                    }

                    string parameterName = reader.GetString(parameter, 0) ?? string.Empty;
                    int vectorTrack = reader.GetTable(parameter, 1);
                    if (string.IsNullOrWhiteSpace(parameterName) || vectorTrack == 0)
                    {
                        continue;
                    }

                    var components = new FloatCurve[4];
                    for (int component = 0; component < components.Length; component++)
                    {
                        components[component] = ParseFloatCurve(reader, reader.GetTable(vectorTrack, component));
                    }
                    tracks.Add(new Vector4ParameterTrack(targetName, materialName, parameterName, components));
                }
            }
        }

        private static FloatCurve ParseFloatCurve(FlatBufferReader reader, int curve)
        {
            var keys = new List<Keyframe>();
            if (curve == 0 || !reader.TryGetVector(curve, 0, out int keyVector, out int keyCount))
            {
                return new FloatCurve(keys);
            }

            for (int i = 0; i < keyCount; i++)
            {
                int key = reader.GetTableVectorElement(keyVector, i);
                if (key == 0)
                {
                    continue;
                }
                keys.Add(new Keyframe(reader.GetSingle(key, 0, 0f), reader.GetSingle(key, 1, 0f)));
            }
            return new FloatCurve(keys);
        }

        private sealed class FlatBufferReader
        {
            private readonly byte[] bytes;

            public FlatBufferReader(byte[] bytes)
            {
                this.bytes = bytes;
                RootTable = ReadInt32(0);
                if (!IsTable(RootTable))
                {
                    throw new InvalidDataException("The TRACM root table is invalid.");
                }
            }

            public int RootTable { get; }

            public int GetTable(int table, int fieldIndex)
            {
                int field = GetField(table, fieldIndex);
                if (field == 0)
                {
                    return 0;
                }
                int target = field + ReadInt32(field);
                return IsTable(target) ? target : 0;
            }

            public string? GetString(int table, int fieldIndex)
            {
                int field = GetField(table, fieldIndex);
                if (field == 0)
                {
                    return null;
                }
                int target = field + ReadInt32(field);
                int length = ReadInt32(target);
                if (length < 0 || length > 4096 || !HasBytes(target + 4, length))
                {
                    return null;
                }
                return Encoding.UTF8.GetString(bytes, target + 4, length);
            }

            public bool TryGetVector(int table, int fieldIndex, out int vector, out int count)
            {
                vector = 0;
                count = 0;
                int field = GetField(table, fieldIndex);
                if (field == 0)
                {
                    return false;
                }
                vector = field + ReadInt32(field);
                count = ReadInt32(vector);
                return count >= 0 && count <= 1_000_000 && HasBytes(vector + 4, 0);
            }

            public int GetTableVectorElement(int vector, int index)
            {
                int count = ReadInt32(vector);
                if (index < 0 || index >= count)
                {
                    return 0;
                }
                int element = vector + 4 + index * 4;
                int table = element + ReadInt32(element);
                return IsTable(table) ? table : 0;
            }

            public byte GetByte(int table, int fieldIndex, byte fallback)
            {
                int field = GetField(table, fieldIndex);
                return field != 0 && HasBytes(field, 1) ? bytes[field] : fallback;
            }

            public int GetInt32(int table, int fieldIndex, int fallback)
            {
                int field = GetField(table, fieldIndex);
                return field != 0 ? ReadInt32(field) : fallback;
            }

            public float GetSingle(int table, int fieldIndex, float fallback)
            {
                int field = GetField(table, fieldIndex);
                return field != 0 && HasBytes(field, 4) ? BitConverter.ToSingle(bytes, field) : fallback;
            }

            public byte[] GetByteVector(int table, int fieldIndex)
            {
                if (!TryGetVector(table, fieldIndex, out int vector, out int count) || !HasBytes(vector + 4, count))
                {
                    return Array.Empty<byte>();
                }
                var result = new byte[count];
                Buffer.BlockCopy(bytes, vector + 4, result, 0, count);
                return result;
            }

            public ushort[] GetUInt16Vector(int table, int fieldIndex)
            {
                if (!TryGetVector(table, fieldIndex, out int vector, out int count) || !HasBytes(vector + 4, count * 2))
                {
                    return Array.Empty<ushort>();
                }
                var result = new ushort[count];
                for (int i = 0; i < count; i++)
                {
                    result[i] = ReadUInt16(vector + 4 + i * 2);
                }
                return result;
            }

            public uint[] GetUInt32Vector(int table, int fieldIndex)
            {
                if (!TryGetVector(table, fieldIndex, out int vector, out int count) || !HasBytes(vector + 4, count * 4))
                {
                    return Array.Empty<uint>();
                }
                var result = new uint[count];
                for (int i = 0; i < count; i++)
                {
                    result[i] = ReadUInt32(vector + 4 + i * 4);
                }
                return result;
            }

            private int GetField(int table, int fieldIndex)
            {
                if (!IsTable(table) || fieldIndex < 0)
                {
                    return 0;
                }
                int vtable = table - ReadInt32(table);
                int vtableSize = ReadUInt16(vtable);
                int entry = vtable + 4 + fieldIndex * 2;
                if (!HasBytes(entry, 2) || entry + 2 > vtable + vtableSize)
                {
                    return 0;
                }
                int offset = ReadUInt16(entry);
                return offset == 0 ? 0 : table + offset;
            }

            private bool IsTable(int table)
            {
                if (!HasBytes(table, 4))
                {
                    return false;
                }
                int vtable = table - ReadInt32(table);
                if (!HasBytes(vtable, 4))
                {
                    return false;
                }
                int vtableSize = ReadUInt16(vtable);
                int objectSize = ReadUInt16(vtable + 2);
                return vtableSize >= 4 && vtableSize <= 256 &&
                    objectSize >= 4 && objectSize <= 512 &&
                    HasBytes(vtable, vtableSize) && HasBytes(table, objectSize);
            }

            private bool HasBytes(int offset, int count)
            {
                return offset >= 0 && count >= 0 && offset <= bytes.Length - count;
            }

            private int ReadInt32(int offset)
            {
                return HasBytes(offset, 4) ? BitConverter.ToInt32(bytes, offset) : 0;
            }

            private ushort ReadUInt16(int offset)
            {
                return HasBytes(offset, 2) ? BitConverter.ToUInt16(bytes, offset) : (ushort)0;
            }

            private uint ReadUInt32(int offset)
            {
                return HasBytes(offset, 4) ? BitConverter.ToUInt32(bytes, offset) : 0u;
            }
        }
    }
}
