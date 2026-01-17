using FlatSharp.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GFTool.Core.Flatbuffers.TR.UI.Components
{
    [FlatBufferTable]
    public class UikitShortcut
    {
        [FlatBufferItem(0)]
        public string Name { get; set; }

        [FlatBufferItem(1)]
        public int Key { get; set; }

        [FlatBufferItem(2)]
        public bool Repeat { get; set; }

        [FlatBufferItem(3)]
        public string ActionName { get; set; }

        [FlatBufferItem(4)]
        public string Sfx { get; set; }
    }
}
