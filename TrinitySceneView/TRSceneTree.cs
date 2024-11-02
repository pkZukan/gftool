using GFTool.Core.Flatbuffers.TR.Scene;
using GFTool.Core.Flatbuffers.TR.Scene.Components;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Trinity.Core.Utils;

namespace TrinitySceneView
{
    public class TRSceneTree
    {
        private Dictionary<TreeNode, SceneEntry> InnerData = new Dictionary<TreeNode, SceneEntry>();

        public TreeNode TreeNode { get; private set; }

        public TRSceneTree(string filename)
        {
            var trsot = FlatBufferConverter.DeserializeFrom<TrinitySceneObjTemplate>(filename);
            TreeNode = new TreeNode(trsot.SceneName);
            WalkTrsot(TreeNode, trsot.SceneObjectList, filename);
        }

        private void WalkTrsot(TreeNode node, SceneEntry[] ents, string filepath)
        {
            foreach (var ent in ents)
            {
                var newnode = node.Nodes.Add(ent.TypeName);
                if (ent.NestedType.Length > 0)
                    InnerData.Add(newnode, ent);
                if (ent.TypeName == "SubScene")
                {
                    SubScene s = FlatBufferConverter.DeserializeFrom<SubScene>(ent.NestedType);
                    string path = new Uri(Path.Combine(Path.GetDirectoryName(filepath), s.Filepath).Replace(".trscn", "_1.trscn")).AbsolutePath;
                    if (File.Exists(path))
                    {
                        var trsot = FlatBufferConverter.DeserializeFrom<TrinitySceneObjTemplate>(path);
                        newnode.Text += "_" + trsot.SceneName;
                        WalkTrsot(newnode, trsot.SceneObjectList, path);
                    }
                }
                if (ent.SubObjects.Length > 0)
                    WalkTrsot(newnode, ent.SubObjects, filepath);
            }
        }

        public KeyValuePair<TreeNode, SceneEntry> FindFirst(TreeNode node)
        { 
            return InnerData.Where(x => x.Key == node).First();
        }
    }
}
