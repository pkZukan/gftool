namespace TrinityModLoader
{
    partial class TrinityExplorerWindow
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            romFS_treeContext = new ContextMenuStrip(components);
            saveFileToolStripMenuItem = new ToolStripMenuItem();
            AddToLayeredFSMenuItem = new ToolStripMenuItem();
            menuStrip1 = new MenuStrip();
            fileToolStripMenuItem = new ToolStripMenuItem();
            openFileDescriptorToolStripMenuItem = new ToolStripMenuItem();
            openRomFSMenuItem = new ToolStripMenuItem();
            toolStripSeparator1 = new ToolStripSeparator();
            saveFileDescriptorMenuItem = new ToolStripMenuItem();
            ExportFilesToolStripMenuItem = new ToolStripMenuItem();
            allFilesToolStripMenuItem = new ToolStripMenuItem();
            visibleFilesToolStripMenuItem = new ToolStripMenuItem();
            unhashedFilesToolStripMenuItem = new ToolStripMenuItem();
            viewToolStripMenuItem = new ToolStripMenuItem();
            fileSystemToolStripMenuItem = new ToolStripMenuItem();
            archivesToolStripMenuItem = new ToolStripMenuItem();
            layeredFS_treeContext = new ContextMenuStrip(components);
            RemoveFromLayeredFSMenuItem = new ToolStripMenuItem();
            unhashed_treeContext = new ContextMenuStrip(components);
            saveUnhashedFileToolStripMenuItem = new ToolStripMenuItem();
            statusStrip1 = new StatusStrip();
            statusLabel1 = new ToolStripStatusLabel();
            romfsItemLabel = new ToolStripStatusLabel();
            layeredfsItemLabel = new ToolStripStatusLabel();
            unhashedItemLabel = new ToolStripStatusLabel();
            explorerFileViewer = new ExplorerFileViewer();
            explorerPackViewer = new ExplorerPackViewer();
            navigateUpButton = new Button();
            refreshButton = new Button();
            rootPathTextbox = new TextBox();
            romFS_treeContext.SuspendLayout();
            menuStrip1.SuspendLayout();
            layeredFS_treeContext.SuspendLayout();
            unhashed_treeContext.SuspendLayout();
            statusStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)explorerFileViewer).BeginInit();
            ((System.ComponentModel.ISupportInitialize)explorerPackViewer).BeginInit();
            SuspendLayout();
            // 
            // romFS_treeContext
            // 
            romFS_treeContext.ImageScalingSize = new Size(24, 24);
            romFS_treeContext.Items.AddRange(new ToolStripItem[] { saveFileToolStripMenuItem, AddToLayeredFSMenuItem });
            romFS_treeContext.Name = "treeContext";
            romFS_treeContext.Size = new Size(226, 68);
            // 
            // saveFileToolStripMenuItem
            // 
            saveFileToolStripMenuItem.Name = "saveFileToolStripMenuItem";
            saveFileToolStripMenuItem.Size = new Size(225, 32);
            saveFileToolStripMenuItem.Text = "Save File";
            saveFileToolStripMenuItem.Click += saveRomFSFileToolStripMenuItem_Click;
            // 
            // AddToLayeredFSMenuItem
            // 
            AddToLayeredFSMenuItem.Name = "AddToLayeredFSMenuItem";
            AddToLayeredFSMenuItem.Size = new Size(225, 32);
            AddToLayeredFSMenuItem.Text = "Add to LayeredFS";
            AddToLayeredFSMenuItem.Click += AddToLayeredFSMenuItem_Click;
            // 
            // menuStrip1
            // 
            menuStrip1.ImageScalingSize = new Size(24, 24);
            menuStrip1.Items.AddRange(new ToolStripItem[] { fileToolStripMenuItem, viewToolStripMenuItem });
            menuStrip1.Location = new Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Padding = new Padding(9, 3, 0, 3);
            menuStrip1.Size = new Size(829, 35);
            menuStrip1.TabIndex = 10;
            menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            fileToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { openFileDescriptorToolStripMenuItem, openRomFSMenuItem, toolStripSeparator1, saveFileDescriptorMenuItem, ExportFilesToolStripMenuItem });
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
            // openRomFSMenuItem
            // 
            openRomFSMenuItem.Name = "openRomFSMenuItem";
            openRomFSMenuItem.Size = new Size(276, 34);
            openRomFSMenuItem.Text = "Open RomFS Folder";
            openRomFSMenuItem.Click += openRomFSToolStripMenuItem_Click;
            // 
            // toolStripSeparator1
            // 
            toolStripSeparator1.Name = "toolStripSeparator1";
            toolStripSeparator1.Size = new Size(273, 6);
            // 
            // saveFileDescriptorMenuItem
            // 
            saveFileDescriptorMenuItem.Name = "saveFileDescriptorMenuItem";
            saveFileDescriptorMenuItem.Size = new Size(276, 34);
            saveFileDescriptorMenuItem.Text = "Save File Descriptor";
            // 
            // ExportFilesToolStripMenuItem
            // 
            ExportFilesToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { allFilesToolStripMenuItem, visibleFilesToolStripMenuItem, unhashedFilesToolStripMenuItem });
            ExportFilesToolStripMenuItem.Name = "ExportFilesToolStripMenuItem";
            ExportFilesToolStripMenuItem.Size = new Size(276, 34);
            ExportFilesToolStripMenuItem.Text = "Export Files";
            // 
            // allFilesToolStripMenuItem
            // 
            allFilesToolStripMenuItem.Name = "allFilesToolStripMenuItem";
            allFilesToolStripMenuItem.Size = new Size(270, 34);
            allFilesToolStripMenuItem.Text = "All Files";
            allFilesToolStripMenuItem.Click += allFilesToolStripMenuItem_Click;
            // 
            // visibleFilesToolStripMenuItem
            // 
            visibleFilesToolStripMenuItem.Name = "visibleFilesToolStripMenuItem";
            visibleFilesToolStripMenuItem.Size = new Size(270, 34);
            visibleFilesToolStripMenuItem.Text = "Visible Files";
            visibleFilesToolStripMenuItem.Click += visibleFilesToolStripMenuItem_Click;
            // 
            // unhashedFilesToolStripMenuItem
            // 
            unhashedFilesToolStripMenuItem.Name = "unhashedFilesToolStripMenuItem";
            unhashedFilesToolStripMenuItem.Size = new Size(270, 34);
            unhashedFilesToolStripMenuItem.Text = "Unhashed Files";
            unhashedFilesToolStripMenuItem.Click += unhashedFilesToolStripMenuItem_Click;
            // 
            // viewToolStripMenuItem
            // 
            viewToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { fileSystemToolStripMenuItem, archivesToolStripMenuItem });
            viewToolStripMenuItem.Name = "viewToolStripMenuItem";
            viewToolStripMenuItem.Size = new Size(65, 29);
            viewToolStripMenuItem.Text = "View";
            // 
            // fileSystemToolStripMenuItem
            // 
            fileSystemToolStripMenuItem.Checked = true;
            fileSystemToolStripMenuItem.CheckState = CheckState.Checked;
            fileSystemToolStripMenuItem.Name = "fileSystemToolStripMenuItem";
            fileSystemToolStripMenuItem.Size = new Size(202, 34);
            fileSystemToolStripMenuItem.Text = "File System";
            fileSystemToolStripMenuItem.Click += fileSystemToolStripMenuItem_Click;
            // 
            // archivesToolStripMenuItem
            // 
            archivesToolStripMenuItem.Name = "archivesToolStripMenuItem";
            archivesToolStripMenuItem.Size = new Size(202, 34);
            archivesToolStripMenuItem.Text = "Archives";
            archivesToolStripMenuItem.Click += archivesToolStripMenuItem_Click;
            // 
            // layeredFS_treeContext
            // 
            layeredFS_treeContext.ImageScalingSize = new Size(24, 24);
            layeredFS_treeContext.Items.AddRange(new ToolStripItem[] { RemoveFromLayeredFSMenuItem });
            layeredFS_treeContext.Name = "treeContext";
            layeredFS_treeContext.Size = new Size(278, 36);
            // 
            // RemoveFromLayeredFSMenuItem
            // 
            RemoveFromLayeredFSMenuItem.Name = "RemoveFromLayeredFSMenuItem";
            RemoveFromLayeredFSMenuItem.Size = new Size(277, 32);
            RemoveFromLayeredFSMenuItem.Text = "Remove from LayeredFS";
            RemoveFromLayeredFSMenuItem.Click += RemoveFromLayeredFSMenuItem_Click;
            // 
            // unhashed_treeContext
            // 
            unhashed_treeContext.ImageScalingSize = new Size(24, 24);
            unhashed_treeContext.Items.AddRange(new ToolStripItem[] { saveUnhashedFileToolStripMenuItem });
            unhashed_treeContext.Name = "treeContext";
            unhashed_treeContext.Size = new Size(153, 36);
            // 
            // saveUnhashedFileToolStripMenuItem
            // 
            saveUnhashedFileToolStripMenuItem.Name = "saveUnhashedFileToolStripMenuItem";
            saveUnhashedFileToolStripMenuItem.Size = new Size(152, 32);
            saveUnhashedFileToolStripMenuItem.Text = "Save File";
            saveUnhashedFileToolStripMenuItem.Click += saveRomFSFileToolStripMenuItem_Click;
            // 
            // statusStrip1
            // 
            statusStrip1.ImageScalingSize = new Size(24, 24);
            statusStrip1.Items.AddRange(new ToolStripItem[] { statusLabel1, romfsItemLabel, layeredfsItemLabel, unhashedItemLabel });
            statusStrip1.Location = new Point(0, 946);
            statusStrip1.Name = "statusStrip1";
            statusStrip1.Padding = new Padding(1, 0, 20, 0);
            statusStrip1.Size = new Size(829, 22);
            statusStrip1.TabIndex = 12;
            statusStrip1.Text = "statusStrip1";
            // 
            // statusLabel1
            // 
            statusLabel1.Name = "statusLabel1";
            statusLabel1.Size = new Size(0, 15);
            // 
            // romfsItemLabel
            // 
            romfsItemLabel.Name = "romfsItemLabel";
            romfsItemLabel.Size = new Size(0, 15);
            // 
            // layeredfsItemLabel
            // 
            layeredfsItemLabel.Name = "layeredfsItemLabel";
            layeredfsItemLabel.Size = new Size(0, 15);
            // 
            // unhashedItemLabel
            // 
            unhashedItemLabel.Name = "unhashedItemLabel";
            unhashedItemLabel.Size = new Size(0, 15);
            // 
            // explorerFileViewer
            // 
            explorerFileViewer.AllowUserToAddRows = false;
            explorerFileViewer.AllowUserToDeleteRows = false;
            explorerFileViewer.AllowUserToResizeRows = false;
            explorerFileViewer.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            explorerFileViewer.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            explorerFileViewer.BackgroundColor = SystemColors.ControlLightLight;
            explorerFileViewer.BorderStyle = BorderStyle.None;
            explorerFileViewer.CellBorderStyle = DataGridViewCellBorderStyle.None;
            explorerFileViewer.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            explorerFileViewer.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            explorerFileViewer.Location = new Point(0, 87);
            explorerFileViewer.Margin = new Padding(4, 5, 4, 5);
            explorerFileViewer.MultiSelect = false;
            explorerFileViewer.Name = "explorerFileViewer";
            explorerFileViewer.ReadOnly = true;
            explorerFileViewer.RowHeadersVisible = false;
            explorerFileViewer.RowHeadersWidth = 62;
            explorerFileViewer.RowTemplate.Height = 25;
            explorerFileViewer.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            explorerFileViewer.Size = new Size(829, 845);
            explorerFileViewer.TabIndex = 13;
            explorerFileViewer.CellDoubleClick += fileView_CellDoubleClick;
            explorerFileViewer.MouseUp += fileView_MouseUp;
            // 
            // explorerPackViewer
            // 
            explorerPackViewer.AllowUserToAddRows = false;
            explorerPackViewer.AllowUserToDeleteRows = false;
            explorerPackViewer.AllowUserToResizeRows = false;
            explorerPackViewer.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            explorerPackViewer.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            explorerPackViewer.BackgroundColor = SystemColors.ControlLightLight;
            explorerPackViewer.BorderStyle = BorderStyle.None;
            explorerPackViewer.CellBorderStyle = DataGridViewCellBorderStyle.None;
            explorerPackViewer.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            explorerPackViewer.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            explorerPackViewer.Location = new Point(0, 87);
            explorerPackViewer.Margin = new Padding(4, 5, 4, 5);
            explorerPackViewer.MultiSelect = false;
            explorerPackViewer.Name = "explorerPackViewer";
            explorerPackViewer.ReadOnly = true;
            explorerPackViewer.RowHeadersVisible = false;
            explorerPackViewer.RowHeadersWidth = 62;
            explorerPackViewer.RowTemplate.Height = 25;
            explorerPackViewer.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            explorerPackViewer.Size = new Size(829, 845);
            explorerPackViewer.TabIndex = 13;
            explorerPackViewer.Visible = false;
            explorerPackViewer.CellDoubleClick += fileView_CellDoubleClick;
            explorerPackViewer.MouseUp += fileView_MouseUp;
            // 
            // navigateUpButton
            // 
            navigateUpButton.FlatAppearance.BorderSize = 0;
            navigateUpButton.FlatStyle = FlatStyle.System;
            navigateUpButton.Location = new Point(0, 45);
            navigateUpButton.Margin = new Padding(4, 5, 4, 5);
            navigateUpButton.Name = "navigateUpButton";
            navigateUpButton.Size = new Size(50, 38);
            navigateUpButton.TabIndex = 14;
            navigateUpButton.Text = "↑";
            navigateUpButton.UseVisualStyleBackColor = true;
            navigateUpButton.Click += navigateUpButton_Click;
            // 
            // refreshButton
            // 
            refreshButton.FlatAppearance.BorderSize = 0;
            refreshButton.FlatStyle = FlatStyle.System;
            refreshButton.Location = new Point(53, 45);
            refreshButton.Margin = new Padding(4, 5, 4, 5);
            refreshButton.Name = "refreshButton";
            refreshButton.Size = new Size(50, 38);
            refreshButton.TabIndex = 15;
            refreshButton.Text = "⟳";
            refreshButton.UseVisualStyleBackColor = true;
            refreshButton.Click += refreshButton_Click;
            // 
            // rootPathTextbox
            // 
            rootPathTextbox.BorderStyle = BorderStyle.FixedSingle;
            rootPathTextbox.Location = new Point(117, 45);
            rootPathTextbox.Margin = new Padding(4, 5, 4, 5);
            rootPathTextbox.Name = "rootPathTextbox";
            rootPathTextbox.Size = new Size(693, 31);
            rootPathTextbox.TabIndex = 16;
            rootPathTextbox.KeyUp += rootPathTextbox_KeyUp;
            // 
            // TrinityExplorerWindow
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(829, 968);
            Controls.Add(explorerPackViewer);
            Controls.Add(rootPathTextbox);
            Controls.Add(refreshButton);
            Controls.Add(navigateUpButton);
            Controls.Add(explorerFileViewer);
            Controls.Add(statusStrip1);
            Controls.Add(menuStrip1);
            Name = "TrinityExplorerWindow";
            Text = "Trinity Explorer Window - No File Loaded";
            FormClosing += TrinityExplorerWindow_FormClosing;
            romFS_treeContext.ResumeLayout(false);
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            layeredFS_treeContext.ResumeLayout(false);
            unhashed_treeContext.ResumeLayout(false);
            statusStrip1.ResumeLayout(false);
            statusStrip1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)explorerFileViewer).EndInit();
            ((System.ComponentModel.ISupportInitialize)explorerPackViewer).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private ContextMenuStrip romFS_treeContext;
        private ToolStripMenuItem saveFileToolStripMenuItem;
        private MenuStrip menuStrip1;
        private ToolStripMenuItem fileToolStripMenuItem;
        private ToolStripMenuItem openFileDescriptorToolStripMenuItem;
        private ToolStripMenuItem openRomFSMenuItem;
        private ToolStripSeparator toolStripSeparator1;
        private ToolStripMenuItem saveFileDescriptorMenuItem;
        private ContextMenuStrip layeredFS_treeContext;
        private ContextMenuStrip unhashed_treeContext;
        private ToolStripMenuItem saveUnhashedFileToolStripMenuItem;
        private StatusStrip statusStrip1;
        private ToolStripStatusLabel statusLabel1;
        private ToolStripStatusLabel romfsItemLabel;
        private ToolStripStatusLabel layeredfsItemLabel;
        private ToolStripStatusLabel unhashedItemLabel;
        private ToolStripMenuItem RemoveFromLayeredFSMenuItem;
        private ToolStripMenuItem AddToLayeredFSMenuItem;
        private ExplorerFileViewer explorerFileViewer;
        private ExplorerPackViewer explorerPackViewer;
        private Button navigateUpButton;
        private Button refreshButton;
        private TextBox rootPathTextbox;
        private ToolStripMenuItem viewToolStripMenuItem;
        private ToolStripMenuItem fileSystemToolStripMenuItem;
        private ToolStripMenuItem archivesToolStripMenuItem;
        private ToolStripMenuItem ExportFilesToolStripMenuItem;
        private ToolStripMenuItem allFilesToolStripMenuItem;
        private ToolStripMenuItem visibleFilesToolStripMenuItem;
        private ToolStripMenuItem unhashedFilesToolStripMenuItem;
    }
}