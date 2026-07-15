using GFTool.Renderer.Scene.GraphicsObjects;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Trinity.Core.Flatbuffers.TR.Model;
using Trinity.Core.Utils;

namespace TrinityModelViewer.Export
{
    internal static class NpcPackageExporter
    {
        private static readonly Regex ResourcePathRegex = new Regex(
            @"(?:[A-Za-z0-9_.\-]+[/\\])*[A-Za-z0-9_.\-]+\.(?:trmdl|tracn|tracs|tracl|tracr|tracp|tracm|tranm|traef|trmtr|trmsh|trmbf|trskl|tralk|trbik|trslp|trmmt|trmdd|trmdt|trspn|trslt|trssp|trmae|trpokecfg|bntx|ptcl|hkx|bin)",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

        internal sealed class Result
        {
            public required string PackagePath { get; init; }
            public required string EntryPoint { get; init; }
            public required string ManifestPath { get; init; }
            public required int FileCount { get; init; }
            public required int MissingCount { get; init; }
            public required long TotalBytes { get; init; }
        }

        private sealed class Dependency
        {
            public required string SourcePath { get; init; }
            public HashSet<string> Roles { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        private sealed class MissingDependency
        {
            public required string Reference { get; init; }
            public required string ReferencedBy { get; init; }
            public required string Role { get; init; }
        }

        private sealed class Manifest
        {
            [JsonPropertyName("formatVersion")] public int FormatVersion { get; set; } = 1;
            [JsonPropertyName("modelName")] public string ModelName { get; set; } = string.Empty;
            [JsonPropertyName("createdUtc")] public DateTime CreatedUtc { get; set; }
            [JsonPropertyName("sourceRoot")] public string SourceRoot { get; set; } = string.Empty;
            [JsonPropertyName("sourceModel")] public string SourceModel { get; set; } = string.Empty;
            [JsonPropertyName("originalInput")] public string? OriginalInput { get; set; }
            [JsonPropertyName("entryPoint")] public string EntryPoint { get; set; } = string.Empty;
            [JsonPropertyName("resourceDirectory")] public string ResourceDirectory { get; set; } = "files";
            [JsonPropertyName("ccdataPathsRewritten")] public bool CcDataPathsRewritten { get; set; }
            [JsonPropertyName("rewrittenPathCount")] public int RewrittenPathCount { get; set; }
            [JsonPropertyName("fileCount")] public int FileCount { get; set; }
            [JsonPropertyName("totalBytes")] public long TotalBytes { get; set; }
            [JsonPropertyName("files")] public List<ManifestFile> Files { get; set; } = new List<ManifestFile>();
            [JsonPropertyName("missing")] public List<MissingDependency> Missing { get; set; } = new List<MissingDependency>();
        }

        private sealed class ManifestFile
        {
            [JsonPropertyName("roles")] public List<string> Roles { get; set; } = new List<string>();
            [JsonPropertyName("sourcePath")] public string SourcePath { get; set; } = string.Empty;
            [JsonPropertyName("packagePath")] public string PackagePath { get; set; } = string.Empty;
            [JsonPropertyName("bytes")] public long Bytes { get; set; }
            [JsonPropertyName("sha256")] public string Sha256 { get; set; } = string.Empty;
        }

        public static Result Export(
            Model model,
            string packagePath,
            string? originalInputPath,
            IReadOnlyCollection<string> indexedResources,
            IReadOnlyCollection<string> animationPaths)
        {
            ArgumentNullException.ThrowIfNull(model);
            if (string.IsNullOrWhiteSpace(packagePath))
            {
                throw new ArgumentException("A package output path is required.", nameof(packagePath));
            }

            var files = new Dictionary<string, Dependency>(StringComparer.OrdinalIgnoreCase);
            var missing = new List<MissingDependency>();
            var sourceModel = Path.GetFullPath(model.SourcePath);
            var modelDirectory = Path.GetDirectoryName(sourceModel)
                ?? throw new InvalidOperationException("The model source has no parent directory.");

            AddExisting(files, missing, sourceModel, "Model", sourceModel);
            if (!string.IsNullOrWhiteSpace(originalInputPath))
            {
                AddExisting(files, missing, originalInputPath, "OriginalInput", originalInputPath);
            }

            foreach (var resource in indexedResources)
            {
                AddExisting(files, missing, resource, "IndexedResource", originalInputPath ?? sourceModel);
                CollectIndexedResourceSiblings(resource, files, missing);
            }

            CollectModelGraph(sourceModel, modelDirectory, files, missing);
            CollectLoadedTextures(model, files, missing);
            CollectAnimationFiles(animationPaths, files, missing);
            CollectModelSidecars(sourceModel, files);

            var existingPaths = files.Values.Select(value => value.SourcePath).ToList();
            if (existingPaths.Count == 0)
            {
                throw new InvalidOperationException("No existing model dependencies were found.");
            }

            var sourceRoot = FindCommonRoot(existingPaths);
            packagePath = Path.GetFullPath(packagePath);
            Directory.CreateDirectory(packagePath);
            var contentRoot = Path.Combine(packagePath, "files");
            Directory.CreateDirectory(contentRoot);

            var manifest = new Manifest
            {
                ModelName = model.Name,
                CreatedUtc = DateTime.UtcNow,
                SourceRoot = sourceRoot,
                SourceModel = sourceModel,
                OriginalInput = string.IsNullOrWhiteSpace(originalInputPath) ? null : Path.GetFullPath(originalInputPath),
                Missing = missing
                    .OrderBy(item => item.Reference, StringComparer.OrdinalIgnoreCase)
                    .ToList()
            };

            long totalBytes = 0;
            string? packagedCcDataPath = null;
            ManifestFile? packagedCcDataManifest = null;
            var packagePathsBySource = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var dependency in files.Values.OrderBy(value => value.SourcePath, StringComparer.OrdinalIgnoreCase))
            {
                string destination;
                if (!string.IsNullOrWhiteSpace(originalInputPath) &&
                    string.Equals(Path.GetExtension(originalInputPath), ".ccdata", StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(dependency.SourcePath, Path.GetFullPath(originalInputPath), StringComparison.OrdinalIgnoreCase))
                {
                    destination = Path.Combine(packagePath, Path.GetFileName(dependency.SourcePath));
                    packagedCcDataPath = destination;
                }
                else
                {
                    var relative = Path.GetRelativePath(sourceRoot, dependency.SourcePath);
                    if (relative.StartsWith("..", StringComparison.Ordinal) || Path.IsPathRooted(relative))
                    {
                        var pathRoot = Path.GetPathRoot(dependency.SourcePath) ?? string.Empty;
                        var pathWithoutRoot = dependency.SourcePath[pathRoot.Length..]
                            .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                        relative = Path.Combine("external", SanitizePathSegment(pathRoot), pathWithoutRoot);
                    }

                    destination = Path.GetFullPath(Path.Combine(contentRoot, relative));
                }

                if (!IsWithinDirectory(packagePath, destination))
                {
                    throw new InvalidOperationException($"Package path escaped the package root: {destination}");
                }

                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                File.Copy(dependency.SourcePath, destination, overwrite: false);
                var info = new FileInfo(destination);
                totalBytes += info.Length;
                var manifestFile = new ManifestFile
                {
                    Roles = dependency.Roles.OrderBy(role => role, StringComparer.OrdinalIgnoreCase).ToList(),
                    SourcePath = dependency.SourcePath,
                    PackagePath = NormalizeManifestPath(Path.GetRelativePath(packagePath, destination)),
                    Bytes = info.Length,
                    Sha256 = ComputeSha256(destination)
                };
                manifest.Files.Add(manifestFile);
                packagePathsBySource[dependency.SourcePath] = manifestFile.PackagePath;
                if (string.Equals(destination, packagedCcDataPath, StringComparison.OrdinalIgnoreCase))
                {
                    packagedCcDataManifest = manifestFile;
                }
            }

            if (packagedCcDataPath != null && packagedCcDataManifest != null && originalInputPath != null)
            {
                manifest.RewrittenPathCount = RewriteCcDataPaths(
                    packagedCcDataPath,
                    Path.GetFullPath(originalInputPath),
                    packagePathsBySource);
                manifest.CcDataPathsRewritten = manifest.RewrittenPathCount > 0;
                packagedCcDataManifest.Roles.Add("RewrittenEntryPoint");
                var rewrittenInfo = new FileInfo(packagedCcDataPath);
                packagedCcDataManifest.Bytes = rewrittenInfo.Length;
                packagedCcDataManifest.Sha256 = ComputeSha256(packagedCcDataPath);
            }

            manifest.FileCount = manifest.Files.Count;
            manifest.TotalBytes = totalBytes;
            var preferredEntrySource = !string.IsNullOrWhiteSpace(originalInputPath) &&
                                       string.Equals(Path.GetExtension(originalInputPath), ".ccdata", StringComparison.OrdinalIgnoreCase) &&
                                       File.Exists(originalInputPath)
                ? Path.GetFullPath(originalInputPath)
                : sourceModel;
            var copiedEntry = manifest.Files.First(file => string.Equals(file.SourcePath, preferredEntrySource, StringComparison.OrdinalIgnoreCase));
            manifest.EntryPoint = copiedEntry.PackagePath;

            var manifestPath = Path.Combine(packagePath, "manifest.json");
            File.WriteAllText(
                manifestPath,
                JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }),
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

            var readmePath = Path.Combine(packagePath, "README.txt");
            File.WriteAllText(
                readmePath,
                BuildReadme(manifest),
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

            return new Result
            {
                PackagePath = packagePath,
                EntryPoint = Path.Combine(packagePath, manifest.EntryPoint.Replace('/', Path.DirectorySeparatorChar)),
                ManifestPath = manifestPath,
                FileCount = manifest.FileCount,
                MissingCount = manifest.Missing.Count,
                TotalBytes = totalBytes
            };
        }

        private static void CollectModelGraph(
            string sourceModel,
            string modelDirectory,
            Dictionary<string, Dependency> files,
            List<MissingDependency> missing)
        {
            var trmdl = FlatBufferConverter.DeserializeFrom<TRMDL>(sourceModel);
            foreach (var mesh in trmdl.Meshes ?? Array.Empty<ModelMesh>())
            {
                var meshPath = ResolvePath(modelDirectory, mesh.PathName);
                AddExisting(files, missing, meshPath, "Mesh", sourceModel);
                if (!File.Exists(meshPath))
                {
                    continue;
                }

                try
                {
                    var trmsh = FlatBufferConverter.DeserializeFrom<TRMSH>(meshPath);
                    var bufferPath = ResolvePath(modelDirectory, trmsh.bufferFilePath);
                    AddExisting(files, missing, bufferPath, "MeshBuffer", meshPath);
                }
                catch (Exception ex)
                {
                    missing.Add(new MissingDependency
                    {
                        Reference = meshPath,
                        ReferencedBy = sourceModel,
                        Role = $"MeshParseError: {ex.Message}"
                    });
                }
            }

            foreach (var materialReference in trmdl.Materials ?? Array.Empty<string>())
            {
                var materialPath = ResolvePath(modelDirectory, materialReference);
                AddExisting(files, missing, materialPath, "Material", sourceModel);
                if (File.Exists(materialPath))
                {
                    CollectDeclaredTextures(materialPath, modelDirectory, files, missing);
                }
            }

            if (trmdl.Skeleton != null && !string.IsNullOrWhiteSpace(trmdl.Skeleton.PathName))
            {
                AddExisting(
                    files,
                    missing,
                    ResolvePath(modelDirectory, trmdl.Skeleton.PathName),
                    "Skeleton",
                    sourceModel);
            }
        }

        private static void CollectDeclaredTextures(
            string materialPath,
            string modelDirectory,
            Dictionary<string, Dependency> files,
            List<MissingDependency> missing)
        {
            var references = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                var gfx = FlatBufferConverter.DeserializeFrom<Trinity.Core.Flatbuffers.Gfx2.Material>(materialPath);
                foreach (var item in gfx.ItemList ?? Array.Empty<Trinity.Core.Flatbuffers.Gfx2.MaterialItem>())
                {
                    foreach (var texture in item.TextureParamList ?? Array.Empty<Trinity.Core.Flatbuffers.Gfx2.TextureParam>())
                    {
                        if (!string.IsNullOrWhiteSpace(texture.FilePath)) references.Add(texture.FilePath);
                    }
                }
            }
            catch
            {
            }

            try
            {
                var trmtr = FlatBufferConverter.DeserializeFrom<TRMTR>(materialPath);
                foreach (var material in trmtr.Materials ?? Array.Empty<TRMaterial>())
                {
                    foreach (var texture in material.Textures ?? Array.Empty<TRTexture>())
                    {
                        if (!string.IsNullOrWhiteSpace(texture.File)) references.Add(texture.File);
                    }
                }
            }
            catch
            {
            }

            var materialDirectory = Path.GetDirectoryName(materialPath) ?? modelDirectory;
            foreach (var reference in references)
            {
                var resolved = ResolveTextureReference(reference, materialDirectory, modelDirectory);
                AddExisting(files, missing, resolved, "DeclaredTexture", materialPath, reference);
            }
        }

        private static void CollectLoadedTextures(
            Model model,
            Dictionary<string, Dependency> files,
            List<MissingDependency> missing)
        {
            foreach (var material in model.GetMaterials())
            {
                foreach (var texture in material.Textures)
                {
                    if (texture.TryGetResolvedSourcePath(out var resolved) && File.Exists(resolved))
                    {
                        AddExisting(files, missing, resolved, $"Texture:{material.Name}:{texture.Name}", model.SourcePath);
                    }
                    else
                    {
                        missing.Add(new MissingDependency
                        {
                            Reference = texture.SourceFile,
                            ReferencedBy = model.SourcePath,
                            Role = $"Texture:{material.Name}:{texture.Name}"
                        });
                    }
                }
            }
        }

        private static void CollectAnimationFiles(
            IReadOnlyCollection<string> animationPaths,
            Dictionary<string, Dependency> files,
            List<MissingDependency> missing)
        {
            foreach (var animationPath in animationPaths)
            {
                var full = Path.GetFullPath(animationPath);
                AddExisting(files, missing, full, "Animation", full);
                var directory = Path.GetDirectoryName(full);
                var stem = Path.GetFileNameWithoutExtension(full);
                if (string.IsNullOrWhiteSpace(directory) || string.IsNullOrWhiteSpace(stem) || !Directory.Exists(directory))
                {
                    continue;
                }

                foreach (var sidecar in Directory.EnumerateFiles(directory, stem + ".*", SearchOption.TopDirectoryOnly))
                {
                    AddExisting(files, missing, sidecar, "AnimationSidecar", full);
                }
            }
        }

        private static void CollectIndexedResourceSiblings(
            string indexedResource,
            Dictionary<string, Dependency> files,
            List<MissingDependency> missing)
        {
            var extension = Path.GetExtension(indexedResource);
            if (!IsAnimationIndexExtension(extension))
            {
                return;
            }

            var directory = Path.GetDirectoryName(Path.GetFullPath(indexedResource));
            if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
            {
                return;
            }

            foreach (var path in Directory.EnumerateFiles(directory, "*", SearchOption.TopDirectoryOnly))
            {
                AddExisting(files, missing, path, "IndexedAnimationResource", indexedResource);
            }
        }

        private static bool IsAnimationIndexExtension(string extension)
        {
            return extension.Equals(".tracn", StringComparison.OrdinalIgnoreCase) ||
                   extension.Equals(".tracs", StringComparison.OrdinalIgnoreCase) ||
                   extension.Equals(".tracl", StringComparison.OrdinalIgnoreCase) ||
                   extension.Equals(".tracr", StringComparison.OrdinalIgnoreCase) ||
                   extension.Equals(".tracp", StringComparison.OrdinalIgnoreCase) ||
                   extension.Equals(".tralk", StringComparison.OrdinalIgnoreCase) ||
                   extension.Equals(".trbik", StringComparison.OrdinalIgnoreCase) ||
                   extension.Equals(".trmdd", StringComparison.OrdinalIgnoreCase) ||
                   extension.Equals(".trmdt", StringComparison.OrdinalIgnoreCase) ||
                   extension.Equals(".trssp", StringComparison.OrdinalIgnoreCase);
        }

        private static void CollectModelSidecars(string sourceModel, Dictionary<string, Dependency> files)
        {
            var directory = Path.GetDirectoryName(sourceModel);
            var modelName = Path.GetFileNameWithoutExtension(sourceModel);
            if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
            {
                return;
            }

            foreach (var path in Directory.EnumerateFiles(directory, modelName + ".*", SearchOption.TopDirectoryOnly))
            {
                AddExisting(files, new List<MissingDependency>(), path, "ModelSidecar", sourceModel);
            }
        }

        private static string ResolveTextureReference(string reference, string materialDirectory, string modelDirectory)
        {
            var normalized = reference.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
            var candidates = new List<string>
            {
                ResolvePath(materialDirectory, normalized),
                Path.Combine(materialDirectory, Path.GetFileName(normalized)),
                ResolvePath(modelDirectory, normalized),
                Path.Combine(modelDirectory, Path.GetFileName(normalized))
            };

            var shareRelative = GetShareRelativePath(normalized);
            if (shareRelative != null)
            {
                foreach (var root in EnumerateAncestors(materialDirectory).Concat(EnumerateAncestors(modelDirectory)))
                {
                    candidates.Add(Path.Combine(root, shareRelative));
                }
            }

            return candidates
                .Select(path => Path.GetFullPath(path))
                .FirstOrDefault(File.Exists)
                ?? Path.GetFullPath(candidates[0]);
        }

        private static string? GetShareRelativePath(string reference)
        {
            var normalized = reference.Replace('\\', '/');
            var index = normalized.IndexOf("/share/", StringComparison.OrdinalIgnoreCase);
            if (index >= 0)
            {
                return normalized[(index + 1)..].Replace('/', Path.DirectorySeparatorChar);
            }

            return normalized.StartsWith("share/", StringComparison.OrdinalIgnoreCase)
                ? normalized.Replace('/', Path.DirectorySeparatorChar)
                : null;
        }

        private static IEnumerable<string> EnumerateAncestors(string path)
        {
            DirectoryInfo? current = new DirectoryInfo(Path.GetFullPath(path));
            while (current != null)
            {
                yield return current.FullName;
                current = current.Parent;
            }
        }

        private static void AddExisting(
            Dictionary<string, Dependency> files,
            List<MissingDependency> missing,
            string path,
            string role,
            string referencedBy,
            string? originalReference = null)
        {
            string full;
            try
            {
                full = Path.GetFullPath(path);
            }
            catch
            {
                missing.Add(new MissingDependency
                {
                    Reference = originalReference ?? path,
                    ReferencedBy = referencedBy,
                    Role = role
                });
                return;
            }

            if (!File.Exists(full))
            {
                missing.Add(new MissingDependency
                {
                    Reference = originalReference ?? full,
                    ReferencedBy = referencedBy,
                    Role = role
                });
                return;
            }

            if (!files.TryGetValue(full, out var dependency))
            {
                dependency = new Dependency { SourcePath = full };
                files.Add(full, dependency);
            }
            dependency.Roles.Add(role);
        }

        private static string ResolvePath(string baseDirectory, string reference)
        {
            var normalized = reference.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
            return Path.GetFullPath(Path.IsPathRooted(normalized) ? normalized : Path.Combine(baseDirectory, normalized));
        }

        private static string FindCommonRoot(IReadOnlyList<string> paths)
        {
            var roots = paths
                .Select(path => Path.GetPathRoot(path) ?? string.Empty)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (roots.Count != 1)
            {
                return Path.GetPathRoot(paths[0]) ?? Directory.GetCurrentDirectory();
            }

            var candidate = Path.GetDirectoryName(paths[0]) ?? roots[0];
            while (!paths.All(path => IsWithinDirectory(candidate, path)))
            {
                var parent = Directory.GetParent(candidate);
                if (parent == null)
                {
                    return roots[0];
                }
                candidate = parent.FullName;
            }
            return candidate;
        }

        private static bool IsWithinDirectory(string directory, string path)
        {
            var root = Path.GetFullPath(directory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            var full = Path.GetFullPath(path);
            return full.StartsWith(root, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(full.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), root.TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase);
        }

        private static string ComputeSha256(string path)
        {
            using var stream = File.OpenRead(path);
            return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
        }

        private static string NormalizeManifestPath(string path) => path.Replace('\\', '/');

        private static int RewriteCcDataPaths(
            string packagedCcDataPath,
            string originalCcDataPath,
            IReadOnlyDictionary<string, string> packagePathsBySource)
        {
            var bytes = File.ReadAllBytes(packagedCcDataPath);
            var text = Encoding.UTF8.GetString(bytes);
            var originalDirectory = Path.GetDirectoryName(originalCcDataPath)
                ?? throw new InvalidOperationException("The source CCData file has no parent directory.");
            var rewritten = 0;

            foreach (var rawReference in ResourcePathRegex.Matches(text)
                         .Select(match => match.Value)
                         .Distinct(StringComparer.Ordinal))
            {
                var sourcePath = ResolvePath(originalDirectory, rawReference);
                if (!packagePathsBySource.TryGetValue(sourcePath, out var packagePath))
                {
                    throw new InvalidOperationException($"CCData resource was not included in the package: {rawReference}");
                }

                var replacement = rawReference.Contains('\\')
                    ? packagePath.Replace('/', '\\')
                    : packagePath.Replace('\\', '/');
                var sourceBytes = Encoding.UTF8.GetBytes(rawReference);
                var replacementBytes = Encoding.UTF8.GetBytes(replacement);
                if (sourceBytes.Length != replacementBytes.Length)
                {
                    throw new InvalidOperationException(
                        $"CCData path cannot be rewritten safely because its FlatBuffer string length would change: " +
                        $"'{rawReference}' ({sourceBytes.Length}) -> '{replacement}' ({replacementBytes.Length}).");
                }

                var occurrence = IndexOf(bytes, sourceBytes, 0);
                if (occurrence < 0)
                {
                    throw new InvalidDataException($"CCData path bytes were not found: {rawReference}");
                }

                while (occurrence >= 0)
                {
                    Buffer.BlockCopy(replacementBytes, 0, bytes, occurrence, replacementBytes.Length);
                    rewritten++;
                    occurrence = IndexOf(bytes, sourceBytes, occurrence + sourceBytes.Length);
                }
            }

            File.WriteAllBytes(packagedCcDataPath, bytes);
            return rewritten;
        }

        private static int IndexOf(byte[] haystack, byte[] needle, int startIndex)
        {
            if (needle.Length == 0)
            {
                return startIndex;
            }

            for (var index = Math.Max(0, startIndex); index <= haystack.Length - needle.Length; index++)
            {
                var matches = true;
                for (var offset = 0; offset < needle.Length; offset++)
                {
                    if (haystack[index + offset] == needle[offset])
                    {
                        continue;
                    }

                    matches = false;
                    break;
                }

                if (matches)
                {
                    return index;
                }
            }

            return -1;
        }

        private static string SanitizePathSegment(string value)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var builder = new StringBuilder(value.Length);
            foreach (var character in value)
            {
                builder.Append(invalid.Contains(character) ? '_' : character);
            }
            return builder.ToString().Trim('_');
        }

        private static string BuildReadme(Manifest manifest)
        {
            var builder = new StringBuilder();
            builder.AppendLine("Trinity NPC resource package");
            builder.AppendLine();
            builder.AppendLine($"Model: {manifest.ModelName}");
            builder.AppendLine($"Open this file: {manifest.EntryPoint}");
            builder.AppendLine($"Files: {manifest.FileCount}");
            builder.AppendLine($"Missing references: {manifest.Missing.Count}");
            builder.AppendLine();
            builder.AppendLine("The files directory preserves the source relative-path layout.");
            builder.AppendLine("For CCData exports, indexed paths are rewritten so the CCData entry point can stay in this package root.");
            builder.AppendLine("Do not flatten or move individual files inside files; Trinity resources use relative indexes.");
            builder.AppendLine("See manifest.json for original paths, package paths, roles, sizes, SHA-256 hashes, and missing references.");
            return builder.ToString();
        }
    }
}
