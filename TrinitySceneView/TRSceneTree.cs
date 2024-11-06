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
            WalkTrsot(TreeNode, trscn.Chunk, filename);
        }

        private void WalkTrsot(TreeNode node, SceneChunk[] ents, string filepath)
        {
            foreach (var ent in ents)
            {
                var newnode = node.Nodes.Add(ent.Type);
                if (ent.Children.Length > 0)
                    InnerData.Add(newnode, ent);
                var test = typeof(FlatBufferConverter).GetMethods();

                var method = typeof(FlatBufferConverter).GetMethod("DeserializeFrom",
                    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public,
                    null,  // Don't specify binder
                    new Type[] { typeof(byte[]) },  // Parameter types
                    null); // Don't specify modifiers
                var compType = Assembly.Load("GFTool.Core").GetTypes().FirstOrDefault(t => t.Name == ent.Type);
                var generic = method.MakeGenericMethod(compType);
                var result = generic.Invoke(null, new object[] { ent.Data });

                /*if (ent.Type == "SubScene")
                {
                    SubScene s = FlatBufferConverter.DeserializeFrom<SubScene>(ent.Children);
                    string path = new Uri(Path.Combine(Path.GetDirectoryName(filepath), s.Filepath).Replace(".trscn", "_0.trscn")).AbsolutePath;
                    if (File.Exists(path))
                    {
                        var trsot = FlatBufferConverter.DeserializeFrom<TRSCN>(path);
                        newnode.Text += "_" + trsot.Name;
                        WalkTrsot(newnode, trsot.SceneObjectList, path);
                    }
                }
                if (ent.SubObjects.Length > 0)
                    WalkTrsot(newnode, ent.SubObjects, filepath);*/
            }
        }

        public KeyValuePair<TreeNode, SceneChunk> FindFirst(TreeNode node)
        { 
            return InnerData.Where(x => x.Key == node).First();
        }
    }
}
