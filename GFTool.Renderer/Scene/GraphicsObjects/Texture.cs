using BnTxx;
using GFTool.Core.Utils;
using GFTool.Renderer.Core;
using OpenTK.Graphics.OpenGL4;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Trinity.Core.Flatbuffers.TR.Model;

namespace GFTool.Renderer.Scene.GraphicsObjects
{
    public class Texture : IDisposable
    {
        private class CachedTexture
        {
            public int TextureId;
            public int RefCount;
            public int Width;
            public int Height;
        }

        private static readonly Dictionary<string, CachedTexture> cache = new Dictionary<string, CachedTexture>(StringComparer.OrdinalIgnoreCase);
        private static readonly object cacheLock = new object();

        public string Name { get; private set; }
        public string SourceFile { get; private set; }
        public uint Slot { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }
        public Bitmap tex { get; private set; }
        public int textureId { get; private set; }

        private readonly string cacheKey;
        private readonly string texturePath;
        private readonly string altTexturePath;
        private readonly List<string> candidateTexturePaths;
        private readonly string preferredName;
        private bool cacheReferenceAcquired;

        public static int CacheEntryCount
        {
            get
            {
                lock (cacheLock)
                {
                    return cache.Count;
                }
            }
        }

        public Texture(PathString modelPath, TRTexture img)
            : this(modelPath, img.Name, img.File, img.Slot)
        {
        }

        public Texture(PathString modelPath, string name, string file, uint slot)
        {
            Name = name;
            SourceFile = file;
            Slot = slot;
            string texturePath;
            try
            {
                texturePath = Path.GetFullPath(modelPath.Combine(file));
            }
            catch
            {
                texturePath = modelPath.Combine(file);
            }
            var preferredName = Path.GetFileNameWithoutExtension(file);
            this.texturePath = texturePath;
            try
            {
                altTexturePath = Path.GetFullPath(modelPath.Combine(Path.GetFileName(file)));
            }
            catch
            {
                altTexturePath = modelPath.Combine(Path.GetFileName(file));
            }
            this.preferredName = preferredName;
            candidateTexturePaths = BuildTextureCandidates(modelPath, file, texturePath, altTexturePath);
            var keyPath = candidateTexturePaths.FirstOrDefault(File.Exists) ?? texturePath;
            cacheKey = $"{keyPath}|{preferredName}";
            DiagnosticLog.Write($"Texture reference: name={Name}, file={SourceFile}, slot={Slot}, preferredName={preferredName}, candidates={DescribeCandidates(candidateTexturePaths)}");

            lock (cacheLock)
            {
                if (cache.TryGetValue(cacheKey, out var cached))
                {
                    cached.RefCount++;
                    textureId = cached.TextureId;
                    Width = cached.Width;
                    Height = cached.Height;
                    cacheReferenceAcquired = true;
                    DiagnosticLog.Write($"Texture cache hit: name={Name}, textureId={textureId}, refCount={cached.RefCount}");
                    return;
                }
            }
        }

        public void EnsureLoaded()
        {
            if (textureId > 0)
            {
                return;
            }

            lock (cacheLock)
            {
                if (cache.TryGetValue(cacheKey, out var cached))
                {
                    cached.RefCount++;
                    textureId = cached.TextureId;
                    Width = cached.Width;
                    Height = cached.Height;
                    cacheReferenceAcquired = true;
                    DiagnosticLog.Write($"Texture cache late hit: name={Name}, textureId={textureId}, refCount={cached.RefCount}");
                    return;
                }
            }

            try
            {
                foreach (var candidate in candidateTexturePaths)
                {
                    if (!File.Exists(candidate))
                    {
                        continue;
                    }

                    DiagnosticLog.Write($"Texture load attempt: name={Name}, path={candidate}, preferredName={preferredName}");
                    tex = BNTX.LoadFromFile(candidate, preferredName);
                    if (tex != null)
                    {
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                tex = null;
                DiagnosticLog.WriteException($"Texture decode failed: name={Name}, file={SourceFile}", ex);
            }

            if (tex == null)
            {
                tex = new Bitmap(32, 32);
                MessageHandler.Instance.AddMessage(MessageType.WARNING, string.Format("Failed to load texture: {0}", SourceFile));
                DiagnosticLog.Write($"Texture fallback created: name={Name}, file={SourceFile}, size=32x32");
            }
            else
            {
                DiagnosticLog.Write($"Texture decoded: name={Name}, file={SourceFile}, size={tex.Width}x{tex.Height}, pixelFormat={tex.PixelFormat}");
                LogFireTextureStats(tex);
            }

            Width = tex.Width;
            Height = tex.Height;

            int id = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, id);

            BitmapData bitmapData = tex.LockBits(new Rectangle(0, 0, tex.Width, tex.Height), ImageLockMode.ReadOnly, tex.PixelFormat);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, bitmapData.Width, bitmapData.Height, 0, OpenTK.Graphics.OpenGL4.PixelFormat.Bgra, PixelType.UnsignedByte, bitmapData.Scan0);
            tex.UnlockBits(bitmapData);

            // Trinity models can intentionally use tiled UV ranges outside 0..1.
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

            tex.Dispose();
            tex = null;

            lock (cacheLock)
            {
                cache[cacheKey] = new CachedTexture
                {
                    TextureId = id,
                    RefCount = 1,
                    Width = Width,
                    Height = Height
                };
                cacheReferenceAcquired = true;
            }

            textureId = id;
            DiagnosticLog.Write($"Texture uploaded: name={Name}, textureId={textureId}, wrap=Repeat/Repeat, minMag=Linear");
        }

        public Bitmap? LoadPreviewBitmap()
        {
            try
            {
                foreach (var candidate in candidateTexturePaths)
                {
                    if (!File.Exists(candidate))
                    {
                        continue;
                    }

                    var bmp = BNTX.LoadFromFile(candidate, preferredName);
                    DiagnosticLog.Write($"Texture preview decoded: name={Name}, path={candidate}, size={bmp.Width}x{bmp.Height}, pixelFormat={bmp.PixelFormat}");
                    return bmp;
                }
            }
            catch (Exception ex)
            {
                DiagnosticLog.WriteException($"Texture preview decode failed: name={Name}, file={SourceFile}", ex);
                return null;
            }

            DiagnosticLog.Write($"Texture preview missing: name={Name}, file={SourceFile}, candidates={DescribeCandidates(candidateTexturePaths)}");
            return null;
        }

        public bool TryGetResolvedSourcePath(out string path)
        {
            foreach (var candidate in candidateTexturePaths)
            {
                if (File.Exists(candidate))
                {
                    path = candidate;
                    return true;
                }
            }

            path = texturePath;
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
                if (!cacheReferenceAcquired ||
                    !cache.TryGetValue(cacheKey, out var cached) ||
                    cached.TextureId != textureId)
                {
                    GL.DeleteTexture(textureId);
                    textureId = 0;
                    cacheReferenceAcquired = false;
                    return;
                }

                cached.RefCount--;
                if (cached.RefCount <= 0)
                {
                    GL.DeleteTexture(cached.TextureId);
                    cache.Remove(cacheKey);
                }
                DiagnosticLog.Write($"Texture cache release: name={Name}, textureId={textureId}, refCount={Math.Max(0, cached.RefCount)}, entries={cache.Count}");
            }
            textureId = 0;
            cacheReferenceAcquired = false;
        }

        public static void ClearCache()
        {
            lock (cacheLock)
            {
                int released = cache.Count;
                foreach (var cached in cache.Values)
                {
                    if (cached.TextureId != 0)
                    {
                        GL.DeleteTexture(cached.TextureId);
                    }
                }
                cache.Clear();
                DiagnosticLog.Write($"Texture cache cleared: releasedEntries={released}");
            }
        }

        private static string DescribeFile(string path)
        {
            try
            {
                var info = new FileInfo(path);
                return info.Exists ? $"exists bytes={info.Length}" : "missing";
            }
            catch (Exception ex)
            {
                return $"unavailable {ex.Message}";
            }
        }

        private static List<string> BuildTextureCandidates(PathString modelPath, string file, string primaryPath, string localFilePath)
        {
            var result = new List<string>();
            AddCandidate(result, primaryPath);
            AddCandidate(result, localFilePath);

            string rootPath;
            try
            {
                rootPath = Path.GetFullPath(modelPath.Combine("."));
            }
            catch
            {
                rootPath = Directory.GetCurrentDirectory();
            }

            var shareRelative = GetShareRelativePath(file);
            if (!string.IsNullOrWhiteSpace(shareRelative))
            {
                foreach (var root in EnumerateSearchRoots(rootPath))
                {
                    AddCandidate(result, Path.Combine(root, shareRelative));
                }
            }

            return result;
        }

        private static IEnumerable<string> EnumerateSearchRoots(string startPath)
        {
            foreach (var path in EnumerateAncestors(startPath))
            {
                yield return path;
            }

            foreach (var path in EnumerateAncestors(Directory.GetCurrentDirectory()))
            {
                yield return path;
            }

            foreach (var path in EnumerateAncestors(AppContext.BaseDirectory))
            {
                yield return path;
            }
        }

        private static IEnumerable<string> EnumerateAncestors(string startPath)
        {
            string? current = startPath;
            while (!string.IsNullOrWhiteSpace(current))
            {
                string full;
                try
                {
                    full = Path.GetFullPath(current);
                }
                catch
                {
                    yield break;
                }

                yield return full;

                var parent = Directory.GetParent(full);
                if (parent == null || string.Equals(parent.FullName, full, StringComparison.OrdinalIgnoreCase))
                {
                    yield break;
                }

                current = parent.FullName;
            }
        }

        private static string? GetShareRelativePath(string file)
        {
            if (string.IsNullOrWhiteSpace(file))
            {
                return null;
            }

            var normalized = file.Replace('\\', '/');
            const string marker = "/share/";
            var index = normalized.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (index >= 0)
            {
                return normalized.Substring(index + 1).Replace('/', Path.DirectorySeparatorChar);
            }

            if (normalized.StartsWith("share/", StringComparison.OrdinalIgnoreCase))
            {
                return normalized.Replace('/', Path.DirectorySeparatorChar);
            }

            return null;
        }

        private static void AddCandidate(List<string> paths, string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            string full;
            try
            {
                full = Path.GetFullPath(path);
            }
            catch
            {
                full = path;
            }

            if (!paths.Any(existing => string.Equals(existing, full, StringComparison.OrdinalIgnoreCase)))
            {
                paths.Add(full);
            }
        }

        private static string DescribeCandidates(IEnumerable<string> paths)
        {
            return string.Join(" | ", paths.Select(path => $"{path} [{DescribeFile(path)}]"));
        }

        private void LogFireTextureStats(Bitmap bitmap)
        {
            if (!Name.Contains("fire", StringComparison.OrdinalIgnoreCase) &&
                !SourceFile.Contains("fire", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            int[] min = { 255, 255, 255, 255 };
            int[] max = { 0, 0, 0, 0 };
            long[] sum = { 0, 0, 0, 0 };
            long count = 0;
            int stepX = Math.Max(1, bitmap.Width / 64);
            int stepY = Math.Max(1, bitmap.Height / 64);

            for (int y = 0; y < bitmap.Height; y += stepY)
            {
                for (int x = 0; x < bitmap.Width; x += stepX)
                {
                    var pixel = bitmap.GetPixel(x, y);
                    int[] rgba = { pixel.R, pixel.G, pixel.B, pixel.A };
                    for (int i = 0; i < rgba.Length; i++)
                    {
                        min[i] = Math.Min(min[i], rgba[i]);
                        max[i] = Math.Max(max[i], rgba[i]);
                        sum[i] += rgba[i];
                    }
                    count++;
                }
            }

            if (count == 0)
            {
                return;
            }

            string avg = string.Join(",", sum.Select(v => Math.Round(v / (double)count, 2)));
            DiagnosticLog.Write(
                $"Fire texture stats: name={Name}, file={SourceFile}, " +
                $"minRGBA={string.Join(",", min)}, maxRGBA={string.Join(",", max)}, avgRGBA={avg}");
        }
    }
}
