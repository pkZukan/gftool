using FlatSharp.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GFTool.Core.Flatbuffers.TR.Scene.Components
{
    [FlatBufferTable]
    public class SubScene
    {
        [FlatBufferItem(0)]
        public string Filepath { get; set; }
        [FlatBufferItem(1)]
        public int unk1 { get; set; }
    }
}
