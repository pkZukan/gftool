namespace TrinitySceneView
{
    partial class SceneViewerForm
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
            openTRSOT = new ToolStripMenuItem();
            viewToolStripMenuItem = new ToolStripMenuItem();
            toolStripMenuItem1 = new ToolStripMenuItem();
            toolstripGBuf_All = new ToolStripMenuItem();
            toolstripGBuf_Albedo = new ToolStripMenuItem();
            toolstripGBuf_Normal = new ToolStripMenuItem();
            toolstripGBuf_Specular = new ToolStripMenuItem();
            toolstripGBuf_AO = new ToolStripMenuItem();
            toolstripGBuf_Depth = new ToolStripMenuItem();
            sceneView = new TreeView();
            sceneContext = new ContextMenuStrip(components);
            expandToolStripMenuItem = new ToolStripMenuItem();
            statusLbl = new Label();
            messageListView = new ListView();
            columnHeader1 = new ColumnHeader();
            InfoBox = new TextBox();
            renderCtrl = new GFTool.RenderControl_WinForms.RenderControl();
            splitContainerOuter = new SplitContainer();
            splitContainerCenterRight = new SplitContainer();
            splitContainerRenderBottom = new SplitContainer();
            bottomPanel = new Panel();
            rightTabs = new TabControl();
            tabProperties = new TabPage();
            splitContainerProperties = new SplitContainer();
            propertyGrid = new PropertyGrid();
            tabModels = new TabPage();
            modelsListView = new ListView();
            columnHeaderModelName = new ColumnHeader();
            columnHeaderModelPath = new ColumnHeader();
            menuStrip1.SuspendLayout();
            sceneContext.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainerOuter).BeginInit();
            splitContainerOuter.Panel1.SuspendLayout();
            splitContainerOuter.Panel2.SuspendLayout();
            splitContainerOuter.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainerCenterRight).BeginInit();
            splitContainerCenterRight.Panel1.SuspendLayout();
            splitContainerCenterRight.Panel2.SuspendLayout();
            splitContainerCenterRight.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainerRenderBottom).BeginInit();
            splitContainerRenderBottom.Panel1.SuspendLayout();
            splitContainerRenderBottom.Panel2.SuspendLayout();
            splitContainerRenderBottom.SuspendLayout();
            bottomPanel.SuspendLayout();
            rightTabs.SuspendLayout();
            tabProperties.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainerProperties).BeginInit();
            splitContainerProperties.Panel1.SuspendLayout();
            splitContainerProperties.Panel2.SuspendLayout();
            splitContainerProperties.SuspendLayout();
            tabModels.SuspendLayout();
            SuspendLayout();
            //
            // menuStrip1
            //
            menuStrip1.Items.AddRange(new ToolStripItem[] { fileToolStripMenuItem, viewToolStripMenuItem });
            menuStrip1.Location = new Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Size = new Size(1659, 24);
            menuStrip1.TabIndex = 0;
            menuStrip1.Text = "menuStrip1";
            //
            // fileToolStripMenuItem
            //
            fileToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { openTRSOT });
            fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            fileToolStripMenuItem.Size = new Size(37, 20);
            fileToolStripMenuItem.Text = "File";
            //
            // openTRSOT
            //
            openTRSOT.Name = "openTRSOT";
            openTRSOT.Size = new Size(175, 22);
            openTRSOT.Text = "Open Scene Object";
            openTRSOT.Click += openTRSOT_Click;
            //
            // viewToolStripMenuItem
            //
            viewToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { toolStripMenuItem1 });
            viewToolStripMenuItem.Name = "viewToolStripMenuItem";
            viewToolStripMenuItem.Size = new Size(44, 20);
            viewToolStripMenuItem.Text = "View";
            //
            // toolStripMenuItem1
            //
            toolStripMenuItem1.DropDownItems.AddRange(new ToolStripItem[] { toolstripGBuf_All, toolstripGBuf_Albedo, toolstripGBuf_Normal, toolstripGBuf_Specular, toolstripGBuf_AO, toolstripGBuf_Depth });
            toolStripMenuItem1.Name = "toolStripMenuItem1";
            toolStripMenuItem1.Size = new Size(114, 22);
            toolStripMenuItem1.Text = "GBuffer";
            //
            // toolstripGBuf_All
            //
            toolstripGBuf_All.Checked = true;
            toolstripGBuf_All.CheckState = CheckState.Checked;
            toolstripGBuf_All.Name = "toolstripGBuf_All";
            toolstripGBuf_All.Size = new Size(119, 22);
            toolstripGBuf_All.Text = "All";
            toolstripGBuf_All.Click += toolstripGBuf_Clicked;
            //
            // toolstripGBuf_Albedo
            //
            toolstripGBuf_Albedo.Name = "toolstripGBuf_Albedo";
            toolstripGBuf_Albedo.Size = new Size(119, 22);
            toolstripGBuf_Albedo.Text = "Albedo";
            toolstripGBuf_Albedo.Click += toolstripGBuf_Clicked;
            //
            // toolstripGBuf_Normal
            //
            toolstripGBuf_Normal.Name = "toolstripGBuf_Normal";
            toolstripGBuf_Normal.Size = new Size(119, 22);
            toolstripGBuf_Normal.Text = "Normal";
            toolstripGBuf_Normal.Click += toolstripGBuf_Clicked;
            //
            // toolstripGBuf_Specular
            //
            toolstripGBuf_Specular.Name = "toolstripGBuf_Specular";
            toolstripGBuf_Specular.Size = new Size(119, 22);
            toolstripGBuf_Specular.Text = "Specular";
            toolstripGBuf_Specular.Click += toolstripGBuf_Clicked;
            //
            // toolstripGBuf_AO
            //
            toolstripGBuf_AO.Name = "toolstripGBuf_AO";
            toolstripGBuf_AO.Size = new Size(119, 22);
            toolstripGBuf_AO.Text = "AO";
            //
            // toolstripGBuf_Depth
            //
            toolstripGBuf_Depth.Name = "toolstripGBuf_Depth";
            toolstripGBuf_Depth.Size = new Size(119, 22);
            toolstripGBuf_Depth.Text = "Depth";
            toolstripGBuf_Depth.Click += toolstripGBuf_Clicked;
            //
            // sceneView
            //
            sceneView.Dock = DockStyle.Fill;
            sceneView.Location = new Point(0, 0);
            sceneView.Name = "sceneView";
            sceneView.Size = new Size(241, 897);
            sceneView.TabIndex = 1;
            sceneView.MouseUp += sceneView_MouseUp;
            //
            // sceneContext
            //
            sceneContext.Items.AddRange(new ToolStripItem[] { expandToolStripMenuItem });
            sceneContext.Name = "sceneContext";
            sceneContext.Size = new Size(114, 26);
            //
            // expandToolStripMenuItem
            //
            expandToolStripMenuItem.Name = "expandToolStripMenuItem";
            expandToolStripMenuItem.Size = new Size(113, 22);
            expandToolStripMenuItem.Text = "Expand";
            expandToolStripMenuItem.Click += expandToolStripMenuItem_Click;
            //
            // statusLbl
            //
            statusLbl.Dock = DockStyle.Top;
            statusLbl.Location = new Point(0, 0);
            statusLbl.Name = "statusLbl";
            statusLbl.Size = new Size(1080, 14);
            statusLbl.TabIndex = 3;
            statusLbl.Text = "label1";
            //
            // messageListView
            //
            messageListView.Dock = DockStyle.Fill;
            messageListView.Columns.AddRange(new ColumnHeader[] { columnHeader1 });
            messageListView.FullRowSelect = true;
            messageListView.GridLines = true;
            messageListView.LabelWrap = false;
            messageListView.Location = new Point(0, 14);
            messageListView.Name = "messageListView";
            messageListView.Size = new Size(1108, 159);
            messageListView.TabIndex = 4;
            messageListView.UseCompatibleStateImageBehavior = false;
            messageListView.View = View.Details;
            //
            // columnHeader1
            //
            columnHeader1.Text = "Messages";
            columnHeader1.Width = 1000;
            //
            // InfoBox
            //
            InfoBox.BorderStyle = BorderStyle.None;
            InfoBox.Dock = DockStyle.Fill;
            InfoBox.Multiline = true;
            InfoBox.Name = "InfoBox";
            InfoBox.ReadOnly = true;
            InfoBox.ScrollBars = ScrollBars.Vertical;
            InfoBox.Size = new Size(290, 190);
            InfoBox.TabIndex = 0;
            //
            // renderCtrl
            //
            renderCtrl.API = OpenTK.Windowing.Common.ContextAPI.OpenGL;
            renderCtrl.APIVersion = new Version(3, 3, 0, 0);
            renderCtrl.Flags = OpenTK.Windowing.Common.ContextFlags.Default;
            renderCtrl.IsEventDriven = true;
            renderCtrl.Dock = DockStyle.Fill;
            renderCtrl.Location = new Point(0, 0);
            renderCtrl.Name = "renderCtrl";
            renderCtrl.Profile = OpenTK.Windowing.Common.ContextProfile.Core;
            renderCtrl.SharedContext = null;
            renderCtrl.Size = new Size(1080, 720);
            renderCtrl.TabIndex = 6;
            renderCtrl.Load += glCtxt_Load;
            renderCtrl.Paint += glCtxt_Paint;
            renderCtrl.KeyDown += glCtxt_KeyDown;
            renderCtrl.KeyUp += glCtxt_KeyUp;
            //
            // splitContainerOuter
            //
            splitContainerOuter.Dock = DockStyle.Fill;
            splitContainerOuter.FixedPanel = FixedPanel.Panel1;
            splitContainerOuter.Location = new Point(0, 24);
            splitContainerOuter.Name = "splitContainerOuter";
            splitContainerOuter.Panel1.Controls.Add(sceneView);
            splitContainerOuter.Panel2.Controls.Add(splitContainerCenterRight);
            splitContainerOuter.Size = new Size(1659, 897);
            splitContainerOuter.SplitterDistance = 241;
            splitContainerOuter.TabIndex = 7;
            //
            // splitContainerCenterRight
            //
            splitContainerCenterRight.Dock = DockStyle.Fill;
            splitContainerCenterRight.FixedPanel = FixedPanel.Panel2;
            splitContainerCenterRight.Location = new Point(0, 0);
            splitContainerCenterRight.Name = "splitContainerCenterRight";
            splitContainerCenterRight.Panel1.Controls.Add(splitContainerRenderBottom);
            splitContainerCenterRight.Panel2.Controls.Add(rightTabs);
            splitContainerCenterRight.Size = new Size(1414, 897);
            splitContainerCenterRight.SplitterDistance = 1108;
            splitContainerCenterRight.TabIndex = 0;
            //
            // splitContainerRenderBottom
            //
            splitContainerRenderBottom.Dock = DockStyle.Fill;
            splitContainerRenderBottom.FixedPanel = FixedPanel.Panel2;
            splitContainerRenderBottom.Location = new Point(0, 0);
            splitContainerRenderBottom.Name = "splitContainerRenderBottom";
            splitContainerRenderBottom.Orientation = Orientation.Horizontal;
            splitContainerRenderBottom.Panel1.Controls.Add(renderCtrl);
            splitContainerRenderBottom.Panel2.Controls.Add(bottomPanel);
            splitContainerRenderBottom.Size = new Size(1108, 897);
            splitContainerRenderBottom.SplitterDistance = 720;
            splitContainerRenderBottom.TabIndex = 0;
            //
            // bottomPanel
            //
            bottomPanel.Controls.Add(messageListView);
            bottomPanel.Controls.Add(statusLbl);
            bottomPanel.Dock = DockStyle.Fill;
            bottomPanel.Location = new Point(0, 0);
            bottomPanel.Name = "bottomPanel";
            bottomPanel.Size = new Size(1108, 173);
            bottomPanel.TabIndex = 0;
            //
            // rightTabs
            //
            rightTabs.Controls.Add(tabProperties);
            rightTabs.Controls.Add(tabModels);
            rightTabs.Dock = DockStyle.Fill;
            rightTabs.Location = new Point(0, 0);
            rightTabs.Name = "rightTabs";
            rightTabs.SelectedIndex = 0;
            rightTabs.Size = new Size(302, 897);
            rightTabs.TabIndex = 8;
            //
            // tabProperties
            //
            tabProperties.Controls.Add(splitContainerProperties);
            tabProperties.Location = new Point(4, 24);
            tabProperties.Name = "tabProperties";
            tabProperties.Padding = new Padding(3);
            tabProperties.Size = new Size(294, 869);
            tabProperties.TabIndex = 0;
            tabProperties.Text = "Properties";
            tabProperties.UseVisualStyleBackColor = true;
            //
            // splitContainerProperties
            //
            splitContainerProperties.Dock = DockStyle.Fill;
            splitContainerProperties.FixedPanel = FixedPanel.Panel2;
            splitContainerProperties.Location = new Point(3, 3);
            splitContainerProperties.Name = "splitContainerProperties";
            splitContainerProperties.Orientation = Orientation.Horizontal;
            splitContainerProperties.Panel1.Controls.Add(InfoBox);
            splitContainerProperties.Panel2.Controls.Add(propertyGrid);
            splitContainerProperties.Size = new Size(288, 863);
            splitContainerProperties.SplitterDistance = 400;
            splitContainerProperties.TabIndex = 0;
            //
            // propertyGrid
            //
            propertyGrid.Dock = DockStyle.Fill;
            propertyGrid.Location = new Point(0, 0);
            propertyGrid.Name = "propertyGrid";
            propertyGrid.Size = new Size(288, 459);
            propertyGrid.TabIndex = 0;
            //
            // tabModels
            //
            tabModels.Controls.Add(modelsListView);
            tabModels.Location = new Point(4, 24);
            tabModels.Name = "tabModels";
            tabModels.Padding = new Padding(3);
            tabModels.Size = new Size(294, 869);
            tabModels.TabIndex = 1;
            tabModels.Text = "Models";
            tabModels.UseVisualStyleBackColor = true;
            //
            // modelsListView
            //
            modelsListView.CheckBoxes = true;
            modelsListView.Columns.AddRange(new ColumnHeader[] { columnHeaderModelName, columnHeaderModelPath });
            modelsListView.Dock = DockStyle.Fill;
            modelsListView.FullRowSelect = true;
            modelsListView.GridLines = true;
            modelsListView.Location = new Point(3, 3);
            modelsListView.Name = "modelsListView";
            modelsListView.Size = new Size(288, 863);
            modelsListView.TabIndex = 0;
            modelsListView.UseCompatibleStateImageBehavior = false;
            modelsListView.View = View.Details;
            modelsListView.ItemChecked += modelsListView_ItemChecked;
            //
            // columnHeaderModelName
            //
            columnHeaderModelName.Text = "Name";
            columnHeaderModelName.Width = 120;
            //
            // columnHeaderModelPath
            //
            columnHeaderModelPath.Text = "Path";
            columnHeaderModelPath.Width = 400;
            //
            // SceneViewerForm
            //
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1659, 921);
            Controls.Add(splitContainerOuter);
            Controls.Add(menuStrip1);
            KeyPreview = true;
            MainMenuStrip = menuStrip1;
            Name = "SceneViewerForm";
            Text = "Trinity Scene View";
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            sceneContext.ResumeLayout(false);
            splitContainerOuter.Panel1.ResumeLayout(false);
            splitContainerOuter.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainerOuter).EndInit();
            splitContainerOuter.ResumeLayout(false);
            splitContainerCenterRight.Panel1.ResumeLayout(false);
            splitContainerCenterRight.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainerCenterRight).EndInit();
            splitContainerCenterRight.ResumeLayout(false);
            splitContainerRenderBottom.Panel1.ResumeLayout(false);
            splitContainerRenderBottom.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainerRenderBottom).EndInit();
            splitContainerRenderBottom.ResumeLayout(false);
            bottomPanel.ResumeLayout(false);
            rightTabs.ResumeLayout(false);
            tabProperties.ResumeLayout(false);
            splitContainerProperties.Panel1.ResumeLayout(false);
            splitContainerProperties.Panel1.PerformLayout();
            splitContainerProperties.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainerProperties).EndInit();
            splitContainerProperties.ResumeLayout(false);
            tabModels.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private MenuStrip menuStrip1;
        private ToolStripMenuItem fileToolStripMenuItem;
        private ToolStripMenuItem openTRSOT;
        private TreeView sceneView;
        private ContextMenuStrip sceneContext;
        private ToolStripMenuItem expandToolStripMenuItem;
        private Label statusLbl;
        private ToolStripMenuItem viewToolStripMenuItem;
        private ToolStripMenuItem toolStripMenuItem1;
        private ToolStripMenuItem toolstripGBuf_All;
        private ToolStripMenuItem toolstripGBuf_Albedo;
        private ToolStripMenuItem toolstripGBuf_Normal;
        private ToolStripMenuItem toolstripGBuf_Depth;
        private ToolStripMenuItem toolstripGBuf_Specular;
        private ToolStripMenuItem toolstripGBuf_AO;
        private ListView messageListView;
        private ColumnHeader columnHeader1;
        private TextBox InfoBox;
        private GFTool.RenderControl_WinForms.RenderControl renderCtrl;
        private SplitContainer splitContainerOuter;
        private SplitContainer splitContainerCenterRight;
        private SplitContainer splitContainerRenderBottom;
        private Panel bottomPanel;
        private TabControl rightTabs;
        private TabPage tabProperties;
        private TabPage tabModels;
        private SplitContainer splitContainerProperties;
        private PropertyGrid propertyGrid;
        private ListView modelsListView;
        private ColumnHeader columnHeaderModelName;
        private ColumnHeader columnHeaderModelPath;
    }
}
