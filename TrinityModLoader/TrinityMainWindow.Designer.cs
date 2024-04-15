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
            newModPackMenuItem = new ToolStripMenuItem();
            OpenModPackMenuItems = new ToolStripMenuItem();
            ChooseModPackMenuItem = new ToolStripMenuItem();
            toolStripSeparator3 = new ToolStripSeparator();
            toolStripSeparator2 = new ToolStripSeparator();
            toolStripMenuItem6 = new ToolStripMenuItem();
            viewToolStripMenuItem = new ToolStripMenuItem();
            advancedToggle = new ToolStripMenuItem();
            helpToolStripMenuItem = new ToolStripMenuItem();
            preferencesMenuItem = new ToolStripMenuItem();
            toolStripMenuItem1 = new ToolStripMenuItem();
            toolStripSeparator1 = new ToolStripSeparator();
            getLatestHashesMenuItem = new ToolStripMenuItem();
            toolStripMenuItem3 = new ToolStripMenuItem();
            toolStripSeparator4 = new ToolStripSeparator();
            aboutToolStripMenuItem = new ToolStripMenuItem();
            helpProvider1 = new HelpProvider();
            saveFileToolStripMenuItem = new ToolStripMenuItem();
            markForLayeredFSToolStripMenuItem = new ToolStripMenuItem();
            basicContext = new ContextMenuStrip(components);
            deleteModBut = new ToolStripMenuItem();
            addPackedMod = new Button();
            modOrderUp = new Button();
            modOrderDown = new Button();
            modList = new CheckedListBox();
            applyModsBut = new Button();
            addFolderMod = new Button();
            splitContainer1 = new SplitContainer();
            modPropertyGrid = new PropertyGrid();
            menuStrip1.SuspendLayout();
            basicContext.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            SuspendLayout();
            // 
            // menuStrip1
            // 
            menuStrip1.ImageScalingSize = new Size(24, 24);
            menuStrip1.Items.AddRange(new ToolStripItem[] { fileToolStripMenuItem, viewToolStripMenuItem, helpToolStripMenuItem });
            menuStrip1.Location = new Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Padding = new Padding(9, 3, 0, 3);
            menuStrip1.Size = new Size(1174, 35);
            menuStrip1.TabIndex = 2;
            menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            fileToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { newModPackMenuItem, OpenModPackMenuItems, toolStripSeparator2, toolStripMenuItem6 });
            fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            fileToolStripMenuItem.Size = new Size(54, 29);
            fileToolStripMenuItem.Text = "File";
            // 
            // newModPackMenuItem
            // 
            newModPackMenuItem.Name = "newModPackMenuItem";
            newModPackMenuItem.Size = new Size(248, 34);
            newModPackMenuItem.Text = "New Mod Pack";
            newModPackMenuItem.Click += newModPackMenuItem_Click;
            // 
            // OpenModPackMenuItems
            // 
            OpenModPackMenuItems.DropDownItems.AddRange(new ToolStripItem[] { ChooseModPackMenuItem, toolStripSeparator3 });
            OpenModPackMenuItems.Name = "OpenModPackMenuItems";
            OpenModPackMenuItems.Size = new Size(248, 34);
            OpenModPackMenuItems.Text = "Open Mod Pack";
            // 
            // ChooseModPackMenuItem
            // 
            ChooseModPackMenuItem.Name = "ChooseModPackMenuItem";
            ChooseModPackMenuItem.Size = new Size(186, 34);
            ChooseModPackMenuItem.Text = "Choose...";
            ChooseModPackMenuItem.Click += ChooseModPackMenuItem_Click;
            // 
            // toolStripSeparator3
            // 
            toolStripSeparator3.Name = "toolStripSeparator3";
            toolStripSeparator3.Size = new Size(183, 6);
            // 
            // toolStripSeparator2
            // 
            toolStripSeparator2.Name = "toolStripSeparator2";
            toolStripSeparator2.Size = new Size(245, 6);
            // 
            // toolStripMenuItem6
            // 
            toolStripMenuItem6.Name = "toolStripMenuItem6";
            toolStripMenuItem6.Size = new Size(248, 34);
            toolStripMenuItem6.Text = "Export Mod Pack";
            // 
            // viewToolStripMenuItem
            // 
            viewToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { advancedToggle });
            viewToolStripMenuItem.Name = "viewToolStripMenuItem";
            viewToolStripMenuItem.Size = new Size(69, 29);
            viewToolStripMenuItem.Text = "Tools";
            // 
            // advancedToggle
            // 
            advancedToggle.Name = "advancedToggle";
            advancedToggle.Size = new Size(235, 34);
            advancedToggle.Text = "TRPFD Explorer";
            advancedToggle.Click += advancedViewToolStripMenuItem_Click;
            // 
            // helpToolStripMenuItem
            // 
            helpToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { preferencesMenuItem, toolStripSeparator1, getLatestHashesMenuItem, toolStripMenuItem3, toolStripSeparator4, aboutToolStripMenuItem });
            helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            helpToolStripMenuItem.Size = new Size(65, 29);
            helpToolStripMenuItem.Text = "Help";
            // 
            // preferencesMenuItem
            // 
            preferencesMenuItem.DropDownItems.AddRange(new ToolStripItem[] { toolStripMenuItem1 });
            preferencesMenuItem.Name = "preferencesMenuItem";
            preferencesMenuItem.Size = new Size(317, 34);
            preferencesMenuItem.Text = "Preferences";
            // 
            // toolStripMenuItem1
            // 
            toolStripMenuItem1.Name = "toolStripMenuItem1";
            toolStripMenuItem1.Size = new Size(255, 34);
            toolStripMenuItem1.Text = "Set RomFS Folder";
            toolStripMenuItem1.Click += openRomFSFolderToolStripMenuItem_Click;
            // 
            // toolStripSeparator1
            // 
            toolStripSeparator1.Name = "toolStripSeparator1";
            toolStripSeparator1.Size = new Size(314, 6);
            // 
            // getLatestHashesMenuItem
            // 
            getLatestHashesMenuItem.Name = "getLatestHashesMenuItem";
            getLatestHashesMenuItem.Size = new Size(317, 34);
            getLatestHashesMenuItem.Text = "Get Updated Hashes";
            getLatestHashesMenuItem.Click += getLatestHashes_Click;
            // 
            // toolStripMenuItem3
            // 
            toolStripMenuItem3.Name = "toolStripMenuItem3";
            toolStripMenuItem3.Size = new Size(317, 34);
            toolStripMenuItem3.Text = "Get Hashes From File";
            toolStripMenuItem3.Click += addHashesFromFile_Click;
            // 
            // toolStripSeparator4
            // 
            toolStripSeparator4.Name = "toolStripSeparator4";
            toolStripSeparator4.Size = new Size(314, 6);
            // 
            // aboutToolStripMenuItem
            // 
            aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
            aboutToolStripMenuItem.Size = new Size(317, 34);
            aboutToolStripMenuItem.Text = "About Trinity Mod Loader";
            aboutToolStripMenuItem.Click += aboutToolStripMenuItem_Click;
            // 
            // saveFileToolStripMenuItem
            // 
            saveFileToolStripMenuItem.Name = "saveFileToolStripMenuItem";
            saveFileToolStripMenuItem.Size = new Size(32, 19);
            // 
            // markForLayeredFSToolStripMenuItem
            // 
            markForLayeredFSToolStripMenuItem.Name = "markForLayeredFSToolStripMenuItem";
            markForLayeredFSToolStripMenuItem.Size = new Size(119, 22);
            // 
            // basicContext
            // 
            basicContext.ImageScalingSize = new Size(24, 24);
            basicContext.Items.AddRange(new ToolStripItem[] { deleteModBut });
            basicContext.Name = "basicContext";
            basicContext.Size = new Size(192, 36);
            // 
            // deleteModBut
            // 
            deleteModBut.Name = "deleteModBut";
            deleteModBut.Size = new Size(191, 32);
            deleteModBut.Text = "Remove Mod";
            deleteModBut.Click += deleteModButton_Click;
            // 
            // addPackedMod
            // 
            addPackedMod.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            addPackedMod.Enabled = false;
            addPackedMod.Location = new Point(6, 818);
            addPackedMod.Margin = new Padding(4, 5, 4, 5);
            addPackedMod.Name = "addPackedMod";
            addPackedMod.Size = new Size(325, 67);
            addPackedMod.TabIndex = 1;
            addPackedMod.Text = "Add mod (.zip)";
            addPackedMod.UseVisualStyleBackColor = true;
            addPackedMod.Click += addPackedMod_Click;
            // 
            // modOrderUp
            // 
            modOrderUp.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            modOrderUp.Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point);
            modOrderUp.Location = new Point(340, 35);
            modOrderUp.Margin = new Padding(4, 5, 4, 5);
            modOrderUp.Name = "modOrderUp";
            modOrderUp.Size = new Size(40, 50);
            modOrderUp.TabIndex = 2;
            modOrderUp.Text = "↑";
            modOrderUp.UseVisualStyleBackColor = true;
            modOrderUp.Click += modOrderUp_Click;
            // 
            // modOrderDown
            // 
            modOrderDown.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            modOrderDown.Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point);
            modOrderDown.Location = new Point(340, 95);
            modOrderDown.Margin = new Padding(4, 5, 4, 5);
            modOrderDown.Name = "modOrderDown";
            modOrderDown.Size = new Size(40, 50);
            modOrderDown.TabIndex = 3;
            modOrderDown.Text = "↓";
            modOrderDown.UseVisualStyleBackColor = true;
            modOrderDown.Click += modOrderDown_Click;
            // 
            // modList
            // 
            modList.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            modList.FormattingEnabled = true;
            modList.Location = new Point(6, 35);
            modList.Margin = new Padding(4, 5, 4, 5);
            modList.Name = "modList";
            modList.Size = new Size(323, 760);
            modList.TabIndex = 4;
            modList.ItemCheck += modList_ItemCheck;
            modList.SelectedIndexChanged += modList_SelectedIndexChanged;
            modList.MouseUp += modList_MouseUp;
            // 
            // applyModsBut
            // 
            applyModsBut.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            applyModsBut.Enabled = false;
            applyModsBut.Location = new Point(6, 965);
            applyModsBut.Margin = new Padding(4, 5, 4, 5);
            applyModsBut.Name = "applyModsBut";
            applyModsBut.Size = new Size(325, 67);
            applyModsBut.TabIndex = 5;
            applyModsBut.Text = "Apply mods";
            applyModsBut.UseVisualStyleBackColor = true;
            applyModsBut.Click += applyModsBut_Click;
            // 
            // addFolderMod
            // 
            addFolderMod.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            addFolderMod.Enabled = false;
            addFolderMod.Location = new Point(6, 892);
            addFolderMod.Margin = new Padding(4, 5, 4, 5);
            addFolderMod.Name = "addFolderMod";
            addFolderMod.Size = new Size(325, 67);
            addFolderMod.TabIndex = 7;
            addFolderMod.Text = "Add mod (folder)";
            addFolderMod.UseVisualStyleBackColor = true;
            addFolderMod.Click += addFolderMod_Click;
            // 
            // splitContainer1
            // 
            splitContainer1.Dock = DockStyle.Fill;
            splitContainer1.Location = new Point(0, 35);
            splitContainer1.Margin = new Padding(4, 5, 4, 5);
            splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            splitContainer1.Panel1.Controls.Add(modList);
            splitContainer1.Panel1.Controls.Add(addPackedMod);
            splitContainer1.Panel1.Controls.Add(addFolderMod);
            splitContainer1.Panel1.Controls.Add(applyModsBut);
            splitContainer1.Panel1.Controls.Add(modOrderUp);
            splitContainer1.Panel1.Controls.Add(modOrderDown);
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(modPropertyGrid);
            splitContainer1.Size = new Size(1174, 1040);
            splitContainer1.SplitterDistance = 389;
            splitContainer1.SplitterWidth = 6;
            splitContainer1.TabIndex = 8;
            // 
            // modPropertyGrid
            // 
            modPropertyGrid.Location = new Point(3, 0);
            modPropertyGrid.Name = "modPropertyGrid";
            modPropertyGrid.Size = new Size(773, 1040);
            modPropertyGrid.TabIndex = 0;
            // 
            // TrinityMainWindow
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1174, 1075);
            Controls.Add(splitContainer1);
            Controls.Add(menuStrip1);
            MainMenuStrip = menuStrip1;
            Margin = new Padding(4, 5, 4, 5);
            Name = "TrinityMainWindow";
            Text = "Trinity Mod Loader";
            FormClosing += TrinityMainWindow_FormClosing;
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            basicContext.ResumeLayout(false);
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private MenuStrip menuStrip1;
        private ToolStripMenuItem fileToolStripMenuItem;
        private HelpProvider helpProvider1;
        private ToolStripMenuItem saveFileToolStripMenuItem;
        private ToolStripMenuItem markForLayeredFSToolStripMenuItem;
        private ToolStripMenuItem viewToolStripMenuItem;
        private ToolStripMenuItem advancedToggle;
        private ToolStripMenuItem helpToolStripMenuItem;
        private ToolStripMenuItem aboutToolStripMenuItem;
        private ToolStripMenuItem optionsToolStripMenuItem;
        private ContextMenuStrip basicContext;
        private ToolStripMenuItem deleteModBut;
        private ToolStripMenuItem getLatestHashesMenuItem;
        private ToolStripMenuItem preferencesMenuItem;
        private ToolStripMenuItem toolStripMenuItem1;
        private ToolStripSeparator toolStripSeparator1;
        private ToolStripMenuItem toolStripMenuItem3;
        private ToolStripMenuItem newModPackMenuItem;
        private ToolStripMenuItem toolStripMenuItem6;
        private ToolStripSeparator toolStripSeparator2;
        private ToolStripMenuItem OpenModPackMenuItems;
        private ToolStripMenuItem ChooseModPackMenuItem;
        private ToolStripSeparator toolStripSeparator3;
        private Button addPackedMod;
        private Button modOrderUp;
        private Button modOrderDown;
        private CheckedListBox modList;
        private Button applyModsBut;
        private Button addFolderMod;
        private SplitContainer splitContainer1;
        private ToolStripSeparator toolStripSeparator4;
        private PropertyGrid modPropertyGrid;
    }
}