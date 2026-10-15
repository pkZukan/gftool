using System.Reflection;
using FlatSharp.Attributes;
using GFTool.Core.Flatbuffers.TR.Scene;
using GFTool.Core.Flatbuffers.TR.Scene.Components;
using GFTool.Core.Utils;
using OpenTK.Mathematics;
using Trinity.Core.Utils;

namespace TrinitySceneView.Loading;

/// <summary>Loads a scene and resolves its resources without requiring an OpenGL context.</summary>
public sealed class SceneLoader
{
    private readonly AssetPathResolver resolver;
    private readonly CancellationToken cancellation;
    private readonly Dictionary<string, TRSCN> scenes = new(StringComparer.OrdinalIgnoreCase);
    private int nodeCount;
    private static readonly Dictionary<string, Type> components = typeof(TRSCN).Assembly.GetTypes()
        .Where(t => t.Namespace == typeof(SubScene).Namespace && t.IsDefined(typeof(FlatBufferTableAttribute)))
        .ToDictionary(t => Normalize(t.Name), StringComparer.OrdinalIgnoreCase);
    private static readonly MethodInfo deserialize = typeof(FlatBufferConverter).GetMethods()
        .Single(m => m.Name == nameof(FlatBufferConverter.DeserializeFrom) && m.GetParameters()[0].ParameterType == typeof(byte[]));

    public SceneLoader(string? assetRoot = null, CancellationToken cancellation = default)
    {
        resolver = new AssetPathResolver(assetRoot);
        this.cancellation = cancellation;
    }

    public SceneDocument Load(string file)
    {
        nodeCount = 0;
        scenes.Clear();
        var root = new SceneNode { Name = Path.GetFileName(file), Type = "TRSCN", SourceFile = Path.GetFullPath(file) };
        root.Notes.Add(resolver.AssetRoot == null ? "Asset root: automatic (scene directory and its ancestors)." : $"Asset root: {resolver.AssetRoot}");
        ReadScene(root, root.SourceFile, new HashSet<string>(StringComparer.OrdinalIgnoreCase), 0);
        return new SceneDocument { Root = root, AssetRoot = resolver.AssetRoot };
    }

    public bool ExpandExternal(SceneNode node)
    {
        if (!node.IsExternal || node.ExternalLoaded || string.IsNullOrEmpty(node.ResolvedPath)) return false;
        var target = node.ExternalTarget ?? node;
        if (!string.IsNullOrWhiteSpace(node.Reference))
        {
            node.ResolvedPath = resolver.Resolve(node.Reference, node.SourceFile, node.Data is SubScene || node.Type == "SubScene");
            target.ResolvedPath = node.ResolvedPath;
        }
        var active = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var parent = target.Parent; parent != null; parent = parent.Parent)
            if (!string.IsNullOrEmpty(parent.SourceFile)) active.Add(parent.SourceFile);
        ReadScene(target, node.ResolvedPath, active, 0);
        node.ExternalLoaded = target.ExternalLoaded;
        node.Status = target.Status;
        node.Error = target.Error;
        return node.ExternalLoaded;
    }

    private void ReadScene(SceneNode node, string file, HashSet<string> active, int depth)
    {
        cancellation.ThrowIfCancellationRequested();
        if (depth > 128) { Fail(node, "Scene nesting limit exceeded."); return; }
        if (!File.Exists(file)) { Fail(node, $"Scene file not found: {file}"); return; }
        if (!active.Add(file)) { Fail(node, $"Circular scene reference: {file}"); return; }
        try
        {
            if (!scenes.TryGetValue(file, out var scene))
            {
                scene = FlatBufferConverter.DeserializeFrom<TRSCN>(file);
                scenes.Add(file, scene);
            }
            // Failed retries must not duplicate the partially loaded children.
            node.Children.Clear();
            node.ExternalLoaded = true;
            node.Error = null;
            node.Status = "Scene loaded";
            if (node.Type == "TRSCN")
            {
                node.SourceFile = file;
                node.Data = scene;
                node.Name = string.IsNullOrWhiteSpace(scene.Name) ? Path.GetFileName(file) : scene.Name;
            }
            else
            {
                node.Notes.Add($"Referenced scene: {scene.Name}");
            }
            foreach (var chunk in scene.Chunks ?? Array.Empty<SceneChunk>())
                ReadChunk(node, chunk, file, active, depth + 1);

            // Some scenes list references only on the root table. References already
            // represented by a SubScene chunk must not be instantiated a second time.
            var represented = node.DescendantsAndSelf().Where(n => n.IsExternal)
                .Select(n => n.ResolvedPath).ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var reference in scene.SubScenes ?? Array.Empty<string>())
            {
                if (string.IsNullOrWhiteSpace(reference)) continue;
                var resolved = resolver.Resolve(reference, file, numberedScene: true);
                if (!represented.Add(resolved)) continue;
                var child = NewNode(node, "SubScene", file);
                child.Name = Path.GetFileName(reference);
                SetReference(child, reference, numberedScene: true);
                child.IsExternal = true;
                ReadScene(child, resolved, active, depth + 1);
            }
            AssociateInstances(node);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) { Fail(node, ex.GetBaseException().Message); node.ExternalLoaded = false; }
        finally { active.Remove(file); }
    }

    private SceneNode ReadChunk(SceneNode parent, SceneChunk chunk, string source, HashSet<string> active, int depth)
    {
        cancellation.ThrowIfCancellationRequested();
        if (depth > 256) throw new InvalidDataException("Scene component nesting limit exceeded.");
        var node = NewNode(parent, chunk.Type ?? "Unknown", source);
        node.Id = chunk.Id;
        node.PayloadSize = chunk.Data?.Length ?? 0;
        Parse(node, chunk.Data);
        var childParent = node;
        if (node.Data is trinity_SceneObject entity) ApplyEntity(node, entity);
        else if (node.Data is trinity_ObjectTemplate template)
        {
            node.Name = string.IsNullOrWhiteSpace(template.Name) ? node.Name : template.Name;
            if (template.EntityData?.Length > 0 && !string.IsNullOrWhiteSpace(template.EntityType))
            {
                childParent = ReadChunk(node, new SceneChunk { Type = template.EntityType, Data = template.EntityData }, source, active, depth + 1);
            }
            if (!string.IsNullOrWhiteSpace(template.FilePath))
            {
                SetReference(node, template.FilePath);
                // Inline/expanded template contents take precedence over the source file.
                node.IsExternal = !template.IsExpanded && (chunk.Children?.Length ?? 0) == 0;
            }
        }
        else if (node.Data is SubScene sub)
        {
            SetReference(node, sub.Filepath, numberedScene: true);
            node.IsExternal = true;
        }
        else if (node.Data is trinity_ModelComponent model)
        {
            node.IsModel = true;
            if (!string.IsNullOrWhiteSpace(model.ResourceName)) node.Name = model.ResourceName;
            SetReference(node, model.FilePath);
            node.Status = File.Exists(node.ResolvedPath) ? "Ready to render" : "Missing model";
            if (!File.Exists(node.ResolvedPath)) node.Error = $"Model file not found: {node.ResolvedPath ?? model.FilePath}";
        }
        else if (node.Data is trinity_ModelInstancerComponent instancer) SetReference(node, instancer.FilePath);
        else if (node.Data is trinity_AnimationComponent animation) SetReference(node, animation.FilePath);

        foreach (var child in chunk.Children ?? Array.Empty<SceneChunk>())
            ReadChunk(childParent, child, source, active, depth + 1);
        AssociateInstances(childParent);
        if (node.IsExternal && node.ResolvedPath != null)
        {
            // Keep the template's embedded entity transform as the parent of its file contents.
            if (childParent != node)
            {
                var external = NewNode(childParent, "TRSCN", source);
                node.ExternalTarget = external;
                external.Reference = node.Reference;
                external.ResolvedPath = node.ResolvedPath;
                external.IsExternal = true;
                ReadScene(external, node.ResolvedPath, active, depth + 1);
                node.ExternalLoaded = external.ExternalLoaded;
                node.Status = external.Status;
                node.Error = external.Error;
            }
            else ReadScene(node, node.ResolvedPath, active, depth + 1);
        }
        return node;
    }

    private SceneNode NewNode(SceneNode parent, string type, string source)
    {
        if (++nodeCount > 100000) throw new InvalidDataException("Scene node limit exceeded.");
        var node = new SceneNode { Type = type, Name = type, SourceFile = source, Parent = parent, WorldTransform = parent.WorldTransform };
        parent.Children.Add(node);
        return node;
    }

    private static string Normalize(string type) => new(type.Where(char.IsLetterOrDigit).ToArray());

    private static void Parse(SceneNode node, byte[]? bytes)
    {
        if (bytes == null || bytes.Length == 0) { node.Status = "Container (no payload)"; return; }
        if (!components.TryGetValue(Normalize(node.Type), out var component))
        {
            node.Status = "Unsupported component";
            node.Notes.Add("Payload retained in the scene file; this component has no schema yet.");
            return;
        }
        try { node.Data = deserialize.MakeGenericMethod(component).Invoke(null, new object[] { bytes }); }
        catch (Exception ex) { Fail(node, $"Cannot parse {node.Type}: {ex.GetBaseException().Message}"); }
    }

    private static void ApplyEntity(SceneNode node, trinity_SceneObject entity)
    {
        if (!string.IsNullOrWhiteSpace(entity.Name)) node.Name = entity.Name;
        node.LocalTransform = SceneTransforms.FromSrt(entity.Srt);
        node.WorldTransform = entity.KeepWorldSrt || !entity.AttachTransform
            ? node.LocalTransform : node.LocalTransform * (node.Parent?.WorldTransform ?? Matrix4.Identity);
        if (!string.IsNullOrWhiteSpace(entity.AttachJointName))
            node.Notes.Add($"Joint attachment '{entity.AttachJointName}' is shown in its static scene position; joint-following is not implemented.");
    }

    private void SetReference(SceneNode node, string? reference, bool numberedScene = false)
    {
        if (string.IsNullOrWhiteSpace(reference)) return;
        node.Reference = reference;
        try { node.ResolvedPath = resolver.Resolve(reference, node.SourceFile, numberedScene); }
        catch (Exception ex) { Fail(node, $"Invalid asset path '{reference}': {ex.Message}"); }
    }

    private static void Fail(SceneNode node, string message) { node.Status = "Load error"; node.Error = message; }

    private static void AssociateInstances(SceneNode parent)
    {
        var instancer = parent.Children.FirstOrDefault(n => n.Data is trinity_ModelInstancerComponent);
        if (instancer == null) return;
        try
        {
            if (instancer.InstanceTransforms == null)
            {
                if (!File.Exists(instancer.ResolvedPath)) throw new FileNotFoundException("Instance buffer not found.", instancer.ResolvedPath);
                instancer.InstanceTransforms = SceneTransforms.FromInstances(FlatBufferConverter.DeserializeFrom<TRINS>(instancer.ResolvedPath!));
                instancer.Status = $"Loaded {instancer.InstanceTransforms.Count} instances";
            }
            foreach (var model in parent.Children.Where(n => n.IsModel))
            {
                model.InstanceTransforms = instancer.InstanceTransforms;
                model.Notes.Add($"Instance buffer: {instancer.ResolvedPath}");
            }
        }
        catch (Exception ex)
        {
            Fail(instancer, ex.GetBaseException().Message);
            // Never display a made-up single model when its placement buffer failed.
            foreach (var model in parent.Children.Where(n => n.IsModel)) Fail(model, "Cannot place model: " + instancer.Error);
        }
    }
}
