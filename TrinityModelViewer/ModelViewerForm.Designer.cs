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
            messageListView = new ListView();
            columnHeader1 = new ColumnHeader();
            statusLbl = new Label();
            panel1 = new Panel();
            splitContainer1 = new SplitContainer();
            sceneTree = new TreeView();
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
            sceneTreeCtxtMenu = new ContextMenuStrip(components);
            deleteToolStripMenuItem = new ToolStripMenuItem();
            renderCtrl = new GFTool.RenderControl_WinForms.RenderControl();
            menuStrip1.SuspendLayout();
            panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
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
            viewToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { wireframeToolStripMenuItem });
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
            // messageListView
            // 
            messageListView.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
            messageListView.Columns.AddRange(new ColumnHeader[] { columnHeader1 });
            messageListView.Location = new Point(12, 753);
            messageListView.Name = "messageListView";
            messageListView.Size = new Size(1080, 144);
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
            statusLbl.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            statusLbl.AutoSize = true;
            statusLbl.Location = new Point(12, 900);
            statusLbl.Name = "statusLbl";
            statusLbl.Size = new Size(38, 15);
            statusLbl.TabIndex = 3;
            statusLbl.Text = "label1";
            // 
            // panel1
            // 
            panel1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            panel1.Controls.Add(splitContainer1);
            panel1.Location = new Point(1098, 27);
            panel1.Name = "panel1";
            panel1.Size = new Size(364, 870);
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
            splitContainer1.Panel1.Controls.Add(sceneTree);
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
            // sceneTree
            // 
            sceneTree.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            sceneTree.Location = new Point(3, 3);
            sceneTree.Name = "sceneTree";
            sceneTree.Size = new Size(358, 413);
            sceneTree.TabIndex = 0;
            sceneTree.MouseUp += sceneTree_MouseUp;
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
            tabPage2.Controls.Add(materialList);
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
            materialList.Location = new Point(6, 6);
            materialList.Name = "materialList";
            materialList.Size = new Size(338, 407);
            materialList.TabIndex = 0;
            materialList.UseCompatibleStateImageBehavior = false;
            // 
            // sceneTreeCtxtMenu
            // 
            sceneTreeCtxtMenu.Items.AddRange(new ToolStripItem[] { deleteToolStripMenuItem });
            sceneTreeCtxtMenu.Name = "sceneTreeCtxtMenu";
            sceneTreeCtxtMenu.Size = new Size(108, 26);
            // 
            // deleteToolStripMenuItem
            // 
            deleteToolStripMenuItem.Name = "deleteToolStripMenuItem";
            deleteToolStripMenuItem.Size = new Size(107, 22);
            deleteToolStripMenuItem.Text = "Delete";
            deleteToolStripMenuItem.Click += deleteToolStripMenuItem_Click;
            // 
            // renderCtrl
            // 
            renderCtrl.API = OpenTK.Windowing.Common.ContextAPI.OpenGL;
            renderCtrl.APIVersion = new Version(3, 3, 0, 0);
            renderCtrl.Flags = OpenTK.Windowing.Common.ContextFlags.Default;
            renderCtrl.IsEventDriven = true;
            renderCtrl.Location = new Point(15, 27);
            renderCtrl.Name = "renderCtrl";
            renderCtrl.Profile = OpenTK.Windowing.Common.ContextProfile.Core;
            renderCtrl.SharedContext = null;
            renderCtrl.Size = new Size(1080, 720);
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
            Controls.Add(renderCtrl);
            Controls.Add(panel1);
            Controls.Add(statusLbl);
            Controls.Add(messageListView);
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
        private TreeView sceneTree;
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
        private GroupBox groupBox1;
        private ContextMenuStrip sceneTreeCtxtMenu;
        private ToolStripMenuItem deleteToolStripMenuItem;
        private GFTool.RenderControl_WinForms.RenderControl renderCtrl;
    }
}
