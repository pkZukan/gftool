using FlatSharp.Attributes;

namespace GFTool.Flatbuffers.TR.Model
{
    [FlatBufferTable]
    public class MaterialSwitch
    {
        [FlatBufferItem(00)] public string Name { get; set; }
        [FlatBufferItem(01)] public byte Flag { get; set; }

    }

    [FlatBufferTable]
    public class MaterialMapper
    {
        [FlatBufferItem(00)] public string MeshName { get; set; } = "";
        [FlatBufferItem(01)] public string MaterialName { get; set; } = "";
        [FlatBufferItem(02)] public string LayerName { get; set; } = "";
    }

    [FlatBufferTable]
    public class MaterialEmbed
    {
        [FlatBufferItem(00)] public byte[] EmbeddedFile { get; set; } = Array.Empty<byte>();
    }

    [FlatBufferTable]
    public class MaterialProperty
    {
        [FlatBufferItem(00)] public string Name { get; set; }
        [FlatBufferItem(01)] public List<MaterialMapper> Mappers { get; set; }
        [FlatBufferItem(02)] public int Field_02 { get; set; }
        [FlatBufferItem(03)] public int Field_03 { get; set; }
        [FlatBufferItem(04)] public MaterialEmbed Embed { get; set; }
        [FlatBufferItem(05)] public List<int> Field_05 { get; set; } = new List<int>();
    }

    [FlatBufferTable]
    public class MaterialEntry
    {
        [FlatBufferItem(00)] public string Name { get; set; }
        [FlatBufferItem(01)] public List<string> FileNames { get; set; }
        [FlatBufferItem(02)] public List<MaterialSwitch> MaterialSwitches { get; set; }
        [FlatBufferItem(03)] public List<string> MaterialProperties { get; set; }

    }
    [FlatBufferTable]
    public class MaterialTable
    {
        [FlatBufferItem(00)] public int Field_00 { get; set; }
        [FlatBufferItem(01)] public int Field_01 { get; set; }
        [FlatBufferItem(02)] public List<MaterialEntry> Materials { get; set; } = new List<MaterialEntry>();
    }
}