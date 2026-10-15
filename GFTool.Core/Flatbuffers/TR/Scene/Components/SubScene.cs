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
        [FlatBufferItem(1)] public string Group { get; set; }
        [FlatBufferItem(2)]
        public int unk1 { get; set; }
        [FlatBufferItem(3)] public int unk2 { get; set; }
        [FlatBufferItem(4)] public byte[] Data { get; set; }
    }
}
