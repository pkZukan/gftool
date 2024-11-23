using GFTool.Core.Flatbuffers.TR.Scene.Components;
using GFTool.Renderer;
using System.Diagnostics;
using System.Text;
using Point = System.Drawing.Point;

namespace TrinitySceneView
{
    public partial class SceneViewerForm : Form
    {
        RenderContext renderer;
        Point prevMousePos;

        private TRSceneTree sceneTree;

        public SceneViewerForm()
        {
            InitializeComponent();
        }

        private void openTRSOT_Click(object sender, EventArgs e)
        {
            var ofd = new OpenFileDialog();
            if (ofd.ShowDialog() != DialogResult.OK) return;
            sceneView.Nodes.Clear();
            sceneTree = new TRSceneTree();
            sceneTree.DeserializeScene(ofd.FileName);
            sceneView.Nodes.Add(sceneTree.TreeNode);
        }

        private void expandToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TreeNode node = sceneView.SelectedNode;
            var pair = sceneTree.FindFirst(node);
            var meta = pair.Value;
            //Only expand nodes with external files
            if (meta.IsExternal)
                sceneTree.NodeExpand(pair.Key, meta);
        }

        private void ClearProperties()
        {
            InfoBox.Text = string.Empty;
        }

        private void sceneView_MouseUp(object sender, MouseEventArgs e)
        {
            Point ClickPoint = new Point(e.X, e.Y);
            TreeNode ClickNode = sceneView.GetNodeAt(ClickPoint);
            sceneView.SelectedNode = ClickNode;
            if (ClickNode == null) return;

            if (e.Button == MouseButtons.Right)
            {
                Point ScreenPoint = sceneView.PointToScreen(ClickPoint);
                Point FormPoint = this.PointToClient(ScreenPoint);
                sceneContext.Show(this, FormPoint);
            }

            //Check for data to display
            var meta = sceneTree.GetNodeMeta(sceneView.SelectedNode);
            if (meta == null || meta?.Data == null)
            {
                ClearProperties();
                return;
            }
            StringBuilder sb = new StringBuilder();
            switch (meta?.Type)
            {
                case "trinity_CameraEntity":
                {
                    var data = (trinity_CameraEntity)meta?.Data;
                    sb.AppendLine("Name: " + data.Name);
                    sb.AppendLine("Target: " + data.TargetName);
                    break;
                }
                case "trinity_SceneObject":
                {
                    var data = (trinity_SceneObject)meta?.Data;
                    sb.AppendLine("Name: " + data.Name);
                    if (data.AttachJointName != string.Empty)
                        sb.AppendLine("Attach joint name: " + data.AttachJointName);
                    sb.AppendLine(string.Format("Tags: ({0})", data.TagList.Length));

                    foreach (var tag in data.TagList)
                    {
                        sb.AppendLine(string.Format("  {0}" + Environment.NewLine, tag == string.Empty ? "(Blank)" : tag));
                    }

                    if (data.Layers.Length > 0)
                    {
                        sb.AppendLine(string.Format("Layers: ({0})", data.Layers.Length));
                        foreach (var layer in data.Layers)
                        {
                            sb.AppendLine(string.Format("  {0}" + Environment.NewLine, layer.Name));
                        }
                    }
                    break;
                }
                case "trinity_OverrideSensorData":
                {
                    var data = (trinity_OverrideSensorData)meta?.Data;
                    sb.AppendLine("Realizing Dist: " + data.RealizingDistance);
                    sb.AppendLine("Unrealizing Dist: " + data.UnrealizingDistance);
                    sb.AppendLine("Loading Dist: " + data.LoadingDistance);
                    sb.AppendLine("Unloading Dist: " + data.UnloadingDistance);
                    break;
                }
                case "trinity_ScriptComponent":
                {
                    var data = (trinity_ScriptComponent)meta?.Data;
                    sb.AppendLine("File: " + data.FilePath);
                    sb.AppendLine("Package: " + data.PackageName);
                    sb.AppendLine("Is static: " + (data.IsStatic ? "True" : "False"));
                    break;
                }
            }
            InfoBox.Text = sb.ToString();
        }

        private void glCtxt_Paint(object sender, PaintEventArgs e)
        {
            renderer.Update();
            statusLbl.Text = string.Format("Camera: Pos={0}, [Quat={1} Euler={2}]", renderer.camera.Position.ToString(), renderer.camera.Rotation.ToString(), renderer.camera.Rotation.ToEulerAngles().ToString());
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
                renderer.camera.ApplyRotationalDelta(deltaX, deltaY);
                glCtxt.Invalidate();
            }
        }

        private void toolstripGBuf_Clicked(object sender, EventArgs e)
        {
            var item = sender as ToolStripMenuItem;
            if (item.Checked) return;

            GBuffer.DisplayType disp = GBuffer.DisplayType.DISPLAY_ALL;
            switch (item.Name)
            {
                case "toolstripGBuf_All": disp = GBuffer.DisplayType.DISPLAY_ALL; break;
                case "toolstripGBuf_Albedo": disp = GBuffer.DisplayType.DISPLAY_ALBEDO; break;
                case "toolstripGBuf_Normal": disp = GBuffer.DisplayType.DISPLAY_NORMAL; break;
                case "toolstripGBuf_Specular": disp = GBuffer.DisplayType.DISPLAY_SPECULAR; break;
                case "toolstripGBuf_AO": disp = GBuffer.DisplayType.DISPLAY_AO; break;
                case "toolstripGBuf_Depth": disp = GBuffer.DisplayType.DISPLAY_DEPTH; break;
            }

            //Only one checked at a time
            toolstripGBuf_All.CheckState = item.Name == "toolstripGBuf_All" ? CheckState.Checked : CheckState.Unchecked;
            toolstripGBuf_Albedo.CheckState = item.Name == "toolstripGBuf_Albedo" ? CheckState.Checked : CheckState.Unchecked;
            toolstripGBuf_Normal.CheckState = item.Name == "toolstripGBuf_Normal" ? CheckState.Checked : CheckState.Unchecked;
            toolstripGBuf_Specular.CheckState = item.Name == "toolstripGBuf_Specular" ? CheckState.Checked : CheckState.Unchecked;
            toolstripGBuf_AO.CheckState = item.Name == "toolstripGBuf_AO" ? CheckState.Checked : CheckState.Unchecked;
            toolstripGBuf_Depth.CheckState = item.Name == "toolstripGBuf_Depth" ? CheckState.Checked : CheckState.Unchecked;

            renderer.SetGBufferDisplayMode(disp);
            glCtxt.Invalidate();
        }

        private void keyTimer_Tick(object sender, EventArgs e)
        {
            float x = 0;
            float y = 0;
            float z = 0;

            if (KeyboardControls.Forward)
                x = 1.0f;
            else if (KeyboardControls.Backward)
                x = -1.0f;
            if (KeyboardControls.Right)
                z = 1.0f;
            else if (KeyboardControls.Left)
                z = -1.0f;
            if (KeyboardControls.Up)
                y = 1.0f;
            else if (KeyboardControls.Down)
                y = -1.0f;

            renderer.camera.ApplyMovement(x, y, z);
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

        private void messageHandler_Callback(object? sender, GFTool.Renderer.Message e)
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
    }
}