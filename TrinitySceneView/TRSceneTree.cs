using TrinitySceneView.Loading;

namespace TrinitySceneView;

public sealed class TRSceneTree
{
    private readonly Dictionary<SceneNode, TreeNode> nodes = new();
    public SceneDocument Document { get; }
    public TreeNode TreeNode { get; private set; } = new();

    public TRSceneTree(SceneDocument document)
    {
        Document = document;
        Refresh();
    }

    public void Refresh()
    {
        nodes.Clear();
        TreeNode = Build(Document.Root);
    }

    private TreeNode Build(SceneNode data)
    {
        var node = new TreeNode { Tag = data };
        nodes.Add(data, node);
        Update(data);
        foreach (var child in data.Children) node.Nodes.Add(Build(child));
        return node;
    }

    public void Update(SceneNode data)
    {
        if (!nodes.TryGetValue(data, out var node)) return;
        node.Text = data.Name == data.Type ? data.Type : $"{data.Name} [{data.Type}]";
        if (data.Error != null) node.Text += " (error)";
        node.ForeColor = data.Error != null ? Color.Firebrick : SystemColors.WindowText;
        node.ToolTipText = $"{data.Status}\n{data.ResolvedPath ?? data.SourceFile}" + (data.Error == null ? "" : $"\n{data.Error}");
    }

    public SceneNode? GetNodeMeta(TreeNode? node) => node?.Tag as SceneNode;
    public TreeNode? GetTreeNode(SceneNode node) => nodes.GetValueOrDefault(node);
}
