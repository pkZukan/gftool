using FlatSharp.Attributes;

namespace GFTool.Core.Flatbuffers.TR.Scene.Components
{
    [FlatBufferTable]
    public class TrinityGroundPlaceComponent
    {
        [FlatBufferItem(0)]
        public uint index { get; set; }
    }
}
