namespace GFTool.Core.Utils;

/// <summary>Resolves extracted assets relative to their referring file or an asset root.</summary>
public sealed class AssetPathResolver
{
    public string? AssetRoot { get; }

    public AssetPathResolver(string? assetRoot = null)
    {
        AssetRoot = string.IsNullOrWhiteSpace(assetRoot) ? null : Path.GetFullPath(assetRoot);
    }

    public string Resolve(string reference, string sourceFile, bool numberedScene = false)
    {
        if (string.IsNullOrWhiteSpace(reference)) throw new ArgumentException("The asset reference is empty.");
        var path = reference.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
        if (path.StartsWith("romfs:", StringComparison.OrdinalIgnoreCase)) path = path[6..];
        if (Path.IsPathFullyQualified(path) && File.Exists(path)) return path;
        // Game paths beginning with / are relative to the extracted asset root.
        path = path.TrimStart(Path.DirectorySeparatorChar);
        var directories = new List<string>();
        if (AssetRoot != null) directories.Add(AssetRoot);
        var dir = Path.GetDirectoryName(Path.GetFullPath(sourceFile));
        while (dir != null)
        {
            directories.Add(dir);
            dir = Directory.GetParent(dir)?.FullName;
        }
        var candidates = new List<string>();
        foreach (var directory in directories.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var candidate = Path.GetFullPath(Path.Combine(directory, path));
            candidates.Add(candidate);
            if (numberedScene && !Path.GetFileNameWithoutExtension(candidate).EndsWith("_0", StringComparison.OrdinalIgnoreCase))
                candidates.Add(Path.Combine(Path.GetDirectoryName(candidate)!, Path.GetFileNameWithoutExtension(candidate) + "_0" + Path.GetExtension(candidate)));
        }
        return candidates.FirstOrDefault(File.Exists) ?? candidates[0];
    }
}
