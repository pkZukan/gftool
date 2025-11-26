using TrinityModLoader.UI;

namespace TrinityModLoader
{
    partial class TrinityModLoaderWindow
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(TrinityModLoaderWindow));
            modLoaderMenuStrip = new MenuStrip();
            fileToolStripMenuItem = new ToolStripMenuItem();
            newModPackMenuItem = new ToolStripMenuItem();
            openModPackMenuItems = new ToolStripMenuItem();
            chooseModPackMenuItem = new ToolStripMenuItem();
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
            listContext = new ContextMenuStrip(components);
            deleteModBut = new ToolStripMenuItem();
            splitContainer1 = new SplitContainer();
            modListToolStrip = new ToolStrip();
            addPackedMod = new ToolStripButton();
            addFolderMod = new ToolStripButton();
            applyModsBut = new ToolStripButton();
            toolStripSeparator4 = new ToolStripSeparator();
            enableAllButton = new ToolStripButton();
            disableAllButton = new ToolStripButton();
            toolStripSeparator6 = new ToolStripSeparator();
            modOrderDownButton = new ToolStripButton();
            modOrderUpButton = new ToolStripButton();
            splitContainer2 = new SplitContainer();
            ModDescriptionBox = new RichTextBox();
            ModNameLabel = new Label();
            ModAuthorLabel = new Label();
            ModVersionLabel = new Label();
            ModPathLabel = new Label();
            modToolStrip = new ToolStrip();
            refreshModButton = new ToolStripButton();
            toolStripSeparator5 = new ToolStripSeparator();
            fileView = new ListBox();
            modList = new CheckedModListBox();
            modLoaderMenuStrip.SuspendLayout();
            listContext.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            modListToolStrip.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer2).BeginInit();
            splitContainer2.Panel1.SuspendLayout();
            splitContainer2.Panel2.SuspendLayout();
            splitContainer2.SuspendLayout();
            modToolStrip.SuspendLayout();
            SuspendLayout();
            // 
            // modLoaderMenuStrip
            // 
            modLoaderMenuStrip.ImageScalingSize = new Size(24, 24);
            modLoaderMenuStrip.Items.AddRange(new ToolStripItem[] { fileToolStripMenuItem, helpToolStripMenuItem });
            modLoaderMenuStrip.Location = new Point(0, 0);
            modLoaderMenuStrip.Name = "modLoaderMenuStrip";
            modLoaderMenuStrip.Size = new Size(822, 24);
            modLoaderMenuStrip.TabIndex = 2;
            modLoaderMenuStrip.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            fileToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { newModPackMenuItem, openModPackMenuItems, toolStripSeparator2, toolStripMenuItem6 });
            fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            fileToolStripMenuItem.Size = new Size(37, 20);
            fileToolStripMenuItem.Text = "File";
            // 
            // newModPackMenuItem
            // 
            newModPackMenuItem.Name = "newModPackMenuItem";
            newModPackMenuItem.Size = new Size(163, 22);
            newModPackMenuItem.Text = "New Mod Pack";
            newModPackMenuItem.Click += newModPackMenuItem_Click;
            // 
            // OpenModPackMenuItems
            // 
            openModPackMenuItems.DropDownItems.AddRange(new ToolStripItem[] { chooseModPackMenuItem, toolStripSeparator3 });
            openModPackMenuItems.Name = "OpenModPackMenuItems";
            openModPackMenuItems.Size = new Size(163, 22);
            openModPackMenuItems.Text = "Open Mod Pack";
            // 
            // ChooseModPackMenuItem
            // 
            chooseModPackMenuItem.Name = "ChooseModPackMenuItem";
            chooseModPackMenuItem.Size = new Size(123, 22);
            chooseModPackMenuItem.Text = "Choose...";
            chooseModPackMenuItem.Click += chooseModPackMenuItem_Click;
            // 
            // toolStripSeparator3
            // 
            toolStripSeparator3.Name = "toolStripSeparator3";
            toolStripSeparator3.Size = new Size(120, 6);
            // 
            // toolStripSeparator2
            // 
            toolStripSeparator2.Name = "toolStripSeparator2";
            toolStripSeparator2.Size = new Size(160, 6);
            // 
            // toolStripMenuItem6
            // 
            toolStripMenuItem6.Name = "toolStripMenuItem6";
            toolStripMenuItem6.Size = new Size(163, 22);
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
            preferencesMenuItem.Size = new Size(210, 22);
            preferencesMenuItem.Text = "Preferences";
            // 
            // romfsPathMenuItem
            // 
            romfsPathMenuItem.Name = "romfsPathMenuItem";
            romfsPathMenuItem.Size = new Size(259, 22);
            romfsPathMenuItem.Text = "Change RomFS Path...";
            romfsPathMenuItem.Click += openRomFSFolderToolStripMenuItem_Click;
            // 
            // openModWindowMenuItem
            // 
            openModWindowMenuItem.Name = "openModWindowMenuItem";
            openModWindowMenuItem.Size = new Size(259, 22);
            openModWindowMenuItem.Text = "Show Explorer after Applying Mods";
            openModWindowMenuItem.Click += openModWindowMenuItem_Click;
            // 
            // toolStripSeparator1
            // 
            toolStripSeparator1.Name = "toolStripSeparator1";
            toolStripSeparator1.Size = new Size(207, 6);
            // 
            // aboutToolStripMenuItem
            // 
            aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
            aboutToolStripMenuItem.Size = new Size(210, 22);
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
            // listContext
            // 
            listContext.ImageScalingSize = new Size(24, 24);
            listContext.Items.AddRange(new ToolStripItem[] { deleteModBut });
            listContext.Name = "basicContext";
            listContext.Size = new Size(146, 26);
            // 
            // deleteModBut
            // 
            deleteModBut.Name = "deleteModBut";
            deleteModBut.Size = new Size(145, 22);
            deleteModBut.Text = "Remove Mod";
            deleteModBut.Click += deleteModButton_Click;
            // 
            // splitContainer1
            // 
            splitContainer1.Dock = DockStyle.Fill;
            splitContainer1.Location = new Point(0, 24);
            splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            splitContainer1.Panel1.Controls.Add(modListToolStrip);
            splitContainer1.Panel1.Controls.Add(modList);
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(splitContainer2);
            splitContainer1.Size = new Size(822, 683);
            splitContainer1.SplitterDistance = 274;
            splitContainer1.TabIndex = 1;
            // 
            // modListToolStrip
            // 
            modListToolStrip.BackColor = SystemColors.Control;
            modListToolStrip.GripStyle = ToolStripGripStyle.Hidden;
            modListToolStrip.ImeMode = ImeMode.NoControl;
            modListToolStrip.Items.AddRange(new ToolStripItem[] { addPackedMod, addFolderMod, applyModsBut, toolStripSeparator4, enableAllButton, disableAllButton, toolStripSeparator6, modOrderDownButton, modOrderUpButton });
            modListToolStrip.Location = new Point(0, 0);
            modListToolStrip.Name = "modListToolStrip";
            modListToolStrip.Size = new Size(274, 25);
            modListToolStrip.TabIndex = 8;
            modListToolStrip.Text = "toolStrip1";
            // 
            // addPackedMod
            // 
            addPackedMod.DisplayStyle = ToolStripItemDisplayStyle.Image;
            addPackedMod.Image = Properties.Resources.folder_archive;
            addPackedMod.ImageTransparentColor = Color.Magenta;
            addPackedMod.Name = "addPackedMod";
            addPackedMod.Size = new Size(23, 22);
            addPackedMod.Text = "Add mod (.zip)";
            addPackedMod.Click += addPackedMod_Click;
            // 
            // addFolderMod
            // 
            addFolderMod.DisplayStyle = ToolStripItemDisplayStyle.Image;
            addFolderMod.Image = Properties.Resources.folder_plus;
            addFolderMod.ImageTransparentColor = Color.Magenta;
            addFolderMod.Name = "addFolderMod";
            addFolderMod.Size = new Size(23, 22);
            addFolderMod.Text = "Add mod (folder)";
            addFolderMod.Click += addFolderMod_Click;
            // 
            // applyModsBut
            // 
            applyModsBut.DisplayStyle = ToolStripItemDisplayStyle.Image;
            applyModsBut.Image = Properties.Resources.folder_down;
            applyModsBut.ImageTransparentColor = Color.Magenta;
            applyModsBut.Name = "applyModsBut";
            applyModsBut.Size = new Size(23, 22);
            applyModsBut.Text = "Apply Mods";
            applyModsBut.Click += applyModsBut_Click;
            // 
            // toolStripSeparator4
            // 
            toolStripSeparator4.Name = "toolStripSeparator4";
            toolStripSeparator4.Size = new Size(6, 25);
            // 
            // enableAllButton
            // 
            enableAllButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
            enableAllButton.Image = Properties.Resources.list_check;
            enableAllButton.ImageTransparentColor = Color.Magenta;
            enableAllButton.Name = "enableAllButton";
            enableAllButton.Size = new Size(23, 22);
            enableAllButton.Text = "Check All";
            enableAllButton.Click += enableAllButton_Click;
            // 
            // disableAllButton
            // 
            disableAllButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
            disableAllButton.Image = Properties.Resources.list_x;
            disableAllButton.ImageTransparentColor = Color.Magenta;
            disableAllButton.Name = "disableAllButton";
            disableAllButton.Size = new Size(23, 22);
            disableAllButton.Text = "Uncheck All";
            disableAllButton.Click += disableAllButton_Click;
            // 
            // toolStripSeparator6
            // 
            toolStripSeparator6.Name = "toolStripSeparator6";
            toolStripSeparator6.Size = new Size(6, 25);
            // 
            // modOrderDownButton
            // 
            modOrderDownButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
            modOrderDownButton.Image = (Image)resources.GetObject("modOrderDownButton.Image");
            modOrderDownButton.ImageTransparentColor = Color.Magenta;
            modOrderDownButton.Name = "modOrderDownButton";
            modOrderDownButton.Size = new Size(23, 22);
            modOrderDownButton.Text = "↓";
            modOrderDownButton.ToolTipText = "Move Down";
            modOrderDownButton.Click += modOrderDown_Click;
            // 
            // modOrderUpButton
            // 
            modOrderUpButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
            modOrderUpButton.Image = (Image)resources.GetObject("modOrderUpButton.Image");
            modOrderUpButton.ImageTransparentColor = Color.Magenta;
            modOrderUpButton.Name = "modOrderUpButton";
            modOrderUpButton.Size = new Size(23, 22);
            modOrderUpButton.Text = "↑";
            modOrderUpButton.ToolTipText = "Move Up";
            modOrderUpButton.Click += modOrderUp_Click;
            // 
            // splitContainer2
            // 
            splitContainer2.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            splitContainer2.FixedPanel = FixedPanel.Panel2;
            splitContainer2.Location = new Point(0, 0);
            splitContainer2.Name = "splitContainer2";
            splitContainer2.Orientation = Orientation.Horizontal;
            // 
            // splitContainer2.Panel1
            // 
            splitContainer2.Panel1.Controls.Add(ModDescriptionBox);
            splitContainer2.Panel1.Controls.Add(ModNameLabel);
            splitContainer2.Panel1.Controls.Add(ModAuthorLabel);
            splitContainer2.Panel1.Controls.Add(ModVersionLabel);
            splitContainer2.Panel1.Controls.Add(ModPathLabel);
            splitContainer2.Panel1.Controls.Add(modToolStrip);
            // 
            // splitContainer2.Panel2
            // 
            splitContainer2.Panel2.Controls.Add(fileView);
            splitContainer2.Size = new Size(540, 683);
            splitContainer2.SplitterDistance = 369;
            splitContainer2.TabIndex = 0;
            // 
            // ModDescriptionBox
            // 
            ModDescriptionBox.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            ModDescriptionBox.BackColor = SystemColors.Window;
            ModDescriptionBox.BorderStyle = BorderStyle.FixedSingle;
            ModDescriptionBox.CausesValidation = false;
            ModDescriptionBox.Location = new Point(0, 94);
            ModDescriptionBox.Name = "ModDescriptionBox";
            ModDescriptionBox.ReadOnly = true;
            ModDescriptionBox.ShortcutsEnabled = false;
            ModDescriptionBox.Size = new Size(537, 272);
            ModDescriptionBox.TabIndex = 5;
            ModDescriptionBox.Text = "";
            // 
            // ModNameLabel
            // 
            ModNameLabel.AutoSize = true;
            ModNameLabel.Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
            ModNameLabel.Location = new Point(3, 28);
            ModNameLabel.Name = "ModNameLabel";
            ModNameLabel.Size = new Size(56, 21);
            ModNameLabel.TabIndex = 2;
            ModNameLabel.Text = "Name";
            // 
            // ModAuthorLabel
            // 
            ModAuthorLabel.AutoSize = true;
            ModAuthorLabel.Location = new Point(3, 55);
            ModAuthorLabel.Name = "ModAuthorLabel";
            ModAuthorLabel.Size = new Size(44, 15);
            ModAuthorLabel.TabIndex = 3;
            ModAuthorLabel.Text = "Author";
            // 
            // ModVersionLabel
            // 
            ModVersionLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            ModVersionLabel.AutoSize = true;
            ModVersionLabel.Location = new Point(519, 28);
            ModVersionLabel.Name = "ModVersionLabel";
            ModVersionLabel.Size = new Size(0, 15);
            ModVersionLabel.TabIndex = 6;
            ModVersionLabel.TextAlign = ContentAlignment.MiddleRight;
            // 
            // ModPathLabel
            // 
            ModPathLabel.AutoSize = true;
            ModPathLabel.ForeColor = SystemColors.GrayText;
            ModPathLabel.Location = new Point(3, 76);
            ModPathLabel.Name = "ModPathLabel";
            ModPathLabel.Size = new Size(31, 15);
            ModPathLabel.TabIndex = 4;
            ModPathLabel.Text = "Path";
            // 
            // modToolStrip
            // 
            modToolStrip.GripStyle = ToolStripGripStyle.Hidden;
            modToolStrip.Items.AddRange(new ToolStripItem[] { refreshModButton, toolStripSeparator5 });
            modToolStrip.Location = new Point(0, 0);
            modToolStrip.Name = "modToolStrip";
            modToolStrip.Size = new Size(540, 25);
            modToolStrip.TabIndex = 1;
            modToolStrip.Text = "toolStrip2";
            // 
            // refreshModButton
            // 
            refreshModButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
            refreshModButton.Image = Properties.Resources.folder_sync;
            refreshModButton.ImageTransparentColor = Color.Magenta;
            refreshModButton.Name = "refreshModButton";
            refreshModButton.Size = new Size(23, 22);
            refreshModButton.Text = "Refresh Mod";
            refreshModButton.Click += refreshModButton_Click;
            // 
            // toolStripSeparator5
            // 
            toolStripSeparator5.Name = "toolStripSeparator5";
            toolStripSeparator5.Size = new Size(6, 25);
            // 
            // fileView
            // 
            fileView.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            fileView.BackColor = SystemColors.Control;
            fileView.FormattingEnabled = true;
            fileView.IntegralHeight = false;
            fileView.ItemHeight = 15;
            fileView.Location = new Point(0, 0);
            fileView.Name = "fileView";
            fileView.Size = new Size(540, 307);
            fileView.TabIndex = 0;
            // 
            // modList
            // 
            modList.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            modList.FormattingEnabled = true;
            modList.IntegralHeight = false;
            modList.Location = new Point(0, 28);
            modList.Name = "modList";
            modList.Size = new Size(274, 652);
            modList.TabIndex = 4;
            modList.SelectedIndexChanged += modList_SelectedIndexChanged;
            modList.MouseUp += modList_MouseUp;
            // 
            // TrinityModLoaderWindow
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(822, 707);
            Controls.Add(splitContainer1);
            Controls.Add(modLoaderMenuStrip);
            MainMenuStrip = modLoaderMenuStrip;
            Name = "TrinityModLoaderWindow";
            Text = "Trinity Mod Loader";
            FormClosing += TrinityMainWindow_FormClosing;
            modLoaderMenuStrip.ResumeLayout(false);
            modLoaderMenuStrip.PerformLayout();
            listContext.ResumeLayout(false);
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel1.PerformLayout();
            splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            modListToolStrip.ResumeLayout(false);
            modListToolStrip.PerformLayout();
            splitContainer2.Panel1.ResumeLayout(false);
            splitContainer2.Panel1.PerformLayout();
            splitContainer2.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer2).EndInit();
            splitContainer2.ResumeLayout(false);
            modToolStrip.ResumeLayout(false);
            modToolStrip.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private MenuStrip modLoaderMenuStrip;
        private ToolStripMenuItem fileToolStripMenuItem;
        private HelpProvider helpProvider1;
        private ToolStripMenuItem saveFileToolStripMenuItem;
        private ToolStripMenuItem markForLayeredFSToolStripMenuItem;
        private ToolStripMenuItem helpToolStripMenuItem;
        private ToolStripMenuItem aboutToolStripMenuItem;
        private ToolStripMenuItem optionsToolStripMenuItem;
        private ContextMenuStrip listContext;
        private ToolStripMenuItem deleteModBut;
        private ToolStripMenuItem preferencesMenuItem;
        private ToolStripMenuItem romfsPathMenuItem;
        private ToolStripSeparator toolStripSeparator1;
        private ToolStripMenuItem newModPackMenuItem;
        private ToolStripMenuItem toolStripMenuItem6;
        private ToolStripSeparator toolStripSeparator2;
        private ToolStripMenuItem openModPackMenuItems;
        private ToolStripMenuItem chooseModPackMenuItem;
        private ToolStripSeparator toolStripSeparator3;
        private SplitContainer splitContainer1;
        private ToolStripMenuItem openModWindowMenuItem;
        private ToolStrip modListToolStrip;
        private ToolStripSeparator toolStripSeparator4;
        private ToolStripButton modOrderUpButton;
        private ToolStripButton modOrderDownButton;
        private ToolStripButton addPackedMod;
        private ToolStripButton addFolderMod;
        private ToolStripButton applyModsBut;
        private ToolStripSeparator toolStripSeparator6;
        private SplitContainer splitContainer2;
        private ListBox fileView;
        private ToolStrip modToolStrip;
        private ToolStripButton refreshModButton;
        private ToolStripButton enableAllButton;
        private ToolStripButton disableAllButton;
        private Label ModNameLabel;
        private Label ModAuthorLabel;
        private Label ModPathLabel;
        private RichTextBox ModDescriptionBox;
        private Label ModVersionLabel;
        private ToolStripSeparator toolStripSeparator5;
        private CheckedModListBox modList;
    }
}