using FlatSharp.Attributes;
using GFTool.Flatbuffers.Utils;

namespace GFTool.Flatbuffers.TR.Model
{

    [FlatBufferTable]
    public class ModelMesh
    {
        [FlatBufferItem(0)] public string PathName { get; set; }
    }

    [FlatBufferTable]
    public class ModelSkeleton
    {
        [FlatBufferItem(0)] public string PathName { get; set; }
    }


    [FlatBufferTable]
    public class ModelLOD
    {
        [FlatBufferItem(0)] public ModelLODEntry[] Entries { get; set; }
        [FlatBufferItem(1)] public string Type { get; set; }
    }

    [FlatBufferTable]
    public class ModelLODEntry
    {
        [FlatBufferItem(0)] public int Index { get; set; }
    }

    [FlatBufferTable]
    public class TRMDL
    {
        [FlatBufferItem(0)] public int Field_00 { get; set; }
        [FlatBufferItem(1)] public ModelMesh[] Meshes { get; set; }
        [FlatBufferItem(2)] public ModelSkeleton Skeleton { get; set; }
        [FlatBufferItem(3)] public string[] Materials { get; set; }
        [FlatBufferItem(4)] public ModelLOD[] LODs { get; set; }
        [FlatBufferItem(5)] public BoundingBox Bounds { get; set; }
        [FlatBufferItem(6)] public Vector4 Field_06 { get; set; }
    }
}
