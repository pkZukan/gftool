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
        public CustomFileDescriptor? fileDescriptor = null;
        public FileSystem? fileSystem = null;

        private bool hasOodleDll = false;
        private bool exportable = false;

        private IExplorerViewer activeViewer;
        private DataGridView? lastContextGrid;

        private ToolStripMenuItem? settingsToolStripMenuItem;
        private ToolStripMenuItem? setModelViewerPathToolStripMenuItem;
        private readonly List<ToolStripMenuItem> openInModelViewerMenuItems = new List<ToolStripMenuItem>();
        private static readonly Color TrmdlHighlightColor = Color.FromArgb(220, 245, 255); // light blue

        public TrinityExplorerWindow()
        {
            InitializeComponent();
            InitializeModelViewerIntegration();
            InitializeSettings();
            InitializeCache();
            InitializeOodle();
            InitializeExplorer();
        }

        private void InitializeModelViewerIntegration()
        {
            AddSettingsMenu();
            AddOpenInModelViewerContextMenus();
            AddFileHighlighting();
        }

        private void AddFileHighlighting()
        {
            explorerFileViewer.CellFormatting += ExplorerGrid_CellFormatting;
            explorerPackViewer.CellFormatting += ExplorerGrid_CellFormatting;
        }

        private static void ExplorerGrid_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (sender is not DataGridView grid || e.RowIndex < 0 || e.RowIndex >= grid.Rows.Count)
            {
                return;
            }

            var row = grid.Rows[e.RowIndex];
            string fileType = row.Cells["FileType"].Value?.ToString() ?? string.Empty;
            string fileName = row.Cells["FileName"].Value?.ToString() ?? string.Empty;

            bool isTrmdl =
                fileType.Equals(".trmdl", StringComparison.OrdinalIgnoreCase) ||
                fileName.EndsWith(".trmdl", StringComparison.OrdinalIgnoreCase);

            if (isTrmdl)
            {
                row.DefaultCellStyle.BackColor = TrmdlHighlightColor;
            }
            else if (row.DefaultCellStyle.BackColor == TrmdlHighlightColor)
            {
                row.DefaultCellStyle.BackColor = grid.DefaultCellStyle.BackColor;
            }
        }

        private void AddSettingsMenu()
        {
            if (menuStrip1 == null)
            {
                return;
            }

            settingsToolStripMenuItem = new ToolStripMenuItem("Settings");
            setModelViewerPathToolStripMenuItem = new ToolStripMenuItem("Set Model Viewer Path...");
            setModelViewerPathToolStripMenuItem.Click += (_, _) => PromptForModelViewerPath(alwaysPrompt: true);
            settingsToolStripMenuItem.DropDownItems.Add(setModelViewerPathToolStripMenuItem);

            int viewIndex = menuStrip1.Items.IndexOf(viewToolStripMenuItem);
            if (viewIndex >= 0)
            {
                menuStrip1.Items.Insert(viewIndex + 1, settingsToolStripMenuItem);
            }
            else
            {
                menuStrip1.Items.Add(settingsToolStripMenuItem);
            }
        }

        private void AddOpenInModelViewerContextMenus()
        {
            void add(ContextMenuStrip? context)
            {
                if (context == null)
                {
                    return;
                }

                var item = new ToolStripMenuItem("Open in Model Viewer");
                item.Click += openInModelViewerToolStripMenuItem_Click;
                openInModelViewerMenuItems.Add(item);
                context.Items.Add(new ToolStripSeparator());
                context.Items.Add(item);
            }

            add(romFS_treeContext);
            add(layeredFS_treeContext);
            add(unhashed_treeContext);
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
                    GFPakHashCache.Clear();
                    GFPakHashCache.Save();
                    MessageBox.Show($"Empty GFPAKHashCache.bin created. ({GFPakHashCache.Count} entries)", "Hash Cache", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            else
            {
                GFPakHashCache.Open();
            }
        }

        private static readonly string[] DefaultHashlistUrls =
        {
            "https://raw.githubusercontent.com/pkZukan/PokeDocs/main/SV/Hashlists/FileSystem/hashes_inside_fd.txt",
            "https://raw.githubusercontent.com/pkZukan/PokeDocs/main/ZA/Hashlists/FileSystem/hashes_inside_fd.txt"
        };

        private void FetchHashesFromURL(string fileUrl = "https://raw.githubusercontent.com/pkZukan/PokeDocs/main/SV/Hashlists/FileSystem/hashes_inside_fd.txt")
        {
            FetchHashesFromURLs(new[] { fileUrl });
        }

        private void FetchHashesFromURLs(IEnumerable<string> fileUrls)
        {
            using var httpClient = new HttpClient();

            int success = 0;
            var failures = new List<string>();

            foreach (var url in fileUrls.Where(u => !string.IsNullOrWhiteSpace(u)))
            {
                try
                {
                    HttpResponseMessage response = httpClient.GetAsync(url).GetAwaiter().GetResult();
                    if (!response.IsSuccessStatusCode)
                    {
                        failures.Add(url);
                        continue;
                    }

                    Stream fileStream = response.Content.ReadAsStreamAsync().GetAwaiter().GetResult();
                    using var streamReader = new StreamReader(fileStream);

                    List<string> lines = new List<string>();
                    string? line;
                    while ((line = streamReader.ReadLine()) != null)
                    {
                        lines.Add(line);
                    }
                    GFPakHashCache.AddHashFromList(lines);
                    success++;
                }
                catch
                {
                    failures.Add(url);
                }
            }

            if (success > 0)
            {
                GFPakHashCache.Save();
                string msg = $"GFPAKHashCache.bin updated! ({GFPakHashCache.Count} entries)\nSources: {success}/{fileUrls.Count()}";
                if (failures.Count > 0)
                {
                    msg += "\nFailed:\n" + string.Join("\n", failures);
                }
                MessageBox.Show(msg, "Hash Cache", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                var message_text = "Failed to download latest hashes.\n\nManually download the \"hashes_inside_fd.txt\" file into your Trinity folder.\n\nClick OK to copy the URLs to your clipboard.";

                if (MessageBox.Show(message_text, "Failed to download", MessageBoxButtons.OKCancel) == DialogResult.OK)
                {
                    Clipboard.SetText(string.Join("\n", failures.Count > 0 ? failures : fileUrls));
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

        private void fileView_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                if (sender is not DataGridView grid)
                {
                    return;
                }

                Point ClickPoint = new Point(e.X, e.Y);
                DataGridView.HitTestInfo hit = grid.HitTest(e.X, e.Y);

                if (hit.Type != DataGridViewHitTestType.Cell) return;

                Point ScreenPoint = grid.PointToScreen(ClickPoint);
                Point FormPoint = this.PointToClient(ScreenPoint);

                var ClickRow = grid.Rows[hit.RowIndex];

                ClickRow.Selected = true;

                lastContextGrid = grid;
                ConfigureOpenInModelViewerMenuItems(grid, ClickRow);

                string location = ClickRow.Cells["FileLocation"].Value?.ToString() ?? string.Empty;

                if (location.Equals("romfs"))
                {
                    romFS_treeContext.Show(this, FormPoint);
                }
                else if (location.Equals("arc"))
                {
                    // Archive view (TRPFS pack contents) behaves like romfs export.
                    romFS_treeContext.Show(this, FormPoint);
                }
                else if (location.Equals("layeredfs"))
                {
                    layeredFS_treeContext.Show(this, FormPoint);
                }
                else if (location.Equals("unhashed"))
                {
                    unhashed_treeContext.Show(this, FormPoint);
                }
                else if (location.Equals("trpfs"))
                {
                    unhashed_treeContext.Show(this, FormPoint);
                }
            }
        }

        private sealed class ModelViewerOpenContext
        {
            public required ulong FileHash { get; init; }
            public required string RomfsRelativePath { get; init; }
        }

        private void ConfigureOpenInModelViewerMenuItems(DataGridView grid, DataGridViewRow row)
        {
            if (openInModelViewerMenuItems.Count == 0)
            {
                return;
            }

            foreach (var item in openInModelViewerMenuItems)
            {
                item.Enabled = false;
                item.Tag = null;
            }

            if (!TryGetFileContext(grid, row, out var fileHash, out var romfsRelPath))
            {
                return;
            }

            if (!romfsRelPath.EndsWith(".trmdl", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var ctx = new ModelViewerOpenContext
            {
                FileHash = fileHash,
                RomfsRelativePath = romfsRelPath
            };

            foreach (var item in openInModelViewerMenuItems)
            {
                item.Enabled = true;
                item.Tag = ctx;
            }
        }

        private bool TryGetFileContext(DataGridView grid, DataGridViewRow row, out ulong fileHash, out string romfsRelPath)
        {
            fileHash = 0;
            romfsRelPath = string.Empty;

            string? hashText = row.Cells["FileHash"].Value?.ToString();
            if (string.IsNullOrWhiteSpace(hashText) || hashText == "-")
            {
                return false;
            }

            if (!ulong.TryParse(hashText, System.Globalization.NumberStyles.HexNumber, null, out fileHash))
            {
                return false;
            }

            string? name = row.Cells["FileName"].Value?.ToString();
            if (string.IsNullOrWhiteSpace(name))
            {
                return false;
            }

            name = name.Replace('\\', '/');

            if (grid is ExplorerFileViewer efv)
            {
                string cwd = efv.GetCwd();
                string diskPath = efv.GetDiskPath();
                string relativeCwd = cwd.StartsWith(diskPath, StringComparison.Ordinal)
                    ? cwd.Substring(diskPath.Length)
                    : cwd;
                romfsRelPath = (relativeCwd + name).Replace('\\', '/');
                romfsRelPath = romfsRelPath.TrimStart('/');
                return true;
            }

            // Pack viewer lists already use full romfs relative paths from the hash cache when available.
            romfsRelPath = name.TrimStart('/');
            return true;
        }

        private void openInModelViewerToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            if (sender is not ToolStripMenuItem item || item.Tag is not ModelViewerOpenContext ctx)
            {
                return;
            }

            if (!hasOodleDll)
            {
                MessageBox.Show("LibOodle (oo2core_8_win64.dll) is missing.\nExporting from TRPFS is disabled.", "Missing Oodle");
                return;
            }

            if (fileDescriptor == null || fileSystem == null)
            {
                MessageBox.Show("TRPFD/TRPFS not loaded.", "Missing Data", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!EnsureModelViewerPathConfigured())
            {
                return;
            }

            try
            {
                string extractedTrmdlPath = ExtractTrmdlWithDependenciesToTemp(ctx.FileHash, ctx.RomfsRelativePath);
                LaunchModelViewer(extractedTrmdlPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open in Model Viewer:\n{ex.Message}", "Model Viewer", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private bool EnsureModelViewerPathConfigured()
        {
            string path = ExplorerSettings.GetModelViewerExePath();
            if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
            {
                return true;
            }

            return PromptForModelViewerPath(alwaysPrompt: false);
        }

        private bool PromptForModelViewerPath(bool alwaysPrompt)
        {
            if (!alwaysPrompt)
            {
                var result = MessageBox.Show(
                    "TrinityModelViewer path is not set.\nSelect TrinityModelViewer.exe now?",
                    "Model Viewer Path",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result != DialogResult.Yes)
                {
                    return false;
                }
            }

            using var ofd = new OpenFileDialog();
            ofd.Filter = "TrinityModelViewer (TrinityModelViewer.exe)|TrinityModelViewer.exe|Executables (*.exe)|*.exe|All files (*.*)|*.*";
            ofd.Title = "Select TrinityModelViewer.exe";

            if (ofd.ShowDialog(this) != DialogResult.OK)
            {
                return false;
            }

            ExplorerSettings.SetModelViewerExePath(ofd.FileName);
            ExplorerSettings.Save();
            return true;
        }

        private void LaunchModelViewer(string trmdlPath)
        {
            string exePath = ExplorerSettings.GetModelViewerExePath();
            if (string.IsNullOrWhiteSpace(exePath) || !File.Exists(exePath))
            {
                throw new FileNotFoundException("Model viewer executable not found.", exePath);
            }

            string? exeDir = Path.GetDirectoryName(exePath);
            if (string.IsNullOrWhiteSpace(exeDir) || !Directory.Exists(exeDir))
            {
                exeDir = Environment.CurrentDirectory;
            }

            var psi = new ProcessStartInfo
            {
                FileName = exePath,
                Arguments = $"\"{trmdlPath}\"",
                UseShellExecute = false,
                WorkingDirectory = exeDir
            };

            Process.Start(psi);
        }

        private sealed class TrpfsExtractor
        {
            private readonly CustomFileDescriptor fileDescriptor;
            private readonly FileSystem fileSystem;
            private readonly string trpfsPath;

            private readonly Dictionary<ulong, PackedArchive> packCache = new Dictionary<ulong, PackedArchive>();
            private readonly Dictionary<ulong, Dictionary<ulong, int>> packIndexCache = new Dictionary<ulong, Dictionary<ulong, int>>();

            public TrpfsExtractor(CustomFileDescriptor fileDescriptor, FileSystem fileSystem, string trpfsPath)
            {
                this.fileDescriptor = fileDescriptor;
                this.fileSystem = fileSystem;
                this.trpfsPath = trpfsPath;
            }

            public bool TryGetFileBytes(ulong fileHash, out byte[] bytes)
            {
                bytes = Array.Empty<byte>();

                if (!TryResolvePackInfo(fileHash, out string packName, out long packSize))
                {
                    return false;
                }

                ulong packHash = GFFNV.Hash(packName);
                if (!TryGetPack(packHash, packSize, out var pack))
                {
                    return false;
                }

                if (!TryGetPackEntryIndex(packHash, pack, fileHash, out int entryIndex))
                {
                    return false;
                }

                var entry = pack.FileEntry[entryIndex];
                bytes = entry.FileBuffer;
                if (entry.EncryptionType != -1)
                {
                    bytes = Oodle.Decompress(bytes, (long)entry.FileSize);
                }

                return true;
            }

            private bool TryResolvePackInfo(ulong fileHash, out string packName, out long packSize)
            {
                packName = string.Empty;
                packSize = 0;

                int idx = Array.IndexOf(fileDescriptor.FileHashes, fileHash);
                if (idx >= 0)
                {
                    ulong packIndex = fileDescriptor.FileInfo[idx].PackIndex;
                    if (packIndex < (ulong)fileDescriptor.PackNames.Length && packIndex < (ulong)fileDescriptor.PackInfo.Length)
                    {
                        packName = fileDescriptor.PackNames[packIndex];
                        packSize = checked((long)fileDescriptor.PackInfo[packIndex].FileSize);
                        return !string.IsNullOrWhiteSpace(packName);
                    }
                    return false;
                }

                if (fileDescriptor.UnusedHashes != null && fileDescriptor.UnusedFileInfo != null)
                {
                    int unusedIdx = Array.IndexOf(fileDescriptor.UnusedHashes, fileHash);
                    if (unusedIdx >= 0 && unusedIdx < fileDescriptor.UnusedFileInfo.Length)
                    {
                        ulong packIndex = fileDescriptor.UnusedFileInfo[unusedIdx].PackIndex;
                        if (packIndex < (ulong)fileDescriptor.PackNames.Length && packIndex < (ulong)fileDescriptor.PackInfo.Length)
                        {
                            packName = fileDescriptor.PackNames[packIndex];
                            packSize = checked((long)fileDescriptor.PackInfo[packIndex].FileSize);
                            return !string.IsNullOrWhiteSpace(packName);
                        }
                    }
                }

                return false;
            }

            private bool TryGetPack(ulong packHash, long packSize, out PackedArchive pack)
            {
                if (packCache.TryGetValue(packHash, out pack))
                {
                    return true;
                }

                int fileIndex = Array.IndexOf(fileSystem.FileHashes, packHash);
                if (fileIndex < 0)
                {
                    pack = null!;
                    return false;
                }

                byte[] fileBytes = ONEFILESerializer.SplitTRPAK(trpfsPath, (long)fileSystem.FileOffsets[fileIndex], packSize);
                pack = FlatBufferConverter.DeserializeFrom<PackedArchive>(fileBytes);
                packCache[packHash] = pack;
                return true;
            }

            private bool TryGetPackEntryIndex(ulong packHash, PackedArchive pack, ulong fileHash, out int index)
            {
                if (!packIndexCache.TryGetValue(packHash, out var map))
                {
                    map = new Dictionary<ulong, int>();
                    for (int i = 0; i < pack.FileHashes.Length; i++)
                    {
                        map[pack.FileHashes[i]] = i;
                    }
                    packIndexCache[packHash] = map;
                }

                return map.TryGetValue(fileHash, out index);
            }
        }

        private string ExtractTrmdlWithDependenciesToTemp(ulong trmdlFileHash, string trmdlRomfsRelativePath)
        {
            if (fileDescriptor == null || fileSystem == null)
            {
                throw new InvalidOperationException("TRPFD/TRPFS not loaded.");
            }

            string trpfsPath = Path.Join(ExplorerSettings.GetRomFSPath(), FilepathSettings.trpfsRel);
            var extractor = new TrpfsExtractor(fileDescriptor, fileSystem, trpfsPath);

            string importRoot = Path.Combine(Path.GetTempPath(), "GFTool", "TrinityModelViewerImport", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(importRoot);
            string normalizedTrmdl = NormalizeRomfsRelativePath(trmdlRomfsRelativePath);

            var pending = new Queue<string>();
            var extracted = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (!extractor.TryGetFileBytes(trmdlFileHash, out var trmdlBytes))
            {
                throw new FileNotFoundException("Failed to extract TRMDL from TRPFS.", normalizedTrmdl);
            }

            extracted.Add(normalizedTrmdl);
            WriteExtractedFile(importRoot, normalizedTrmdl, trmdlBytes);
            EnqueueTrmdlDependencies(normalizedTrmdl, trmdlBytes, pending);
            ExtractAnimationsForTrmdlIfPresent(importRoot, normalizedTrmdl, extractor);

            while (pending.Count > 0)
            {
                string relPath = NormalizeRomfsRelativePath(pending.Dequeue());
                if (!extracted.Add(relPath))
                {
                    continue;
                }

                ulong hash = GFFNV.Hash(relPath);
                if (!extractor.TryGetFileBytes(hash, out var bytes))
                {
                    continue;
                }

                WriteExtractedFile(importRoot, relPath, bytes);

                string ext = Path.GetExtension(relPath).ToLowerInvariant();
                if (ext == ".trmdl")
                {
                    EnqueueTrmdlDependencies(relPath, bytes, pending);
                }
                else if (ext == ".trmsh")
                {
                    EnqueueTrmshDependencies(relPath, bytes, pending);
                }
                else if (ext == ".trmtr")
                {
                    EnqueueTrmtrDependencies(relPath, bytes, pending);
                }
            }

            string extractedTrmdlAbs = Path.Combine(importRoot, normalizedTrmdl.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(extractedTrmdlAbs))
            {
                throw new FileNotFoundException("Failed to extract TRMDL.", extractedTrmdlAbs);
            }

            PruneModelViewerTempImports(currentImportRoot: importRoot, keepLatest: 5);
            return extractedTrmdlAbs;
        }

        private static void PruneModelViewerTempImports(string currentImportRoot, int keepLatest)
        {
            if (keepLatest < 1)
            {
                return;
            }

            try
            {
                var baseDir = Path.Combine(Path.GetTempPath(), "GFTool", "TrinityModelViewerImport");
                if (!Directory.Exists(baseDir))
                {
                    return;
                }

                var dirs = new DirectoryInfo(baseDir)
                    .EnumerateDirectories()
                    .Select(d =>
                    {
                        try
                        {
                            return d;
                        }
                        catch
                        {
                            return null;
                        }
                    })
                    .Where(d => d != null)
                    .Cast<DirectoryInfo>()
                    .OrderByDescending(d => d.CreationTimeUtc)
                    .ThenByDescending(d => d.Name, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (dirs.Count <= keepLatest)
                {
                    return;
                }

                var currentFull = Path.GetFullPath(currentImportRoot);
                int kept = 0;
                foreach (var dir in dirs)
                {
                    if (kept < keepLatest)
                    {
                        kept++;
                        continue;
                    }

                    string full;
                    try
                    {
                        full = Path.GetFullPath(dir.FullName);
                    }
                    catch
                    {
                        continue;
                    }

                    if (string.Equals(full, currentFull, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    try
                    {
                        dir.Delete(recursive: true);
                    }
                    catch
                    {
                        // Ignore delete failures (directory might be in use).
                    }
                }
            }
            catch
            {
                // Ignore pruning failures.
            }
        }

        private void ExtractAnimationsForTrmdlIfPresent(string importRoot, string trmdlRomfsRelativePath, TrpfsExtractor extractor)
        {
            if (fileDescriptor == null)
            {
                return;
            }

            string? motionDir = GuessMotionDirectoryFromTrmdl(trmdlRomfsRelativePath);
            if (string.IsNullOrWhiteSpace(motionDir))
            {
                return;
            }

            string prefix = NormalizeRomfsRelativePath(motionDir);
            if (!prefix.EndsWith("/", StringComparison.Ordinal))
            {
                prefix += "/";
            }

            const int maxAnims = 500;
            int extractedCount = 0;

            IEnumerable<ulong> allHashes = fileDescriptor.FileHashes ?? Array.Empty<ulong>();
            if (fileDescriptor.UnusedHashes != null)
            {
                allHashes = allHashes.Concat(fileDescriptor.UnusedHashes);
            }

            foreach (var hash in allHashes)
            {
                if (extractedCount >= maxAnims)
                {
                    break;
                }

                var name = GFPakHashCache.GetName(hash);
                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                string rel = NormalizeRomfsRelativePath(name);
                if (!rel.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!rel.EndsWith(".tranm", StringComparison.OrdinalIgnoreCase) &&
                    !rel.EndsWith(".gfbanm", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!extractor.TryGetFileBytes(hash, out var bytes))
                {
                    continue;
                }

                WriteExtractedFile(importRoot, rel, bytes);
                extractedCount++;
            }

            // File Explorer doesn't have the renderer MessageHandler; keep extraction silent here.
        }

        private static void WriteExtractedFile(string importRoot, string romfsRelativePath, byte[] bytes)
        {
            string outPath = Path.Combine(importRoot, romfsRelativePath.Replace('/', Path.DirectorySeparatorChar));
            string? dir = Path.GetDirectoryName(outPath);
            if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            File.WriteAllBytes(outPath, bytes);
        }

        private static void EnqueueTrmdlDependencies(string trmdlRomfsRelativePath, byte[] trmdlBytes, Queue<string> pending)
        {
            var mdl = FlatBufferConverter.DeserializeFrom<TRMDL>(trmdlBytes);
            string modelDir = GetDirectoryOrEmpty(trmdlRomfsRelativePath);

            if (mdl.Meshes != null)
            {
                foreach (var mesh in mdl.Meshes)
                {
                    if (!string.IsNullOrWhiteSpace(mesh?.PathName))
                    {
                        EnqueueRomfsPathCandidates(modelDir, mesh.PathName, pending);
                    }
                }
            }

            if (mdl.Materials != null)
            {
                foreach (var mat in mdl.Materials)
                {
                    if (!string.IsNullOrWhiteSpace(mat))
                    {
                        EnqueueRomfsPathCandidates(modelDir, mat, pending);
                    }
                }
            }

            if (mdl.Skeleton != null && !string.IsNullOrWhiteSpace(mdl.Skeleton.PathName))
            {
                string skelPath = CombineAndNormalizeRomfsPath(modelDir, mdl.Skeleton.PathName);
                pending.Enqueue(skelPath);

                string? category = GuessBaseSkeletonCategoryFromMesh(mdl.Meshes != null && mdl.Meshes.Length > 0 ? mdl.Meshes[0]?.PathName : null);
                if (!string.IsNullOrWhiteSpace(category))
                {
                    string skelDir = GetDirectoryOrEmpty(skelPath);
                    foreach (var relBase in GetBaseSkeletonCandidateRels(category))
                    {
                        pending.Enqueue(CombineAndNormalizeRomfsPath(skelDir, relBase));
                    }
                }
            }
        }

        private static void EnqueueTrmshDependencies(string trmshRomfsRelativePath, byte[] trmshBytes, Queue<string> pending)
        {
            var msh = FlatBufferConverter.DeserializeFrom<TRMSH>(trmshBytes);
            if (msh == null || string.IsNullOrWhiteSpace(msh.bufferFilePath))
            {
                return;
            }

            string mshDir = GetDirectoryOrEmpty(trmshRomfsRelativePath);
            EnqueueRomfsPathCandidates(mshDir, msh.bufferFilePath, pending);
        }

        private static void EnqueueTrmtrDependencies(string trmtrRomfsRelativePath, byte[] trmtrBytes, Queue<string> pending)
        {
            var mtr = FlatBufferConverter.DeserializeFrom<TRMTR>(trmtrBytes);
            if (mtr?.Materials == null)
            {
                return;
            }

            string mtrDir = GetDirectoryOrEmpty(trmtrRomfsRelativePath);
            foreach (var mat in mtr.Materials)
            {
                if (mat?.Textures == null)
                {
                    continue;
                }

                foreach (var tex in mat.Textures)
                {
                    if (string.IsNullOrWhiteSpace(tex?.File))
                    {
                        continue;
                    }

                    EnqueueRomfsPathCandidates(mtrDir, tex.File, pending);
                }
            }
        }

        private static string NormalizeRomfsRelativePath(string path)
        {
            path = (path ?? string.Empty).Replace('\\', '/').Trim();
            if (path.StartsWith("romfs://", StringComparison.OrdinalIgnoreCase))
            {
                path = path.Substring("romfs://".Length);
            }
            if (path.StartsWith("trpfs://", StringComparison.OrdinalIgnoreCase))
            {
                path = path.Substring("trpfs://".Length);
            }
            path = path.TrimStart('/');
            return path;
        }

        private static string GetDirectoryOrEmpty(string romfsRelativePath)
        {
            string normalized = NormalizeRomfsRelativePath(romfsRelativePath);
            int lastSlash = normalized.LastIndexOf('/');
            if (lastSlash < 0)
            {
                return string.Empty;
            }
            return normalized.Substring(0, lastSlash + 1);
        }

        private static string? GuessMotionDirectoryFromTrmdl(string trmdlRomfsRelativePath)
        {
            string dir = GetDirectoryOrEmpty(trmdlRomfsRelativePath);
            if (string.IsNullOrWhiteSpace(dir))
            {
                return null;
            }

            var parts = dir.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            int modelIndex = parts.FindIndex(p => p.StartsWith("model_", StringComparison.OrdinalIgnoreCase));
            if (modelIndex < 0)
            {
                return null;
            }

            string modelFolder = parts[modelIndex];
            string suffix = modelFolder.Length > "model_".Length ? modelFolder.Substring("model_".Length) : string.Empty;
            parts[modelIndex] = string.IsNullOrEmpty(suffix) ? "motion" : $"motion_{suffix}";

            return string.Join("/", parts) + "/";
        }

        private static string CombineAndNormalizeRomfsPath(string baseDir, string rel)
        {
            baseDir = NormalizeRomfsRelativePath(baseDir);
            rel = NormalizeRomfsRelativePath(rel);

            var parts = new List<string>();

            void push(string segment)
            {
                if (segment == "." || string.IsNullOrEmpty(segment))
                {
                    return;
                }
                if (segment == "..")
                {
                    if (parts.Count > 0)
                    {
                        parts.RemoveAt(parts.Count - 1);
                    }
                    return;
                }
                parts.Add(segment);
            }

            foreach (var seg in baseDir.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries))
            {
                push(seg);
            }
            foreach (var seg in rel.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries))
            {
                push(seg);
            }

            return string.Join("/", parts);
        }

        private static void EnqueueRomfsPathCandidates(string baseDir, string referencedPath, Queue<string> pending)
        {
            if (string.IsNullOrWhiteSpace(referencedPath))
            {
                return;
            }

            // Relative to base paths are queued first (most TR files use relative paths).
            pending.Enqueue(CombineAndNormalizeRomfsPath(baseDir, referencedPath));

            // Some TR files, especially textures, may store full romfs relative paths without dot segments.
            string raw = NormalizeRomfsRelativePath(referencedPath);
            if (!string.IsNullOrWhiteSpace(raw) &&
                !raw.StartsWith(".", StringComparison.Ordinal) &&
                !raw.Contains("/./", StringComparison.Ordinal) &&
                !raw.Contains("/../", StringComparison.Ordinal))
            {
                pending.Enqueue(raw);
            }
        }

        private static string? GuessBaseSkeletonCategoryFromMesh(string? meshPathName)
        {
            if (string.IsNullOrWhiteSpace(meshPathName))
            {
                return null;
            }

            string file = Path.GetFileName(meshPathName.Replace('\\', '/'));
            if (file.StartsWith("p0", StringComparison.OrdinalIgnoreCase) ||
                file.StartsWith("p1", StringComparison.OrdinalIgnoreCase) ||
                file.StartsWith("p2", StringComparison.OrdinalIgnoreCase))
            {
                return "Protag";
            }

            if (file.StartsWith("bu_", StringComparison.OrdinalIgnoreCase)) return "CommonNPCbu";
            if (file.StartsWith("dm_", StringComparison.OrdinalIgnoreCase)) return "CommonNPCdm";
            if (file.StartsWith("df_", StringComparison.OrdinalIgnoreCase)) return "CommonNPCdf";
            if (file.StartsWith("em_", StringComparison.OrdinalIgnoreCase)) return "CommonNPCem";
            if (file.StartsWith("fm_", StringComparison.OrdinalIgnoreCase)) return "CommonNPCfm";
            if (file.StartsWith("ff_", StringComparison.OrdinalIgnoreCase)) return "CommonNPCff";
            if (file.StartsWith("gm_", StringComparison.OrdinalIgnoreCase)) return "CommonNPCgm";
            if (file.StartsWith("gf_", StringComparison.OrdinalIgnoreCase)) return "CommonNPCgf";
            if (file.StartsWith("rv_", StringComparison.OrdinalIgnoreCase)) return "CommonNPCrv";

            return null;
        }

        private static IEnumerable<string> GetBaseSkeletonCandidateRels(string category)
        {
            return category switch
            {
                "Protag" => new[]
                {
                    "../../model_pc_base/model/p0_base.trskl",
                    "../../../../p2/model/base/p2_base0001_00_default/p2_base0001_00_default.trskl",
                    "../../p2/p2_base0001_00_default/p2_base0001_00_default.trskl"
                },
                "CommonNPCbu" => new[] { "../../../model_cc_base/bu/bu_base.trskl", "../../base/cc_base0001_00_young_m/cc_base0001_00_young_m.trskl" },
                "CommonNPCdm" or "CommonNPCdf" => new[] { "../../../model_cc_base/dm/dm_base.trskl", "../../base/cc_base0001_00_young_m/cc_base0001_00_young_m.trskl" },
                "CommonNPCem" => new[] { "../../../model_cc_base/em/em_base.trskl", "../../base/cc_base0001_00_young_m/cc_base0001_00_young_m.trskl" },
                "CommonNPCfm" or "CommonNPCff" => new[] { "../../../model_cc_base/fm/fm_base.trskl", "../../base/cc_base0001_00_young_m/cc_base0001_00_young_m.trskl" },
                "CommonNPCgm" or "CommonNPCgf" => new[] { "../../../model_cc_base/gm/gm_base.trskl", "../../base/cc_base0001_00_young_m/cc_base0001_00_young_m.trskl" },
                "CommonNPCrv" => new[] { "../../../model_cc_base/rv/rv_base.trskl", "../../base/cc_base0001_00_young_m/cc_base0001_00_young_m.trskl" },
                _ => Array.Empty<string>()
            };
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
            FetchHashesFromURLs(DefaultHashlistUrls);
            InitializeExplorer();
        }

        private void fromFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var ofd = new OpenFileDialog();
            if (ofd.ShowDialog() != DialogResult.OK) return;
            GFPakHashCache.AddHashFromList(File.ReadAllLines(ofd.FileName).ToList());
            GFPakHashCache.Save();
            MessageBox.Show($"GFPAKHashCache.bin updated! ({GFPakHashCache.Count} entries)", "Hash Cache", MessageBoxButtons.OK, MessageBoxIcon.Information);
            InitializeExplorer();
        }

        private void fromURLToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FetchHashesFromURLs(DefaultHashlistUrls);
            InitializeExplorer();
        }
    }

}
