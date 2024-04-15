using FlatSharp.Attributes;
using Trinity.Core.Flatbuffers.TR.ResourceDictionary;
using FileInfo = Trinity.Core.Flatbuffers.TR.ResourceDictionary.FileInfo;

namespace TrinityModLoader
{
    [FlatBufferTable]
    public class CustomFileDescriptor
    {
        [FlatBufferItem(0)] public UInt64[] FileHashes { get; set; } = Array.Empty<UInt64>();
        [FlatBufferItem(1)] public string[] PackNames { get; set; } = Array.Empty<string>();
        [FlatBufferItem(2)] public FileInfo[] FileInfo { get; set; } = Array.Empty<FileInfo>();
        [FlatBufferItem(3)] public PackInfo[] PackInfo { get; set; } = Array.Empty<PackInfo>();

        //Only used by us
        [FlatBufferItem(4)] public UInt64[] UnusedHashes { get; set; } = Array.Empty<UInt64>();
        [FlatBufferItem(5)] public FileInfo[] UnusedFileInfo { get; set; } = Array.Empty<FileInfo>();

        public void AddFile(UInt64 fileHash)
        {
            if (UnusedHashes == null || UnusedFileInfo == null) return;

            var fileHashes = FileHashes.ToList();
            var fileInfos = FileInfo.ToList();
            var unusedHashes = UnusedHashes.ToList();
            var unusedFileInfo = UnusedFileInfo.ToList();

            //Make sure hash exists in unused
            var unusedInd = Array.IndexOf(UnusedHashes, fileHash);
            if (unusedInd < 0) return;

            //Add hash back into usage, sort and get its index to place the fileinfo
            fileHashes.Add(fileHash);
            fileHashes.Sort();
            FileHashes = fileHashes.ToArray();

            //Insert file infos at the same index
            var ind = Array.IndexOf(FileHashes, fileHash);
            fileInfos.Insert(ind, unusedFileInfo[unusedInd]);

            //Finally remove from unused
            unusedFileInfo.Remove(unusedFileInfo[unusedInd]);
            unusedHashes.Remove(fileHash);

            UnusedHashes = unusedHashes.ToArray();
            UnusedFileInfo = unusedFileInfo.ToArray();
            FileInfo = fileInfos.ToArray();
        }

        public void RemoveFile(UInt64 fileHash)
        {
            int ind = Array.IndexOf(FileHashes, fileHash);
            if (ind < 0) return;

            var hashList = FileHashes.ToList();
            var fileInfoList = FileInfo.ToList();

            List<UInt64> unusedHashesList = (UnusedHashes == null) ? new List<UInt64>() : UnusedHashes.ToList();
            List<FileInfo> unusedFileInfoList = (UnusedFileInfo == null) ? new List<FileInfo>() : UnusedFileInfo.ToList();

            unusedHashesList.Add(hashList[ind]);
            unusedFileInfoList.Add(fileInfoList[ind]);

            hashList.RemoveAt(ind);
            fileInfoList.RemoveAt(ind);

            UnusedHashes = unusedHashesList.ToArray();
            UnusedFileInfo = unusedFileInfoList.ToArray();

            FileHashes = hashList.ToArray();
            FileInfo = fileInfoList.ToArray();
        }

        public bool IsFileUnused(UInt64 hash)
        {
            if (UnusedHashes == null) return false;
            return UnusedHashes.Contains(hash);
        }

        public bool HasUnusedFiles()
        {
            return UnusedHashes != null;
        }

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
            if (ind >= 0)
            {
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
