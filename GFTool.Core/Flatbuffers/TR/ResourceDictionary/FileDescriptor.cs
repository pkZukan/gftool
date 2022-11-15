using FlatSharp.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GFTool.Core.Flatbuffers.TR.ResourceDictionary
{

    [FlatBufferTable]
    public class FileInfo
    {
        [FlatBufferItem(0)] public UInt64 FileIndex { get; set; } = 0;
        [FlatBufferItem(1)] public UInt32 UnusedTable { get; set; } = 0;
    }

    [FlatBufferTable]
    public class PackInfo
    {
        [FlatBufferItem(0)] public UInt64 FileSize { get; set; } = 0;
        [FlatBufferItem(1)] public UInt64 FileCount { get; set; } = 0;
    }

    [FlatBufferTable]
    public class FileDescriptor
    {
        [FlatBufferItem(0)] public UInt64[] FileHashes { get; set; } = Array.Empty<UInt64>();
        [FlatBufferItem(1)] public string[] PackNames { get; set; } = Array.Empty<string>();
        [FlatBufferItem(2)] public FileInfo[] FileInfo { get; set; } = Array.Empty<FileInfo>();
        [FlatBufferItem(3)] public PackInfo[] PackInfo { get; set; } = Array.Empty<PackInfo>();
    }

}
