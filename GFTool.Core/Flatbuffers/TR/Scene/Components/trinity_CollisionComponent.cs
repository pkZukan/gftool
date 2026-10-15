using FlatSharp;
using FlatSharp.Attributes;
using Trinity.Core.Flatbuffers.Utils;
using Trinity.Core.Flatbuffers.TR.Model;
using ColShapeType = FlatSharp.FlatBufferUnion<GFTool.Core.Flatbuffers.TR.Scene.Components.SphereShape, GFTool.Core.Flatbuffers.TR.Scene.Components.BoxShape, GFTool.Core.Flatbuffers.TR.Scene.Components.CapsuleShape, GFTool.Core.Flatbuffers.TR.Scene.Components.ModelShape, GFTool.Core.Flatbuffers.TR.Scene.Components.PencilShape>;
using Collision = FlatSharp.FlatBufferUnion<GFTool.Core.Flatbuffers.TR.Scene.Components.BodyCollision, GFTool.Core.Flatbuffers.TR.Scene.Components.CharCollision>;

namespace GFTool.Core.Flatbuffers.TR.Scene.Components
{
    [FlatBufferTable]
    public class SphereShape
    {
        [FlatBufferItem(0)] public Vector3f Pos { get; set; }
        [FlatBufferItem(1)] public float Radius { get; set; }
    }

    [FlatBufferTable]
    public class BoxShape
    {
        [FlatBufferItem(0)] public Vector3f Pos { get; set; }
        [FlatBufferItem(1)] public Vector3f Size { get; set; }
        [FlatBufferItem(2)] public Vector3f Rot { get; set; }
    }

    [FlatBufferTable]
    public class CapsuleShape
    {
        [FlatBufferItem(0)] public Vector3f Pos { get; set; }
        [FlatBufferItem(1)] public float Radius { get; set; }
        [FlatBufferItem(2)] public float Height { get; set; }
        [FlatBufferItem(3)] public Vector3f Rot { get; set; }
    }

    [FlatBufferTable]
    public class ModelShape
    {
        [FlatBufferItem(0)] public string FilePath { get; set; }
        [FlatBufferItem(1)] public Vector3f Pos { get; set; }
        [FlatBufferItem(2)] public Vector3f Rot { get; set; }
        [FlatBufferItem(3)] public Vector3f Scale { get; set; }
    }

    [FlatBufferTable]
    public class PencilShape
    {
        [FlatBufferItem(0)] public Vector3f Pos { get; set; }
        [FlatBufferItem(1)] public float Radius { get; set; }
        [FlatBufferItem(2)] public float CylHeight { get; set; }
        [FlatBufferItem(3)] public float TipHeight { get; set; }
        [FlatBufferItem(4)] public int SliceCount { get; set; }
        [FlatBufferItem(5)] public Vector3f Rot { get; set; }
    }

    [FlatBufferTable]
    public class BodyCollision
    {
        [FlatBufferItem(0)] public ColShapeType? Shape { get; set; }
    }

    [FlatBufferTable]
    public class CharCollision
    {
        [FlatBufferItem(0)] public ColShapeType? Shape { get; set; }
    }

    [FlatBufferTable]
    public class trinity_CollisionComponent
    {
        [FlatBufferItem(0)] public Collision? Collision { get; set; }
    }

}
