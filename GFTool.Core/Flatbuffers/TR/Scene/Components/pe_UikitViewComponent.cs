using FlatSharp.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GFTool.Core.Flatbuffers.TR.Scene.Components
{
    [FlatBufferTable]
    public class pe_UikitViewComponent
    {
        [FlatBufferItem(0)]
        public string FilePath { get; set; }

        [FlatBufferItem(1)]
        public string BluaPath { get; set; }
    }
}
