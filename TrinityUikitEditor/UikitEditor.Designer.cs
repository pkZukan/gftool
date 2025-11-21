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
            messageListView = new ListView();
            columnHeader1 = new ColumnHeader();
            sceneView = new TreeView();
            renderControl1 = new GFTool.RenderControl_WinForms.RenderControl();
            SuspendLayout();
            // 
            // messageListView
            // 
            messageListView.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
            messageListView.Columns.AddRange(new ColumnHeader[] { columnHeader1 });
            messageListView.FullRowSelect = true;
            messageListView.GridLines = true;
            messageListView.LabelWrap = false;
            messageListView.Location = new Point(254, 738);
            messageListView.Name = "messageListView";
            messageListView.Size = new Size(1080, 139);
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
            sceneView.Location = new Point(12, 12);
            sceneView.Name = "sceneView";
            sceneView.Size = new Size(236, 865);
            sceneView.TabIndex = 5;
            // 
            // renderControl1
            // 
            renderControl1.API = OpenTK.Windowing.Common.ContextAPI.OpenGL;
            renderControl1.APIVersion = new Version(3, 3, 0, 0);
            renderControl1.Flags = OpenTK.Windowing.Common.ContextFlags.Default;
            renderControl1.IsEventDriven = true;
            renderControl1.Location = new Point(254, 12);
            renderControl1.Name = "renderControl1";
            renderControl1.Profile = OpenTK.Windowing.Common.ContextProfile.Core;
            renderControl1.SharedContext = null;
            renderControl1.Size = new Size(1080, 720);
            renderControl1.TabIndex = 7;
            // 
            // UikitEditor
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1346, 889);
            Controls.Add(renderControl1);
            Controls.Add(messageListView);
            Controls.Add(sceneView);
            Name = "UikitEditor";
            Text = "Trinity Uikit Editor";
            ResumeLayout(false);
        }

        #endregion

        private ListView messageListView;
        private ColumnHeader columnHeader1;
        private TreeView sceneView;
        private GFTool.RenderControl_WinForms.RenderControl renderControl1;
    }
}
