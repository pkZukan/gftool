using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trinity.Core.Flatbuffers.TR.ResourceDictionary;
using Trinity;
using Trinity.Core.Math.Hash;
using Trinity.Core.Serializers.TR;
using Trinity.Core.Utils;
using System.Xml.XPath;
using Trinity.Core.Cache;
using Trinity.Core.Compression;
using System.IO;

namespace TrinityModLoader
{
    public partial class ExplorerPackViewer : DataGridView, IExplorerViewer
    {
        public const string arc_disk = "arc://";

        private DataGridViewTextBoxColumn FileName;
        private DataGridViewTextBoxColumn FileHash;
        private DataGridViewTextBoxColumn FileLocation;
        private DataGridViewTextBoxColumn FileType;
        private DataGridViewTextBoxColumn FileSize;

        private CustomFileDescriptor _fileDescriptor;
        private FileSystem _fileSystem;

        private List<string> packNames = new List<string>();
        public int activePack = -1;

        public ExplorerPackViewer()
        {
            FileName = new DataGridViewTextBoxColumn() { HeaderText = "Name", Name = "FileName", ReadOnly = true };
            FileHash = new DataGridViewTextBoxColumn() { HeaderText = "Hash", Name = "FileHash", ReadOnly = true };
            FileLocation = new DataGridViewTextBoxColumn() { HeaderText = "Location", Name = "FileLocation", ReadOnly = true };
            FileType = new DataGridViewTextBoxColumn() { HeaderText = "Type", Name = "FileType", ReadOnly = true };
            FileSize = new DataGridViewTextBoxColumn() { HeaderText = "Size", Name = "FileSize", ReadOnly = true };

            Columns.AddRange(new DataGridViewColumn[] { FileName, FileHash, FileLocation, FileType, FileSize });
        }

        public string GetCwd()
        {
            var cwd = arc_disk;

            if (activePack != -1)
            {
                cwd += packNames[activePack];
            }

            return cwd;
        }

        public void ParseFileDescriptor(CustomFileDescriptor fileDescriptor)
        {
            _fileDescriptor = fileDescriptor;
            packNames = _fileDescriptor.PackNames.ToList();
        }

        public string? GetPathAtIndex(int index)
        {
            var row = Rows[index];
            if (row.Cells["FileType"].Value.ToString().Equals("File Archive"))
            {
                var path = row.Cells["FileName"].Value.ToString();
                return path;
            }
            return null;
        }

        private void AddRowsFromPackNames(List<string> paths)
        {
            foreach (string path in paths)
            {
                var index = packNames.IndexOf(path);
                PackInfo? packInfo = _fileDescriptor.PackInfo[index];
                string[] row = new string[] { path, GFFNV.Hash(path).ToString("X16"), "arc", "File Archive", packInfo.FileSize.ToString() + "B" };
                Rows.Add(row);
            }
        }

        private void AddRowsFromPack(int index)
        {
            int fileIndex = Array.IndexOf(_fileSystem.FileHashes, GFFNV.Hash(packNames[index]));
            PackInfo? packInfo = _fileDescriptor.PackInfo[index];

            byte[] fileBytes = ONEFILESerializer.SplitTRPAK(Path.Join(ModLoaderSettings.GetRomFSPath(), Settings.trpfsRel), (long)_fileSystem.FileOffsets[fileIndex], (long)packInfo.FileSize);
            PackedArchive pack = FlatBufferConverter.DeserializeFrom<PackedArchive>(fileBytes);

            for (int i = 0; i < pack.FileEntry.Length; i++)
            {
                var hash = pack.FileHashes[i];
                var fileName = GFPakHashCache.GetName(hash);
                string? fileType = "unknown";
               
                if (fileName != null) {
                    fileType = fileName.Substring(fileName.IndexOf('.'));
                }

                string?[] row = new string?[] { fileName, hash.ToString("X16"), "arc", fileType, pack.FileEntry[i].FileSize.ToString()};
                Rows.Add(row);
            }
        }

        public void NavigateTo(string path)
        {
            Rows.Clear();

            activePack = packNames.IndexOf(path);

            if (activePack == -1)
            {
                AddRowsFromPackNames(packNames);
            }
            else
            {
                AddRowsFromPack(activePack);
            }
        }

        public void ParseFileSystem(FileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        public void Disable()
        {
            Rows.Clear();
            Visible = false;
        }

        public void Enable()
        {
            Visible = true;
        }

        public IEnumerable<ulong> GetFiles()
        {
            List<ulong> files = new List<ulong>();

            foreach (DataGridViewRow row in Rows)
            {
                var rowHash = row.Cells["FileHash"].Value;

                if (rowHash != null)
                {
                    files.Add(Convert.ToUInt64(rowHash.ToString(), 16));
                }

            }

            return files;
        }

        public IEnumerable<ulong> GetUnhashedFiles()
        {
            List<ulong> files = new List<ulong>();

            foreach (DataGridViewRow row in Rows)
            {
                if (row.Cells["FileName"].Value != null) continue;

                var rowHash = row.Cells["FileHash"].Value;

                if (rowHash != null)
                {
                    files.Add(Convert.ToUInt64(rowHash.ToString(), 16));
                }

            }

            return files;
        }

        public IEnumerable<ulong> GetFolderPaths(string path)
        {
            throw new NotImplementedException();
        }
    }
}
