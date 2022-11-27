namespace GFTool.TrinityExplorer
{
    partial class FileSystemForm
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
            this.components = new System.ComponentModel.Container();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openFileDescriptorToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveFileDescriptorAsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openRomFSFolderToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.viewToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.advancedToggle = new System.Windows.Forms.ToolStripMenuItem();
            this.showUnhashedFilesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.helpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.aboutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.fileView = new System.Windows.Forms.TreeView();
            this.helpProvider1 = new System.Windows.Forms.HelpProvider();
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.statusLbl = new System.Windows.Forms.Label();
            this.treeContext = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.saveFileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.markForLayeredFSToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.advancedPanel = new System.Windows.Forms.Panel();
            this.basicPanel = new System.Windows.Forms.Panel();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.authorLbl = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.modNameLbl = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.applyModsBut = new System.Windows.Forms.Button();
            this.modList = new System.Windows.Forms.CheckedListBox();
            this.modOrderDown = new System.Windows.Forms.Button();
            this.modOrderUp = new System.Windows.Forms.Button();
            this.addMod = new System.Windows.Forms.Button();
            this.menuStrip1.SuspendLayout();
            this.treeContext.SuspendLayout();
            this.advancedPanel.SuspendLayout();
            this.basicPanel.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.viewToolStripMenuItem,
            this.helpToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(581, 24);
            this.menuStrip1.TabIndex = 2;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openFileDescriptorToolStripMenuItem,
            this.saveFileDescriptorAsToolStripMenuItem,
            this.openRomFSFolderToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // openFileDescriptorToolStripMenuItem
            // 
            this.openFileDescriptorToolStripMenuItem.Name = "openFileDescriptorToolStripMenuItem";
            this.openFileDescriptorToolStripMenuItem.Size = new System.Drawing.Size(181, 22);
            this.openFileDescriptorToolStripMenuItem.Text = "Open File Descriptor";
            this.openFileDescriptorToolStripMenuItem.Click += new System.EventHandler(this.openFileDescriptorToolStripMenuItem_Click);
            // 
            // saveFileDescriptorAsToolStripMenuItem
            // 
            this.saveFileDescriptorAsToolStripMenuItem.Name = "saveFileDescriptorAsToolStripMenuItem";
            this.saveFileDescriptorAsToolStripMenuItem.Size = new System.Drawing.Size(181, 22);
            this.saveFileDescriptorAsToolStripMenuItem.Text = "Save File Descriptor";
            this.saveFileDescriptorAsToolStripMenuItem.Click += new System.EventHandler(this.saveFileDescriptorAsToolStripMenuItem_Click);
            // 
            // openRomFSFolderToolStripMenuItem
            // 
            this.openRomFSFolderToolStripMenuItem.Name = "openRomFSFolderToolStripMenuItem";
            this.openRomFSFolderToolStripMenuItem.Size = new System.Drawing.Size(181, 22);
            this.openRomFSFolderToolStripMenuItem.Text = "Open RomFS Folder";
            this.openRomFSFolderToolStripMenuItem.Click += new System.EventHandler(this.openRomFSFolderToolStripMenuItem_Click);
            // 
            // viewToolStripMenuItem
            // 
            this.viewToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.advancedToggle,
            this.showUnhashedFilesToolStripMenuItem});
            this.viewToolStripMenuItem.Name = "viewToolStripMenuItem";
            this.viewToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
            this.viewToolStripMenuItem.Text = "View";
            // 
            // advancedToggle
            // 
            this.advancedToggle.CheckOnClick = true;
            this.advancedToggle.Name = "advancedToggle";
            this.advancedToggle.Size = new System.Drawing.Size(185, 22);
            this.advancedToggle.Text = "Advanced View";
            this.advancedToggle.CheckedChanged += new System.EventHandler(this.advancedViewToolStripMenuItem_CheckedChanged);
            // 
            // showUnhashedFilesToolStripMenuItem
            // 
            this.showUnhashedFilesToolStripMenuItem.Enabled = false;
            this.showUnhashedFilesToolStripMenuItem.Name = "showUnhashedFilesToolStripMenuItem";
            this.showUnhashedFilesToolStripMenuItem.Size = new System.Drawing.Size(185, 22);
            this.showUnhashedFilesToolStripMenuItem.Text = "Show Unhashed Files";
            this.showUnhashedFilesToolStripMenuItem.Click += new System.EventHandler(this.showUnhashedFilesToolStripMenuItem_Click);
            // 
            // helpToolStripMenuItem
            // 
            this.helpToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.aboutToolStripMenuItem});
            this.helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            this.helpToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
            this.helpToolStripMenuItem.Text = "Help";
            // 
            // aboutToolStripMenuItem
            // 
            this.aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
            this.aboutToolStripMenuItem.Size = new System.Drawing.Size(107, 22);
            this.aboutToolStripMenuItem.Text = "About";
            this.aboutToolStripMenuItem.Click += new System.EventHandler(this.aboutToolStripMenuItem_Click);
            // 
            // fileView
            // 
            this.fileView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.fileView.Location = new System.Drawing.Point(3, 3);
            this.fileView.Name = "fileView";
            this.fileView.Size = new System.Drawing.Size(551, 406);
            this.fileView.TabIndex = 3;
            this.fileView.MouseUp += new System.Windows.Forms.MouseEventHandler(this.fileView_MouseUp);
            // 
            // progressBar1
            // 
            this.progressBar1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.progressBar1.Location = new System.Drawing.Point(312, 445);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(257, 15);
            this.progressBar1.TabIndex = 4;
            // 
            // statusLbl
            // 
            this.statusLbl.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.statusLbl.AutoSize = true;
            this.statusLbl.Location = new System.Drawing.Point(12, 445);
            this.statusLbl.Name = "statusLbl";
            this.statusLbl.Size = new System.Drawing.Size(39, 15);
            this.statusLbl.TabIndex = 5;
            this.statusLbl.Text = "Ready";
            // 
            // treeContext
            // 
            this.treeContext.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.saveFileToolStripMenuItem,
            this.markForLayeredFSToolStripMenuItem});
            this.treeContext.Name = "treeContext";
            this.treeContext.Size = new System.Drawing.Size(176, 48);
            // 
            // saveFileToolStripMenuItem
            // 
            this.saveFileToolStripMenuItem.Name = "saveFileToolStripMenuItem";
            this.saveFileToolStripMenuItem.Size = new System.Drawing.Size(175, 22);
            this.saveFileToolStripMenuItem.Text = "Save File";
            this.saveFileToolStripMenuItem.Click += new System.EventHandler(this.saveFileToolStripMenuItem_Click);
            // 
            // markForLayeredFSToolStripMenuItem
            // 
            this.markForLayeredFSToolStripMenuItem.Name = "markForLayeredFSToolStripMenuItem";
            this.markForLayeredFSToolStripMenuItem.Size = new System.Drawing.Size(175, 22);
            this.markForLayeredFSToolStripMenuItem.Text = "Mark for LayeredFS";
            this.markForLayeredFSToolStripMenuItem.Click += new System.EventHandler(this.markForLayeredFSToolStripMenuItem_Click);
            // 
            // advancedPanel
            // 
            this.advancedPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.advancedPanel.Controls.Add(this.fileView);
            this.advancedPanel.Enabled = false;
            this.advancedPanel.Location = new System.Drawing.Point(12, 27);
            this.advancedPanel.Name = "advancedPanel";
            this.advancedPanel.Size = new System.Drawing.Size(557, 412);
            this.advancedPanel.TabIndex = 6;
            this.advancedPanel.Visible = false;
            // 
            // basicPanel
            // 
            this.basicPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.basicPanel.Controls.Add(this.groupBox1);
            this.basicPanel.Controls.Add(this.applyModsBut);
            this.basicPanel.Controls.Add(this.modList);
            this.basicPanel.Controls.Add(this.modOrderDown);
            this.basicPanel.Controls.Add(this.modOrderUp);
            this.basicPanel.Controls.Add(this.addMod);
            this.basicPanel.Location = new System.Drawing.Point(12, 27);
            this.basicPanel.Name = "basicPanel";
            this.basicPanel.Size = new System.Drawing.Size(557, 412);
            this.basicPanel.TabIndex = 7;
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(this.textBox1);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.authorLbl);
            this.groupBox1.Controls.Add(this.label4);
            this.groupBox1.Controls.Add(this.modNameLbl);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Location = new System.Drawing.Point(229, 0);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(328, 412);
            this.groupBox1.TabIndex = 6;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Mod Info";
            // 
            // textBox1
            // 
            this.textBox1.AcceptsReturn = true;
            this.textBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBox1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox1.Location = new System.Drawing.Point(16, 82);
            this.textBox1.Multiline = true;
            this.textBox1.Name = "textBox1";
            this.textBox1.ReadOnly = true;
            this.textBox1.Size = new System.Drawing.Size(304, 322);
            this.textBox1.TabIndex = 3;
            this.textBox1.Text = "None";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(16, 64);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(70, 15);
            this.label2.TabIndex = 2;
            this.label2.Text = "Description:";
            // 
            // authorLbl
            // 
            this.authorLbl.AutoSize = true;
            this.authorLbl.Location = new System.Drawing.Point(64, 44);
            this.authorLbl.Name = "authorLbl";
            this.authorLbl.Size = new System.Drawing.Size(0, 15);
            this.authorLbl.TabIndex = 5;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(16, 44);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(47, 15);
            this.label4.TabIndex = 4;
            this.label4.Text = "Author:";
            // 
            // modNameLbl
            // 
            this.modNameLbl.AutoSize = true;
            this.modNameLbl.Location = new System.Drawing.Point(64, 24);
            this.modNameLbl.Name = "modNameLbl";
            this.modNameLbl.Size = new System.Drawing.Size(0, 15);
            this.modNameLbl.TabIndex = 1;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(16, 24);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(42, 15);
            this.label1.TabIndex = 0;
            this.label1.Text = "Name:";
            // 
            // applyModsBut
            // 
            this.applyModsBut.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.applyModsBut.Enabled = false;
            this.applyModsBut.Location = new System.Drawing.Point(3, 364);
            this.applyModsBut.Name = "applyModsBut";
            this.applyModsBut.Size = new System.Drawing.Size(184, 40);
            this.applyModsBut.TabIndex = 5;
            this.applyModsBut.Text = "Apply mods";
            this.applyModsBut.UseVisualStyleBackColor = true;
            this.applyModsBut.Click += new System.EventHandler(this.applyModsBut_Click);
            // 
            // modList
            // 
            this.modList.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.modList.FormattingEnabled = true;
            this.modList.Location = new System.Drawing.Point(3, 0);
            this.modList.Name = "modList";
            this.modList.Size = new System.Drawing.Size(184, 310);
            this.modList.TabIndex = 4;
            this.modList.SelectedIndexChanged += new System.EventHandler(this.modList_SelectedIndexChanged);
            // 
            // modOrderDown
            // 
            this.modOrderDown.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.modOrderDown.Location = new System.Drawing.Point(192, 32);
            this.modOrderDown.Name = "modOrderDown";
            this.modOrderDown.Size = new System.Drawing.Size(32, 32);
            this.modOrderDown.TabIndex = 3;
            this.modOrderDown.Text = "↓";
            this.modOrderDown.UseVisualStyleBackColor = true;
            this.modOrderDown.Click += new System.EventHandler(this.modOrderDown_Click);
            // 
            // modOrderUp
            // 
            this.modOrderUp.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.modOrderUp.Location = new System.Drawing.Point(192, 0);
            this.modOrderUp.Name = "modOrderUp";
            this.modOrderUp.Size = new System.Drawing.Size(32, 32);
            this.modOrderUp.TabIndex = 2;
            this.modOrderUp.Text = "↑";
            this.modOrderUp.UseVisualStyleBackColor = true;
            this.modOrderUp.Click += new System.EventHandler(this.modOrderUp_Click);
            // 
            // addMod
            // 
            this.addMod.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.addMod.Location = new System.Drawing.Point(3, 317);
            this.addMod.Name = "addMod";
            this.addMod.Size = new System.Drawing.Size(184, 40);
            this.addMod.TabIndex = 1;
            this.addMod.Text = "Add mod";
            this.addMod.UseVisualStyleBackColor = true;
            this.addMod.Click += new System.EventHandler(this.addMod_Click);
            // 
            // FileSystemForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(581, 471);
            this.Controls.Add(this.basicPanel);
            this.Controls.Add(this.statusLbl);
            this.Controls.Add(this.progressBar1);
            this.Controls.Add(this.menuStrip1);
            this.Controls.Add(this.advancedPanel);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "FileSystemForm";
            this.Text = "Trinity Mod Loader";
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.treeContext.ResumeLayout(false);
            this.advancedPanel.ResumeLayout(false);
            this.basicPanel.ResumeLayout(false);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private MenuStrip menuStrip1;
        private ToolStripMenuItem fileToolStripMenuItem;
        private ToolStripMenuItem openFileDescriptorToolStripMenuItem;
        private TreeView fileView;
        private HelpProvider helpProvider1;
        private ProgressBar progressBar1;
        private Label statusLbl;
        private ContextMenuStrip treeContext;
        private ToolStripMenuItem saveFileToolStripMenuItem;
        private ToolStripMenuItem markForLayeredFSToolStripMenuItem;
        private ToolStripMenuItem saveFileDescriptorAsToolStripMenuItem;
        private ToolStripMenuItem viewToolStripMenuItem;
        private ToolStripMenuItem advancedToggle;
        private Panel advancedPanel;
        private Panel basicPanel;
        private Button addMod;
        private Button modOrderDown;
        private Button modOrderUp;
        private CheckedListBox modList;
        private Button applyModsBut;
        private GroupBox groupBox1;
        private Label modNameLbl;
        private Label label1;
        private Label label2;
        private TextBox textBox1;
        private Label authorLbl;
        private Label label4;
        private ToolStripMenuItem showUnhashedFilesToolStripMenuItem;
        private ToolStripMenuItem helpToolStripMenuItem;
        private ToolStripMenuItem aboutToolStripMenuItem;
        private ToolStripMenuItem openRomFSFolderToolStripMenuItem;
    }
}