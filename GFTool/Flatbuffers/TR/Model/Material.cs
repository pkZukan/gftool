using FlatSharp.Attributes;

namespace GFTool.Flatbuffers.TR.Model
{
    [FlatBufferTable]
    public class Material
    {
        [FlatBufferItem(00)] public string Name { get; set; }

    }
    [FlatBufferTable]
    public class MaterialList
    {
        [FlatBufferItem(00)] public int Field_00 { get; set; }
        [FlatBufferItem(01)] public Material[] Materials { get; set; }
    }
}