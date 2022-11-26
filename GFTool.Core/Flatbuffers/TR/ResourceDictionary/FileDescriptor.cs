using FlatSharp.Attributes;
using GFTool.Core.Math.Hash;
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

            fileHashes.Add(fileHash);
            fileHashes.Sort();
            FileHashes = fileHashes.ToArray();

            var ind = Array.IndexOf(FileHashes, fileHash);
            var unusedInd = Array.IndexOf(UnusedHashes, fileHash);
            fileInfos.Insert(ind, unusedFileInfo[unusedInd]);

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

        private long GetPackIndex(UInt64 hash)
        {
            long ret = -1;
            int ind = Array.IndexOf(FileHashes, hash);
            if (ind > 0)
            {
                var finfo = FileInfo[ind];
                ret = (long)finfo.PackIndex;
            }

            return ret;
        }

        public string GetPackName(UInt64 hash)
        {
            string ret = "";
            long ind = GetPackIndex(hash);
            if (ind > 0) {
                ret = PackNames[ind];
            }

            return ret;
        }

        public PackInfo? GetPackInfo(UInt64 hash)
        {
            PackInfo ret = null;
            long ind = GetPackIndex(hash);
            if (ind > 0)
            {
                ret = PackInfo[ind];
            }

            return ret;
        }
    }

}
