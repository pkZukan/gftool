using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trinity.Core.Cache;
using Trinity.Core.Flatbuffers.TR.ResourceDictionary;
using Trinity.Core.Math.Hash;
using Trinity.Core.Utils;

namespace TrinityFileExplorer
{
    public partial class ExplorerFileViewer : DataGridView, IExplorerViewer
    {
        public const string disk_path = "romfs://";

        private DataGridViewTextBoxColumn FileName;
        private DataGridViewTextBoxColumn FileHash;
        private DataGridViewTextBoxColumn FileLocation;
        private DataGridViewTextBoxColumn FileType;
        private DataGridViewTextBoxColumn FileSize;

        private CustomFileDescriptor _fileDescriptor;
        private FileSystem _fileSystem;

        private List<string> romfs_paths = new List<string>();
        private List<string> layeredfs_paths = new List<string>();
        private string _cwd = "";

        public ExplorerFileViewer()
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
            return disk_path + _cwd;
        }

        public void ParseFileDescriptor(CustomFileDescriptor fileDescriptor)
        {
            _fileDescriptor = fileDescriptor;

            ulong[] fileHashes = _fileDescriptor.FileHashes;
            ulong[] markedHashes = _fileDescriptor.HasUnusedFiles() ? _fileDescriptor.UnusedHashes : new ulong[] { };

            romfs_paths = fileHashes.Select(x => GFPakHashCache.GetName(x)).Where(x => !string.IsNullOrEmpty(x)).ToList();
            layeredfs_paths = markedHashes.Select(x => GFPakHashCache.GetName(x)).Where(x => !string.IsNullOrEmpty(x)).ToList();

            romfs_paths.Sort();
            layeredfs_paths.Sort();

            NavigateTo();
        }

        private void AddRowFromPath(string path, string location)
        {
            var pathItems = path.Split('/');
            var pathItem = pathItems.First();

            var isFile = pathItem.IndexOf(".");

            if (FindRowFromPath(pathItem) != null) return;

            string[] row = isFile == -1
                ? (new string[] { pathItem, "-", location, "File Folder", "-" })
                : (new string[] { pathItem, GFFNV.Hash(_cwd + path).ToString("X16"), location, pathItem.Substring(isFile), "-" });

            Rows.Add(row);
        }

        private void AddRowsFromPaths(List<string> paths, string location)
        {
            foreach (var path in paths)
            {
                AddRowFromPath(path.Substring(_cwd.Length), location);
            }
        }

        private DataGridViewRow? FindRowFromPath(string path)
        {
            if (Rows.Count == 0) return null;
            try
            {
                return Rows.Cast<DataGridViewRow>().FirstOrDefault(row => row.Cells["FileName"].Value.ToString().Equals(path));
            }
            catch
            {
                return null;
            }
        }

        public void NavigateTo(string path = "")
        {
            Rows.Clear();
            _cwd = path;

            AddRowsFromPaths(romfs_paths.Where(x => x.StartsWith(_cwd)).ToList(), "romfs");
            AddRowsFromPaths(layeredfs_paths.Where(x => x.StartsWith(_cwd)).ToList(), "layeredfs");
        }

        public string? GetPathAtIndex(int index)
        {
            if (index < 0) return null;

            System.Console.WriteLine(index);
            var row = Rows[index];

            if (row.Cells["FileType"].Value.ToString().Equals("File Folder"))
            {
                var path = _cwd + row.Cells["FileName"].Value.ToString() + "/";
                return path;
            }

            return null;
        }

        private void MovePath(string path, List<string> sourcePaths, List<string> destinationPaths)
        {
            sourcePaths.Remove(path);
            destinationPaths.Add(path);
        }

        private void MovePaths(DataGridViewRow row, List<string> sourcePaths, List<string> destinationPaths, string fileLocation)
        {
            var fileName = _cwd + row.Cells["FileName"].Value.ToString();
            row.Cells["FileLocation"].Value = fileLocation;

            if (row.Cells["FileType"].Value == "File Folder")
            {
                var folderPaths = sourcePaths.Where(x => x.StartsWith(fileName)).ToList();
                foreach (var path in folderPaths)
                {
                    MovePath(path, sourcePaths, destinationPaths);
                }
            }
            else
            {
                MovePath(fileName, sourcePaths, destinationPaths);
            }
        }

        internal void RemoveFromLayeredFS()
        {
            foreach (DataGridViewRow row in SelectedRows)
            {
                MovePaths(row, layeredfs_paths, romfs_paths, "romfs");
            }
        }

        internal void AddToLayeredFS()
        {
            foreach (DataGridViewRow row in SelectedRows)
            {
                MovePaths(row, romfs_paths, layeredfs_paths, "layeredfs");
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
            return romfs_paths.Where(x => x.StartsWith(path)).Select(x => Convert.ToUInt64(x.ToString(), 16));
        }

        public string GetDiskPath()
        {
            return disk_path;
        }
    }
}
