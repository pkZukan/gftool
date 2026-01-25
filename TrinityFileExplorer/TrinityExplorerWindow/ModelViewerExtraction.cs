using Trinity.Core.Math.Hash;
using Trinity.Core.Cache;
using Trinity.Core.Compression;
using Trinity.Core.Flatbuffers.TR.ResourceDictionary;
using Trinity.Core.Serializers.TR;
using Trinity.Core.Utils;
using System.Linq;
using System.Diagnostics;
using Trinity.Core.Flatbuffers.TR.Model;
using System.Drawing;


namespace TrinityFileExplorer
{
    public partial class TrinityExplorerWindow : Form
    {
        private void LaunchModelViewer(string trmdlPath)
        {
            string exePath = ExplorerSettings.GetModelViewerExePath();
            if (string.IsNullOrWhiteSpace(exePath) || !File.Exists(exePath))
            {
                throw new FileNotFoundException("Model viewer executable not found.", exePath);
            }

            string? exeDir = Path.GetDirectoryName(exePath);
            if (string.IsNullOrWhiteSpace(exeDir) || !Directory.Exists(exeDir))
            {
                exeDir = Environment.CurrentDirectory;
            }

            var psi = new ProcessStartInfo
            {
                FileName = exePath,
                Arguments = $"\"{trmdlPath}\"",
                UseShellExecute = false,
                WorkingDirectory = exeDir
            };

            Process.Start(psi);
        }

        private sealed class TrpfsExtractor
        {
            private readonly CustomFileDescriptor fileDescriptor;
            private readonly FileSystem fileSystem;
            private readonly string trpfsPath;

            private readonly Dictionary<ulong, PackedArchive> packCache = new Dictionary<ulong, PackedArchive>();
            private readonly Dictionary<ulong, Dictionary<ulong, int>> packIndexCache = new Dictionary<ulong, Dictionary<ulong, int>>();

            public TrpfsExtractor(CustomFileDescriptor fileDescriptor, FileSystem fileSystem, string trpfsPath)
            {
                this.fileDescriptor = fileDescriptor;
                this.fileSystem = fileSystem;
                this.trpfsPath = trpfsPath;
            }

            public bool TryGetFileBytes(ulong fileHash, out byte[] bytes)
            {
                bytes = Array.Empty<byte>();

                if (!TryResolvePackInfo(fileHash, out string packName, out long packSize))
                {
                    return false;
                }

                ulong packHash = GFFNV.Hash(packName);
                if (!TryGetPack(packHash, packSize, out var pack))
                {
                    return false;
                }

                if (!TryGetPackEntryIndex(packHash, pack, fileHash, out int entryIndex))
                {
                    return false;
                }

                var entry = pack.FileEntry[entryIndex];
                bytes = entry.FileBuffer;
                if (entry.EncryptionType != -1)
                {
                    bytes = Oodle.Decompress(bytes, (long)entry.FileSize);
                }

                return true;
            }

            private bool TryResolvePackInfo(ulong fileHash, out string packName, out long packSize)
            {
                packName = string.Empty;
                packSize = 0;

                int idx = Array.IndexOf(fileDescriptor.FileHashes, fileHash);
                if (idx >= 0)
                {
                    ulong packIndex = fileDescriptor.FileInfo[idx].PackIndex;
                    if (packIndex < (ulong)fileDescriptor.PackNames.Length && packIndex < (ulong)fileDescriptor.PackInfo.Length)
                    {
                        packName = fileDescriptor.PackNames[packIndex];
                        packSize = checked((long)fileDescriptor.PackInfo[packIndex].FileSize);
                        return !string.IsNullOrWhiteSpace(packName);
                    }
                    return false;
                }

                if (fileDescriptor.UnusedHashes != null && fileDescriptor.UnusedFileInfo != null)
                {
                    int unusedIdx = Array.IndexOf(fileDescriptor.UnusedHashes, fileHash);
                    if (unusedIdx >= 0 && unusedIdx < fileDescriptor.UnusedFileInfo.Length)
                    {
                        ulong packIndex = fileDescriptor.UnusedFileInfo[unusedIdx].PackIndex;
                        if (packIndex < (ulong)fileDescriptor.PackNames.Length && packIndex < (ulong)fileDescriptor.PackInfo.Length)
                        {
                            packName = fileDescriptor.PackNames[packIndex];
                            packSize = checked((long)fileDescriptor.PackInfo[packIndex].FileSize);
                            return !string.IsNullOrWhiteSpace(packName);
                        }
                    }
                }

                return false;
            }

            private bool TryGetPack(ulong packHash, long packSize, out PackedArchive pack)
            {
                if (packCache.TryGetValue(packHash, out pack))
                {
                    return true;
                }

                int fileIndex = Array.IndexOf(fileSystem.FileHashes, packHash);
                if (fileIndex < 0)
                {
                    pack = null!;
                    return false;
                }

                byte[] fileBytes = ONEFILESerializer.SplitTRPAK(trpfsPath, (long)fileSystem.FileOffsets[fileIndex], packSize);
                pack = FlatBufferConverter.DeserializeFrom<PackedArchive>(fileBytes);
                packCache[packHash] = pack;
                return true;
            }

            private bool TryGetPackEntryIndex(ulong packHash, PackedArchive pack, ulong fileHash, out int index)
            {
                if (!packIndexCache.TryGetValue(packHash, out var map))
                {
                    map = new Dictionary<ulong, int>();
                    for (int i = 0; i < pack.FileHashes.Length; i++)
                    {
                        map[pack.FileHashes[i]] = i;
                    }
                    packIndexCache[packHash] = map;
                }

                return map.TryGetValue(fileHash, out index);
            }
        }

        private string ExtractTrmdlWithDependenciesToTemp(ulong trmdlFileHash, string trmdlRomfsRelativePath)
        {
            if (fileDescriptor == null || fileSystem == null)
            {
                throw new InvalidOperationException("TRPFD/TRPFS not loaded.");
            }

            string trpfsPath = Path.Join(ExplorerSettings.GetRomFSPath(), FilepathSettings.trpfsRel);
            var extractor = new TrpfsExtractor(fileDescriptor, fileSystem, trpfsPath);

            string importRoot = Path.Combine(Path.GetTempPath(), "GFTool", "TrinityModelViewerImport", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(importRoot);
            string normalizedTrmdl = NormalizeRomfsRelativePath(trmdlRomfsRelativePath);

            var pending = new Queue<string>();
            var extracted = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (!extractor.TryGetFileBytes(trmdlFileHash, out var trmdlBytes))
            {
                throw new FileNotFoundException("Failed to extract TRMDL from TRPFS.", normalizedTrmdl);
            }

            extracted.Add(normalizedTrmdl);
            WriteExtractedFile(importRoot, normalizedTrmdl, trmdlBytes);
            EnqueueTrmdlDependencies(normalizedTrmdl, trmdlBytes, pending);
            ExtractAnimationsForTrmdlIfPresent(importRoot, normalizedTrmdl, extractor);

            while (pending.Count > 0)
            {
                string relPath = NormalizeRomfsRelativePath(pending.Dequeue());
                if (!extracted.Add(relPath))
                {
                    continue;
                }

                ulong hash = GFFNV.Hash(relPath);
                if (!extractor.TryGetFileBytes(hash, out var bytes))
                {
                    continue;
                }

                WriteExtractedFile(importRoot, relPath, bytes);

                string ext = Path.GetExtension(relPath).ToLowerInvariant();
                if (ext == ".trmdl")
                {
                    EnqueueTrmdlDependencies(relPath, bytes, pending);
                }
                else if (ext == ".trmsh")
                {
                    EnqueueTrmshDependencies(relPath, bytes, pending);
                }
                else if (ext == ".trmtr")
                {
                    EnqueueTrmtrDependencies(relPath, bytes, pending);
                }
            }

            string extractedTrmdlAbs = Path.Combine(importRoot, normalizedTrmdl.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(extractedTrmdlAbs))
            {
                throw new FileNotFoundException("Failed to extract TRMDL.", extractedTrmdlAbs);
            }

            PruneModelViewerTempImports(currentImportRoot: importRoot, keepLatest: 5);
            return extractedTrmdlAbs;
        }

        private static void PruneModelViewerTempImports(string currentImportRoot, int keepLatest)
        {
            if (keepLatest < 1)
            {
                return;
            }

            try
            {
                var baseDir = Path.Combine(Path.GetTempPath(), "GFTool", "TrinityModelViewerImport");
                if (!Directory.Exists(baseDir))
                {
                    return;
                }

                var dirs = new DirectoryInfo(baseDir)
                    .EnumerateDirectories()
                    .Select(d =>
                    {
                        try
                        {
                            return d;
                        }
                        catch
                        {
                            return null;
                        }
                    })
                    .Where(d => d != null)
                    .Cast<DirectoryInfo>()
                    .OrderByDescending(d => d.CreationTimeUtc)
                    .ThenByDescending(d => d.Name, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (dirs.Count <= keepLatest)
                {
                    return;
                }

                var currentFull = Path.GetFullPath(currentImportRoot);
                int kept = 0;
                foreach (var dir in dirs)
                {
                    if (kept < keepLatest)
                    {
                        kept++;
                        continue;
                    }

                    string full;
                    try
                    {
                        full = Path.GetFullPath(dir.FullName);
                    }
                    catch
                    {
                        continue;
                    }

                    if (string.Equals(full, currentFull, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    try
                    {
                        dir.Delete(recursive: true);
                    }
                    catch
                    {
                        // Ignore delete failures (directory might be in use).
                    }
                }
            }
            catch
            {
                // Ignore pruning failures.
            }
        }

        private void ExtractAnimationsForTrmdlIfPresent(string importRoot, string trmdlRomfsRelativePath, TrpfsExtractor extractor)
        {
            if (fileDescriptor == null)
            {
                return;
            }

            string? motionDir = GuessMotionDirectoryFromTrmdl(trmdlRomfsRelativePath);
            if (string.IsNullOrWhiteSpace(motionDir))
            {
                return;
            }

            string prefix = NormalizeRomfsRelativePath(motionDir);
            if (!prefix.EndsWith("/", StringComparison.Ordinal))
            {
                prefix += "/";
            }

            const int maxAnims = 500;
            int extractedCount = 0;

            IEnumerable<ulong> allHashes = fileDescriptor.FileHashes ?? Array.Empty<ulong>();
            if (fileDescriptor.UnusedHashes != null)
            {
                allHashes = allHashes.Concat(fileDescriptor.UnusedHashes);
            }

            foreach (var hash in allHashes)
            {
                if (extractedCount >= maxAnims)
                {
                    break;
                }

                var name = GFPakHashCache.GetName(hash);
                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                string rel = NormalizeRomfsRelativePath(name);
                if (!rel.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!rel.EndsWith(".tranm", StringComparison.OrdinalIgnoreCase) &&
                    !rel.EndsWith(".gfbanm", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!extractor.TryGetFileBytes(hash, out var bytes))
                {
                    continue;
                }

                WriteExtractedFile(importRoot, rel, bytes);
                extractedCount++;
            }

            // File Explorer doesn't have the renderer MessageHandler; keep extraction silent here.
        }

        private static void WriteExtractedFile(string importRoot, string romfsRelativePath, byte[] bytes)
        {
            string outPath = Path.Combine(importRoot, romfsRelativePath.Replace('/', Path.DirectorySeparatorChar));
            string? dir = Path.GetDirectoryName(outPath);
            if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            File.WriteAllBytes(outPath, bytes);
        }

        private static void EnqueueTrmdlDependencies(string trmdlRomfsRelativePath, byte[] trmdlBytes, Queue<string> pending)
        {
            var mdl = FlatBufferConverter.DeserializeFrom<TRMDL>(trmdlBytes);
            string modelDir = GetDirectoryOrEmpty(trmdlRomfsRelativePath);

            if (mdl.Meshes != null)
            {
                foreach (var mesh in mdl.Meshes)
                {
                    if (!string.IsNullOrWhiteSpace(mesh?.PathName))
                    {
                        EnqueueRomfsPathCandidates(modelDir, mesh.PathName, pending);
                    }
                }
            }

            if (mdl.Materials != null)
            {
                foreach (var mat in mdl.Materials)
                {
                    if (!string.IsNullOrWhiteSpace(mat))
                    {
                        EnqueueRomfsPathCandidates(modelDir, mat, pending);
                    }
                }
            }

            if (mdl.Skeleton != null && !string.IsNullOrWhiteSpace(mdl.Skeleton.PathName))
            {
                string skelPath = CombineAndNormalizeRomfsPath(modelDir, mdl.Skeleton.PathName);
                pending.Enqueue(skelPath);

                string? category = GuessBaseSkeletonCategoryFromMesh(mdl.Meshes != null && mdl.Meshes.Length > 0 ? mdl.Meshes[0]?.PathName : null);
                if (!string.IsNullOrWhiteSpace(category))
                {
                    string skelDir = GetDirectoryOrEmpty(skelPath);
                    foreach (var relBase in GetBaseSkeletonCandidateRels(category))
                    {
                        pending.Enqueue(CombineAndNormalizeRomfsPath(skelDir, relBase));
                    }
                }
            }
        }

        private static void EnqueueTrmshDependencies(string trmshRomfsRelativePath, byte[] trmshBytes, Queue<string> pending)
        {
            var msh = FlatBufferConverter.DeserializeFrom<TRMSH>(trmshBytes);
            if (msh == null || string.IsNullOrWhiteSpace(msh.bufferFilePath))
            {
                return;
            }

            string mshDir = GetDirectoryOrEmpty(trmshRomfsRelativePath);
            EnqueueRomfsPathCandidates(mshDir, msh.bufferFilePath, pending);
        }

        private static void EnqueueTrmtrDependencies(string trmtrRomfsRelativePath, byte[] trmtrBytes, Queue<string> pending)
        {
            var mtr = FlatBufferConverter.DeserializeFrom<TRMTR>(trmtrBytes);
            if (mtr?.Materials == null)
            {
                return;
            }

            string mtrDir = GetDirectoryOrEmpty(trmtrRomfsRelativePath);
            foreach (var mat in mtr.Materials)
            {
                if (mat?.Textures == null)
                {
                    continue;
                }

                foreach (var tex in mat.Textures)
                {
                    if (string.IsNullOrWhiteSpace(tex?.File))
                    {
                        continue;
                    }

                    EnqueueRomfsPathCandidates(mtrDir, tex.File, pending);
                }
            }
        }

        private static string NormalizeRomfsRelativePath(string path)
        {
            path = (path ?? string.Empty).Replace('\\', '/').Trim();
            if (path.StartsWith("romfs://", StringComparison.OrdinalIgnoreCase))
            {
                path = path.Substring("romfs://".Length);
            }
            if (path.StartsWith("trpfs://", StringComparison.OrdinalIgnoreCase))
            {
                path = path.Substring("trpfs://".Length);
            }
            path = path.TrimStart('/');
            return path;
        }

        private static string GetDirectoryOrEmpty(string romfsRelativePath)
        {
            string normalized = NormalizeRomfsRelativePath(romfsRelativePath);
            int lastSlash = normalized.LastIndexOf('/');
            if (lastSlash < 0)
            {
                return string.Empty;
            }
            return normalized.Substring(0, lastSlash + 1);
        }

        private static string? GuessMotionDirectoryFromTrmdl(string trmdlRomfsRelativePath)
        {
            string dir = GetDirectoryOrEmpty(trmdlRomfsRelativePath);
            if (string.IsNullOrWhiteSpace(dir))
            {
                return null;
            }

            var parts = dir.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            int modelIndex = parts.FindIndex(p => p.StartsWith("model_", StringComparison.OrdinalIgnoreCase));
            if (modelIndex < 0)
            {
                return null;
            }

            string modelFolder = parts[modelIndex];
            string suffix = modelFolder.Length > "model_".Length ? modelFolder.Substring("model_".Length) : string.Empty;
            parts[modelIndex] = string.IsNullOrEmpty(suffix) ? "motion" : $"motion_{suffix}";

            return string.Join("/", parts) + "/";
        }

        private static string CombineAndNormalizeRomfsPath(string baseDir, string rel)
        {
            baseDir = NormalizeRomfsRelativePath(baseDir);
            rel = NormalizeRomfsRelativePath(rel);

            var parts = new List<string>();

            void push(string segment)
            {
                if (segment == "." || string.IsNullOrEmpty(segment))
                {
                    return;
                }
                if (segment == "..")
                {
                    if (parts.Count > 0)
                    {
                        parts.RemoveAt(parts.Count - 1);
                    }
                    return;
                }
                parts.Add(segment);
            }

            foreach (var seg in baseDir.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries))
            {
                push(seg);
            }
            foreach (var seg in rel.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries))
            {
                push(seg);
            }

            return string.Join("/", parts);
        }

        private static void EnqueueRomfsPathCandidates(string baseDir, string referencedPath, Queue<string> pending)
        {
            if (string.IsNullOrWhiteSpace(referencedPath))
            {
                return;
            }

            // Relative to base paths are queued first (most TR files use relative paths).
            pending.Enqueue(CombineAndNormalizeRomfsPath(baseDir, referencedPath));

            // Some TR files, especially textures, may store full romfs relative paths without dot segments.
            string raw = NormalizeRomfsRelativePath(referencedPath);
            if (!string.IsNullOrWhiteSpace(raw) &&
                !raw.StartsWith(".", StringComparison.Ordinal) &&
                !raw.Contains("/./", StringComparison.Ordinal) &&
                !raw.Contains("/../", StringComparison.Ordinal))
            {
                pending.Enqueue(raw);
            }
        }

        private static string? GuessBaseSkeletonCategoryFromMesh(string? meshPathName)
        {
            if (string.IsNullOrWhiteSpace(meshPathName))
            {
                return null;
            }

            string file = Path.GetFileName(meshPathName.Replace('\\', '/'));
            if (file.StartsWith("p0", StringComparison.OrdinalIgnoreCase) ||
                file.StartsWith("p1", StringComparison.OrdinalIgnoreCase) ||
                file.StartsWith("p2", StringComparison.OrdinalIgnoreCase))
            {
                return "Protag";
            }

            if (file.StartsWith("bu_", StringComparison.OrdinalIgnoreCase)) return "CommonNPCbu";
            if (file.StartsWith("dm_", StringComparison.OrdinalIgnoreCase)) return "CommonNPCdm";
            if (file.StartsWith("df_", StringComparison.OrdinalIgnoreCase)) return "CommonNPCdf";
            if (file.StartsWith("em_", StringComparison.OrdinalIgnoreCase)) return "CommonNPCem";
            if (file.StartsWith("fm_", StringComparison.OrdinalIgnoreCase)) return "CommonNPCfm";
            if (file.StartsWith("ff_", StringComparison.OrdinalIgnoreCase)) return "CommonNPCff";
            if (file.StartsWith("gm_", StringComparison.OrdinalIgnoreCase)) return "CommonNPCgm";
            if (file.StartsWith("gf_", StringComparison.OrdinalIgnoreCase)) return "CommonNPCgf";
            if (file.StartsWith("rv_", StringComparison.OrdinalIgnoreCase)) return "CommonNPCrv";

            return null;
        }

        private static IEnumerable<string> GetBaseSkeletonCandidateRels(string category)
        {
            return category switch
            {
                "Protag" => new[]
                {
                    "../../model_pc_base/model/p0_base.trskl",
                    "../../../../p2/model/base/p2_base0001_00_default/p2_base0001_00_default.trskl",
                    "../../p2/p2_base0001_00_default/p2_base0001_00_default.trskl"
                },
                "CommonNPCbu" => new[] { "../../../model_cc_base/bu/bu_base.trskl", "../../base/cc_base0001_00_young_m/cc_base0001_00_young_m.trskl" },
                "CommonNPCdm" or "CommonNPCdf" => new[] { "../../../model_cc_base/dm/dm_base.trskl", "../../base/cc_base0001_00_young_m/cc_base0001_00_young_m.trskl" },
                "CommonNPCem" => new[] { "../../../model_cc_base/em/em_base.trskl", "../../base/cc_base0001_00_young_m/cc_base0001_00_young_m.trskl" },
                "CommonNPCfm" or "CommonNPCff" => new[] { "../../../model_cc_base/fm/fm_base.trskl", "../../base/cc_base0001_00_young_m/cc_base0001_00_young_m.trskl" },
                "CommonNPCgm" or "CommonNPCgf" => new[] { "../../../model_cc_base/gm/gm_base.trskl", "../../base/cc_base0001_00_young_m/cc_base0001_00_young_m.trskl" },
                "CommonNPCrv" => new[] { "../../../model_cc_base/rv/rv_base.trskl", "../../base/cc_base0001_00_young_m/cc_base0001_00_young_m.trskl" },
                _ => Array.Empty<string>()
            };
        }
    }
}
