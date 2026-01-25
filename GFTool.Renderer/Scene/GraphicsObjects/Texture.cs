using BnTxx;
using GFTool.Core.Utils;
using GFTool.Renderer.Core;
using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.Versioning;
using Trinity.Core.Flatbuffers.TR.Model;
using Trinity.Core.Assets;

namespace GFTool.Renderer.Scene.GraphicsObjects
{
    public partial class Texture : IDisposable
    {
        private class CachedTexture
        {
            public int TextureId;
            public int RefCount;
        }

        private static readonly Dictionary<string, CachedTexture> cache = new Dictionary<string, CachedTexture>(StringComparer.OrdinalIgnoreCase);
        private static readonly object cacheLock = new object();
        private static readonly Dictionary<string, Bitmap> overrideBitmaps = new Dictionary<string, Bitmap>(StringComparer.OrdinalIgnoreCase);

        public string Name { get; private set; }
        public string SourceFile { get; private set; }
        public uint Slot { get; private set; }
        public Bitmap tex { get; private set; }
        public int textureId { get; private set; }

        private readonly string cacheKey;
        private readonly string logicalTexturePath;
        private readonly string logicalAltTexturePath;
        private readonly string diskTexturePath;
        private readonly string diskAltTexturePath;
        private readonly string preferredName;
        private readonly TextureWrapMode wrapS;
        private readonly TextureWrapMode wrapT;
        private readonly TextureWrapMode wrapR;
        private readonly IAssetProvider? assetProvider;

        public TextureWrapMode WrapS => wrapS;
        public TextureWrapMode WrapT => wrapT;
        public string CacheKey => cacheKey;
        public bool IsEdited
        {
            get
            {
                lock (cacheLock)
                {
                    return overrideBitmaps.ContainsKey(cacheKey);
                }
            }
        }

        internal void BeginAsyncLoadIfEnabled()
        {
            if (!RenderOptions.EnableAsyncResourceLoading)
            {
                return;
            }

            StartAsyncDecodeIfNeeded();
        }

        public Texture(PathString modelPath, TRTexture img, TRSampler? sampler = null, IAssetProvider? assetProvider = null)
        {
            Name = img.Name;
            SourceFile = img.File;
            Slot = img.Slot;
            this.assetProvider = assetProvider;

            logicalTexturePath = modelPath.Combine(img.File);
            logicalAltTexturePath = modelPath.Combine(Path.GetFileName(img.File));

            string texturePath;
            try
            {
                texturePath = Path.GetFullPath(logicalTexturePath);
            }
            catch
            {
                texturePath = logicalTexturePath;
            }
            var preferredName = Path.GetFileNameWithoutExtension(img.File);
            diskTexturePath = texturePath;
            try
            {
                diskAltTexturePath = Path.GetFullPath(logicalAltTexturePath);
            }
            catch
            {
                diskAltTexturePath = logicalAltTexturePath;
            }
            this.preferredName = preferredName;
            var keyPath =
                File.Exists(diskTexturePath) ? diskTexturePath :
                (File.Exists(diskAltTexturePath) ? diskAltTexturePath :
                diskTexturePath);

            wrapS = ConvertWrapMode(sampler?.RepeatU);
            wrapT = ConvertWrapMode(sampler?.RepeatV);
            wrapR = ConvertWrapMode(sampler?.RepeatW);
            cacheKey = $"{keyPath}|{preferredName}|{wrapS}|{wrapT}|{wrapR}";

            lock (cacheLock)
            {
                if (cache.TryGetValue(cacheKey, out var cached))
                {
                    cached.RefCount++;
                    textureId = cached.TextureId;
                    return;
                }
            }
        }

        private IEnumerable<string> EnumerateCandidateTexturePaths()
        {
            var candidates = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            void Add(string path)
            {
                if (string.IsNullOrWhiteSpace(path))
                {
                    return;
                }

                if (seen.Add(path))
                {
                    candidates.Add(path);
                }
            }

            // Prefer normalized disk paths so File.Exists works even when logical paths contain mixed separators / "..".
            Add(diskTexturePath);
            Add(diskAltTexturePath);
            Add(logicalTexturePath);
            Add(logicalAltTexturePath);

            // If the relative depth differs between extracted layouts, try locating the "share/..." subtree by
            // walking up from the model directory.
            if (!string.IsNullOrWhiteSpace(SourceFile))
            {
                var normalized = SourceFile.Replace('\\', '/');
                int shareIndex = normalized.IndexOf("/share/", StringComparison.OrdinalIgnoreCase);
                if (shareIndex < 0)
                {
                    shareIndex = normalized.IndexOf("share/", StringComparison.OrdinalIgnoreCase);
                }

                if (shareIndex >= 0)
                {
                    var shareSubpath = normalized.Substring(shareIndex).TrimStart('/');
                    shareSubpath = shareSubpath.Replace('/', Path.DirectorySeparatorChar);

                    var modelDir =
                        Path.GetDirectoryName(diskAltTexturePath) ??
                        Path.GetDirectoryName(diskTexturePath) ??
                        string.Empty;

                    try
                    {
                        var current = new DirectoryInfo(modelDir);
                        for (int i = 0; i < 8 && current != null; i++)
                        {
                            Add(Path.Combine(current.FullName, shareSubpath));
                            current = current.Parent;
                        }
                    }
                    catch
                    {
                        // Ignore filesystem path issues; other candidates will still be tried.
                    }
                }
            }

            // Optional: extracted "out" fallback for disk loads in known Trinity subtrees (model_*/motion_*/share, ik_*).
            if (assetProvider is DiskAssetProvider &&
                RenderOptions.EnableExtractedOutFallback &&
                !string.IsNullOrWhiteSpace(RenderOptions.ExtractedOutRoot) &&
                !string.IsNullOrWhiteSpace(SourceFile))
            {
                var baseDir =
                    Path.GetDirectoryName(diskAltTexturePath) ??
                    Path.GetDirectoryName(diskTexturePath) ??
                    string.Empty;

                if (IsEligibleForExtractedOutFallback(baseDir) || SourceFile.IndexOf("share/", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    string outRoot = RenderOptions.ExtractedOutRoot;
                    string game = RenderOptions.ExtractedOutGame;
                    string domain = ResolveCharaOrPokemonRootForSharePath(game, outRoot, SourceFile);
                    if (TryMapBaseDirToExtractedOut(game, outRoot, domain, baseDir, out var mappedBaseDir))
                    {
                        Add(CombineAndNormalize(mappedBaseDir, SourceFile));
                        Add(CombineAndNormalize(mappedBaseDir, Path.GetFileName(SourceFile)));
                    }
                    else
                    {
                        // we can't infer the in-game root from baseDir. For share/ references, we can still re-root the
                        // request directly under the extracted out tree.
                        if (TryGetShareDomain(SourceFile, out _))
                        {
                            var normalized = SourceFile.Replace('\\', '/');
                            int shareIndex = normalized.IndexOf("/share/", StringComparison.OrdinalIgnoreCase);
                            if (shareIndex < 0)
                            {
                                shareIndex = normalized.IndexOf("share/", StringComparison.OrdinalIgnoreCase);
                            }

                            if (shareIndex >= 0)
                            {
                                var shareSubpath = normalized.Substring(shareIndex).TrimStart('/');
                                shareSubpath = shareSubpath.Replace('/', Path.DirectorySeparatorChar);
                                Add(Path.Combine(outRoot, domain, shareSubpath));
                            }
                        }
                    }
                }
            }

            foreach (var candidate in candidates)
            {
                yield return candidate;
            }
        }

        private static bool IsEligibleForExtractedOutFallback(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            string lower = path.Replace('\\', '/').ToLowerInvariant();
            // Keep this generic: don’t depend on any specific local folder name (e.g. "glue").
            // We only opt-in for known Trinity subtrees where shared assets are expected.
            return lower.Contains("/ik_chara/") ||
                   lower.Contains("/ik_pokemon/") ||
                   lower.Contains("/chara/") ||
                   lower.Contains("/pokemon/") ||
                   lower.Contains("/model_pc/") ||
                   lower.Contains("/model_pc_base/") ||
                   lower.Contains("/model_cc_") ||
                   lower.Contains("/motion_pc/") ||
                   lower.Contains("/motion_cc_") ||
                   lower.Contains("/share/");
        }

        private static string ResolveCharaOrPokemonRootForSharePath(string game, string outRoot, string sourceFile)
        {
            // Default to "chara". Switch to "pokemon" only when the share/<domain> folder exists there.
            if (string.IsNullOrWhiteSpace(outRoot) || string.IsNullOrWhiteSpace(sourceFile))
            {
                return GetCharaRootFolder(game);
            }

            if (!TryGetShareDomain(sourceFile, out var domain))
            {
                return GetCharaRootFolder(game);
            }

            try
            {
                string pokemonRoot = GetPokemonRootFolder(game);
                string pokemonDomain = Path.Combine(outRoot, pokemonRoot, "share", domain);
                if (Directory.Exists(pokemonDomain))
                {
                    return pokemonRoot;
                }
            }
            catch
            {
                // Ignore.
            }

            return GetCharaRootFolder(game);
        }

        private static bool TryGetShareDomain(string sourceFile, out string domain)
        {
            domain = string.Empty;
            if (string.IsNullOrWhiteSpace(sourceFile))
            {
                return false;
            }

            string normalized = sourceFile.Replace('\\', '/');
            int shareIdx = normalized.IndexOf("/share/", StringComparison.OrdinalIgnoreCase);
            if (shareIdx < 0)
            {
                shareIdx = normalized.IndexOf("share/", StringComparison.OrdinalIgnoreCase);
            }

            if (shareIdx < 0)
            {
                return false;
            }

            string tail = normalized.Substring(shareIdx).TrimStart('/');
            var parts = tail.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2 || !parts[0].Equals("share", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            domain = parts[1];
            return !string.IsNullOrWhiteSpace(domain);
        }

        private static bool TryMapBaseDirToExtractedOut(string game, string outRoot, string domainRoot, string baseDir, out string mappedBaseDir)
        {
            mappedBaseDir = string.Empty;
            if (string.IsNullOrWhiteSpace(outRoot) || string.IsNullOrWhiteSpace(domainRoot) || string.IsNullOrWhiteSpace(baseDir))
            {
                return false;
            }

            string norm = baseDir.Replace('\\', '/');

            // If the current path already contains an extracted-tree root folder, keep the tail under it.
            // ZA: ik_chara / ik_pokemon
            // SV: chara / pokemon
            foreach (var root in new[] { "ik_chara", "ik_pokemon", "chara", "pokemon" })
            {
                string token = "/" + root + "/";
                int idx = norm.IndexOf(token, StringComparison.OrdinalIgnoreCase);
                if (idx >= 0)
                {
                    string tail = norm.Substring(idx + token.Length).TrimStart('/');
                    mappedBaseDir = Path.Combine(outRoot, domainRoot, tail);
                    return true;
                }
            }

            if (TryExtractTailFromKnownTrinityRoot(norm, out var tailFromRoot))
            {
                mappedBaseDir = Path.Combine(outRoot, domainRoot, tailFromRoot);
                return true;
            }

            return false;
        }

        private static string GetCharaRootFolder(string game)
        {
            return string.Equals(game?.Trim(), "SV", StringComparison.OrdinalIgnoreCase) ? "chara" : "ik_chara";
        }

        private static string GetPokemonRootFolder(string game)
        {
            return string.Equals(game?.Trim(), "SV", StringComparison.OrdinalIgnoreCase) ? "pokemon" : "ik_pokemon";
        }

        private static bool TryExtractTailFromKnownTrinityRoot(string normalizedPath, out string tail)
        {
            tail = string.Empty;
            if (string.IsNullOrWhiteSpace(normalizedPath))
            {
                return false;
            }

            // Choose the earliest known subtree segment in the path and treat it as the in-game relative root.
            // Examples:
            // - .../model_pc/p0_xxx/...   -> model_pc/p0_xxx/...
            // - .../motion_pc/base/...    -> motion_pc/base/...
            // - .../share/common/...      -> share/common/...
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
                "/ik_effect/",
                "/ik_message/",
                "/ik_event/",
                "/ik_demo/"
            };

            int best = -1;
            string bestToken = string.Empty;
            foreach (var token in roots)
            {
                int idx = normalizedPath.IndexOf(token, StringComparison.OrdinalIgnoreCase);
                if (idx >= 0 && (best < 0 || idx < best))
                {
                    best = idx;
                    bestToken = token;
                }
            }

            if (best < 0)
            {
                return false;
            }

            tail = normalizedPath.Substring(best + 1).TrimStart('/');
            return !string.IsNullOrWhiteSpace(tail);
        }

        private static string CombineAndNormalize(string dir, string relativeOrName)
        {
            if (string.IsNullOrWhiteSpace(dir))
            {
                return relativeOrName ?? string.Empty;
            }

            if (string.IsNullOrWhiteSpace(relativeOrName))
            {
                return dir;
            }

            // If a path is already rooted, treat it as "in-game rooted" and try to re-root it under outRoot instead.
            if (Path.IsPathRooted(relativeOrName))
            {
                relativeOrName = relativeOrName.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }

            try
            {
                return Path.GetFullPath(Path.Combine(dir, relativeOrName));
            }
            catch
            {
                return Path.Combine(dir, relativeOrName);
            }
        }

        public void EnsureLoaded()
        {
            if (textureId > 0 && (!RenderOptions.EnableAsyncResourceLoading || textureId != placeholderTextureId))
            {
                return;
            }

            if (!RenderOptions.EnableAsyncResourceLoading)
            {
                EnsureLoadedSync();
                return;
            }

            lock (cacheLock)
            {
                if (cache.TryGetValue(cacheKey, out var cached))
                {
                    cached.RefCount++;
                    textureId = cached.TextureId;
                    return;
                }
            }

            EnsurePlaceholderTexture();
            StartAsyncDecodeIfNeeded();
            textureId = placeholderTextureId;
        }

        public bool TryReplaceFromImage(Bitmap bitmap, out string error)
        {
            if (!OperatingSystem.IsWindows())
            {
                error = "Editing textures requires Windows (System.Drawing).";
                return false;
            }

            return TryReplaceFromImageWindows(bitmap, out error);
        }

        [SupportedOSPlatform("windows")]
        private bool TryReplaceFromImageWindows(Bitmap bitmap, out string error)
        {
            error = string.Empty;
            if (bitmap == null)
            {
                error = "No bitmap provided.";
                return false;
            }

            try
            {
                EnsureLoaded();
                if (textureId <= 0)
                {
                    error = "Texture is not loaded.";
                    return false;
                }

                Bitmap upload = bitmap.PixelFormat == System.Drawing.Imaging.PixelFormat.Format32bppArgb
                    ? (Bitmap)bitmap.Clone()
                    : bitmap.Clone(new Rectangle(0, 0, bitmap.Width, bitmap.Height), System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                lock (cacheLock)
                {
                    if (overrideBitmaps.TryGetValue(cacheKey, out var existing))
                    {
                        existing.Dispose();
                    }
                    overrideBitmaps[cacheKey] = (Bitmap)upload.Clone();
                }

                GL.BindTexture(TextureTarget.Texture2D, textureId);
                var internalFormat = IsColorTexture(Name) ? PixelInternalFormat.Srgb8Alpha8 : PixelInternalFormat.Rgba8;
                var data = upload.LockBits(new Rectangle(0, 0, upload.Width, upload.Height), ImageLockMode.ReadOnly, upload.PixelFormat);
                try
                {
                    GL.TexImage2D(TextureTarget.Texture2D, 0, internalFormat, data.Width, data.Height, 0, OpenTK.Graphics.OpenGL4.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);
                }
                finally
                {
                    upload.UnlockBits(data);
                    upload.Dispose();
                }

                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)wrapS);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)wrapT);
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        public bool TryGetEditedBitmap(out Bitmap bitmap)
        {
            if (!OperatingSystem.IsWindows())
            {
                bitmap = null!;
                return false;
            }

            return TryGetEditedBitmapWindows(out bitmap);
        }

        [SupportedOSPlatform("windows")]
        private bool TryGetEditedBitmapWindows(out Bitmap bitmap)
        {
            lock (cacheLock)
            {
                if (overrideBitmaps.TryGetValue(cacheKey, out var bmp))
                {
                    bitmap = (Bitmap)bmp.Clone();
                    return true;
                }
            }

            bitmap = null!;
            return false;
        }

        private static bool IsColorTexture(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return true;
            }

            // Treat base/albedo and other color-bearing maps as sRGB, keep masks/normal/packed maps linear.
            // This greatly affects perceived color matching vs external reference renders.
            if (name.EndsWith("MaskMap", StringComparison.OrdinalIgnoreCase) ||
                name.EndsWith("Mask", StringComparison.OrdinalIgnoreCase) ||
                name.EndsWith("NormalMap", StringComparison.OrdinalIgnoreCase) ||
                name.EndsWith("AOMap", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("OcclusionMap", StringComparison.OrdinalIgnoreCase) ||
                name.EndsWith("RoughnessMap", StringComparison.OrdinalIgnoreCase) ||
                name.EndsWith("MetallicMap", StringComparison.OrdinalIgnoreCase) ||
                name.EndsWith("ParallaxMap", StringComparison.OrdinalIgnoreCase) ||
                name.EndsWith("LayerMaskMap", StringComparison.OrdinalIgnoreCase) ||
                name.EndsWith("DetailMaskMap", StringComparison.OrdinalIgnoreCase) ||
                name.EndsWith("SSSMaskMap", StringComparison.OrdinalIgnoreCase) ||
                name.EndsWith("HairFlowMap", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return true;
        }

        public Bitmap? LoadPreviewBitmap()
        {
            if (!OperatingSystem.IsWindows())
            {
                return null;
            }

            try
            {
                lock (cacheLock)
                {
                    if (overrideBitmaps.TryGetValue(cacheKey, out var bmp))
                    {
                        return (Bitmap)bmp.Clone();
                    }
                }

                foreach (var candidatePath in EnumerateCandidateTexturePaths())
                {
                    if (File.Exists(candidatePath) || File.Exists(diskTexturePath) || File.Exists(diskAltTexturePath))
                    {
                        var diskPath =
                            File.Exists(diskTexturePath) ? diskTexturePath :
                            (File.Exists(diskAltTexturePath) ? diskAltTexturePath :
                            candidatePath);

                        if (BNTX.TryLoadFromFile(diskPath, preferredName, out var img, out var error))
                        {
                            return img;
                        }

                        if (!string.IsNullOrEmpty(error) && MessageHandler.Instance.DebugLogsEnabled)
                        {
                            MessageHandler.Instance.AddMessage(MessageType.WARNING, $"Failed to decode texture preview '{SourceFile}': {error}");
                        }
                    }
                    else if (assetProvider != null && assetProvider.Exists(candidatePath))
                    {
                        using var s = assetProvider.OpenRead(candidatePath);
                        if (BNTX.TryLoadFromStream(s, preferredName, out var img, out var error))
                        {
                            return img;
                        }

                        if (!string.IsNullOrEmpty(error) && MessageHandler.Instance.DebugLogsEnabled)
                        {
                            MessageHandler.Instance.AddMessage(MessageType.WARNING, $"Failed to decode texture preview '{SourceFile}': {error}");
                        }
                    }
                }
            }
            catch
            {
                return null;
            }

            return null;
        }

        public bool TryGetResolvedSourcePath(out string path)
        {
            foreach (var candidatePath in EnumerateCandidateTexturePaths())
            {
                if (File.Exists(candidatePath))
                {
                    path = candidatePath;
                    return true;
                }

                if (assetProvider != null && assetProvider.Exists(candidatePath))
                {
                    path = candidatePath;
                    return true;
                }
            }

            path = diskTexturePath;
            return false;
        }

        public void Dispose()
        {
            tex?.Dispose();
            tex = null;
            if (textureId <= 0)
            {
                return;
            }

            lock (cacheLock)
            {
                if (!cache.TryGetValue(cacheKey, out var cached))
                {
                    GL.DeleteTexture(textureId);
                    textureId = 0;
                    return;
                }

                cached.RefCount--;
                if (cached.RefCount <= 0)
                {
                    GL.DeleteTexture(cached.TextureId);
                    cache.Remove(cacheKey);
                }
            }
            textureId = 0;
        }

        private static TextureWrapMode ConvertWrapMode(UVWrapMode? mode)
        {
            if (mode == null)
            {
                return TextureWrapMode.ClampToEdge;
            }

            // OpenGL "mirror once" wrap mode. OpenTK doesn't always expose this enum value depending on version,
            // so we use the numeric constant directly (GL_MIRROR_CLAMP_TO_EDGE = 0x8743).
            const TextureWrapMode mirrorClampToEdge = (TextureWrapMode)0x8743;

            return mode.Value switch
            {
                UVWrapMode.WRAP => TextureWrapMode.Repeat,
                UVWrapMode.CLAMP => TextureWrapMode.ClampToEdge,
                UVWrapMode.MIRROR => TextureWrapMode.MirroredRepeat,
                UVWrapMode.MIRROR_ONCE => mirrorClampToEdge,
                _ => TextureWrapMode.ClampToEdge
            };
        }
    }
}
