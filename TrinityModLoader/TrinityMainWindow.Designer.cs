namespace TrinityModLoader
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
            helpToolStripMenuItem = new ToolStripMenuItem();
            preferencesMenuItem = new ToolStripMenuItem();
            romfsPathMenuItem = new ToolStripMenuItem();
            openModWindowMenuItem = new ToolStripMenuItem();
            toolStripSeparator1 = new ToolStripSeparator();
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
            menuStrip1.Items.AddRange(new ToolStripItem[] { fileToolStripMenuItem, helpToolStripMenuItem });
            menuStrip1.Location = new Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Size = new Size(822, 24);
            menuStrip1.TabIndex = 2;
            menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            fileToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { newModPackMenuItem, OpenModPackMenuItems, toolStripSeparator2, toolStripMenuItem6 });
            fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            fileToolStripMenuItem.Size = new Size(37, 20);
            fileToolStripMenuItem.Text = "File";
            // 
            // newModPackMenuItem
            // 
            newModPackMenuItem.Name = "newModPackMenuItem";
            newModPackMenuItem.Size = new Size(180, 22);
            newModPackMenuItem.Text = "New Mod Pack";
            newModPackMenuItem.Click += newModPackMenuItem_Click;
            // 
            // OpenModPackMenuItems
            // 
            OpenModPackMenuItems.DropDownItems.AddRange(new ToolStripItem[] { ChooseModPackMenuItem, toolStripSeparator3 });
            OpenModPackMenuItems.Name = "OpenModPackMenuItems";
            OpenModPackMenuItems.Size = new Size(180, 22);
            OpenModPackMenuItems.Text = "Open Mod Pack";
            // 
            // ChooseModPackMenuItem
            // 
            ChooseModPackMenuItem.Name = "ChooseModPackMenuItem";
            ChooseModPackMenuItem.Size = new Size(123, 22);
            ChooseModPackMenuItem.Text = "Choose...";
            ChooseModPackMenuItem.Click += ChooseModPackMenuItem_Click;
            // 
            // toolStripSeparator3
            // 
            toolStripSeparator3.Name = "toolStripSeparator3";
            toolStripSeparator3.Size = new Size(120, 6);
            // 
            // toolStripSeparator2
            // 
            toolStripSeparator2.Name = "toolStripSeparator2";
            toolStripSeparator2.Size = new Size(177, 6);
            // 
            // toolStripMenuItem6
            // 
            toolStripMenuItem6.Name = "toolStripMenuItem6";
            toolStripMenuItem6.Size = new Size(180, 22);
            toolStripMenuItem6.Text = "Export Mod Pack";
            // 
            // helpToolStripMenuItem
            // 

            helpToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { preferencesMenuItem, toolStripSeparator1, aboutToolStripMenuItem });

            helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            helpToolStripMenuItem.Size = new Size(44, 20);
            helpToolStripMenuItem.Text = "Help";
            // 
            // preferencesMenuItem
            // 
            preferencesMenuItem.DropDownItems.AddRange(new ToolStripItem[] { romfsPathMenuItem, openModWindowMenuItem });
            preferencesMenuItem.Name = "preferencesMenuItem";
            preferencesMenuItem.Size = new Size(209, 22);
            preferencesMenuItem.Text = "Preferences";
            // 
            // romfsPathMenuItem
            // 
            romfsPathMenuItem.Name = "romfsPathMenuItem";
            romfsPathMenuItem.Size = new Size(260, 22);
            romfsPathMenuItem.Text = "Change RomFS Path...";
            romfsPathMenuItem.Click += openRomFSFolderToolStripMenuItem_Click;
            // 
            // openModWindowMenuItem
            // 
            openModWindowMenuItem.Name = "openModWindowMenuItem";
            openModWindowMenuItem.Size = new Size(260, 22);
            openModWindowMenuItem.Text = "Show Explorer after Applying Mods";
            openModWindowMenuItem.Click += openModWindowMenuItem_Click;
            // 
            // toolStripSeparator1
            // 
            toolStripSeparator1.Name = "toolStripSeparator1";
            toolStripSeparator1.Size = new Size(206, 6);
            // 
            // aboutToolStripMenuItem
            // 
            aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
            aboutToolStripMenuItem.Size = new Size(209, 22);
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
            basicContext.Size = new Size(146, 26);
            // 
            // deleteModBut
            // 
            deleteModBut.Name = "deleteModBut";
            deleteModBut.Size = new Size(145, 22);
            deleteModBut.Text = "Remove Mod";
            deleteModBut.Click += deleteModButton_Click;
            // 
            // addPackedMod
            // 
            addPackedMod.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            addPackedMod.Enabled = false;
            addPackedMod.Location = new Point(4, 488);
            addPackedMod.Name = "addPackedMod";
            addPackedMod.Size = new Size(227, 40);
            addPackedMod.TabIndex = 1;
            addPackedMod.Text = "Add mod (.zip)";
            addPackedMod.UseVisualStyleBackColor = true;
            addPackedMod.Click += addPackedMod_Click;
            // 
            // modOrderUp
            // 
            modOrderUp.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            modOrderUp.Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point);
            modOrderUp.Location = new Point(236, 21);
            modOrderUp.Name = "modOrderUp";
            modOrderUp.Size = new Size(28, 30);
            modOrderUp.TabIndex = 2;
            modOrderUp.Text = "↑";
            modOrderUp.UseVisualStyleBackColor = true;
            modOrderUp.Click += modOrderUp_Click;
            // 
            // modOrderDown
            // 
            modOrderDown.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            modOrderDown.Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point);
            modOrderDown.Location = new Point(236, 57);
            modOrderDown.Name = "modOrderDown";
            modOrderDown.Size = new Size(28, 30);
            modOrderDown.TabIndex = 3;
            modOrderDown.Text = "↓";
            modOrderDown.UseVisualStyleBackColor = true;
            modOrderDown.Click += modOrderDown_Click;
            // 
            // modList
            // 
            modList.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            modList.FormattingEnabled = true;
            modList.Location = new Point(4, 26);
            modList.Name = "modList";
            modList.Size = new Size(226, 400);
            modList.TabIndex = 4;
            modList.ItemCheck += modList_ItemCheck;
            modList.SelectedIndexChanged += modList_SelectedIndexChanged;
            modList.MouseUp += modList_MouseUp;
            // 
            // applyModsBut
            // 
            applyModsBut.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            applyModsBut.Enabled = false;
            applyModsBut.Location = new Point(4, 576);
            applyModsBut.Name = "applyModsBut";
            applyModsBut.Size = new Size(227, 40);
            applyModsBut.TabIndex = 5;
            applyModsBut.Text = "Apply mods";
            applyModsBut.UseVisualStyleBackColor = true;
            applyModsBut.Click += applyModsBut_Click;
            // 
            // addFolderMod
            // 
            addFolderMod.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            addFolderMod.Enabled = false;
            addFolderMod.Location = new Point(4, 532);
            addFolderMod.Name = "addFolderMod";
            addFolderMod.Size = new Size(227, 40);
            addFolderMod.TabIndex = 7;
            addFolderMod.Text = "Add mod (folder)";
            addFolderMod.UseVisualStyleBackColor = true;
            addFolderMod.Click += addFolderMod_Click;
            // 
            // splitContainer1
            // 
            splitContainer1.Dock = DockStyle.Fill;
            splitContainer1.Location = new Point(0, 24);
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
            splitContainer1.Size = new Size(822, 621);
            splitContainer1.SplitterDistance = 271;
            splitContainer1.TabIndex = 8;
            // 
            // modPropertyGrid
            // 
            modPropertyGrid.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            modPropertyGrid.Location = new Point(2, 0);
            modPropertyGrid.Margin = new Padding(2);
            modPropertyGrid.Name = "modPropertyGrid";
            modPropertyGrid.Size = new Size(541, 624);
            modPropertyGrid.TabIndex = 0;
            // 
            // TrinityMainWindow
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(822, 645);
            Controls.Add(splitContainer1);
            Controls.Add(menuStrip1);
            MainMenuStrip = menuStrip1;
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
        private ToolStripMenuItem helpToolStripMenuItem;
        private ToolStripMenuItem aboutToolStripMenuItem;
        private ToolStripMenuItem optionsToolStripMenuItem;
        private ContextMenuStrip basicContext;
        private ToolStripMenuItem deleteModBut;
        private ToolStripMenuItem preferencesMenuItem;
        private ToolStripMenuItem romfsPathMenuItem;
        private ToolStripSeparator toolStripSeparator1;
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
        private PropertyGrid modPropertyGrid;
        private ToolStripMenuItem openModWindowMenuItem;
    }
}