using FlatSharp.Attributes;

namespace GFTool.Core.Flatbuffers.TR.Model
{
    [FlatBufferTable]
    public class MaterialSwitch
    {
        [FlatBufferItem(0)] public string Name { get; set; }
        [FlatBufferItem(1)] public byte Flag { get; set; }

    }

    [FlatBufferTable]
    public class MaterialMapper
    {
        [FlatBufferItem(0)] public string MeshName { get; set; } = "";
        [FlatBufferItem(1)] public string MaterialName { get; set; } = "";
        [FlatBufferItem(2)] public string LayerName { get; set; } = "";
    }

    [FlatBufferTable]
    public class MaterialEmbed
    {
        [FlatBufferItem(0)] public byte[] EmbeddedFile { get; set; } = Array.Empty<byte>();
    }

    [FlatBufferTable]
    public class MaterialProperty
    {
        [FlatBufferItem(0)] public string Name { get; set; }
        [FlatBufferItem(1)] public List<MaterialMapper> Mappers { get; set; }
        [FlatBufferItem(2)] public int Field_02 { get; set; }
        [FlatBufferItem(3)] public int Field_03 { get; set; }
        [FlatBufferItem(4)] public MaterialEmbed Embed { get; set; }
        [FlatBufferItem(5)] public List<int> Field_05 { get; set; } = new List<int>();
    }

    [FlatBufferTable]
    public class MaterialEntry
    {
        [FlatBufferItem(0)] public string Name { get; set; }
        [FlatBufferItem(1)] public List<string> FileNames { get; set; }
        [FlatBufferItem(2)] public List<MaterialSwitch> MaterialSwitches { get; set; }
        [FlatBufferItem(3)] public List<string> MaterialProperties { get; set; }

    }
    [FlatBufferTable]
    public class MaterialTable
    {
        [FlatBufferItem(0)] public int Field_00 { get; set; }
        [FlatBufferItem(1)] public int Field_01 { get; set; }
        [FlatBufferItem(2)] public List<MaterialEntry> Materials { get; set; } = new List<MaterialEntry>();
    }
}