using System;
using System.Collections.Generic;
using System.IO;

namespace Trinity.Core.Assets
{
    /// <summary>
    /// Asset provider that reads from a primary "overlay" root first, then falls back to a secondary root.
    /// Intended for glTF preview exports where the temp folder contains generated TR* files but should reuse
    /// original textures/aux files from the source model directory.
    /// </summary>
    public sealed class OverlayDiskAssetProvider : IAssetProvider
    {
        private readonly string overlayRootFull;
        private readonly string fallbackRootFull;

        public OverlayDiskAssetProvider(string overlayRoot, string fallbackRoot)
        {
            if (string.IsNullOrWhiteSpace(overlayRoot)) throw new ArgumentException("Missing overlayRoot.", nameof(overlayRoot));
            if (string.IsNullOrWhiteSpace(fallbackRoot)) throw new ArgumentException("Missing fallbackRoot.", nameof(fallbackRoot));

            overlayRootFull = Path.GetFullPath(overlayRoot)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            fallbackRootFull = Path.GetFullPath(fallbackRoot)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        public string DisplayName => $"OverlayDisk(overlay='{overlayRootFull}', fallback='{fallbackRootFull}')";

        private string Resolve(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return path;
            }

            // Prefer the requested path if it exists (covers both overlay and fallback absolute paths).
            if (File.Exists(path))
            {
                return path;
            }

            // If the request points into the overlay root, try the same relative path under fallback.
            var full = Path.GetFullPath(path);
            if (full.StartsWith(overlayRootFull, StringComparison.OrdinalIgnoreCase))
            {
                var rel = full.Substring(overlayRootFull.Length)
                    .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                var mapped = Path.Combine(fallbackRootFull, rel);
                if (File.Exists(mapped))
                {
                    return mapped;
                }
            }

            return path;
        }

        public bool Exists(string path) => File.Exists(Resolve(path));

        public Stream OpenRead(string path) => File.OpenRead(Resolve(path));

        public byte[] ReadAllBytes(string path)
        {
            var resolved = Resolve(path);
            if (!string.IsNullOrWhiteSpace(resolved) && Directory.Exists(resolved))
            {
                throw new IOException($"Asset path resolved to a directory, not a file: '{resolved}'.");
            }
            return File.ReadAllBytes(resolved);
        }

        public IEnumerable<AssetEntry> EnumerateEntries()
        {
            yield break;
        }

        public void Dispose()
        {
        }
    }
}
