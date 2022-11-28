using FlatSharp.Attributes;

namespace GFTool.Core.Flatbuffers.TR.ResourceDictionary.SV
{
    [FlatBufferTable]
    public class PokemonCatalogInfo
    {
        [FlatBufferItem(0)]
        public UInt32 Field_00 { get; set; }

        [FlatBufferItem(1)]
        public UInt32 Field_01 { get; set; }

    }

    [FlatBufferTable]
    public class PokemonCatalogSpeciesInfo
    {
        [FlatBufferItem(0)]
        public ushort SpeciesNumber { get; set; }

        [FlatBufferItem(1)]
        public ushort FormNumber { get; set; }
    }
    
    [FlatBufferTable]
    public class PokemonCatalogAnimationInfo
    {
        [FlatBufferItem(0)]
        public string AnimationName { get; set; } = "";

        [FlatBufferItem(1)]
        public string AnimationPath { get; set; } = "";
    }
    
    [FlatBufferTable]
    public class PokemonCatalogLocatorInfo
    {
        [FlatBufferItem(0)]
        public string LocatorName { get; set; } = "";

        [FlatBufferItem(1)]
        public string LocatorPath { get; set; } = "";
    }

    [FlatBufferTable]
    public class PokemonCatalogEntry
    {
        [FlatBufferItem(0)]
        public PokemonCatalogSpeciesInfo SpeciesInfo { get; set; } = new PokemonCatalogSpeciesInfo();

        [FlatBufferItem(1)]
        public string ModelPath { get; set; } = "";

        [FlatBufferItem(2)]
        public string MaterialPath { get; set; } = "";

        [FlatBufferItem(3)]
        public string ConfigPath { get; set; } = "";

        [FlatBufferItem(4)]
        public uint? Field_04 { get; set; } = null;

        [FlatBufferItem(5)]
        public PokemonCatalogAnimationInfo[] AnimationInfos { get; set; } = Array.Empty<PokemonCatalogAnimationInfo>();

        [FlatBufferItem(6)]
        public PokemonCatalogLocatorInfo[] LocatorInfos { get; set; } = Array.Empty<PokemonCatalogLocatorInfo>();
    }

    [FlatBufferTable]
    public class PokemonCatalog
    {
        [FlatBufferItem(0)]
        public PokemonCatalogInfo? CatalogInfo { get; set; }

        [FlatBufferItem(1)]
        public PokemonCatalogEntry[] CatalogEntries { get; set; } = Array.Empty<PokemonCatalogEntry>();
    }
}
