using FlatSharp.Attributes;

namespace Trinity.Core.Flatbuffers.TR.ResourceDictionary
{

    [FlatBufferTable]
    public class FileInfo
    {
        [FlatBufferItem(0)] public UInt64 PackIndex { get; set; } = 0;
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


        public virtual long GetPackIndex(UInt64 hash)
        {
            long ret = -1;
            long ind = Array.IndexOf(FileHashes, hash);
            if (ind >= 0)
            {
                var finfo = FileInfo[ind];
                ret = (long)finfo.PackIndex;
            }

            return ret;
        }

        public virtual string GetPackName(UInt64 hash)
        {
            string ret = "";
            long ind = GetPackIndex(hash);
            if (ind >= 0) {
                ret = PackNames[ind];
            }

            return ret;
        }

        public virtual PackInfo? GetPackInfo(UInt64 hash)
        {
            PackInfo ret = null;
            long ind = GetPackIndex(hash);
            if (ind >= 0)
            {
                ret = PackInfo[ind];
            }

            return ret;
        }


    }
}
