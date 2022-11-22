using GFTool.Core.Models.TR;
using GFTool.Core.Flatbuffers.TR.ResourceDictionary;
using GFTool.Core.Utils;
using GFTool.Core.Serializers.TR;
using System;
using GFTool.Core.Math.Hash;
using GFTool.Core.Compression;

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

        public void OpenFileDescriptor() {
            var openFileDialog = new OpenFileDialog()
            {
                DefaultExt = "trpfd",
                Filter = "Trinity File Descriptor (*.trpfd) |*.trpfd",
            };
            if (openFileDialog.ShowDialog(this) == DialogResult.OK) {
                fd_name = openFileDialog.FileName;
                fs_name = fd_name.Replace("trpfd", "trpfs");
                fileDescriptor = FlatBufferConverter.DeserializeFrom<FileDescriptor>(fd_name);
                fileSystem = ONEFILESerializer.DeserializeFileSystem(fs_name);

                if (fileDescriptor != null)
                {
                    comboBox1.Items.Clear();
                    foreach (string packName in fileDescriptor.PackNames)
                    {
                        comboBox1.Items.Add(packName);
                    }
                    comboBox1.SelectedIndex = 0;
                }
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
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
        }

        private void openFileDescriptorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDescriptor();
        }

        private void exportPackFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var index = comboBox1.SelectedIndex;
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

        }

        private void exportPackContentsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var index = comboBox1.SelectedIndex;
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
                for (int i =0; i < pack.FileEntry.Length; i++)
                {
                    var hash = pack.FileHashes[i];
                    var name = GFTool.Core.Cache.GFPakHashCache.GetName(hash);
                    
                    if (name == null)
                    {
                        name = hash.ToString("X16");
                    }

                    var entry = pack.FileEntry[i];
                    var buffer = entry.FileBuffer;

                    if (entry.EncryptionType != -1) {
                        buffer = Oodle.Decompress(buffer, (long) entry.FileSize);
                    }
                    var filepath = Path.Combine(fileDialog.SelectedPath, name);
                    
                    if (!System.IO.Directory.Exists(Path.GetDirectoryName(filepath))) {
                        System.IO.Directory.CreateDirectory(Path.GetDirectoryName(filepath));
                    }
                    
                    BinaryWriter bw = new BinaryWriter(File.OpenWrite(Path.Combine(fileDialog.SelectedPath, name)));
                    bw.Write(buffer);
                    bw.Close();

                }
            }
        }
    }
}