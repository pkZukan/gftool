using FlatSharp.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GFTool.Core.Flatbuffers.TR.UI.Components
{
    [FlatBufferTable]
    public class UikitCursor
    {
        [FlatBufferItem(0)]
        public string Name { get; set; }

        [FlatBufferItem(1)]
        public string ControlName { get; set; }

        [FlatBufferItem(2)]
        public int ControlIndex { get; set; }
    }
}
