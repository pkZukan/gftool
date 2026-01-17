namespace TrinityUikitEditor
{
    partial class UikitEditor
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
            menuStrip1 = new MenuStrip();
            fileToolStripMenuItem = new ToolStripMenuItem();
            openToolStripMenuItem = new ToolStripMenuItem();
            tableLayoutPanel1 = new TableLayoutPanel();
            sceneView = new TreeView();
            tableLayoutPanel2 = new TableLayoutPanel();
            messageListView = new ListView();
            columnHeader1 = new ColumnHeader();
            tableLayoutPanel3 = new TableLayoutPanel();
            renderCtrl = new GFTool.RenderControl_WinForms.RenderControl();
            propertiesGroup = new GroupBox();
            InfoBox = new TextBox();
            statusLbl = new Label();
            menuStrip1.SuspendLayout();
            tableLayoutPanel1.SuspendLayout();
            tableLayoutPanel2.SuspendLayout();
            tableLayoutPanel3.SuspendLayout();
            propertiesGroup.SuspendLayout();
            SuspendLayout();
            // 
            // menuStrip1
            // 
            menuStrip1.Items.AddRange(new ToolStripItem[] { fileToolStripMenuItem });
            menuStrip1.Location = new Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Size = new Size(1793, 24);
            menuStrip1.TabIndex = 8;
            menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            fileToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { openToolStripMenuItem });
            fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            fileToolStripMenuItem.Size = new Size(37, 20);
            fileToolStripMenuItem.Text = "File";
            // 
            // openToolStripMenuItem
            // 
            openToolStripMenuItem.Name = "openToolStripMenuItem";
            openToolStripMenuItem.Size = new Size(103, 22);
            openToolStripMenuItem.Text = "Open";
            openToolStripMenuItem.Click += openToolStripMenuItem_Click;
            // 
            // tableLayoutPanel1
            // 
            tableLayoutPanel1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            tableLayoutPanel1.ColumnCount = 2;
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 18.00487F));
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 81.99513F));
            tableLayoutPanel1.Controls.Add(sceneView, 0, 0);
            tableLayoutPanel1.Controls.Add(tableLayoutPanel2, 1, 0);
            tableLayoutPanel1.Location = new Point(9, 24);
            tableLayoutPanel1.Margin = new Padding(0);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 1;
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel1.Size = new Size(1775, 842);
            tableLayoutPanel1.TabIndex = 9;
            // 
            // sceneView
            // 
            sceneView.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            sceneView.BorderStyle = BorderStyle.FixedSingle;
            sceneView.Location = new Point(3, 3);
            sceneView.Name = "sceneView";
            sceneView.Size = new Size(313, 836);
            sceneView.TabIndex = 1;
            sceneView.MouseUp += sceneView_MouseUp;
            // 
            // tableLayoutPanel2
            // 
            tableLayoutPanel2.ColumnCount = 1;
            tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle());
            tableLayoutPanel2.Controls.Add(messageListView, 0, 1);
            tableLayoutPanel2.Controls.Add(tableLayoutPanel3, 0, 0);
            tableLayoutPanel2.Dock = DockStyle.Fill;
            tableLayoutPanel2.Location = new Point(319, 0);
            tableLayoutPanel2.Margin = new Padding(0);
            tableLayoutPanel2.Name = "tableLayoutPanel2";
            tableLayoutPanel2.RowCount = 2;
            tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Absolute, 726F));
            tableLayoutPanel2.RowStyles.Add(new RowStyle());
            tableLayoutPanel2.Size = new Size(1456, 842);
            tableLayoutPanel2.TabIndex = 2;
            // 
            // messageListView
            // 
            messageListView.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            messageListView.BorderStyle = BorderStyle.FixedSingle;
            messageListView.Columns.AddRange(new ColumnHeader[] { columnHeader1 });
            messageListView.FullRowSelect = true;
            messageListView.GridLines = true;
            messageListView.LabelWrap = false;
            messageListView.Location = new Point(3, 729);
            messageListView.Name = "messageListView";
            messageListView.Size = new Size(1450, 110);
            messageListView.TabIndex = 4;
            messageListView.UseCompatibleStateImageBehavior = false;
            messageListView.View = View.Details;
            // 
            // columnHeader1
            // 
            columnHeader1.Text = "Messages";
            columnHeader1.Width = 1000;
            // 
            // tableLayoutPanel3
            // 
            tableLayoutPanel3.ColumnCount = 2;
            tableLayoutPanel3.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 1086F));
            tableLayoutPanel3.ColumnStyles.Add(new ColumnStyle());
            tableLayoutPanel3.Controls.Add(renderCtrl, 0, 0);
            tableLayoutPanel3.Controls.Add(propertiesGroup, 1, 0);
            tableLayoutPanel3.Dock = DockStyle.Fill;
            tableLayoutPanel3.Location = new Point(0, 0);
            tableLayoutPanel3.Margin = new Padding(0);
            tableLayoutPanel3.Name = "tableLayoutPanel3";
            tableLayoutPanel3.RowCount = 1;
            tableLayoutPanel3.RowStyles.Add(new RowStyle());
            tableLayoutPanel3.Size = new Size(1456, 726);
            tableLayoutPanel3.TabIndex = 0;
            // 
            // renderCtrl
            // 
            renderCtrl.API = OpenTK.Windowing.Common.ContextAPI.OpenGL;
            renderCtrl.APIVersion = new Version(3, 3, 0, 0);
            renderCtrl.Dock = DockStyle.Fill;
            renderCtrl.Flags = OpenTK.Windowing.Common.ContextFlags.Default;
            renderCtrl.IsEventDriven = true;
            renderCtrl.Location = new Point(3, 3);
            renderCtrl.Name = "renderCtrl";
            renderCtrl.Profile = OpenTK.Windowing.Common.ContextProfile.Core;
            renderCtrl.SharedContext = null;
            renderCtrl.Size = new Size(1080, 720);
            renderCtrl.TabIndex = 6;
            // 
            // propertiesGroup
            // 
            propertiesGroup.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            propertiesGroup.Controls.Add(InfoBox);
            propertiesGroup.Location = new Point(1089, 3);
            propertiesGroup.Name = "propertiesGroup";
            propertiesGroup.Size = new Size(364, 720);
            propertiesGroup.TabIndex = 5;
            propertiesGroup.TabStop = false;
            propertiesGroup.Text = "Properties";
            // 
            // InfoBox
            // 
            InfoBox.BackColor = SystemColors.Control;
            InfoBox.BorderStyle = BorderStyle.None;
            InfoBox.Dock = DockStyle.Fill;
            InfoBox.Location = new Point(3, 19);
            InfoBox.Multiline = true;
            InfoBox.Name = "InfoBox";
            InfoBox.ReadOnly = true;
            InfoBox.Size = new Size(358, 698);
            InfoBox.TabIndex = 0;
            // 
            // statusLbl
            // 
            statusLbl.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            statusLbl.Location = new Point(12, 866);
            statusLbl.Name = "statusLbl";
            statusLbl.Size = new Size(1080, 14);
            statusLbl.TabIndex = 10;
            statusLbl.Text = "label1";
            // 
            // UikitEditor
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1793, 889);
            Controls.Add(statusLbl);
            Controls.Add(tableLayoutPanel1);
            Controls.Add(menuStrip1);
            MainMenuStrip = menuStrip1;
            Name = "UikitEditor";
            Text = "Trinity Uikit Editor";
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            tableLayoutPanel1.ResumeLayout(false);
            tableLayoutPanel2.ResumeLayout(false);
            tableLayoutPanel3.ResumeLayout(false);
            propertiesGroup.ResumeLayout(false);
            propertiesGroup.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private MenuStrip menuStrip1;
        private ToolStripMenuItem fileToolStripMenuItem;
        private ToolStripMenuItem openToolStripMenuItem;
        private TableLayoutPanel tableLayoutPanel1;
        private TreeView sceneView;
        private TableLayoutPanel tableLayoutPanel2;
        private ListView messageListView;
        private ColumnHeader columnHeader1;
        private TableLayoutPanel tableLayoutPanel3;
        private GFTool.RenderControl_WinForms.RenderControl renderCtrl;
        private GroupBox propertiesGroup;
        private TextBox InfoBox;
        private Label statusLbl;
    }
}
