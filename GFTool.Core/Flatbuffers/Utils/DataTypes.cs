using FlatSharp.Attributes;
using GFToolCore.Flatbuffers.Converters;
using System.Text.Json.Serialization;

namespace GFToolCore.Flatbuffers.Utils
{
    [FlatBufferStruct]
    public class Vector3
    {
        [FlatBufferItem(0)] public float X { get; set; } = 0.0f;
        [FlatBufferItem(1)] public float Y { get; set; } = 0.0f;
        [FlatBufferItem(2)] public float Z { get; set; } = 0.0f;
    }

    [FlatBufferStruct]
    public class Vector4
    {
        [FlatBufferItem(0)] public float W { get; set; } = 0.0f;
        [FlatBufferItem(1)] public float X { get; set; } = 0.0f;
        [FlatBufferItem(2)] public float Y { get; set; } = 0.0f;
        [FlatBufferItem(3)] public float Z { get; set; } = 0.0f;
    }

    [FlatBufferStruct]
    public class Sphere
    {
        [FlatBufferItem(0)] public float X { get; set; } = 0.0f;
        [FlatBufferItem(1)] public float Y { get; set; } = 0.0f;
        [FlatBufferItem(2)] public float Z { get; set; } = 0.0f;
        [FlatBufferItem(3)] public float Radius { get; set; } = 0.0f;
    }

    [FlatBufferTable]
    public class BoundingBox
    {
        [FlatBufferItem(0)] public Vector3 MinBound { get; set; } = new Vector3();
        [FlatBufferItem(1)] public Vector3 MaxBound { get; set; } = new Vector3();
    }

    [JsonConverter(typeof(QuaternionConverter))]
    [FlatBufferStruct]
    public class PackedQuaternion
    {
        [FlatBufferItem(0)] public ushort X { get; set; }
        [FlatBufferItem(1)] public ushort Y { get; set; }
        [FlatBufferItem(2)] public ushort Z { get; set; }
    }

    [FlatBufferStruct]
    public class Transform
    {
        [FlatBufferItem(0)]
        public Vector3 Scale { get; set; }
        [FlatBufferItem(1)]
        public Vector4 Rotate { get; set; }
        [FlatBufferItem(2)]
        public Vector3 Translate { get; set; }
    }

}
