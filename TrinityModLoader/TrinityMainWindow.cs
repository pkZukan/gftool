using Trinity.Core.Flatbuffers.TR.ResourceDictionary;
using Trinity.Core.Utils;
using Trinity.Core.Serializers.TR;
using Trinity.Core.Math.Hash;
using Trinity.Core.Compression;
using Trinity.Core.Cache;
using SharpCompress.Readers;
using SharpCompress.Common;
using System.Diagnostics;
using System.Text.Json;
using Tomlyn;
using Tomlyn.Model;
using TrinityModLoader;

namespace Trinity
{
    public partial class TrinityMainWindow : Form
    {
        private FileDescriptorCustom? fileDescriptor = null;
        private FileSystem? fileSystem = null;

        private bool hasOodleDll = false;
        private Settings settings;

        private string trpfdRel = @"\arc\data.trpfd";
        private string trpfsRel = @"\arc\data.trpfs";

        public TrinityMainWindow()
        {
            InitializeComponent();
            hasOodleDll = File.Exists("oo2core_8_win64.dll");
            if (!hasOodleDll)
                MessageBox.Show("Liboodle dll missing. Exporting from TRPFS is disabled.");
            LoadMods();
            LoadSettings();
        }

        public void LoadSettings()
        {
            var file = "settings.json";
            if (!File.Exists(file))
            {
                settings = new Settings();

                var romfs = new FolderBrowserDialog();
                if (romfs.ShowDialog() != DialogResult.OK) return;

                settings.archiveDir = romfs.SelectedPath;
                settings.Save();
            }
            else {
                settings = JsonSerializer.Deserialize<Settings>(File.ReadAllText(file));
                disableAutoLoad.Checked = !settings.autoloadTrpfd;
            }
        }

        //Populate mods from local mods/ folder
        public void LoadMods()
        {
            if (!Directory.Exists("mods")) {
                Directory.CreateDirectory("mods");
            }

            var files = Directory.EnumerateFiles("mods").Where(x => x.EndsWith(".zip") || x.EndsWith(".rar"));
            foreach ( var file in files)
            {
                var name = Path.GetFileName(file);
                modList.Items.Add(name);
            }
        }

        public TreeNode MakeTreeFromPaths(List<string> paths, TreeNode rootNode, bool used = true)
        {
            foreach (var path in paths)
            {
                var currentNode = rootNode;
                var pathItems = path.Split('/');
                foreach (var item in pathItems)
                {
                    var tmp = currentNode.Nodes.Cast<TreeNode>().Where(x => x.Text.Equals(item));
                    TreeNode tn = new TreeNode();
                    if (tmp.Count() == 0)
                    {
                        tn.Text = item;
                        if (!used) tn.BackColor = Color.Red;
                        currentNode.Nodes.Add(tn);
                        currentNode = tn;
                    }
                    else 
                        currentNode = tmp.Single();
                }
                ThreadSafe(() => progressBar1.Increment(1));
            }
            return rootNode;
        }

        private TreeNode LoadTree(ulong[] hashes, ulong[] unused = null) {
            List<string> paths = hashes.Select(x => GFPakHashCache.GetName(x)).Where(x => !string.IsNullOrEmpty(x)).ToList();
            List<string> unusedPaths = null;
            if (unused != null) {
                unusedPaths = unused.Select(x => GFPakHashCache.GetName(x)).Where(x => !string.IsNullOrEmpty(x)).ToList();
                unusedPaths.Sort();
            }

            paths.Sort();

            ThreadSafe(() => {
                progressBar1.Maximum = paths.Count;
                progressBar1.Value = 0;
            });

            var rootNode = new TreeNode("romfs");
            var nodes = MakeTreeFromPaths(paths, rootNode);
            
            if(unused != null) 
                nodes = MakeTreeFromPaths(unusedPaths, nodes, false);

            return nodes;
        }

        public void OpenFileDescriptor() {
            var ofd = new OpenFileDialog()
            {
                Filter = "Trinity File Descriptor (*.trpfd) |*.trpfd",
                InitialDirectory = settings.archiveDir
            };
            if (ofd.ShowDialog() != DialogResult.OK) return;

            ParseFileDescriptor(ofd.FileName);
        }

        public void ParseFileDescriptor(string file = "") {
            var trpfs = settings.archiveDir + trpfsRel;
            var trpfd = settings.archiveDir + trpfdRel;

            try
            {
                fileDescriptor = FlatBufferConverter.DeserializeFrom<FileDescriptorCustom>(file != "" ? file : trpfd);

                if (File.Exists(trpfs))
                {
                    fileSystem = ONEFILESerializer.DeserializeFileSystem(trpfs);
                }
                else {
                    MessageBox.Show("Trpfs not found, saving files from treeview disabled. Check directory settings");
                }

                if (fileDescriptor != null)
                {
                    if (fileDescriptor.HasUnusedFiles())
                    {
                        MessageBox.Show("This is a modified trpfd");
                    }
                    Task.Run(() =>
                    {
                        ThreadSafe(() => { 
                            statusLbl.Text = "Loading...";
                            fileView.Nodes.Clear();
                        });
                        
                        TreeNode nodes = LoadTree(fileDescriptor.FileHashes, fileDescriptor.UnusedHashes);
                        ThreadSafe(() =>
                        {
                            fileView.Nodes.Add(nodes);
                            statusLbl.Text = "Done";
                            applyModsBut.Enabled = true;
                        });
                        
                    });
                }
            } catch {
                MessageBox.Show("Failed to load either trpfd or trpfs");
            }
        }

        void SaveFiles(TreeNode node, string outFolder)
        {
            string file = node.FullPath.Replace("\\", "/");
            file = file.Substring(file.IndexOf('/') + 1);

            var currNode = node.Nodes.Cast<TreeNode>();
            if (currNode.Count() > 0)
            {
                foreach (var n in currNode) SaveFiles(n, outFolder);
                return;
            }

            var fileHash = GFFNV.Hash(file);
            var packName = fileDescriptor.GetPackName(fileHash);
            var packHash = GFFNV.Hash(packName);
            var packInfo = fileDescriptor.GetPackInfo(fileHash);

            var fileIndex = Array.IndexOf(fileSystem.FileHashes, packHash);
            var fileBytes = ONEFILESerializer.SplitTRPAK(settings.archiveDir + trpfsRel, (long)fileSystem.FileOffsets[fileIndex], (long)packInfo.FileSize);
            PackedArchive pack = FlatBufferConverter.DeserializeFrom<PackedArchive>(fileBytes);
            for (int i = 0; i < pack.FileEntry.Length; i++)
            {
                var hash = pack.FileHashes[i];
                var name = GFPakHashCache.GetName(hash);

                if (hash == fileHash) {
                    if (name == null)
                        name = hash.ToString("X16");

                    var entry = pack.FileEntry[i];
                    var buffer = entry.FileBuffer;

                    if (entry.EncryptionType != -1)
                        buffer = Oodle.Decompress(buffer, (long)entry.FileSize);

                    var filepath = string.Format("{0}\\{1}", outFolder, name);

                    if (!Directory.Exists(Path.GetDirectoryName(filepath)))
                        Directory.CreateDirectory(Path.GetDirectoryName(filepath));

                    File.WriteAllBytes(filepath, buffer);

                    break;
                }
            }
        }

        void MarkFile(string file)
        {
            var hash = GFFNV.Hash(file);
            fileDescriptor?.RemoveFile(hash);
            MessageBox.Show("Marked file " + file);
            fileView.SelectedNode.BackColor = Color.Red;
        }

        void UnmarkFile(string file)
        {
            var hash = GFFNV.Hash(file);
            fileDescriptor?.AddFile(hash);
            MessageBox.Show("Unmark file " + file);
            fileView.SelectedNode.BackColor= Color.White;
        }

        List<string> EnumerateArchiveFiles(string file)
        {
            List<string> files = new List<string>();
            using (Stream stream = File.OpenRead(file))
            using (var reader = ReaderFactory.Open(stream))
            {
                while (reader.MoveToNextEntry())
                {
                    if (!reader.Entry.IsDirectory)
                    {
                        string entry = reader.Entry.Key;
                        files.Add(entry.Replace('\\', '/'));
                    }
                }
            }

            return files;
        }

        void ApplyModPack(string modFile, string lfsDir) 
        {
            List<string> files = new List<string>();
            using (Stream stream = File.OpenRead(@"mods\" + modFile))
            using (var reader = ReaderFactory.Open(stream))
            {
                while (reader.MoveToNextEntry())
                {
                    if (!reader.Entry.IsDirectory)
                    {
                        string entry = reader.Entry.Key;
                        if (entry.Contains("info.toml")) continue;
                        reader.WriteEntryToDirectory(lfsDir, new ExtractionOptions() { ExtractFullPath = true, Overwrite = true });
                        files.Add(entry.Replace('\\', '/'));
                    }
                }
            }

            foreach (var f in files) 
            {
                var fhash = GFFNV.Hash(f);
                fileDescriptor?.RemoveFile(fhash);
            }
        }

        void RemoveModPack(string modFile) 
        {
            var files = EnumerateArchiveFiles(@"mods\" + modFile);
            foreach (var f in files)
            {
                fileDescriptor.RemoveFile(GFFNV.Hash(f));
            }
        }

        void GuessModsInstalled() 
        {
            List<string> files = new List<string>();
            for (int i = 0; i < modList.Items.Count; i++)
            {
                using (Stream stream = File.OpenRead(@"mods\" + modList.Items[i].ToString()))
                using (var reader = ReaderFactory.Open(stream))
                {
                    while (reader.MoveToNextEntry())
                    {
                        if (!reader.Entry.IsDirectory)
                        {
                            string entry = reader.Entry.Key.Replace('\\', '/');
                            var hash = GFFNV.Hash(entry);
                            if (fileDescriptor.IsFileUnused(hash)) { //gonna assume if at least one of the files is marked, that the mod is installed since not all files will be in trpfs
                                modList.SetItemCheckState(i, CheckState.Checked);
                            }
                        }
                    }
                }
            }
        }

        private void AddModToList(string file)
        {
            var fn = Path.GetFileName(file);

            bool overwriting = false;
            if (modList.Items.Contains(fn))
            {
                var ow = MessageBox.Show("File already exists, do you want to overwrite?", "Mod exists", MessageBoxButtons.YesNo);
                if (ow == DialogResult.No) return;
                overwriting = true;
            }

            File.Copy(file, @"mods\" + fn, overwriting);

            if (!overwriting)
                modList.Items.Add(fn);
        }

        void SerializeTrpfd(string fileOut) 
        {
            var file = new System.IO.FileInfo(fileOut);
            if(!file.Directory.Exists) file.Directory.Create();

            var trpfd = FlatBufferConverter.SerializeFrom<FileDescriptorCustom>(fileDescriptor);
            File.WriteAllBytes(fileOut, trpfd);
        }

        private TomlTable FetchToml(string file)
        {
            var toml = "";
            using (Stream stream = File.OpenRead(file))
            using (var reader = ReaderFactory.Open(stream))
            {
                while (reader.MoveToNextEntry())
                {
                    if (!reader.Entry.IsDirectory)
                    {
                        string entry = reader.Entry.Key.Replace('\\', '/');
                        if (entry.EndsWith("info.toml")) {
                            using (var entryStream = reader.OpenEntryStream())
                            {
                                using (var r = new StreamReader(entryStream))
                                {
                                    toml = r.ReadToEnd();
                                }
                            }
                        }
                    }
                }
            }
            return Toml.ToModel(toml);
        }

        private void PopulateMetaData()
        {
            var mod = modList.Items[modList.SelectedIndex].ToString();
            var toml = FetchToml(@"mods\" + mod);
            if (toml.Count >= 3)
            {
                modNameLbl.Text = toml["display_name"].ToString();
                modDescriptionBox.Text = toml["description"].ToString();
                versionLbl.Text = toml["version"].ToString();
            }
            else 
            {
                modNameLbl.Text = mod;
                modDescriptionBox.Text = "None";
                versionLbl.Text = "Unknown";
            }
            
        }

        private void SaveTrpfsFiles()
        {
            var sfd = new FolderBrowserDialog();
            if (sfd.ShowDialog() != DialogResult.OK) return;

            statusLbl.Text = "Saving file...";
            SaveFiles(fileView.SelectedNode, sfd.SelectedPath);
            statusLbl.Text = "Done";
            MessageBox.Show("Done");
        }

        #region UTIL
        private void ThreadSafe(MethodInvoker method)
        {
            if (InvokeRequired)
                Invoke(method);
            else
                method();
        }
        #endregion

        #region UI_HANDLERS
        private void FileSystemForm_Load(object sender, EventArgs e)
        {
            if (File.Exists(settings.archiveDir + trpfsRel) && File.Exists(settings.archiveDir + trpfdRel) && settings.autoloadTrpfd)
                ParseFileDescriptor();
            else
                MessageBox.Show("Couldnt find trpfs/trpfd");
        }

        private void openFileDescriptorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDescriptor();
            GuessModsInstalled();
        }

        private void saveFileDescriptorAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var sfd = new SaveFileDialog()
            {
                Filter = "Trinity File Descriptor (*.trpfd) |*.trpfd",
            };
            if (sfd.ShowDialog() != DialogResult.OK) return;
            SerializeTrpfd(sfd.FileName);
            MessageBox.Show("Data saved");
        }

        private void fileView_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                Point ClickPoint = new Point(e.X, e.Y);
                TreeNode ClickNode = fileView.GetNodeAt(ClickPoint);
                fileView.SelectedNode = ClickNode;
                if (ClickNode == null) return;
                
                Point ScreenPoint = fileView.PointToScreen(ClickPoint);  
                Point FormPoint = this.PointToClient(ScreenPoint);
                if(ClickNode.BackColor == Color.Red)
                    treeContext.Items[1].Text = "Unmark for LayeredFS";
                treeContext.Show(this, FormPoint);
            }
        }

        private void saveFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!hasOodleDll) return;

            SaveTrpfsFiles();
        }

        private void markForLayeredFSToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TreeNode node = fileView.SelectedNode;
            string file = node.FullPath.Replace("\\", "/");
            file = file.Substring(file.IndexOf('/') + 1);
            if (node.BackColor != Color.Red)
                MarkFile(file);
            else
                UnmarkFile(file);
        }

        private void advancedViewToolStripMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            advancedPanel.Enabled = advancedToggle.Checked;
            advancedPanel.Visible = advancedToggle.Checked;
            basicPanel.Enabled = !advancedToggle.Checked;
            basicPanel.Visible = !advancedToggle.Checked;
        }

        private void addMod_Click(object sender, EventArgs e)
        {
            var openFileDialog = new OpenFileDialog()
            {
                Filter = "Zip Files (*.zip)|*.zip|Rar Files (*.rar)|*.rar|All (*.*)|*.*"
            };
            if (openFileDialog.ShowDialog() != DialogResult.OK) return;
            AddModToList(openFileDialog.FileName);
        }

        private void modList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (modList.SelectedIndex >= 0)
            {
                PopulateMetaData();
            }
        }

        private void applyModsBut_Click(object sender, EventArgs e)
        {
            statusLbl.Text = "Applying mods...";
            string lfsDir = settings.outputDir;
            if (Directory.Exists(lfsDir))
                Directory.Delete(lfsDir, true);
            Directory.CreateDirectory(lfsDir);
            for (int i = 0; i < modList.Items.Count; i++)
            {
                if (modList.GetItemChecked(i))
                    ApplyModPack(modList.Items[i].ToString(), lfsDir);
                else
                    RemoveModPack(modList.Items[i].ToString());
            }
            SerializeTrpfd(lfsDir + trpfdRel);
            statusLbl.Text = "Done";
            MessageBox.Show("Done!");

            var filePath = Path.GetFullPath(settings.outputDir);
            Process.Start("explorer.exe", string.Format("\"{0}\"", filePath));
        }

        private void modOrderUp_Click(object sender, EventArgs e)
        {
            if (modList.SelectedIndex < 0 || modList.SelectedIndex == 0) return;

            var selected = modList.SelectedIndex;
            var item = modList.Items[selected];
            modList.Items.RemoveAt(selected);
            modList.Items.Insert(selected - 1, item);
        }

        private void modOrderDown_Click(object sender, EventArgs e)
        {
            if (modList.SelectedIndex < 0 || modList.SelectedIndex >= modList.Items.Count - 1) return;

            var selected = modList.SelectedIndex;
            var item = modList.Items[selected];
            modList.Items.RemoveAt(selected);
            modList.Items.Insert(selected + 1, item);
        }
        

        private void showUnhashedFilesToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new About().ShowDialog();
        }

        private void openRomFSFolderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var romfs = new FolderBrowserDialog();
            if (romfs.ShowDialog() != DialogResult.OK) return;

            settings = new Settings();
            settings.archiveDir = romfs.SelectedPath;
            settings.Save();

            ParseFileDescriptor();
        }

        private void disableTRPFDAutoloadToolStripMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            settings.autoloadTrpfd = !disableAutoLoad.Checked;
            settings.Save();
        }

        private void deleteModBut_Click(object sender, EventArgs e)
        {
            var file = @"mods\" + modList.Items[modList.SelectedIndex].ToString();
            if (File.Exists(file)) {
                File.Delete(file);
                modList.Items.Remove(modList.Items[modList.SelectedIndex]);
            }
        }

        private void modList_MouseUp(object sender, MouseEventArgs e)
        {
            Point ClickPoint = new Point(e.X, e.Y);
            modList.SelectedIndex = modList.IndexFromPoint(ClickPoint);

            if (e.Button == MouseButtons.Right && modList.SelectedIndex >= 0) {
                
                basicContext.Show(modList, ClickPoint);
            }
        }
        

        private void setOutputFolderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var fold = new FolderBrowserDialog();
            if (fold.ShowDialog() != DialogResult.OK) return;
            settings.outputDir = fold.SelectedPath + @"\romfs\";
            settings.Save();
        }
        #endregion
    }
}