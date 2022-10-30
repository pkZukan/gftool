using FlatSharp.Attributes;
using Newtonsoft.Json;

namespace GFTool.Flatbuffers.TR.ResourceDictionary
{
    [FlatBufferTable]
    public class PokemonCatalogInfo
    {
        [JsonProperty("Field_00")]
        [FlatBufferItem(0)]
        public UInt32 Field_00 { get; set; }
        [JsonProperty("Field_01")]
        [FlatBufferItem(1)]
        public UInt32 Field_01 { get; set; }

    }

    [FlatBufferTable]
    public class PokemonCatalogSpeciesInfo
    {
        [JsonProperty("SpeciesNumber")]
        [FlatBufferItem(0)]
        public ushort SpeciesNumber { get; set; }
        [JsonProperty("FormNumber")]
        [FlatBufferItem(1)]
        public ushort FormNumber { get; set; }
    }
    
    [FlatBufferTable]
    public class PokemonCatalogAnimationInfo
    {
        [JsonProperty("AnimationName")]
        [FlatBufferItem(0)]
        public string AnimationName { get; set; } = "";
        [JsonProperty("AnimationPath")]
        [FlatBufferItem(1)]
        public string AnimationPath { get; set; } = "";
    }
    
    [FlatBufferTable]
    public class PokemonCatalogLocatorInfo
    {
        [JsonProperty("LocatorName")]
        [FlatBufferItem(0)]
        public string LocatorName { get; set; } = "";
        [JsonProperty("LocatorPath")]
        [FlatBufferItem(1)]
        public string LocatorPath { get; set; } = "";
    }

    [FlatBufferTable]
    public class PokemonCatalogEntry
    {
        [JsonProperty("SpeciesInfo")]
        [FlatBufferItem(0)]
        public PokemonCatalogSpeciesInfo SpeciesInfo { get; set; } = new PokemonCatalogSpeciesInfo();
        [JsonProperty("ModelPath")]
        [FlatBufferItem(1)]
        public string ModelPath { get; set; } = "";
        [JsonProperty("MaterialPath")]
        [FlatBufferItem(2)]
        public string MaterialPath { get; set; } = "";
        [JsonProperty("ConfigPath")]
        [FlatBufferItem(3)]
        public string ConfigPath { get; set; } = "";
        [JsonProperty("Field_04")]
        [FlatBufferItem(4)]
        public uint? Field_04 { get; set; } = null;
        [JsonProperty("AnimationInfos")]
        [FlatBufferItem(5)]
        public PokemonCatalogAnimationInfo[] AnimationInfos { get; set; } = Array.Empty<PokemonCatalogAnimationInfo>();
        [JsonProperty("LocatorInfos")]
        [FlatBufferItem(6)]
        public PokemonCatalogLocatorInfo[] LocatorInfos { get; set; } = Array.Empty<PokemonCatalogLocatorInfo>();
    }

    [FlatBufferTable]
    public class PokemonCatalog
    {
        [JsonProperty("CatalogInfo")]
        [FlatBufferItem(0)]
        public PokemonCatalogInfo? CatalogInfo { get; set; }
        [JsonProperty("CatalogEntries")]
        [FlatBufferItem(1)]
        public PokemonCatalogEntry[] CatalogEntries { get; set; } = Array.Empty<PokemonCatalogEntry>();
    }
}
