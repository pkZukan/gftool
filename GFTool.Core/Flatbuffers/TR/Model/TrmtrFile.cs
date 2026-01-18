using FlatSharp.Attributes;
using Trinity.Core.Flatbuffers.Utils;

namespace Trinity.Core.Flatbuffers.TR.Model
{
    // PokeDocs-based TRMTR schema (Titan/Ikkaku).
    // This file models the on-disk TRMTR structure. The renderer still consumes `TRMaterial`
    // as its runtime representation.

    [FlatBufferTable]
    public class TrmtrFileStringParameter
    {
        [FlatBufferItem(0)] public string Name { get; set; } = string.Empty;
        [FlatBufferItem(1)] public string Value { get; set; } = string.Empty;
    }

    [FlatBufferTable]
    public class TrmtrFileShader
    {
        [FlatBufferItem(0)] public string Name { get; set; } = string.Empty;
        [FlatBufferItem(1)] public TrmtrFileStringParameter[] Values { get; set; } = Array.Empty<TrmtrFileStringParameter>();
    }

    [FlatBufferTable]
    public class TrmtrFileTexture
    {
        [FlatBufferItem(0)] public string Name { get; set; } = string.Empty;
        [FlatBufferItem(1)] public string File { get; set; } = string.Empty;
        [FlatBufferItem(2)] public uint Slot { get; set; } = 0;
    }

    [FlatBufferTable]
    public class TrmtrFileSamplerState
    {
        [FlatBufferItem(0)] public uint State0 { get; set; } = 0;
        [FlatBufferItem(1)] public uint State1 { get; set; } = 0;
        [FlatBufferItem(2)] public uint State2 { get; set; } = 0;
        [FlatBufferItem(3)] public uint State3 { get; set; } = 0;
        [FlatBufferItem(4)] public uint State4 { get; set; } = 0;
        [FlatBufferItem(5)] public uint State5 { get; set; } = 0;
        [FlatBufferItem(6)] public uint State6 { get; set; } = 0;
        [FlatBufferItem(7)] public uint State7 { get; set; } = 0;
        [FlatBufferItem(8)] public uint State8 { get; set; } = 0;
        [FlatBufferItem(9)] public UVWrapMode RepeatU { get; set; } = UVWrapMode.WRAP;
        [FlatBufferItem(10)] public UVWrapMode RepeatV { get; set; } = UVWrapMode.WRAP;
        [FlatBufferItem(11)] public UVWrapMode RepeatW { get; set; } = UVWrapMode.WRAP;
        [FlatBufferItem(12)] public RGBA BorderColor { get; set; } = new RGBA();
    }

    [FlatBufferTable]
    public class TrmtrFileFloatParameter
    {
        [FlatBufferItem(0)] public string Name { get; set; } = string.Empty;
        [FlatBufferItem(1)] public float Value { get; set; }
    }

    [FlatBufferTable]
    public class TrmtrFileFloat4Parameter
    {
        [FlatBufferItem(0)] public string Name { get; set; } = string.Empty;
        [FlatBufferItem(1)] public RGBA Value { get; set; } = new RGBA();
    }

    [FlatBufferTable]
    public class TrmtrFileIntParameter
    {
        [FlatBufferItem(0)] public string Name { get; set; } = string.Empty;
        [FlatBufferItem(1, DefaultValue = -1)] public int Value { get; set; } = -1;
    }

    [FlatBufferTable]
    public class TrmtrFileByteExtra
    {
        [FlatBufferItem(0, DefaultValue = (sbyte)-1)] public sbyte Value { get; set; } = -1;
    }

    [FlatBufferTable]
    public class TrmtrFileIntExtra
    {
        [FlatBufferItem(0)] public uint Field0 { get; set; }
        [FlatBufferItem(1, DefaultValue = -1)] public int Value { get; set; } = -1;
    }

    [FlatBufferTable]
    public class TrmtrFileMaterial
    {
        [FlatBufferItem(0)] public string Name { get; set; } = string.Empty;
        [FlatBufferItem(1)] public TrmtrFileShader[] Shaders { get; set; } = Array.Empty<TrmtrFileShader>();
        [FlatBufferItem(2)] public TrmtrFileTexture[] Textures { get; set; } = Array.Empty<TrmtrFileTexture>();
        [FlatBufferItem(3)] public TrmtrFileSamplerState[] Samplers { get; set; } = Array.Empty<TrmtrFileSamplerState>();
        [FlatBufferItem(4)] public TrmtrFileFloatParameter[] FloatParameters { get; set; } = Array.Empty<TrmtrFileFloatParameter>();
        [FlatBufferItem(5)] public string Unknown5 { get; set; } = string.Empty;
        [FlatBufferItem(6)] public TrmtrFileFloat4Parameter[] Float4LightParameters { get; set; } = Array.Empty<TrmtrFileFloat4Parameter>();
        [FlatBufferItem(7)] public TrmtrFileFloat4Parameter[] Float4Parameters { get; set; } = Array.Empty<TrmtrFileFloat4Parameter>();
        [FlatBufferItem(8)] public string Unknown8 { get; set; } = string.Empty;
        [FlatBufferItem(9)] public TrmtrFileIntParameter[] IntParameters { get; set; } = Array.Empty<TrmtrFileIntParameter>();
        [FlatBufferItem(10)] public string Unknown10 { get; set; } = string.Empty;
        [FlatBufferItem(11)] public string Unknown11 { get; set; } = string.Empty;
        [FlatBufferItem(12)] public string Unknown12 { get; set; } = string.Empty;
        [FlatBufferItem(13)] public TrmtrFileByteExtra? ByteExtra { get; set; }
        [FlatBufferItem(14)] public TrmtrFileIntExtra? IntExtra { get; set; }
        [FlatBufferItem(15)] public string AlphaType { get; set; } = string.Empty;
    }

    [FlatBufferTable]
    public class TrmtrFile
    {
        [FlatBufferItem(0)] public uint Field0 { get; set; }
        [FlatBufferItem(1)] public TrmtrFileMaterial[] Materials { get; set; } = Array.Empty<TrmtrFileMaterial>();
    }
}
