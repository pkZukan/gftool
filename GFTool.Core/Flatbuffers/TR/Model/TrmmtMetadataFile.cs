using System;
using FlatSharp.Attributes;
using Trinity.Core.Flatbuffers.Utils;

namespace Trinity.Core.Flatbuffers.TR.Model
{
    // TRMMT (.trmmt) metadata/variation flavor observed in SV/ZA.
    // This models the on-disk structure used for per-variation material parameter defaults.
    //
    // PokeDocs also documents another TRMMT flavor that maps material sets to .trmtr files. Some assets may
    // include both concepts; the renderer chooses behavior based on which fields are populated.

    [FlatBufferTable]
    public class TrmmtMetaFloatParams
    {
        [FlatBufferItem(0)] public string Name { get; set; } = string.Empty;
        [FlatBufferItem(1)] public float[] Values { get; set; } = Array.Empty<float>();
    }

    [FlatBufferTable]
    public class TrmmtMetaFloat3Params
    {
        [FlatBufferItem(0)] public string Name { get; set; } = string.Empty;
        [FlatBufferItem(1)] public Vector3f[] Values { get; set; } = Array.Empty<Vector3f>();
    }

    [FlatBufferTable]
    public class TrmmtMetaFloat4Params
    {
        [FlatBufferItem(0)] public string Name { get; set; } = string.Empty;
        [FlatBufferItem(1)] public Vector4f[] Values { get; set; } = Array.Empty<Vector4f>();
    }

    [FlatBufferTable]
    public class TrmmtMetaIntParams
    {
        [FlatBufferItem(0)] public string Name { get; set; } = string.Empty;
        [FlatBufferItem(1)] public int[] Values { get; set; } = Array.Empty<int>();
    }

    [FlatBufferTable]
    public class TrmmtMetaMaterial
    {
        [FlatBufferItem(0)] public string MaterialName { get; set; } = string.Empty;
        [FlatBufferItem(1)] public TrmmtMetaFloatParams[] FloatParamList { get; set; } = Array.Empty<TrmmtMetaFloatParams>();
        [FlatBufferItem(2)] public TrmmtMetaFloat3Params[] Float3ParamList { get; set; } = Array.Empty<TrmmtMetaFloat3Params>();
        [FlatBufferItem(3)] public TrmmtMetaFloat4Params[] Float4ParamList { get; set; } = Array.Empty<TrmmtMetaFloat4Params>();
        [FlatBufferItem(4)] public TrmmtMetaIntParams[] IntParamList { get; set; } = Array.Empty<TrmmtMetaIntParams>();
    }

    [FlatBufferTable]
    public class TrmmtMetaNoAnimeParam
    {
        [FlatBufferItem(0)] public int VariationCount { get; set; }
        [FlatBufferItem(1)] public TrmmtMetaMaterial[] MaterialList { get; set; } = Array.Empty<TrmmtMetaMaterial>();
    }

    [FlatBufferTable]
    public class TrmmtMetaParam
    {
        [FlatBufferItem(0)] public string Name { get; set; } = string.Empty;
        [FlatBufferItem(8, DefaultValue = -1)] public int OverrideDefaultValue { get; set; } = -1;
        [FlatBufferItem(9)] public bool UseNoAnime { get; set; }
        [FlatBufferItem(10)] public TrmmtMetaNoAnimeParam? NoAnimeParam { get; set; }
    }

    [FlatBufferTable]
    public class TrmmtMetaItem
    {
        [FlatBufferItem(0)] public string Name { get; set; } = string.Empty;
        [FlatBufferItem(1)] public string[] MaterialPathList { get; set; } = Array.Empty<string>();
        [FlatBufferItem(3)] public TrmmtMetaParam[] ParamList { get; set; } = Array.Empty<TrmmtMetaParam>();
    }

    [FlatBufferTable]
    public class TrmmtMetadataFile
    {
        [FlatBufferItem(0)] public uint Version { get; set; } = 2;
        [FlatBufferItem(2)] public TrmmtMetaItem[] ItemList { get; set; } = Array.Empty<TrmmtMetaItem>();
    }
}
