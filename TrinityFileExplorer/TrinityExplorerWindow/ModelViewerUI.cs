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

            if (!EnsureModelViewerPathConfigured())
            {
                return;
            }

            try
            {
                // Prefer opening directly from the extracted RomFS folder when available.
                string romfsRoot = ExplorerSettings.GetRomFSPath();
                string rel = NormalizeRomfsRelativePath(ctx.RomfsRelativePath);
                if (!string.IsNullOrWhiteSpace(romfsRoot))
                {
                    string diskCandidate = Path.Combine(romfsRoot, rel.Replace('/', Path.DirectorySeparatorChar));
                    if (File.Exists(diskCandidate))
                    {
                        LaunchModelViewer(diskCandidate);
                        return;
                    }
                }

                if (!hasOodleDll)
                {
                    MessageBox.Show(
                        "LibOodle (oo2core_8_win64.dll) is missing.\n" +
                        "This is required to extract models from TRPFS when the file isn't present in your RomFS folder.",
                        "Missing Oodle",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                if (fileDescriptor == null || fileSystem == null)
                {
                    MessageBox.Show("TRPFD/TRPFS not loaded.", "Missing Data", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string extractedTrmdlPath = ExtractTrmdlWithDependenciesToTemp(ctx.FileHash, ctx.RomfsRelativePath);
                LaunchModelViewer(extractedTrmdlPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open in Model Viewer:\n{ex}", "Model Viewer", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
    }
}
