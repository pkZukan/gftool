using FlatSharp.Attributes;
using GFTool.Flatbuffers.Utils;

namespace GFTool.Flatbuffers.TR.Model
{

    [FlatBufferTable]
    public class ModelMesh
    {
        [FlatBufferItem(00)] public string PathName { get; set; }
    }

    [FlatBufferTable]
    public class ModelSkeleton
    {
        [FlatBufferItem(00)] public string PathName { get; set; }
    }


    [FlatBufferTable]
    public class ModelLOD
    {
        [FlatBufferItem(00)] public ModelLODEntry[] Entries { get; set; }
        [FlatBufferItem(01)] public string Type { get; set; }
    }

    [FlatBufferTable]
    public class ModelLODEntry
    {
        [FlatBufferItem(00)] public int Index { get; set; }
    }

    [FlatBufferTable]
    public class TRMDL
    {
        [FlatBufferItem(00)] public int Field_00 { get; set; }
        [FlatBufferItem(01)] public ModelMesh[] Meshes { get; set; }
        [FlatBufferItem(02)] public ModelSkeleton Skeleton { get; set; }
        [FlatBufferItem(03)] public string[] Materials { get; set; }
        [FlatBufferItem(04)] public ModelLOD[] LODs { get; set; }
        [FlatBufferItem(05)] public BoundingBox Bounds { get; set; }
        [FlatBufferItem(06)] public Vector4 Field_06 { get; set; }
    }
}
