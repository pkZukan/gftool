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
    public struct SceneMetaData
    {
        public bool IsExternal;
        public string FilePath;
        public SceneChunk? Chunk;

        public SceneMetaData(SceneChunk chunk)
        { 
            IsExternal = false;
            FilePath = string.Empty;
            Chunk = chunk;
        }

        public SceneMetaData(string extFile)
        {
            IsExternal = true;
            FilePath = extFile;
            Chunk = null;
        }
    }

    public class TRSceneTree
    {
        //Map node to metadata
        private Dictionary<TreeNode, SceneMetaData> InnerData = new Dictionary<TreeNode, SceneMetaData>();

        public TreeNode TreeNode { get; private set; } = new TreeNode();

        public TRSceneTree()
        {
            //
        }

        //When node is expanded
        public void NodeExpand(TreeNode node, SceneMetaData meta)
        {
            DeserializeScene(meta, node);
        }

        //Deserialize scene from metadata
        public void DeserializeScene(SceneMetaData meta, TreeNode node = null)
        {
            var trscn = FlatBufferConverter.DeserializeFrom<TRSCN>(meta.FilePath);
            var n = (node == null) ? TreeNode : node;
            if (node == null)
                TreeNode.Text = trscn.Name;
            else
                node.Text = string.Format("SubScene ({0})", trscn.Name);
            WalkTrScene(n, trscn, meta.FilePath);
        }

        //Deserialize scene from filepath
        public void DeserializeScene(string filePath)
        {
            DeserializeScene(new SceneMetaData(filePath));
        }

        //Deserialize scene chunk / nested flatbuffers
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

        private void WalkTrScene(TreeNode node, TRSCN scene, string sceneFile)
        {
            //Iterate over all children in scene and create tree
            foreach (var ent in scene.Chunks)
            {
                var newnode = node.Nodes.Add(ent.Type);

                //SubScenes save meta with external path
                if (ent.Type == "SubScene")
                {
                    SubScene sub = FlatBufferConverter.DeserializeFrom<SubScene>(ent.Data);

                    string absPath = Path.Combine(Path.GetDirectoryName(sceneFile), sub.Filepath).Replace(".trscn", "_0.trscn");
                    InnerData.Add(newnode, new SceneMetaData(absPath));
                }
                else
                    InnerData.Add(newnode, new SceneMetaData(ent));
            }
        }

        public KeyValuePair<TreeNode, SceneMetaData> FindFirst(TreeNode node)
        { 
            return InnerData.Where(x => x.Key == node).First();
        }
    }
}
