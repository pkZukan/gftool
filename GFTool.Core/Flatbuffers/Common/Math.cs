using FlatSharp.Attributes;
using Trinity.Core.Flatbuffers.Converters;
using System.Text.Json.Serialization;

namespace Trinity.Core.Flatbuffers.Utils
{
    [FlatBufferStruct]
    public class Vector2f
    {
        [FlatBufferItem(0)] public float X { get; set; } = 0.0f;
        [FlatBufferItem(1)] public float Y { get; set; } = 0.0f;
    }

    [FlatBufferStruct]
    public class Vector3f
    {
        [FlatBufferItem(0)] public float X { get; set; } = 0.0f;
        [FlatBufferItem(1)] public float Y { get; set; } = 0.0f;
        [FlatBufferItem(2)] public float Z { get; set; } = 0.0f;
    }

    [FlatBufferStruct]
    public class Vector4f
    {
        [FlatBufferItem(0)] public float W { get; set; } = 0.0f;
        [FlatBufferItem(1)] public float X { get; set; } = 0.0f;
        [FlatBufferItem(2)] public float Y { get; set; } = 0.0f;
        [FlatBufferItem(3)] public float Z { get; set; } = 0.0f;
    }

    [FlatBufferStruct]
    public class Vector2i
    {
        [FlatBufferItem(0)] public int X { get; set; } = 0;
        [FlatBufferItem(1)] public int Y { get; set; } = 0;
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
    public class TRBoundingBox
    {
        [FlatBufferItem(0)] public Vector3f MinBound { get; set; } = new Vector3f();
        [FlatBufferItem(1)] public Vector3f MaxBound { get; set; } = new Vector3f();
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
    public class RGBA
    {
        [FlatBufferItem(0)] public float R { get; set; }
        [FlatBufferItem(1)] public float G { get; set; }
        [FlatBufferItem(2)] public float B { get; set; }
        [FlatBufferItem(3)] public float A { get; set; }
    };

    [FlatBufferStruct]
    public class Transform
    {
        [FlatBufferItem(0)]
        public Vector3f Scale { get; set; }
        [FlatBufferItem(1)]
        public Vector4f Rotate { get; set; }
        [FlatBufferItem(2)]
        public Vector3f Translate { get; set; }
    }

}
