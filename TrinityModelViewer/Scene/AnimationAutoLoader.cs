using GFTool.Renderer.Scene.GraphicsObjects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GfAnim = Trinity.Core.Flatbuffers.GF.Animation;
using Trinity.Core.Utils;

namespace TrinityModelViewer.Scene
{
    internal sealed class AnimationAutoLoader
    {
        internal readonly record struct LoadedAnimation(string Path, Animation Animation);

        public IReadOnlyList<string> BuildSearchDirectories(ViewerSettings settings, string trmdlPath, Action<string>? debugLog = null)
        {
            if (settings == null || string.IsNullOrWhiteSpace(trmdlPath))
            {
                return Array.Empty<string>();
            }

            string? effectiveTrmdlPath = trmdlPath;
            string? animDir = GuessAnimationDirectory(trmdlPath);
            if ((string.IsNullOrWhiteSpace(animDir) || !Directory.Exists(animDir)) &&
                settings.EnableExtractedOutFallback)
            {
                var outRoot = ResolveActiveExtractedOutRoot(settings);
                if (!string.IsNullOrWhiteSpace(outRoot) &&
                    TryMapPathToExtractedOut(trmdlPath, outRoot, settings.ActiveExtractedGame, out var mappedTrmdlPath))
                {
                    string? mappedAnimDir = GuessAnimationDirectory(mappedTrmdlPath);
                    if (!string.IsNullOrWhiteSpace(mappedAnimDir) && Directory.Exists(mappedAnimDir))
                    {
                        effectiveTrmdlPath = mappedTrmdlPath;
                        animDir = mappedAnimDir;
                        debugLog?.Invoke($"[Anim] AutoLoad: mapped to extracted out -> '{mappedAnimDir}'");
                    }
                }
            }

            bool wantsMotionPcBase =
                NeedsMotionPcBaseAnimations(effectiveTrmdlPath) ||
                UsesP0BaseSkeleton(trmdlPath, settings.EnableExtractedOutFallback ? ResolveActiveExtractedOutRoot(settings) : null, settings.ActiveExtractedGame);

            var animDirs = new List<string>();
            if (!string.IsNullOrWhiteSpace(animDir) && Directory.Exists(animDir))
            {
                animDirs.Add(animDir);
            }

            if (IsZaTrainerModelForMotionUq(trmdlPath, settings.ActiveExtractedGame))
            {
                string? motionUqDir =
                    GuessIkCharaMotionUqDirectory(effectiveTrmdlPath) ??
                    GuessCharaMotionUqDirectory(effectiveTrmdlPath) ??
                    GuessExtractedOutMotionUqDirectory(settings, trmdlPath);

                if (!string.IsNullOrWhiteSpace(motionUqDir) && Directory.Exists(motionUqDir))
                {
                    animDirs.Add(motionUqDir);
                    debugLog?.Invoke($"[Anim] AutoLoad: also scanning uq motions '{motionUqDir}'");
                }
            }

            if (wantsMotionPcBase)
            {
                string? motionPcBaseDir =
                    GuessIkCharaMotionPcBaseDirectory(effectiveTrmdlPath) ??
                    GuessCharaMotionPcBaseDirectory(effectiveTrmdlPath) ??
                    (!string.IsNullOrWhiteSpace(animDir) ? (GuessIkCharaMotionPcBaseDirectory(animDir) ?? GuessMotionPcBaseDirectory(animDir)) : null) ??
                    GuessMotionPcBaseFromModelPath(effectiveTrmdlPath) ??
                    GuessExtractedOutMotionPcBaseDirectory(settings);

                if (!string.IsNullOrWhiteSpace(motionPcBaseDir) && Directory.Exists(motionPcBaseDir))
                {
                    animDirs.Add(motionPcBaseDir);
                    debugLog?.Invoke($"[Anim] AutoLoad: also scanning base motions '{motionPcBaseDir}'");
                }
            }

            return animDirs.Count == 0
                ? Array.Empty<string>()
                : animDirs.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        public async Task<List<LoadedAnimation>> LoadAnimationsFromDirectoriesAsync(
            IReadOnlyList<string> animDirs,
            int maxToLoadTotal,
            HashSet<string> existingPaths,
            CancellationToken token)
        {
            if (animDirs == null || animDirs.Count == 0 || maxToLoadTotal <= 0)
            {
                return new List<LoadedAnimation>();
            }

            var loaded = new List<LoadedAnimation>();
            int loadedTotal = 0;

            foreach (var dir in animDirs)
            {
                token.ThrowIfCancellationRequested();

                int remaining = maxToLoadTotal - loadedTotal;
                if (remaining <= 0)
                {
                    break;
                }

                var results = await Task.Run(
                    () => LoadAnimationsFromDirectoryWorker(dir, remaining, existingPaths, token),
                    token);

                loaded.AddRange(results.Select(r => new LoadedAnimation(r.Path, r.Anim)));
                loadedTotal += results.Count;
            }

            return loaded;
        }

        private static List<(string Path, Animation Anim)> LoadAnimationsFromDirectoryWorker(
            string animDir,
            int maxToLoad,
            HashSet<string> existingPaths,
            CancellationToken token)
        {
            var results = new List<(string, Animation)>();
            if (maxToLoad <= 0 || string.IsNullOrWhiteSpace(animDir))
            {
                return results;
            }

            IEnumerable<string> tranm = Enumerable.Empty<string>();
            IEnumerable<string> gfbanm = Enumerable.Empty<string>();

            try
            {
                tranm = Directory.EnumerateFiles(animDir, "*.tranm", SearchOption.TopDirectoryOnly);
                gfbanm = Directory.EnumerateFiles(animDir, "*.gfbanm", SearchOption.TopDirectoryOnly);
            }
            catch
            {
                return results;
            }

            var files = tranm.Concat(gfbanm)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
                .Take(maxToLoad)
                .ToList();

            foreach (var file in files)
            {
                token.ThrowIfCancellationRequested();
                if (!existingPaths.Add(file))
                {
                    continue;
                }

                try
                {
                    var animFile = FlatBufferConverter.DeserializeFrom<GfAnim.Animation>(file);
                    var anim = new Animation(animFile, Path.GetFileNameWithoutExtension(file), file);
                    results.Add((file, anim));
                }
                catch
                {
                    // Ignore individual animation failures in worker mode.
                }
            }

            return results;
        }

        private static string ResolveActiveExtractedOutRoot(ViewerSettings settings)
        {
            string active = settings.ActiveExtractedGame?.Trim() ?? string.Empty;
            string za = settings.ZaExtractedOutRoot?.Trim() ?? string.Empty;
            string sv = settings.SvExtractedOutRoot?.Trim() ?? string.Empty;

            if (string.Equals(active, "SV", StringComparison.OrdinalIgnoreCase))
            {
                return !string.IsNullOrWhiteSpace(sv) ? sv : za;
            }

            return !string.IsNullOrWhiteSpace(za) ? za : sv;
        }

        private static string? GuessAnimationDirectory(string trmdlPath)
        {
            if (string.IsNullOrWhiteSpace(trmdlPath))
            {
                return null;
            }

            string full = Path.GetFullPath(trmdlPath);
            string? dir = Path.GetDirectoryName(full);
            if (string.IsNullOrWhiteSpace(dir))
            {
                return null;
            }

            var fileName = Path.GetFileName(trmdlPath);
            if (!string.IsNullOrEmpty(fileName) && fileName.StartsWith("pm", StringComparison.OrdinalIgnoreCase))
            {
                return dir;
            }

            char[] seps = new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };
            var parts = dir.Split(seps, StringSplitOptions.RemoveEmptyEntries).ToList();
            if (parts.Count == 0)
            {
                return null;
            }

            int modelIndex = parts.FindIndex(p => p.StartsWith("model_", StringComparison.OrdinalIgnoreCase));
            if (modelIndex < 0)
            {
                return null;
            }

            string modelFolder = parts[modelIndex];
            string suffix = modelFolder.Length > "model_".Length ? modelFolder.Substring("model_".Length) : string.Empty;
            string motionFolder = string.IsNullOrEmpty(suffix) ? "motion" : $"motion_{suffix}";
            parts[modelIndex] = motionFolder;

            return string.Join(Path.DirectorySeparatorChar, parts);
        }

        private static bool NeedsMotionPcBaseAnimations(string trmdlPath)
        {
            if (string.IsNullOrWhiteSpace(trmdlPath))
            {
                return false;
            }

            var file = Path.GetFileName(trmdlPath);
            if (!string.IsNullOrWhiteSpace(file) &&
                (file.StartsWith("p0_", StringComparison.OrdinalIgnoreCase) ||
                 file.StartsWith("p1_", StringComparison.OrdinalIgnoreCase) ||
                 file.StartsWith("p2_", StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            string lower = trmdlPath.Replace('\\', '/').ToLowerInvariant();
            return lower.Contains("/ik_chara/") ||
                   ((lower.Contains("/model_pc/") || lower.Contains("/model_pc_base/") || lower.Contains("/model_cc_")) &&
                    !lower.Contains("/ik_pokemon/") &&
                    !lower.Contains("/pokemon/"));
        }

        private static bool UsesP0BaseSkeleton(string trmdlPath, string? extractedOutRoot, string? extractedGame)
        {
            if (string.IsNullOrWhiteSpace(trmdlPath))
            {
                return false;
            }

            try
            {
                string pathToRead = trmdlPath;
                if (!File.Exists(pathToRead) &&
                    !string.IsNullOrWhiteSpace(extractedOutRoot) &&
                    TryMapPathToExtractedOut(trmdlPath, extractedOutRoot, extractedGame, out var mapped) &&
                    File.Exists(mapped))
                {
                    pathToRead = mapped;
                }

                if (!File.Exists(pathToRead))
                {
                    return false;
                }

                var trmdl = FlatBufferConverter.DeserializeFrom<Trinity.Core.Flatbuffers.TR.Model.TRMDL>(pathToRead);
                var skel = trmdl?.Skeleton?.PathName;
                return !string.IsNullOrWhiteSpace(skel) &&
                       skel.Replace('\\', '/').EndsWith("/p0_base.trskl", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private static string? GuessCharaMotionPcBaseDirectory(string anyPathUnderChara)
        {
            if (string.IsNullOrWhiteSpace(anyPathUnderChara))
            {
                return null;
            }

            string full = Path.GetFullPath(anyPathUnderChara);
            char[] seps = new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };
            var parts = full.Split(seps, StringSplitOptions.RemoveEmptyEntries).ToList();
            if (parts.Count == 0)
            {
                return null;
            }

            int charaIndex = parts.FindIndex(p => string.Equals(p, "chara", StringComparison.OrdinalIgnoreCase));
            if (charaIndex < 0)
            {
                return null;
            }

            var root = parts.Take(charaIndex + 1).ToList();
            root.Add("motion_pc");
            root.Add("base");
            return string.Join(Path.DirectorySeparatorChar, root);
        }

        private static bool IsZaTrainerModelForMotionUq(string trmdlPath, string? extractedGame)
        {
            if (!string.IsNullOrWhiteSpace(extractedGame) &&
                extractedGame.Trim().Equals("SV", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var file = Path.GetFileName(trmdlPath);
            return !string.IsNullOrWhiteSpace(file) && file.StartsWith("tr", StringComparison.OrdinalIgnoreCase);
        }

        private static string? GuessIkCharaMotionUqDirectory(string anyPathUnderIkChara)
        {
            if (string.IsNullOrWhiteSpace(anyPathUnderIkChara))
            {
                return null;
            }

            string full = Path.GetFullPath(anyPathUnderIkChara);
            char[] seps = new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };
            var parts = full.Split(seps, StringSplitOptions.RemoveEmptyEntries).ToList();
            if (parts.Count == 0)
            {
                return null;
            }

            int ikIndex = parts.FindIndex(p => string.Equals(p, "ik_chara", StringComparison.OrdinalIgnoreCase));
            if (ikIndex < 0)
            {
                return null;
            }

            string modelFolder = parts.Count > 0 ? parts[^2] : string.Empty;
            if (string.IsNullOrWhiteSpace(modelFolder))
            {
                return null;
            }

            var root = parts.Take(ikIndex + 1).ToList();
            root.Add("motion_uq");
            root.Add(modelFolder);
            return string.Join(Path.DirectorySeparatorChar, root);
        }

        private static string? GuessCharaMotionUqDirectory(string anyPathUnderChara)
        {
            if (string.IsNullOrWhiteSpace(anyPathUnderChara))
            {
                return null;
            }

            string full = Path.GetFullPath(anyPathUnderChara);
            char[] seps = new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };
            var parts = full.Split(seps, StringSplitOptions.RemoveEmptyEntries).ToList();
            if (parts.Count == 0)
            {
                return null;
            }

            int charaIndex = parts.FindIndex(p => string.Equals(p, "chara", StringComparison.OrdinalIgnoreCase));
            if (charaIndex < 0)
            {
                return null;
            }

            string modelFolder = parts.Count > 0 ? parts[^2] : string.Empty;
            if (string.IsNullOrWhiteSpace(modelFolder))
            {
                return null;
            }

            var root = parts.Take(charaIndex + 1).ToList();
            root.Add("motion_uq");
            root.Add(modelFolder);
            return string.Join(Path.DirectorySeparatorChar, root);
        }

        private static string? GuessExtractedOutMotionUqDirectory(ViewerSettings settings, string trmdlPath)
        {
            if (settings == null || !settings.EnableExtractedOutFallback)
            {
                return null;
            }

            var outRoot = ResolveActiveExtractedOutRoot(settings);
            if (string.IsNullOrWhiteSpace(outRoot) || !Directory.Exists(outRoot))
            {
                return null;
            }

            string full = Path.GetFullPath(trmdlPath);
            string modelFolder = Path.GetFileName(Path.GetDirectoryName(full) ?? string.Empty);
            if (string.IsNullOrWhiteSpace(modelFolder))
            {
                return null;
            }

            string charaRoot = string.Equals(settings.ActiveExtractedGame?.Trim(), "SV", StringComparison.OrdinalIgnoreCase) ? "chara" : "ik_chara";
            return Path.Combine(outRoot, charaRoot, "motion_uq", modelFolder);
        }

        private static string? GuessMotionPcBaseFromModelPath(string trmdlPath)
        {
            if (string.IsNullOrWhiteSpace(trmdlPath))
            {
                return null;
            }

            string full = Path.GetFullPath(trmdlPath);
            string norm = full.Replace('\\', '/');

            int modelPcIdx = norm.IndexOf("/model_pc/", StringComparison.OrdinalIgnoreCase);
            int modelPcBaseIdx = norm.IndexOf("/model_pc_base/", StringComparison.OrdinalIgnoreCase);
            int idx = modelPcIdx >= 0 ? modelPcIdx : modelPcBaseIdx;
            if (idx < 0)
            {
                return null;
            }

            string prefix = norm.Substring(0, idx).TrimEnd('/');
            string baseDir = $"{prefix}/motion_pc/base";
            return baseDir.Replace('/', Path.DirectorySeparatorChar);
        }

        private static string? GuessExtractedOutMotionPcBaseDirectory(ViewerSettings settings)
        {
            if (settings == null || !settings.EnableExtractedOutFallback)
            {
                return null;
            }

            var outRoot = ResolveActiveExtractedOutRoot(settings);
            if (string.IsNullOrWhiteSpace(outRoot) || !Directory.Exists(outRoot))
            {
                return null;
            }

            string charaRoot = string.Equals(settings.ActiveExtractedGame?.Trim(), "SV", StringComparison.OrdinalIgnoreCase) ? "chara" : "ik_chara";
            return Path.Combine(outRoot, charaRoot, "motion_pc", "base");
        }

        private static bool TryMapPathToExtractedOut(string inputPath, string outRoot, string? extractedGame, out string mappedPath)
        {
            mappedPath = string.Empty;
            if (string.IsNullOrWhiteSpace(inputPath) || string.IsNullOrWhiteSpace(outRoot))
            {
                return false;
            }

            string charaRoot = string.Equals(extractedGame?.Trim(), "SV", StringComparison.OrdinalIgnoreCase) ? "chara" : "ik_chara";
            string pokemonRoot = string.Equals(extractedGame?.Trim(), "SV", StringComparison.OrdinalIgnoreCase) ? "pokemon" : "ik_pokemon";

            string full = Path.GetFullPath(inputPath);
            string norm = full.Replace('\\', '/');

            int ikCharaIdx = norm.IndexOf("/ik_chara/", StringComparison.OrdinalIgnoreCase);
            if (ikCharaIdx >= 0)
            {
                string tail = norm.Substring(ikCharaIdx + "/ik_chara/".Length).TrimStart('/');
                mappedPath = Path.Combine(outRoot, charaRoot, tail);
                return true;
            }

            int ikPokemonIdx = norm.IndexOf("/ik_pokemon/", StringComparison.OrdinalIgnoreCase);
            if (ikPokemonIdx >= 0)
            {
                string tail = norm.Substring(ikPokemonIdx + "/ik_pokemon/".Length).TrimStart('/');
                mappedPath = Path.Combine(outRoot, pokemonRoot, tail);
                return true;
            }

            int charaIdx = norm.IndexOf("/chara/", StringComparison.OrdinalIgnoreCase);
            if (charaIdx >= 0)
            {
                string tail = norm.Substring(charaIdx + "/chara/".Length).TrimStart('/');
                mappedPath = Path.Combine(outRoot, charaRoot, tail);
                return true;
            }

            int pokemonIdx = norm.IndexOf("/pokemon/", StringComparison.OrdinalIgnoreCase);
            if (pokemonIdx >= 0)
            {
                string tail = norm.Substring(pokemonIdx + "/pokemon/".Length).TrimStart('/');
                mappedPath = Path.Combine(outRoot, pokemonRoot, tail);
                return true;
            }

            if (TryExtractTailFromKnownTrinityRoot(norm, out var tailFromRoot))
            {
                mappedPath = Path.Combine(outRoot, charaRoot, tailFromRoot);
                return true;
            }

            try
            {
                var fileName = Path.GetFileName(full);
                var folderName = Path.GetFileName(Path.GetDirectoryName(full) ?? string.Empty);
                if (!string.IsNullOrWhiteSpace(fileName) && !string.IsNullOrWhiteSpace(folderName))
                {
                    foreach (var modelRoot in new[] { "model_pc", "model_pc_base", "model_cc_base", "model_cc" })
                    {
                        var candidate = Path.Combine(outRoot, charaRoot, modelRoot, folderName, fileName);
                        if (File.Exists(candidate))
                        {
                            mappedPath = candidate;
                            return true;
                        }
                    }
                }
            }
            catch
            {
                // Ignore and fall through.
            }

            return false;
        }

        private static bool TryExtractTailFromKnownTrinityRoot(string normalizedPath, out string tail)
        {
            tail = string.Empty;
            if (string.IsNullOrWhiteSpace(normalizedPath))
            {
                return false;
            }

            string[] roots =
            {
                "/model_pc_base/",
                "/model_pc/",
                "/model_cc_base/",
                "/model_cc_",
                "/motion_pc_base/",
                "/motion_pc/",
                "/motion_cc_base/",
                "/motion_cc_",
                "/share/",
            };

            int best = -1;
            foreach (var token in roots)
            {
                int idx = normalizedPath.IndexOf(token, StringComparison.OrdinalIgnoreCase);
                if (idx >= 0 && (best < 0 || idx < best))
                {
                    best = idx;
                }
            }

            if (best < 0)
            {
                return false;
            }

            tail = normalizedPath.Substring(best + 1).TrimStart('/');
            return !string.IsNullOrWhiteSpace(tail);
        }

        private static string? GuessIkCharaMotionPcBaseDirectory(string anyPathUnderIkChara)
        {
            if (string.IsNullOrWhiteSpace(anyPathUnderIkChara))
            {
                return null;
            }

            string full = Path.GetFullPath(anyPathUnderIkChara);
            char[] seps = new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };
            var parts = full.Split(seps, StringSplitOptions.RemoveEmptyEntries).ToList();
            if (parts.Count == 0)
            {
                return null;
            }

            int ikIndex = parts.FindIndex(p => string.Equals(p, "ik_chara", StringComparison.OrdinalIgnoreCase));
            if (ikIndex < 0)
            {
                return null;
            }

            var root = parts.Take(ikIndex + 1).ToList();
            root.Add("motion_pc");
            root.Add("base");
            return string.Join(Path.DirectorySeparatorChar, root);
        }

        private static string? GuessMotionPcBaseDirectory(string animDir)
        {
            if (string.IsNullOrWhiteSpace(animDir))
            {
                return null;
            }

            string full = Path.GetFullPath(animDir);
            char[] seps = new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };
            var parts = full.Split(seps, StringSplitOptions.RemoveEmptyEntries).ToList();
            if (parts.Count == 0)
            {
                return null;
            }

            int motionPcIndex = parts.FindIndex(p => string.Equals(p, "motion_pc", StringComparison.OrdinalIgnoreCase));
            if (motionPcIndex < 0)
            {
                return null;
            }

            string? root = Path.GetPathRoot(full);
            int startIndex = 0;

            if (!string.IsNullOrWhiteSpace(root) && parts[0].EndsWith(":", StringComparison.Ordinal))
            {
                startIndex = 1;
            }

            if (motionPcIndex < startIndex)
            {
                return null;
            }

            var segments = parts
                .Skip(startIndex)
                .Take(motionPcIndex - startIndex + 1)
                .Concat(new[] { "base" })
                .ToArray();

            return string.IsNullOrWhiteSpace(root)
                ? Path.Combine(segments)
                : Path.Combine(new[] { root }.Concat(segments).ToArray());
        }
    }
}
