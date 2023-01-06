using GFTool.Core.Flatbuffers.TR.Scene;
using SubScene = GFTool.Core.Flatbuffers.TR.Scene.Components.SubScene;
using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using Trinity.Core.Utils;

namespace TrinitySceneView
{
    public partial class Form1 : Form
    {
        Dictionary<TreeNode, SceneEntry> InnerData= new Dictionary<TreeNode, SceneEntry>();

        public Form1()
        {
            InitializeComponent();
        }

        void WalkTrsot(TreeNode node, SceneEntry[] ents, string filepath) 
        {
            foreach (var ent in ents) 
            {
                var newnode = node.Nodes.Add(ent.TypeName);
                if (ent.NestedType.Length > 0)
                    InnerData.Add(newnode, ent);
                if(ent.TypeName == "SubScene")
                {
                    SubScene s = FlatBufferConverter.DeserializeFrom<SubScene>(ent.NestedType);
                    string path = new Uri(Path.Combine(Path.GetDirectoryName(filepath), s.Filepath).Replace(".trscn", "_1.trscn")).AbsolutePath;
                    if (File.Exists(path))
                    {
                        var trsot = FlatBufferConverter.DeserializeFrom<TrinitySceneObjTemplate>(path);
                        newnode.Text += "_" + trsot.SceneName;
                        WalkTrsot(newnode, trsot.SceneObjectList,path);
                    }
                }
                if (ent.SubObjects.Length > 0) 
                    WalkTrsot(newnode, ent.SubObjects, filepath);
            }
        }

        private void openTRSOT_Click(object sender, EventArgs e)
        {
            var ofd = new OpenFileDialog();
            if (ofd.ShowDialog() != DialogResult.OK) return;
            sceneView.Nodes.Clear();
            var trsot = FlatBufferConverter.DeserializeFrom<TrinitySceneObjTemplate>(ofd.FileName);
            var tree = new TreeNode(trsot.SceneName);
            WalkTrsot(tree, trsot.SceneObjectList,ofd.FileName);
            sceneView.Nodes.Add(tree);
        }

        private void expandToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TreeNode node = sceneView.SelectedNode;
            var pair = InnerData.Where(x => x.Key == node).First();
            if (pair.Value != null)
            {
                //MethodInfo method = typeof(FlatBufferConverter).GetMethod("DeserializeFrom", BindingFlags.Public | BindingFlags.Static);
                //MethodInfo generic = method.MakeGenericMethod(Type.GetType(pair.Value.TypeName));
                //var trsot = generic.Invoke(null, new object[] { pair.Value.NestedType });
                //WalkTrsot(pair.Key, trsot.);
            }
        }

        private void sceneView_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                Point ClickPoint = new Point(e.X, e.Y);
                TreeNode ClickNode = sceneView.GetNodeAt(ClickPoint);
                sceneView.SelectedNode = ClickNode;
                if (ClickNode == null) return;

                Point ScreenPoint = sceneView.PointToScreen(ClickPoint);
                Point FormPoint = this.PointToClient(ScreenPoint);
                sceneContext.Show(this, FormPoint);
            }
        }
    }
}