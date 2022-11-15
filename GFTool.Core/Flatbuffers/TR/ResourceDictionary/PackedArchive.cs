using FlatSharp.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GFTool.Core.Flatbuffers.TR.ResourceDictionary
{
    [FlatBufferTable]
    public class PackedFile
    {
        [FlatBufferItem(0)] UInt32 Field_00 { get; set; }
        [FlatBufferItem(1)] Byte EncryptionType { get; set; }
        [FlatBufferItem(2)] Byte Level { get; set; }
        [FlatBufferItem(3)] UInt64 FileSize { get; set; }
        [FlatBufferItem(4)] Byte[] FileBuffer { get; set; } = Array.Empty<Byte>();

    }
    [FlatBufferTable]
    public class PackedArchive
    {
        [FlatBufferItem(0)] UInt64[] FileHashes { get; set; } = Array.Empty<UInt64>();
        [FlatBufferItem(1)] PackedFile[] FileEntry { get; set; } = Array.Empty<PackedFile>();
    }
}
