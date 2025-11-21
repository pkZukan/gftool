using FlatSharp.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GFTool.Core.Flatbuffers.TR.Scene.Components
{
    [FlatBufferTable]
    public class pe_TextComponent
    {
        [FlatBufferItem(0)]
        public string FilePath { get; set; }

    }
}
