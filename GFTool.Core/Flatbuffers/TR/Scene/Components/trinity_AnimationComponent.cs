using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GFTool.Core.Flatbuffers.TR.Scene.Components
{
    [FlatSharp.Attributes.FlatBufferTable]
    public class trinity_AnimationComponent
    {
        [FlatSharp.Attributes.FlatBufferItem(0)] public string FilePath { get; set; }
        [FlatSharp.Attributes.FlatBufferItem(1)] public string Name { get; set; }
    }
}
