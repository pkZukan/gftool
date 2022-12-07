using GFTool.Core.Flatbuffers.TR.Scene;
using Trinity.Core.Utils;

namespace TrinitySceneView
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        void WalkTrsot(TreeNode node, SceneEntry[] ents) 
        {
            foreach (var ent in ents) 
            {
                var newnode = node.Nodes.Add(ent.TypeName);
                if (ent.SubObjects.Length > 0) WalkTrsot(newnode, ent.SubObjects);
            }
        }

        private void openTRSOT_Click(object sender, EventArgs e)
        {
            var ofd = new OpenFileDialog();
            if (ofd.ShowDialog() != DialogResult.OK) return;
            sceneView.Nodes.Clear();
            var trsot = FlatBufferConverter.DeserializeFrom<TrinitySceneObjTemplate>(ofd.FileName);
            var tree = new TreeNode(trsot.SceneName);
            WalkTrsot(tree, trsot.SceneObjectList);
            sceneView.Nodes.Add(tree);
        }
    }
}