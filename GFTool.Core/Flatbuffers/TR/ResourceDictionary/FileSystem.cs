using FlatSharp.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GFTool.Core.Flatbuffers.TR.ResourceDictionary
{
    [FlatBufferTable]
    public class FileSystem
    {
        [FlatBufferItem(0)] public UInt64[] FileHashes { get; set; } = Array.Empty<UInt64>();
        [FlatBufferItem(1)] public UInt64[] FileOffsets { get; set; } = Array.Empty<UInt64>();
    }

}
