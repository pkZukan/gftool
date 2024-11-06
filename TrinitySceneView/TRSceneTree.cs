using GFTool.Core.Flatbuffers.TR.Scene;
using GFTool.Core.Flatbuffers.TR.Scene.Components;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Trinity.Core.Utils;

namespace TrinitySceneView
{
    public class TRSceneTree
    {
        private Dictionary<TreeNode, SceneChunk> InnerData = new Dictionary<TreeNode, SceneChunk>();

        public TreeNode TreeNode { get; private set; }

        public TRSceneTree(string filename)
        {
            var trscn = FlatBufferConverter.DeserializeFrom<TRSCN>(filename);
            TreeNode = new TreeNode(trscn.Name);
            WalkTrScene(TreeNode, trscn, filename);
        }

        private object DeserializeChunk(SceneChunk chunk)
        {
            var method = typeof(FlatBufferConverter).GetMethod("DeserializeFrom",
                        BindingFlags.Static | BindingFlags.Public,
                        null,  // Don't specify binder
                        new Type[] { typeof(byte[]) },  // Parameter types
                        null); // Don't specify modifiers
            var compType = Assembly.Load("GFTool.Core").GetTypes().FirstOrDefault(t => t.Name == chunk.Type);
            var generic = method.MakeGenericMethod(compType);
            return generic.Invoke(null, new object[] { chunk.Data });
        }

        private void WalkTrScene(TreeNode node, TRSCN scene, string filepath)
        {
            foreach (var ent in scene.Chunks)
            {
                var newnode = node.Nodes.Add(ent.Type);
                if (ent.Children.Length > 0)
                    InnerData.Add(newnode, ent);

                var data = DeserializeChunk(ent);

                if (ent.Type == "SubScene")
                {
                    //TODO: Link node with trscn
                    /*string path = new Uri(Path.Combine(Path.GetDirectoryName(filepath), scene.SubScenes[]).Replace(".trscn", "_0.trscn")).AbsolutePath;
                    if (File.Exists(path))
                    {
                        //var trsot = FlatBufferConverter.DeserializeFrom<TRSCN>(path);
                        //newnode.Text += "_" + trsot.Name;
                        WalkTrScene(newnode, trsot.SceneObjectList, path);
                    }*/
                }
            }
        }

        public KeyValuePair<TreeNode, SceneChunk> FindFirst(TreeNode node)
        { 
            return InnerData.Where(x => x.Key == node).First();
        }
    }
}
