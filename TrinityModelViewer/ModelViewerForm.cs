using GFTool.Renderer.Core;
using GFTool.Renderer.Scene.GraphicsObjects;
using OpenTK.Mathematics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using GfAnim = Trinity.Core.Flatbuffers.GF.Animation;
using Trinity.Core.Utils;
using Point = System.Drawing.Point;
using TrinityModelViewer.Export;

namespace TrinityModelViewer
{
    public partial class ModelViewerForm : Form
    {
        private enum NodeType
        {
            ModelRoot,
            MeshGroup,
            Mesh,
            MaterialsGroup,
            Material,
            ArmatureGroup,
            ArmatureBone
        }

        private sealed class NodeTag
        {
            public NodeType Type { get; set; }
            public Model Model { get; set; } = null!;
            public string? MeshName { get; set; }
            public string? MaterialName { get; set; }
            public int? BoneIndex { get; set; }
            public List<int>? SubmeshIndices { get; set; }
            public Dictionary<string, List<int>>? MaterialMap { get; set; }
        }

        private sealed class MeshEntry
        {
            public string Name { get; set; } = string.Empty;
            public List<int> SubmeshIndices { get; } = new List<int>();
            public Dictionary<string, List<int>> MaterialMap { get; } = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);
        }
        private Dictionary<TreeNode, Model> modelMap = new Dictionary<TreeNode, Model>();
        private ViewerSettings settings;
        private ToolStripMenuItem? lastModelToolStripMenuItem;
        private Image? texturePreviewImage;
        private Image? uvPreviewImage;
        private Model? currentMaterialsModel;
        private Material? currentMaterial;
        private readonly List<GFTool.Renderer.Scene.GraphicsObjects.Animation> animations = new List<GFTool.Renderer.Scene.GraphicsObjects.Animation>();
        private readonly HashSet<string> loadedAnimationPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly string[] startupFiles;
        private bool startupFilesLoaded;
        private ContextMenuStrip? textureGridContextMenu;

        public ModelViewerForm()
            : this(null)
        {
        }

        public ModelViewerForm(string[]? startupFiles)
        {
            InitializeComponent();
            settings = ViewerSettings.Load();
            this.startupFiles = startupFiles?.Where(s => !string.IsNullOrWhiteSpace(s)).ToArray() ?? Array.Empty<string>();
            MessageHandler.Instance.DebugLogsEnabled = settings.DebugLogs;
            ApplyRenderSettingsToMenu();
            ApplyTheme();
            AddSettingsMenu();
            AddLastModelMenu();
            renderCtrl.RendererReady += renderCtrl_RendererReady;
            AllowDrop = true;
            DragEnter += ModelViewerForm_DragEnter;
            DragDrop += ModelViewerForm_DragDrop;
            renderCtrl.AllowDrop = true;
            renderCtrl.DragEnter += ModelViewerForm_DragEnter;
            renderCtrl.DragDrop += ModelViewerForm_DragDrop;
            materialList.MultiSelect = false;
            materialList.SelectedIndexChanged += materialList_SelectedIndexChanged;
            materialTexturesGrid.SelectionChanged += materialTexturesGrid_SelectionChanged;
            materialTexturesGrid.MouseUp += materialTexturesGrid_MouseUp;
            SetupMaterialGrids();
            sceneTree.AfterSelect += sceneTree_AfterSelect;
            sceneTree.NodeMouseDoubleClick += sceneTree_NodeMouseDoubleClick;
            sceneTree.BeforeExpand += sceneTree_BeforeExpand;
            SetupAnimationsList();
        }

        private void SetupAnimationsList()
        {
            animationsList.View = View.Details;
            animationsList.FullRowSelect = true;
            animationsList.HideSelection = false;
            animationsList.MultiSelect = false;
            animationsList.Columns.Clear();
            animationsList.Columns.Add("Animation", -2);
            animationsList.DoubleClick += animationsList_DoubleClick;
        }

        private void messageHandler_Callback(object? sender, GFTool.Renderer.Core.Message e)
        {
            var item = new ListViewItem();
            item.Name = e.GetHashCode().ToString();
            item.Text = e.Content;
            item.ImageKey = e.Type switch
            {
                MessageType.LOG => "Log",
                MessageType.WARNING => "Warning",
                MessageType.ERROR => "Error"
            };

            //Only unique errors
            if (!messageListView.Items.ContainsKey(e.GetHashCode().ToString()))
            {
                messageListView.Items.Add(item);
                messageListView.EnsureVisible(messageListView.Items.Count - 1);
            }
        }

        #region GL_CONTEXT
        private void glCtxt_Paint(object sender, PaintEventArgs e)
        {
            var cam = renderCtrl.renderer.GetCameraTransform();
            statusLbl.Text = string.Format("Camera: Pos={0}, [Quat={1} Euler={2}]", cam.Position.ToString(), cam.Rotation.ToString(), cam.Rotation.ToEulerAngles().ToString());
        }

        private void glCtxt_Load(object sender, EventArgs e)
        {
            //Connect to message handler
            MessageHandler.Instance.MessageCallback += messageHandler_Callback;
            var messageIcons = new ImageList();
            messageIcons.Images.Add("Log", SystemIcons.Information.ToBitmap());
            messageIcons.Images.Add("Warning", SystemIcons.Warning.ToBitmap());
            messageIcons.Images.Add("Error", SystemIcons.Error.ToBitmap());
            messageListView.SmallImageList = messageIcons;
            messageListView.FullRowSelect = true;
            messageListView.AutoResizeColumn(0, ColumnHeaderAutoResizeStyle.HeaderSize);
        }

        private void renderCtrl_RendererReady(object? sender, EventArgs e)
        {
            ApplyRenderSettings();
            LoadStartupFilesIfAny();
        }

        private void LoadStartupFilesIfAny()
        {
            if (startupFilesLoaded)
            {
                return;
            }
            startupFilesLoaded = true;

            if (startupFiles.Length == 0)
            {
                return;
            }

            try
            {
                ClearAll();
                foreach (var path in startupFiles)
                {
                    if (path.EndsWith(".trmdl", StringComparison.OrdinalIgnoreCase) && File.Exists(path))
                    {
                        AddModelToScene(path);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Failed to load startup model:\n{ex.Message}", "Startup Load", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void AddSettingsMenu()
        {
            var settingsMenu = new ToolStripMenuItem("Settings");
            settingsMenu.Click += (s, e) => OpenSettings();
            int viewIndex = menuStrip1.Items.IndexOf(viewToolStripMenuItem);
            if (viewIndex >= 0 && viewIndex < menuStrip1.Items.Count - 1)
            {
                menuStrip1.Items.Insert(viewIndex + 1, settingsMenu);
            }
            else
            {
                menuStrip1.Items.Add(settingsMenu);
            }

            var helpMenu = new ToolStripMenuItem("Help");
            helpMenu.Click += (s, e) => OpenHelp();
            int settingsIndex = menuStrip1.Items.IndexOf(settingsMenu);
            if (settingsIndex >= 0 && settingsIndex < menuStrip1.Items.Count - 1)
            {
                menuStrip1.Items.Insert(settingsIndex + 1, helpMenu);
            }
            else
            {
                menuStrip1.Items.Add(helpMenu);
            }
        }

        private void AddLastModelMenu()
        {
            lastModelToolStripMenuItem = new ToolStripMenuItem("Last Model");
            lastModelToolStripMenuItem.Click += (s, e) => OpenLastModel();

            int insertIndex = fileToolStripMenuItem.DropDownItems.IndexOf(importToolStripMenuItem);
            if (insertIndex >= 0)
            {
                fileToolStripMenuItem.DropDownItems.Insert(insertIndex + 1, lastModelToolStripMenuItem);
            }
            else
            {
                fileToolStripMenuItem.DropDownItems.Add(lastModelToolStripMenuItem);
            }

            UpdateLastModelMenu();
        }

        private void OpenSettings()
        {
            using var dialog = new SettingsForm(
                settings.DarkMode,
                settings.LoadAllLods,
                settings.DebugLogs,
                settings.AutoLoadAnimations);
            ApplyTheme(dialog);
            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            settings.DarkMode = dialog.DarkModeEnabled;
            settings.LoadAllLods = dialog.LoadAllLodsEnabled;
            settings.DebugLogs = dialog.DebugLogsEnabled;
            settings.AutoLoadAnimations = dialog.AutoLoadAnimationsEnabled;
            settings.Save();
            MessageHandler.Instance.DebugLogsEnabled = settings.DebugLogs;
            ApplyTheme();
            ApplyRenderSettings();
        }

        private void UpdateLastModelMenu()
        {
            if (lastModelToolStripMenuItem == null)
            {
                return;
            }

            bool hasPath = !string.IsNullOrWhiteSpace(settings.LastModelPath) && File.Exists(settings.LastModelPath);
            lastModelToolStripMenuItem.Enabled = hasPath;
            lastModelToolStripMenuItem.ToolTipText = hasPath ? settings.LastModelPath : "No previous model found";
        }

        private void OpenLastModel()
        {
            if (string.IsNullOrWhiteSpace(settings.LastModelPath) || !File.Exists(settings.LastModelPath))
            {
                UpdateLastModelMenu();
                return;
            }

            ClearAll();
            AddModelToScene(settings.LastModelPath);
        }

        private void OpenHelp()
        {
            const string message =
                "Controls:\n" +
                "- Right Mouse Drag: Orbit camera\n" +
                "- Ctrl + Right Mouse Drag: Dolly (zoom)\n" +
                "- Left Mouse Drag: Pan camera\n" +
                "- WASD: Move camera\n" +
                "- Q/E: Move down/up\n" +
                "- Shift: Slow movement (0.2x)\n" +
                "- Ctrl: Fast movement (2x)";

            MessageBox.Show(this, message, "Controls", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void loadAnimationButton_Click(object sender, EventArgs e)
        {
            using var ofd = new OpenFileDialog();
            ofd.Filter = "Trinity Animation files (*.tranm;*.gfbanm)|*.tranm;*.gfbanm|All files (*.*)|*.*";
            ofd.Multiselect = true;
            if (ofd.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            try
            {
                int loaded = 0;
                foreach (var file in ofd.FileNames.Where(f => !string.IsNullOrWhiteSpace(f)))
                {
                    if (!loadedAnimationPaths.Add(file))
                    {
                        continue;
                    }

                    var animFile = FlatBufferConverter.DeserializeFrom<GfAnim.Animation>(file);
                    var anim = new GFTool.Renderer.Scene.GraphicsObjects.Animation(animFile, Path.GetFileNameWithoutExtension(file));
                    animations.Add(anim);
                    var item = new ListViewItem(anim.Name) { Tag = anim };
                    animationsList.Items.Add(item);
                    loaded++;

                    if (MessageHandler.Instance.DebugLogsEnabled)
                    {
                        MessageHandler.Instance.AddMessage(MessageType.LOG, $"[Anim] Loaded '{anim.Name}' file='{file}' frames={anim.FrameCount} fps={anim.FrameRate} tracks={anim.TrackCount}");
                    }
                }

                if (loaded > 0)
                {
                    animationsList.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Failed to load animation:\n{ex.Message}", "Animation Load", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void playAnimationButton_Click(object sender, EventArgs e)
        {
            var anim = GetSelectedAnimation();
            if (anim == null)
            {
                return;
            }

            renderCtrl.renderer.PlayAnimation(anim);
        }

        private void stopAnimationButton_Click(object sender, EventArgs e)
        {
            renderCtrl.renderer.StopAnimation();
        }

        private void exportAnimationButton_Click(object sender, EventArgs e)
        {
            var anim = GetSelectedAnimation();
            if (anim == null)
            {
                MessageBox.Show(this, "Select an animation to export.", "Export Animation", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var mdl = GetModelForAnimationExport();
            if (mdl?.Armature == null || mdl.Armature.Bones.Count == 0)
            {
                MessageBox.Show(this, "Load a model with a skeleton first (animation export needs an armature).", "Export Animation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using var sfd = new SaveFileDialog();
            sfd.Filter = "glTF 2.0 (*.gltf)|*.gltf";
            sfd.FileName = $"{anim.Name}.gltf";
            if (sfd.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            try
            {
                GltfExporter.ExportAnimation(mdl.Armature, anim, sfd.FileName);
                MessageBox.Show(this, $"Exported:\n{sfd.FileName}", "Export Animation", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Export failed:\n{ex.Message}", "Export Animation", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void exportModelWithAnimationsButton_Click(object sender, EventArgs e)
        {
            var mdl = GetModelForAnimationExport();
            if (mdl == null)
            {
                MessageBox.Show(this, "Load a model first.", "Export Model + Animations", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using var sfd = new SaveFileDialog();
            sfd.Filter = "glTF 2.0 (*.gltf)|*.gltf";
            sfd.FileName = $"{mdl.Name}_with_anims.gltf";
            if (sfd.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            try
            {
                var anims = animations.ToArray();
                if (anims.Length == 0)
                {
                    MessageBox.Show(this, "No animations are loaded; exporting the model only.", "Export Model + Animations", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                GltfExporter.ExportModel(mdl, sfd.FileName, anims);
                MessageBox.Show(this, $"Exported:\n{sfd.FileName}", "Export Model + Animations", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Export failed:\n{ex.Message}", "Export Model + Animations", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private Model? GetModelForAnimationExport()
        {
            // Prefer the selected model root (or any node under it).
            var selected = sceneTree.SelectedNode;
            if (selected != null)
            {
                var node = selected;
                while (node != null)
                {
                    if (modelMap.TryGetValue(node, out var m) && m != null)
                    {
                        return m;
                    }
                    node = node.Parent;
                }
            }

            // Fallback: first loaded model.
            foreach (var kvp in modelMap)
            {
                if (kvp.Value != null)
                {
                    return kvp.Value;
                }
            }

            return null;
        }

        private void animationsList_DoubleClick(object? sender, EventArgs e)
        {
            var anim = GetSelectedAnimation();
            if (anim == null)
            {
                return;
            }

            renderCtrl.renderer.PlayAnimation(anim);
        }

        private GFTool.Renderer.Scene.GraphicsObjects.Animation? GetSelectedAnimation()
        {
            if (animationsList.SelectedItems.Count == 0)
            {
                return null;
            }

            return animationsList.SelectedItems[0].Tag as Animation;
        }

        private void ApplyTheme()
        {
            ApplyTheme(this);
        }

        private void ApplyTheme(Control root)
        {
            if (root == null) return;

            var isDark = settings?.DarkMode == true;
            var back = isDark ? Color.FromArgb(30, 30, 30) : SystemColors.Control;
            var fore = isDark ? Color.Gainsboro : SystemColors.ControlText;
            var panelBack = isDark ? Color.FromArgb(40, 40, 40) : SystemColors.Control;
            var listBack = isDark ? Color.FromArgb(24, 24, 24) : SystemColors.Window;

            ApplyThemeRecursive(root, back, panelBack, listBack, fore, isDark);
        }

        private void ApplyThemeRecursive(Control control, Color back, Color panelBack, Color listBack, Color fore, bool isDark)
        {
            if (control is Form || control is Panel || control is SplitContainer || control is TabPage || control is GroupBox)
            {
                control.BackColor = panelBack;
                control.ForeColor = fore;
            }
            else if (control is PictureBox)
            {
                control.BackColor = listBack;
                control.ForeColor = fore;
            }
            else if (control is ListView || control is TreeView || control is TextBox)
            {
                control.BackColor = listBack;
                control.ForeColor = fore;
            }
            else if (control is DataGridView grid)
            {
                grid.BackgroundColor = listBack;
                grid.GridColor = isDark ? Color.FromArgb(50, 50, 50) : SystemColors.ControlDark;
                grid.DefaultCellStyle.BackColor = listBack;
                grid.DefaultCellStyle.ForeColor = fore;
                grid.DefaultCellStyle.SelectionBackColor = isDark ? Color.FromArgb(60, 90, 120) : SystemColors.Highlight;
                grid.DefaultCellStyle.SelectionForeColor = fore;
                grid.ColumnHeadersDefaultCellStyle.BackColor = panelBack;
                grid.ColumnHeadersDefaultCellStyle.ForeColor = fore;
                grid.EnableHeadersVisualStyles = false;
            }
            else if (control is MenuStrip || control is ToolStrip)
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
        #endregion


        private void ClearAll()
        {
            renderCtrl.renderer.ClearScene();
            renderCtrl.renderer.StopAnimation();
            messageListView.Items.Clear();
            materialList.Items.Clear();
            materialList.Columns.Clear();
            modelMap.Clear();
            sceneTree.Nodes.Clear();
            animations.Clear();
            animationsList.Items.Clear();
            loadedAnimationPaths.Clear();
            currentMaterialsModel = null;
            currentMaterial = null;
            ClearMaterialDetails();
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Trinity Model files (*.trmdl)|*.trmdl|All files (*.*)|*.*";
            if (ofd.ShowDialog() != DialogResult.OK) return;

            ClearAll();
            AddModelToScene(ofd.FileName);
        }

        private void importToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Trinity Model files (*.trmdl)|*.trmdl|All files (*.*)|*.*";
            ofd.Multiselect = true;
            if (ofd.ShowDialog() != DialogResult.OK) return;

            foreach (var file in ofd.FileNames.Where(f => !string.IsNullOrWhiteSpace(f)))
            {
                AddModelToScene(file);
            }
        }

        private void wireframeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            renderCtrl.renderer.SetWireframe(wireframeToolStripMenuItem.CheckState == CheckState.Checked);
            renderCtrl.Invalidate();
        }

        private void showSkeletonToolStripMenuItem_Click(object sender, EventArgs e)
        {
            settings.ShowSkeleton = showSkeletonToolStripMenuItem.Checked;
            settings.Save();
            ApplyRenderSettings();
        }

        private void ApplyRenderSettingsToMenu()
        {
            shadingLitToolStripMenuItem.Checked = settings.DisplayShading == ViewerSettings.ShadingMode.Lit;
            shadingToonToolStripMenuItem.Checked = settings.DisplayShading == ViewerSettings.ShadingMode.Toon;
            shadingLegacyToolStripMenuItem.Checked = settings.DisplayShading == ViewerSettings.ShadingMode.Legacy;
            showSkeletonToolStripMenuItem.Checked = settings.ShowSkeleton;
        }

        private void ApplyRenderSettings()
        {
            if (renderCtrl?.renderer == null) return;
            renderCtrl.renderer.SetNormalMapsEnabled(settings.EnableNormalMaps);
            renderCtrl.renderer.SetAOEnabled(settings.EnableAO);
            renderCtrl.renderer.SetVertexColorsEnabled(settings.EnableVertexColors);
            renderCtrl.renderer.SetFlipNormalY(settings.FlipNormalY);
            renderCtrl.renderer.SetReconstructNormalZ(settings.ReconstructNormalZ);
            // TrinityModelViewer skinning defaults.
            // TRSKL inverse binds are enabled.
            // Blend indices are mapped via joint info.
            // Other debug options stay disabled.
            RenderOptions.UseTrsklInverseBind = true;
            RenderOptions.MapBlendIndicesViaJointInfo = true;
            RenderOptions.SwapBlendOrder = false;
            RenderOptions.MapBlendIndicesViaBoneMeta = false;
            RenderOptions.TransposeSkinMatrices = false;
            RenderOptions.MapBlendIndicesViaSkinningPalette = false;
            RenderOptions.UseSkinningPaletteMatrices = false;
            RenderOptions.UseJointInfoMatrices = false;
            var display = settings.DisplayShading == ViewerSettings.ShadingMode.Toon
                ? GFTool.Renderer.Core.Graphics.GBuffer.DisplayType.DISPLAY_TOON
                : settings.DisplayShading == ViewerSettings.ShadingMode.Legacy
                    ? GFTool.Renderer.Core.Graphics.GBuffer.DisplayType.DISPLAY_LEGACY
                    : GFTool.Renderer.Core.Graphics.GBuffer.DisplayType.DISPLAY_ALL;
            renderCtrl.renderer.SetGBufferDisplayMode(display);
            renderCtrl.renderer.SetSkeletonVisible(settings.ShowSkeleton);
            renderCtrl.Invalidate();
        }

        private void shadingLitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            settings.DisplayShading = ViewerSettings.ShadingMode.Lit;
            settings.Save();
            ApplyRenderSettingsToMenu();
            ApplyRenderSettings();
        }

        private void shadingToonToolStripMenuItem_Click(object sender, EventArgs e)
        {
            settings.DisplayShading = ViewerSettings.ShadingMode.Toon;
            settings.Save();
            ApplyRenderSettingsToMenu();
            ApplyRenderSettings();
        }

        private void shadingLegacyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            settings.DisplayShading = ViewerSettings.ShadingMode.Legacy;
            settings.Save();
            ApplyRenderSettingsToMenu();
            ApplyRenderSettings();
        }

        private void glCtxt_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.W: KeyboardControls.Forward = true; break;
                case Keys.A: KeyboardControls.Left = true; break;
                case Keys.S: KeyboardControls.Backward = true; break;
                case Keys.D: KeyboardControls.Right = true; break;
                case Keys.Q: KeyboardControls.Up = true; break;
                case Keys.E: KeyboardControls.Down = true; break;
            }
        }

        private void glCtxt_KeyUp(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.W: KeyboardControls.Forward = false; break;
                case Keys.A: KeyboardControls.Left = false; break;
                case Keys.S: KeyboardControls.Backward = false; break;
                case Keys.D: KeyboardControls.Right = false; break;
                case Keys.Q: KeyboardControls.Up = false; break;
                case Keys.E: KeyboardControls.Down = false; break;
            }
        }

        //Treeview handler
        private void sceneTree_MouseUp(object sender, MouseEventArgs e)
        {
            Point ClickPoint = new Point(e.X, e.Y);
            TreeNode ClickNode = sceneTree.GetNodeAt(ClickPoint);
            sceneTree.SelectedNode = ClickNode;
            if (ClickNode == null) return;

            if (e.Button == MouseButtons.Right)
            {
                ConfigureSceneContextMenu(ClickNode);
                Point ScreenPoint = sceneTree.PointToScreen(ClickPoint);
                Point FormPoint = this.PointToClient(ScreenPoint);
                sceneTreeCtxtMenu.Show(this, FormPoint);
            }
        }

        private void ConfigureSceneContextMenu(TreeNode node)
        {
            bool isModelRoot = (node.Tag as NodeTag)?.Type == NodeType.ModelRoot;
            exportToolStripMenuItem.Visible = isModelRoot;
            deleteToolStripMenuItem.Visible = isModelRoot;
        }

        //Context menu delete
        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var selected = sceneTree.SelectedNode;
            if (selected != null)
            {
                modelMap.TryGetValue(selected, out var mdl);
                if (mdl == null) return;

                renderCtrl.renderer.RemoveSceneModel(mdl);
                sceneTree.Nodes.Remove(selected);
                modelMap.Remove(selected);
                materialList.Items.Clear();
                ClearMaterialDetails();
            }
        }

        private void exportToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var selected = sceneTree.SelectedNode;
            if (selected == null) return;
            if ((selected.Tag as NodeTag)?.Type != NodeType.ModelRoot) return;
            if (!modelMap.TryGetValue(selected, out var mdl) || mdl == null) return;

            using var sfd = new SaveFileDialog();
            sfd.Filter = "glTF 2.0 (*.gltf)|*.gltf";
            sfd.FileName = $"{mdl.Name}.gltf";
            if (sfd.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            try
            {
                GltfExporter.ExportModel(mdl, sfd.FileName);
                MessageBox.Show(this, $"Exported:\n{sfd.FileName}", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Export failed:\n{ex.Message}", "Export", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void PopulateSubmeshes(TreeNode node, Model mdl)
        {
            node.Nodes.Clear();
            var meshesNode = new TreeNode("Meshes")
            {
                Tag = new NodeTag
                {
                    Type = NodeType.MeshGroup,
                    Model = mdl
                }
            };
            meshesNode.Nodes.Add(new TreeNode("..."));
            node.Nodes.Add(meshesNode);

            var armatureNode = new TreeNode("Armature")
            {
                Tag = new NodeTag
                {
                    Type = NodeType.ArmatureGroup,
                    Model = mdl
                }
            };
            armatureNode.Nodes.Add(new TreeNode("..."));
            node.Nodes.Add(armatureNode);
            node.Expand();
        }

        private void PopulateMaterials(Model mdl)
        {
            currentMaterialsModel = mdl;
            materialList.BeginUpdate();
            materialList.Items.Clear();
            materialList.Columns.Clear();
            materialList.View = View.Details;
            materialList.Columns.Add("Material", 160);

            foreach (var mat in mdl.GetMaterials())
            {
                var item = new ListViewItem(mat.Name);
                item.Tag = mat;
                materialList.Items.Add(item);
            }

            materialList.AutoResizeColumn(0, ColumnHeaderAutoResizeStyle.HeaderSize);
            materialList.EndUpdate();

            if (materialList.Items.Count > 0)
            {
                materialList.Items[0].Selected = true;
            }
        }


        private void SetupMaterialGrids()
        {
            materialTexturesGrid.Columns.Clear();
            materialTexturesGrid.Columns.Add("TextureName", "Name");
            materialTexturesGrid.Columns.Add("TextureFile", "File");
            materialTexturesGrid.Columns.Add("TextureSlot", "Slot");
            materialTexturesGrid.Columns.Add("TextureSampler", "Sampler");
            EnsureTextureGridContextMenu();

            materialParamsGrid.Columns.Clear();
            materialParamsGrid.Columns.Add("ParamName", "Name");
            materialParamsGrid.Columns.Add("ParamType", "Type");
            materialParamsGrid.Columns.Add("ParamValue", "Value");

            materialUvGrid.Columns.Clear();
            materialUvGrid.Columns.Add("UvName", "Name");
            materialUvGrid.Columns.Add("UvValue", "Value");

            materialSamplersGrid.Columns.Clear();
            materialSamplersGrid.Columns.Add("SamplerIndex", "Index");
            materialSamplersGrid.Columns.Add("SamplerRepeatU", "Repeat U");
            materialSamplersGrid.Columns.Add("SamplerRepeatV", "Repeat V");
            materialSamplersGrid.Columns.Add("SamplerRepeatW", "Repeat W");
            materialSamplersGrid.Columns.Add("SamplerBorderColor", "Border Color");
            materialSamplersGrid.Columns.Add("SamplerState0", "State0");
            materialSamplersGrid.Columns.Add("SamplerState1", "State1");
            materialSamplersGrid.Columns.Add("SamplerState2", "State2");
            materialSamplersGrid.Columns.Add("SamplerState3", "State3");
            materialSamplersGrid.Columns.Add("SamplerState4", "State4");
            materialSamplersGrid.Columns.Add("SamplerState5", "State5");
            materialSamplersGrid.Columns.Add("SamplerState6", "State6");
            materialSamplersGrid.Columns.Add("SamplerState7", "State7");
            materialSamplersGrid.Columns.Add("SamplerState8", "State8");
        }

        private void EnsureTextureGridContextMenu()
        {
            if (textureGridContextMenu != null)
            {
                return;
            }

            textureGridContextMenu = new ContextMenuStrip();
            var export = new ToolStripMenuItem("Export...");
            export.Click += (_, _) => ExportSelectedTexture();
            textureGridContextMenu.Items.Add(export);
        }

        private void materialTexturesGrid_MouseUp(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
            {
                return;
            }

            var hit = materialTexturesGrid.HitTest(e.X, e.Y);
            if (hit.Type != DataGridViewHitTestType.Cell || hit.RowIndex < 0)
            {
                return;
            }

            materialTexturesGrid.ClearSelection();
            materialTexturesGrid.Rows[hit.RowIndex].Selected = true;

            if (materialTexturesGrid.Rows[hit.RowIndex].Tag is not Texture)
            {
                return;
            }

            EnsureTextureGridContextMenu();
            textureGridContextMenu?.Show(materialTexturesGrid, new Point(e.X, e.Y));
        }

        private void ExportSelectedTexture()
        {
            var texture = GetSelectedTexture();
            if (texture == null)
            {
                MessageBox.Show(this, "No texture selected.", "Export Texture", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string baseName;
            try
            {
                baseName = Path.GetFileNameWithoutExtension(texture.SourceFile);
                if (string.IsNullOrWhiteSpace(baseName))
                {
                    baseName = texture.Name;
                }
            }
            catch
            {
                baseName = texture.Name;
            }

            using var sfd = new SaveFileDialog();
            sfd.Title = "Export Texture";
            sfd.Filter = "PNG image (*.png)|*.png|BNTX texture (*.bntx)|*.bntx";
            sfd.FileName = baseName + ".png";
            if (sfd.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            string outPath = sfd.FileName;
            string ext = Path.GetExtension(outPath).ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(ext))
            {
                ext = sfd.FilterIndex == 2 ? ".bntx" : ".png";
                outPath += ext;
            }

            try
            {
                if (ext == ".bntx")
                {
                    if (!texture.TryGetResolvedSourcePath(out var sourcePath) || !File.Exists(sourcePath))
                    {
                        MessageBox.Show(this, "Source BNTX file was not found on disk.", "Export Texture", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    File.Copy(sourcePath, outPath, overwrite: true);
                }
                else
                {
                    using var bmp = texture.LoadPreviewBitmap();
                    if (bmp == null)
                    {
                        MessageBox.Show(this, "Texture could not be decoded.", "Export Texture", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    bmp.Save(outPath, System.Drawing.Imaging.ImageFormat.Png);
                }

                MessageBox.Show(this, $"Exported:\n{outPath}", "Export Texture", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Export failed:\n{ex.Message}", "Export Texture", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void materialList_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (materialList.SelectedItems.Count == 0)
            {
                currentMaterial = null;
                ClearMaterialDetails();
                return;
            }

            if (materialList.SelectedItems[0].Tag is not Material mat)
            {
                currentMaterial = null;
                ClearMaterialDetails();
                return;
            }

            currentMaterial = mat;
            PopulateMaterialDetails(mat);
        }

        private void ClearMaterialDetails()
        {
            materialTexturesGrid.Rows.Clear();
            materialParamsGrid.Rows.Clear();
            materialUvGrid.Rows.Clear();
            materialSamplersGrid.Rows.Clear();
            SetTexturePreview(null);
            SetUvPreview(null);
        }

        private void PopulateMaterialDetails(Material mat)
        {
            ClearMaterialDetails();

            materialParamsGrid.Rows.Add("Shader", "Name", mat.ShaderName);

            foreach (var param in mat.ShaderParameters)
            {
                materialParamsGrid.Rows.Add(param.Name, "Option", param.Value);
            }

            foreach (var param in mat.FloatParameters)
            {
                materialParamsGrid.Rows.Add(param.Name, "Float", param.Value.ToString("0.####"));
            }

            foreach (var param in mat.Vec2Parameters)
            {
                materialParamsGrid.Rows.Add(param.Name, "Vec2", $"{param.Value.X:0.####}, {param.Value.Y:0.####}");
            }

            foreach (var param in mat.Vec3Parameters)
            {
                materialParamsGrid.Rows.Add(param.Name, "Vec3", $"{param.Value.X:0.####}, {param.Value.Y:0.####}, {param.Value.Z:0.####}");
            }

            foreach (var param in mat.Vec4Parameters)
            {
                materialParamsGrid.Rows.Add(param.Name, "Vec4", $"{param.Value.X:0.####}, {param.Value.Y:0.####}, {param.Value.Z:0.####}, {param.Value.W:0.####}");
            }

            for (int i = 0; i < mat.Textures.Count; i++)
            {
                var tex = mat.Textures[i];
                string samplerIndex = i < mat.Samplers.Count ? i.ToString() : "-";
                var rowIndex = materialTexturesGrid.Rows.Add(tex.Name, tex.SourceFile, tex.Slot.ToString(), samplerIndex);
                materialTexturesGrid.Rows[rowIndex].Tag = tex;
            }

            foreach (var param in mat.ShaderParameters)
            {
                if (IsUvParamName(param.Name))
                {
                    materialUvGrid.Rows.Add(param.Name, param.Value);
                }
            }

            foreach (var param in mat.Vec2Parameters)
            {
                if (IsUvParamName(param.Name))
                {
                    materialUvGrid.Rows.Add(param.Name, $"{param.Value.X:0.####}, {param.Value.Y:0.####}");
                }
            }

            foreach (var param in mat.Vec3Parameters)
            {
                if (IsUvParamName(param.Name))
                {
                    materialUvGrid.Rows.Add(param.Name, $"{param.Value.X:0.####}, {param.Value.Y:0.####}, {param.Value.Z:0.####}");
                }
            }

            foreach (var param in mat.Vec4Parameters)
            {
                if (IsUvParamName(param.Name))
                {
                    materialUvGrid.Rows.Add(param.Name, $"{param.Value.X:0.####}, {param.Value.Y:0.####}, {param.Value.Z:0.####}, {param.Value.W:0.####}");
                }
            }

            for (int i = 0; i < mat.Samplers.Count; i++)
            {
                var sampler = mat.Samplers[i];
                var border = sampler.BorderColor;
                var borderText = border == null
                    ? "0, 0, 0, 0"
                    : $"{border.R:0.###}, {border.G:0.###}, {border.B:0.###}, {border.A:0.###}";
                materialSamplersGrid.Rows.Add(
                    i.ToString(),
                    sampler.RepeatU.ToString(),
                    sampler.RepeatV.ToString(),
                    sampler.RepeatW.ToString(),
                    borderText,
                    $"0x{sampler.State0:X8}",
                    $"0x{sampler.State1:X8}",
                    $"0x{sampler.State2:X8}",
                    $"0x{sampler.State3:X8}",
                    $"0x{sampler.State4:X8}",
                    $"0x{sampler.State5:X8}",
                    $"0x{sampler.State6:X8}",
                    $"0x{sampler.State7:X8}",
                    $"0x{sampler.State8:X8}"
                );
            }

            if (materialTexturesGrid.Rows.Count > 0)
            {
                materialTexturesGrid.ClearSelection();
                materialTexturesGrid.Rows[0].Selected = true;
            }

            UpdateUvPreview();
        }

        private void materialTexturesGrid_SelectionChanged(object? sender, EventArgs e)
        {
            if (materialTexturesGrid.SelectedRows.Count == 0)
            {
                SetTexturePreview(null);
                UpdateUvPreview();
                return;
            }

            if (materialTexturesGrid.SelectedRows[0].Tag is Texture texture)
            {
                SetTexturePreview(texture.LoadPreviewBitmap());
                UpdateUvPreview();
                return;
            }

            SetTexturePreview(null);
            UpdateUvPreview();
        }

        private void SetTexturePreview(Image? image)
        {
            if (texturePreviewImage != null)
            {
                texturePreviewImage.Dispose();
            }

            texturePreviewImage = image;
            materialTexturePreview.Image = image;
        }

        private void UpdateUvPreview()
        {
            if (currentMaterialsModel == null || currentMaterial == null)
            {
                SetUvPreview(null);
                return;
            }

            var texture = GetSelectedTexture();
            var uvSets = currentMaterialsModel.GetUvSetsForMaterial(currentMaterial.Name);
            if (uvSets.Count == 0)
            {
                SetUvPreview(texture?.LoadPreviewBitmap());
                return;
            }

            var uvScaleOffset = GetUvScaleOffset(currentMaterial);
            var preview = BuildUvPreview(texture, uvSets, uvScaleOffset);
            SetUvPreview(preview);
        }

        private Texture? GetSelectedTexture()
        {
            if (materialTexturesGrid.SelectedRows.Count > 0 &&
                materialTexturesGrid.SelectedRows[0].Tag is Texture selected)
            {
                return selected;
            }

            if (materialTexturesGrid.Rows.Count > 0 && materialTexturesGrid.Rows[0].Tag is Texture first)
            {
                return first;
            }

            return null;
        }

        private static Vector4 GetUvScaleOffset(Material material)
        {
            foreach (var param in material.Vec4Parameters)
            {
                if (string.Equals(param.Name, "UVScaleOffset", StringComparison.OrdinalIgnoreCase))
                {
                    return new Vector4(param.Value.X, param.Value.Y, param.Value.Z, param.Value.W);
                }
            }

            return new Vector4(1f, 1f, 0f, 0f);
        }

        private Image BuildUvPreview(Texture? texture, IReadOnlyList<Model.UvSet> uvSets, Vector4 uvScaleOffset)
        {
            using var sourceBitmap = texture?.LoadPreviewBitmap();
            var baseBitmap = new Bitmap(
                sourceBitmap?.Width ?? 256,
                sourceBitmap?.Height ?? 256,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            using var g = Graphics.FromImage(baseBitmap);
            g.Clear(Color.FromArgb(30, 30, 30));
            if (sourceBitmap != null)
            {
                g.DrawImage(sourceBitmap, 0, 0, baseBitmap.Width, baseBitmap.Height);
            }
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using var pen = new Pen(Color.FromArgb(255, 255, 220, 40), 2.0f);

            var width = baseBitmap.Width;
            var height = baseBitmap.Height;

            foreach (var set in uvSets)
            {
                var uvs = set.Uvs;
                var indices = set.Indices;

                if (indices.Length < 3)
                {
                    continue;
                }

                for (int i = 0; i + 2 < indices.Length; i += 3)
                {
                    var i0 = (int)indices[i];
                    var i1 = (int)indices[i + 1];
                    var i2 = (int)indices[i + 2];
                    if (i0 >= uvs.Length || i1 >= uvs.Length || i2 >= uvs.Length)
                    {
                        continue;
                    }

                    var p0 = UvToPoint(uvs[i0], uvScaleOffset, width, height);
                    var p1 = UvToPoint(uvs[i1], uvScaleOffset, width, height);
                    var p2 = UvToPoint(uvs[i2], uvScaleOffset, width, height);

                    g.DrawLine(pen, p0, p1);
                    g.DrawLine(pen, p1, p2);
                    g.DrawLine(pen, p2, p0);
                }
            }

            return baseBitmap;
        }

        private static PointF UvToPoint(Vector2 uv, Vector4 uvScaleOffset, int width, int height)
        {
            var t = TransformUv(uv, uvScaleOffset);
            float u = t.X;
            float v = t.Y;

            if (float.IsNaN(u) || float.IsInfinity(u)) u = 0.5f;
            if (float.IsNaN(v) || float.IsInfinity(v)) v = 0.5f;

            u = Math.Clamp(u, 0f, 1f);
            v = Math.Clamp(v, 0f, 1f);

            float x = u * (width - 1);
            float y = (1f - v) * (height - 1);
            return new PointF(x, y);
        }

        private static Vector2 TransformUv(Vector2 uv, Vector4 uvScaleOffset)
        {
            var scaleX = Math.Abs(uvScaleOffset.X) < 0.0001f ? 1f : uvScaleOffset.X;
            var scaleY = Math.Abs(uvScaleOffset.Y) < 0.0001f ? 1f : uvScaleOffset.Y;
            float u = uv.X * scaleX + uvScaleOffset.Z;
            float v = uv.Y * scaleY + uvScaleOffset.W;
            u = u - (float)Math.Floor(u);
            v = v - (float)Math.Floor(v);
            return new Vector2(u, v);
        }

        private void SetUvPreview(Image? image)
        {
            if (uvPreviewImage != null)
            {
                uvPreviewImage.Dispose();
            }

            uvPreviewImage = image;
            materialUvPreview.Image = image;
        }

        private static bool IsUvParamName(string name)
        {
            return name.Contains("UV", StringComparison.OrdinalIgnoreCase);
        }

        private void AddModelToScene(string filePath)
        {
            var mdl = renderCtrl.renderer.AddSceneModel(filePath, settings.LoadAllLods);
            var node = new TreeNode(mdl.Name)
            {
                Tag = new NodeTag
                {
                    Type = NodeType.ModelRoot,
                    Model = mdl
                }
            };
            modelMap.Add(node, mdl);
            sceneTree.Nodes.Add(node);
            PopulateSubmeshes(node, mdl);
            PopulateMaterials(mdl);
            TryAutoLoadAnimations(filePath);

            // Default to "solo" display for the most recently added model.
            ShowOnlyModel(mdl);
            sceneTree.SelectedNode = node;
            node.EnsureVisible();

            settings.LastModelPath = filePath;
            settings.Save();
            UpdateLastModelMenu();
        }

        private void TryAutoLoadAnimations(string trmdlPath)
        {
            if (!settings.AutoLoadAnimations)
            {
                return;
            }

            string? animDir = GuessAnimationDirectory(trmdlPath);
            if (string.IsNullOrWhiteSpace(animDir) || !Directory.Exists(animDir))
            {
                if (MessageHandler.Instance.DebugLogsEnabled)
                {
                    MessageHandler.Instance.AddMessage(MessageType.LOG, $"[Anim] AutoLoad: no animation directory found for '{trmdlPath}'");
                }
                return;
            }

            LoadAnimationsFromDirectory(animDir);
        }

        private void LoadAnimationsFromDirectory(string animDir)
        {
            const int maxToLoad = 500;

            IEnumerable<string> tranm = Enumerable.Empty<string>();
            IEnumerable<string> gfbanm = Enumerable.Empty<string>();

            try
            {
                tranm = Directory.EnumerateFiles(animDir, "*.tranm", SearchOption.TopDirectoryOnly);
                gfbanm = Directory.EnumerateFiles(animDir, "*.gfbanm", SearchOption.TopDirectoryOnly);
            }
            catch (Exception ex)
            {
                if (MessageHandler.Instance.DebugLogsEnabled)
                {
                    MessageHandler.Instance.AddMessage(MessageType.WARNING, $"[Anim] AutoLoad: failed to enumerate '{animDir}': {ex.Message}");
                }
                return;
            }

            var files = tranm.Concat(gfbanm)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
                .Take(maxToLoad)
                .ToList();

            int loaded = 0;
            foreach (var file in files)
            {
                if (!loadedAnimationPaths.Add(file))
                {
                    continue;
                }

                try
                {
                    var animFile = FlatBufferConverter.DeserializeFrom<GfAnim.Animation>(file);
                    var anim = new GFTool.Renderer.Scene.GraphicsObjects.Animation(animFile, Path.GetFileNameWithoutExtension(file));
                    animations.Add(anim);
                    var item = new ListViewItem(anim.Name) { Tag = anim };
                    animationsList.Items.Add(item);
                    loaded++;
                }
                catch (Exception ex)
                {
                    if (MessageHandler.Instance.DebugLogsEnabled)
                    {
                        MessageHandler.Instance.AddMessage(MessageType.WARNING, $"[Anim] AutoLoad: failed '{file}': {ex.Message}");
                    }
                }
            }

            animationsList.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
            if (MessageHandler.Instance.DebugLogsEnabled)
            {
                MessageHandler.Instance.AddMessage(MessageType.LOG, $"[Anim] AutoLoad: loaded {loaded} animations from '{animDir}'");
            }
        }

        private static string? GuessAnimationDirectory(string trmdlPath)
        {
            if (string.IsNullOrWhiteSpace(trmdlPath))
            {
                return null;
            }

            string full = Path.GetFullPath(trmdlPath);
            string? dir = Path.GetDirectoryName(full);
            if (string.IsNullOrWhiteSpace(dir))
            {
                return null;
            }

            char[] seps = new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };
            var parts = dir.Split(seps, StringSplitOptions.RemoveEmptyEntries).ToList();
            if (parts.Count == 0)
            {
                return null;
            }

            int modelIndex = parts.FindIndex(p => p.StartsWith("model_", StringComparison.OrdinalIgnoreCase));
            if (modelIndex < 0)
            {
                return null;
            }

            string modelFolder = parts[modelIndex];
            string suffix = modelFolder.Length > "model_".Length ? modelFolder.Substring("model_".Length) : string.Empty;
            string motionFolder = string.IsNullOrEmpty(suffix) ? "motion" : $"motion_{suffix}";
            parts[modelIndex] = motionFolder;

            return string.Join(Path.DirectorySeparatorChar, parts);
        }

        private void sceneTree_AfterSelect(object? sender, TreeViewEventArgs e)
        {
            ClearSubmeshSelections();
            renderCtrl.Invalidate();
            if (e.Node == null)
            {
                ShowOnlyModel(null);
                return;
            }

            if (e.Node.Tag is not NodeTag tag)
            {
                ShowOnlyModel(null);
                return;
            }

            ShowOnlyModel(tag.Model);

            if (tag.Type == NodeType.Mesh && tag.SubmeshIndices != null && tag.SubmeshIndices.Count > 0)
            {
                tag.Model.SetSelectedSubmesh(tag.SubmeshIndices[0]);
                renderCtrl.Invalidate();
                return;
            }

            if (tag.Type == NodeType.Material && tag.SubmeshIndices != null && tag.SubmeshIndices.Count > 0)
            {
                tag.Model.SetSelectedSubmesh(tag.SubmeshIndices[0]);
                renderCtrl.Invalidate();
                if (!string.IsNullOrWhiteSpace(tag.MaterialName))
                {
                    SelectMaterialByName(tag.MaterialName);
                }
            }
        }

        private void ShowOnlyModel(Model? model)
        {
            foreach (var mdl in modelMap.Values)
            {
                mdl.SetVisible(model == null || ReferenceEquals(mdl, model));
            }

            renderCtrl.Invalidate();
        }

        private void sceneTree_NodeMouseDoubleClick(object? sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Node == null)
            {
                return;
            }

            if (e.Node.Tag is not NodeTag tag)
            {
                return;
            }

            if (tag.Type == NodeType.Material && !string.IsNullOrWhiteSpace(tag.MaterialName))
            {
                SelectMaterialByName(tag.MaterialName);
            }
        }

        private void sceneTree_BeforeExpand(object? sender, TreeViewCancelEventArgs e)
        {
            if (e.Node == null)
            {
                return;
            }

            if (e.Node.Tag is not NodeTag tag)
            {
                return;
            }

            switch (tag.Type)
            {
                case NodeType.MeshGroup:
                    EnsureMeshNodes(e.Node, tag.Model);
                    break;
                case NodeType.Mesh:
                    EnsureMaterialsGroupNode(e.Node, tag);
                    break;
                case NodeType.MaterialsGroup:
                    EnsureMaterialNodes(e.Node, tag);
                    break;
                case NodeType.ArmatureGroup:
                    EnsureArmatureNodes(e.Node, tag.Model);
                    break;
                case NodeType.ArmatureBone:
                    EnsureArmatureChildNodes(e.Node, tag);
                    break;
            }
        }

        private void EnsureMeshNodes(TreeNode meshesNode, Model mdl)
        {
            ClearPlaceholderNode(meshesNode);
            if (meshesNode.Nodes.Count > 0)
            {
                return;
            }

            foreach (var entry in BuildMeshEntries(mdl))
            {
                var meshNode = new TreeNode(entry.Name)
                {
                    Tag = new NodeTag
                    {
                        Type = NodeType.Mesh,
                        Model = mdl,
                        MeshName = entry.Name,
                        SubmeshIndices = entry.SubmeshIndices,
                        MaterialMap = entry.MaterialMap
                    }
                };
                meshNode.Nodes.Add(new TreeNode("..."));
                meshesNode.Nodes.Add(meshNode);
            }
        }

        private void EnsureMaterialsGroupNode(TreeNode meshNode, NodeTag meshTag)
        {
            ClearPlaceholderNode(meshNode);
            foreach (TreeNode child in meshNode.Nodes)
            {
                if (child.Tag is NodeTag tag && tag.Type == NodeType.MaterialsGroup)
                {
                    return;
                }
            }

            var materialsNode = new TreeNode("Materials")
            {
                Tag = new NodeTag
                {
                    Type = NodeType.MaterialsGroup,
                    Model = meshTag.Model,
                    MeshName = meshTag.MeshName,
                    SubmeshIndices = meshTag.SubmeshIndices,
                    MaterialMap = meshTag.MaterialMap
                }
            };
            materialsNode.Nodes.Add(new TreeNode("..."));
            meshNode.Nodes.Add(materialsNode);
        }

        private void EnsureMaterialNodes(TreeNode materialsNode, NodeTag materialsTag)
        {
            ClearPlaceholderNode(materialsNode);
            if (materialsTag.MaterialMap == null)
            {
                return;
            }

            if (materialsNode.Nodes.Count > 0)
            {
                return;
            }

            foreach (var kvp in materialsTag.MaterialMap)
            {
                var materialNode = new TreeNode(kvp.Key)
                {
                    Tag = new NodeTag
                    {
                        Type = NodeType.Material,
                        Model = materialsTag.Model,
                        MaterialName = kvp.Key,
                        SubmeshIndices = kvp.Value
                    }
                };
                materialsNode.Nodes.Add(materialNode);
            }
        }

        private static void ClearPlaceholderNode(TreeNode node)
        {
            for (int i = node.Nodes.Count - 1; i >= 0; i--)
            {
                if (node.Nodes[i].Text == "...")
                {
                    node.Nodes.RemoveAt(i);
                }
            }
        }

        private static List<MeshEntry> BuildMeshEntries(Model mdl)
        {
            var entries = new Dictionary<string, MeshEntry>(StringComparer.OrdinalIgnoreCase);
            var submeshNames = mdl.GetSubmeshNames();
            var submeshMaterials = mdl.GetSubmeshMaterials();
            var count = Math.Min(submeshNames.Count, submeshMaterials.Count);

            for (int i = 0; i < count; i++)
            {
                var displayName = submeshNames[i];
                var colonIndex = displayName.IndexOf(':');
                if (colonIndex > -1)
                {
                    displayName = displayName.Substring(0, colonIndex);
                }

                if (!entries.TryGetValue(displayName, out var entry))
                {
                    entry = new MeshEntry { Name = displayName };
                    entries[displayName] = entry;
                }

                entry.SubmeshIndices.Add(i);
                var materialName = submeshMaterials[i] ?? string.Empty;
                if (!entry.MaterialMap.TryGetValue(materialName, out var indices))
                {
                    indices = new List<int>();
                    entry.MaterialMap[materialName] = indices;
                }
                indices.Add(i);
            }

            return entries.Values.ToList();
        }

        private void EnsureArmatureNodes(TreeNode armatureNode, Model mdl)
        {
            ClearPlaceholderNode(armatureNode);
            if (armatureNode.Nodes.Count > 0)
            {
                return;
            }

            var armature = mdl.GetArmature();
            if (armature == null || armature.Bones.Count == 0)
            {
                return;
            }

            for (int i = 0; i < armature.Bones.Count; i++)
            {
                var parent = armature.Bones[i].ParentIndex;
                if (parent >= 0 && parent < armature.Bones.Count && parent != i)
                {
                    continue;
                }

                armatureNode.Nodes.Add(CreateBoneNode(mdl, armature, i));
            }
        }

        private void EnsureArmatureChildNodes(TreeNode boneNode, NodeTag boneTag)
        {
            ClearPlaceholderNode(boneNode);
            if (boneNode.Nodes.Count > 0)
            {
                return;
            }

            var armature = boneTag.Model.GetArmature();
            if (armature == null || boneTag.BoneIndex == null)
            {
                return;
            }

            foreach (var child in armature.Bones[boneTag.BoneIndex.Value].Children)
            {
                var childIndex = armature.Bones.IndexOf(child);
                if (childIndex < 0)
                {
                    continue;
                }

                boneNode.Nodes.Add(CreateBoneNode(boneTag.Model, armature, childIndex));
            }
        }

        private static TreeNode CreateBoneNode(Model mdl, Armature armature, int boneIndex)
        {
            var bone = armature.Bones[boneIndex];
            var node = new TreeNode(bone.Name)
            {
                Tag = new NodeTag
                {
                    Type = NodeType.ArmatureBone,
                    Model = mdl,
                    BoneIndex = boneIndex
                }
            };

            if (bone.Children.Count > 0)
            {
                node.Nodes.Add(new TreeNode("..."));
            }

            return node;
        }

        private void SelectMaterialByName(string materialName)
        {
            if (string.IsNullOrWhiteSpace(materialName))
            {
                return;
            }

            modelProperties.SelectedTab = tabPage2;

            foreach (ListViewItem item in materialList.Items)
            {
                if (string.Equals(item.Text, materialName, StringComparison.OrdinalIgnoreCase))
                {
                    materialList.SelectedItems.Clear();
                    item.Selected = true;
                    item.Focused = true;
                    item.EnsureVisible();
                    break;
                }
            }
        }

        private void ClearSubmeshSelections()
        {
            foreach (var mdl in modelMap.Values)
            {
                mdl.SetSelectedSubmesh(-1);
            }
        }

        private void ModelViewerForm_DragEnter(object? sender, DragEventArgs e)
        {
            if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
            {
                e.Effect = DragDropEffects.Copy;
            }
        }

        private void ModelViewerForm_DragDrop(object? sender, DragEventArgs e)
        {
            if (e.Data?.GetData(DataFormats.FileDrop) is not string[] files || files.Length == 0)
            {
                return;
            }

            var modelFiles = files
                .Where(path => string.Equals(Path.GetExtension(path), ".trmdl", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (modelFiles.Count == 0)
            {
                return;
            }

            ClearAll();
            foreach (var modelFile in modelFiles)
            {
                AddModelToScene(modelFile);
            }
        }
    }
}
