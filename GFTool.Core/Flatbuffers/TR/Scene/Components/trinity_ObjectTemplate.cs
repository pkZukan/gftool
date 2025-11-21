using FlatSharp.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trinity.Core.Flatbuffers.Utils;

namespace GFTool.Core.Flatbuffers.TR.Scene.Components
{
    [FlatBufferTable]
    public class trinity_ObjectTemplate
    {
        [FlatBufferItem(0)]
        public string Name { get; set; }

        [FlatBufferItem(1)]
        public string Scope { get; set; }

        [FlatBufferItem(2)]
        public string FilePath { get; set; }

        [FlatBufferItem(3)]
        public bool IsExpanded { get; set; }

        [FlatBufferItem(4)]
        public string EntityType { get; set; }

        [FlatBufferItem(5)]
        public byte[] EntityData { get; set; }
    }
}
