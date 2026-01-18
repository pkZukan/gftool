using GFTool.Core.Flatbuffers.TR.Scene;
using GFTool.Core.Flatbuffers.TR.Scene.Components;
using GFTool.Renderer.Core;
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
using static System.Net.Mime.MediaTypeNames;

namespace TrinitySceneView
{
    public struct SceneMetaData
    {
        private static readonly Lazy<Dictionary<string, Type>> TypeByName =
            new Lazy<Dictionary<string, Type>>(() =>
                typeof(TRSCN).Assembly
                    .GetTypes()
                    .GroupBy(t => t.Name, StringComparer.Ordinal)
                    .ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal));

        private static readonly Lazy<MethodInfo?> FlatBufferDeserializeMethod =
            new Lazy<MethodInfo?>(() =>
                typeof(FlatBufferConverter).GetMethod(
                    "DeserializeFrom",
                    BindingFlags.Static | BindingFlags.Public,
                    null,
                    new[] { typeof(byte[]) },
                    null));

        private static readonly HashSet<string> MissingTypeWarnings = new HashSet<string>(StringComparer.Ordinal);
        private static readonly HashSet<string> FallbackDecodeWarnings = new HashSet<string>(StringComparer.Ordinal);

        public bool IsExternal { get; private set; }
        public string FilePath { get; private set; }
        public string Type { get; private set; }
        public object? Data { get; private set; }
        public byte[]? RawData { get; private set; }

        //Deserialize scene component via reflection
        private object? DeserializeChunk(SceneChunk chunk)
        {
            // PropertySheet varies between scene versions and can throw during eager decode. Decode lazily
            // when the user selects it in the UI.
            if (chunk.Type == nameof(trinity_PropertySheet))
            {
                return null;
            }

            var method = FlatBufferDeserializeMethod.Value;
            if (method == null)
            {
                MessageHandler.Instance.AddMessage(MessageType.ERROR, "[Scene] FlatBufferConverter.DeserializeFrom(byte[]) not found.");
                return null;
            }

            if (!TypeByName.Value.TryGetValue(chunk.Type, out var compType))
            {
                if (MissingTypeWarnings.Add(chunk.Type))
                {
                    MessageHandler.Instance.AddMessage(
                        MessageType.WARNING,
                        $"[Scene] No schema/type found for chunk '{chunk.Type}'. Skipping chunk decode.");
                }

                return null;
            }

            try
            {
                var generic = method.MakeGenericMethod(compType);
                return InvokeWithFallback(chunk, generic);
            }
            catch (Exception ex)
            {
                MessageHandler.Instance.AddMessage(
                    MessageType.WARNING,
                    $"[Scene] Failed to decode chunk '{chunk.Type}': {ex.GetType().Name}: {ex.Message}");
                return null;
            }
        }

        private static object? InvokeWithFallback(SceneChunk chunk, MethodInfo genericDeserialize)
        {
            try
            {
                return genericDeserialize.Invoke(null, new object[] { chunk.Data });
            }
            catch (TargetInvocationException tie) when (tie.InnerException is InvalidDataException)
            {
                // Some scene chunks appear to store the raw table bytes without the file-style root uoffset.
                // As a fallback, wrap the bytes with a 4-byte uoffset pointing to the table at +4.
                if (chunk.Data == null || chunk.Data.Length < 8)
                {
                    throw;
                }

                if (FallbackDecodeWarnings.Add(chunk.Type))
                {
                    MessageHandler.Instance.AddMessage(
                        MessageType.WARNING,
                        $"[Scene] Chunk '{chunk.Type}' failed FlatBuffer parse; retrying with root-offset wrapper.");
                }

                var wrapped = new byte[chunk.Data.Length + 4];
                BitConverter.GetBytes(4).CopyTo(wrapped, 0);
                Buffer.BlockCopy(chunk.Data, 0, wrapped, 4, chunk.Data.Length);
                return genericDeserialize.Invoke(null, new object[] { wrapped });
            }
        }

        public SceneMetaData(SceneChunk chunk)
        {
            IsExternal = false;
            FilePath = string.Empty;
            Type = chunk.Type;
            Data = null;
            RawData = chunk.Data;

            Data = DeserializeChunk(chunk);
        }

        public SceneMetaData(string extFile)
        {
            IsExternal = true;
            FilePath = extFile;
            Type = string.Empty;
            Data = null;
            RawData = null;
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

        //Get meta from node
        public SceneMetaData? GetNodeMeta(TreeNode node)
        {
            if (!InnerData.ContainsKey(node)) return null;
            return InnerData[node];
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

        private void ProcessSceneMeta(SceneMetaData meta)
        {
            //TODO
        }

        private void WalkTrSceneChunks(TreeNode node, SceneChunk chunk, string sceneFile = "")
        {
            var newnode = node.Nodes.Add(chunk.Type);

            //SubScenes save meta with external path
            if (chunk.Type == "SubScene")
            {
                SubScene sub = FlatBufferConverter.DeserializeFrom<SubScene>(chunk.Data);

                string absPath = Path.Combine(Path.GetDirectoryName(sceneFile), sub.Filepath).Replace(".trs", "_0.trs"); //trscn, trsot, trsog
                InnerData.Add(newnode, new SceneMetaData(absPath));
            }
            else
            {
                var meta = new SceneMetaData(chunk);
                ProcessSceneMeta(meta);
                InnerData.Add(newnode, meta);
                foreach (var child in chunk.Children)
                    WalkTrSceneChunks(newnode, child);
            }
        }

        private void WalkTrScene(TreeNode node, TRSCN scene, string sceneFile)
        {
            //Iterate over all children in scene and create tree
            foreach (var ent in scene.Chunks)
            {
                WalkTrSceneChunks(node, ent, sceneFile);
            }
        }

        public KeyValuePair<TreeNode, SceneMetaData> FindFirst(TreeNode node)
        {
            return InnerData.Where(x => x.Key == node).First();
        }
    }
}
