using FlatSharp.Attributes;

namespace GFToolCore.Flatbuffers.TR.Resident
{
    [FlatBufferTable]
    public class ModelSetAnimations
    {
        [FlatBufferItem(0)] public string[] FolderPath { get; set; } = Array.Empty<string>();//First value is base, usually empty string
        [FlatBufferItem(1)] public string[] AnimationPath { get; set; } = Array.Empty<string>();
    }
    
    [FlatBufferTable]
    public class ModelSetModels
    {
        [FlatBufferItem(0)] public string[] FolderPath { get; set; } = Array.Empty<string>(); //First value is base, usually empty string
        [FlatBufferItem(1)] public string[] MeshPath { get; set; } = Array.Empty<string>();
        [FlatBufferItem(2)] public string[] MeshFlags { get; set; } = Array.Empty<string>();
    }
    
    [FlatBufferTable]
    public class ModelSetSkeletons
    {
        [FlatBufferItem(0)] public string[] Skeletons { get; set; } = Array.Empty<string>();
    }
    
    [FlatBufferTable]
    public class ModelSetColors
    {
        [FlatBufferItem(0)] public string[] Slots { get; set; } = Array.Empty<string>(); // First value is base, usually empty string
        [FlatBufferItem(1)] public uint[] R1 { get; set; } = Array.Empty<uint>();
        [FlatBufferItem(2)] public uint[] G1 { get; set; } = Array.Empty<uint>();
        [FlatBufferItem(3)] public uint[] B1 { get; set; } = Array.Empty<uint>();
        [FlatBufferItem(4)] public uint[] R2 { get; set; } = Array.Empty<uint>();
        [FlatBufferItem(5)] public uint[] G2 { get; set; } = Array.Empty<uint>();
        [FlatBufferItem(6)] public uint[] B2 { get; set; } = Array.Empty<uint>();
        [FlatBufferItem(7)] public uint[] R3 { get; set; } = Array.Empty<uint>();
        [FlatBufferItem(8)] public uint[] G3 { get; set; } = Array.Empty<uint>();
        [FlatBufferItem(9)] public uint[] B3 { get; set; } = Array.Empty<uint>();
    }

    [FlatBufferTable]
    public class ModelSet
    {
        [FlatBufferItem(0)] public UInt64 NPCModelHash { get; set; }
        [FlatBufferItem(1)] public string NPCIndex { get; set; } = string.Empty;
        [FlatBufferItem(2)] public UInt64 NPCResidentHash { get; set; }
        [FlatBufferItem(3)] public ModelSetAnimations Animations { get; set; } = new();
        [FlatBufferItem(4)] public ModelSetModels Meshes { get; set; } = new();
        [FlatBufferItem(5)] public ModelSetSkeletons Skeletons { get; set; } = new();
        [FlatBufferItem(6)] public string Attachment0 { get; set; } = string.Empty;
        [FlatBufferItem(7)] public string Attachment1 { get; set; } = string.Empty;
        [FlatBufferItem(8)] public string Attachment2 { get; set; } = string.Empty;
        [FlatBufferItem(9)] public ModelSetColors Colors { get; set; } = new();
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
