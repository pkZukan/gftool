using GFTool.Renderer.Scene.GraphicsObjects;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;

namespace TrinityModelViewer.Export
{
    internal static partial class GltfExporter
    {
        private static Dictionary<string, int> ExportAllTextures(GltfRoot gltf, string texDir, IReadOnlyList<Material> materials)
        {
            var cache = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var mat in materials)
            {
                if (mat == null) continue;
                foreach (var tex in mat.Textures)
                {
                    if (tex == null) continue;
                    var key = GetTextureKey(tex);
                    if (cache.ContainsKey(key)) continue;

                    using var bmp = tex.LoadPreviewBitmap();
                    if (bmp == null) continue;

                    string outName = MakeUniqueTextureFileName(usedNames, tex);
                    string outPath = Path.Combine(texDir, outName);
                    bmp.Save(outPath, ImageFormat.Png);

                    int imgIndex = gltf.Images.Count;
                    gltf.Images.Add(new GltfImage { Uri = $"{Path.GetFileName(texDir)}/{outName}" });
                    int texIndex = gltf.Textures.Count;
                    gltf.Textures.Add(new GltfTexture { Sampler = 0, Source = imgIndex, Name = tex.Name });

                    cache[key] = texIndex;
                }
            }

            return cache;
        }

        private static string GetTextureKey(Texture tex)
        {
            return $"{tex.Name}|{tex.SourceFile}";
        }

        private static string MakeUniqueTextureFileName(HashSet<string> usedNames, Texture tex)
        {
            string src = tex.SourceFile ?? string.Empty;
            string baseName = Path.GetFileNameWithoutExtension(src);
            if (string.IsNullOrWhiteSpace(baseName))
            {
                baseName = tex.Name;
            }

            string fileName = $"{baseName}.png";
            fileName = SanitizeFileName(fileName);
            if (usedNames.Add(fileName))
            {
                return fileName;
            }

            for (int i = 2; i < 10000; i++)
            {
                string candidate = SanitizeFileName($"{baseName}_{i}.png");
                if (usedNames.Add(candidate))
                {
                    return candidate;
                }
            }

            // Extremely unlikely.
            return SanitizeFileName($"{baseName}_{Guid.NewGuid():N}.png");
        }

        private static string SanitizeFileName(string name)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(c, '_');
            }
            return name;
        }
    }
}
