using GFTool.Core.Flatbuffers.TR.Scene.Components;
using GFTool.Renderer;
using GFTool.Renderer.Core;
using GFTool.Renderer.Scene.GraphicsObjects;
using Point = System.Drawing.Point;

namespace TrinityModelViewer
{
    public partial class ModelViewerForm : Form
    {
        RenderContext renderer;
        Point prevMousePos;

        private Dictionary<TreeNode, Model> modelMap = new Dictionary<TreeNode, Model>();

        public ModelViewerForm()
        {
            InitializeComponent();
        }

        private void messageHandler_Callback(object? sender, GFTool.Renderer.Core.Message e)
        {
            var item = new ListViewItem();
            item.Name = e.GetHashCode().ToString();
            item.Text = e.Content;
            item.ImageKey = e.Type switch
            {
                MessageType.LOG => "Log",
                MessageType.WARNING => "Warning",
                MessageType.ERROR => "Error"
            };

            //Only unique errors
            if (!messageListView.Items.ContainsKey(e.GetHashCode().ToString()))
            {
                messageListView.Items.Add(item);
                messageListView.EnsureVisible(messageListView.Items.Count - 1);
            }
        }

        #region GL_CONTEXT
        private void glCtxt_Paint(object sender, PaintEventArgs e)
        {
            renderer.Update();
            var cam = renderer.GetCameraTransform();
            statusLbl.Text = string.Format("Camera: Pos={0}, [Quat={1} Euler={2}]", cam.Position.ToString(), cam.Rotation.ToString(), cam.Rotation.ToEulerAngles().ToString());
        }

        private void glCtxt_Load(object sender, EventArgs e)
        {
            //Create rendering context
            renderer = new RenderContext(glCtxt.Context, glCtxt.Width, glCtxt.Height);

            //Connect to message handler 
            MessageHandler.Instance.MessageCallback += messageHandler_Callback;
            var messageIcons = new ImageList();
            messageIcons.Images.Add("Log", SystemIcons.Information.ToBitmap());
            messageIcons.Images.Add("Warning", SystemIcons.Warning.ToBitmap());
            messageIcons.Images.Add("Error", SystemIcons.Error.ToBitmap());
            messageListView.SmallImageList = messageIcons;
            messageListView.FullRowSelect = true;
            messageListView.AutoResizeColumn(0, ColumnHeaderAutoResizeStyle.HeaderSize);

            //Initialize renderer
            renderer.Setup();
        }

        private void glCtxt_MouseMove(object sender, MouseEventArgs e)
        {
            Point mousePos = e.Location;

            if (mousePos == prevMousePos) return;

            int deltaX = (mousePos.X - prevMousePos.X);
            int deltaY = (mousePos.Y - prevMousePos.Y);

            prevMousePos = mousePos;
            if ((e.Button & MouseButtons.Left) != 0)
            {
                renderer.RotateCamera(deltaX, deltaY);
                glCtxt.Invalidate();
            }
        }

        private void glCtxt_Resize(object sender, EventArgs e)
        {
            renderer?.Resize(glCtxt.Width, glCtxt.Height);
            glCtxt.Invalidate();
        }

        private void keyTimer_Tick(object sender, EventArgs e)
        {
            renderer.UpdateMovementControls();
            glCtxt.Invalidate();
        }

        private void glCtxt_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.W: KeyboardControls.Forward = true; break;
                case Keys.A: KeyboardControls.Left = true; break;
                case Keys.S: KeyboardControls.Backward = true; break;
                case Keys.D: KeyboardControls.Right = true; break;
                case Keys.Q: KeyboardControls.Up = true; break;
                case Keys.E: KeyboardControls.Down = true; break;
            }
        }

        private void glCtxt_KeyUp(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.W: KeyboardControls.Forward = false; break;
                case Keys.A: KeyboardControls.Left = false; break;
                case Keys.S: KeyboardControls.Backward = false; break;
                case Keys.D: KeyboardControls.Right = false; break;
                case Keys.Q: KeyboardControls.Up = false; break;
                case Keys.E: KeyboardControls.Down = false; break;
            }
        }
        #endregion

        private void ClearAll()
        {
            renderer.ClearScene();
            messageListView.Items.Clear();
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Trinity Model files (*.trmdl)|*.trmdl|All files (*.*)|*.*";
            if (ofd.ShowDialog() != DialogResult.OK) return;

            ClearAll();
            var mdl = renderer.AddSceneModel(ofd.FileName);
            var node = new TreeNode(mdl.Name);
            modelMap.Add(node, mdl);
            sceneTree.Nodes.Add(node);
        }

        private void importToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Trinity Model files (*.trmdl)|*.trmdl|All files (*.*)|*.*";
            if (ofd.ShowDialog() != DialogResult.OK) return;

            var mdl = renderer.AddSceneModel(ofd.FileName);
            var node = new TreeNode(mdl.Name);
            modelMap.Add(node, mdl);
            sceneTree.Nodes.Add(node);
        }

        private void wireframeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            renderer.SetWireframe(wireframeToolStripMenuItem.CheckState == CheckState.Checked);
            glCtxt.Invalidate();
        }

        //Treeview handler
        private void sceneTree_MouseUp(object sender, MouseEventArgs e)
        {
            Point ClickPoint = new Point(e.X, e.Y);
            TreeNode ClickNode = sceneTree.GetNodeAt(ClickPoint);
            sceneTree.SelectedNode = ClickNode;
            if (ClickNode == null) return;

            if (e.Button == MouseButtons.Right)
            {
                Point ScreenPoint = sceneTree.PointToScreen(ClickPoint);
                Point FormPoint = this.PointToClient(ScreenPoint);
                sceneTreeCtxtMenu.Show(this, FormPoint);
            }
        }

        //Context menu delete
        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var selected = sceneTree.SelectedNode;
            if (selected != null)
            {
                modelMap.TryGetValue(selected, out var mdl);
                if (mdl == null) return;

                renderer.RemoveSceneModel(mdl);
                sceneTree.Nodes.Remove(selected);
                modelMap.Remove(selected);
            }
        }
    }
}
