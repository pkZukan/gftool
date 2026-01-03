using FlatSharp.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trinity.Core.Flatbuffers.Utils;

namespace GFTool.Core.Flatbuffers.TR.UI.Components
{
    [FlatBufferTable]
    public class ButtonInfoTable
    {
        [FlatBufferItem(0)]
        public string Name { get; set; }

        [FlatBufferItem(1)]
        public Vector2i Pos { get; set; }

        [FlatBufferItem(2)]
        public Vector2i Size { get; set; }
    }

    [FlatBufferTable]
    public class UikitGridPanel
    {
        [FlatBufferItem(0)]
        public string Name { get; set; }

        [FlatBufferItem(1)]
        public string CursorName { get; set; }

        [FlatBufferItem(2)]
        public Vector2i GridSize { get; set; }

        [FlatBufferItem(3)]
        public int Mode { get; set; }

        [FlatBufferItem(4)]
        public ButtonInfoTable[] ButtonInfo { get; set; }
    }
}
