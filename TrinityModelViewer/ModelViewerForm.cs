using GFTool.Renderer.Core;
using GFTool.Renderer.Scene.GraphicsObjects;
using OpenTK.Mathematics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
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

        private sealed class CcDataReferences
        {
            public string? ModelPath { get; set; }
            public List<string> ResourcePaths { get; } = new List<string>();
            public List<string> AnimationDirectories { get; } = new List<string>();
        }

        private sealed class ModelPackageContext
        {
            public string? OriginalInputPath { get; init; }
            public IReadOnlyList<string> IndexedResources { get; init; } = Array.Empty<string>();
        }

        private Dictionary<TreeNode, Model> modelMap = new Dictionary<TreeNode, Model>();
        private ViewerSettings settings;
        private ToolStripMenuItem? lastModelToolStripMenuItem;
        private Image? texturePreviewImage;
        private Image? uvPreviewImage;
        private Image? sceneTexturePreviewImage;
        private TabPage? sceneTexturesTabPage;
        private DataGridView? sceneTexturesGrid;
        private PictureBox? sceneTexturePreviewBox;
        private TabPage? modelDetailsTabPage;
        private DataGridView? modelDetailsGrid;
        private Model? currentMaterialsModel;
        private Material? currentMaterial;
        private readonly List<GFTool.Renderer.Scene.GraphicsObjects.Animation> animations = new List<GFTool.Renderer.Scene.GraphicsObjects.Animation>();
        private readonly HashSet<string> loadedAnimationPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, GFTool.Renderer.Scene.GraphicsObjects.Animation> loadedAnimationsByPath = new Dictionary<string, GFTool.Renderer.Scene.GraphicsObjects.Animation>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<Model, ModelPackageContext> modelPackageContexts = new Dictionary<Model, ModelPackageContext>();
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
            var version = typeof(ModelViewerForm).Assembly.GetName().Version;
            if (version != null)
            {
                Text = $"Trinity Model Viewer v{version.Major}.{version.Minor}.{version.Build}";
            }
            settings = ViewerSettings.Load();
            this.startupFiles = startupFiles?.Where(s => !string.IsNullOrWhiteSpace(s)).ToArray() ?? Array.Empty<string>();
            MessageHandler.Instance.DebugLogsEnabled = settings.DebugLogs;
            ApplyRenderSettingsToMenu();
            SetupSceneTexturesTab();
            SetupModelDetailsTab();
            ApplyTheme();
            AddSettingsMenu();
            AddLastModelMenu();
            AddNpcPackageMenu();
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
                MessageType.ERROR => "Error",
                _ => "Log"
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
            if (!renderCtrl.RendererInitialized)
            {
                return;
            }

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
                    if (IsSupportedModelInput(path) && File.Exists(path))
                    {
                        AddModelInputToScene(path);
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

        private void AddNpcPackageMenu()
        {
            var exportPackage = new ToolStripMenuItem("Export NPC Package...")
            {
                ToolTipText = "Export the selected NPC model and all of its indexed resources."
            };
            exportPackage.Click += exportNpcPackageToolStripMenuItem_Click;

            fileToolStripMenuItem.DropDownItems.Add(new ToolStripSeparator());
            fileToolStripMenuItem.DropDownItems.Add(exportPackage);
        }

        private void exportNpcPackageToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            var model = GetModelForAnimationExport();
            if (model == null)
            {
                MessageBox.Show(this, "Select a loaded NPC model first.", "Export NPC Package", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (model.Name.StartsWith("pm", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show(this, "This exporter is intentionally limited to NPC models.", "Export NPC Package", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using var dialog = new FolderBrowserDialog
            {
                Description = "Choose where to create the NPC package folder",
                UseDescriptionForTitle = true,
                ShowNewFolderButton = true
            };
            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            var safeName = Regex.Replace(model.Name, @"[^A-Za-z0-9_.-]+", "_").Trim('_');
            if (string.IsNullOrWhiteSpace(safeName))
            {
                safeName = "npc_model";
            }

            var packagePath = Path.Combine(dialog.SelectedPath, $"{safeName}_npc_package");
            if (Directory.Exists(packagePath) || File.Exists(packagePath))
            {
                packagePath = Path.Combine(dialog.SelectedPath, $"{safeName}_npc_package_{DateTime.Now:yyyyMMdd_HHmmss}");
            }

            modelPackageContexts.TryGetValue(model, out var context);
            try
            {
                var result = NpcPackageExporter.Export(
                    model,
                    packagePath,
                    context?.OriginalInputPath,
                    context?.IndexedResources ?? Array.Empty<string>(),
                    loadedAnimationPaths.ToArray());

                DiagnosticLog.Section("Export NPC package");
                DiagnosticLog.Write($"Package: {result.PackagePath}");
                DiagnosticLog.Write($"Entry point: {result.EntryPoint}");
                DiagnosticLog.Write($"Files: {result.FileCount}, bytes: {result.TotalBytes}, missing: {result.MissingCount}");

                var missingText = result.MissingCount == 0
                    ? "All referenced resources were found."
                    : $"Missing references: {result.MissingCount} (see manifest.json).";
                MessageBox.Show(
                    this,
                    $"NPC package exported.\n\nFolder: {result.PackagePath}\nEntry point: {result.EntryPoint}\nFiles: {result.FileCount}\nSize: {result.TotalBytes:N0} bytes\n{missingText}",
                    "Export NPC Package",
                    MessageBoxButtons.OK,
                    result.MissingCount == 0 ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                DiagnosticLog.WriteException($"NPC package export failed: {packagePath}", ex);
                MessageBox.Show(this, $"Failed to export NPC package:\n{ex.Message}", "Export NPC Package", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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
            AddModelInputToScene(settings.LastModelPath);
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

        private GFTool.Renderer.Scene.GraphicsObjects.Animation? LoadAnimationFile(string file, string source, out bool loadedNew)
        {
            loadedNew = false;
            if (string.IsNullOrWhiteSpace(file))
            {
                return null;
            }

            file = Path.GetFullPath(file);
            if (loadedAnimationsByPath.TryGetValue(file, out var existing))
            {
                DiagnosticLog.Write($"{source} animation skipped duplicate: {file}");
                return existing;
            }

            if (!loadedAnimationPaths.Add(file))
            {
                DiagnosticLog.Write($"{source} animation skipped duplicate path without cached animation: {file}");
                return null;
            }

            DiagnosticLog.Write($"{source} animation load attempt: {file}");
            var animFile = FlatBufferConverter.DeserializeFrom<GfAnim.Animation>(file);
            var anim = new GFTool.Renderer.Scene.GraphicsObjects.Animation(animFile, Path.GetFileNameWithoutExtension(file));
            animations.Add(anim);
            loadedAnimationsByPath[file] = anim;

            var item = new ListViewItem(anim.Name) { Tag = anim };
            animationsList.Items.Add(item);
            loadedNew = true;
            DiagnosticLog.Write(
                $"{source} animation loaded: name={anim.Name}, frames={anim.FrameCount}, fps={anim.FrameRate}, " +
                $"tracks={anim.TrackCount}, loop={anim.LoopType}, mouthTracks={anim.MouthPoseTrackCount}, " +
                $"activeMouthTracks={anim.ActiveMouthPoseTrackCount}, " +
                $"embeddedMouth={anim.UsesEmbeddedMouthPoseTracks}, zeroEndpointTracks={anim.ZeroEndpointPlaceholderTrackCount}, " +
                $"animatedMiddleTracks={anim.AnimatedBetweenPlaceholderEndpointsTrackCount}, " +
                $"zeroEndpointEncoding={anim.UsesZeroEndpointPlaceholderEncoding}, additive={anim.UsesAdditivePoseEncoding}, " +
                $"animatedMouthOverlay={anim.RequiresAnimatedMouthOverlay}");
            TryAttachActionClip(anim, file);

            if (MessageHandler.Instance.DebugLogsEnabled)
            {
                MessageHandler.Instance.AddMessage(MessageType.LOG, $"[Anim] Loaded '{anim.Name}' file='{file}' frames={anim.FrameCount} fps={anim.FrameRate} tracks={anim.TrackCount}");
            }

            return anim;
        }

        private static void TryAttachActionClip(GFTool.Renderer.Scene.GraphicsObjects.Animation animation, string animationFile)
        {
            try
            {
                string tracm = Path.ChangeExtension(animationFile, ".tracm");
                if (!File.Exists(tracm))
                {
                    return;
                }

                var clip = ActionClipAnimation.Load(tracm);
                animation.AttachActionClip(clip);
                string preview = clip.TargetNames.Count == 0
                    ? "<none>"
                    : string.Join(", ", clip.TargetNames.Take(16));
                if (clip.TargetNames.Count > 16)
                {
                    preview += $", ... (+{clip.TargetNames.Count - 16})";
                }

                DiagnosticLog.Write($"Action clip loaded: tracm={Path.GetFileName(tracm)}, frames={clip.FrameCount}, fps={clip.FrameRate}, targets={clip.TargetNames.Count}, visibilityTracks={clip.VisibilityTracks.Count}, materialVector4Tracks={clip.Vector4Tracks.Count}, targetNames={preview}");
                foreach (var track in clip.VisibilityTracks.Take(24))
                {
                    DiagnosticLog.Write($"  visibility animation: target={track.TargetName}, encoding={track.Kind}, frame0={track.Sample(0f)}, frameEnd={track.Sample(Math.Max(0, clip.FrameCount - 1))}");
                }
                foreach (var track in clip.Vector4Tracks.Take(16))
                {
                    float endFrame = Math.Max(0, clip.FrameCount - 1);
                    var first = track.Sample(0f, OpenTK.Mathematics.Vector4.Zero);
                    var quarter = track.Sample(endFrame * 0.25f, first);
                    var middle = track.Sample(endFrame * 0.50f, first);
                    var last = track.Sample(endFrame, first);
                    DiagnosticLog.Write($"  material animation: target={track.TargetName}, material={track.MaterialName}, parameter={track.ParameterName}, frame0=({first.X}, {first.Y}, {first.Z}, {first.W}), frame25%=({quarter.X}, {quarter.Y}, {quarter.Z}, {quarter.W}), frame50%=({middle.X}, {middle.Y}, {middle.Z}, {middle.W}), frameEnd=({last.X}, {last.Y}, {last.Z}, {last.W})");
                }
            }
            catch (Exception ex)
            {
                DiagnosticLog.WriteException($"Action clip load failed: {animationFile}", ex);
            }
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
                DiagnosticLog.Section("Manual animation load");
                int loaded = 0;
                foreach (var file in ofd.FileNames.Where(f => !string.IsNullOrWhiteSpace(f)))
                {
                    var anim = LoadAnimationFile(file, "Manual", out var loadedNew);
                    if (loadedNew)
                    {
                        loaded++;
                    }
                }

                if (loaded > 0)
                {
                    animationsList.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
                }
            }
            catch (Exception ex)
            {
                DiagnosticLog.WriteException("Manual animation load failed", ex);
                MessageBox.Show(this, $"Failed to load animation:\n{ex.Message}", "Animation Load", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void playAnimationButton_Click(object sender, EventArgs e)
        {
            var anim = GetSelectedAnimation();
            if (anim == null || !renderCtrl.RendererInitialized)
            {
                return;
            }

            PlayAnimationWithFacialLayers(anim);
        }

        private void stopAnimationButton_Click(object sender, EventArgs e)
        {
            if (!renderCtrl.RendererInitialized)
            {
                return;
            }

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
                DiagnosticLog.Section("Export animation");
                DiagnosticLog.Write($"Export animation: name={anim.Name}, out={sfd.FileName}, model={mdl.Name}, bones={mdl.Armature.Bones.Count}");
                GltfExporter.ExportAnimation(mdl.Armature, anim, sfd.FileName);
                MessageBox.Show(this, $"Exported:\n{sfd.FileName}", "Export Animation", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                DiagnosticLog.WriteException("Export animation failed", ex);
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
                DiagnosticLog.Section("Export model with animations");
                DiagnosticLog.Write($"Export model with animations: model={mdl.Name}, out={sfd.FileName}, animationCount={animations.Count}");
                var anims = animations.ToArray();
                if (anims.Length == 0)
                {
                    MessageBox.Show(this, "No animations are loaded; exporting the model only.", "Export Model + Animations", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                GltfExporter.ExportModel(mdl, sfd.FileName, anims);
                MessageBox.Show(this, $"Exported:\n{sfd.FileName}\n\nBlender helper:\n{GltfExporter.GetBlenderMaterialScriptPath(sfd.FileName)}\n\nTexture manifest:\n{GltfExporter.GetTextureManifestPath(sfd.FileName)}", "Export Model + Animations", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                DiagnosticLog.WriteException("Export model with animations failed", ex);
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
            if (anim == null || !renderCtrl.RendererInitialized)
            {
                return;
            }

            PlayAnimationWithFacialLayers(anim);
        }

        private void PlayAnimationWithFacialLayers(GFTool.Renderer.Scene.GraphicsObjects.Animation anim)
        {
            ApplyMeshVariantVisibilityForAnimation(anim);
            if (anim.UsesAdditivePoseEncoding)
            {
                var baseAnimation = FindAdditiveBaseAnimation(anim.Name);
                if (baseAnimation != null)
                {
                    renderCtrl.renderer.PlayAnimation(baseAnimation);
                    renderCtrl.renderer.PlayAdditiveAnimation(anim);
                    DiagnosticLog.Write(
                        $"Animation additive stack: selected={anim.Name}, base={baseAnimation.Name}, " +
                        $"activeTracks={anim.AnimatedBetweenPlaceholderEndpointsTrackCount}");
                }
                else
                {
                    renderCtrl.renderer.PlayAnimation(anim);
                    DiagnosticLog.Write($"Animation additive stack: no base animation available for {anim.Name}");
                }
            }
            else
            {
                renderCtrl.renderer.PlayAnimation(anim);
            }

            if (anim.IsFacialOverlay)
            {
                return;
            }

            if (anim.UsesEmbeddedMouthPoseTracks)
            {
                DiagnosticLog.Write($"Animation mouth baseline skipped: body={anim.Name}, reason=embedded speak tracks");
                return;
            }

            var neutralMouth = FindNeutralMouthAnimation(anim.Name);
            if (neutralMouth == null)
            {
                DiagnosticLog.Write($"Animation mouth baseline: no mouth animation available for {anim.Name}");
                return;
            }

            bool animateMouth = anim.RequiresAnimatedMouthOverlay;
            DiagnosticLog.Write(
                $"Animation mouth overlay: body={anim.Name}, mouth={neutralMouth.Name}, " +
                $"mode={(animateMouth ? "speech-loop" : "neutral-frame")}, frame={(animateMouth ? "animated" : "0")}");
            renderCtrl.renderer.PlayAnimation(
                neutralMouth,
                holdFacialOverlayAtFirstFrame: !animateMouth,
                loopFacialOverlay: animateMouth);
        }

        private GFTool.Renderer.Scene.GraphicsObjects.Animation? FindAdditiveBaseAnimation(string additiveAnimationName)
        {
            return animations
                .Where(animation => !animation.IsFacialOverlay && !animation.UsesAdditivePoseEncoding)
                .OrderByDescending(animation => ScoreAdditiveBaseAnimation(animation.Name, additiveAnimationName))
                .ThenBy(animation => animation.Name, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault();
        }

        private static int ScoreAdditiveBaseAnimation(string name, string additiveAnimationName)
        {
            int score = 0;
            var prefixMatch = Regex.Match(additiveAnimationName, @"^(?<prefix>[^_]+_[^_]+)_", RegexOptions.CultureInvariant);
            if (prefixMatch.Success && name.StartsWith(prefixMatch.Groups["prefix"].Value + "_", StringComparison.OrdinalIgnoreCase))
            {
                score += 2000;
            }
            if (name.Contains("00001", StringComparison.OrdinalIgnoreCase)) score += 1000;
            if (name.Contains("defaultwait01", StringComparison.OrdinalIgnoreCase)) score += 500;
            if (name.Contains("loop", StringComparison.OrdinalIgnoreCase)) score += 50;
            if (name.Contains("speak", StringComparison.OrdinalIgnoreCase)) score -= 200;
            return score;
        }

        private GFTool.Renderer.Scene.GraphicsObjects.Animation? FindNeutralMouthAnimation(string bodyAnimationName)
        {
            return animations
                .Where(animation => animation.AllowsMouthPoseTracks)
                .OrderByDescending(animation => ScoreNeutralMouthAnimation(animation.Name, bodyAnimationName))
                .ThenBy(animation => animation.Name, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault();
        }

        private static int ScoreNeutralMouthAnimation(string name, string bodyAnimationName)
        {
            int score = 0;
            var prefixMatch = Regex.Match(bodyAnimationName, @"^(?<prefix>[^_]+_[^_]+)_", RegexOptions.CultureInvariant);
            if (prefixMatch.Success && name.StartsWith(prefixMatch.Groups["prefix"].Value + "_", StringComparison.OrdinalIgnoreCase))
            {
                score += 2000;
            }
            if (name.Contains("08101", StringComparison.OrdinalIgnoreCase)) score += 1000;
            if (name.Contains("mouth01", StringComparison.OrdinalIgnoreCase)) score += 500;
            if (name.Contains("mouth", StringComparison.OrdinalIgnoreCase)) score += 100;
            if (name.Contains("speak", StringComparison.OrdinalIgnoreCase)) score -= 50;
            return score;
        }

        private GFTool.Renderer.Scene.GraphicsObjects.Animation? GetSelectedAnimation()
        {
            if (animationsList.SelectedItems.Count == 0)
            {
                return null;
            }

            return animationsList.SelectedItems[0].Tag as Animation;
        }

        private void ApplyMeshVariantVisibilityForAnimation(GFTool.Renderer.Scene.GraphicsObjects.Animation anim)
        {
            if (!renderCtrl.RendererInitialized)
            {
                return;
            }

            const char fallbackVariant = 'a';
            DiagnosticLog.Write($"Animation mesh variant pick: animation={anim.Name}, fallbackVariant={fallbackVariant}, trackCount={anim.TrackNames.Count}");
            renderCtrl.renderer.ApplyMeshVariantVisibility(anim, fallbackVariant, $"animation={anim.Name}");
        }

        private static char GetPreferredMeshVariantFromAnimationName(string animationName)
        {
            if (string.IsNullOrWhiteSpace(animationName))
            {
                return 'a';
            }

            var match = Regex.Match(animationName, @"(?:^|_)(?<group>[0-9])\d{4}_", RegexOptions.CultureInvariant);
            if (!match.Success)
            {
                return 'a';
            }

            int group = match.Groups["group"].Value[0] - '0';
            if (group < 0 || group >= 26)
            {
                return 'a';
            }

            return (char)('a' + group);
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
            if (renderCtrl.RendererInitialized)
            {
                renderCtrl.renderer.StopAnimation();
                renderCtrl.renderer.ClearScene();
            }

            messageListView.Items.Clear();
            materialList.Items.Clear();
            materialList.Columns.Clear();
            modelMap.Clear();
            sceneTree.Nodes.Clear();
            animations.Clear();
            animationsList.Items.Clear();
            loadedAnimationPaths.Clear();
            loadedAnimationsByPath.Clear();
            modelPackageContexts.Clear();
            currentMaterialsModel = null;
            currentMaterial = null;
            ClearMaterialDetails();
            ClearSceneTextureDetails();
            ClearModelDetails();
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Trinity Model files (*.trmdl;*.ccdata)|*.trmdl;*.ccdata|All files (*.*)|*.*";
            if (ofd.ShowDialog() != DialogResult.OK) return;

            ClearAll();
            try
            {
                AddModelInputToScene(ofd.FileName);
            }
            catch (Exception ex)
            {
                DiagnosticLog.WriteException($"Open model failed: {ofd.FileName}", ex);
                MessageBox.Show(this, $"Failed to load model:\n{ex.Message}", "Model Load", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void importToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Trinity Model files (*.trmdl;*.ccdata)|*.trmdl;*.ccdata|All files (*.*)|*.*";
            ofd.Multiselect = true;
            if (ofd.ShowDialog() != DialogResult.OK) return;

            foreach (var file in ofd.FileNames.Where(f => !string.IsNullOrWhiteSpace(f)))
            {
                try
                {
                    AddModelInputToScene(file);
                }
                catch (Exception ex)
                {
                    DiagnosticLog.WriteException($"Import model failed: {file}", ex);
                    MessageBox.Show(this, $"Failed to load model:\n{file}\n\n{ex.Message}", "Model Load", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void wireframeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!renderCtrl.RendererInitialized)
            {
                return;
            }

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
            if (!renderCtrl.RendererInitialized) return;
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
                if (!renderCtrl.RendererInitialized) return;

                renderCtrl.renderer.RemoveSceneModel(mdl);
                sceneTree.Nodes.Remove(selected);
                modelMap.Remove(selected);
                modelPackageContexts.Remove(mdl);
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
                DiagnosticLog.Section("Export model");
                DiagnosticLog.Write($"Export model: model={mdl.Name}, out={sfd.FileName}");
                GltfExporter.ExportModel(mdl, sfd.FileName);
                MessageBox.Show(this, $"Exported:\n{sfd.FileName}\n\nBlender helper:\n{GltfExporter.GetBlenderMaterialScriptPath(sfd.FileName)}\n\nTexture manifest:\n{GltfExporter.GetTextureManifestPath(sfd.FileName)}", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                DiagnosticLog.WriteException("Export model failed", ex);
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

        private void SetupSceneTexturesTab()
        {
            sceneTexturesTabPage = new TabPage("Textures");

            var split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal
            };

            sceneTexturesGrid = new DataGridView
            {
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
                Dock = DockStyle.Fill,
                MultiSelect = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };
            sceneTexturesGrid.Columns.Add("TextureModel", "Model");
            sceneTexturesGrid.Columns.Add("TextureMaterial", "Material");
            sceneTexturesGrid.Columns.Add("TextureShader", "Shader");
            sceneTexturesGrid.Columns.Add("TextureName", "Texture");
            sceneTexturesGrid.Columns.Add("TextureSlot", "Slot");
            sceneTexturesGrid.Columns.Add("TextureStatus", "Status");
            sceneTexturesGrid.Columns.Add("TextureSize", "Size");
            sceneTexturesGrid.Columns.Add("TextureFile", "File");
            sceneTexturesGrid.SelectionChanged += sceneTexturesGrid_SelectionChanged;

            sceneTexturePreviewBox = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom
            };

            split.Panel1.Controls.Add(sceneTexturesGrid);
            split.Panel2.Controls.Add(sceneTexturePreviewBox);
            sceneTexturesTabPage.Controls.Add(split);
            leftTabs.Controls.Add(sceneTexturesTabPage);
        }

        private void ClearSceneTextureDetails()
        {
            sceneTexturesGrid?.Rows.Clear();
            SetSceneTexturePreview(null);
        }

        private void SetupModelDetailsTab()
        {
            modelDetailsTabPage = new TabPage("Details");
            modelDetailsGrid = new DataGridView
            {
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
                Dock = DockStyle.Fill,
                MultiSelect = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };
            modelDetailsGrid.Columns.Add("DetailName", "Name");
            modelDetailsGrid.Columns.Add("DetailValue", "Value");
            modelDetailsGrid.Columns[0].FillWeight = 38;
            modelDetailsGrid.Columns[1].FillWeight = 62;

            modelDetailsTabPage.Controls.Add(modelDetailsGrid);
            modelProperties.Controls.Add(modelDetailsTabPage);
        }

        private void ClearModelDetails()
        {
            modelDetailsGrid?.Rows.Clear();
        }

        private void AddDetailRow(string name, object? value)
        {
            if (modelDetailsGrid == null)
            {
                return;
            }

            modelDetailsGrid.Rows.Add(name, FormatDetailValue(value));
        }

        private static string FormatDetailValue(object? value)
        {
            if (value == null)
            {
                return string.Empty;
            }

            return value switch
            {
                float f => f.ToString("0.#####"),
                double d => d.ToString("0.#####"),
                Vector2 v => $"{v.X:0.#####}, {v.Y:0.#####}",
                Vector3 v => $"{v.X:0.#####}, {v.Y:0.#####}, {v.Z:0.#####}",
                Vector4 v => $"{v.X:0.#####}, {v.Y:0.#####}, {v.Z:0.#####}, {v.W:0.#####}",
                _ => value.ToString() ?? string.Empty
            };
        }

        private void ShowDetailsForNode(TreeNode? node)
        {
            ClearModelDetails();
            if (node?.Tag is not NodeTag tag)
            {
                return;
            }

            switch (tag.Type)
            {
                case NodeType.ModelRoot:
                    PopulateModelDetails(tag.Model);
                    break;
                case NodeType.MeshGroup:
                    PopulateMeshGroupDetails(tag.Model);
                    break;
                case NodeType.Mesh:
                    PopulateMeshDetails(tag);
                    break;
                case NodeType.MaterialsGroup:
                    PopulateMaterialsGroupDetails(tag);
                    break;
                case NodeType.Material:
                    PopulateMaterialDetailsGrid(tag.Model, tag.MaterialName);
                    break;
                case NodeType.ArmatureGroup:
                    PopulateArmatureDetails(tag.Model);
                    break;
                case NodeType.ArmatureBone:
                    PopulateBoneDetails(tag);
                    break;
            }
        }

        private void PopulateModelDetails(Model mdl)
        {
            var export = mdl.CreateExportData();
            var submeshCount = export.Submeshes.Count;
            var vertexCount = export.Submeshes.Sum(s => s.Positions.Length);
            var indexCount = export.Submeshes.Sum(s => s.Indices.Length);
            var triangleCount = export.Submeshes.Sum(s => s.Indices.Length / 3);
            var textureCount = export.Materials.Sum(m => m.Textures.Count);
            var shaderCount = export.Materials.Select(m => m.ShaderName).Distinct(StringComparer.OrdinalIgnoreCase).Count();
            var bounds = CalculateBounds(export.Submeshes.SelectMany(s => s.Positions));

            AddDetailRow("Type", "Model");
            AddDetailRow("Name", mdl.Name);
            AddDetailRow("Source", mdl.SourcePath);
            AddDetailRow("Visible", mdl.IsVisible ? "Yes" : "No");
            AddDetailRow("Submeshes", submeshCount);
            AddDetailRow("Materials", export.Materials.Count);
            AddDetailRow("Shaders", shaderCount);
            AddDetailRow("Textures", textureCount);
            AddDetailRow("Vertices", vertexCount);
            AddDetailRow("Indices", indexCount);
            AddDetailRow("Triangles", triangleCount);
            AddDetailRow("Armature bones", export.Armature?.Bones.Count ?? 0);
            AddDetailRow("Bounds min", bounds.Min);
            AddDetailRow("Bounds max", bounds.Max);
            AddDetailRow("Bounds size", bounds.Size);

            foreach (var shader in export.Materials
                .GroupBy(m => m.ShaderName, StringComparer.OrdinalIgnoreCase)
                .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase))
            {
                AddDetailRow($"Shader: {shader.Key}", $"{shader.Count()} material(s)");
            }
        }

        private void PopulateMeshGroupDetails(Model mdl)
        {
            var meshes = BuildMeshEntries(mdl);
            AddDetailRow("Type", "Mesh group");
            AddDetailRow("Model", mdl.Name);
            AddDetailRow("Meshes", meshes.Count);
            AddDetailRow("Submeshes", meshes.Sum(m => m.SubmeshIndices.Count));

            foreach (var mesh in meshes)
            {
                AddDetailRow(mesh.Name, $"{mesh.SubmeshIndices.Count} submesh(es), {mesh.MaterialMap.Count} material(s)");
            }
        }

        private void PopulateMeshDetails(NodeTag tag)
        {
            var export = tag.Model.CreateExportData();
            var indices = tag.SubmeshIndices ?? new List<int>();
            var submeshes = indices
                .Where(i => i >= 0 && i < export.Submeshes.Count)
                .Select(i => new { Index = i, Data = export.Submeshes[i] })
                .ToList();
            var bounds = CalculateBounds(submeshes.SelectMany(s => s.Data.Positions));

            AddDetailRow("Type", "Mesh");
            AddDetailRow("Model", tag.Model.Name);
            AddDetailRow("Mesh", tag.MeshName);
            AddDetailRow("Submesh indices", string.Join(", ", indices));
            AddDetailRow("Submeshes", submeshes.Count);
            AddDetailRow("Materials", string.Join(", ", submeshes.Select(s => s.Data.MaterialName).Distinct(StringComparer.OrdinalIgnoreCase)));
            AddDetailRow("Vertices", submeshes.Sum(s => s.Data.Positions.Length));
            AddDetailRow("Indices", submeshes.Sum(s => s.Data.Indices.Length));
            AddDetailRow("Triangles", submeshes.Sum(s => s.Data.Indices.Length / 3));
            AddDetailRow("Bounds min", bounds.Min);
            AddDetailRow("Bounds max", bounds.Max);
            AddDetailRow("Bounds size", bounds.Size);

            foreach (var submesh in submeshes)
            {
                AddDetailRow($"Submesh {submesh.Index}", $"{submesh.Data.Name} | mat={submesh.Data.MaterialName} | v={submesh.Data.Positions.Length} i={submesh.Data.Indices.Length} uvSets={submesh.Data.UVSets.Count}");
            }
        }

        private void PopulateMaterialsGroupDetails(NodeTag tag)
        {
            AddDetailRow("Type", "Mesh materials");
            AddDetailRow("Model", tag.Model.Name);
            AddDetailRow("Mesh", tag.MeshName);
            if (tag.MaterialMap == null)
            {
                return;
            }

            AddDetailRow("Materials", tag.MaterialMap.Count);
            foreach (var kvp in tag.MaterialMap.OrderBy(k => k.Key, StringComparer.OrdinalIgnoreCase))
            {
                AddDetailRow(kvp.Key, $"submesh {string.Join(", ", kvp.Value)}");
            }
        }

        private void PopulateMaterialDetailsGrid(Model mdl, string? materialName)
        {
            var mat = mdl.GetMaterials().FirstOrDefault(m => string.Equals(m.Name, materialName, StringComparison.OrdinalIgnoreCase));
            if (mat == null)
            {
                AddDetailRow("Type", "Material");
                AddDetailRow("Name", materialName);
                AddDetailRow("Status", "Not found");
                return;
            }

            PopulateMaterialDetailsGrid(mdl, mat);
        }

        private void PopulateMaterialDetailsGrid(Model? mdl, Material mat)
        {
            AddDetailRow("Type", "Material");
            AddDetailRow("Model", mdl?.Name);
            AddDetailRow("Name", mat.Name);
            AddDetailRow("Shader", mat.ShaderName);
            AddDetailRow("Transparent", mat.IsTransparent ? "Yes" : "No");
            AddDetailRow("Textures", mat.Textures.Count);
            AddDetailRow("Shader options", mat.ShaderParameters.Count);
            AddDetailRow("Float params", mat.FloatParameters.Count);
            AddDetailRow("Vec2 params", mat.Vec2Parameters.Count);
            AddDetailRow("Vec3 params", mat.Vec3Parameters.Count);
            AddDetailRow("Vec4 params", mat.Vec4Parameters.Count);
            AddDetailRow("Samplers", mat.Samplers.Count);
            AddDetailRow("Eye base sclera", mat.EnableEyeBaseSclera ? "Yes" : "No");
            AddDetailRow("Eye point light", mat.EyePointLightIndex);

            if (mdl != null)
            {
                var submeshes = mdl.GetSubmeshMaterials()
                    .Select((name, index) => new { name, index })
                    .Where(x => string.Equals(x.name, mat.Name, StringComparison.OrdinalIgnoreCase))
                    .Select(x => x.index)
                    .ToList();
                AddDetailRow("Used by submeshes", submeshes.Count == 0 ? "<none>" : string.Join(", ", submeshes));
            }

            foreach (var tex in mat.Textures)
            {
                var resolved = tex.TryGetResolvedSourcePath(out var path) && File.Exists(path) ? path : "missing";
                AddDetailRow($"Texture: {tex.Name}", $"slot={tex.Slot}, size={tex.Width}x{tex.Height}, file={tex.SourceFile}, resolved={resolved}");
            }
        }

        private void PopulateArmatureDetails(Model mdl)
        {
            var armature = mdl.GetArmature();
            AddDetailRow("Type", "Armature");
            AddDetailRow("Model", mdl.Name);
            AddDetailRow("Bones", armature?.Bones.Count ?? 0);
            AddDetailRow("Joint info count", armature?.JointInfoCount ?? 0);
            AddDetailRow("Bone meta count", armature?.BoneMetaCount ?? 0);
            if (armature == null)
            {
                return;
            }

            AddDetailRow("Skinning bones", armature.Bones.Count(b => b.Skinning));
            AddDetailRow("Root bones", armature.Bones.Count(b => b.ParentIndex < 0 || b.ParentIndex >= armature.Bones.Count));
            foreach (var root in armature.Bones.Select((bone, index) => new { bone, index }).Where(x => x.bone.ParentIndex < 0 || x.bone.ParentIndex >= armature.Bones.Count))
            {
                AddDetailRow($"Root {root.index}", root.bone.Name);
            }
        }

        private void PopulateBoneDetails(NodeTag tag)
        {
            var armature = tag.Model.GetArmature();
            AddDetailRow("Type", "Bone");
            AddDetailRow("Model", tag.Model.Name);
            if (armature == null || tag.BoneIndex == null || tag.BoneIndex.Value < 0 || tag.BoneIndex.Value >= armature.Bones.Count)
            {
                AddDetailRow("Status", "Not found");
                return;
            }

            int index = tag.BoneIndex.Value;
            var bone = armature.Bones[index];
            AddDetailRow("Index", index);
            AddDetailRow("Name", bone.Name);
            AddDetailRow("Parent index", bone.ParentIndex);
            AddDetailRow("Parent", bone.ParentIndex >= 0 && bone.ParentIndex < armature.Bones.Count ? armature.Bones[bone.ParentIndex].Name : "<root>");
            AddDetailRow("Children", bone.Children.Count);
            AddDetailRow("Skinning", bone.Skinning ? "Yes" : "No");
            AddDetailRow("Has joint inverse bind", bone.HasJointInverseBind ? "Yes" : "No");
            AddDetailRow("Segment scale compensate", bone.UseSegmentScaleCompensate ? "Yes" : "No");
            AddDetailRow("Rest position", bone.RestPosition);
            AddDetailRow("Rest rotation euler", bone.RestEuler);
            AddDetailRow("Rest scale", bone.RestScale);
        }

        private static (Vector3 Min, Vector3 Max, Vector3 Size) CalculateBounds(IEnumerable<Vector3> positions)
        {
            var min = new Vector3(float.PositiveInfinity);
            var max = new Vector3(float.NegativeInfinity);
            bool any = false;
            foreach (var position in positions)
            {
                min = Vector3.ComponentMin(min, position);
                max = Vector3.ComponentMax(max, position);
                any = true;
            }

            if (!any)
            {
                return (Vector3.Zero, Vector3.Zero, Vector3.Zero);
            }

            return (min, max, max - min);
        }

        private void PopulateSceneTextures(Model mdl)
        {
            if (sceneTexturesGrid == null)
            {
                return;
            }

            sceneTexturesGrid.SuspendLayout();
            try
            {
                foreach (var mat in mdl.GetMaterials())
                {
                    if (mat == null)
                    {
                        continue;
                    }

                    foreach (var tex in mat.Textures)
                    {
                        var status = DescribeSceneTexture(tex);
                        int row = sceneTexturesGrid.Rows.Add(
                            mdl.Name,
                            mat.Name,
                            mat.ShaderName,
                            tex.Name,
                            tex.Slot.ToString(),
                            status.Status,
                            status.Size,
                            tex.SourceFile);
                        sceneTexturesGrid.Rows[row].Tag = tex;
                        if (status.Status != "OK")
                        {
                            sceneTexturesGrid.Rows[row].DefaultCellStyle.ForeColor = Color.OrangeRed;
                        }
                    }
                }

                if (sceneTexturesGrid.Rows.Count > 0 && sceneTexturesGrid.SelectedRows.Count == 0)
                {
                    sceneTexturesGrid.Rows[0].Selected = true;
                }
            }
            finally
            {
                sceneTexturesGrid.ResumeLayout();
            }
        }

        private static (string Status, string Size) DescribeSceneTexture(Texture tex)
        {
            bool hasSource = tex.TryGetResolvedSourcePath(out var sourcePath) && File.Exists(sourcePath);
            using var bmp = tex.LoadPreviewBitmap();
            if (bmp != null)
            {
                return ("OK", $"{bmp.Width}x{bmp.Height}");
            }

            return (hasSource ? "Decode failed" : "Missing", string.Empty);
        }

        private void sceneTexturesGrid_SelectionChanged(object? sender, EventArgs e)
        {
            if (sceneTexturesGrid == null || sceneTexturesGrid.SelectedRows.Count == 0)
            {
                SetSceneTexturePreview(null);
                return;
            }

            if (sceneTexturesGrid.SelectedRows[0].Tag is Texture texture)
            {
                SetSceneTexturePreview(texture.LoadPreviewBitmap());
                return;
            }

            SetSceneTexturePreview(null);
        }

        private void SetSceneTexturePreview(Image? image)
        {
            sceneTexturePreviewImage?.Dispose();
            sceneTexturePreviewImage = image;
            if (sceneTexturePreviewBox != null)
            {
                sceneTexturePreviewBox.Image = image;
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
                DiagnosticLog.Section("Export texture");
                DiagnosticLog.Write($"Export texture: name={texture.Name}, source={texture.SourceFile}, out={outPath}, ext={ext}");
                if (ext == ".bntx")
                {
                    if (!texture.TryGetResolvedSourcePath(out var sourcePath) || !File.Exists(sourcePath))
                    {
                        DiagnosticLog.Write($"Export texture failed: original BNTX source missing for {texture.SourceFile}");
                        MessageBox.Show(this, "Source BNTX file was not found on disk.", "Export Texture", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    DiagnosticLog.Write($"Export texture copy BNTX: source={sourcePath}, out={outPath}");
                    File.Copy(sourcePath, outPath, overwrite: true);
                }
                else
                {
                    using var bmp = texture.LoadPreviewBitmap();
                    if (bmp == null)
                    {
                        DiagnosticLog.Write($"Export texture failed: decode returned null for {texture.SourceFile}");
                        MessageBox.Show(this, "Texture could not be decoded.", "Export Texture", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    DiagnosticLog.Write($"Export texture save PNG: size={bmp.Width}x{bmp.Height}, pixelFormat={bmp.PixelFormat}");
                    bmp.Save(outPath, System.Drawing.Imaging.ImageFormat.Png);
                }

                MessageBox.Show(this, $"Exported:\n{outPath}", "Export Texture", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                DiagnosticLog.WriteException("Export texture failed", ex);
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
            ClearModelDetails();
            PopulateMaterialDetailsGrid(currentMaterialsModel, mat);
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

        private void AddModelInputToScene(string filePath)
        {
            if (string.Equals(Path.GetExtension(filePath), ".ccdata", StringComparison.OrdinalIgnoreCase))
            {
                var references = ParseCcDataReferences(filePath);
                if (string.IsNullOrWhiteSpace(references.ModelPath))
                {
                    throw new InvalidDataException("This CCData file does not reference a .trmdl model.");
                }

                DiagnosticLog.Section("Load CCData");
                DiagnosticLog.Write($"CCData path: {filePath}");
                foreach (var resource in references.ResourcePaths)
                {
                    DiagnosticLog.Write($"CCData resource: {resource} ({DescribeLocalFile(resource)})");
                }

                AddModelToScene(references.ModelPath, filePath, references.AnimationDirectories, references.ResourcePaths);
                return;
            }

            AddModelToScene(filePath, filePath);
        }

        private static bool IsSupportedModelInput(string path)
        {
            var ext = Path.GetExtension(path);
            return string.Equals(ext, ".trmdl", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(ext, ".ccdata", StringComparison.OrdinalIgnoreCase);
        }

        private static CcDataReferences ParseCcDataReferences(string ccdataPath)
        {
            var result = new CcDataReferences();
            var dir = Path.GetDirectoryName(Path.GetFullPath(ccdataPath)) ?? Directory.GetCurrentDirectory();
            var text = Encoding.UTF8.GetString(File.ReadAllBytes(ccdataPath));
            foreach (Match match in Regex.Matches(
                text,
                @"(?:[A-Za-z0-9_.\-]+[/\\])*[A-Za-z0-9_.\-]+\.(?:trmdl|tracn|tracs|tracl|tracr|tracp|tracm|tranm|traef|trmtr|trmsh|trmbf|trskl|tralk|trbik|trslp|trmmt|trmdd|trmdt|trspn|trslt|trssp|trmae|trpokecfg|bntx|ptcl|hkx|bin)",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
            {
                var raw = match.Value;
                var full = ResolveCcDataPath(dir, raw);
                if (!result.ResourcePaths.Any(path => string.Equals(path, full, StringComparison.OrdinalIgnoreCase)))
                {
                    result.ResourcePaths.Add(full);
                }

                if (string.Equals(Path.GetExtension(full), ".trmdl", StringComparison.OrdinalIgnoreCase) &&
                    string.IsNullOrWhiteSpace(result.ModelPath))
                {
                    result.ModelPath = full;
                }

                if (IsAnimationResource(full))
                {
                    var animDir = Path.GetDirectoryName(full);
                    if (!string.IsNullOrWhiteSpace(animDir) &&
                        !result.AnimationDirectories.Any(path => string.Equals(path, animDir, StringComparison.OrdinalIgnoreCase)))
                    {
                        result.AnimationDirectories.Add(animDir);
                    }
                }
            }

            return result;
        }

        private static bool IsAnimationResource(string path)
        {
            var ext = Path.GetExtension(path);
            return string.Equals(ext, ".tracn", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(ext, ".tracs", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(ext, ".tracl", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(ext, ".tracr", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(ext, ".tracp", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(ext, ".tracm", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(ext, ".tranm", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(ext, ".gfbanm", StringComparison.OrdinalIgnoreCase);
        }

        private static string ResolveCcDataPath(string ccdataDir, string rawPath)
        {
            var normalized = rawPath.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
            try
            {
                return Path.GetFullPath(Path.Combine(ccdataDir, normalized));
            }
            catch
            {
                return Path.Combine(ccdataDir, normalized);
            }
        }

        private static string DescribeLocalFile(string path)
        {
            try
            {
                var info = new FileInfo(path);
                return info.Exists ? $"exists bytes={info.Length}" : "missing";
            }
            catch (Exception ex)
            {
                return $"unavailable {ex.Message}";
            }
        }

        private void AddModelToScene(
            string filePath,
            string? originalInputPath = null,
            IReadOnlyList<string>? animationDirectories = null,
            IReadOnlyList<string>? indexedResources = null)
        {
            if (!renderCtrl.RendererInitialized)
            {
                throw new InvalidOperationException("Renderer is not initialized. Check the startup error message before loading a model.");
            }

            DiagnosticLog.Section("Add model to scene");
            DiagnosticLog.Write($"Model load requested: {filePath}");
            var mdl = renderCtrl.renderer.AddSceneModel(filePath, settings.LoadAllLods);
            modelPackageContexts[mdl] = new ModelPackageContext
            {
                OriginalInputPath = string.IsNullOrWhiteSpace(originalInputPath) ? null : Path.GetFullPath(originalInputPath),
                IndexedResources = (indexedResources ?? Array.Empty<string>())
                    .Where(path => !string.IsNullOrWhiteSpace(path))
                    .Select(Path.GetFullPath)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray()
            };
            DiagnosticLog.Write($"Model load completed: name={mdl.Name}, materials={mdl.GetMaterials().Count}, submeshes={mdl.GetSubmeshNames().Count}, armatureBones={mdl.GetArmature()?.Bones.Count ?? 0}");
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
            PopulateSceneTextures(mdl);
            TryAutoLoadAnimations(filePath, animationDirectories);

            // Default to "solo" display for the most recently added model.
            ShowOnlyModel(mdl);
            sceneTree.SelectedNode = node;
            node.EnsureVisible();

            settings.LastModelPath = originalInputPath ?? filePath;
            settings.Save();
            UpdateLastModelMenu();
        }

        private void TryAutoLoadAnimations(string trmdlPath, IReadOnlyList<string>? animationDirectories = null)
        {
            string? animDir = GuessAnimationDirectory(trmdlPath);
            var explicitAnimationDirs = (animationDirectories ?? Array.Empty<string>())
                .Where(dir => !string.IsNullOrWhiteSpace(dir) && Directory.Exists(dir))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (explicitAnimationDirs.Count > 0)
            {
                foreach (var explicitAnimationDir in explicitAnimationDirs)
                {
                    DiagnosticLog.Write($"CCData animation directory: {explicitAnimationDir}");
                    LoadAnimationsFromDirectory(explicitAnimationDir);
                }

                TryAutoPlayDefaultPoseAnimation(explicitAnimationDirs[0]);
                return;
            }

            if (!settings.AutoLoadAnimations)
            {
                DiagnosticLog.Write("Auto animation load: disabled");
            }
            else if (string.IsNullOrWhiteSpace(animDir) || !Directory.Exists(animDir))
            {
                DiagnosticLog.Write($"Auto animation load: no directory found for {trmdlPath}, guessed={animDir ?? "<none>"}");
                if (MessageHandler.Instance.DebugLogsEnabled)
                {
                    MessageHandler.Instance.AddMessage(MessageType.LOG, $"[Anim] AutoLoad: no animation directory found for '{trmdlPath}'");
                }
            }
            else
            {
                LoadAnimationsFromDirectory(animDir);
            }

            TryAutoPlayDefaultPoseAnimation(animDir);
        }

        private void LoadAnimationsFromDirectory(string animDir)
        {
            const int maxToLoad = 500;
            DiagnosticLog.Section("Auto animation load");
            DiagnosticLog.Write($"Animation directory: {animDir}");

            IEnumerable<string> tranm = Enumerable.Empty<string>();
            IEnumerable<string> gfbanm = Enumerable.Empty<string>();

            try
            {
                tranm = Directory.EnumerateFiles(animDir, "*.tranm", SearchOption.TopDirectoryOnly);
                gfbanm = Directory.EnumerateFiles(animDir, "*.gfbanm", SearchOption.TopDirectoryOnly);
            }
            catch (Exception ex)
            {
                DiagnosticLog.WriteException($"Auto animation enumeration failed: {animDir}", ex);
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
                try
                {
                    _ = LoadAnimationFile(file, "Auto", out var loadedNew);
                    if (loadedNew)
                    {
                        loaded++;
                    }
                }
                catch (Exception ex)
                {
                    DiagnosticLog.WriteException($"Auto animation load failed: {file}", ex);
                    if (MessageHandler.Instance.DebugLogsEnabled)
                    {
                        MessageHandler.Instance.AddMessage(MessageType.WARNING, $"[Anim] AutoLoad: failed '{file}': {ex.Message}");
                    }
                }
            }

            animationsList.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
            DiagnosticLog.Write($"Auto animation load complete: loaded={loaded}, scanned={files.Count}");
            if (MessageHandler.Instance.DebugLogsEnabled)
            {
                MessageHandler.Instance.AddMessage(MessageType.LOG, $"[Anim] AutoLoad: loaded {loaded} animations from '{animDir}'");
            }
        }

        private void TryAutoPlayDefaultPoseAnimation(string? animDir)
        {
            if (string.IsNullOrWhiteSpace(animDir) || !Directory.Exists(animDir))
            {
                DiagnosticLog.Write($"Auto default pose: no animation directory, guessed={animDir ?? "<none>"}");
                return;
            }

            string? file = FindDefaultPoseAnimation(animDir);
            if (string.IsNullOrWhiteSpace(file))
            {
                DiagnosticLog.Write($"Auto default pose: no default wait animation found in {animDir}");
                return;
            }

            try
            {
                var anim = LoadAnimationFile(file, "Auto default pose", out var loadedNew);
                if (anim == null)
                {
                    return;
                }

                if (loadedNew)
                {
                    animationsList.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
                }

                SelectAnimation(anim);
                if (renderCtrl.RendererInitialized)
                {
                    DiagnosticLog.Write($"Auto default pose play: {file}");
                    PlayAnimationWithFacialLayers(anim);
                }
            }
            catch (Exception ex)
            {
                DiagnosticLog.WriteException($"Auto default pose failed: {file}", ex);
                if (MessageHandler.Instance.DebugLogsEnabled)
                {
                    MessageHandler.Instance.AddMessage(MessageType.WARNING, $"[Anim] Auto default pose failed '{file}': {ex.Message}");
                }
            }
        }

        private static string? FindDefaultPoseAnimation(string animDir)
        {
            try
            {
                var files = Directory.EnumerateFiles(animDir, "*.tranm", SearchOption.TopDirectoryOnly).ToList();
                var controllerPreferred = TryFindControllerPreferredDefaultPose(animDir, files);
                if (!string.IsNullOrWhiteSpace(controllerPreferred))
                {
                    return controllerPreferred;
                }

                return files
                    .Select(file => new { File = file, Score = ScoreDefaultPoseAnimation(file) })
                    .Where(x => x.Score > 0)
                    .OrderByDescending(x => x.Score)
                    .ThenBy(x => Path.GetFileName(x.File), StringComparer.OrdinalIgnoreCase)
                    .Select(x => x.File)
                    .FirstOrDefault();
            }
            catch
            {
                return null;
            }
        }

        private static string? TryFindControllerPreferredDefaultPose(string animDir, IReadOnlyList<string> animationFiles)
        {
            if (animationFiles.Count == 0)
            {
                return null;
            }

            var candidates = animationFiles
                .Select(file => new
                {
                    File = file,
                    Name = Path.GetFileNameWithoutExtension(file)
                })
                .ToList();

            var stateClips = new List<(string State, string Clip, string Controller)>();
            foreach (var controller in Directory.EnumerateFiles(animDir, "*.tracs", SearchOption.TopDirectoryOnly))
            {
                try
                {
                    string text = Encoding.ASCII.GetString(File.ReadAllBytes(controller));
                    foreach (Match match in Regex.Matches(
                        text,
                        @"Top/poke_default/(?<state>ground_state|water_state|sky_state)/move/move_base/(?<clip>[0-9]{5}_[A-Za-z0-9_]*defaultwait[A-Za-z0-9_]*)",
                        RegexOptions.CultureInvariant))
                    {
                        string state = match.Groups["state"].Value;
                        string clip = match.Groups["clip"].Value;
                        if (!string.IsNullOrWhiteSpace(state) && !string.IsNullOrWhiteSpace(clip) &&
                            !stateClips.Any(x => string.Equals(x.State, state, StringComparison.OrdinalIgnoreCase) &&
                                                 string.Equals(x.Clip, clip, StringComparison.OrdinalIgnoreCase)))
                        {
                            stateClips.Add((state, clip, Path.GetFileName(controller)));
                        }
                    }
                }
                catch (Exception ex)
                {
                    DiagnosticLog.WriteException($"Auto default pose controller read failed: {controller}", ex);
                }
            }

            if (stateClips.Count == 0)
            {
                return null;
            }

            // For Switch Pokemon action controllers, non-ground locomotion states often carry the in-game presentation pose.
            // Fall back to ground if the controller only exposes the ordinary 00000 set.
            string[] statePreference = { "water_state", "sky_state", "ground_state" };
            foreach (string state in statePreference)
            {
                foreach (var entry in stateClips.Where(x => string.Equals(x.State, state, StringComparison.OrdinalIgnoreCase)))
                {
                    var match = candidates.FirstOrDefault(x =>
                        string.Equals(x.Name, entry.Clip, StringComparison.OrdinalIgnoreCase) ||
                        x.Name.EndsWith("_" + entry.Clip, StringComparison.OrdinalIgnoreCase));
                    if (match != null)
                    {
                        DiagnosticLog.Write($"Auto default pose controller pick: state={entry.State}, clip={entry.Clip}, controller={entry.Controller}, file={match.File}");
                        return match.File;
                    }
                }
            }

            return null;
        }

        private static int ScoreDefaultPoseAnimation(string file)
        {
            string name = Path.GetFileNameWithoutExtension(file).ToLowerInvariant();
            int score = 0;
            if (name.Contains("defaultwait")) score += 1000;
            if (name.Contains("battlewait")) score += 800;
            if (name.Contains("wait")) score += 400;
            if (name.Contains("loop")) score += 100;
            if (name.Contains("_00000_")) score += 40;
            if (name.Contains("_00001_")) score += 20;
            if (name.EndsWith("_loop", StringComparison.Ordinal)) score += 10;
            return score;
        }

        private void SelectAnimation(GFTool.Renderer.Scene.GraphicsObjects.Animation anim)
        {
            foreach (ListViewItem item in animationsList.Items)
            {
                item.Selected = ReferenceEquals(item.Tag, anim);
                if (item.Selected)
                {
                    item.EnsureVisible();
                }
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

            if (Directory.EnumerateFiles(dir, "*.tranm", SearchOption.TopDirectoryOnly).Any() ||
                Directory.EnumerateFiles(dir, "*.gfbanm", SearchOption.TopDirectoryOnly).Any())
            {
                return dir;
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
            ShowDetailsForNode(e.Node);
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
                .Where(IsSupportedModelInput)
                .ToList();

            if (modelFiles.Count == 0)
            {
                return;
            }

            ClearAll();
            foreach (var modelFile in modelFiles)
            {
                AddModelInputToScene(modelFile);
            }
        }
    }
}
