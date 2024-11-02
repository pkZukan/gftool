namespace TrinitySceneView
{
    partial class SceneViewerForm
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
            openTRSOT = new ToolStripMenuItem();
            viewToolStripMenuItem = new ToolStripMenuItem();
            toolStripMenuItem1 = new ToolStripMenuItem();
            toolstripGBuf_All = new ToolStripMenuItem();
            toolstripGBuf_Albedo = new ToolStripMenuItem();
            toolstripGBuf_Normal = new ToolStripMenuItem();
            toolstripGBuf_Specular = new ToolStripMenuItem();
            toolstripGBuf_AO = new ToolStripMenuItem();
            toolstripGBuf_Depth = new ToolStripMenuItem();
            sceneView = new TreeView();
            sceneContext = new ContextMenuStrip(components);
            expandToolStripMenuItem = new ToolStripMenuItem();
            glCtxt = new OpenTK.GLControl.GLControl();
            statusLbl = new Label();
            keyTimer = new System.Windows.Forms.Timer(components);
            messageListView = new ListView();
            columnHeader1 = new ColumnHeader();
            menuStrip1.SuspendLayout();
            sceneContext.SuspendLayout();
            SuspendLayout();
            // 
            // menuStrip1
            // 
            menuStrip1.Items.AddRange(new ToolStripItem[] { fileToolStripMenuItem, viewToolStripMenuItem });
            menuStrip1.Location = new Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Size = new Size(1384, 24);
            menuStrip1.TabIndex = 0;
            menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            fileToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { openTRSOT });
            fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            fileToolStripMenuItem.Size = new Size(37, 20);
            fileToolStripMenuItem.Text = "File";
            // 
            // openTRSOT
            // 
            openTRSOT.Name = "openTRSOT";
            openTRSOT.Size = new Size(175, 22);
            openTRSOT.Text = "Open Scene Object";
            openTRSOT.Click += openTRSOT_Click;
            // 
            // viewToolStripMenuItem
            // 
            viewToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { toolStripMenuItem1 });
            viewToolStripMenuItem.Name = "viewToolStripMenuItem";
            viewToolStripMenuItem.Size = new Size(44, 20);
            viewToolStripMenuItem.Text = "View";
            // 
            // toolStripMenuItem1
            // 
            toolStripMenuItem1.DropDownItems.AddRange(new ToolStripItem[] { toolstripGBuf_All, toolstripGBuf_Albedo, toolstripGBuf_Normal, toolstripGBuf_Specular, toolstripGBuf_AO, toolstripGBuf_Depth });
            toolStripMenuItem1.Name = "toolStripMenuItem1";
            toolStripMenuItem1.Size = new Size(114, 22);
            toolStripMenuItem1.Text = "GBuffer";
            // 
            // toolstripGBuf_All
            // 
            toolstripGBuf_All.Checked = true;
            toolstripGBuf_All.CheckState = CheckState.Checked;
            toolstripGBuf_All.Name = "toolstripGBuf_All";
            toolstripGBuf_All.Size = new Size(119, 22);
            toolstripGBuf_All.Text = "All";
            toolstripGBuf_All.Click += toolstripGBuf_Clicked;
            // 
            // toolstripGBuf_Albedo
            // 
            toolstripGBuf_Albedo.Name = "toolstripGBuf_Albedo";
            toolstripGBuf_Albedo.Size = new Size(119, 22);
            toolstripGBuf_Albedo.Text = "Albedo";
            toolstripGBuf_Albedo.Click += toolstripGBuf_Clicked;
            // 
            // toolstripGBuf_Normal
            // 
            toolstripGBuf_Normal.Name = "toolstripGBuf_Normal";
            toolstripGBuf_Normal.Size = new Size(119, 22);
            toolstripGBuf_Normal.Text = "Normal";
            toolstripGBuf_Normal.Click += toolstripGBuf_Clicked;
            // 
            // toolstripGBuf_Specular
            // 
            toolstripGBuf_Specular.Name = "toolstripGBuf_Specular";
            toolstripGBuf_Specular.Size = new Size(119, 22);
            toolstripGBuf_Specular.Text = "Specular";
            toolstripGBuf_Specular.Click += toolstripGBuf_Clicked;
            // 
            // toolstripGBuf_AO
            // 
            toolstripGBuf_AO.Name = "toolstripGBuf_AO";
            toolstripGBuf_AO.Size = new Size(119, 22);
            toolstripGBuf_AO.Text = "AO";
            // 
            // toolstripGBuf_Depth
            // 
            toolstripGBuf_Depth.Name = "toolstripGBuf_Depth";
            toolstripGBuf_Depth.Size = new Size(119, 22);
            toolstripGBuf_Depth.Text = "Depth";
            toolstripGBuf_Depth.Click += toolstripGBuf_Clicked;
            // 
            // sceneView
            // 
            sceneView.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            sceneView.Location = new Point(12, 27);
            sceneView.Name = "sceneView";
            sceneView.Size = new Size(274, 880);
            sceneView.TabIndex = 1;
            sceneView.MouseUp += sceneView_MouseUp;
            // 
            // sceneContext
            // 
            sceneContext.Items.AddRange(new ToolStripItem[] { expandToolStripMenuItem });
            sceneContext.Name = "sceneContext";
            sceneContext.Size = new Size(114, 26);
            // 
            // expandToolStripMenuItem
            // 
            expandToolStripMenuItem.Name = "expandToolStripMenuItem";
            expandToolStripMenuItem.Size = new Size(113, 22);
            expandToolStripMenuItem.Text = "Expand";
            expandToolStripMenuItem.Click += expandToolStripMenuItem_Click;
            // 
            // glCtxt
            // 
            glCtxt.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            glCtxt.API = OpenTK.Windowing.Common.ContextAPI.OpenGL;
            glCtxt.APIVersion = new Version(4, 2, 0, 0);
            glCtxt.Flags = OpenTK.Windowing.Common.ContextFlags.Default;
            glCtxt.IsEventDriven = true;
            glCtxt.Location = new Point(292, 27);
            glCtxt.Name = "glCtxt";
            glCtxt.Profile = OpenTK.Windowing.Common.ContextProfile.Core;
            glCtxt.SharedContext = null;
            glCtxt.Size = new Size(1080, 720);
            glCtxt.TabIndex = 2;
            glCtxt.Load += glCtxt_Load;
            glCtxt.Paint += glCtxt_Paint;
            glCtxt.MouseMove += glCtxt_MouseMove;
            // 
            // statusLbl
            // 
            statusLbl.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            statusLbl.Location = new Point(292, 910);
            statusLbl.Name = "statusLbl";
            statusLbl.Size = new Size(1080, 14);
            statusLbl.TabIndex = 3;
            statusLbl.Text = "label1";
            // 
            // keyTimer
            // 
            keyTimer.Enabled = true;
            keyTimer.Interval = 10;
            keyTimer.Tick += keyTimer_Tick;
            // 
            // messageListView
            // 
            messageListView.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
            messageListView.Columns.AddRange(new ColumnHeader[] { columnHeader1 });
            messageListView.FullRowSelect = true;
            messageListView.GridLines = true;
            messageListView.LabelWrap = false;
            messageListView.Location = new Point(292, 753);
            messageListView.Name = "messageListView";
            messageListView.Size = new Size(1080, 154);
            messageListView.TabIndex = 4;
            messageListView.UseCompatibleStateImageBehavior = false;
            messageListView.View = View.Details;
            // 
            // columnHeader1
            // 
            columnHeader1.Text = "Messages";
            columnHeader1.Width = 1076;
            // 
            // SceneViewerForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1384, 933);
            Controls.Add(messageListView);
            Controls.Add(statusLbl);
            Controls.Add(glCtxt);
            Controls.Add(sceneView);
            Controls.Add(menuStrip1);
            KeyPreview = true;
            MainMenuStrip = menuStrip1;
            Name = "SceneViewerForm";
            Text = "Trinity Scene View";
            KeyDown += glCtxt_KeyDown;
            KeyUp += glCtxt_KeyUp;
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            sceneContext.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private MenuStrip menuStrip1;
        private ToolStripMenuItem fileToolStripMenuItem;
        private ToolStripMenuItem openTRSOT;
        private TreeView sceneView;
        private ContextMenuStrip sceneContext;
        private ToolStripMenuItem expandToolStripMenuItem;
        private OpenTK.GLControl.GLControl glCtxt;
        private Label statusLbl;
        private ToolStripMenuItem viewToolStripMenuItem;
        private ToolStripMenuItem toolStripMenuItem1;
        private ToolStripMenuItem toolstripGBuf_All;
        private ToolStripMenuItem toolstripGBuf_Albedo;
        private ToolStripMenuItem toolstripGBuf_Normal;
        private ToolStripMenuItem toolstripGBuf_Depth;
        private ToolStripMenuItem toolstripGBuf_Specular;
        private System.Windows.Forms.Timer keyTimer;
        private ToolStripMenuItem toolstripGBuf_AO;
        private ListView messageListView;
        private ColumnHeader columnHeader1;
    }
}