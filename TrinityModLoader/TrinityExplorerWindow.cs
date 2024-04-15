using Trinity.Core.Math.Hash;
using Trinity.Core.Cache;
using Trinity.Core.Compression;
using Trinity.Core.Flatbuffers.TR.ResourceDictionary;
using Trinity.Core.Serializers.TR;
using Trinity.Core.Utils;
using Trinity;

namespace TrinityModLoader
{
    public partial class TrinityExplorerWindow : Form
    {
        public CustomFileDescriptor? fileDescriptor = null;
        public FileSystem? fileSystem = null;

        private bool hasOodleDll = false;
        private bool exportable = false;

        private IExplorerViewer activeViewer;

        public TrinityExplorerWindow()
        {
            InitializeComponent();
            InitializeOodle();
            OpenRomFSFolder();
        }

        private void InitializeOodle()
        {
            hasOodleDll = File.Exists("oo2core_8_win64.dll");

            if (!hasOodleDll)
                MessageBox.Show("LibOodle (oo2core_8_win64.dll) is missing.\nExporting from TRPFS is disabled.");
        }

        public void OpenRomFSFolder()
        {
            activeViewer = explorerFileViewer;

            ParseFileDescriptor(Path.Join(ModLoaderSettings.GetRomFSPath(), Settings.trpfdRel));
            ParseFileSystem(Path.Join(ModLoaderSettings.GetRomFSPath(), Settings.trpfsRel));

            Text = $"Trinity Explorer - Viewing ROMFS - {ModLoaderSettings.GetRomFSPath()}";
            exportable = hasOodleDll ? true : false;
        }

        public void OpenFileDescriptor()
        {
            var ofd = new OpenFileDialog()
            {
                Filter = "Trinity File Descriptor (*.trpfd) |*.trpfd",
            };
            if (ofd.ShowDialog() != DialogResult.OK) return;

            ParseFileDescriptor(ofd.FileName);

            Text = $"Trinity Explorer - Viewing TRPFD - {ofd.FileName}";
            exportable = false;
        }

        private void ParseFileSystem(string trpfs)
        {
            if (Directory.Exists(trpfs))
            {
                MessageBox.Show("It appears data.trpfs file is split.\nPlease concatenate the files into a single data.trpfs.");
                return;
            }
            if (File.Exists(trpfs))
            {
                fileSystem = ONEFILESerializer.DeserializeFileSystem(trpfs);
                explorerFileViewer.ParseFileSystem(fileSystem);
                explorerPackViewer.ParseFileSystem(fileSystem);
            }
            else
            {
                MessageBox.Show("data.trpfs file not found.");
            }
        }

        private void UpdateCounts()
        {
            romfsItemLabel.Text = $"{fileDescriptor.FileHashes.Count()} Items";
            if (fileDescriptor.HasUnusedFiles()) layeredfsItemLabel.Text = $"{fileDescriptor.UnusedHashes.Count()} Marked";
        }

        private void ParseFileDescriptor(string trpfd)
        {
            openFileDescriptorToolStripMenuItem.Enabled = false;

            fileDescriptor = FlatBufferConverter.DeserializeFrom<CustomFileDescriptor>(trpfd);

            if (fileDescriptor != null)
            {
                if (fileDescriptor.HasUnusedFiles())
                {
                    MessageBox.Show("This is a modified data.trpfd, loading marked files.");
                }

                explorerFileViewer.ParseFileDescriptor(fileDescriptor);
                explorerPackViewer.ParseFileDescriptor(fileDescriptor);

                UpdateCounts();
                NavigateTo();
            }

            openFileDescriptorToolStripMenuItem.Enabled = true;
        }

        private void NavigateTo(string path = "")
        {
            activeViewer.NavigateTo(path);
            rootPathTextbox.Text = activeViewer.GetCwd();
        }

        private void SaveFile(ulong fileHash, string outFolder)
        {
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

                    if (!Directory.Exists(Path.GetDirectoryName(filepath)))
                        Directory.CreateDirectory(Path.GetDirectoryName(filepath));

                    File.WriteAllBytes(filepath, buffer);

                    break;
                }
            }

        }

        private PackedArchive GetPack(ulong fileHash)
        {
            ulong packHash = GFFNV.Hash(fileDescriptor.GetPackName(fileHash));
            int fileIndex = Array.IndexOf(fileSystem.FileHashes, packHash);

            PackInfo? packInfo = fileDescriptor.GetPackInfo(fileHash);
            byte[] fileBytes = ONEFILESerializer.SplitTRPAK(Path.Join(ModLoaderSettings.GetRomFSPath(), Settings.trpfsRel), (long)fileSystem.FileOffsets[fileIndex], (long)packInfo.FileSize);

            PackedArchive pack = FlatBufferConverter.DeserializeFrom<PackedArchive>(fileBytes);
            return pack;
        }

        private void TrinityExplorerWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            explorerFileViewer.Rows.Clear();
        }

        private void openFileDescriptorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDescriptor();
        }

        private void openRomFSToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenRomFSFolder();
        }

        private void saveRomFSFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!hasOodleDll) return;

            var sfd = new FolderBrowserDialog();
            if (sfd.ShowDialog() != DialogResult.OK) return;

            foreach (DataGridViewRow row in explorerFileViewer.SelectedRows)
            {
                if (row.Cells["FileType"].Value.ToString() == "File Folder")
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
            
            throw new NotImplementedException();
        }

        private void RemoveFromLayeredFSMenuItem_Click(object sender, EventArgs e)
        {
            explorerFileViewer.RemoveFromLayeredFS();
        }

        private void AddToLayeredFSMenuItem_Click(object sender, EventArgs e)
        {
            explorerFileViewer.AddToLayeredFS();
        }

        private void fileView_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                Point ClickPoint = new Point(e.X, e.Y);
                DataGridView.HitTestInfo hit = explorerFileViewer.HitTest(e.X, e.Y);

                if (hit.Type != DataGridViewHitTestType.Cell) return;

                Point ScreenPoint = explorerFileViewer.PointToScreen(ClickPoint);
                Point FormPoint = this.PointToClient(ScreenPoint);

                var ClickRow = explorerFileViewer.Rows[hit.RowIndex];

                ClickRow.Selected = true;

                if (ClickRow.Cells["FileLocation"].Value.ToString().Equals("romfs"))
                {
                    romFS_treeContext.Show(this, FormPoint);
                }
                else if (ClickRow.Cells["FileLocation"].Value.ToString().Equals("layeredfs"))
                {
                    layeredFS_treeContext.Show(this, FormPoint);
                }
                else if (ClickRow.Cells["FileLocation"].Value.ToString().Equals("unhashed"))
                {
                    unhashed_treeContext.Show(this, FormPoint);
                }
            }
        }

        private void fileView_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            var path = activeViewer.GetPathAtIndex(e.RowIndex);
            if (path == null) return;
            NavigateTo(path);
        }

        private void rootPathTextbox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;

                var path = rootPathTextbox.Text;
                path = path.Substring(path.IndexOf("://") + 3);

                if (path.Last() == '/')
                {
                    path = path.Remove(path.Length - 1, 1);
                }

                NavigateTo(path);
            }
        }

        private void navigateUpButton_Click(object sender, EventArgs e)
        {
            var path = activeViewer.GetCwd();
            path = path.Substring(path.IndexOf("://") + 3);

            if (String.IsNullOrEmpty(path)) {
                NavigateTo();
                return;
            }

            if (path.Last() == '/')
            {
                path = path.Remove(path.Length - 1, 1);
            }

            var lastItem = path.Split('/').Last();
            NavigateTo(path.Remove(path.IndexOf(lastItem)));
        }

        private void refreshButton_Click(object sender, EventArgs e)
        {
            var path = activeViewer.GetCwd();
            path = path.Substring(path.IndexOf("://") + 3);

            NavigateTo(path);
        }

        private void fileSystemToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (fileSystemToolStripMenuItem.Checked) return;

            fileSystemToolStripMenuItem.Checked = true;
            archivesToolStripMenuItem.Checked = false;

            activeViewer.Disable();
            activeViewer = explorerFileViewer;
            activeViewer.Enable();

            NavigateTo();
        }

        private void archivesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (archivesToolStripMenuItem.Checked) return;

            archivesToolStripMenuItem.Checked = true;
            fileSystemToolStripMenuItem.Checked = false;

            activeViewer.Disable();
            activeViewer = explorerPackViewer;
            activeViewer.Enable();

            NavigateTo();
        }

        private void allFilesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!hasOodleDll) return;

            var sfd = new FolderBrowserDialog();
            if (sfd.ShowDialog() != DialogResult.OK) return;

            foreach (ulong hash in fileDescriptor.FileHashes)
            {
                SaveFile(hash, sfd.SelectedPath);
            }
        }

        private void visibleFilesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!hasOodleDll) return;

            var sfd = new FolderBrowserDialog();
            if (sfd.ShowDialog() != DialogResult.OK) return;

            foreach (ulong hash in activeViewer.GetFiles())
            {
                SaveFile(hash, sfd.SelectedPath);
            }
        }

        private void unhashedFilesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!hasOodleDll) return;

            var sfd = new FolderBrowserDialog();
            if (sfd.ShowDialog() != DialogResult.OK) return;

            foreach (ulong hash in activeViewer.GetUnhashedFiles())
            {
                SaveFile(hash, sfd.SelectedPath);
            }
        }
    }

}
