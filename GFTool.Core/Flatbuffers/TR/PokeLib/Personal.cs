using FlatSharp;
using FlatSharp.Attributes;
using Trinity.Core.Flatbuffers.Utils;
using Trinity.Core.Flatbuffers.TR.Model;

namespace Trinity.Core.Flatbuffers.TR.PokeLib
{
    [FlatBufferStruct]
    public class SpeciesInfo
    {
        [FlatBufferItem(0)] public ushort SpeciesIndex { get; set; }
        [FlatBufferItem(1)] public ushort SpeciesForm { get; set; }
        [FlatBufferItem(2)] public ushort SpeciesModel { get; set; }
        [FlatBufferItem(3)] public byte Color { get; set; }
        [FlatBufferItem(4)] public byte Bodytype { get; set; }
        [FlatBufferItem(5)] public ushort Height { get; set; }
        [FlatBufferItem(6)] public ushort Weight { get; set; }
        [FlatBufferItem(7)] public uint Res0 { get; set; }
        [FlatBufferItem(8)] public uint Res1 { get; set; }
        [FlatBufferItem(9)] public uint Res2 { get; set; }
    }

    [FlatBufferStruct]
    public class StatInfo
    {
        [FlatBufferItem(0)] public byte HP { get; set; }
        [FlatBufferItem(1)] public byte ATK { get; set; }
        [FlatBufferItem(2)] public byte DEF { get; set; }
        [FlatBufferItem(3)] public byte SPA { get; set; }
        [FlatBufferItem(4)] public byte SPD { get; set; }
        [FlatBufferItem(5)] public byte SPE { get; set; }
    }

    [FlatBufferStruct]
    public class GenderData
    {
        [FlatBufferItem(0)] public byte Group { get; set; }
        [FlatBufferItem(1)] public byte Ratio { get; set; }
    }

    [FlatBufferStruct]
    public class EvoData
    {
        [FlatBufferItem(0)] public ushort Level { get; set; }
        [FlatBufferItem(1)] public ushort Method { get; set; }
        [FlatBufferItem(2)] public ushort Arg { get; set; }
        [FlatBufferItem(3)] public ushort Res0 { get; set; }
        [FlatBufferItem(4)] public ushort Res1 { get; set; }
        [FlatBufferItem(5)] public ushort Res2 { get; set; }
        [FlatBufferItem(6)] public ushort Species { get; set; }
        [FlatBufferItem(7)] public ushort Form { get; set; }
    }

    [FlatBufferStruct]
    public class Hatch
    {
        [FlatBufferItem(0)] public ushort Species { get; set; }
        [FlatBufferItem(1)] public ushort Form { get; set; }
        [FlatBufferItem(2)] public ushort Region { get; set; }
        [FlatBufferItem(3)] public ushort CanEvolve { get; set; }
    }

    [FlatBufferStruct]
    public class ZukanData
    {
        [FlatBufferItem(0)] public ushort Index { get; set; }
        [FlatBufferItem(1)] public byte Group { get; set; }
    }

    [FlatBufferStruct]
    public class LearnedMove
    {
        [FlatBufferItem(0)] public ushort MoveId { get; set; }
        [FlatBufferItem(1)] public sbyte Level { get; set; }
    }

    [FlatBufferTable]
    public class Personal
    {
        [FlatBufferItem(0)] public SpeciesInfo Species { get; set; }
        [FlatBufferItem(1)] public bool IsPresent { get; set; }
        [FlatBufferItem(2)] public ZukanData Dex { get; set; }
        [FlatBufferItem(3)] public byte Type1 { get; set; }
        [FlatBufferItem(4)] public byte Type2 { get; set; }
        [FlatBufferItem(5)] public ushort Ability1 { get; set; }
        [FlatBufferItem(6)] public ushort Ability2 { get; set; }
        [FlatBufferItem(7)] public ushort Ability3 { get; set; }
        [FlatBufferItem(8)] public byte Growth { get; set; }
        [FlatBufferItem(9)] public byte Rate { get; set; }
        [FlatBufferItem(10)] public GenderData Gender { get; set; }
        [FlatBufferItem(11)] public byte Group1 { get; set; }
        [FlatBufferItem(12)] public byte Group2 { get; set; }
        [FlatBufferItem(13)] public Hatch Hatch { get; set; }
        [FlatBufferItem(14)] public byte HatchSteps { get; set; }
        [FlatBufferItem(15)] public byte BaseFriendship { get; set; }
        [FlatBufferItem(16)] public ushort BaseExp { get; set; }
        [FlatBufferItem(17)] public byte EvoStage { get; set; }
        [FlatBufferItem(18)] public byte UnkFlag { get; set; }
        [FlatBufferItem(19)] public StatInfo YieldInfo { get; set; }
        [FlatBufferItem(20)] public StatInfo Stats { get; set; }
        [FlatBufferItem(21)] public EvoData EvoTable { get; set; }
        [FlatBufferItem(22)] public IList<ushort> TechnicalMachines { get; set; }
        [FlatBufferItem(23)] public IList<ushort> EggMoves { get; set; }
        [FlatBufferItem(24)] public IList<ushort> ReminderMoves { get; set; }
        [FlatBufferItem(25)] public IList<LearnedMove> Learnset { get; set; }
    }

    [FlatBufferTable]
    public class PersonalTotal
    {
        [FlatBufferItem(0)] public IList<Personal> personalTable { get; set; }
    }

}
