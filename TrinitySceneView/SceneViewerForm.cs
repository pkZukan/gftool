using GFTool.RenderControl_WinForms;
using GFTool.Core.Flatbuffers.TR.Scene.Components;
using GFTool.Renderer;
using GFTool.Renderer.Core;
using GFTool.Renderer.Core.Graphics;
using System.Diagnostics;
using System.Text;
using TrinitySceneView.Loading;
using GFTool.Renderer.Scene.GraphicsObjects;
using Point = System.Drawing.Point;

namespace TrinitySceneView
{
    public partial class SceneViewerForm : Form
    {

        Point prevMousePos;

        private TRSceneTree? sceneTree;
        private SceneLoader? sceneLoader;
        private readonly Dictionary<SceneNode, Model> renderedModels = new();
        private readonly CancellationTokenSource lifetime = new();
        private string? assetRoot;
        private string? currentSceneFile;
        private bool loading;
        private bool closing;
        private string sceneStatus = "Open a scene to load its models.";
        private readonly ToolStripMenuItem assetRootMenu = new("Set Asset Root...");
        private readonly ToolStripMenuItem reloadMenu = new("Reload Scene");
        private readonly ToolStripMenuItem frameSceneMenu = new("Frame Scene");
        private readonly ToolStripMenuItem frameSelectedMenu = new("Frame Selected");

        public SceneViewerForm()
        {
            InitializeComponent();
            openTRSOT.Text = "Open Scene...";
            openTRSOT.Enabled = false;
            fileToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { assetRootMenu, reloadMenu });
            viewToolStripMenuItem.DropDownItems.Add(frameSceneMenu);
            sceneContext.Items.Add(frameSelectedMenu);
            sceneView.ShowNodeToolTips = true;
            sceneView.AfterSelect += (_, _) => ShowSelectedProperties();
            sceneView.NodeMouseDoubleClick += (_, _) => FrameSelected();
            PropertiesBox.ScrollBars = ScrollBars.Both;
            PropertiesBox.WordWrap = false;
            AttribBox.ScrollBars = ScrollBars.Both;
            AttribBox.WordWrap = false;
            reloadMenu.Enabled = frameSceneMenu.Enabled = false;
            assetRootMenu.Click += async (_, _) =>
            {
                using var dialog = new FolderBrowserDialog { Description = "Choose the extracted asset root (containing world, pokemon, or other game folders).", UseDescriptionForTitle = true };
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                assetRoot = dialog.SelectedPath;
                assetRootMenu.ToolTipText = assetRoot;
                if (currentSceneFile != null) await LoadSceneAsync(currentSceneFile);
            };
            reloadMenu.Click += async (_, _) => { if (currentSceneFile != null) await LoadSceneAsync(currentSceneFile); };
            frameSceneMenu.Click += (_, _) => { renderCtrl.renderer?.FrameModels(renderedModels.Values); renderCtrl.Invalidate(); };
            frameSelectedMenu.Click += (_, _) => FrameSelected();
            sceneContext.Opening += (_, _) =>
            {
                var selected = sceneTree?.GetNodeMeta(sceneView.SelectedNode);
                expandToolStripMenuItem.Enabled = !loading && selected?.IsExternal == true && !selected.ExternalLoaded;
                expandToolStripMenuItem.Text = "Retry Reference";
                frameSelectedMenu.Enabled = !loading && selected != null && ModelsFor(selected).Any();
            };
            renderCtrl.RendererReady += (_, _) => openTRSOT.Enabled = true;
            var statusTimer = new System.Windows.Forms.Timer(components) { Interval = 200, Enabled = true };
            statusTimer.Tick += (_, _) => UpdateStatus();
            FormClosing += (_, _) =>
            {
                closing = true;
                lifetime.Cancel();
                MessageHandler.Instance.MessageCallback -= messageHandler_Callback;
                renderCtrl.renderer?.ClearModels();
            };
        }

        private async void openTRSOT_Click(object sender, EventArgs e)
        {
            using var ofd = new OpenFileDialog { Filter = "Trinity scenes (*.trscn;*.trsot;*.trsog)|*.trscn;*.trsot;*.trsog|All files (*.*)|*.*" };
            if (ofd.ShowDialog(this) == DialogResult.OK) await LoadSceneAsync(ofd.FileName);
        }

        private async void expandToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var selected = sceneTree?.GetNodeMeta(sceneView.SelectedNode);
            if (loading || selected == null || sceneLoader == null || !selected.IsExternal || selected.ExternalLoaded) return;
            SetLoading(true);
            try
            {
                foreach (var node in selected.DescendantsAndSelf().ToArray())
                    if (renderedModels.Remove(node, out var model)) renderCtrl.renderer.RemoveModel(model);
                sceneLoader.ExpandExternal(selected);
                sceneTree!.Refresh();
                PopulateTree(selected);
                await LoadModelsAsync(selected);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { MessageHandler.Instance.AddMessage(MessageType.ERROR, ex.Message); }
            finally { if (!closing) { SetLoading(false); ShowSelectedProperties(); } }
        }

        private async Task LoadSceneAsync(string file)
        {
            if (loading || renderCtrl.renderer == null) return;
            SetLoading(true);
            sceneStatus = "Reading scene references...";
            try
            {
                var loader = new SceneLoader(assetRoot, lifetime.Token);
                var document = await Task.Run(() => loader.Load(file), lifetime.Token);
                lifetime.Token.ThrowIfCancellationRequested();
                // Keep the current scene if the replacement root cannot be read.
                if (document.Root.Error != null && document.Root.Data == null) throw new InvalidDataException(document.Root.Error);
                renderCtrl.renderer.ClearModels();
                renderedModels.Clear();
                messageListView.Items.Clear();
                currentSceneFile = file;
                sceneLoader = loader;
                sceneTree = new TRSceneTree(document);
                PopulateTree(document.Root);
                foreach (var node in document.Root.DescendantsAndSelf().Where(n => n.Error != null))
                    MessageHandler.Instance.AddMessage(MessageType.WARNING, $"{node.Name}: {node.Error}");
                await LoadModelsAsync(document.Root);
                if (renderedModels.Count > 0) renderCtrl.renderer.FrameModels(renderedModels.Values);
                Text = $"Trinity Scene Viewer — {document.Root.Name}";
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                sceneStatus = "Scene loading failed.";
                MessageHandler.Instance.AddMessage(MessageType.ERROR, $"Cannot load scene '{file}': {ex.GetBaseException().Message}");
                if (!closing) MessageBox.Show(this, ex.GetBaseException().Message, "Cannot load scene", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally { if (!closing) { SetLoading(false); ShowSelectedProperties(); } }
        }

        private async Task LoadModelsAsync(SceneNode root)
        {
            var nodes = root.DescendantsAndSelf().Where(n => n.IsModel).ToArray();
            var yieldTimer = Stopwatch.StartNew();
            var totalTimer = Stopwatch.StartNew();
            for (int i = 0; i < nodes.Length; i++)
            {
                lifetime.Token.ThrowIfCancellationRequested();
                var node = nodes[i];
                sceneStatus = $"Loading models: {i + 1}/{nodes.Length} — {node.Name}";
                if (!renderedModels.ContainsKey(node) && node.Error == null && File.Exists(node.ResolvedPath))
                {
                    try
                    {
                        var model = renderCtrl.renderer.AddModel(node.ResolvedPath!, assetRoot: assetRoot, reuseResources: true);
                        model.SetWorldTransforms(node.InstanceTransforms ?? new[] { node.WorldTransform });
                        renderedModels.Add(node, model);
                        node.Status = $"Rendered ({model.WorldTransforms.Count} placements)";
                    }
                    catch (Exception ex)
                    {
                        node.Status = "Model loading failed";
                        node.Error = ex.GetBaseException().Message;
                        MessageHandler.Instance.AddMessage(MessageType.ERROR, $"{node.Name}: {node.Error}");
                    }
                }
                sceneTree?.Update(node);
                // Let input and the viewport run between model uploads. All GL work stays on this thread.
                if (yieldTimer.ElapsedMilliseconds >= 30)
                {
                    await Task.Delay(1, lifetime.Token);
                    yieldTimer.Restart();
                }
            }
            int failed = sceneTree?.Document.Models.Count(n => n.Error != null) ?? 0;
            int unsupported = sceneTree?.Document.Root.DescendantsAndSelf().Count(n => n.Status == "Unsupported component") ?? 0;
            int placements = renderedModels.Values.Sum(m => m.WorldTransforms.Count);
            int uniqueModels = renderedModels.Keys.Select(n => n.ResolvedPath).Distinct(StringComparer.OrdinalIgnoreCase).Count();
            sceneStatus = $"{renderedModels.Count} models ({uniqueModels} unique) / {placements} placements loaded in {totalTimer.Elapsed.TotalSeconds:0.0}s; {failed} failed; {unsupported} unsupported components.";
            MessageHandler.Instance.AddMessage(MessageType.LOG, sceneStatus);
        }

        private void SetLoading(bool value)
        {
            loading = value;
            UseWaitCursor = value;
            openTRSOT.Enabled = assetRootMenu.Enabled = !value;
            reloadMenu.Enabled = !value && currentSceneFile != null;
            frameSceneMenu.Enabled = !value && renderedModels.Count > 0;
        }

        private void PopulateTree(SceneNode selected)
        {
            sceneView.BeginUpdate();
            try
            {
                sceneView.Nodes.Clear();
                sceneView.Nodes.Add(sceneTree!.TreeNode);
                sceneTree.TreeNode.Expand();
                sceneView.SelectedNode = sceneTree.GetTreeNode(selected) ?? sceneTree.TreeNode;
                sceneView.SelectedNode.EnsureVisible();
            }
            finally { sceneView.EndUpdate(); }
        }

        private IEnumerable<Model> ModelsFor(SceneNode node) => node.DescendantsAndSelf()
            .Where(renderedModels.ContainsKey).Select(n => renderedModels[n]);

        private void ShowSelectedProperties()
        {
            var node = sceneTree?.GetNodeMeta(sceneView.SelectedNode);
            if (node == null) { ClearProperties(); return; }
            PropertiesBox.Text = TRSceneProperties.GetProperties(node, ModelsFor(node));
            AttribBox.Text = TRSceneProperties.GetAttributes(node.Data);
        }

        private void FrameSelected()
        {
            var node = sceneTree?.GetNodeMeta(sceneView.SelectedNode);
            if (node != null && renderCtrl.renderer != null)
            {
                renderCtrl.renderer.FrameModels(ModelsFor(node));
                renderCtrl.Invalidate();
            }
        }

        private void ClearProperties()
        {
            PropertiesBox.Text = string.Empty;
            AttribBox.Text = string.Empty;
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

            ShowSelectedProperties();
        }

        //Update camera position info
        private void glCtxt_Paint(object sender, PaintEventArgs e)
        {
            UpdateStatus();
        }

        private void UpdateStatus()
        {
            var cam = renderCtrl.renderer?.GetCameraTransform();
            statusLbl.Text = cam == null ? sceneStatus : $"{sceneStatus}  Camera: {cam.Position}";
        }

        private void toolstripGBuf_Clicked(object sender, EventArgs e)
        {
            var item = sender as ToolStripMenuItem;
            if (item.Checked) return;

            GBuffer.DisplayType disp = GBuffer.DisplayType.DISPLAY_ALL;
            switch (item.Name)
            {
                case "toolstripGBuf_All": disp = GBuffer.DisplayType.DISPLAY_ALL; break;
                case "toolstripGBuf_Albedo": disp = GBuffer.DisplayType.DISPLAY_ALBEDO; break;
                case "toolstripGBuf_Normal": disp = GBuffer.DisplayType.DISPLAY_NORMAL; break;
                case "toolstripGBuf_Specular": disp = GBuffer.DisplayType.DISPLAY_SPECULAR; break;
                case "toolstripGBuf_AO": disp = GBuffer.DisplayType.DISPLAY_AO; break;
                case "toolstripGBuf_Depth": disp = GBuffer.DisplayType.DISPLAY_DEPTH; break;
            }

            //Only one checked at a time
            toolstripGBuf_All.CheckState = item.Name == "toolstripGBuf_All" ? CheckState.Checked : CheckState.Unchecked;
            toolstripGBuf_Albedo.CheckState = item.Name == "toolstripGBuf_Albedo" ? CheckState.Checked : CheckState.Unchecked;
            toolstripGBuf_Normal.CheckState = item.Name == "toolstripGBuf_Normal" ? CheckState.Checked : CheckState.Unchecked;
            toolstripGBuf_Specular.CheckState = item.Name == "toolstripGBuf_Specular" ? CheckState.Checked : CheckState.Unchecked;
            toolstripGBuf_AO.CheckState = item.Name == "toolstripGBuf_AO" ? CheckState.Checked : CheckState.Unchecked;
            toolstripGBuf_Depth.CheckState = item.Name == "toolstripGBuf_Depth" ? CheckState.Checked : CheckState.Unchecked;

            renderCtrl.renderer.SetGBufferDisplayMode(disp);
        }

        //Setup message list
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

        //Message handler
        private void messageHandler_Callback(object? sender, GFTool.Renderer.Core.Message e)
        {
            if (closing || IsDisposed) return;
            if (InvokeRequired) { BeginInvoke(() => messageHandler_Callback(sender, e)); return; }
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

        private void renderCtrl_Click(object sender, EventArgs e)
        {

        }
    }
}
