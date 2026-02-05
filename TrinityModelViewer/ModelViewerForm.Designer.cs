namespace TrinityModelViewer
{
    partial class ModelViewerForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            menuStrip1 = new MenuStrip();
            fileToolStripMenuItem = new ToolStripMenuItem();
            openToolStripMenuItem = new ToolStripMenuItem();
            importToolStripMenuItem = new ToolStripMenuItem();
            viewToolStripMenuItem = new ToolStripMenuItem();
            wireframeToolStripMenuItem = new ToolStripMenuItem();
            showSkeletonToolStripMenuItem = new ToolStripMenuItem();
            useRareTrmtrMaterialsToolStripMenuItem = new ToolStripMenuItem();
            enableNormalMapsToolStripMenuItem = new ToolStripMenuItem();
            enableAOToolStripMenuItem = new ToolStripMenuItem();
            useVertexColorsToolStripMenuItem = new ToolStripMenuItem();
            toolStripSeparator1 = new ToolStripSeparator();
            shadingToolStripMenuItem = new ToolStripMenuItem();
            shadingLitToolStripMenuItem = new ToolStripMenuItem();
            shadingToonToolStripMenuItem = new ToolStripMenuItem();
            shadingLegacyToolStripMenuItem = new ToolStripMenuItem();
            displayToolStripMenuItem = new ToolStripMenuItem();
            displayAllToolStripMenuItem = new ToolStripMenuItem();
            displayAlbedoToolStripMenuItem = new ToolStripMenuItem();
            displayNormalToolStripMenuItem = new ToolStripMenuItem();
            displaySpecularToolStripMenuItem = new ToolStripMenuItem();
            displayAOToolStripMenuItem = new ToolStripMenuItem();
            displayDepthToolStripMenuItem = new ToolStripMenuItem();
            shaderDebugToolStripMenuItem = new ToolStripMenuItem();
            shaderDebugOffToolStripMenuItem = new ToolStripMenuItem();
            shaderDebugIepTextureToolStripMenuItem = new ToolStripMenuItem();
            shaderDebugIepLayerMaskToolStripMenuItem = new ToolStripMenuItem();
            shaderDebugIepUvToolStripMenuItem = new ToolStripMenuItem();
            shaderDebugIepUv01ToolStripMenuItem = new ToolStripMenuItem();
            shaderDebugIkLayerMask1ToolStripMenuItem = new ToolStripMenuItem();
            shaderDebugIkLayerMask2ToolStripMenuItem = new ToolStripMenuItem();
            shaderDebugIkLayerMask3ToolStripMenuItem = new ToolStripMenuItem();
            shaderDebugIkLayerMask4ToolStripMenuItem = new ToolStripMenuItem();
            messageListView = new ListView();
            columnHeader1 = new ColumnHeader();
            statusLbl = new Label();
            loadingProgressBar = new ProgressBar();
            panel1 = new Panel();
            splitContainer1 = new SplitContainer();
            leftTabs = new TabControl();
            sceneTabPage = new TabPage();
            sceneTree = new TreeView();
            animationsTabPage = new TabPage();
            animationsList = new ListView();
            animationsPlayerPanel = new Panel();
            playAnimationButton = new Button();
            pauseAnimationButton = new Button();
            stopAnimationButton = new Button();
            loopAnimationCheckBox = new CheckBox();
            animationScrubBar = new TrackBar();
            animationsToolbar = new Panel();
            loadAnimationButton = new Button();
            exportModelWithAnimationsButton = new Button();
            modelProperties = new TabControl();
            tabPage1 = new TabPage();
            groupBox1 = new GroupBox();
            label1 = new Label();
            numericScaleZ = new NumericUpDown();
            numericTransX = new NumericUpDown();
            numericScaleY = new NumericUpDown();
            numericTransY = new NumericUpDown();
            numericScaleX = new NumericUpDown();
            numericTransZ = new NumericUpDown();
            label3 = new Label();
            label2 = new Label();
            numericRotZ = new NumericUpDown();
            numericRotX = new NumericUpDown();
            numericRotY = new NumericUpDown();
            tabPage2 = new TabPage();
            materialSplitContainer = new SplitContainer();
            materialList = new ListView();
            materialColumnHeader = new ColumnHeader();
            materialTabs = new TabControl();
            materialTexturesTab = new TabPage();
            materialTexturesSplit = new SplitContainer();
            materialTexturesGrid = new DataGridView();
            materialTexturePreviewPanel = new Panel();
            materialTexturePreview = new PictureBox();
            materialTexturePreviewHeaderPanel = new Panel();
            materialTextureChannelCombo = new ComboBox();
            materialTextureReplaceChannelButton = new Button();
            materialParamsTab = new TabPage();
            materialParamsGrid = new DataGridView();
            materialUvTab = new TabPage();
            materialUvSplit = new SplitContainer();
            materialUvGrid = new DataGridView();
            materialUvPreviewPanel = new Panel();
            materialUvPreview = new PictureBox();
            materialUvWrapModeCombo = new ComboBox();
            materialUvSetCombo = new ComboBox();
            materialSamplersTab = new TabPage();
            materialSamplersGrid = new DataGridView();
            splitContainerMain = new SplitContainer();
            splitContainerLeft = new SplitContainer();
            renderCtrl = new GFTool.RenderControl_WinForms.RenderControl();
            sceneTreeCtxtMenu = new ContextMenuStrip(components);
            exportToolStripMenuItem = new ToolStripMenuItem();
            deleteToolStripMenuItem = new ToolStripMenuItem();
            toggleModelVisibilityToolStripMenuItem = new ToolStripMenuItem();
            menuStrip1.SuspendLayout();
            panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            leftTabs.SuspendLayout();
            sceneTabPage.SuspendLayout();
            animationsTabPage.SuspendLayout();
            animationsPlayerPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)animationScrubBar).BeginInit();
            animationsToolbar.SuspendLayout();
            modelProperties.SuspendLayout();
            tabPage1.SuspendLayout();
            groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numericScaleZ).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numericTransX).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numericScaleY).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numericTransY).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numericScaleX).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numericTransZ).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numericRotZ).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numericRotX).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numericRotY).BeginInit();
            tabPage2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)materialSplitContainer).BeginInit();
            materialSplitContainer.Panel1.SuspendLayout();
            materialSplitContainer.Panel2.SuspendLayout();
            materialSplitContainer.SuspendLayout();
            materialTabs.SuspendLayout();
            materialTexturesTab.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)materialTexturesSplit).BeginInit();
            materialTexturesSplit.Panel1.SuspendLayout();
            materialTexturesSplit.Panel2.SuspendLayout();
            materialTexturesSplit.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)materialTexturesGrid).BeginInit();
            materialTexturePreviewPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)materialTexturePreview).BeginInit();
            materialTexturePreviewHeaderPanel.SuspendLayout();
            materialParamsTab.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)materialParamsGrid).BeginInit();
            materialUvTab.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)materialUvSplit).BeginInit();
            materialUvSplit.Panel1.SuspendLayout();
            materialUvSplit.Panel2.SuspendLayout();
            materialUvSplit.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)materialUvGrid).BeginInit();
            materialUvPreviewPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)materialUvPreview).BeginInit();
            materialSamplersTab.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)materialSamplersGrid).BeginInit();
            ((System.ComponentModel.ISupportInitialize)splitContainerMain).BeginInit();
            splitContainerMain.Panel1.SuspendLayout();
            splitContainerMain.Panel2.SuspendLayout();
            splitContainerMain.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainerLeft).BeginInit();
            splitContainerLeft.Panel1.SuspendLayout();
            splitContainerLeft.Panel2.SuspendLayout();
            splitContainerLeft.SuspendLayout();
            sceneTreeCtxtMenu.SuspendLayout();
            SuspendLayout();
            // 
            // menuStrip1
            // 
            menuStrip1.Items.AddRange(new ToolStripItem[] { fileToolStripMenuItem, viewToolStripMenuItem });
            menuStrip1.Location = new Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Size = new Size(1474, 24);
            menuStrip1.TabIndex = 1;
            menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            fileToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { openToolStripMenuItem, importToolStripMenuItem });
            fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            fileToolStripMenuItem.Size = new Size(37, 20);
            fileToolStripMenuItem.Text = "File";
            // 
            // openToolStripMenuItem
            // 
            openToolStripMenuItem.Name = "openToolStripMenuItem";
            openToolStripMenuItem.Size = new Size(110, 22);
            openToolStripMenuItem.Text = "Open";
            openToolStripMenuItem.Click += openToolStripMenuItem_Click;
            // 
            // importToolStripMenuItem
            // 
            importToolStripMenuItem.Name = "importToolStripMenuItem";
            importToolStripMenuItem.Size = new Size(110, 22);
            importToolStripMenuItem.Text = "Import";
            importToolStripMenuItem.Click += importToolStripMenuItem_Click;
            // 
            // viewToolStripMenuItem
            // 
            viewToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { wireframeToolStripMenuItem, showSkeletonToolStripMenuItem, useRareTrmtrMaterialsToolStripMenuItem, enableNormalMapsToolStripMenuItem, enableAOToolStripMenuItem, useVertexColorsToolStripMenuItem, toolStripSeparator1, shadingToolStripMenuItem, displayToolStripMenuItem, shaderDebugToolStripMenuItem });
            viewToolStripMenuItem.Name = "viewToolStripMenuItem";
            viewToolStripMenuItem.Size = new Size(44, 20);
            viewToolStripMenuItem.Text = "View";
            // 
            // wireframeToolStripMenuItem
            // 
            wireframeToolStripMenuItem.CheckOnClick = true;
            wireframeToolStripMenuItem.Name = "wireframeToolStripMenuItem";
            wireframeToolStripMenuItem.Size = new Size(210, 22);
            wireframeToolStripMenuItem.Text = "Wireframe";
            wireframeToolStripMenuItem.Click += wireframeToolStripMenuItem_Click;
            // 
            // showSkeletonToolStripMenuItem
            // 
            showSkeletonToolStripMenuItem.CheckOnClick = true;
            showSkeletonToolStripMenuItem.Name = "showSkeletonToolStripMenuItem";
            showSkeletonToolStripMenuItem.Size = new Size(210, 22);
            showSkeletonToolStripMenuItem.Text = "Show Skeleton";
            showSkeletonToolStripMenuItem.Click += showSkeletonToolStripMenuItem_Click;
            // 
            // useRareTrmtrMaterialsToolStripMenuItem
            // 
            useRareTrmtrMaterialsToolStripMenuItem.CheckOnClick = true;
            useRareTrmtrMaterialsToolStripMenuItem.Name = "useRareTrmtrMaterialsToolStripMenuItem";
            useRareTrmtrMaterialsToolStripMenuItem.Size = new Size(210, 22);
            useRareTrmtrMaterialsToolStripMenuItem.Text = "Use Rare TRMTR Materials";
            useRareTrmtrMaterialsToolStripMenuItem.Click += useRareTrmtrMaterialsToolStripMenuItem_Click;
            // 
            // enableNormalMapsToolStripMenuItem
            // 
            enableNormalMapsToolStripMenuItem.CheckOnClick = true;
            enableNormalMapsToolStripMenuItem.Name = "enableNormalMapsToolStripMenuItem";
            enableNormalMapsToolStripMenuItem.Size = new Size(210, 22);
            enableNormalMapsToolStripMenuItem.Text = "Enable Normal Maps";
            enableNormalMapsToolStripMenuItem.Click += enableNormalMapsToolStripMenuItem_Click;
            // 
            // enableAOToolStripMenuItem
            // 
            enableAOToolStripMenuItem.CheckOnClick = true;
            enableAOToolStripMenuItem.Name = "enableAOToolStripMenuItem";
            enableAOToolStripMenuItem.Size = new Size(210, 22);
            enableAOToolStripMenuItem.Text = "Enable AO";
            enableAOToolStripMenuItem.Click += enableAOToolStripMenuItem_Click;
            // 
            // useVertexColorsToolStripMenuItem
            // 
            useVertexColorsToolStripMenuItem.CheckOnClick = true;
            useVertexColorsToolStripMenuItem.Name = "useVertexColorsToolStripMenuItem";
            useVertexColorsToolStripMenuItem.Size = new Size(210, 22);
            useVertexColorsToolStripMenuItem.Text = "Use Vertex Colors";
            useVertexColorsToolStripMenuItem.Click += useVertexColorsToolStripMenuItem_Click;
            // 
            // toolStripSeparator1
            // 
            toolStripSeparator1.Name = "toolStripSeparator1";
            toolStripSeparator1.Size = new Size(207, 6);
            // 
            // shadingToolStripMenuItem
            // 
            shadingToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { shadingLitToolStripMenuItem, shadingToonToolStripMenuItem, shadingLegacyToolStripMenuItem });
            shadingToolStripMenuItem.Name = "shadingToolStripMenuItem";
            shadingToolStripMenuItem.Size = new Size(210, 22);
            shadingToolStripMenuItem.Text = "Shading";
            // 
            // shadingLitToolStripMenuItem
            // 
            shadingLitToolStripMenuItem.CheckOnClick = true;
            shadingLitToolStripMenuItem.Name = "shadingLitToolStripMenuItem";
            shadingLitToolStripMenuItem.Size = new Size(111, 22);
            shadingLitToolStripMenuItem.Text = "Lit";
            shadingLitToolStripMenuItem.Click += shadingLitToolStripMenuItem_Click;
            // 
            // shadingToonToolStripMenuItem
            // 
            shadingToonToolStripMenuItem.CheckOnClick = true;
            shadingToonToolStripMenuItem.Name = "shadingToonToolStripMenuItem";
            shadingToonToolStripMenuItem.Size = new Size(111, 22);
            shadingToonToolStripMenuItem.Text = "Toon";
            shadingToonToolStripMenuItem.Click += shadingToonToolStripMenuItem_Click;
            // 
            // shadingLegacyToolStripMenuItem
            // 
            shadingLegacyToolStripMenuItem.CheckOnClick = true;
            shadingLegacyToolStripMenuItem.Name = "shadingLegacyToolStripMenuItem";
            shadingLegacyToolStripMenuItem.Size = new Size(111, 22);
            shadingLegacyToolStripMenuItem.Text = "Legacy";
            shadingLegacyToolStripMenuItem.Click += shadingLegacyToolStripMenuItem_Click;
            // 
            // displayToolStripMenuItem
            // 
            displayToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { displayAllToolStripMenuItem, displayAlbedoToolStripMenuItem, displayNormalToolStripMenuItem, displaySpecularToolStripMenuItem, displayAOToolStripMenuItem, displayDepthToolStripMenuItem });
            displayToolStripMenuItem.Name = "displayToolStripMenuItem";
            displayToolStripMenuItem.Size = new Size(210, 22);
            displayToolStripMenuItem.Text = "Display";
            // 
            // displayAllToolStripMenuItem
            // 
            displayAllToolStripMenuItem.CheckOnClick = true;
            displayAllToolStripMenuItem.Name = "displayAllToolStripMenuItem";
            displayAllToolStripMenuItem.Size = new Size(119, 22);
            displayAllToolStripMenuItem.Text = "All";
            displayAllToolStripMenuItem.Click += displayAllToolStripMenuItem_Click;
            // 
            // displayAlbedoToolStripMenuItem
            // 
            displayAlbedoToolStripMenuItem.CheckOnClick = true;
            displayAlbedoToolStripMenuItem.Name = "displayAlbedoToolStripMenuItem";
            displayAlbedoToolStripMenuItem.Size = new Size(119, 22);
            displayAlbedoToolStripMenuItem.Text = "Albedo";
            displayAlbedoToolStripMenuItem.Click += displayAlbedoToolStripMenuItem_Click;
            // 
            // displayNormalToolStripMenuItem
            // 
            displayNormalToolStripMenuItem.CheckOnClick = true;
            displayNormalToolStripMenuItem.Name = "displayNormalToolStripMenuItem";
            displayNormalToolStripMenuItem.Size = new Size(119, 22);
            displayNormalToolStripMenuItem.Text = "Normal";
            displayNormalToolStripMenuItem.Click += displayNormalToolStripMenuItem_Click;
            // 
            // displaySpecularToolStripMenuItem
            // 
            displaySpecularToolStripMenuItem.CheckOnClick = true;
            displaySpecularToolStripMenuItem.Name = "displaySpecularToolStripMenuItem";
            displaySpecularToolStripMenuItem.Size = new Size(119, 22);
            displaySpecularToolStripMenuItem.Text = "Specular";
            displaySpecularToolStripMenuItem.Click += displaySpecularToolStripMenuItem_Click;
            // 
            // displayAOToolStripMenuItem
            // 
            displayAOToolStripMenuItem.CheckOnClick = true;
            displayAOToolStripMenuItem.Name = "displayAOToolStripMenuItem";
            displayAOToolStripMenuItem.Size = new Size(119, 22);
            displayAOToolStripMenuItem.Text = "AO";
            displayAOToolStripMenuItem.Click += displayAOToolStripMenuItem_Click;
            // 
            // displayDepthToolStripMenuItem
            // 
            displayDepthToolStripMenuItem.CheckOnClick = true;
            displayDepthToolStripMenuItem.Name = "displayDepthToolStripMenuItem";
            displayDepthToolStripMenuItem.Size = new Size(119, 22);
            displayDepthToolStripMenuItem.Text = "Depth";
            displayDepthToolStripMenuItem.Click += displayDepthToolStripMenuItem_Click;
            // 
            // shaderDebugToolStripMenuItem
            // 
            shaderDebugToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { shaderDebugOffToolStripMenuItem, shaderDebugIepTextureToolStripMenuItem, shaderDebugIepLayerMaskToolStripMenuItem, shaderDebugIepUvToolStripMenuItem, shaderDebugIepUv01ToolStripMenuItem, shaderDebugIkLayerMask1ToolStripMenuItem, shaderDebugIkLayerMask2ToolStripMenuItem, shaderDebugIkLayerMask3ToolStripMenuItem, shaderDebugIkLayerMask4ToolStripMenuItem });
            shaderDebugToolStripMenuItem.Name = "shaderDebugToolStripMenuItem";
            shaderDebugToolStripMenuItem.Size = new Size(210, 22);
            shaderDebugToolStripMenuItem.Text = "Shader Debug";
            // 
            // shaderDebugOffToolStripMenuItem
            // 
            shaderDebugOffToolStripMenuItem.CheckOnClick = true;
            shaderDebugOffToolStripMenuItem.Name = "shaderDebugOffToolStripMenuItem";
            shaderDebugOffToolStripMenuItem.Size = new Size(201, 22);
            shaderDebugOffToolStripMenuItem.Text = "Off";
            shaderDebugOffToolStripMenuItem.Click += shaderDebugOffToolStripMenuItem_Click;
            // 
            // shaderDebugIepTextureToolStripMenuItem
            // 
            shaderDebugIepTextureToolStripMenuItem.CheckOnClick = true;
            shaderDebugIepTextureToolStripMenuItem.Name = "shaderDebugIepTextureToolStripMenuItem";
            shaderDebugIepTextureToolStripMenuItem.Size = new Size(201, 22);
            shaderDebugIepTextureToolStripMenuItem.Text = "IEP: Texture";
            shaderDebugIepTextureToolStripMenuItem.Click += shaderDebugIepTextureToolStripMenuItem_Click;
            // 
            // shaderDebugIepLayerMaskToolStripMenuItem
            // 
            shaderDebugIepLayerMaskToolStripMenuItem.CheckOnClick = true;
            shaderDebugIepLayerMaskToolStripMenuItem.Name = "shaderDebugIepLayerMaskToolStripMenuItem";
            shaderDebugIepLayerMaskToolStripMenuItem.Size = new Size(201, 22);
            shaderDebugIepLayerMaskToolStripMenuItem.Text = "IEP: Layer Mask";
            shaderDebugIepLayerMaskToolStripMenuItem.Click += shaderDebugIepLayerMaskToolStripMenuItem_Click;
            // 
            // shaderDebugIepUvToolStripMenuItem
            // 
            shaderDebugIepUvToolStripMenuItem.CheckOnClick = true;
            shaderDebugIepUvToolStripMenuItem.Name = "shaderDebugIepUvToolStripMenuItem";
            shaderDebugIepUvToolStripMenuItem.Size = new Size(201, 22);
            shaderDebugIepUvToolStripMenuItem.Text = "IEP: UV";
            shaderDebugIepUvToolStripMenuItem.Click += shaderDebugIepUvToolStripMenuItem_Click;
            // 
            // shaderDebugIepUv01ToolStripMenuItem
            // 
            shaderDebugIepUv01ToolStripMenuItem.CheckOnClick = true;
            shaderDebugIepUv01ToolStripMenuItem.Name = "shaderDebugIepUv01ToolStripMenuItem";
            shaderDebugIepUv01ToolStripMenuItem.Size = new Size(201, 22);
            shaderDebugIepUv01ToolStripMenuItem.Text = "IEP: UV0/UV1";
            shaderDebugIepUv01ToolStripMenuItem.Click += shaderDebugIepUv01ToolStripMenuItem_Click;
            // 
            // shaderDebugIkLayerMask1ToolStripMenuItem
            // 
            shaderDebugIkLayerMask1ToolStripMenuItem.CheckOnClick = true;
            shaderDebugIkLayerMask1ToolStripMenuItem.Name = "shaderDebugIkLayerMask1ToolStripMenuItem";
            shaderDebugIkLayerMask1ToolStripMenuItem.Size = new Size(201, 22);
            shaderDebugIkLayerMask1ToolStripMenuItem.Text = "IkChar: Layer Mask 1 (R)";
            shaderDebugIkLayerMask1ToolStripMenuItem.Click += shaderDebugIkLayerMask1ToolStripMenuItem_Click;
            // 
            // shaderDebugIkLayerMask2ToolStripMenuItem
            // 
            shaderDebugIkLayerMask2ToolStripMenuItem.CheckOnClick = true;
            shaderDebugIkLayerMask2ToolStripMenuItem.Name = "shaderDebugIkLayerMask2ToolStripMenuItem";
            shaderDebugIkLayerMask2ToolStripMenuItem.Size = new Size(201, 22);
            shaderDebugIkLayerMask2ToolStripMenuItem.Text = "IkChar: Layer Mask 2 (G)";
            shaderDebugIkLayerMask2ToolStripMenuItem.Click += shaderDebugIkLayerMask2ToolStripMenuItem_Click;
            // 
            // shaderDebugIkLayerMask3ToolStripMenuItem
            // 
            shaderDebugIkLayerMask3ToolStripMenuItem.CheckOnClick = true;
            shaderDebugIkLayerMask3ToolStripMenuItem.Name = "shaderDebugIkLayerMask3ToolStripMenuItem";
            shaderDebugIkLayerMask3ToolStripMenuItem.Size = new Size(201, 22);
            shaderDebugIkLayerMask3ToolStripMenuItem.Text = "IkChar: Layer Mask 3 (B)";
            shaderDebugIkLayerMask3ToolStripMenuItem.Click += shaderDebugIkLayerMask3ToolStripMenuItem_Click;
            // 
            // shaderDebugIkLayerMask4ToolStripMenuItem
            // 
            shaderDebugIkLayerMask4ToolStripMenuItem.CheckOnClick = true;
            shaderDebugIkLayerMask4ToolStripMenuItem.Name = "shaderDebugIkLayerMask4ToolStripMenuItem";
            shaderDebugIkLayerMask4ToolStripMenuItem.Size = new Size(201, 22);
            shaderDebugIkLayerMask4ToolStripMenuItem.Text = "IkChar: Layer Mask 4 (A)";
            shaderDebugIkLayerMask4ToolStripMenuItem.Click += shaderDebugIkLayerMask4ToolStripMenuItem_Click;
            // 
            // messageListView
            // 
            messageListView.Columns.AddRange(new ColumnHeader[] { columnHeader1 });
            messageListView.Dock = DockStyle.Fill;
            messageListView.Location = new Point(0, 0);
            messageListView.Name = "messageListView";
            messageListView.Size = new Size(1098, 121);
            messageListView.TabIndex = 2;
            messageListView.UseCompatibleStateImageBehavior = false;
            messageListView.View = View.Details;
            // 
            // columnHeader1
            // 
            columnHeader1.Text = "Messages";
            // 
            // statusLbl
            // 
            statusLbl.AutoSize = true;
            statusLbl.Dock = DockStyle.Bottom;
            statusLbl.Location = new Point(0, 131);
            statusLbl.Name = "statusLbl";
            statusLbl.Size = new Size(38, 15);
            statusLbl.TabIndex = 3;
            statusLbl.Text = "label1";
            // 
            // loadingProgressBar
            // 
            loadingProgressBar.Dock = DockStyle.Bottom;
            loadingProgressBar.Location = new Point(0, 121);
            loadingProgressBar.Name = "loadingProgressBar";
            loadingProgressBar.Size = new Size(1098, 10);
            loadingProgressBar.Style = ProgressBarStyle.Continuous;
            loadingProgressBar.TabIndex = 4;
            loadingProgressBar.Visible = false;
            // 
            // panel1
            // 
            panel1.Controls.Add(splitContainer1);
            panel1.Dock = DockStyle.Fill;
            panel1.Location = new Point(0, 0);
            panel1.Name = "panel1";
            panel1.Size = new Size(372, 900);
            panel1.TabIndex = 4;
            // 
            // splitContainer1
            // 
            splitContainer1.Dock = DockStyle.Fill;
            splitContainer1.Location = new Point(0, 0);
            splitContainer1.Name = "splitContainer1";
            splitContainer1.Orientation = Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            splitContainer1.Panel1.Controls.Add(leftTabs);
            splitContainer1.Panel1.RightToLeft = RightToLeft.No;
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(modelProperties);
            splitContainer1.Panel2.RightToLeft = RightToLeft.No;
            splitContainer1.RightToLeft = RightToLeft.No;
            splitContainer1.Size = new Size(372, 900);
            splitContainer1.SplitterDistance = 433;
            splitContainer1.TabIndex = 0;
            // 
            // leftTabs
            // 
            leftTabs.Controls.Add(sceneTabPage);
            leftTabs.Controls.Add(animationsTabPage);
            leftTabs.Dock = DockStyle.Fill;
            leftTabs.Location = new Point(0, 0);
            leftTabs.Name = "leftTabs";
            leftTabs.SelectedIndex = 0;
            leftTabs.Size = new Size(372, 433);
            leftTabs.TabIndex = 0;
            // 
            // sceneTabPage
            // 
            sceneTabPage.Controls.Add(sceneTree);
            sceneTabPage.Location = new Point(4, 24);
            sceneTabPage.Name = "sceneTabPage";
            sceneTabPage.Padding = new Padding(3);
            sceneTabPage.Size = new Size(364, 405);
            sceneTabPage.TabIndex = 0;
            sceneTabPage.Text = "Scene";
            sceneTabPage.UseVisualStyleBackColor = true;
            // 
            // sceneTree
            // 
            sceneTree.Dock = DockStyle.Fill;
            sceneTree.Location = new Point(3, 3);
            sceneTree.Name = "sceneTree";
            sceneTree.Size = new Size(358, 399);
            sceneTree.TabIndex = 0;
            sceneTree.MouseUp += sceneTree_MouseUp;
            // 
            // animationsTabPage
            // 
            animationsTabPage.Controls.Add(animationsList);
            animationsTabPage.Controls.Add(animationsPlayerPanel);
            animationsTabPage.Controls.Add(animationsToolbar);
            animationsTabPage.Location = new Point(4, 24);
            animationsTabPage.Name = "animationsTabPage";
            animationsTabPage.Padding = new Padding(3);
            animationsTabPage.Size = new Size(364, 405);
            animationsTabPage.TabIndex = 1;
            animationsTabPage.Text = "Animations";
            animationsTabPage.UseVisualStyleBackColor = true;
            // 
            // animationsList
            // 
            animationsList.Dock = DockStyle.Fill;
            animationsList.Location = new Point(3, 63);
            animationsList.Name = "animationsList";
            animationsList.Size = new Size(358, 265);
            animationsList.TabIndex = 1;
            animationsList.UseCompatibleStateImageBehavior = false;
            // 
            // animationsPlayerPanel
            // 
            animationsPlayerPanel.Controls.Add(playAnimationButton);
            animationsPlayerPanel.Controls.Add(pauseAnimationButton);
            animationsPlayerPanel.Controls.Add(stopAnimationButton);
            animationsPlayerPanel.Controls.Add(loopAnimationCheckBox);
            animationsPlayerPanel.Controls.Add(animationScrubBar);
            animationsPlayerPanel.Dock = DockStyle.Bottom;
            animationsPlayerPanel.Location = new Point(3, 328);
            animationsPlayerPanel.Name = "animationsPlayerPanel";
            animationsPlayerPanel.Size = new Size(358, 74);
            animationsPlayerPanel.TabIndex = 2;
            // 
            // playAnimationButton
            // 
            playAnimationButton.Location = new Point(6, 6);
            playAnimationButton.Name = "playAnimationButton";
            playAnimationButton.Size = new Size(60, 24);
            playAnimationButton.TabIndex = 0;
            playAnimationButton.Text = "Play";
            playAnimationButton.UseVisualStyleBackColor = true;
            playAnimationButton.Click += playAnimationButton_Click;
            // 
            // pauseAnimationButton
            // 
            pauseAnimationButton.Location = new Point(72, 6);
            pauseAnimationButton.Name = "pauseAnimationButton";
            pauseAnimationButton.Size = new Size(60, 24);
            pauseAnimationButton.TabIndex = 1;
            pauseAnimationButton.Text = "Pause";
            pauseAnimationButton.UseVisualStyleBackColor = true;
            pauseAnimationButton.Click += pauseAnimationButton_Click;
            // 
            // stopAnimationButton
            // 
            stopAnimationButton.Location = new Point(138, 6);
            stopAnimationButton.Name = "stopAnimationButton";
            stopAnimationButton.Size = new Size(60, 24);
            stopAnimationButton.TabIndex = 2;
            stopAnimationButton.Text = "Stop";
            stopAnimationButton.UseVisualStyleBackColor = true;
            stopAnimationButton.Click += stopAnimationButton_Click;
            // 
            // loopAnimationCheckBox
            // 
            loopAnimationCheckBox.AutoSize = true;
            loopAnimationCheckBox.Location = new Point(216, 9);
            loopAnimationCheckBox.Name = "loopAnimationCheckBox";
            loopAnimationCheckBox.Size = new Size(53, 19);
            loopAnimationCheckBox.TabIndex = 3;
            loopAnimationCheckBox.Text = "Loop";
            loopAnimationCheckBox.UseVisualStyleBackColor = true;
            loopAnimationCheckBox.CheckedChanged += loopAnimationCheckBox_CheckedChanged;
            // 
            // animationScrubBar
            // 
            animationScrubBar.Location = new Point(0, 34);
            animationScrubBar.Name = "animationScrubBar";
            animationScrubBar.Size = new Size(344, 45);
            animationScrubBar.TabIndex = 4;
            animationScrubBar.TickStyle = TickStyle.None;
            // 
            // animationsToolbar
            // 
            animationsToolbar.Controls.Add(loadAnimationButton);
            animationsToolbar.Controls.Add(exportModelWithAnimationsButton);
            animationsToolbar.Dock = DockStyle.Top;
            animationsToolbar.Location = new Point(3, 3);
            animationsToolbar.Name = "animationsToolbar";
            animationsToolbar.Size = new Size(358, 60);
            animationsToolbar.TabIndex = 0;
            // 
            // loadAnimationButton
            // 
            loadAnimationButton.Location = new Point(6, 4);
            loadAnimationButton.Name = "loadAnimationButton";
            loadAnimationButton.Size = new Size(75, 24);
            loadAnimationButton.TabIndex = 0;
            loadAnimationButton.Text = "Load...";
            loadAnimationButton.UseVisualStyleBackColor = true;
            loadAnimationButton.Click += loadAnimationButton_Click;
            // 
            // exportModelWithAnimationsButton
            // 
            exportModelWithAnimationsButton.Location = new Point(6, 32);
            exportModelWithAnimationsButton.Name = "exportModelWithAnimationsButton";
            exportModelWithAnimationsButton.Size = new Size(318, 24);
            exportModelWithAnimationsButton.TabIndex = 4;
            exportModelWithAnimationsButton.Text = "Export Model + All Anims...";
            exportModelWithAnimationsButton.UseVisualStyleBackColor = true;
            exportModelWithAnimationsButton.Click += exportModelWithAnimationsButton_Click;
            // 
            // modelProperties
            // 
            modelProperties.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            modelProperties.Controls.Add(tabPage1);
            modelProperties.Controls.Add(tabPage2);
            modelProperties.Location = new Point(3, 3);
            modelProperties.Name = "modelProperties";
            modelProperties.SelectedIndex = 0;
            modelProperties.Size = new Size(366, 457);
            modelProperties.TabIndex = 0;
            // 
            // tabPage1
            // 
            tabPage1.Controls.Add(groupBox1);
            tabPage1.Location = new Point(4, 24);
            tabPage1.Name = "tabPage1";
            tabPage1.Padding = new Padding(3);
            tabPage1.Size = new Size(358, 429);
            tabPage1.TabIndex = 0;
            tabPage1.Text = "Object";
            tabPage1.UseVisualStyleBackColor = true;
            // 
            // groupBox1
            // 
            groupBox1.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            groupBox1.Controls.Add(label1);
            groupBox1.Controls.Add(numericScaleZ);
            groupBox1.Controls.Add(numericTransX);
            groupBox1.Controls.Add(numericScaleY);
            groupBox1.Controls.Add(numericTransY);
            groupBox1.Controls.Add(numericScaleX);
            groupBox1.Controls.Add(numericTransZ);
            groupBox1.Controls.Add(label3);
            groupBox1.Controls.Add(label2);
            groupBox1.Controls.Add(numericRotZ);
            groupBox1.Controls.Add(numericRotX);
            groupBox1.Controls.Add(numericRotY);
            groupBox1.Location = new Point(6, 6);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(338, 120);
            groupBox1.TabIndex = 12;
            groupBox1.TabStop = false;
            groupBox1.Text = "Transform";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(6, 28);
            label1.Name = "label1";
            label1.Size = new Size(64, 15);
            label1.TabIndex = 0;
            label1.Text = "Translation";
            // 
            // numericScaleZ
            // 
            numericScaleZ.Location = new Point(226, 84);
            numericScaleZ.Name = "numericScaleZ";
            numericScaleZ.Size = new Size(69, 23);
            numericScaleZ.TabIndex = 11;
            // 
            // numericTransX
            // 
            numericTransX.Location = new Point(76, 26);
            numericTransX.Name = "numericTransX";
            numericTransX.Size = new Size(69, 23);
            numericTransX.TabIndex = 1;
            // 
            // numericScaleY
            // 
            numericScaleY.Location = new Point(151, 84);
            numericScaleY.Name = "numericScaleY";
            numericScaleY.Size = new Size(69, 23);
            numericScaleY.TabIndex = 10;
            // 
            // numericTransY
            // 
            numericTransY.Location = new Point(151, 26);
            numericTransY.Name = "numericTransY";
            numericTransY.Size = new Size(69, 23);
            numericTransY.TabIndex = 2;
            // 
            // numericScaleX
            // 
            numericScaleX.Location = new Point(76, 84);
            numericScaleX.Name = "numericScaleX";
            numericScaleX.Size = new Size(69, 23);
            numericScaleX.TabIndex = 9;
            // 
            // numericTransZ
            // 
            numericTransZ.Location = new Point(226, 26);
            numericTransZ.Name = "numericTransZ";
            numericTransZ.Size = new Size(69, 23);
            numericTransZ.TabIndex = 3;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(36, 86);
            label3.Name = "label3";
            label3.Size = new Size(34, 15);
            label3.TabIndex = 8;
            label3.Text = "Scale";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(18, 57);
            label2.Name = "label2";
            label2.Size = new Size(52, 15);
            label2.TabIndex = 4;
            label2.Text = "Rotation";
            // 
            // numericRotZ
            // 
            numericRotZ.Location = new Point(226, 55);
            numericRotZ.Name = "numericRotZ";
            numericRotZ.Size = new Size(69, 23);
            numericRotZ.TabIndex = 7;
            // 
            // numericRotX
            // 
            numericRotX.Location = new Point(76, 55);
            numericRotX.Name = "numericRotX";
            numericRotX.Size = new Size(69, 23);
            numericRotX.TabIndex = 5;
            // 
            // numericRotY
            // 
            numericRotY.Location = new Point(151, 55);
            numericRotY.Name = "numericRotY";
            numericRotY.Size = new Size(69, 23);
            numericRotY.TabIndex = 6;
            // 
            // tabPage2
            // 
            tabPage2.Controls.Add(materialSplitContainer);
            tabPage2.Location = new Point(4, 24);
            tabPage2.Name = "tabPage2";
            tabPage2.Padding = new Padding(3);
            tabPage2.Size = new Size(358, 429);
            tabPage2.TabIndex = 1;
            tabPage2.Text = "Materials";
            tabPage2.UseVisualStyleBackColor = true;
            // 
            // materialSplitContainer
            // 
            materialSplitContainer.Dock = DockStyle.Fill;
            materialSplitContainer.Location = new Point(3, 3);
            materialSplitContainer.Name = "materialSplitContainer";
            // 
            // materialSplitContainer.Panel1
            // 
            materialSplitContainer.Panel1.Controls.Add(materialList);
            // 
            // materialSplitContainer.Panel2
            // 
            materialSplitContainer.Panel2.Controls.Add(materialTabs);
            materialSplitContainer.Size = new Size(352, 423);
            materialSplitContainer.SplitterDistance = 143;
            materialSplitContainer.TabIndex = 1;
            // 
            // materialList
            // 
            materialList.Columns.AddRange(new ColumnHeader[] { materialColumnHeader });
            materialList.Dock = DockStyle.Fill;
            materialList.FullRowSelect = true;
            materialList.Location = new Point(0, 0);
            materialList.Name = "materialList";
            materialList.Size = new Size(143, 423);
            materialList.TabIndex = 0;
            materialList.UseCompatibleStateImageBehavior = false;
            materialList.View = View.Details;
            // 
            // materialColumnHeader
            // 
            materialColumnHeader.Text = "Materials";
            // 
            // materialTabs
            // 
            materialTabs.Controls.Add(materialTexturesTab);
            materialTabs.Controls.Add(materialParamsTab);
            materialTabs.Controls.Add(materialUvTab);
            materialTabs.Controls.Add(materialSamplersTab);
            materialTabs.Dock = DockStyle.Fill;
            materialTabs.Location = new Point(0, 0);
            materialTabs.Name = "materialTabs";
            materialTabs.SelectedIndex = 0;
            materialTabs.Size = new Size(205, 423);
            materialTabs.TabIndex = 0;
            // 
            // materialTexturesTab
            // 
            materialTexturesTab.Controls.Add(materialTexturesSplit);
            materialTexturesTab.Location = new Point(4, 24);
            materialTexturesTab.Name = "materialTexturesTab";
            materialTexturesTab.Padding = new Padding(3);
            materialTexturesTab.Size = new Size(197, 395);
            materialTexturesTab.TabIndex = 0;
            materialTexturesTab.Text = "Textures";
            materialTexturesTab.UseVisualStyleBackColor = true;
            // 
            // materialTexturesSplit
            // 
            materialTexturesSplit.Dock = DockStyle.Fill;
            materialTexturesSplit.Location = new Point(3, 3);
            materialTexturesSplit.Name = "materialTexturesSplit";
            materialTexturesSplit.Orientation = Orientation.Horizontal;
            // 
            // materialTexturesSplit.Panel1
            // 
            materialTexturesSplit.Panel1.Controls.Add(materialTexturesGrid);
            // 
            // materialTexturesSplit.Panel2
            // 
            materialTexturesSplit.Panel2.Controls.Add(materialTexturePreviewPanel);
            materialTexturesSplit.Size = new Size(191, 389);
            materialTexturesSplit.SplitterDistance = 239;
            materialTexturesSplit.TabIndex = 0;
            // 
            // materialTexturesGrid
            // 
            materialTexturesGrid.AllowUserToAddRows = false;
            materialTexturesGrid.AllowUserToDeleteRows = false;
            materialTexturesGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            materialTexturesGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            materialTexturesGrid.Dock = DockStyle.Fill;
            materialTexturesGrid.Location = new Point(0, 0);
            materialTexturesGrid.MultiSelect = false;
            materialTexturesGrid.Name = "materialTexturesGrid";
            materialTexturesGrid.ReadOnly = true;
            materialTexturesGrid.RowHeadersVisible = false;
            materialTexturesGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            materialTexturesGrid.Size = new Size(191, 239);
            materialTexturesGrid.TabIndex = 0;
            // 
            // materialTexturePreviewPanel
            // 
            materialTexturePreviewPanel.Controls.Add(materialTexturePreview);
            materialTexturePreviewPanel.Controls.Add(materialTexturePreviewHeaderPanel);
            materialTexturePreviewPanel.Dock = DockStyle.Fill;
            materialTexturePreviewPanel.Location = new Point(0, 0);
            materialTexturePreviewPanel.Name = "materialTexturePreviewPanel";
            materialTexturePreviewPanel.Size = new Size(191, 146);
            materialTexturePreviewPanel.TabIndex = 1;
            // 
            // materialTexturePreview
            // 
            materialTexturePreview.Dock = DockStyle.Fill;
            materialTexturePreview.Location = new Point(0, 23);
            materialTexturePreview.Name = "materialTexturePreview";
            materialTexturePreview.Size = new Size(191, 123);
            materialTexturePreview.SizeMode = PictureBoxSizeMode.Zoom;
            materialTexturePreview.TabIndex = 1;
            materialTexturePreview.TabStop = false;
            // 
            // materialTexturePreviewHeaderPanel
            // 
            materialTexturePreviewHeaderPanel.Controls.Add(materialTextureChannelCombo);
            materialTexturePreviewHeaderPanel.Controls.Add(materialTextureReplaceChannelButton);
            materialTexturePreviewHeaderPanel.Dock = DockStyle.Top;
            materialTexturePreviewHeaderPanel.Location = new Point(0, 0);
            materialTexturePreviewHeaderPanel.Name = "materialTexturePreviewHeaderPanel";
            materialTexturePreviewHeaderPanel.Size = new Size(191, 23);
            materialTexturePreviewHeaderPanel.TabIndex = 0;
            // 
            // materialTextureChannelCombo
            // 
            materialTextureChannelCombo.Dock = DockStyle.Fill;
            materialTextureChannelCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            materialTextureChannelCombo.FormattingEnabled = true;
            materialTextureChannelCombo.Location = new Point(0, 0);
            materialTextureChannelCombo.Name = "materialTextureChannelCombo";
            materialTextureChannelCombo.Size = new Size(101, 23);
            materialTextureChannelCombo.TabIndex = 0;
            materialTextureChannelCombo.SelectedIndexChanged += materialTextureChannelCombo_SelectedIndexChanged;
            // 
            // materialTextureReplaceChannelButton
            // 
            materialTextureReplaceChannelButton.Dock = DockStyle.Right;
            materialTextureReplaceChannelButton.Location = new Point(101, 0);
            materialTextureReplaceChannelButton.Name = "materialTextureReplaceChannelButton";
            materialTextureReplaceChannelButton.Size = new Size(90, 23);
            materialTextureReplaceChannelButton.TabIndex = 1;
            materialTextureReplaceChannelButton.Text = "Replace";
            materialTextureReplaceChannelButton.UseVisualStyleBackColor = true;
            materialTextureReplaceChannelButton.Click += materialTextureReplaceChannelButton_Click;
            // 
            // materialParamsTab
            // 
            materialParamsTab.Controls.Add(materialParamsGrid);
            materialParamsTab.Location = new Point(4, 24);
            materialParamsTab.Name = "materialParamsTab";
            materialParamsTab.Padding = new Padding(3);
            materialParamsTab.Size = new Size(197, 395);
            materialParamsTab.TabIndex = 1;
            materialParamsTab.Text = "Params";
            materialParamsTab.UseVisualStyleBackColor = true;
            // 
            // materialParamsGrid
            // 
            materialParamsGrid.AllowUserToAddRows = false;
            materialParamsGrid.AllowUserToDeleteRows = false;
            materialParamsGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            materialParamsGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            materialParamsGrid.Dock = DockStyle.Fill;
            materialParamsGrid.Location = new Point(3, 3);
            materialParamsGrid.MultiSelect = false;
            materialParamsGrid.Name = "materialParamsGrid";
            materialParamsGrid.ReadOnly = true;
            materialParamsGrid.RowHeadersVisible = false;
            materialParamsGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            materialParamsGrid.Size = new Size(191, 389);
            materialParamsGrid.TabIndex = 0;
            // 
            // materialUvTab
            // 
            materialUvTab.Controls.Add(materialUvSplit);
            materialUvTab.Location = new Point(4, 24);
            materialUvTab.Name = "materialUvTab";
            materialUvTab.Padding = new Padding(3);
            materialUvTab.Size = new Size(197, 395);
            materialUvTab.TabIndex = 2;
            materialUvTab.Text = "UVs";
            materialUvTab.UseVisualStyleBackColor = true;
            // 
            // materialUvSplit
            // 
            materialUvSplit.Dock = DockStyle.Fill;
            materialUvSplit.Location = new Point(3, 3);
            materialUvSplit.Name = "materialUvSplit";
            materialUvSplit.Orientation = Orientation.Horizontal;
            // 
            // materialUvSplit.Panel1
            // 
            materialUvSplit.Panel1.Controls.Add(materialUvGrid);
            // 
            // materialUvSplit.Panel2
            // 
            materialUvSplit.Panel2.Controls.Add(materialUvPreviewPanel);
            materialUvSplit.Size = new Size(191, 389);
            materialUvSplit.SplitterDistance = 239;
            materialUvSplit.TabIndex = 0;
            // 
            // materialUvGrid
            // 
            materialUvGrid.AllowUserToAddRows = false;
            materialUvGrid.AllowUserToDeleteRows = false;
            materialUvGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            materialUvGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            materialUvGrid.Dock = DockStyle.Fill;
            materialUvGrid.Location = new Point(0, 0);
            materialUvGrid.MultiSelect = false;
            materialUvGrid.Name = "materialUvGrid";
            materialUvGrid.ReadOnly = true;
            materialUvGrid.RowHeadersVisible = false;
            materialUvGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            materialUvGrid.Size = new Size(191, 239);
            materialUvGrid.TabIndex = 0;
            // 
            // materialUvPreviewPanel
            // 
            materialUvPreviewPanel.Controls.Add(materialUvPreview);
            materialUvPreviewPanel.Controls.Add(materialUvWrapModeCombo);
            materialUvPreviewPanel.Controls.Add(materialUvSetCombo);
            materialUvPreviewPanel.Dock = DockStyle.Fill;
            materialUvPreviewPanel.Location = new Point(0, 0);
            materialUvPreviewPanel.Name = "materialUvPreviewPanel";
            materialUvPreviewPanel.Size = new Size(191, 146);
            materialUvPreviewPanel.TabIndex = 1;
            // 
            // materialUvPreview
            // 
            materialUvPreview.Dock = DockStyle.Fill;
            materialUvPreview.Location = new Point(0, 46);
            materialUvPreview.Name = "materialUvPreview";
            materialUvPreview.Size = new Size(191, 100);
            materialUvPreview.SizeMode = PictureBoxSizeMode.Zoom;
            materialUvPreview.TabIndex = 0;
            materialUvPreview.TabStop = false;
            // 
            // materialUvWrapModeCombo
            // 
            materialUvWrapModeCombo.Dock = DockStyle.Top;
            materialUvWrapModeCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            materialUvWrapModeCombo.FormattingEnabled = true;
            materialUvWrapModeCombo.Location = new Point(0, 23);
            materialUvWrapModeCombo.Name = "materialUvWrapModeCombo";
            materialUvWrapModeCombo.Size = new Size(191, 23);
            materialUvWrapModeCombo.TabIndex = 1;
            materialUvWrapModeCombo.SelectedIndexChanged += materialUvWrapModeCombo_SelectedIndexChanged;
            // 
            // materialUvSetCombo
            // 
            materialUvSetCombo.Dock = DockStyle.Top;
            materialUvSetCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            materialUvSetCombo.FormattingEnabled = true;
            materialUvSetCombo.Location = new Point(0, 0);
            materialUvSetCombo.Name = "materialUvSetCombo";
            materialUvSetCombo.Size = new Size(191, 23);
            materialUvSetCombo.TabIndex = 0;
            materialUvSetCombo.SelectedIndexChanged += materialUvSetCombo_SelectedIndexChanged;
            // 
            // materialSamplersTab
            // 
            materialSamplersTab.Controls.Add(materialSamplersGrid);
            materialSamplersTab.Location = new Point(4, 24);
            materialSamplersTab.Name = "materialSamplersTab";
            materialSamplersTab.Padding = new Padding(3);
            materialSamplersTab.Size = new Size(197, 395);
            materialSamplersTab.TabIndex = 3;
            materialSamplersTab.Text = "Samplers";
            materialSamplersTab.UseVisualStyleBackColor = true;
            // 
            // materialSamplersGrid
            // 
            materialSamplersGrid.AllowUserToAddRows = false;
            materialSamplersGrid.AllowUserToDeleteRows = false;
            materialSamplersGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            materialSamplersGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            materialSamplersGrid.Dock = DockStyle.Fill;
            materialSamplersGrid.Location = new Point(3, 3);
            materialSamplersGrid.MultiSelect = false;
            materialSamplersGrid.Name = "materialSamplersGrid";
            materialSamplersGrid.ReadOnly = true;
            materialSamplersGrid.RowHeadersVisible = false;
            materialSamplersGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            materialSamplersGrid.Size = new Size(191, 389);
            materialSamplersGrid.TabIndex = 0;
            // 
            // splitContainerMain
            // 
            splitContainerMain.Dock = DockStyle.Fill;
            splitContainerMain.Location = new Point(0, 24);
            splitContainerMain.Name = "splitContainerMain";
            // 
            // splitContainerMain.Panel1
            // 
            splitContainerMain.Panel1.Controls.Add(splitContainerLeft);
            // 
            // splitContainerMain.Panel2
            // 
            splitContainerMain.Panel2.Controls.Add(panel1);
            splitContainerMain.Size = new Size(1474, 900);
            splitContainerMain.SplitterDistance = 1098;
            splitContainerMain.TabIndex = 6;
            // 
            // splitContainerLeft
            // 
            splitContainerLeft.Dock = DockStyle.Fill;
            splitContainerLeft.Location = new Point(0, 0);
            splitContainerLeft.Name = "splitContainerLeft";
            splitContainerLeft.Orientation = Orientation.Horizontal;
            // 
            // splitContainerLeft.Panel1
            // 
            splitContainerLeft.Panel1.Controls.Add(renderCtrl);
            // 
            // splitContainerLeft.Panel2
            // 
            splitContainerLeft.Panel2.Controls.Add(messageListView);
            splitContainerLeft.Panel2.Controls.Add(loadingProgressBar);
            splitContainerLeft.Panel2.Controls.Add(statusLbl);
            splitContainerLeft.Size = new Size(1098, 900);
            splitContainerLeft.SplitterDistance = 750;
            splitContainerLeft.TabIndex = 0;
            // 
            // renderCtrl
            // 
            renderCtrl.API = OpenTK.Windowing.Common.ContextAPI.OpenGL;
            renderCtrl.APIVersion = new Version(3, 3, 0, 0);
            renderCtrl.Dock = DockStyle.Fill;
            renderCtrl.Flags = OpenTK.Windowing.Common.ContextFlags.Default;
            renderCtrl.IsEventDriven = true;
            renderCtrl.Location = new Point(0, 0);
            renderCtrl.Name = "renderCtrl";
            renderCtrl.Profile = OpenTK.Windowing.Common.ContextProfile.Core;
            renderCtrl.SharedContext = null;
            renderCtrl.Size = new Size(1098, 750);
            renderCtrl.TabIndex = 5;
            renderCtrl.Load += glCtxt_Load;
            renderCtrl.Paint += glCtxt_Paint;
            renderCtrl.KeyDown += glCtxt_KeyDown;
            renderCtrl.KeyUp += glCtxt_KeyUp;
            // 
            // sceneTreeCtxtMenu
            // 
            sceneTreeCtxtMenu.Items.AddRange(new ToolStripItem[] { exportToolStripMenuItem, deleteToolStripMenuItem, toggleModelVisibilityToolStripMenuItem });
            sceneTreeCtxtMenu.Name = "sceneTreeCtxtMenu";
            sceneTreeCtxtMenu.Size = new Size(122, 70);
            // 
            // exportToolStripMenuItem
            // 
            exportToolStripMenuItem.Name = "exportToolStripMenuItem";
            exportToolStripMenuItem.Size = new Size(121, 22);
            exportToolStripMenuItem.Text = "Modify...";
            exportToolStripMenuItem.Click += exportToolStripMenuItem_Click;
            // 
            // deleteToolStripMenuItem
            // 
            deleteToolStripMenuItem.Name = "deleteToolStripMenuItem";
            deleteToolStripMenuItem.Size = new Size(121, 22);
            deleteToolStripMenuItem.Text = "Delete";
            deleteToolStripMenuItem.Click += deleteToolStripMenuItem_Click;
            // 
            // toggleModelVisibilityToolStripMenuItem
            // 
            toggleModelVisibilityToolStripMenuItem.Name = "toggleModelVisibilityToolStripMenuItem";
            toggleModelVisibilityToolStripMenuItem.Size = new Size(121, 22);
            toggleModelVisibilityToolStripMenuItem.Text = "Hide";
            toggleModelVisibilityToolStripMenuItem.Click += toggleModelVisibilityToolStripMenuItem_Click;
            // 
            // ModelViewerForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1474, 924);
            Controls.Add(splitContainerMain);
            Controls.Add(menuStrip1);
            MainMenuStrip = menuStrip1;
            Name = "ModelViewerForm";
            Text = "Trinity Model Viewer";
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            panel1.ResumeLayout(false);
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            leftTabs.ResumeLayout(false);
            sceneTabPage.ResumeLayout(false);
            animationsTabPage.ResumeLayout(false);
            animationsPlayerPanel.ResumeLayout(false);
            animationsPlayerPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)animationScrubBar).EndInit();
            animationsToolbar.ResumeLayout(false);
            modelProperties.ResumeLayout(false);
            tabPage1.ResumeLayout(false);
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numericScaleZ).EndInit();
            ((System.ComponentModel.ISupportInitialize)numericTransX).EndInit();
            ((System.ComponentModel.ISupportInitialize)numericScaleY).EndInit();
            ((System.ComponentModel.ISupportInitialize)numericTransY).EndInit();
            ((System.ComponentModel.ISupportInitialize)numericScaleX).EndInit();
            ((System.ComponentModel.ISupportInitialize)numericTransZ).EndInit();
            ((System.ComponentModel.ISupportInitialize)numericRotZ).EndInit();
            ((System.ComponentModel.ISupportInitialize)numericRotX).EndInit();
            ((System.ComponentModel.ISupportInitialize)numericRotY).EndInit();
            tabPage2.ResumeLayout(false);
            materialSplitContainer.Panel1.ResumeLayout(false);
            materialSplitContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)materialSplitContainer).EndInit();
            materialSplitContainer.ResumeLayout(false);
            materialTabs.ResumeLayout(false);
            materialTexturesTab.ResumeLayout(false);
            materialTexturesSplit.Panel1.ResumeLayout(false);
            materialTexturesSplit.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)materialTexturesSplit).EndInit();
            materialTexturesSplit.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)materialTexturesGrid).EndInit();
            materialTexturePreviewPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)materialTexturePreview).EndInit();
            materialTexturePreviewHeaderPanel.ResumeLayout(false);
            materialParamsTab.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)materialParamsGrid).EndInit();
            materialUvTab.ResumeLayout(false);
            materialUvSplit.Panel1.ResumeLayout(false);
            materialUvSplit.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)materialUvSplit).EndInit();
            materialUvSplit.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)materialUvGrid).EndInit();
            materialUvPreviewPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)materialUvPreview).EndInit();
            materialSamplersTab.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)materialSamplersGrid).EndInit();
            splitContainerMain.Panel1.ResumeLayout(false);
            splitContainerMain.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainerMain).EndInit();
            splitContainerMain.ResumeLayout(false);
            splitContainerLeft.Panel1.ResumeLayout(false);
            splitContainerLeft.Panel2.ResumeLayout(false);
            splitContainerLeft.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainerLeft).EndInit();
            splitContainerLeft.ResumeLayout(false);
            sceneTreeCtxtMenu.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private MenuStrip menuStrip1;
        private ToolStripMenuItem fileToolStripMenuItem;
        private ToolStripMenuItem openToolStripMenuItem;
        private ListView messageListView;
        private ColumnHeader columnHeader1;
        private Label statusLbl;
        private ProgressBar loadingProgressBar;
        private Panel panel1;
        private SplitContainer splitContainer1;
        private SplitContainer splitContainerMain;
        private SplitContainer splitContainerLeft;
        private TabControl leftTabs;
        private TabPage sceneTabPage;
        private TabPage animationsTabPage;
        private TreeView sceneTree;
        private ListView animationsList;
        private Panel animationsToolbar;
        private Button loadAnimationButton;
        private Panel animationsPlayerPanel;
        private Button playAnimationButton;
        private Button pauseAnimationButton;
        private Button stopAnimationButton;
        private TrackBar animationScrubBar;
        private CheckBox loopAnimationCheckBox;
        private Button exportModelWithAnimationsButton;
        private TabControl modelProperties;
        private TabPage tabPage1;
        private TabPage tabPage2;
        private NumericUpDown numericTransZ;
        private NumericUpDown numericTransY;
        private NumericUpDown numericTransX;
        private Label label1;
        private NumericUpDown numericScaleZ;
        private NumericUpDown numericScaleY;
        private NumericUpDown numericScaleX;
        private Label label3;
        private NumericUpDown numericRotZ;
        private NumericUpDown numericRotY;
        private NumericUpDown numericRotX;
        private Label label2;
        private ToolStripMenuItem importToolStripMenuItem;
        private ToolStripMenuItem viewToolStripMenuItem;
        private ToolStripMenuItem wireframeToolStripMenuItem;
        private ListView materialList;
        private ColumnHeader materialColumnHeader;
        private SplitContainer materialSplitContainer;
        private TabControl materialTabs;
        private TabPage materialTexturesTab;
        private TabPage materialParamsTab;
        private TabPage materialUvTab;
        private TabPage materialSamplersTab;
        private SplitContainer materialTexturesSplit;
        private DataGridView materialTexturesGrid;
        private Panel materialTexturePreviewPanel;
        private Panel materialTexturePreviewHeaderPanel;
        private ComboBox materialTextureChannelCombo;
        private Button materialTextureReplaceChannelButton;
        private PictureBox materialTexturePreview;
        private SplitContainer materialUvSplit;
        private DataGridView materialParamsGrid;
        private DataGridView materialUvGrid;
        private Panel materialUvPreviewPanel;
        private ComboBox materialUvSetCombo;
        private ComboBox materialUvWrapModeCombo;
        private PictureBox materialUvPreview;
        private DataGridView materialSamplersGrid;
        private ToolStripSeparator toolStripSeparator1;
        private ToolStripMenuItem shadingToolStripMenuItem;
        private ToolStripMenuItem shadingLitToolStripMenuItem;
        private ToolStripMenuItem shadingToonToolStripMenuItem;
        private ToolStripMenuItem shadingLegacyToolStripMenuItem;
        private ToolStripMenuItem displayToolStripMenuItem;
        private ToolStripMenuItem displayAllToolStripMenuItem;
        private ToolStripMenuItem displayAlbedoToolStripMenuItem;
        private ToolStripMenuItem displayNormalToolStripMenuItem;
        private ToolStripMenuItem displaySpecularToolStripMenuItem;
        private ToolStripMenuItem displayAOToolStripMenuItem;
        private ToolStripMenuItem displayDepthToolStripMenuItem;
        private ToolStripMenuItem shaderDebugToolStripMenuItem;
        private ToolStripMenuItem shaderDebugOffToolStripMenuItem;
        private ToolStripMenuItem shaderDebugIepTextureToolStripMenuItem;
        private ToolStripMenuItem shaderDebugIepLayerMaskToolStripMenuItem;
        private ToolStripMenuItem shaderDebugIepUvToolStripMenuItem;
        private ToolStripMenuItem shaderDebugIepUv01ToolStripMenuItem;
        private ToolStripMenuItem shaderDebugIkLayerMask1ToolStripMenuItem;
        private ToolStripMenuItem shaderDebugIkLayerMask2ToolStripMenuItem;
        private ToolStripMenuItem shaderDebugIkLayerMask3ToolStripMenuItem;
        private ToolStripMenuItem shaderDebugIkLayerMask4ToolStripMenuItem;
        private ToolStripMenuItem showSkeletonToolStripMenuItem;
        private ToolStripMenuItem useRareTrmtrMaterialsToolStripMenuItem;
        private ToolStripMenuItem enableNormalMapsToolStripMenuItem;
        private ToolStripMenuItem enableAOToolStripMenuItem;
        private ToolStripMenuItem useVertexColorsToolStripMenuItem;
        private GroupBox groupBox1;
        private ContextMenuStrip sceneTreeCtxtMenu;
        private ToolStripMenuItem deleteToolStripMenuItem;
        private ToolStripMenuItem exportToolStripMenuItem;
        private ToolStripMenuItem toggleModelVisibilityToolStripMenuItem;
        private GFTool.RenderControl_WinForms.RenderControl renderCtrl;
    }
}
