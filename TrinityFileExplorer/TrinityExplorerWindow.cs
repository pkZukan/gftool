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

        private IExplorerViewer activeViewer = null!;
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

        private bool TryLoadFileDescriptor(string trpfd, out CustomFileDescriptor? customFileDescriptor)
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

        private bool TryLoadFileSystem(string trpfs, out FileSystem? fileSystem)
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
        private void InitializeOodle()
        {
            hasOodleDll = File.Exists("oo2core_8_win64.dll");

            if (!hasOodleDll)
                MessageBox.Show("LibOodle (oo2core_8_win64.dll) is missing.\nExporting from TRPFS is disabled.");
        }

        private void UpdateCounts()
        {
            if (fileDescriptor == null)
            {
                return;
            }

            romfsItemLabel.Text = $"{fileDescriptor.FileHashes.Count()} Items";
            if (fileDescriptor.HasUnusedFiles()) layeredfsItemLabel.Text = $"{fileDescriptor.UnusedHashes.Count()} Marked";
        }

        private void NavigateTo(string path = "")
        {
            activeViewer.NavigateTo(path);
            rootPathTextbox.Text = activeViewer.GetCwd();
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
    }
}
