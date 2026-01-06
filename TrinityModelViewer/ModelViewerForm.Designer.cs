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
            toolStripSeparator1 = new ToolStripSeparator();
            shadingToolStripMenuItem = new ToolStripMenuItem();
            shadingLitToolStripMenuItem = new ToolStripMenuItem();
            shadingToonToolStripMenuItem = new ToolStripMenuItem();
            shadingLegacyToolStripMenuItem = new ToolStripMenuItem();
            showSkeletonToolStripMenuItem = new ToolStripMenuItem();
            messageListView = new ListView();
            columnHeader1 = new ColumnHeader();
            statusLbl = new Label();
            panel1 = new Panel();
            splitContainer1 = new SplitContainer();
            splitContainerMain = new SplitContainer();
            splitContainerLeft = new SplitContainer();
            leftTabs = new TabControl();
            sceneTabPage = new TabPage();
            animationsTabPage = new TabPage();
            sceneTree = new TreeView();
            animationsList = new ListView();
            animationsToolbar = new Panel();
            loadAnimationButton = new Button();
            playAnimationButton = new Button();
            stopAnimationButton = new Button();
            exportAnimationButton = new Button();
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
            materialList = new ListView();
            materialColumnHeader = new ColumnHeader();
            materialSplitContainer = new SplitContainer();
            materialTabs = new TabControl();
            materialTexturesTab = new TabPage();
            materialParamsTab = new TabPage();
            materialUvTab = new TabPage();
            materialSamplersTab = new TabPage();
            materialTexturesSplit = new SplitContainer();
            materialTexturesGrid = new DataGridView();
            materialTexturePreview = new PictureBox();
            materialUvSplit = new SplitContainer();
            materialParamsGrid = new DataGridView();
            materialUvGrid = new DataGridView();
            materialUvPreview = new PictureBox();
            materialSamplersGrid = new DataGridView();
            sceneTreeCtxtMenu = new ContextMenuStrip(components);
            deleteToolStripMenuItem = new ToolStripMenuItem();
            exportToolStripMenuItem = new ToolStripMenuItem();
            renderCtrl = new GFTool.RenderControl_WinForms.RenderControl();
            menuStrip1.SuspendLayout();
            panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainerMain).BeginInit();
            splitContainerMain.Panel1.SuspendLayout();
            splitContainerMain.Panel2.SuspendLayout();
            splitContainerMain.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainerLeft).BeginInit();
            splitContainerLeft.Panel1.SuspendLayout();
            splitContainerLeft.Panel2.SuspendLayout();
            splitContainerLeft.SuspendLayout();
            leftTabs.SuspendLayout();
            sceneTabPage.SuspendLayout();
            animationsTabPage.SuspendLayout();
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
            materialParamsTab.SuspendLayout();
            materialUvTab.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)materialUvSplit).BeginInit();
            materialUvSplit.Panel1.SuspendLayout();
            materialUvSplit.Panel2.SuspendLayout();
            materialUvSplit.SuspendLayout();
            materialSamplersTab.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)materialTexturesGrid).BeginInit();
            ((System.ComponentModel.ISupportInitialize)materialTexturePreview).BeginInit();
            ((System.ComponentModel.ISupportInitialize)materialParamsGrid).BeginInit();
            ((System.ComponentModel.ISupportInitialize)materialUvGrid).BeginInit();
            ((System.ComponentModel.ISupportInitialize)materialUvPreview).BeginInit();
            ((System.ComponentModel.ISupportInitialize)materialSamplersGrid).BeginInit();
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
            viewToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { wireframeToolStripMenuItem, showSkeletonToolStripMenuItem, toolStripSeparator1, shadingToolStripMenuItem });
            viewToolStripMenuItem.Name = "viewToolStripMenuItem";
            viewToolStripMenuItem.Size = new Size(44, 20);
            viewToolStripMenuItem.Text = "View";
            //
            // wireframeToolStripMenuItem
            //
            wireframeToolStripMenuItem.CheckOnClick = true;
            wireframeToolStripMenuItem.Name = "wireframeToolStripMenuItem";
            wireframeToolStripMenuItem.Size = new Size(129, 22);
            wireframeToolStripMenuItem.Text = "Wireframe";
            wireframeToolStripMenuItem.Click += wireframeToolStripMenuItem_Click;
            //
            // shadingToolStripMenuItem
            //
            shadingToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { shadingLitToolStripMenuItem, shadingToonToolStripMenuItem, shadingLegacyToolStripMenuItem });
            shadingToolStripMenuItem.Name = "shadingToolStripMenuItem";
            shadingToolStripMenuItem.Size = new Size(62, 22);
            shadingToolStripMenuItem.Text = "Shading";
            //
            // shadingLitToolStripMenuItem
            //
            shadingLitToolStripMenuItem.CheckOnClick = true;
            shadingLitToolStripMenuItem.Name = "shadingLitToolStripMenuItem";
            shadingLitToolStripMenuItem.Size = new Size(114, 22);
            shadingLitToolStripMenuItem.Text = "Lit";
            shadingLitToolStripMenuItem.Click += shadingLitToolStripMenuItem_Click;
            //
            // shadingToonToolStripMenuItem
            //
            shadingToonToolStripMenuItem.CheckOnClick = true;
            shadingToonToolStripMenuItem.Name = "shadingToonToolStripMenuItem";
            shadingToonToolStripMenuItem.Size = new Size(114, 22);
            shadingToonToolStripMenuItem.Text = "Toon";
            shadingToonToolStripMenuItem.Click += shadingToonToolStripMenuItem_Click;
            //
            // shadingLegacyToolStripMenuItem
            //
            shadingLegacyToolStripMenuItem.CheckOnClick = true;
            shadingLegacyToolStripMenuItem.Name = "shadingLegacyToolStripMenuItem";
            shadingLegacyToolStripMenuItem.Size = new Size(114, 22);
            shadingLegacyToolStripMenuItem.Text = "Legacy";
            shadingLegacyToolStripMenuItem.Click += shadingLegacyToolStripMenuItem_Click;
            //
            // showSkeletonToolStripMenuItem
            //
            showSkeletonToolStripMenuItem.CheckOnClick = true;
            showSkeletonToolStripMenuItem.Name = "showSkeletonToolStripMenuItem";
            showSkeletonToolStripMenuItem.Size = new Size(155, 22);
            showSkeletonToolStripMenuItem.Text = "Show Skeleton";
            showSkeletonToolStripMenuItem.Click += showSkeletonToolStripMenuItem_Click;
            //
            // messageListView
            //
            messageListView.Columns.AddRange(new ColumnHeader[] { columnHeader1 });
            messageListView.Dock = DockStyle.Fill;
            messageListView.Name = "messageListView";
            messageListView.Size = new Size(1080, 120);
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
            statusLbl.Name = "statusLbl";
            statusLbl.Size = new Size(38, 15);
            statusLbl.TabIndex = 3;
            statusLbl.Text = "label1";
            //
            // panel1
            //
            panel1.Controls.Add(splitContainer1);
            panel1.Dock = DockStyle.Fill;
            panel1.Name = "panel1";
            panel1.Size = new Size(364, 893);
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
            splitContainer1.Size = new Size(364, 870);
            splitContainer1.SplitterDistance = 419;
            splitContainer1.TabIndex = 0;
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
            splitContainerLeft.Panel2.Controls.Add(statusLbl);
            splitContainerLeft.Size = new Size(1098, 900);
            splitContainerLeft.SplitterDistance = 750;
            splitContainerLeft.TabIndex = 0;
            //
            // leftTabs
            //
            leftTabs.Controls.Add(sceneTabPage);
            leftTabs.Controls.Add(animationsTabPage);
            leftTabs.Dock = DockStyle.Fill;
            leftTabs.Location = new Point(3, 3);
            leftTabs.Name = "leftTabs";
            leftTabs.SelectedIndex = 0;
            leftTabs.Size = new Size(358, 413);
            leftTabs.TabIndex = 0;
            //
            // sceneTabPage
            //
            sceneTabPage.Controls.Add(sceneTree);
            sceneTabPage.Location = new Point(4, 24);
            sceneTabPage.Name = "sceneTabPage";
            sceneTabPage.Padding = new Padding(3);
            sceneTabPage.Size = new Size(350, 385);
            sceneTabPage.TabIndex = 0;
            sceneTabPage.Text = "Scene";
            sceneTabPage.UseVisualStyleBackColor = true;
            //
            // animationsTabPage
            //
            animationsTabPage.Controls.Add(animationsList);
            animationsTabPage.Controls.Add(animationsToolbar);
            animationsTabPage.Location = new Point(4, 24);
            animationsTabPage.Name = "animationsTabPage";
            animationsTabPage.Padding = new Padding(3);
            animationsTabPage.Size = new Size(350, 385);
            animationsTabPage.TabIndex = 1;
            animationsTabPage.Text = "Animations";
            animationsTabPage.UseVisualStyleBackColor = true;
            //
            // sceneTree
            //
            sceneTree.Dock = DockStyle.Fill;
            sceneTree.Location = new Point(3, 3);
            sceneTree.Name = "sceneTree";
            sceneTree.Size = new Size(344, 379);
            sceneTree.TabIndex = 0;
            sceneTree.MouseUp += sceneTree_MouseUp;
            //
            // animationsList
            //
            animationsList.Dock = DockStyle.Fill;
            animationsList.Location = new Point(3, 63);
            animationsList.Name = "animationsList";
            animationsList.Size = new Size(344, 319);
            animationsList.TabIndex = 1;
            animationsList.UseCompatibleStateImageBehavior = false;
            //
            // animationsToolbar
            //
            animationsToolbar.Dock = DockStyle.Top;
            animationsToolbar.Controls.Add(loadAnimationButton);
            animationsToolbar.Controls.Add(playAnimationButton);
            animationsToolbar.Controls.Add(stopAnimationButton);
            animationsToolbar.Controls.Add(exportAnimationButton);
            animationsToolbar.Controls.Add(exportModelWithAnimationsButton);
            animationsToolbar.Location = new Point(3, 3);
            animationsToolbar.Name = "animationsToolbar";
            animationsToolbar.Size = new Size(344, 60);
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
            // playAnimationButton
            //
            playAnimationButton.Location = new Point(87, 4);
            playAnimationButton.Name = "playAnimationButton";
            playAnimationButton.Size = new Size(75, 24);
            playAnimationButton.TabIndex = 1;
            playAnimationButton.Text = "Play";
            playAnimationButton.UseVisualStyleBackColor = true;
            playAnimationButton.Click += playAnimationButton_Click;
            //
            // stopAnimationButton
            //
            stopAnimationButton.Location = new Point(168, 4);
            stopAnimationButton.Name = "stopAnimationButton";
            stopAnimationButton.Size = new Size(75, 24);
            stopAnimationButton.TabIndex = 2;
            stopAnimationButton.Text = "Stop";
            stopAnimationButton.UseVisualStyleBackColor = true;
            stopAnimationButton.Click += stopAnimationButton_Click;
            //
            // exportAnimationButton
            //
            exportAnimationButton.Location = new Point(249, 4);
            exportAnimationButton.Name = "exportAnimationButton";
            exportAnimationButton.Size = new Size(75, 24);
            exportAnimationButton.TabIndex = 3;
            exportAnimationButton.Text = "Export...";
            exportAnimationButton.UseVisualStyleBackColor = true;
            exportAnimationButton.Click += exportAnimationButton_Click;
            //
            // exportModelWithAnimationsButton
            //
            exportModelWithAnimationsButton.Location = new Point(6, 32);
            exportModelWithAnimationsButton.Name = "exportModelWithAnimationsButton";
            exportModelWithAnimationsButton.Size = new Size(318, 24);
            exportModelWithAnimationsButton.TabIndex = 4;
            exportModelWithAnimationsButton.Text = "Export Model + Anims...";
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
            modelProperties.Size = new Size(358, 441);
            modelProperties.TabIndex = 0;
            //
            // tabPage1
            //
            tabPage1.Controls.Add(groupBox1);
            tabPage1.Location = new Point(4, 24);
            tabPage1.Name = "tabPage1";
            tabPage1.Padding = new Padding(3);
            tabPage1.Size = new Size(350, 413);
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
            tabPage2.Size = new Size(350, 413);
            tabPage2.TabIndex = 1;
            tabPage2.Text = "Materials";
            tabPage2.UseVisualStyleBackColor = true;
            //
            // materialList
            //
            materialList.Columns.AddRange(new ColumnHeader[] { materialColumnHeader });
            materialList.Dock = DockStyle.Fill;
            materialList.FullRowSelect = true;
            materialList.Name = "materialList";
            materialList.Size = new Size(140, 407);
            materialList.TabIndex = 0;
            materialList.UseCompatibleStateImageBehavior = false;
            materialList.View = View.Details;
            //
            // materialColumnHeader
            //
            materialColumnHeader.Text = "Materials";
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
            materialSplitContainer.Size = new Size(344, 407);
            materialSplitContainer.SplitterDistance = 140;
            materialSplitContainer.TabIndex = 1;
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
            materialTabs.Size = new Size(200, 407);
            materialTabs.TabIndex = 0;
            //
            // materialTexturesTab
            //
            materialTexturesTab.Controls.Add(materialTexturesSplit);
            materialTexturesTab.Location = new Point(4, 24);
            materialTexturesTab.Name = "materialTexturesTab";
            materialTexturesTab.Padding = new Padding(3);
            materialTexturesTab.Size = new Size(192, 379);
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
            materialTexturesSplit.Panel2.Controls.Add(materialTexturePreview);
            materialTexturesSplit.Size = new Size(186, 373);
            materialTexturesSplit.SplitterDistance = 230;
            materialTexturesSplit.TabIndex = 0;
            //
            // materialParamsTab
            //
            materialParamsTab.Controls.Add(materialParamsGrid);
            materialParamsTab.Location = new Point(4, 24);
            materialParamsTab.Name = "materialParamsTab";
            materialParamsTab.Padding = new Padding(3);
            materialParamsTab.Size = new Size(192, 379);
            materialParamsTab.TabIndex = 1;
            materialParamsTab.Text = "Params";
            materialParamsTab.UseVisualStyleBackColor = true;
            //
            // materialUvTab
            //
            materialUvTab.Controls.Add(materialUvSplit);
            materialUvTab.Location = new Point(4, 24);
            materialUvTab.Name = "materialUvTab";
            materialUvTab.Padding = new Padding(3);
            materialUvTab.Size = new Size(192, 379);
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
            materialUvSplit.Panel2.Controls.Add(materialUvPreview);
            materialUvSplit.Size = new Size(186, 373);
            materialUvSplit.SplitterDistance = 230;
            materialUvSplit.TabIndex = 0;
            //
            // materialSamplersTab
            //
            materialSamplersTab.Controls.Add(materialSamplersGrid);
            materialSamplersTab.Location = new Point(4, 24);
            materialSamplersTab.Name = "materialSamplersTab";
            materialSamplersTab.Padding = new Padding(3);
            materialSamplersTab.Size = new Size(192, 379);
            materialSamplersTab.TabIndex = 3;
            materialSamplersTab.Text = "Samplers";
            materialSamplersTab.UseVisualStyleBackColor = true;
            //
            // materialTexturesGrid
            //
            materialTexturesGrid.AllowUserToAddRows = false;
            materialTexturesGrid.AllowUserToDeleteRows = false;
            materialTexturesGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            materialTexturesGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            materialTexturesGrid.Dock = DockStyle.Fill;
            materialTexturesGrid.MultiSelect = false;
            materialTexturesGrid.Name = "materialTexturesGrid";
            materialTexturesGrid.ReadOnly = true;
            materialTexturesGrid.RowHeadersVisible = false;
            materialTexturesGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            materialTexturesGrid.TabIndex = 0;
            //
            // materialTexturePreview
            //
            materialTexturePreview.Dock = DockStyle.Fill;
            materialTexturePreview.Location = new Point(0, 0);
            materialTexturePreview.Name = "materialTexturePreview";
            materialTexturePreview.SizeMode = PictureBoxSizeMode.Zoom;
            materialTexturePreview.TabIndex = 0;
            materialTexturePreview.TabStop = false;
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
            materialParamsGrid.Size = new Size(186, 373);
            materialParamsGrid.TabIndex = 0;
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
            materialUvGrid.TabIndex = 0;
            //
            // materialUvPreview
            //
            materialUvPreview.Dock = DockStyle.Fill;
            materialUvPreview.Location = new Point(0, 0);
            materialUvPreview.Name = "materialUvPreview";
            materialUvPreview.SizeMode = PictureBoxSizeMode.Zoom;
            materialUvPreview.TabIndex = 0;
            materialUvPreview.TabStop = false;
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
            materialSamplersGrid.Size = new Size(186, 373);
            materialSamplersGrid.TabIndex = 0;
            //
            // sceneTreeCtxtMenu
            //
            sceneTreeCtxtMenu.Items.AddRange(new ToolStripItem[] { exportToolStripMenuItem, deleteToolStripMenuItem });
            sceneTreeCtxtMenu.Name = "sceneTreeCtxtMenu";
            sceneTreeCtxtMenu.Size = new Size(109, 48);
            //
            // exportToolStripMenuItem
            //
            exportToolStripMenuItem.Name = "exportToolStripMenuItem";
            exportToolStripMenuItem.Size = new Size(108, 22);
            exportToolStripMenuItem.Text = "Export...";
            exportToolStripMenuItem.Click += exportToolStripMenuItem_Click;
            //
            // deleteToolStripMenuItem
            //
            deleteToolStripMenuItem.Name = "deleteToolStripMenuItem";
            deleteToolStripMenuItem.Size = new Size(108, 22);
            deleteToolStripMenuItem.Text = "Delete";
            deleteToolStripMenuItem.Click += deleteToolStripMenuItem_Click;
            //
            // renderCtrl
            //
            renderCtrl.API = OpenTK.Windowing.Common.ContextAPI.OpenGL;
            renderCtrl.APIVersion = new Version(3, 3, 0, 0);
            renderCtrl.Flags = OpenTK.Windowing.Common.ContextFlags.Default;
            renderCtrl.IsEventDriven = true;
            renderCtrl.Dock = DockStyle.Fill;
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
            splitContainerMain.Panel1.ResumeLayout(false);
            splitContainerMain.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainerMain).EndInit();
            splitContainerMain.ResumeLayout(false);
            splitContainerLeft.Panel1.ResumeLayout(false);
            splitContainerLeft.Panel2.ResumeLayout(false);
            splitContainerLeft.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainerLeft).EndInit();
            splitContainerLeft.ResumeLayout(false);
            leftTabs.ResumeLayout(false);
            sceneTabPage.ResumeLayout(false);
            animationsTabPage.ResumeLayout(false);
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
            materialTexturesSplit.Panel1.ResumeLayout(false);
            materialTexturesSplit.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)materialTexturesSplit).EndInit();
            materialTexturesSplit.ResumeLayout(false);
            materialTexturesTab.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)materialTexturePreview).EndInit();
            materialParamsTab.ResumeLayout(false);
            materialUvSplit.Panel1.ResumeLayout(false);
            materialUvSplit.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)materialUvSplit).EndInit();
            materialUvSplit.ResumeLayout(false);
            materialUvTab.ResumeLayout(false);
            materialSamplersTab.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)materialTexturesGrid).EndInit();
            ((System.ComponentModel.ISupportInitialize)materialParamsGrid).EndInit();
            ((System.ComponentModel.ISupportInitialize)materialUvGrid).EndInit();
            ((System.ComponentModel.ISupportInitialize)materialUvPreview).EndInit();
            ((System.ComponentModel.ISupportInitialize)materialSamplersGrid).EndInit();
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
        private Button playAnimationButton;
        private Button stopAnimationButton;
        private Button exportAnimationButton;
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
        private PictureBox materialTexturePreview;
        private SplitContainer materialUvSplit;
        private DataGridView materialParamsGrid;
        private DataGridView materialUvGrid;
        private PictureBox materialUvPreview;
        private DataGridView materialSamplersGrid;
        private ToolStripSeparator toolStripSeparator1;
        private ToolStripMenuItem shadingToolStripMenuItem;
        private ToolStripMenuItem shadingLitToolStripMenuItem;
        private ToolStripMenuItem shadingToonToolStripMenuItem;
        private ToolStripMenuItem shadingLegacyToolStripMenuItem;
        private ToolStripMenuItem showSkeletonToolStripMenuItem;
        private GroupBox groupBox1;
        private ContextMenuStrip sceneTreeCtxtMenu;
        private ToolStripMenuItem deleteToolStripMenuItem;
        private ToolStripMenuItem exportToolStripMenuItem;
        private GFTool.RenderControl_WinForms.RenderControl renderCtrl;
    }
}
