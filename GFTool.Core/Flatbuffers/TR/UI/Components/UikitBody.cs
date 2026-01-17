using FlatSharp.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GFTool.Core.Flatbuffers.TR.UI.Components
{
    [FlatBufferTable]
    public class UikitBody
    {
        [FlatBufferItem(0)]
        public string Name { get; set; }

        [FlatBufferItem(1)]
        public string ControlName { get; set; }

        [FlatBufferItem(2)]
        public int ControlIndex { get; set; }

        [FlatBufferItem(3)]
        public string InSfx { get; set; }

        [FlatBufferItem(4)]
        public string OutSfx { get; set; }

        [FlatBufferItem(5)]
        public bool ApplyInSfx { get; set; }

        [FlatBufferItem(6)]
        public bool ApplyOutSfx { get; set; }

    }
}
