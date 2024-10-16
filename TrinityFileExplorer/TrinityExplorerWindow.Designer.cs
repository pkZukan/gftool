namespace TrinityFileExplorer
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
            helpToolStripMenuItem = new ToolStripMenuItem();
            getLatestHashListToolStripMenuItem = new ToolStripMenuItem();
            latestToolStripMenuItem = new ToolStripMenuItem();
            fromFileToolStripMenuItem = new ToolStripMenuItem();
            fromURLToolStripMenuItem = new ToolStripMenuItem();
            layeredFS_treeContext = new ContextMenuStrip(components);
            RemoveFromLayeredFSMenuItem = new ToolStripMenuItem();
            unhashed_treeContext = new ContextMenuStrip(components);
            saveUnhashedFileToolStripMenuItem = new ToolStripMenuItem();
            statusStrip1 = new StatusStrip();
            statusLabel1 = new ToolStripStatusLabel();
            romfsItemLabel = new ToolStripStatusLabel();
            layeredfsItemLabel = new ToolStripStatusLabel();
            unhashedItemLabel = new ToolStripStatusLabel();
            navigateUpButton = new Button();
            refreshButton = new Button();
            rootPathTextbox = new TextBox();
            explorerFileViewer = new ExplorerFileViewer();
            dataGridViewTextBoxColumn1 = new DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn2 = new DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn3 = new DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn4 = new DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn5 = new DataGridViewTextBoxColumn();
            explorerPackViewer = new ExplorerPackViewer();
            dataGridViewTextBoxColumn6 = new DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn7 = new DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn8 = new DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn9 = new DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn10 = new DataGridViewTextBoxColumn();
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
            romFS_treeContext.Size = new Size(167, 48);
            // 
            // saveFileToolStripMenuItem
            // 
            saveFileToolStripMenuItem.Name = "saveFileToolStripMenuItem";
            saveFileToolStripMenuItem.Size = new Size(166, 22);
            saveFileToolStripMenuItem.Text = "Save File";
            saveFileToolStripMenuItem.Click += saveRomFSFileToolStripMenuItem_Click;
            // 
            // AddToLayeredFSMenuItem
            // 
            AddToLayeredFSMenuItem.Name = "AddToLayeredFSMenuItem";
            AddToLayeredFSMenuItem.Size = new Size(166, 22);
            AddToLayeredFSMenuItem.Text = "Add to LayeredFS";
            AddToLayeredFSMenuItem.Click += AddToLayeredFSMenuItem_Click;
            // 
            // menuStrip1
            // 
            menuStrip1.ImageScalingSize = new Size(24, 24);
            menuStrip1.Items.AddRange(new ToolStripItem[] { fileToolStripMenuItem, viewToolStripMenuItem, helpToolStripMenuItem });
            menuStrip1.Location = new Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Size = new Size(580, 24);
            menuStrip1.TabIndex = 10;
            menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            fileToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { openFileDescriptorToolStripMenuItem, openRomFSMenuItem, toolStripSeparator1, saveFileDescriptorMenuItem, ExportFilesToolStripMenuItem });
            fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            fileToolStripMenuItem.Size = new Size(37, 20);
            fileToolStripMenuItem.Text = "File";
            // 
            // openFileDescriptorToolStripMenuItem
            // 
            openFileDescriptorToolStripMenuItem.Name = "openFileDescriptorToolStripMenuItem";
            openFileDescriptorToolStripMenuItem.Size = new Size(181, 22);
            openFileDescriptorToolStripMenuItem.Text = "Open File Descriptor";
            openFileDescriptorToolStripMenuItem.Click += openFileDescriptorToolStripMenuItem_Click;
            // 
            // openRomFSMenuItem
            // 
            openRomFSMenuItem.Name = "openRomFSMenuItem";
            openRomFSMenuItem.Size = new Size(181, 22);
            openRomFSMenuItem.Text = "Open RomFS Folder";
            openRomFSMenuItem.Click += openRomFSToolStripMenuItem_Click;
            // 
            // toolStripSeparator1
            // 
            toolStripSeparator1.Name = "toolStripSeparator1";
            toolStripSeparator1.Size = new Size(178, 6);
            // 
            // saveFileDescriptorMenuItem
            // 
            saveFileDescriptorMenuItem.Name = "saveFileDescriptorMenuItem";
            saveFileDescriptorMenuItem.Size = new Size(181, 22);
            saveFileDescriptorMenuItem.Text = "Save File Descriptor";
            // 
            // ExportFilesToolStripMenuItem
            // 
            ExportFilesToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { allFilesToolStripMenuItem, visibleFilesToolStripMenuItem, unhashedFilesToolStripMenuItem });
            ExportFilesToolStripMenuItem.Name = "ExportFilesToolStripMenuItem";
            ExportFilesToolStripMenuItem.Size = new Size(181, 22);
            ExportFilesToolStripMenuItem.Text = "Export Files";
            // 
            // allFilesToolStripMenuItem
            // 
            allFilesToolStripMenuItem.Name = "allFilesToolStripMenuItem";
            allFilesToolStripMenuItem.Size = new Size(153, 22);
            allFilesToolStripMenuItem.Text = "All Files";
            allFilesToolStripMenuItem.Click += allFilesToolStripMenuItem_Click;
            // 
            // visibleFilesToolStripMenuItem
            // 
            visibleFilesToolStripMenuItem.Name = "visibleFilesToolStripMenuItem";
            visibleFilesToolStripMenuItem.Size = new Size(153, 22);
            visibleFilesToolStripMenuItem.Text = "Visible Files";
            visibleFilesToolStripMenuItem.Click += visibleFilesToolStripMenuItem_Click;
            // 
            // unhashedFilesToolStripMenuItem
            // 
            unhashedFilesToolStripMenuItem.Name = "unhashedFilesToolStripMenuItem";
            unhashedFilesToolStripMenuItem.Size = new Size(153, 22);
            unhashedFilesToolStripMenuItem.Text = "Unhashed Files";
            unhashedFilesToolStripMenuItem.Click += unhashedFilesToolStripMenuItem_Click;
            // 
            // viewToolStripMenuItem
            // 
            viewToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { fileSystemToolStripMenuItem, archivesToolStripMenuItem });
            viewToolStripMenuItem.Name = "viewToolStripMenuItem";
            viewToolStripMenuItem.Size = new Size(44, 20);
            viewToolStripMenuItem.Text = "View";
            // 
            // fileSystemToolStripMenuItem
            // 
            fileSystemToolStripMenuItem.Checked = true;
            fileSystemToolStripMenuItem.CheckState = CheckState.Checked;
            fileSystemToolStripMenuItem.Name = "fileSystemToolStripMenuItem";
            fileSystemToolStripMenuItem.Size = new Size(133, 22);
            fileSystemToolStripMenuItem.Text = "File System";
            fileSystemToolStripMenuItem.Click += fileSystemToolStripMenuItem_Click;
            // 
            // archivesToolStripMenuItem
            // 
            archivesToolStripMenuItem.Name = "archivesToolStripMenuItem";
            archivesToolStripMenuItem.Size = new Size(133, 22);
            archivesToolStripMenuItem.Text = "Archives";
            archivesToolStripMenuItem.Click += archivesToolStripMenuItem_Click;
            // 
            // helpToolStripMenuItem
            // 
            helpToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { getLatestHashListToolStripMenuItem });
            helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            helpToolStripMenuItem.Size = new Size(44, 20);
            helpToolStripMenuItem.Text = "Help";
            // 
            // getLatestHashListToolStripMenuItem
            // 
            getLatestHashListToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { latestToolStripMenuItem, fromFileToolStripMenuItem, fromURLToolStripMenuItem });
            getLatestHashListToolStripMenuItem.Name = "getLatestHashListToolStripMenuItem";
            getLatestHashListToolStripMenuItem.Size = new Size(145, 22);
            getLatestHashListToolStripMenuItem.Text = "Load Hashlist";
            // 
            // latestToolStripMenuItem
            // 
            latestToolStripMenuItem.Name = "latestToolStripMenuItem";
            latestToolStripMenuItem.Size = new Size(157, 22);
            latestToolStripMenuItem.Text = "From PokeDocs";
            latestToolStripMenuItem.Click += latestToolStripMenuItem_Click;
            // 
            // fromFileToolStripMenuItem
            // 
            fromFileToolStripMenuItem.Name = "fromFileToolStripMenuItem";
            fromFileToolStripMenuItem.Size = new Size(157, 22);
            fromFileToolStripMenuItem.Text = "From File...";
            fromFileToolStripMenuItem.Click += fromFileToolStripMenuItem_Click;
            // 
            // fromURLToolStripMenuItem
            // 
            fromURLToolStripMenuItem.Name = "fromURLToolStripMenuItem";
            fromURLToolStripMenuItem.Size = new Size(157, 22);
            fromURLToolStripMenuItem.Text = "From URL...";
            fromURLToolStripMenuItem.Click += fromURLToolStripMenuItem_Click;
            // 
            // layeredFS_treeContext
            // 
            layeredFS_treeContext.ImageScalingSize = new Size(24, 24);
            layeredFS_treeContext.Items.AddRange(new ToolStripItem[] { RemoveFromLayeredFSMenuItem });
            layeredFS_treeContext.Name = "treeContext";
            layeredFS_treeContext.Size = new Size(203, 26);
            // 
            // RemoveFromLayeredFSMenuItem
            // 
            RemoveFromLayeredFSMenuItem.Name = "RemoveFromLayeredFSMenuItem";
            RemoveFromLayeredFSMenuItem.Size = new Size(202, 22);
            RemoveFromLayeredFSMenuItem.Text = "Remove from LayeredFS";
            RemoveFromLayeredFSMenuItem.Click += RemoveFromLayeredFSMenuItem_Click;
            // 
            // unhashed_treeContext
            // 
            unhashed_treeContext.ImageScalingSize = new Size(24, 24);
            unhashed_treeContext.Items.AddRange(new ToolStripItem[] { saveUnhashedFileToolStripMenuItem });
            unhashed_treeContext.Name = "treeContext";
            unhashed_treeContext.Size = new Size(120, 26);
            // 
            // saveUnhashedFileToolStripMenuItem
            // 
            saveUnhashedFileToolStripMenuItem.Name = "saveUnhashedFileToolStripMenuItem";
            saveUnhashedFileToolStripMenuItem.Size = new Size(119, 22);
            saveUnhashedFileToolStripMenuItem.Text = "Save File";
            saveUnhashedFileToolStripMenuItem.Click += saveRomFSFileToolStripMenuItem_Click;
            // 
            // statusStrip1
            // 
            statusStrip1.ImageScalingSize = new Size(24, 24);
            statusStrip1.Items.AddRange(new ToolStripItem[] { statusLabel1, romfsItemLabel, layeredfsItemLabel, unhashedItemLabel });
            statusStrip1.Location = new Point(0, 559);
            statusStrip1.Name = "statusStrip1";
            statusStrip1.Size = new Size(580, 22);
            statusStrip1.TabIndex = 12;
            statusStrip1.Text = "statusStrip1";
            // 
            // statusLabel1
            // 
            statusLabel1.Name = "statusLabel1";
            statusLabel1.Size = new Size(0, 17);
            // 
            // romfsItemLabel
            // 
            romfsItemLabel.Name = "romfsItemLabel";
            romfsItemLabel.Size = new Size(0, 17);
            // 
            // layeredfsItemLabel
            // 
            layeredfsItemLabel.Name = "layeredfsItemLabel";
            layeredfsItemLabel.Size = new Size(0, 17);
            // 
            // unhashedItemLabel
            // 
            unhashedItemLabel.Name = "unhashedItemLabel";
            unhashedItemLabel.Size = new Size(0, 17);
            // 
            // navigateUpButton
            // 
            navigateUpButton.FlatAppearance.BorderSize = 0;
            navigateUpButton.FlatStyle = FlatStyle.System;
            navigateUpButton.Location = new Point(6, 27);
            navigateUpButton.Name = "navigateUpButton";
            navigateUpButton.Size = new Size(35, 23);
            navigateUpButton.TabIndex = 14;
            navigateUpButton.Text = "↑";
            navigateUpButton.UseVisualStyleBackColor = true;
            navigateUpButton.Click += navigateUpButton_Click;
            // 
            // refreshButton
            // 
            refreshButton.FlatAppearance.BorderSize = 0;
            refreshButton.FlatStyle = FlatStyle.System;
            refreshButton.Location = new Point(44, 27);
            refreshButton.Name = "refreshButton";
            refreshButton.Size = new Size(35, 23);
            refreshButton.TabIndex = 15;
            refreshButton.Text = "⟳";
            refreshButton.UseVisualStyleBackColor = true;
            refreshButton.Click += refreshButton_Click;
            // 
            // rootPathTextbox
            // 
            rootPathTextbox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            rootPathTextbox.BorderStyle = BorderStyle.FixedSingle;
            rootPathTextbox.Location = new Point(82, 27);
            rootPathTextbox.Name = "rootPathTextbox";
            rootPathTextbox.Size = new Size(486, 23);
            rootPathTextbox.TabIndex = 16;
            rootPathTextbox.KeyUp += rootPathTextbox_KeyUp;
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
            explorerFileViewer.Location = new Point(0, 58);
            explorerFileViewer.Margin = new Padding(4, 5, 4, 5);
            explorerFileViewer.MultiSelect = false;
            explorerFileViewer.Name = "explorerFileViewer";
            explorerFileViewer.ReadOnly = true;
            explorerFileViewer.RowHeadersVisible = false;
            explorerFileViewer.RowHeadersWidth = 62;
            explorerFileViewer.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            explorerFileViewer.Size = new Size(580, 496);
            explorerFileViewer.TabIndex = 13;
            explorerFileViewer.CellDoubleClick += fileView_CellDoubleClick;
            explorerFileViewer.MouseUp += fileView_MouseUp;
            // 
            // dataGridViewTextBoxColumn1
            // 
            dataGridViewTextBoxColumn1.HeaderText = "Name";
            dataGridViewTextBoxColumn1.Name = "dataGridViewTextBoxColumn1";
            dataGridViewTextBoxColumn1.ReadOnly = true;
            // 
            // dataGridViewTextBoxColumn2
            // 
            dataGridViewTextBoxColumn2.HeaderText = "Hash";
            dataGridViewTextBoxColumn2.Name = "dataGridViewTextBoxColumn2";
            dataGridViewTextBoxColumn2.ReadOnly = true;
            // 
            // dataGridViewTextBoxColumn3
            // 
            dataGridViewTextBoxColumn3.HeaderText = "Location";
            dataGridViewTextBoxColumn3.Name = "dataGridViewTextBoxColumn3";
            dataGridViewTextBoxColumn3.ReadOnly = true;
            // 
            // dataGridViewTextBoxColumn4
            // 
            dataGridViewTextBoxColumn4.HeaderText = "Type";
            dataGridViewTextBoxColumn4.Name = "dataGridViewTextBoxColumn4";
            dataGridViewTextBoxColumn4.ReadOnly = true;
            // 
            // dataGridViewTextBoxColumn5
            // 
            dataGridViewTextBoxColumn5.HeaderText = "Size";
            dataGridViewTextBoxColumn5.Name = "dataGridViewTextBoxColumn5";
            dataGridViewTextBoxColumn5.ReadOnly = true;
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
            explorerPackViewer.Location = new Point(0, 58);
            explorerPackViewer.Margin = new Padding(4, 5, 4, 5);
            explorerPackViewer.MultiSelect = false;
            explorerPackViewer.Name = "explorerPackViewer";
            explorerPackViewer.ReadOnly = true;
            explorerPackViewer.RowHeadersVisible = false;
            explorerPackViewer.RowHeadersWidth = 62;
            explorerPackViewer.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            explorerPackViewer.Size = new Size(580, 496);
            explorerPackViewer.TabIndex = 13;
            explorerPackViewer.Visible = false;
            explorerPackViewer.CellDoubleClick += fileView_CellDoubleClick;
            explorerPackViewer.MouseUp += fileView_MouseUp;
            // 
            // dataGridViewTextBoxColumn6
            // 
            dataGridViewTextBoxColumn6.HeaderText = "Name";
            dataGridViewTextBoxColumn6.Name = "dataGridViewTextBoxColumn6";
            dataGridViewTextBoxColumn6.ReadOnly = true;
            // 
            // dataGridViewTextBoxColumn7
            // 
            dataGridViewTextBoxColumn7.HeaderText = "Hash";
            dataGridViewTextBoxColumn7.Name = "dataGridViewTextBoxColumn7";
            dataGridViewTextBoxColumn7.ReadOnly = true;
            // 
            // dataGridViewTextBoxColumn8
            // 
            dataGridViewTextBoxColumn8.HeaderText = "Location";
            dataGridViewTextBoxColumn8.Name = "dataGridViewTextBoxColumn8";
            dataGridViewTextBoxColumn8.ReadOnly = true;
            // 
            // dataGridViewTextBoxColumn9
            // 
            dataGridViewTextBoxColumn9.HeaderText = "Type";
            dataGridViewTextBoxColumn9.Name = "dataGridViewTextBoxColumn9";
            dataGridViewTextBoxColumn9.ReadOnly = true;
            // 
            // dataGridViewTextBoxColumn10
            // 
            dataGridViewTextBoxColumn10.HeaderText = "Size";
            dataGridViewTextBoxColumn10.Name = "dataGridViewTextBoxColumn10";
            dataGridViewTextBoxColumn10.ReadOnly = true;
            // 
            // TrinityExplorerWindow
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(580, 581);
            Controls.Add(explorerPackViewer);
            Controls.Add(explorerFileViewer);
            Controls.Add(rootPathTextbox);
            Controls.Add(refreshButton);
            Controls.Add(navigateUpButton);
            Controls.Add(statusStrip1);
            Controls.Add(menuStrip1);
            Margin = new Padding(2);
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
        private ToolStripMenuItem helpToolStripMenuItem;
        private ToolStripMenuItem getLatestHashListToolStripMenuItem;
        private ToolStripMenuItem latestToolStripMenuItem;
        private ToolStripMenuItem fromFileToolStripMenuItem;
        private ToolStripMenuItem fromURLToolStripMenuItem;
        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn1;
        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn2;
        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn3;
        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn4;
        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn5;
        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn6;
        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn7;
        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn8;
        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn9;
        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn10;
    }
}