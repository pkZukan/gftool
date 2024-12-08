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
            glCtxt = new OpenTK.GLControl.GLControl();
            menuStrip1 = new MenuStrip();
            fileToolStripMenuItem = new ToolStripMenuItem();
            openToolStripMenuItem = new ToolStripMenuItem();
            importToolStripMenuItem = new ToolStripMenuItem();
            viewToolStripMenuItem = new ToolStripMenuItem();
            wireframeToolStripMenuItem = new ToolStripMenuItem();
            messageListView = new ListView();
            columnHeader1 = new ColumnHeader();
            keyTimer = new System.Windows.Forms.Timer(components);
            statusLbl = new Label();
            panel1 = new Panel();
            splitContainer1 = new SplitContainer();
            sceneTree = new TreeView();
            modelProperties = new TabControl();
            tabPage1 = new TabPage();
            numericUpDown7 = new NumericUpDown();
            numericUpDown8 = new NumericUpDown();
            numericUpDown9 = new NumericUpDown();
            label3 = new Label();
            numericUpDown4 = new NumericUpDown();
            numericUpDown5 = new NumericUpDown();
            numericUpDown6 = new NumericUpDown();
            label2 = new Label();
            numericUpDown3 = new NumericUpDown();
            numericUpDown2 = new NumericUpDown();
            numericUpDown1 = new NumericUpDown();
            label1 = new Label();
            tabPage2 = new TabPage();
            menuStrip1.SuspendLayout();
            panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            modelProperties.SuspendLayout();
            tabPage1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numericUpDown7).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDown8).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDown9).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDown4).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDown5).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDown6).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDown3).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDown2).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDown1).BeginInit();
            SuspendLayout();
            // 
            // glCtxt
            // 
            glCtxt.API = OpenTK.Windowing.Common.ContextAPI.OpenGL;
            glCtxt.APIVersion = new Version(4, 2, 0, 0);
            glCtxt.Flags = OpenTK.Windowing.Common.ContextFlags.Default;
            glCtxt.IsEventDriven = true;
            glCtxt.Location = new Point(12, 27);
            glCtxt.Name = "glCtxt";
            glCtxt.Profile = OpenTK.Windowing.Common.ContextProfile.Core;
            glCtxt.SharedContext = null;
            glCtxt.Size = new Size(1080, 720);
            glCtxt.TabIndex = 0;
            glCtxt.Load += glCtxt_Load;
            glCtxt.Paint += glCtxt_Paint;
            glCtxt.MouseMove += glCtxt_MouseMove;
            glCtxt.Resize += glCtxt_Resize;
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
            wireframeToolStripMenuItem.Size = new Size(180, 22);
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
            // keyTimer
            // 
            keyTimer.Enabled = true;
            keyTimer.Interval = 10;
            keyTimer.Tick += keyTimer_Tick;
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
            tabPage1.Controls.Add(numericUpDown7);
            tabPage1.Controls.Add(numericUpDown8);
            tabPage1.Controls.Add(numericUpDown9);
            tabPage1.Controls.Add(label3);
            tabPage1.Controls.Add(numericUpDown4);
            tabPage1.Controls.Add(numericUpDown5);
            tabPage1.Controls.Add(numericUpDown6);
            tabPage1.Controls.Add(label2);
            tabPage1.Controls.Add(numericUpDown3);
            tabPage1.Controls.Add(numericUpDown2);
            tabPage1.Controls.Add(numericUpDown1);
            tabPage1.Controls.Add(label1);
            tabPage1.Location = new Point(4, 24);
            tabPage1.Name = "tabPage1";
            tabPage1.Padding = new Padding(3);
            tabPage1.Size = new Size(350, 413);
            tabPage1.TabIndex = 0;
            tabPage1.Text = "Object";
            tabPage1.UseVisualStyleBackColor = true;
            // 
            // numericUpDown7
            // 
            numericUpDown7.Location = new Point(226, 81);
            numericUpDown7.Name = "numericUpDown7";
            numericUpDown7.Size = new Size(69, 23);
            numericUpDown7.TabIndex = 11;
            // 
            // numericUpDown8
            // 
            numericUpDown8.Location = new Point(151, 81);
            numericUpDown8.Name = "numericUpDown8";
            numericUpDown8.Size = new Size(69, 23);
            numericUpDown8.TabIndex = 10;
            // 
            // numericUpDown9
            // 
            numericUpDown9.Location = new Point(76, 81);
            numericUpDown9.Name = "numericUpDown9";
            numericUpDown9.Size = new Size(69, 23);
            numericUpDown9.TabIndex = 9;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(36, 83);
            label3.Name = "label3";
            label3.Size = new Size(34, 15);
            label3.TabIndex = 8;
            label3.Text = "Scale";
            // 
            // numericUpDown4
            // 
            numericUpDown4.Location = new Point(226, 52);
            numericUpDown4.Name = "numericUpDown4";
            numericUpDown4.Size = new Size(69, 23);
            numericUpDown4.TabIndex = 7;
            // 
            // numericUpDown5
            // 
            numericUpDown5.Location = new Point(151, 52);
            numericUpDown5.Name = "numericUpDown5";
            numericUpDown5.Size = new Size(69, 23);
            numericUpDown5.TabIndex = 6;
            // 
            // numericUpDown6
            // 
            numericUpDown6.Location = new Point(76, 52);
            numericUpDown6.Name = "numericUpDown6";
            numericUpDown6.Size = new Size(69, 23);
            numericUpDown6.TabIndex = 5;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(18, 54);
            label2.Name = "label2";
            label2.Size = new Size(52, 15);
            label2.TabIndex = 4;
            label2.Text = "Rotation";
            // 
            // numericUpDown3
            // 
            numericUpDown3.Location = new Point(226, 23);
            numericUpDown3.Name = "numericUpDown3";
            numericUpDown3.Size = new Size(69, 23);
            numericUpDown3.TabIndex = 3;
            // 
            // numericUpDown2
            // 
            numericUpDown2.Location = new Point(151, 23);
            numericUpDown2.Name = "numericUpDown2";
            numericUpDown2.Size = new Size(69, 23);
            numericUpDown2.TabIndex = 2;
            // 
            // numericUpDown1
            // 
            numericUpDown1.Location = new Point(76, 23);
            numericUpDown1.Name = "numericUpDown1";
            numericUpDown1.Size = new Size(69, 23);
            numericUpDown1.TabIndex = 1;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(6, 25);
            label1.Name = "label1";
            label1.Size = new Size(64, 15);
            label1.TabIndex = 0;
            label1.Text = "Translation";
            // 
            // tabPage2
            // 
            tabPage2.Location = new Point(4, 24);
            tabPage2.Name = "tabPage2";
            tabPage2.Padding = new Padding(3);
            tabPage2.Size = new Size(350, 413);
            tabPage2.TabIndex = 1;
            tabPage2.Text = "Materials";
            tabPage2.UseVisualStyleBackColor = true;
            // 
            // ModelViewerForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1474, 924);
            Controls.Add(panel1);
            Controls.Add(statusLbl);
            Controls.Add(messageListView);
            Controls.Add(glCtxt);
            Controls.Add(menuStrip1);
            MainMenuStrip = menuStrip1;
            Name = "ModelViewerForm";
            Text = "Trinity Model Viewer";
            KeyDown += glCtxt_KeyDown;
            KeyUp += glCtxt_KeyUp;
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            panel1.ResumeLayout(false);
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            modelProperties.ResumeLayout(false);
            tabPage1.ResumeLayout(false);
            tabPage1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numericUpDown7).EndInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDown8).EndInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDown9).EndInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDown4).EndInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDown5).EndInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDown6).EndInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDown3).EndInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDown2).EndInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDown1).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private OpenTK.GLControl.GLControl glCtxt;
        private MenuStrip menuStrip1;
        private ToolStripMenuItem fileToolStripMenuItem;
        private ToolStripMenuItem openToolStripMenuItem;
        private ListView messageListView;
        private ColumnHeader columnHeader1;
        private System.Windows.Forms.Timer keyTimer;
        private Label statusLbl;
        private Panel panel1;
        private SplitContainer splitContainer1;
        private TreeView sceneTree;
        private TabControl modelProperties;
        private TabPage tabPage1;
        private TabPage tabPage2;
        private NumericUpDown numericUpDown3;
        private NumericUpDown numericUpDown2;
        private NumericUpDown numericUpDown1;
        private Label label1;
        private NumericUpDown numericUpDown7;
        private NumericUpDown numericUpDown8;
        private NumericUpDown numericUpDown9;
        private Label label3;
        private NumericUpDown numericUpDown4;
        private NumericUpDown numericUpDown5;
        private NumericUpDown numericUpDown6;
        private Label label2;
        private ToolStripMenuItem importToolStripMenuItem;
        private ToolStripMenuItem viewToolStripMenuItem;
        private ToolStripMenuItem wireframeToolStripMenuItem;
    }
}
