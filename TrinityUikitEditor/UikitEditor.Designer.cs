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
            components = new System.ComponentModel.Container();
            messageListView = new ListView();
            columnHeader1 = new ColumnHeader();
            sceneView = new TreeView();
            renderControl1 = new GFTool.RenderControl_WinForms.RenderControl();
            menuStrip1 = new MenuStrip();
            fileToolStripMenuItem = new ToolStripMenuItem();
            openToolStripMenuItem = new ToolStripMenuItem();
            propertiesGroup = new GroupBox();
            InfoBox = new TextBox();
            sceneContext = new ContextMenuStrip(components);
            expandToolStripMenuItem = new ToolStripMenuItem();
            menuStrip1.SuspendLayout();
            propertiesGroup.SuspendLayout();
            sceneContext.SuspendLayout();
            SuspendLayout();
            // 
            // messageListView
            // 
            messageListView.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
            messageListView.Columns.AddRange(new ColumnHeader[] { columnHeader1 });
            messageListView.FullRowSelect = true;
            messageListView.GridLines = true;
            messageListView.LabelWrap = false;
            messageListView.Location = new Point(254, 753);
            messageListView.Name = "messageListView";
            messageListView.Size = new Size(1383, 124);
            messageListView.TabIndex = 6;
            messageListView.UseCompatibleStateImageBehavior = false;
            messageListView.View = View.Details;
            // 
            // columnHeader1
            // 
            columnHeader1.Text = "Messages";
            columnHeader1.Width = 1000;
            // 
            // sceneView
            // 
            sceneView.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            sceneView.Location = new Point(12, 27);
            sceneView.Name = "sceneView";
            sceneView.Size = new Size(236, 850);
            sceneView.TabIndex = 5;
            sceneView.MouseUp += sceneView_MouseUp;
            // 
            // renderControl1
            // 
            renderControl1.API = OpenTK.Windowing.Common.ContextAPI.OpenGL;
            renderControl1.APIVersion = new Version(3, 3, 0, 0);
            renderControl1.Flags = OpenTK.Windowing.Common.ContextFlags.Default;
            renderControl1.IsEventDriven = true;
            renderControl1.Location = new Point(254, 27);
            renderControl1.Name = "renderControl1";
            renderControl1.Profile = OpenTK.Windowing.Common.ContextProfile.Core;
            renderControl1.SharedContext = null;
            renderControl1.Size = new Size(1080, 720);
            renderControl1.TabIndex = 7;
            // 
            // menuStrip1
            // 
            menuStrip1.Items.AddRange(new ToolStripItem[] { fileToolStripMenuItem });
            menuStrip1.Location = new Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Size = new Size(1649, 24);
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
            // propertiesGroup
            // 
            propertiesGroup.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            propertiesGroup.Controls.Add(InfoBox);
            propertiesGroup.Location = new Point(1340, 27);
            propertiesGroup.Name = "propertiesGroup";
            propertiesGroup.Size = new Size(297, 720);
            propertiesGroup.TabIndex = 9;
            propertiesGroup.TabStop = false;
            propertiesGroup.Text = "Properties";
            // 
            // InfoBox
            // 
            InfoBox.BackColor = SystemColors.Control;
            InfoBox.BorderStyle = BorderStyle.None;
            InfoBox.Location = new Point(6, 22);
            InfoBox.Multiline = true;
            InfoBox.Name = "InfoBox";
            InfoBox.ReadOnly = true;
            InfoBox.Size = new Size(290, 190);
            InfoBox.TabIndex = 0;
            // 
            // sceneContext
            // 
            sceneContext.Items.AddRange(new ToolStripItem[] { expandToolStripMenuItem });
            sceneContext.Name = "sceneContext";
            sceneContext.Size = new Size(181, 48);
            // 
            // expandToolStripMenuItem
            // 
            expandToolStripMenuItem.Name = "expandToolStripMenuItem";
            expandToolStripMenuItem.Size = new Size(180, 22);
            expandToolStripMenuItem.Text = "Expand";
            // 
            // UikitEditor
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1649, 889);
            Controls.Add(propertiesGroup);
            Controls.Add(renderControl1);
            Controls.Add(messageListView);
            Controls.Add(sceneView);
            Controls.Add(menuStrip1);
            MainMenuStrip = menuStrip1;
            Name = "UikitEditor";
            Text = "Trinity Uikit Editor";
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            propertiesGroup.ResumeLayout(false);
            propertiesGroup.PerformLayout();
            sceneContext.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private ListView messageListView;
        private ColumnHeader columnHeader1;
        private TreeView sceneView;
        private GFTool.RenderControl_WinForms.RenderControl renderControl1;
        private MenuStrip menuStrip1;
        private ToolStripMenuItem fileToolStripMenuItem;
        private ToolStripMenuItem openToolStripMenuItem;
        private GroupBox propertiesGroup;
        private TextBox InfoBox;
        private ContextMenuStrip sceneContext;
        private ToolStripMenuItem expandToolStripMenuItem;
    }
}
