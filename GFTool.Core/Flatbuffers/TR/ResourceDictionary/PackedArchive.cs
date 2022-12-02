using FlatSharp.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Trinity.Core.Flatbuffers.TR.ResourceDictionary
{
    [FlatBufferTable]
    public class PackedFile
    {
        [FlatBufferItem(0)] public UInt32 Field_00 { get; set; }
        [FlatBufferItem(1)] public SByte EncryptionType { get; set; }
        [FlatBufferItem(2)] public Byte Level { get; set; }
        [FlatBufferItem(3)] public UInt64 FileSize { get; set; }
        [FlatBufferItem(4)] public Byte[] FileBuffer { get; set; } = Array.Empty<Byte>();

    }
    [FlatBufferTable]
    public class PackedArchive
    {
        [FlatBufferItem(0)] public UInt64[] FileHashes { get; set; } = Array.Empty<UInt64>();
        [FlatBufferItem(1)] public PackedFile[] FileEntry { get; set; } = Array.Empty<PackedFile>();
    }
}
