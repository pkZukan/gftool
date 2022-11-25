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

namespace GFTool.TrinityExplorer
{
    public partial class FileSystemForm : Form
    {
        private FileDescriptor? fileDescriptor;
        private FileSystem? fileSystem;
        private string? fs_name;
        private string? fd_name;

        public FileSystemForm()
        {
            InitializeComponent();
        }

        public TreeNode MakeTreeFromPaths(List<string> paths, string rootNodeName = "", char separator = '/')
        {
            var rootNode = new TreeNode(rootNodeName);
            foreach (var path in paths)
            {
                var currentNode = rootNode;
                var pathItems = path.Split(separator);
                foreach (var item in pathItems)
                {
                    var tmp = currentNode.Nodes.Cast<TreeNode>().Where(x => x.Text.Equals(item));
                    currentNode = tmp.Count() > 0 ? tmp.Single() : currentNode.Nodes.Add(item);
                    
                }
                ThreadSafe(() => progressBar1.Increment(1));
            }
            return rootNode;
        }

        private void LoadTree(ulong[] hashes) {
            var paths = hashes.Select(x => GFPakHashCache.GetName(x)).Where(x => !string.IsNullOrEmpty(x)).ToList();

            ThreadSafe(() => {
                statusLbl.Text = "Loading...";
                progressBar1.Maximum = paths.Count;
                progressBar1.Value = 0;
            });
            var nodes = MakeTreeFromPaths(paths);
            ThreadSafe(() => {
                fileView.Nodes.Add(nodes);
                statusLbl.Text = "Done";
            });
        }

        public void OpenFileDescriptor() {
            var openFileDialog = new OpenFileDialog()
            {
                Filter = "Trinity File Descriptor (*.trpfd) |*.trpfd",
            };
            if (openFileDialog.ShowDialog() != DialogResult.OK) return;

            fileDescriptor = FlatBufferConverter.DeserializeFrom<FileDescriptor>(openFileDialog.FileName);
            //fileSystem = ONEFILESerializer.DeserializeFileSystem(openFileDialog.FileName.Replace("trpfd", "trpfs"));

            if (fileDescriptor != null)
            {
                
                fileView.Nodes.Add("TRPFS");
                Task.Run(() => {
                    LoadTree(fileDescriptor.FileHashes);
                });
                
            }
        }

        void SaveFile(string file)
        {
            MessageBox.Show("Saving " + file);
        }

        void MarkFile(string file)
        {
            MessageBox.Show("Marking " + file);
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
        }

        /*private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (fileDescriptor== null)
            {
                return;
            }
            var index = comboBox1.SelectedIndex;

            listBox1.Items.Clear();
            for (int i = 0; i < fileDescriptor.FileInfo.Length; i++)
            {
                var FileInfo = fileDescriptor.FileInfo[i];
                var FileHash = fileDescriptor.FileHashes[i];

                if ((int)FileInfo.FileIndex == index)
                {
                    var name = GFTool.Core.Cache.GFPakHashCache.GetName(FileHash);
                    if (name == null)
                    {
                        name = FileHash.ToString("X16");
                    }
                    listBox1.Items.Add(name);
                }
            }
        }*/

        private void exportPackContentsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            /*var index = comboBox1.SelectedIndex;
            var packName = fileDescriptor.PackNames[index];
            var packInfo = fileDescriptor.PackInfo[index];
            var packHash = GFFNV.Hash(packName);

            var fileDialog = new FolderBrowserDialog()
            {

            };

            if (fileDialog.ShowDialog() == DialogResult.OK)
            {
                var fileIndex = Array.IndexOf(fileSystem.FileHashes, packHash);
                var fileBytes = ONEFILESerializer.SplitTRPAK(fs_name, (long)fileSystem.FileOffsets[fileIndex], (long)packInfo.FileSize);
                PackedArchive pack = FlatBufferConverter.DeserializeFrom<PackedArchive>(fileBytes);
                //TODO: Make a TRPAK Serializer class and use the model for it
                for (int i = 0; i < pack.FileEntry.Length; i++)
                {
                    var hash = pack.FileHashes[i];
                    var name = GFTool.Core.Cache.GFPakHashCache.GetName(hash);

                    if (name == null)
                    {
                        name = hash.ToString("X16");
                    }

                    var entry = pack.FileEntry[i];
                    var buffer = entry.FileBuffer;

                    if (entry.EncryptionType != -1)
                    {
                        buffer = Oodle.Decompress(buffer, (long)entry.FileSize);
                    }
                    var filepath = Path.Combine(fileDialog.SelectedPath, name);

                    if (!System.IO.Directory.Exists(Path.GetDirectoryName(filepath)))
                    {
                        System.IO.Directory.CreateDirectory(Path.GetDirectoryName(filepath));
                    }

                    BinaryWriter bw = new BinaryWriter(File.OpenWrite(Path.Combine(fileDialog.SelectedPath, name)));
                    bw.Write(buffer);
                    bw.Close();

                }
            }*/
        }

        private void exportPackFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            /*var index = comboBox1.SelectedIndex;
            var packName = fileDescriptor.PackNames[index];
            var packInfo = fileDescriptor.PackInfo[index];
            var fileDialog = new SaveFileDialog()
            {
                Filter = "Trinity FilePack (*.trpak) |*.trpak",
                FileName = packName.Replace("arc/", ""),
            };
            if (fileDialog.ShowDialog() == DialogResult.OK)
            {
                var fileHash = GFFNV.Hash(packName);
                var fileIndex = Array.IndexOf(fileSystem.FileHashes, fileHash);
                var fileBytes = ONEFILESerializer.SplitTRPAK(fs_name, (long)fileSystem.FileOffsets[fileIndex], (long)packInfo.FileSize);
                BinaryWriter bw = new BinaryWriter(File.OpenWrite(fileDialog.FileName));
                bw.Write(fileBytes);
                bw.Close();
            }
            */
        }

        private void fileView_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                Point ClickPoint = new Point(e.X, e.Y);
                TreeNode ClickNode = fileView.GetNodeAt(ClickPoint);
                if (ClickNode == null) return;
                
                Point ScreenPoint = fileView.PointToScreen(ClickPoint);  
                Point FormPoint = this.PointToClient(ScreenPoint);

                treeContext.Show(this, FormPoint);
            }
        }

        private void saveFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TreeNode node = fileView.SelectedNode;
            string file = node.FullPath.Replace("\\", "/").Substring(1);
            SaveFile(file);
        }

        private void markForLayeredFSToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TreeNode node = fileView.SelectedNode;
            string file = node.FullPath.Replace("\\", "/").Substring(1);
            MarkFile(file);
        }
        #endregion
    }
}