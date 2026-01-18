using GFTool.Renderer.Scene.GraphicsObjects;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using Trinity.Core.Assets;
using TrinityModelViewer.Scene;
using TrinityModelViewer.UI.Materials;

namespace TrinityModelViewer
{
    public partial class ModelViewerForm
    {
        private sealed class ComboDisplayItem
        {
            public ComboDisplayItem(string text)
            {
                Text = text;
            }

            public string Text { get; set; }

            public override string ToString()
            {
                return Text;
            }
        }

        private enum NodeType
        {
            ModelRoot,
            MeshGroup,
            Mesh,
            MaterialsGroup,
            Material,
            ArmatureGroup,
            ArmatureBone
        }

        private sealed class NodeTag
        {
            public NodeType Type { get; set; }
            public Model Model { get; set; } = null!;
            public string? MeshName { get; set; }
            public string? MaterialName { get; set; }
            public int? BoneIndex { get; set; }
            public List<int>? SubmeshIndices { get; set; }
            public Dictionary<string, List<int>>? MaterialMap { get; set; }
        }

        private sealed class MeshEntry
        {
            public string Name { get; set; } = string.Empty;
            public List<int> SubmeshIndices { get; } = new List<int>();
            public Dictionary<string, List<int>> MaterialMap { get; } = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);
        }

        private Dictionary<TreeNode, Model> modelMap = new Dictionary<TreeNode, Model>();
        private readonly SceneModelManager sceneModelManager = new SceneModelManager();
        private readonly Dictionary<Model, (string ReferenceTrmdlPath, string GltfPath)> gltfImportContextByModel =
            new Dictionary<Model, (string ReferenceTrmdlPath, string GltfPath)>();
        private ViewerSettings settings = null!;
        private ToolStripMenuItem? lastModelToolStripMenuItem;
        private ToolStripMenuItem? recentModelsToolStripMenuItem;
        private Image? texturePreviewSourceImage;
        private Image? texturePreviewDisplayImage;
        private Image? uvPreviewImage;
        private Model? currentMaterialsModel;
        private Material? currentMaterial;
        private readonly List<GFTool.Renderer.Scene.GraphicsObjects.Animation> animations = new List<GFTool.Renderer.Scene.GraphicsObjects.Animation>();
        private readonly HashSet<string> loadedAnimationPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly object animationLoadGate = new object();
        private System.Threading.CancellationTokenSource? autoLoadAnimationsCts;
        private readonly string[] startupFiles = Array.Empty<string>();
        private bool startupFilesLoaded;
        private ContextMenuStrip? animationsContextMenu;
        private ToolStripMenuItem? exportAnimationMenuItem;
        private ToolStripMenuItem? exportModelWithAnimationMenuItem;
        private readonly System.Windows.Forms.Timer animationUiTimer = new System.Windows.Forms.Timer();
        private bool isScrubbingAnimation;
        private ContextMenuStrip? textureGridContextMenu;
        private Button? exportEditedMaterialsButton;
        private ContextMenuStrip? sceneTreeContextMenu;
        private ToolStripMenuItem? toggleMeshVisibilityMenuItem;
        private ToolStripMenuItem? assignMeshMaterialMenuItem;
        private ToolStripMenuItem? uvOverridesContextMenuItem;
        private bool suppressMaterialParamCellClickOnce;
        private ToolStripMenuItem? layerMaskUvContextMenuItem;
        private ToolStripMenuItem? layerMaskUvMaterialContextMenuItem;
        private ToolStripMenuItem? layerMaskUv0ContextMenuItem;
        private ToolStripMenuItem? layerMaskUv1ContextMenuItem;
        private ToolStripMenuItem? aoUvContextMenuItem;
        private ToolStripMenuItem? aoUvMaterialContextMenuItem;
        private ToolStripMenuItem? aoUv0ContextMenuItem;
        private ToolStripMenuItem? aoUv1ContextMenuItem;
        private TreeNode? contextMenuNode;
        private ToolStripMenuItem? toolsToolStripMenuItem;
        private ToolStripMenuItem? useBackupIkCharacterShaderToolStripMenuItem;
        private ToolStripMenuItem? perfHudToolStripMenuItem;
        private ToolStripMenuItem? perfSpikeLogToolStripMenuItem;
        private ToolStripMenuItem? vsyncToolStripMenuItem;
        private ToolStripMenuItem? openGfpakToolStripMenuItem;
        private ToolStripMenuItem? exportTrinityToolStripMenuItem;
        private ToolStripMenuItem? exportTrinityPatchToolStripMenuItem;
        private TabPage? jsonEditorTabPage;
        private DataGridView? jsonFilesGrid;
        private Button? refreshJsonFilesButton;
        private int modelLoadDepth;
        private LoadingForm? loadingForm;
        private CancellationTokenSource? modelLoadCts;
        private ToolStripMenuItem? skinningToolStripMenuItem;
        private ToolStripMenuItem? deterministicSkinningToolStripMenuItem;
        private bool isUpdatingMaterialGrids;
        private bool isUpdatingUvPreview;
        private readonly MaterialsEditorService materialsEditorService = new MaterialsEditorService();
        private readonly ComboDisplayItem uvWrapAutoItem = new ComboDisplayItem("Wrap: Auto (Sampler)");
        private Label? perfHudLabel;
        private System.Windows.Forms.Timer? perfHudTimer;
        private Control? perfHudHost;

        private sealed class LruCache<TKey, TValue> where TKey : notnull where TValue : class
        {
            private readonly int capacity;
            private readonly Action<TValue>? onEvict;
            private readonly Dictionary<TKey, LinkedListNode<(TKey Key, TValue Value)>> map = new();
            private readonly LinkedList<(TKey Key, TValue Value)> list = new();
            private readonly object gate = new();

            public LruCache(int capacity, Action<TValue>? onEvict)
            {
                this.capacity = Math.Max(1, capacity);
                this.onEvict = onEvict;
            }

            public bool TryGet(TKey key, out TValue? value)
            {
                lock (gate)
                {
                    if (!map.TryGetValue(key, out var node))
                    {
                        value = null;
                        return false;
                    }

                    list.Remove(node);
                    list.AddFirst(node);
                    value = node.Value.Value;
                    return true;
                }
            }

            public void Put(TKey key, TValue value)
            {
                lock (gate)
                {
                    if (map.TryGetValue(key, out var existing))
                    {
                        list.Remove(existing);
                        onEvict?.Invoke(existing.Value.Value);
                        map.Remove(key);
                    }

                    var node = new LinkedListNode<(TKey Key, TValue Value)>((key, value));
                    list.AddFirst(node);
                    map[key] = node;

                    while (map.Count > capacity && list.Last != null)
                    {
                        var last = list.Last;
                        list.RemoveLast();
                        map.Remove(last.Value.Key);
                        onEvict?.Invoke(last.Value.Value);
                    }
                }
            }

            public void RemoveWhere(Func<TKey, bool> predicate)
            {
                if (predicate == null)
                {
                    return;
                }

                lock (gate)
                {
                    var node = list.First;
                    while (node != null)
                    {
                        var next = node.Next;
                        if (predicate(node.Value.Key))
                        {
                            list.Remove(node);
                            map.Remove(node.Value.Key);
                            onEvict?.Invoke(node.Value.Value);
                        }
                        node = next;
                    }
                }
            }
        }

        private readonly LruCache<string, Bitmap> texturePreviewCache = new LruCache<string, Bitmap>(capacity: 32, onEvict: b => b.Dispose());
        private readonly LruCache<string, Bitmap> textureChannelCache = new LruCache<string, Bitmap>(capacity: 48, onEvict: b => b.Dispose());
        private readonly LruCache<string, Bitmap> uvPreviewCache = new LruCache<string, Bitmap>(capacity: 24, onEvict: b => b.Dispose());
        private CancellationTokenSource? previewUpdateCts;
        private int previewUpdateSerial;

        private bool ownsTexturePreviewSourceImage;
        private bool ownsTexturePreviewDisplayImage;
        private bool ownsUvPreviewImage;
    }
}
