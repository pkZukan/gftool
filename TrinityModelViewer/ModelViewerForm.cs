using GFTool.Renderer.Core;
using System;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace TrinityModelViewer
{
    public partial class ModelViewerForm : Form
    {
        public ModelViewerForm()
            : this(null)
        {
        }

        public ModelViewerForm(string[]? startupFiles)
        {
            InitializeComponent();
            materialTextureChannelCombo.Items.AddRange(new object[]
            {
                "RGBA",
                "R",
                "G",
                "B",
                "A",
                "RGB (ignore alpha)"
            });
            materialTextureChannelCombo.SelectedIndex = 0;
            UpdateReplaceChannelButtonState();
            materialUvSetCombo.Items.AddRange(new object[]
            {
                "UV0",
                "UV1"
            });
            materialUvSetCombo.SelectedIndex = 0;
            materialUvWrapModeCombo.Items.AddRange(new object[]
            {
                uvWrapAutoItem,
                "Wrap: Repeat",
                "Wrap: MirroredRepeat",
                "Wrap: Clamp"
            });
            materialUvWrapModeCombo.SelectedIndex = 0;
            settings = ViewerSettings.Load();
            this.startupFiles = startupFiles?.Where(s => !string.IsNullOrWhiteSpace(s)).ToArray() ?? Array.Empty<string>();
            MessageHandler.Instance.DebugLogsEnabled = settings.DebugLogs;
            ApplyRenderSettingsToMenu();
            ApplyTheme();
            AddSettingsMenu();
            AddToolsMenu();
            AddGfpakMenuItems();
            AddTrinityExportMenuItems();
            AddSkinningMenuItems();
            AddShaderDevMenuItems();
            AddLastModelMenu();
            AddRecentModelsMenu();
            InitializePerfHud();
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
            sceneTree.NodeMouseClick += sceneTree_NodeMouseClick;
            SetupAnimationsList();
            SetupJsonEditorTab();
        }
    }
}
