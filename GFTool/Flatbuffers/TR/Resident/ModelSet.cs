using FlatSharp.Attributes;

namespace GFTool.Flatbuffers.TR.Resident
{
    [FlatBufferTable]
    public class ModelSetAnimations
    {
        [FlatBufferItem(00)] public string[] FolderPath { get; set; } = Array.Empty<string>();//First value is base, usually empty string
        [FlatBufferItem(01)] public string[] AnimationPath { get; set; } = Array.Empty<string>();
    }
    
    [FlatBufferTable]
    public class ModelSetModels
    {
        [FlatBufferItem(00)] public string[] FolderPath { get; set; } = Array.Empty<string>(); //First value is base, usually empty string
        [FlatBufferItem(01)] public string[] MeshPath { get; set; } = Array.Empty<string>();
        [FlatBufferItem(02)] public string[] MeshFlags { get; set; } = Array.Empty<string>();
    }
    
    [FlatBufferTable]
    public class ModelSetSkeletons
    {
        [FlatBufferItem(00)] public string[] Skeletons { get; set; } = Array.Empty<string>();
    }
    
    [FlatBufferTable]
    public class ModelSetColors
    {
        [FlatBufferItem(00)] public string[] Slots { get; set; } = Array.Empty<string>(); // First value is base, usually empty string
        [FlatBufferItem(01)] public uint[] R1 { get; set; } = Array.Empty<uint>();
        [FlatBufferItem(02)] public uint[] G1 { get; set; } = Array.Empty<uint>();
        [FlatBufferItem(03)] public uint[] B1 { get; set; } = Array.Empty<uint>();
        [FlatBufferItem(04)] public uint[] R2 { get; set; } = Array.Empty<uint>();
        [FlatBufferItem(05)] public uint[] G2 { get; set; } = Array.Empty<uint>();
        [FlatBufferItem(06)] public uint[] B2 { get; set; } = Array.Empty<uint>();
        [FlatBufferItem(07)] public uint[] R3 { get; set; } = Array.Empty<uint>();
        [FlatBufferItem(08)] public uint[] G3 { get; set; } = Array.Empty<uint>();
        [FlatBufferItem(09)] public uint[] B3 { get; set; } = Array.Empty<uint>();
    }

    [FlatBufferTable]
    public class ModelSet
    {
        [FlatBufferItem(00)] public UInt64 NPCModelHash { get; set; }
        [FlatBufferItem(01)] public string NPCIndex { get; set; } = string.Empty;
        [FlatBufferItem(02)] public UInt64 NPCResidentHash { get; set; }
        [FlatBufferItem(03)] public ModelSetAnimations Animations { get; set; } = new();
        [FlatBufferItem(04)] public ModelSetModels Meshes { get; set; } = new();
        [FlatBufferItem(05)] public ModelSetSkeletons Skeletons { get; set; } = new();
        [FlatBufferItem(06)] public string Attachment0 { get; set; } = string.Empty;
        [FlatBufferItem(07)] public string Attachment1 { get; set; } = string.Empty;
        [FlatBufferItem(08)] public string Attachment2 { get; set; } = string.Empty;
        [FlatBufferItem(09)] public ModelSetColors Colors { get; set; } = new();
        [FlatBufferItem(10)] public string ValueListID { get; set; } = string.Empty;
        [FlatBufferItem(11)] public List<uint> Field_11 { get; set; } = new List<uint>();
        [FlatBufferItem(12)] public string ArchivePath { get; set; } = string.Empty;
        [FlatBufferItem(13)] public float Field_13 { get; set; }

    }
    [FlatBufferTable]
    public class ModelSetTable
    {
        [FlatBufferItem(0)]
        public List<ModelSet> ModelSets { get; set; } = new List<ModelSet>();
    }
}
