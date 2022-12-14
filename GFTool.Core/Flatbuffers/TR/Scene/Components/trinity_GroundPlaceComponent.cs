using FlatSharp.Attributes;

namespace GFTool.Core.Flatbuffers.TR.Scene.Components
{
    [FlatBufferTable]
    public class trinity_GroundPlaceComponent
    {
        [FlatBufferItem(0)]
        public uint index { get; set; }
    }
}
