using GFTool.Core.Flatbuffers.TR.Scene.Components;
using GFTool.Core.Flatbuffers.TR.Scene;
using GFTool.Renderer;
using GFTool.Renderer.Core;
using GFTool.Renderer.Core.Graphics;
using GFTool.Renderer.Scene.GraphicsObjects;
using OpenTK.Mathematics;
using System.Drawing;
using System.Text;
using Trinity.Core.Utils;
using Point = System.Drawing.Point;
using GFTool.Renderer.Core;


namespace TrinitySceneView
{
    public partial class SceneViewerForm : Form
    {

        Point prevMousePos;

        private TRSceneTree sceneTree;
        private readonly SceneViewerConfig config;
        private string? assetRoot;
        private string? lastOpenedScenePath;
        private int? preferredSceneVariant;
        private bool suppressModelListEvents;

        public SceneViewerForm()
        {
            InitializeComponent();

            config = SceneViewerConfig.Load();
            assetRoot = string.IsNullOrWhiteSpace(config.AssetRoot) ? null : config.AssetRoot;
            MessageHandler.Instance.DebugLogsEnabled = config.DebugLogs;
            ApplyTheme();

            var setRoot = new ToolStripMenuItem("Set Asset Root...");
            setRoot.Click += setAssetRoot_Click;
            fileToolStripMenuItem.DropDownItems.Add(setRoot);

            var darkModeItem = new ToolStripMenuItem("Dark Mode")
            {
                CheckOnClick = true,
                Checked = config.DarkMode
            };
            darkModeItem.CheckedChanged += (_, _) =>
            {
                config.DarkMode = darkModeItem.Checked;
                config.Save();
                ApplyTheme();
            };
            viewToolStripMenuItem.DropDownItems.Add(darkModeItem);

            var debugLogsItem = new ToolStripMenuItem("Enable Debug Logs")
            {
                CheckOnClick = true,
                Checked = config.DebugLogs
            };
            debugLogsItem.CheckedChanged += (_, _) =>
            {
                config.DebugLogs = debugLogsItem.Checked;
                config.Save();
                MessageHandler.Instance.DebugLogsEnabled = config.DebugLogs;
                MessageHandler.Instance.AddMessage(MessageType.LOG, $"[Logs] Debug logs {(config.DebugLogs ? "enabled" : "disabled")}.");
            };
            viewToolStripMenuItem.DropDownItems.Add(debugLogsItem);

            var originItem = new ToolStripMenuItem("Spawn Models At Origin")
            {
                CheckOnClick = true,
                Checked = config.SpawnModelsAtOrigin
            };
            originItem.CheckedChanged += (_, _) =>
            {
                config.SpawnModelsAtOrigin = originItem.Checked;
                config.Save();
                MessageHandler.Instance.AddMessage(
                    MessageType.LOG,
                    $"[Scene] Spawn-at-origin {(config.SpawnModelsAtOrigin ? "enabled" : "disabled")}.");
            };
            viewToolStripMenuItem.DropDownItems.Add(originItem);

            var clipItem = new ToolStripMenuItem("Large Clip Planes")
            {
                CheckOnClick = true,
                Checked = config.LargeClipPlanes
            };
            clipItem.CheckedChanged += (_, _) =>
            {
                config.LargeClipPlanes = clipItem.Checked;
                config.Save();
                ApplySceneClipPlanes(Vector3.Zero, 0f);
                MessageHandler.Instance.AddMessage(
                    MessageType.LOG,
                    $"[Scene] Large clip planes {(config.LargeClipPlanes ? "enabled" : "disabled")}.");
            };
            viewToolStripMenuItem.DropDownItems.Add(clipItem);

            var rotItem = new ToolStripMenuItem("Rotate Models 180° (X)")
            {
                CheckOnClick = true,
                Checked = config.RotateModels180X
            };
            rotItem.CheckedChanged += (_, _) =>
            {
                config.RotateModels180X = rotItem.Checked;
                config.Save();
                MessageHandler.Instance.AddMessage(
                    MessageType.LOG,
                    $"[Scene] 180° rotation {(config.RotateModels180X ? "enabled" : "disabled")} (reload scene to apply).");
            };
            viewToolStripMenuItem.DropDownItems.Add(rotItem);
        }

        private void ApplyTheme()
        {
            ApplyTheme(this);
        }

        private void ApplyTheme(Control root)
        {
            var isDark = config?.DarkMode == true;
            var back = isDark ? Color.FromArgb(30, 30, 30) : SystemColors.Control;
            var fore = isDark ? Color.Gainsboro : SystemColors.ControlText;
            var panelBack = isDark ? Color.FromArgb(40, 40, 40) : SystemColors.Control;
            var listBack = isDark ? Color.FromArgb(24, 24, 24) : SystemColors.Window;

            ApplyThemeRecursive(root, back, panelBack, listBack, fore, isDark);
        }

        private void ApplyThemeRecursive(Control control, Color back, Color panelBack, Color listBack, Color fore, bool isDark)
        {
            if (control is Form || control is Panel || control is SplitContainer || control is GroupBox)
            {
                control.BackColor = panelBack;
                control.ForeColor = fore;
            }
            else if (control is ListView || control is TreeView || control is TextBox)
            {
                control.BackColor = listBack;
                control.ForeColor = fore;
            }
            else if (control is MenuStrip || control is ToolStrip)
            {
                control.BackColor = back;
                control.ForeColor = fore;
            }
            else if (control is Button || control is CheckBox)
            {
                control.BackColor = back;
                control.ForeColor = fore;
            }
            else
            {
                control.BackColor = back;
                control.ForeColor = fore;
            }

            foreach (Control child in control.Controls)
            {
                ApplyThemeRecursive(child, back, panelBack, listBack, fore, isDark);
            }
        }

        private void openTRSOT_Click(object sender, EventArgs e)
        {
            var ofd = new OpenFileDialog();
            ofd.Filter = "Trinity Scene (*.trscn;*.trsog;*.trsot)|*.trscn;*.trsog;*.trsot|All files (*.*)|*.*";
            if (ofd.ShowDialog() != DialogResult.OK) return;

            lastOpenedScenePath = ofd.FileName;
            preferredSceneVariant = TryDetectVariantFromPath(lastOpenedScenePath);
            sceneView.Nodes.Clear();
            sceneTree = new TRSceneTree();
            sceneTree.DeserializeScene(ofd.FileName);
            sceneView.Nodes.Add(sceneTree.TreeNode);

            TryLoadSceneModels(ofd.FileName);
        }

        private void setAssetRoot_Click(object? sender, EventArgs e)
        {
            using var fbd = new FolderBrowserDialog();
            fbd.Description = "Select the extracted game content root (must contain folders like 'field_graphic', etc.)";
            fbd.UseDescriptionForTitle = true;
            if (!string.IsNullOrWhiteSpace(assetRoot) && Directory.Exists(assetRoot))
            {
                fbd.SelectedPath = assetRoot;
            }

            if (fbd.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            assetRoot = fbd.SelectedPath;
            config.AssetRoot = assetRoot;
            config.Save();

            MessageHandler.Instance.AddMessage(MessageType.LOG, $"[Scene] Asset root set to: {assetRoot}");
            if (!string.IsNullOrWhiteSpace(lastOpenedScenePath) && File.Exists(lastOpenedScenePath))
            {
                TryLoadSceneModels(lastOpenedScenePath);
            }
        }

        private void expandToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TreeNode node = sceneView.SelectedNode;
            var pair = sceneTree.FindFirst(node);
            var meta = pair.Value;
            //Only expand nodes with external files
            if (meta.IsExternal)
                sceneTree.DeserializeScene(meta, pair.Key);
        }

        private void ClearProperties()
        {
            InfoBox.Text = string.Empty;
            propertyGrid.SelectedObject = null;
        }

        //Treeview context
        private void sceneView_MouseUp(object sender, MouseEventArgs e)
        {
            Point ClickPoint = new Point(e.X, e.Y);
            TreeNode ClickNode = sceneView.GetNodeAt(ClickPoint);
            sceneView.SelectedNode = ClickNode;
            if (ClickNode == null) return;

            if (e.Button == MouseButtons.Right)
            {
                Point ScreenPoint = sceneView.PointToScreen(ClickPoint);
                Point FormPoint = this.PointToClient(ScreenPoint);
                sceneContext.Show(this, FormPoint);
            }

            //Check for data to display
            var meta = sceneTree.GetNodeMeta(sceneView.SelectedNode);
            if (meta == null || meta?.Data == null)
            {
                // Allow lazy decode for certain chunk types that vary by version (e.g. PropertySheet).
                if (meta != null && meta.Value.Type == nameof(trinity_PropertySheet) && meta.Value.RawData != null)
                {
                    var decoded = TryDecodePropertySheet(meta.Value.RawData);
                    if (decoded != null)
                    {
                        InfoBox.Text = TRSceneProperties.GetProperties(nameof(trinity_PropertySheet), decoded);
                        propertyGrid.SelectedObject = new ScenePropertyGridProxy(meta.Value);
                        return;
                    }
                }

                ClearProperties();
                return;
            }

            InfoBox.Text = TRSceneProperties.GetProperties(meta?.Type, meta?.Data);
            propertyGrid.SelectedObject = new ScenePropertyGridProxy(meta.Value);
        }

        private static trinity_PropertySheet? TryDecodePropertySheet(byte[] data)
        {
            try
            {
                return FlatBufferConverter.DeserializeFrom<trinity_PropertySheet>(data);
            }
            catch (InvalidDataException)
            {
                // Fallback for chunks stored without a root uoffset: wrap with a 4-byte offset.
                if (data.Length < 8)
                {
                    return null;
                }

                try
                {
                    var wrapped = new byte[data.Length + 4];
                    BitConverter.GetBytes(4).CopyTo(wrapped, 0);
                    Buffer.BlockCopy(data, 0, wrapped, 4, data.Length);
                    return FlatBufferConverter.DeserializeFrom<trinity_PropertySheet>(wrapped);
                }
                catch
                {
                    return null;
                }
            }
            catch
            {
                return null;
            }
        }

        private sealed class ScenePropertyGridProxy
        {
            private readonly SceneMetaData meta;

            public ScenePropertyGridProxy(SceneMetaData meta)
            {
                this.meta = meta;
            }

            public bool IsExternal => meta.IsExternal;
            public string Type => meta.Type ?? string.Empty;
            public string FilePath => meta.FilePath ?? string.Empty;

            public string Summary
            {
                get
                {
                    try
                    {
                        if (meta.Data is trinity_SceneObject so)
                        {
                            return $"SceneObject '{so.Name}' tags={so.TagList?.Length ?? 0}";
                        }

                        if (meta.Data is trinity_ModelComponent mc)
                        {
                            return $"ModelComponent '{mc.FilePath}'";
                        }

                        if (meta.Data is trinity_PropertySheet ps)
                        {
                            // Avoid enumerating FlatSharp vectors here; some schema mismatches throw lazily.
                            return $"PropertySheet '{ps.name}' template='{ps.template}'";
                        }
                    }
                    catch
                    {
                        // Some FlatSharp vectors can throw when schema doesn't match; keep property grid stable.
                    }

                    return meta.Data?.GetType().Name ?? "(null)";
                }
            }
        }

    }
}
