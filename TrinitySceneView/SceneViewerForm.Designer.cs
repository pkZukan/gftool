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
            statusLbl = new Label();
            messageListView = new ListView();
            columnHeader1 = new ColumnHeader();
            propertiesGroup = new GroupBox();
            PropertiesBox = new TextBox();
            renderCtrl = new GFTool.RenderControl_WinForms.RenderControl();
            tableLayoutPanel1 = new TableLayoutPanel();
            tableLayoutPanel2 = new TableLayoutPanel();
            tableLayoutPanel3 = new TableLayoutPanel();
            panel1 = new Panel();
            groupBox1 = new GroupBox();
            AttribBox = new TextBox();
            menuStrip1.SuspendLayout();
            sceneContext.SuspendLayout();
            propertiesGroup.SuspendLayout();
            tableLayoutPanel1.SuspendLayout();
            tableLayoutPanel2.SuspendLayout();
            tableLayoutPanel3.SuspendLayout();
            panel1.SuspendLayout();
            groupBox1.SuspendLayout();
            SuspendLayout();
            // 
            // menuStrip1
            // 
            menuStrip1.Items.AddRange(new ToolStripItem[] { fileToolStripMenuItem, viewToolStripMenuItem });
            menuStrip1.Location = new Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Size = new Size(1705, 24);
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
            sceneView.BorderStyle = BorderStyle.FixedSingle;
            sceneView.Location = new Point(3, 3);
            sceneView.Name = "sceneView";
            sceneView.Size = new Size(296, 907);
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
            // statusLbl
            // 
            statusLbl.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            statusLbl.Location = new Point(12, 943);
            statusLbl.Name = "statusLbl";
            statusLbl.Size = new Size(1080, 14);
            statusLbl.TabIndex = 3;
            statusLbl.Text = "label1";
            // 
            // messageListView
            // 
            messageListView.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            messageListView.BorderStyle = BorderStyle.FixedSingle;
            messageListView.Columns.AddRange(new ColumnHeader[] { columnHeader1 });
            messageListView.FullRowSelect = true;
            messageListView.GridLines = true;
            messageListView.LabelWrap = false;
            messageListView.Location = new Point(3, 729);
            messageListView.Name = "messageListView";
            messageListView.Size = new Size(1373, 181);
            messageListView.TabIndex = 4;
            messageListView.UseCompatibleStateImageBehavior = false;
            messageListView.View = View.Details;
            // 
            // columnHeader1
            // 
            columnHeader1.Text = "Messages";
            columnHeader1.Width = 1000;
            // 
            // propertiesGroup
            // 
            propertiesGroup.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            propertiesGroup.Controls.Add(PropertiesBox);
            propertiesGroup.Location = new Point(3, 3);
            propertiesGroup.Name = "propertiesGroup";
            propertiesGroup.Size = new Size(287, 220);
            propertiesGroup.TabIndex = 5;
            propertiesGroup.TabStop = false;
            propertiesGroup.Text = "Properties";
            // 
            // PropertiesBox
            // 
            PropertiesBox.BackColor = SystemColors.Control;
            PropertiesBox.BorderStyle = BorderStyle.None;
            PropertiesBox.Dock = DockStyle.Fill;
            PropertiesBox.Location = new Point(3, 19);
            PropertiesBox.Multiline = true;
            PropertiesBox.Name = "PropertiesBox";
            PropertiesBox.ReadOnly = true;
            PropertiesBox.Size = new Size(281, 198);
            PropertiesBox.TabIndex = 0;
            // 
            // renderCtrl
            // 
            renderCtrl.API = OpenTK.Windowing.Common.ContextAPI.OpenGL;
            renderCtrl.APIVersion = new Version(3, 3, 0, 0);
            renderCtrl.Dock = DockStyle.Fill;
            renderCtrl.Flags = OpenTK.Windowing.Common.ContextFlags.Default;
            renderCtrl.IsEventDriven = true;
            renderCtrl.Location = new Point(3, 3);
            renderCtrl.Name = "renderCtrl";
            renderCtrl.Profile = OpenTK.Windowing.Common.ContextProfile.Core;
            renderCtrl.SharedContext = null;
            renderCtrl.Size = new Size(1080, 720);
            renderCtrl.TabIndex = 6;
            renderCtrl.Load += glCtxt_Load;
            renderCtrl.Click += renderCtrl_Click;
            renderCtrl.Paint += glCtxt_Paint;
            // 
            // tableLayoutPanel1
            // 
            tableLayoutPanel1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            tableLayoutPanel1.ColumnCount = 2;
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 18.00487F));
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 81.99513F));
            tableLayoutPanel1.Controls.Add(sceneView, 0, 0);
            tableLayoutPanel1.Controls.Add(tableLayoutPanel2, 1, 0);
            tableLayoutPanel1.Location = new Point(12, 27);
            tableLayoutPanel1.Margin = new Padding(0);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 1;
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel1.Size = new Size(1681, 913);
            tableLayoutPanel1.TabIndex = 7;
            // 
            // tableLayoutPanel2
            // 
            tableLayoutPanel2.ColumnCount = 1;
            tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle());
            tableLayoutPanel2.Controls.Add(messageListView, 0, 1);
            tableLayoutPanel2.Controls.Add(tableLayoutPanel3, 0, 0);
            tableLayoutPanel2.Dock = DockStyle.Fill;
            tableLayoutPanel2.Location = new Point(302, 0);
            tableLayoutPanel2.Margin = new Padding(0);
            tableLayoutPanel2.Name = "tableLayoutPanel2";
            tableLayoutPanel2.RowCount = 2;
            tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Absolute, 726F));
            tableLayoutPanel2.RowStyles.Add(new RowStyle());
            tableLayoutPanel2.Size = new Size(1379, 913);
            tableLayoutPanel2.TabIndex = 2;
            // 
            // tableLayoutPanel3
            // 
            tableLayoutPanel3.ColumnCount = 2;
            tableLayoutPanel3.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 1086F));
            tableLayoutPanel3.ColumnStyles.Add(new ColumnStyle());
            tableLayoutPanel3.Controls.Add(renderCtrl, 0, 0);
            tableLayoutPanel3.Controls.Add(panel1, 1, 0);
            tableLayoutPanel3.Dock = DockStyle.Fill;
            tableLayoutPanel3.Location = new Point(0, 0);
            tableLayoutPanel3.Margin = new Padding(0);
            tableLayoutPanel3.Name = "tableLayoutPanel3";
            tableLayoutPanel3.RowCount = 1;
            tableLayoutPanel3.RowStyles.Add(new RowStyle());
            tableLayoutPanel3.Size = new Size(1379, 726);
            tableLayoutPanel3.TabIndex = 0;
            // 
            // panel1
            // 
            panel1.Controls.Add(groupBox1);
            panel1.Controls.Add(propertiesGroup);
            panel1.Dock = DockStyle.Fill;
            panel1.Location = new Point(1086, 0);
            panel1.Margin = new Padding(0);
            panel1.Name = "panel1";
            panel1.Size = new Size(293, 726);
            panel1.TabIndex = 7;
            // 
            // groupBox1
            // 
            groupBox1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            groupBox1.Controls.Add(AttribBox);
            groupBox1.Location = new Point(3, 229);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(287, 494);
            groupBox1.TabIndex = 6;
            groupBox1.TabStop = false;
            groupBox1.Text = "Attributes";
            // 
            // AttribBox
            // 
            AttribBox.BackColor = SystemColors.Control;
            AttribBox.BorderStyle = BorderStyle.None;
            AttribBox.Dock = DockStyle.Fill;
            AttribBox.Location = new Point(3, 19);
            AttribBox.Multiline = true;
            AttribBox.Name = "AttribBox";
            AttribBox.ReadOnly = true;
            AttribBox.Size = new Size(281, 472);
            AttribBox.TabIndex = 0;
            // 
            // SceneViewerForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1705, 966);
            Controls.Add(tableLayoutPanel1);
            Controls.Add(statusLbl);
            Controls.Add(menuStrip1);
            KeyPreview = true;
            MainMenuStrip = menuStrip1;
            Name = "SceneViewerForm";
            Text = "Trinity Scene View";
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            sceneContext.ResumeLayout(false);
            propertiesGroup.ResumeLayout(false);
            propertiesGroup.PerformLayout();
            tableLayoutPanel1.ResumeLayout(false);
            tableLayoutPanel2.ResumeLayout(false);
            tableLayoutPanel3.ResumeLayout(false);
            panel1.ResumeLayout(false);
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
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
        private Label statusLbl;
        private ToolStripMenuItem viewToolStripMenuItem;
        private ToolStripMenuItem toolStripMenuItem1;
        private ToolStripMenuItem toolstripGBuf_All;
        private ToolStripMenuItem toolstripGBuf_Albedo;
        private ToolStripMenuItem toolstripGBuf_Normal;
        private ToolStripMenuItem toolstripGBuf_Depth;
        private ToolStripMenuItem toolstripGBuf_Specular;
        private ToolStripMenuItem toolstripGBuf_AO;
        private ListView messageListView;
        private ColumnHeader columnHeader1;
        private GroupBox propertiesGroup;
        private TextBox PropertiesBox;
        private GFTool.RenderControl_WinForms.RenderControl renderCtrl;
        private TableLayoutPanel tableLayoutPanel1;
        private TableLayoutPanel tableLayoutPanel2;
        private TableLayoutPanel tableLayoutPanel3;
        private Panel panel1;
        private GroupBox groupBox1;
        private TextBox AttribBox;
    }
}