using FlatSharp.Attributes;

namespace Trinity.Core.Flatbuffers.TR.Model
{
    // PokeDocs-based TRMMT schema (Titan/Ikkaku).
    // This file models the on-disk TRMMT structure used to map to variant material files (e.g. normal/rare).

    [FlatBufferTable]
    public class TrmmtMaterialSwitch
    {
        [FlatBufferItem(0)] public string MaterialName { get; set; } = string.Empty;
        [FlatBufferItem(1)] public byte MaterialFlag { get; set; }
    }

    [FlatBufferTable]
    public class TrmmtMaterialMapper
    {
        [FlatBufferItem(0)] public string MeshName { get; set; } = string.Empty;
        [FlatBufferItem(1)] public string MaterialName { get; set; } = string.Empty;
        [FlatBufferItem(2)] public string LayerName { get; set; } = string.Empty;
    }

    [FlatBufferTable]
    public class TrmmtEmbeddedTracm
    {
        [FlatBufferItem(0)] public byte[] ByteBuffer { get; set; } = Array.Empty<byte>();
    }

    [FlatBufferTable]
    public class TrmmtMaterialProperties
    {
        [FlatBufferItem(0)] public string Name { get; set; } = string.Empty;
        [FlatBufferItem(1)] public TrmmtMaterialMapper[] Mappers { get; set; } = Array.Empty<TrmmtMaterialMapper>();
        [FlatBufferItem(2)] public uint Field2 { get; set; }
        [FlatBufferItem(3)] public uint Field3 { get; set; }
        [FlatBufferItem(4)] public TrmmtEmbeddedTracm? Tracm { get; set; }
        [FlatBufferItem(5)] public uint[] Field5 { get; set; } = Array.Empty<uint>();
    }

    [FlatBufferTable]
    public class TrmmtEntry
    {
        [FlatBufferItem(0)] public string Name { get; set; } = string.Empty;
        [FlatBufferItem(1)] public string[] MaterialNames { get; set; } = Array.Empty<string>();
        [FlatBufferItem(2)] public TrmmtMaterialSwitch[] MaterialSwitches { get; set; } = Array.Empty<TrmmtMaterialSwitch>();
        [FlatBufferItem(3)] public TrmmtMaterialProperties[] MaterialProperties { get; set; } = Array.Empty<TrmmtMaterialProperties>();
    }

    [FlatBufferTable]
    public class TrmmtFile
    {
        [FlatBufferItem(0)] public uint Field0 { get; set; }
        [FlatBufferItem(1)] public uint Field1 { get; set; }
        [FlatBufferItem(2)] public TrmmtEntry[] Material { get; set; } = Array.Empty<TrmmtEntry>();
    }
}
