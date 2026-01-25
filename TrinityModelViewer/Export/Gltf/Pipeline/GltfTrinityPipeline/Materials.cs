using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using OpenTK.Mathematics;
using Trinity.Core.Flatbuffers.TR.Model;
using Trinity.Core.Flatbuffers.Utils;
using Trinity.Core.Utils;


namespace TrinityModelViewer.Export
{
    internal static partial class GltfTrinityPipeline
    {
        private static TRMTR BuildTrinityMaterials(GltfReader.GltfDocument gltf, HashSet<string> materialNames, string texOutDir, string texDirName)
        {
            var nameToBaseColor = GltfReader.ExtractBaseColorTextures(gltf);
            var materials = new List<TRMaterial>();

            foreach (var matName in materialNames.OrderBy(s => s, StringComparer.OrdinalIgnoreCase))
            {
                nameToBaseColor.TryGetValue(matName, out var baseColorPath);
                if (!string.IsNullOrWhiteSpace(baseColorPath))
                {
                    // Best-effort: keep the glTF path; callers can opt into copying via PatchTrmtrBaseColorTextures.
                }

                materials.Add(new TRMaterial
                {
                    Name = matName,
                    Shader = new[]
                    {
                        new TRMaterialShader
                        {
                            Name = "Standard",
                            Values = new[]
                            {
                                new TRStringParameter { Name = "__TechniqueName", Value = "Opaque" },
                                new TRStringParameter { Name = "EnableBaseColorMap", Value = !string.IsNullOrWhiteSpace(baseColorPath) ? "True" : "False" }
                            }
                        }
                    },
                    Textures = string.IsNullOrWhiteSpace(baseColorPath)
                        ? Array.Empty<TRTexture>()
                        : new[]
                        {
                            new TRTexture { Name = "BaseColorMap", File = baseColorPath, Slot = 0 }
                        },
                    Samplers = new[]
                    {
                        new TRSampler
                        {
                            RepeatU = UVWrapMode.CLAMP,
                            RepeatV = UVWrapMode.CLAMP,
                            RepeatW = UVWrapMode.CLAMP,
                            BorderColor = new RGBA { R = 0f, G = 0f, B = 0f, A = 0f }
                        }
                    },
                    FloatParams = Array.Empty<TRFloatParameter>(),
                    Vec2fParams = Array.Empty<TRVec2fParameter>(),
                    Vec3fParams = Array.Empty<TRVec3fParameter>(),
                    Vec4fParams = Array.Empty<TRVec4fParameter>()
                });
            }

            if (materials.Count == 0)
            {
                materials.Add(new TRMaterial
                {
                    Name = "Material",
                    Shader = new[] { new TRMaterialShader { Name = "Standard", Values = Array.Empty<TRStringParameter>() } },
                    Textures = Array.Empty<TRTexture>(),
                    Samplers = Array.Empty<TRSampler>(),
                    FloatParams = Array.Empty<TRFloatParameter>(),
                    Vec2fParams = Array.Empty<TRVec2fParameter>(),
                    Vec3fParams = Array.Empty<TRVec3fParameter>(),
                    Vec4fParams = Array.Empty<TRVec4fParameter>()
                });
            }

            return new TRMTR
            {
                Field_00 = 0,
                Materials = materials.ToArray()
            };
        }

        private static void PatchTrmtrBaseColorTextures(
            TRMTR trmtr,
            Dictionary<string, string?> baseColorByMaterialName,
            GltfReader.GltfDocument gltf,
            string texOutDir,
            string texDirName)
        {
            if (trmtr?.Materials == null || trmtr.Materials.Length == 0 || baseColorByMaterialName.Count == 0)
            {
                return;
            }

            foreach (var mat in trmtr.Materials)
            {
                if (mat == null || string.IsNullOrWhiteSpace(mat.Name))
                {
                    continue;
                }

                if (!baseColorByMaterialName.TryGetValue(mat.Name, out var baseColorPath) || string.IsNullOrWhiteSpace(baseColorPath))
                {
                    continue;
                }

                TryCopyTextureToOutput(gltf, baseColorPath, texOutDir, texDirName, out var copiedRel);

                var textures = mat.Textures ?? Array.Empty<TRTexture>();
                var baseTex = textures.FirstOrDefault(t => t != null && string.Equals(t.Name, "BaseColorMap", StringComparison.OrdinalIgnoreCase));
                if (baseTex != null)
                {
                    baseTex.File = copiedRel;
                    continue;
                }

                var list = textures.Where(t => t != null).ToList();
                list.Add(new TRTexture { Name = "BaseColorMap", File = copiedRel, Slot = 0 });
                mat.Textures = list.ToArray();
            }
        }

        private static void TryCopyTextureToOutput(
            GltfReader.GltfDocument gltf,
            string texturePath,
            string texOutDir,
            string texDirName,
            out string relativePath)
        {
            relativePath = texturePath;

            if (string.IsNullOrWhiteSpace(texturePath))
            {
                return;
            }

            // Only copy if the referenced file exists on disk.
            var sourcePath = Path.IsPathRooted(texturePath)
                ? texturePath
                : Path.Combine(gltf.Directory, texturePath);

            if (!File.Exists(sourcePath))
            {
                return;
            }

            var fileName = Path.GetFileName(sourcePath);
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return;
            }

            Directory.CreateDirectory(texOutDir);
            var destPath = Path.Combine(texOutDir, fileName);
            File.Copy(sourcePath, destPath, overwrite: true);

            relativePath = $"{texDirName}/{fileName}";
        }
    }
}
