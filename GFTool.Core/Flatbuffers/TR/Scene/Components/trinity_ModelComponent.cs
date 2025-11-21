using FlatSharp.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GFTool.Core.Flatbuffers.TR.Scene.Components
{
    [FlatBufferTable]
    public class trinity_ModelComponent
    {
        [FlatBufferItem(0)]
        public string FilePath { get; set; }

        [FlatBufferItem(1)]
        public string ResourceName { get; set; }

        [FlatBufferItem(2)]
        public string MetaFilePath { get; set; }

        [FlatBufferItem(3)]
        public string MetaItemName { get; set; }

        [FlatBufferItem(4)]
        public bool OverrideLodDist { get; set; }

        [FlatBufferItem(5)]
        public float LodDist0 { get; set; }

        [FlatBufferItem(6)]
        public float LodDist1 { get; set; }

        [FlatBufferItem(7)]
        public float LodDist2 { get; set; }
    }
}
