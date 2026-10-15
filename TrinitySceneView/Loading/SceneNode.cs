using OpenTK.Mathematics;

namespace TrinitySceneView.Loading;

public sealed class SceneNode
{
    public string Name { get; internal set; } = string.Empty;
    public string Type { get; internal set; } = string.Empty;
    public string SourceFile { get; internal set; } = string.Empty;
    public ulong Id { get; internal set; }
    public int PayloadSize { get; internal set; }
    public object? Data { get; internal set; }
    public SceneNode? Parent { get; internal set; }
    public List<SceneNode> Children { get; } = new();
    public Matrix4 LocalTransform { get; internal set; } = Matrix4.Identity;
    public Matrix4 WorldTransform { get; internal set; } = Matrix4.Identity;
    public string? Reference { get; internal set; }
    public string? ResolvedPath { get; internal set; }
    public bool IsExternal { get; internal set; }
    public bool ExternalLoaded { get; internal set; }
    internal SceneNode? ExternalTarget { get; set; }
    public bool IsModel { get; internal set; }
    public IReadOnlyList<Matrix4>? InstanceTransforms { get; internal set; }
    public string Status { get; set; } = "Parsed";
    public string? Error { get; set; }
    public List<string> Notes { get; } = new();

    public IEnumerable<SceneNode> DescendantsAndSelf()
    {
        yield return this;
        foreach (var child in Children)
            foreach (var node in child.DescendantsAndSelf()) yield return node;
    }
}

public sealed class SceneDocument
{
    public required SceneNode Root { get; init; }
    public string? AssetRoot { get; init; }
    public IEnumerable<SceneNode> Models => Root.DescendantsAndSelf().Where(n => n.IsModel);
}
