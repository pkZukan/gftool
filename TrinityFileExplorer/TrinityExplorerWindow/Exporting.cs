using Trinity.Core.Math.Hash;
using Trinity.Core.Cache;
using Trinity.Core.Compression;
using Trinity.Core.Flatbuffers.TR.ResourceDictionary;
using Trinity.Core.Serializers.TR;
using Trinity.Core.Utils;
using System.Linq;
using System.Diagnostics;
using Trinity.Core.Flatbuffers.TR.Model;
using System.Drawing;


namespace TrinityFileExplorer
{
    public partial class TrinityExplorerWindow : Form
    {
        private void SaveFile(ulong fileHash, string outFolder)
        {
            if (fileDescriptor == null || fileSystem == null)
            {
                return;
            }

            PackedArchive pack = GetPack(fileHash);

            for (int i = 0; i < pack.FileEntry.Length; i++)
            {
                var hash = pack.FileHashes[i];

                if (hash == fileHash)
                {
                    var fileName = GFPakHashCache.GetName(hash);
                    fileName ??= hash.ToString("X16") + ".bin";

                    var entry = pack.FileEntry[i];
                    var buffer = entry.FileBuffer;

                    if (entry.EncryptionType != -1)
                        buffer = Oodle.Decompress(buffer, (long)entry.FileSize);

                    var filepath = string.Format("{0}\\{1}", outFolder, fileName);

                    var dir = Path.GetDirectoryName(filepath);
                    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }

                    File.WriteAllBytes(filepath, buffer);

                    break;
                }
            }
        }

	        private PackedArchive GetPack(ulong fileHash)
	        {
                if (fileDescriptor == null || fileSystem == null)
                {
                    throw new InvalidOperationException("Exporting requires a loaded file descriptor and file system.");
                }

	            var packName = fileDescriptor.GetPackName(fileHash);
                if (string.IsNullOrEmpty(packName))
                {
                    throw new InvalidOperationException($"Failed to resolve pack name for file hash {fileHash:X16}.");
                }

                ulong packHash = GFFNV.Hash(packName);
	            int fileIndex = Array.IndexOf(fileSystem.FileHashes, packHash);
                if (fileIndex < 0)
                {
                    throw new InvalidOperationException($"Failed to locate pack '{packName}' ({packHash:X16}) in file system.");
                }

            PackInfo? packInfo = fileDescriptor.GetPackInfo(fileHash);
                if (packInfo == null)
                {
                    throw new InvalidOperationException($"Failed to resolve pack info for file hash {fileHash:X16}.");
                }
            byte[] fileBytes = ONEFILESerializer.SplitTRPAK(Path.Join(ExplorerSettings.GetRomFSPath(), FilepathSettings.trpfsRel), (long)fileSystem.FileOffsets[fileIndex], (long)packInfo.FileSize);

	            PackedArchive pack = FlatBufferConverter.DeserializeFrom<PackedArchive>(fileBytes);
	            return pack;
	        }

	        private void saveRomFSFileToolStripMenuItem_Click(object sender, EventArgs e)
	        {
	            if (!hasOodleDll) return;

            var sfd = new FolderBrowserDialog();
            if (sfd.ShowDialog() != DialogResult.OK) return;

            var grid = lastContextGrid ?? explorerFileViewer;
            foreach (DataGridViewRow row in grid.SelectedRows)
            {
                if (grid == explorerFileViewer && row.Cells["FileType"].Value.ToString() == "File Folder")
                {
                    SaveFolder(row.Cells["FileName"].Value.ToString(), sfd.SelectedPath);
                }
                else
                {
                    SaveFile(Convert.ToUInt64(row.Cells["FileHash"].Value.ToString(), 16), sfd.SelectedPath);
                }
            }
        }

        private void SaveFolder(string? v, string selectedPath)
        {
            if (string.IsNullOrEmpty(v) || fileDescriptor == null || fileSystem == null)
            {
                return;
            }

            var cwd = explorerFileViewer.GetCwd();
            var diskPath = explorerFileViewer.GetDiskPath();
            var relativeCwd = cwd.StartsWith(diskPath, StringComparison.Ordinal)
                ? cwd.Substring(diskPath.Length)
                : cwd;
            var folderPath = $"{relativeCwd}{v}/";
            var hashes = explorerFileViewer.GetFolderPaths(folderPath).Distinct().ToArray();

            if (hashes.Length == 0)
            {
                return;
            }

            var exportWindow = new ExportProgressWindow(fileDescriptor, fileSystem);
            exportWindow.Show();
            exportWindow.SaveFiles(hashes, selectedPath);
            exportWindow.Close();
        }

        private void RemoveFromLayeredFSMenuItem_Click(object sender, EventArgs e)
        {
            explorerFileViewer.RemoveFromLayeredFS();
        }

        private void AddToLayeredFSMenuItem_Click(object sender, EventArgs e)
        {
            explorerFileViewer.AddToLayeredFS();
        }
        private void allFilesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!hasOodleDll) return;
            if (fileDescriptor == null || fileSystem == null) return;

            var sfd = new FolderBrowserDialog();
            if (sfd.ShowDialog() != DialogResult.OK) return;

            var export_window = new ExportProgressWindow(fileDescriptor, fileSystem);

            export_window.Show();
            export_window.SaveFiles(fileDescriptor.FileHashes.ToArray(), sfd.SelectedPath);
            export_window.Close();
        }

        private async void visibleFilesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!hasOodleDll) return;
            if (fileDescriptor == null || fileSystem == null) return;
            if (activeViewer == null) return;

            var sfd = new FolderBrowserDialog();
            if (sfd.ShowDialog() != DialogResult.OK) return;

            var export_window = new ExportProgressWindow(fileDescriptor, fileSystem);

            export_window.Show();
            export_window.SaveFiles(activeViewer.GetFiles().ToArray(), sfd.SelectedPath);
            export_window.Close();

        }

        private void unhashedFilesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!hasOodleDll) return;
            if (fileDescriptor == null || fileSystem == null) return;
            if (activeViewer == null) return;

            var sfd = new FolderBrowserDialog();
            if (sfd.ShowDialog() != DialogResult.OK) return;

            var export_window = new ExportProgressWindow(fileDescriptor, fileSystem);

            export_window.Show();
            export_window.SaveFiles(activeViewer.GetUnhashedFiles().ToArray(), sfd.SelectedPath);
            export_window.Close();
        }
    }
}
