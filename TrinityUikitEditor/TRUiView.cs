using GFTool.Core.Flatbuffers.TR.Scene;
using GFTool.Core.Flatbuffers.TR.Scene.Components;
using GFTool.Core.Flatbuffers.TR.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Trinity.Core.Utils;

namespace TrinityUikitEditor
{
    public struct ViewMetaData
    {
        public bool IsExternal { get; private set; }
        public string FilePath { get; private set; }
        public string Type { get; private set; }
        public object? Data { get; private set; }

        //Deserialize scene component via reflection
        private object? DeserializeChunk(ViewChunk chunk)
        {
            object? obj = null;
            try
            {
                var method = typeof(FlatBufferConverter).GetMethod("DeserializeFrom",
                            BindingFlags.Static | BindingFlags.Public,
                            null,  // Don't specify binder
                            new Type[] { typeof(byte[]) },  // Parameter types
                            null); // Don't specify modifiers
                var compType = Assembly.Load("GFTool.Core").GetTypes().FirstOrDefault(t => t.Name == chunk.Type);
                var generic = method.MakeGenericMethod(compType);
                obj = generic.Invoke(null, new object[] { chunk.Data });
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error parsing chunk data for " + chunk.Type);
            }
            return obj;
        }

        public ViewMetaData(ViewChunk chunk)
        {
            IsExternal = false;
            FilePath = string.Empty;
            Type = chunk.Type;
            Data = null;

            Data = DeserializeChunk(chunk);
        }

        public ViewMetaData(string extFile)
        {
            IsExternal = true;
            FilePath = extFile;
            Type = string.Empty;
            Data = null;
        }
    }

    public class TRUiView
    {
        //Map node to metadata
        private Dictionary<TreeNode, ViewMetaData> InnerData = new Dictionary<TreeNode, ViewMetaData>();

        public TreeNode TreeNode { get; private set; } = new TreeNode();

        //Deserialize scene from metadata
        public void DeserializeView(ViewMetaData meta, TreeNode node = null)
        {
            var truiv = FlatBufferConverter.DeserializeFrom<TRUIV>(meta.FilePath);
            var n = (node == null) ? TreeNode : node;
            var name = Path.GetFileNameWithoutExtension(meta.FilePath);
            if (node == null)
                TreeNode.Text = name;
            else
                node.Text = string.Format("SubComponent ({0})", name);
            WalkTrView(n, truiv, meta.FilePath);
        }

        //Deserialize scene from filepath
        public void DeserializeView(string filePath)
        {
            DeserializeView(new ViewMetaData(filePath));
        }

        private void ProcessSceneMeta(ViewMetaData meta)
        {
            //TODO
        }

        //Get meta from node
        public ViewMetaData? GetNodeMeta(TreeNode node)
        {
            if (!InnerData.ContainsKey(node)) return null;
            return InnerData[node];
        }

        private void WalkTrViewChunks(TreeNode node, ViewChunk chunk, string sceneFile = "")
        {
            var newnode = node.Nodes.Add(chunk.Type);

            var meta = new ViewMetaData(chunk);
            ProcessSceneMeta(meta);
            InnerData.Add(newnode, meta);
            foreach (var child in chunk.Children)
                WalkTrViewChunks(newnode, child);
        }

        private void WalkTrView(TreeNode node, TRUIV view, string sceneFile)
        {
            //Iterate over all children in scene and create tree
            foreach (var ent in view.Chunks)
            {
                WalkTrViewChunks(node, ent, sceneFile);
            }
        }

        public KeyValuePair<TreeNode, ViewMetaData> FindFirst(TreeNode node)
        {
            return InnerData.Where(x => x.Key == node).First();
        }
    }
}
