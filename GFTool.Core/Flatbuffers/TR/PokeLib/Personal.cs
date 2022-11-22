using FlatSharp.Attributes;

namespace GFTool.Core.Flatbuffers.TR.PokeLib
{
    [FlatBufferTable]
    public class Personal
    {

    }

    [FlatBufferTable]
    public class PersonalTotal
    {
        [FlatBufferItem(0)]
        public List<Personal> personalTable { get; set; } = new List<Personal>();
    }
}
