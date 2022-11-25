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
            this.exportPackFileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exportPackContentsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.editToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.helpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.fileView = new System.Windows.Forms.TreeView();
            this.helpProvider1 = new System.Windows.Forms.HelpProvider();
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.statusLbl = new System.Windows.Forms.Label();
            this.treeContext = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.saveFileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.markForLayeredFSToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.menuStrip1.SuspendLayout();
            this.treeContext.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.editToolStripMenuItem,
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
            this.exportPackFileToolStripMenuItem,
            this.exportPackContentsToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // openFileDescriptorToolStripMenuItem
            // 
            this.openFileDescriptorToolStripMenuItem.Name = "openFileDescriptorToolStripMenuItem";
            this.openFileDescriptorToolStripMenuItem.Size = new System.Drawing.Size(192, 22);
            this.openFileDescriptorToolStripMenuItem.Text = "Open File Descriptor";
            this.openFileDescriptorToolStripMenuItem.Click += new System.EventHandler(this.openFileDescriptorToolStripMenuItem_Click);
            // 
            // saveFileDescriptorAsToolStripMenuItem
            // 
            this.saveFileDescriptorAsToolStripMenuItem.Name = "saveFileDescriptorAsToolStripMenuItem";
            this.saveFileDescriptorAsToolStripMenuItem.Size = new System.Drawing.Size(192, 22);
            this.saveFileDescriptorAsToolStripMenuItem.Text = "Save File Descriptor As";
            this.saveFileDescriptorAsToolStripMenuItem.Click += new System.EventHandler(this.saveFileDescriptorAsToolStripMenuItem_Click);
            // 
            // exportPackFileToolStripMenuItem
            // 
            this.exportPackFileToolStripMenuItem.Name = "exportPackFileToolStripMenuItem";
            this.exportPackFileToolStripMenuItem.Size = new System.Drawing.Size(192, 22);
            this.exportPackFileToolStripMenuItem.Text = "Export Pack File";
            this.exportPackFileToolStripMenuItem.Click += new System.EventHandler(this.exportPackFileToolStripMenuItem_Click);
            // 
            // exportPackContentsToolStripMenuItem
            // 
            this.exportPackContentsToolStripMenuItem.Name = "exportPackContentsToolStripMenuItem";
            this.exportPackContentsToolStripMenuItem.Size = new System.Drawing.Size(192, 22);
            this.exportPackContentsToolStripMenuItem.Text = "Export Pack Contents";
            this.exportPackContentsToolStripMenuItem.Click += new System.EventHandler(this.exportPackContentsToolStripMenuItem_Click);
            // 
            // editToolStripMenuItem
            // 
            this.editToolStripMenuItem.Name = "editToolStripMenuItem";
            this.editToolStripMenuItem.Size = new System.Drawing.Size(39, 20);
            this.editToolStripMenuItem.Text = "Edit";
            // 
            // helpToolStripMenuItem
            // 
            this.helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            this.helpToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
            this.helpToolStripMenuItem.Text = "Help";
            // 
            // fileView
            // 
            this.fileView.Location = new System.Drawing.Point(12, 27);
            this.fileView.Name = "fileView";
            this.fileView.Size = new System.Drawing.Size(557, 412);
            this.fileView.TabIndex = 3;
            this.fileView.MouseUp += new System.Windows.Forms.MouseEventHandler(this.fileView_MouseUp);
            // 
            // progressBar1
            // 
            this.progressBar1.Location = new System.Drawing.Point(312, 445);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(257, 15);
            this.progressBar1.TabIndex = 4;
            // 
            // statusLbl
            // 
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
            // FileSystemForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(581, 471);
            this.Controls.Add(this.statusLbl);
            this.Controls.Add(this.progressBar1);
            this.Controls.Add(this.fileView);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "FileSystemForm";
            this.Text = "Filesystem Explorer";
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.treeContext.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private MenuStrip menuStrip1;
        private ToolStripMenuItem fileToolStripMenuItem;
        private ToolStripMenuItem editToolStripMenuItem;
        private ToolStripMenuItem helpToolStripMenuItem;
        private ToolStripMenuItem openFileDescriptorToolStripMenuItem;
        private ToolStripMenuItem exportPackFileToolStripMenuItem;
        private ToolStripMenuItem exportPackContentsToolStripMenuItem;
        private TreeView fileView;
        private HelpProvider helpProvider1;
        private ProgressBar progressBar1;
        private Label statusLbl;
        private ContextMenuStrip treeContext;
        private ToolStripMenuItem saveFileToolStripMenuItem;
        private ToolStripMenuItem markForLayeredFSToolStripMenuItem;
        private ToolStripMenuItem saveFileDescriptorAsToolStripMenuItem;
    }
}