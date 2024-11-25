using FlatSharp.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GFTool.Core.Flatbuffers.TR.Scene.Components
{
    [FlatBufferTable]
    public class DataEntry
    {
        [FlatBufferItem(0)]
        public string Name { get; set; }

        [FlatBufferItem(1)]
        public float FilePath { get; set; }
    }

    [FlatBufferTable]
    public class pe_FlatbuffersDataComponent
    {
        [FlatBufferItem(0)]
        public string BfbsFilePath { get; set; }

        [FlatBufferItem(1)]
        public DataEntry[] Data { get; set; }
    }
}
