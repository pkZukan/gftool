namespace Trinity
{
    partial class TrinityMainWindow
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
            openFileDescriptorToolStripMenuItem = new ToolStripMenuItem();
            saveFileDescriptorAsToolStripMenuItem = new ToolStripMenuItem();
            openRomFSFolderToolStripMenuItem = new ToolStripMenuItem();
            setOutputFolderToolStripMenuItem = new ToolStripMenuItem();
            optionsToolStripMenuItem = new ToolStripMenuItem();
            disableAutoLoad = new ToolStripMenuItem();
            viewToolStripMenuItem = new ToolStripMenuItem();
            advancedToggle = new ToolStripMenuItem();
            showUnhashedFilesToolStripMenuItem = new ToolStripMenuItem();
            helpToolStripMenuItem = new ToolStripMenuItem();
            aboutToolStripMenuItem = new ToolStripMenuItem();
            getLatestHashes = new ToolStripMenuItem();
            fileView = new TreeView();
            helpProvider1 = new HelpProvider();
            progressBar1 = new ProgressBar();
            statusLbl = new Label();
            treeContext = new ContextMenuStrip(components);
            saveFileToolStripMenuItem = new ToolStripMenuItem();
            markForLayeredFSToolStripMenuItem = new ToolStripMenuItem();
            advancedPanel = new Panel();
            basicPanel = new Panel();
            addFolderMod = new Button();
            groupBox1 = new GroupBox();
            versionLbl = new Label();
            label4 = new Label();
            modDescriptionBox = new TextBox();
            label2 = new Label();
            modNameLbl = new Label();
            label1 = new Label();
            applyModsBut = new Button();
            modList = new CheckedListBox();
            modOrderDown = new Button();
            modOrderUp = new Button();
            addZipMod = new Button();
            basicContext = new ContextMenuStrip(components);
            deleteModBut = new ToolStripMenuItem();
            menuStrip1.SuspendLayout();
            treeContext.SuspendLayout();
            advancedPanel.SuspendLayout();
            basicPanel.SuspendLayout();
            groupBox1.SuspendLayout();
            basicContext.SuspendLayout();
            SuspendLayout();
            // 
            // menuStrip1
            // 
            menuStrip1.ImageScalingSize = new Size(24, 24);
            menuStrip1.Items.AddRange(new ToolStripItem[] { fileToolStripMenuItem, optionsToolStripMenuItem, viewToolStripMenuItem, helpToolStripMenuItem });
            menuStrip1.Location = new Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Padding = new Padding(9, 3, 0, 3);
            menuStrip1.Size = new Size(830, 35);
            menuStrip1.TabIndex = 2;
            menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            fileToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { openFileDescriptorToolStripMenuItem, saveFileDescriptorAsToolStripMenuItem, openRomFSFolderToolStripMenuItem, setOutputFolderToolStripMenuItem });
            fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            fileToolStripMenuItem.Size = new Size(54, 29);
            fileToolStripMenuItem.Text = "File";
            // 
            // openFileDescriptorToolStripMenuItem
            // 
            openFileDescriptorToolStripMenuItem.Name = "openFileDescriptorToolStripMenuItem";
            openFileDescriptorToolStripMenuItem.Size = new Size(276, 34);
            openFileDescriptorToolStripMenuItem.Text = "Open File Descriptor";
            openFileDescriptorToolStripMenuItem.Click += openFileDescriptorToolStripMenuItem_Click;
            // 
            // saveFileDescriptorAsToolStripMenuItem
            // 
            saveFileDescriptorAsToolStripMenuItem.Name = "saveFileDescriptorAsToolStripMenuItem";
            saveFileDescriptorAsToolStripMenuItem.Size = new Size(276, 34);
            saveFileDescriptorAsToolStripMenuItem.Text = "Save File Descriptor";
            saveFileDescriptorAsToolStripMenuItem.Click += saveFileDescriptorAsToolStripMenuItem_Click;
            // 
            // openRomFSFolderToolStripMenuItem
            // 
            openRomFSFolderToolStripMenuItem.Name = "openRomFSFolderToolStripMenuItem";
            openRomFSFolderToolStripMenuItem.Size = new Size(276, 34);
            openRomFSFolderToolStripMenuItem.Text = "Open RomFS Folder";
            openRomFSFolderToolStripMenuItem.Click += openRomFSFolderToolStripMenuItem_Click;
            // 
            // setOutputFolderToolStripMenuItem
            // 
            setOutputFolderToolStripMenuItem.Name = "setOutputFolderToolStripMenuItem";
            setOutputFolderToolStripMenuItem.Size = new Size(276, 34);
            setOutputFolderToolStripMenuItem.Text = "Set Output Folder";
            setOutputFolderToolStripMenuItem.Click += setOutputFolderToolStripMenuItem_Click;
            // 
            // optionsToolStripMenuItem
            // 
            optionsToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { disableAutoLoad });
            optionsToolStripMenuItem.Name = "optionsToolStripMenuItem";
            optionsToolStripMenuItem.Size = new Size(92, 29);
            optionsToolStripMenuItem.Text = "Options";
            // 
            // disableAutoLoad
            // 
            disableAutoLoad.CheckOnClick = true;
            disableAutoLoad.Name = "disableAutoLoad";
            disableAutoLoad.Size = new Size(308, 34);
            disableAutoLoad.Text = "Disable TRPFD Autoload";
            disableAutoLoad.CheckedChanged += disableTRPFDAutoloadToolStripMenuItem_CheckedChanged;
            // 
            // viewToolStripMenuItem
            // 
            viewToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { advancedToggle, showUnhashedFilesToolStripMenuItem });
            viewToolStripMenuItem.Name = "viewToolStripMenuItem";
            viewToolStripMenuItem.Size = new Size(65, 29);
            viewToolStripMenuItem.Text = "View";
            // 
            // advancedToggle
            // 
            advancedToggle.CheckOnClick = true;
            advancedToggle.Name = "advancedToggle";
            advancedToggle.Size = new Size(281, 34);
            advancedToggle.Text = "Tree View";
            advancedToggle.CheckedChanged += advancedViewToolStripMenuItem_CheckedChanged;
            // 
            // showUnhashedFilesToolStripMenuItem
            // 
            showUnhashedFilesToolStripMenuItem.Enabled = false;
            showUnhashedFilesToolStripMenuItem.Name = "showUnhashedFilesToolStripMenuItem";
            showUnhashedFilesToolStripMenuItem.Size = new Size(281, 34);
            showUnhashedFilesToolStripMenuItem.Text = "Show Unhashed Files";
            showUnhashedFilesToolStripMenuItem.Click += showUnhashedFilesToolStripMenuItem_Click;
            // 
            // helpToolStripMenuItem
            // 
            helpToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { aboutToolStripMenuItem, getLatestHashes });
            helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            helpToolStripMenuItem.Size = new Size(65, 29);
            helpToolStripMenuItem.Text = "Help";
            // 
            // aboutToolStripMenuItem
            // 
            aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
            aboutToolStripMenuItem.Size = new Size(254, 34);
            aboutToolStripMenuItem.Text = "About";
            aboutToolStripMenuItem.Click += aboutToolStripMenuItem_Click;
            // 
            // getLatestHashes
            // 
            getLatestHashes.Name = "getLatestHashes";
            getLatestHashes.Size = new Size(254, 34);
            getLatestHashes.Text = "Get Latest Hashes";
            getLatestHashes.Click += getLatestHashes_Click;
            // 
            // fileView
            // 
            fileView.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            fileView.Location = new Point(4, 5);
            fileView.Margin = new Padding(4, 5, 4, 5);
            fileView.Name = "fileView";
            fileView.Size = new Size(785, 856);
            fileView.TabIndex = 3;
            fileView.MouseUp += fileView_MouseUp;
            // 
            // progressBar1
            // 
            progressBar1.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            progressBar1.Location = new Point(446, 924);
            progressBar1.Margin = new Padding(4, 5, 4, 5);
            progressBar1.Name = "progressBar1";
            progressBar1.Size = new Size(367, 25);
            progressBar1.TabIndex = 4;
            // 
            // statusLbl
            // 
            statusLbl.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            statusLbl.AutoSize = true;
            statusLbl.Location = new Point(17, 924);
            statusLbl.Margin = new Padding(4, 0, 4, 0);
            statusLbl.Name = "statusLbl";
            statusLbl.Size = new Size(60, 25);
            statusLbl.TabIndex = 5;
            statusLbl.Text = "Ready";
            // 
            // treeContext
            // 
            treeContext.ImageScalingSize = new Size(24, 24);
            treeContext.Items.AddRange(new ToolStripItem[] { saveFileToolStripMenuItem, markForLayeredFSToolStripMenuItem });
            treeContext.Name = "treeContext";
            treeContext.Size = new Size(238, 68);
            // 
            // saveFileToolStripMenuItem
            // 
            saveFileToolStripMenuItem.Name = "saveFileToolStripMenuItem";
            saveFileToolStripMenuItem.Size = new Size(237, 32);
            saveFileToolStripMenuItem.Text = "Save File";
            saveFileToolStripMenuItem.Click += saveFileToolStripMenuItem_Click;
            // 
            // markForLayeredFSToolStripMenuItem
            // 
            markForLayeredFSToolStripMenuItem.Name = "markForLayeredFSToolStripMenuItem";
            markForLayeredFSToolStripMenuItem.Size = new Size(237, 32);
            markForLayeredFSToolStripMenuItem.Text = "Mark for LayeredFS";
            markForLayeredFSToolStripMenuItem.Click += markForLayeredFSToolStripMenuItem_Click;
            // 
            // advancedPanel
            // 
            advancedPanel.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            advancedPanel.Controls.Add(fileView);
            advancedPanel.Enabled = false;
            advancedPanel.Location = new Point(17, 45);
            advancedPanel.Margin = new Padding(4, 5, 4, 5);
            advancedPanel.Name = "advancedPanel";
            advancedPanel.Size = new Size(796, 869);
            advancedPanel.TabIndex = 6;
            advancedPanel.Visible = false;
            // 
            // basicPanel
            // 
            basicPanel.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            basicPanel.Controls.Add(addFolderMod);
            basicPanel.Controls.Add(groupBox1);
            basicPanel.Controls.Add(applyModsBut);
            basicPanel.Controls.Add(modList);
            basicPanel.Controls.Add(modOrderDown);
            basicPanel.Controls.Add(modOrderUp);
            basicPanel.Controls.Add(addZipMod);
            basicPanel.Location = new Point(17, 45);
            basicPanel.Margin = new Padding(4, 5, 4, 5);
            basicPanel.Name = "basicPanel";
            basicPanel.Size = new Size(796, 869);
            basicPanel.TabIndex = 7;
            // 
            // addFolderMod
            // 
            addFolderMod.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            addFolderMod.Location = new Point(4, 717);
            addFolderMod.Margin = new Padding(4, 5, 4, 5);
            addFolderMod.Name = "addFolderMod";
            addFolderMod.Size = new Size(263, 67);
            addFolderMod.TabIndex = 7;
            addFolderMod.Text = "Add mod (folder)";
            addFolderMod.UseVisualStyleBackColor = true;
            addFolderMod.Click += addFolderMod_Click;
            // 
            // groupBox1
            // 
            groupBox1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            groupBox1.Controls.Add(versionLbl);
            groupBox1.Controls.Add(label4);
            groupBox1.Controls.Add(modDescriptionBox);
            groupBox1.Controls.Add(label2);
            groupBox1.Controls.Add(modNameLbl);
            groupBox1.Controls.Add(label1);
            groupBox1.Location = new Point(327, 0);
            groupBox1.Margin = new Padding(4, 5, 4, 5);
            groupBox1.Name = "groupBox1";
            groupBox1.Padding = new Padding(4, 5, 4, 5);
            groupBox1.Size = new Size(469, 869);
            groupBox1.TabIndex = 6;
            groupBox1.TabStop = false;
            groupBox1.Text = "Mod Info";
            // 
            // versionLbl
            // 
            versionLbl.AutoSize = true;
            versionLbl.Location = new Point(100, 72);
            versionLbl.Margin = new Padding(4, 0, 4, 0);
            versionLbl.Name = "versionLbl";
            versionLbl.Size = new Size(0, 25);
            versionLbl.TabIndex = 5;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(23, 72);
            label4.Margin = new Padding(4, 0, 4, 0);
            label4.Name = "label4";
            label4.Size = new Size(74, 25);
            label4.TabIndex = 4;
            label4.Text = "Version:";
            // 
            // modDescriptionBox
            // 
            modDescriptionBox.AcceptsReturn = true;
            modDescriptionBox.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            modDescriptionBox.BorderStyle = BorderStyle.FixedSingle;
            modDescriptionBox.Location = new Point(23, 137);
            modDescriptionBox.Margin = new Padding(4, 5, 4, 5);
            modDescriptionBox.Multiline = true;
            modDescriptionBox.Name = "modDescriptionBox";
            modDescriptionBox.ReadOnly = true;
            modDescriptionBox.Size = new Size(433, 717);
            modDescriptionBox.TabIndex = 3;
            modDescriptionBox.Text = "None";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(23, 107);
            label2.Margin = new Padding(4, 0, 4, 0);
            label2.Name = "label2";
            label2.Size = new Size(106, 25);
            label2.TabIndex = 2;
            label2.Text = "Description:";
            // 
            // modNameLbl
            // 
            modNameLbl.AutoSize = true;
            modNameLbl.Location = new Point(91, 32);
            modNameLbl.Margin = new Padding(4, 0, 4, 0);
            modNameLbl.Name = "modNameLbl";
            modNameLbl.Size = new Size(0, 25);
            modNameLbl.TabIndex = 1;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(23, 32);
            label1.Margin = new Padding(4, 0, 4, 0);
            label1.Name = "label1";
            label1.Size = new Size(63, 25);
            label1.TabIndex = 0;
            label1.Text = "Name:";
            // 
            // applyModsBut
            // 
            applyModsBut.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            applyModsBut.Enabled = false;
            applyModsBut.Location = new Point(4, 794);
            applyModsBut.Margin = new Padding(4, 5, 4, 5);
            applyModsBut.Name = "applyModsBut";
            applyModsBut.Size = new Size(263, 67);
            applyModsBut.TabIndex = 5;
            applyModsBut.Text = "Apply mods";
            applyModsBut.UseVisualStyleBackColor = true;
            applyModsBut.Click += applyModsBut_Click;
            // 
            // modList
            // 
            modList.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
            modList.FormattingEnabled = true;
            modList.Location = new Point(4, 0);
            modList.Margin = new Padding(4, 5, 4, 5);
            modList.Name = "modList";
            modList.Size = new Size(261, 620);
            modList.TabIndex = 4;
            modList.SelectedIndexChanged += modList_SelectedIndexChanged;
            modList.MouseUp += modList_MouseUp;
            // 
            // modOrderDown
            // 
            modOrderDown.Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point);
            modOrderDown.Location = new Point(274, 53);
            modOrderDown.Margin = new Padding(4, 5, 4, 5);
            modOrderDown.Name = "modOrderDown";
            modOrderDown.Size = new Size(46, 53);
            modOrderDown.TabIndex = 3;
            modOrderDown.Text = "↓";
            modOrderDown.UseVisualStyleBackColor = true;
            modOrderDown.Click += modOrderDown_Click;
            // 
            // modOrderUp
            // 
            modOrderUp.Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point);
            modOrderUp.Location = new Point(274, 0);
            modOrderUp.Margin = new Padding(4, 5, 4, 5);
            modOrderUp.Name = "modOrderUp";
            modOrderUp.Size = new Size(46, 53);
            modOrderUp.TabIndex = 2;
            modOrderUp.Text = "↑";
            modOrderUp.UseVisualStyleBackColor = true;
            modOrderUp.Click += modOrderUp_Click;
            // 
            // addZipMod
            // 
            addZipMod.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            addZipMod.Location = new Point(4, 640);
            addZipMod.Margin = new Padding(4, 5, 4, 5);
            addZipMod.Name = "addZipMod";
            addZipMod.Size = new Size(263, 67);
            addZipMod.TabIndex = 1;
            addZipMod.Text = "Add mod (.zip)";
            addZipMod.UseVisualStyleBackColor = true;
            addZipMod.Click += addZipMod_Click;
            // 
            // basicContext
            // 
            basicContext.ImageScalingSize = new Size(24, 24);
            basicContext.Items.AddRange(new ToolStripItem[] { deleteModBut });
            basicContext.Name = "basicContext";
            basicContext.Size = new Size(135, 36);
            // 
            // deleteModBut
            // 
            deleteModBut.Name = "deleteModBut";
            deleteModBut.Size = new Size(134, 32);
            deleteModBut.Text = "Delete";
            deleteModBut.Click += deleteModBut_Click;
            // 
            // TrinityMainWindow
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(830, 967);
            Controls.Add(basicPanel);
            Controls.Add(statusLbl);
            Controls.Add(progressBar1);
            Controls.Add(menuStrip1);
            Controls.Add(advancedPanel);
            MainMenuStrip = menuStrip1;
            Margin = new Padding(4, 5, 4, 5);
            Name = "TrinityMainWindow";
            Text = "Trinity Mod Loader";
            FormClosing += TrinityMainWindow_FormClosing;
            Load += FileSystemForm_Load;
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            treeContext.ResumeLayout(false);
            advancedPanel.ResumeLayout(false);
            basicPanel.ResumeLayout(false);
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            basicContext.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
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
        private Button addZipMod;
        private Button modOrderDown;
        private Button modOrderUp;
        private CheckedListBox modList;
        private Button applyModsBut;
        private GroupBox groupBox1;
        private Label modNameLbl;
        private Label label1;
        private Label label2;
        private TextBox modDescriptionBox;
        private ToolStripMenuItem showUnhashedFilesToolStripMenuItem;
        private ToolStripMenuItem helpToolStripMenuItem;
        private ToolStripMenuItem aboutToolStripMenuItem;
        private ToolStripMenuItem openRomFSFolderToolStripMenuItem;
        private ToolStripMenuItem optionsToolStripMenuItem;
        private ToolStripMenuItem disableAutoLoad;
        private ContextMenuStrip basicContext;
        private ToolStripMenuItem deleteModBut;
        private ToolStripMenuItem setOutputFolderToolStripMenuItem;
        private Label versionLbl;
        private Label label4;
        private ToolStripMenuItem getLatestHashes;
        private Button addFolderMod;
    }
}