using GFTool.Core.Models.TR;
using GFTool.Core.Flatbuffers.TR.ResourceDictionary;
using GFTool.Core.Utils;
using GFTool.Core.Serializers.TR;
using System;
using GFTool.Core.Math.Hash;
using GFTool.Core.Compression;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using GFTool.Core.Cache;
using System.Security.Policy;
using System.Xml.Linq;
using System.Collections.Generic;
using System.IO.Compression;
using SharpCompress.Readers;
using SharpCompress.Common;
using System.Diagnostics;
using System.Reflection;
using GFTool.FilesystemExplorer;
using GFTool.Core.Flatbuffers.TR.PokeLib;
using System.Text.Json;
using System.Security.Cryptography;

namespace GFTool.TrinityExplorer
{
    public partial class FileSystemForm : Form
    {
        private FileDescriptor? fileDescriptor = null;
        private FileSystem? fileSystem = null;

        private bool hasOodleDll = false;
        private Settings settings;

        public FileSystemForm()
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
                var settings = new Settings();
                settings.archiveDir = "";
                settings.autoloadTrpfd = true;
                settings.outputDir = "LayeredFS/";

                var romfs = new FolderBrowserDialog();
                if (romfs.ShowDialog() != DialogResult.OK) return;

                settings.archiveDir = romfs.SelectedPath + "/arc";
                var json = JsonSerializer.Serialize<Settings>(settings, new JsonSerializerOptions() { WriteIndented = true });
                File.WriteAllText(file, json);
            }
            else {
                settings = JsonSerializer.Deserialize<Settings>(File.ReadAllText(file));
                disableAutoLoad.Checked = !settings.autoloadTrpfd;
            }
        }

        void SaveSettings()
        {
            var json = JsonSerializer.Serialize<Settings>(settings, new JsonSerializerOptions() { WriteIndented = true });
            File.WriteAllText("settings.json", json);
        }

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
            }

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
            fileView.Nodes.Clear();

            ParseFileDescriptor(ofd.FileName);
        }

        public void ParseFileDescriptor(string file = "") {
            var trpfs = settings.archiveDir + "/data.trpfs";
            var trpfd = settings.archiveDir + "/data.trpfd";

            try
            {
                fileDescriptor = FlatBufferConverter.DeserializeFrom<FileDescriptor>(file != "" ? file : trpfd);

                if (File.Exists(trpfs))
                {
                    fileSystem = ONEFILESerializer.DeserializeFileSystem(trpfs);
                }

                if (fileDescriptor != null)
                {
                    Task.Run(() =>
                    {
                        ThreadSafe(() => statusLbl.Text = "Loading...");
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
            var packHash = GFFNV.Hash(fileDescriptor.GetPackName(fileHash));
            var packInfo = fileDescriptor.GetPackInfo(fileHash);

            var fileIndex = Array.IndexOf(fileSystem.FileHashes, packHash);
            var fileBytes = ONEFILESerializer.SplitTRPAK(settings.archiveDir + "/data.trpfs", (long)fileSystem.FileOffsets[fileIndex], (long)packInfo.FileSize);
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

                    var filepath = string.Format("{0}/{1}", outFolder, name);

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
            using (Stream stream = File.OpenRead("mods/" + modFile))
            using (var reader = ReaderFactory.Open(stream))
            {
                while (reader.MoveToNextEntry())
                {
                    if (!reader.Entry.IsDirectory)
                    {
                        string entry = reader.Entry.Key;
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
            var files = EnumerateArchiveFiles("mods/" + modFile);
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
                using (Stream stream = File.OpenRead("mods/" + modList.Items[i].ToString()))
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

        void SerializeTrpfd(string fileOut) 
        {
            var file = new System.IO.FileInfo(fileOut);
            if(!file.Directory.Exists) file.Directory.Create();

            var trpfd = FlatBufferConverter.SerializeFrom<FileDescriptor>(fileDescriptor);
            File.WriteAllBytes(fileOut, trpfd);
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
            TreeNode node = fileView.SelectedNode;

            if (!hasOodleDll) return;

            var sfd = new FolderBrowserDialog();
            if (sfd.ShowDialog() != DialogResult.OK) return;

            statusLbl.Text = "Saving file...";
            SaveFiles(node, sfd.SelectedPath);
            statusLbl.Text = "Done";
            MessageBox.Show("Done");
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
            var fn = Path.GetFileName(openFileDialog.FileName);

            bool overwriting = false;
            if (modList.Items.Contains(fn))
            {
                var ow = MessageBox.Show("File already exists, do you want to overwrite?", "Mod exists", MessageBoxButtons.YesNo);
                if (ow == DialogResult.No) return;
                overwriting = true;
            }

            File.Copy(openFileDialog.FileName, "mods/" + fn, overwriting);

            if (!overwriting)
                modList.Items.Add(fn);
        }

        private void modList_SelectedIndexChanged(object sender, EventArgs e)
        {
            var mod = modList.Items[modList.SelectedIndex].ToString();
            modNameLbl.Text = mod;
            authorLbl.Text = "Unknown";
            //TODO: Check for info file in zip
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
            SerializeTrpfd(lfsDir + "arc/data.trpfd");
            statusLbl.Text = "Done";
            MessageBox.Show("Done!");

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                Arguments = lfsDir,
                FileName = "explorer.exe"
            };

            Process.Start(startInfo);
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
            settings.archiveDir = romfs.SelectedPath + "/arc";
            SaveSettings();

            ParseFileDescriptor();
        }

        private void FileSystemForm_Load(object sender, EventArgs e)
        {
            if(settings.archiveDir != string.Empty && File.Exists(settings.archiveDir + "/data.trpfs") && File.Exists(settings.archiveDir + "/data.trpfd") && settings.autoloadTrpfd)
                ParseFileDescriptor();
        }

        private void disableTRPFDAutoloadToolStripMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            settings.autoloadTrpfd = !disableAutoLoad.Checked;
            SaveSettings();
        }

        private void deleteModBut_Click(object sender, EventArgs e)
        {
            //TODO: delete button in modlist
        }

        private void modList_MouseUp(object sender, MouseEventArgs e)
        {
            //TODO impl context menu in modlist
        }
        #endregion

        private void setOutputFolderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var fold = new FolderBrowserDialog();
            if (fold.ShowDialog() != DialogResult.OK) return;
            settings.outputDir = fold.SelectedPath + "/";
            SaveSettings();
        }
    }
}