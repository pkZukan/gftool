using FlatSharp.Attributes;

namespace Trinity.Core.Flatbuffers.TR.Model
{
    [FlatBufferTable]
    public class TRStringParameter
    {
        [FlatBufferItem(0)] public string Name { get; set; }
        [FlatBufferItem(1)] public string Value { get; set; }
    }

    [FlatBufferTable]
    public class TRMaterialShader
    {
        [FlatBufferItem(0)] public string Name { get; set; }
    }

    [FlatBufferTable]
    public class TRMaterial
    {
        [FlatBufferItem(0)] public string Name { get; set; }
        [FlatBufferItem(1)] public TRMaterialShader Shader { get; set; }
    }

    [FlatBufferTable]
    public class TRMTR
    {
        [FlatBufferItem(0)] public int Field_00 { get; set; }
        [FlatBufferItem(1)] public TRMaterial[] Materials { get; set; }
    }
}