using FlatSharp.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GFTool.Core.Flatbuffers.TR.Scene.Components
{
    [FlatBufferTable]
    public class trinity_OverrideSensorData
    {
        [FlatBufferItem(0)]
        public float RealizingDistance { get; set; }

        [FlatBufferItem(1)]
        public float UnrealizingDistance { get; set; }

        [FlatBufferItem(2)]
        public float LoadingDistance { get; set; }

        [FlatBufferItem(3)]
        public float UnloadingDistance { get; set; }
    }
}
