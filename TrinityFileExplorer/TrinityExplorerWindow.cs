using Trinity.Core.Math.Hash;
using Trinity.Core.Cache;
using Trinity.Core.Compression;
using Trinity.Core.Flatbuffers.TR.ResourceDictionary;
using Trinity.Core.Serializers.TR;
using Trinity.Core.Utils;

namespace TrinityFileExplorer
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
            InitializeSettings();
            InitializeCache();
            InitializeOodle();
            InitializeExplorer();
        }

        private void InitializeSettings()
        {
            ExplorerSettings.Open();

            var romfsDir = ExplorerSettings.GetRomFSPath();

            if (romfsDir != "" && Directory.Exists(romfsDir))
            {
                return;
            }

            MessageBox.Show("Please set your RomFS path.", "Missing RomFS");

            var folderBrowser = new FolderBrowserDialog();
            if (folderBrowser.ShowDialog() != DialogResult.OK)
            {
                NoValidRomFS();
            }

            ExplorerSettings.SetRomFSPath(folderBrowser.SelectedPath);
            ExplorerSettings.Save();
        }

        public void InitializeExplorer()
        {
            explorerFileViewer.Disable();
            explorerPackViewer.Disable();


            if (InitializeFileDescriptor(Path.Join(ExplorerSettings.GetRomFSPath(), FilepathSettings.trpfdRel)) != DialogResult.OK)
            {
                NoValidRomFS();
            }

            if (InitializeFileSystem(Path.Join(ExplorerSettings.GetRomFSPath(), FilepathSettings.trpfsRel)) != DialogResult.OK)
            {
                NoValidRomFS();
            }

            Text = $"Trinity Explorer - Viewing ROMFS - {ExplorerSettings.GetRomFSPath()}";
            exportable = hasOodleDll ? true : false;

            //Add config to prefer last viewer
            activeViewer = explorerFileViewer;
            activeViewer.Enable();

            NavigateTo();
        }

        private DialogResult InitializeFileDescriptor(string trpfd)
        {

            if (!TryLoadFileDescriptor(trpfd, out fileDescriptor))
            {
                return DialogResult.Abort;
            }

            if (fileDescriptor != null)
            {
                if (fileDescriptor.HasUnusedFiles())
                {
                    MessageBox.Show("This is a modified data.trpfd, files will be marked appropriately.");
                }

                explorerFileViewer.ParseFileDescriptor(fileDescriptor);
                explorerPackViewer.ParseFileDescriptor(fileDescriptor);

                UpdateCounts();
            }

            return DialogResult.OK;
        }


        private DialogResult InitializeFileSystem(string trpfs)
        {
            if (!TryLoadFileSystem(trpfs, out fileSystem))
            {
                return DialogResult.Abort;
            }

            if (fileSystem != null)
            {

                explorerFileViewer.ParseFileSystem(fileSystem);
                explorerPackViewer.ParseFileSystem(fileSystem);

            }

            return DialogResult.OK;
        }

        private bool TryLoadFileDescriptor(string trpfd, out CustomFileDescriptor customFileDescriptor)
        {
            if (!File.Exists(trpfd))
            {
                MessageBox.Show($"No TRPFD found in the provided RomFS folder.\nUsually it's in {trpfd}");
                customFileDescriptor = null;
                return false;
            }

            customFileDescriptor = FlatBufferConverter.DeserializeFrom<CustomFileDescriptor>(trpfd);
            if (customFileDescriptor == null)
            {
                MessageBox.Show("Failed to load TRPFD.");
                return false;
            }

            return true;
        }

        private bool TryLoadFileSystem(string trpfs, out FileSystem fileSystem)
        {
            if (Directory.Exists(trpfs))
            {
                MessageBox.Show("It appears data.trpfs file is split.\nPlease concatenate the files into a single data.trpfs.");
                fileSystem = null;
                return false;
            }

            if (!File.Exists(trpfs))
            {
                MessageBox.Show("data.trpfs file not found.");
                fileSystem = null;
                return false;
            }

            fileSystem = ONEFILESerializer.DeserializeFileSystem(trpfs);

            if (fileSystem == null)
            {
                MessageBox.Show("Failed to load TRPFS.");
                return false;
            }

            return true;

        }

        private void NoValidRomFS()
        {
            MessageBox.Show("Trinity Mod Explorer requires a valid RomFS folder to function.");
            Environment.Exit(0);
        }

        private void InitializeCache()
        {
            if (!File.Exists("GFPAKHashCache.bin"))
            {
                DialogResult dialogResult = MessageBox.Show("No GFPAKHashCache.bin found, do you want to create one?", "Missing Files", MessageBoxButtons.YesNo);

                if (dialogResult == DialogResult.Yes)
                {
                    FetchHashesFromURL();
                }
                else
                {
                    MessageBox.Show("Empty GFPAKHashCache.bin Created.");
                }
            }
            else
            {
                GFPakHashCache.Open();
            }
        }

        private void FetchHashesFromURL(string fileUrl = "https://raw.githubusercontent.com/pkZukan/PokeDocs/main/SV/Hashlists/FileSystem/hashes_inside_fd.txt")
        {
            using var httpClient = new HttpClient();

            HttpResponseMessage response = httpClient.GetAsync(fileUrl).GetAwaiter().GetResult();
            if (response.IsSuccessStatusCode)
            {
                Stream fileStream = response.Content.ReadAsStreamAsync().GetAwaiter().GetResult();
                var streamReader = new StreamReader(fileStream);

                List<string> lines = new List<string>();
                string line;
                while ((line = streamReader.ReadLine()) != null)
                {
                    lines.Add(line);
                }
                GFPakHashCache.AddHashFromList(lines);
                GFPakHashCache.Save();

                MessageBox.Show("GFPAKHashCache.bin Created!");
            }
            else
            {
                var message_text = "Failed to download latest hashes.\n\nManually download the \"hashes_inside_fd.txt\" file into your Trinity folder.\n\nClick OK to copy the URL of the file to your clipboard.";

                if (MessageBox.Show(message_text, "Failed to download", MessageBoxButtons.OKCancel) == DialogResult.OK)
                {
                    Clipboard.SetText(fileUrl);
                }
            }
        }

        private void InitializeOodle()
        {
            hasOodleDll = File.Exists("oo2core_8_win64.dll");

            if (!hasOodleDll)
                MessageBox.Show("LibOodle (oo2core_8_win64.dll) is missing.\nExporting from TRPFS is disabled.");
        }

        private void UpdateCounts()
        {
            romfsItemLabel.Text = $"{fileDescriptor.FileHashes.Count()} Items";
            if (fileDescriptor.HasUnusedFiles()) layeredfsItemLabel.Text = $"{fileDescriptor.UnusedHashes.Count()} Marked";
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
            byte[] fileBytes = ONEFILESerializer.SplitTRPAK(Path.Join(ExplorerSettings.GetRomFSPath(), FilepathSettings.trpfsRel), (long)fileSystem.FileOffsets[fileIndex], (long)packInfo.FileSize);

            PackedArchive pack = FlatBufferConverter.DeserializeFrom<PackedArchive>(fileBytes);
            return pack;
        }

        private void TrinityExplorerWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            explorerFileViewer.Rows.Clear();
        }

        private void openFileDescriptorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var ofd = new OpenFileDialog()
            {
                Filter = "Trinity File Descriptor (*.trpfd) |*.trpfd",
            };
            if (ofd.ShowDialog() != DialogResult.OK) return;

            InitializeFileDescriptor(ofd.FileName);

            Text = $"Trinity Explorer - Viewing TRPFD - {ofd.FileName}";
            exportable = false;
        }

        private void openRomFSToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var folderBrowser = new FolderBrowserDialog();
            if (folderBrowser.ShowDialog() != DialogResult.OK) return;

            ExplorerSettings.SetRomFSPath(folderBrowser.SelectedPath);

            if (InitializeFileDescriptor(Path.Join(ExplorerSettings.GetRomFSPath(), FilepathSettings.trpfdRel)) != DialogResult.OK)
            {
                return;
            }

            ExplorerSettings.Save();
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
                else if (ClickRow.Cells["FileLocation"].Value.ToString().Equals("trpfs"))
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

                if (String.IsNullOrEmpty(path))
                {
                    NavigateTo();
                    return;
                }

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

            if (String.IsNullOrEmpty(path))
            {
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

            var export_window = new ExportProgressWindow(fileDescriptor, fileSystem);

            export_window.Show();
            export_window.SaveFiles(fileDescriptor.FileHashes.ToArray(), sfd.SelectedPath);
            export_window.Close();
        }

        private async void visibleFilesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!hasOodleDll) return;

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

            var sfd = new FolderBrowserDialog();
            if (sfd.ShowDialog() != DialogResult.OK) return;

            var export_window = new ExportProgressWindow(fileDescriptor, fileSystem);

            export_window.Show();
            export_window.SaveFiles(activeViewer.GetUnhashedFiles().ToArray(), sfd.SelectedPath);
            export_window.Close();
        }

        private void latestToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FetchHashesFromURL();
            InitializeExplorer();
        }

        private void fromFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var ofd = new OpenFileDialog();
            if (ofd.ShowDialog() != DialogResult.OK) return;
            GFPakHashCache.AddHashFromList(File.ReadAllLines(ofd.FileName).ToList());
            GFPakHashCache.Save();
            MessageBox.Show("GFPAKHashCache.bin Created!");
            InitializeExplorer();
        }

        private void fromURLToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FetchHashesFromURL();
            InitializeExplorer();
        }
    }

}
