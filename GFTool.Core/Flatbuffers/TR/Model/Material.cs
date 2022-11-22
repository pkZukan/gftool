using FlatSharp.Attributes;

namespace GFTool.Core.Flatbuffers.TR.Model
{
    [FlatBufferTable]
    public class Material
    {
        [FlatBufferItem(0)] public string Name { get; set; }

    }
    [FlatBufferTable]
    public class MaterialList
    {
        [FlatBufferItem(0)] public int Field_00 { get; set; }
        [FlatBufferItem(1)] public Material[] Materials { get; set; }
    }
}