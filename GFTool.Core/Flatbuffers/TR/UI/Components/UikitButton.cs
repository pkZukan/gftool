using FlatSharp.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GFTool.Core.Flatbuffers.TR.UI.Components
{
    [FlatBufferTable]
    public class UikitButton
    {
        [FlatBufferItem(0)]
        public string Name { get; set; }

        [FlatBufferItem(1)]
        public string ControlName { get; set; }

        [FlatBufferItem(2)]
        public int ControlIndex { get; set; }

        [FlatBufferItem(3)]
        public string ActionName { get; set; }
        [FlatBufferItem(4)] public int ShortcutKey { get; set; }
        [FlatBufferItem(5)] public bool Repeat { get; set; }
        [FlatBufferItem(6)] public string DecideSfx { get; set; }
        [FlatBufferItem(7)] public string ShortcutSfx { get; set; }
        [FlatBufferItem(8)] public string ReleaseSfx { get; set; }
    }
}
