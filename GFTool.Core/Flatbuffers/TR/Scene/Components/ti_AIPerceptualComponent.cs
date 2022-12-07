using FlatSharp.Attributes;

namespace GFTool.Core.Flatbuffers.TR.Scene.Components
{
    [FlatBufferTable]
    public class ti_AIPerceptualComponent
    {
        [FlatBufferItem(0)]
        public bool value { get; set; }
    }
}
