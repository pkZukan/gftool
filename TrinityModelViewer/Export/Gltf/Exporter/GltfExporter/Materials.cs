using GFTool.Renderer.Scene.GraphicsObjects;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace TrinityModelViewer.Export
{
    internal static partial class GltfExporter
    {
        private static int GetOrCreateMaterial(
            GltfRoot gltf,
            Dictionary<string, int> gltfMaterialIndex,
            Dictionary<string, Material> materialByName,
            Dictionary<string, int> textureCache,
            string materialName,
            string texDir)
        {
            materialName ??= string.Empty;
            if (gltfMaterialIndex.TryGetValue(materialName, out int existing))
            {
                return existing;
            }

            materialByName.TryGetValue(materialName, out var mat);
            var texByName = mat?.Textures?.ToDictionary(t => t.Name, StringComparer.OrdinalIgnoreCase) ?? new Dictionary<string, Texture>(StringComparer.OrdinalIgnoreCase);

            int? baseColorTex = TryGetTextureIndex(textureCache, texByName, "BaseColorMap");
            int? normalTex = TryGetTextureIndex(textureCache, texByName, "NormalMap");
            int? aoTex = TryGetTextureIndex(textureCache, texByName, "AOMap");

            int? mrTex = TryAddMetallicRoughnessTexture(gltf, texDir, texByName);

            var pbr = new GltfPbrMetallicRoughness();
            if (baseColorTex.HasValue)
            {
                pbr.BaseColorTexture = new GltfTextureInfo { Index = baseColorTex.Value };
            }
            pbr.BaseColorFactor = new[] { 1f, 1f, 1f, 1f };
            pbr.MetallicFactor = 1f;
            pbr.RoughnessFactor = 1f;
            if (mrTex.HasValue)
            {
                pbr.MetallicRoughnessTexture = new GltfTextureInfo { Index = mrTex.Value };
            }

            var gltfMat = new GltfMaterial
            {
                Name = string.IsNullOrWhiteSpace(materialName) ? "Material" : materialName,
                PbrMetallicRoughness = pbr,
                AlphaMode = mat?.IsTransparent == true ? "BLEND" : null,
                DoubleSided = true
            };

            if (normalTex.HasValue)
            {
                gltfMat.NormalTexture = new GltfNormalTextureInfo { Index = normalTex.Value, Scale = 1f };
            }

            if (aoTex.HasValue)
            {
                gltfMat.OcclusionTexture = new GltfOcclusionTextureInfo { Index = aoTex.Value, Strength = 1f };
            }

            int gltfIndex = gltf.Materials.Count;
            gltf.Materials.Add(gltfMat);
            gltfMaterialIndex[materialName] = gltfIndex;
            return gltfIndex;
        }

        private static int? TryGetTextureIndex(Dictionary<string, int> textureCache, Dictionary<string, Texture> texByName, string textureName)
        {
            if (!texByName.TryGetValue(textureName, out var tex) || tex == null)
            {
                return null;
            }

            if (textureCache.TryGetValue(GetTextureKey(tex), out var idx))
            {
                return idx;
            }

            return null;
        }

        private static int? TryAddMetallicRoughnessTexture(GltfRoot gltf, string texDir, Dictionary<string, Texture> texByName)
        {
            texByName.TryGetValue("RoughnessMap", out var roughTex);
            texByName.TryGetValue("MetallicMap", out var metalTex);
            if (roughTex == null && metalTex == null)
            {
                return null;
            }

            using var roughBmp = roughTex?.LoadPreviewBitmap();
            using var metalBmp = metalTex?.LoadPreviewBitmap();
            if (roughBmp == null && metalBmp == null) return null;

            int width = roughBmp?.Width ?? metalBmp!.Width;
            int height = roughBmp?.Height ?? metalBmp!.Height;

            using var outBmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    byte rough = 255;
                    byte metal = 0;
                    if (roughBmp != null)
                    {
                        var c = roughBmp.GetPixel(x * roughBmp.Width / width, y * roughBmp.Height / height);
                        rough = c.R;
                    }
                    if (metalBmp != null)
                    {
                        var c = metalBmp.GetPixel(x * metalBmp.Width / width, y * metalBmp.Height / height);
                        metal = c.R;
                    }
                    // glTF expects roughness in G and metallic in B.
                    outBmp.SetPixel(x, y, Color.FromArgb(255, 0, rough, metal));
                }
            }

            string fileName = "metallicRoughness.png";
            string outPath = Path.Combine(texDir, fileName);
            outBmp.Save(outPath, ImageFormat.Png);

            int imgIndex = gltf.Images.Count;
            gltf.Images.Add(new GltfImage { Uri = $"{Path.GetFileName(texDir)}/{fileName}" });
            int texIndex = gltf.Textures.Count;
            gltf.Textures.Add(new GltfTexture { Sampler = 0, Source = imgIndex, Name = "metallicRoughness" });
            return texIndex;
        }

        private static Bitmap FlipGreenChannel(Bitmap src)
        {
            var dst = new Bitmap(src.Width, src.Height, PixelFormat.Format32bppArgb);
            for (int y = 0; y < src.Height; y++)
            {
                for (int x = 0; x < src.Width; x++)
                {
                    var c = src.GetPixel(x, y);
                    dst.SetPixel(x, y, Color.FromArgb(c.A, c.R, 255 - c.G, c.B));
                }
            }
            return dst;
        }
    }
}
