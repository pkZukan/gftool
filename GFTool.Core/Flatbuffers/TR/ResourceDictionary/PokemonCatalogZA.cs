using FlatSharp;
using FlatSharp.Attributes;
using Trinity.Core.Flatbuffers.Utils;
using Trinity.Core.Flatbuffers.TR.Model;

namespace Trinity.Core.Flatbuffers.TR.ResourceDictionary.ZA
{
    [FlatBufferTable]
    public class PokemonCatalogInfo
    {
        [FlatBufferItem(0)] public uint Version { get; set; }
    }

    [FlatBufferTable]
    public class PokemonCatalogSpeciesInfo
    {
        [FlatBufferItem(0)] public ushort Species { get; set; }
        [FlatBufferItem(1)] public ushort Form { get; set; }
        [FlatBufferItem(2)] public byte Gender { get; set; }
    }

    [FlatBufferTable]
    public class PokemonCatalogAnimationInfo
    {
        [FlatBufferItem(0)] public short FormNumber { get; set; }
        [FlatBufferItem(1)] public string Path { get; set; }
    }

    [FlatBufferTable]
    public class PokemonCatalogLocatorInfo
    {
        [FlatBufferItem(0)] public short FormNumber { get; set; }
        [FlatBufferItem(1)] public byte LocIndex { get; set; }
        [FlatBufferItem(2)] public string LocPath { get; set; }
    }

    [FlatBufferTable]
    public class PokemonCatalogEntry
    {
        [FlatBufferItem(0)] public PokemonCatalogSpeciesInfo SpeciesInfo { get; set; }
        [FlatBufferItem(1)] public string ModelPath { get; set; }
        [FlatBufferItem(2)] public string MaterialTablePath { get; set; }
        [FlatBufferItem(3)] public string ConfigPath { get; set; }
        [FlatBufferItem(4)] public IList<PokemonCatalogAnimationInfo> Animations { get; set; }
        [FlatBufferItem(5)] public IList<PokemonCatalogLocatorInfo> Locators { get; set; }
        [FlatBufferItem(6)] public string IconPath { get; set; }
        [FlatBufferItem(7)] public uint UnkId { get; set; }
    }

    [FlatBufferTable]
    public class PokemonCatalog
    {
        [FlatBufferItem(0)] public PokemonCatalogInfo Version { get; set; }
        [FlatBufferItem(1)] public IList<PokemonCatalogEntry> CatalogTable { get; set; }
    }

}
