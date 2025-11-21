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
    public class trinity_TerrainStreamingSetting
    {
        [FlatBufferItem(0)]
        public float LowLoadRadius { get; set; }

        [FlatBufferItem(1)]
        public float MediumLoadRadius { get; set; }

        [FlatBufferItem(2)]
        public float HighLoadRadius { get; set; }

        [FlatBufferItem(3)]
        public float CollisionLoadRadius { get; set; }

        [FlatBufferItem(4)]
        public float TreeLoadRadius { get; set; }
    }
}
