using GFTool.Renderer.Core;
using GFTool.Renderer.Scene.GraphicsObjects;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TrinityModelViewer.Export
{
    internal static partial class GltfExporter
    {
        public static void ExportModel(Model model, string gltfPath)
        {
            ExportModel(model, gltfPath, animations: null);
        }

        public static void ExportModel(Model model, string gltfPath, IReadOnlyList<GFTool.Renderer.Scene.GraphicsObjects.Animation>? animations)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));
            if (string.IsNullOrWhiteSpace(gltfPath)) throw new ArgumentException("Missing output path.", nameof(gltfPath));

            var data = model.CreateExportData();
            if (data.Submeshes.Count == 0) throw new InvalidOperationException("Model has no meshes to export.");

            var outDir = Path.GetDirectoryName(gltfPath) ?? Environment.CurrentDirectory;
            Directory.CreateDirectory(outDir);
            var baseName = Path.GetFileNameWithoutExtension(gltfPath);
            var binName = $"{baseName}.bin";
            var binPath = Path.Combine(outDir, binName);
            var texDir = Path.Combine(outDir, $"{baseName}_textures");
            Directory.CreateDirectory(texDir);

            var materialByName = data.Materials
                .Where(m => m != null && !string.IsNullOrWhiteSpace(m.Name))
                .GroupBy(m => m.Name, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            var buffer = new BinaryBufferBuilder();
            var gltf = new GltfRoot();
            gltf.Asset = new GltfAsset { Version = "2.0", Generator = "TrinityModelViewer" };

            gltf.Samplers.Add(new GltfSampler
            {
                MagFilter = 9729,
                MinFilter = 9729,
                WrapS = 33071,
                WrapT = 33071
            });

            int sceneIndex = 0;
            gltf.Scene = sceneIndex;
            var scene = new GltfScene();
            gltf.Scenes.Add(scene);

            int rootNodeIndex = gltf.Nodes.Count;
            gltf.Nodes.Add(new GltfNode { Name = data.Name, Children = new List<int>() });
            scene.Nodes.Add(rootNodeIndex);

            int? skinIndex = null;
            int[]? boneNodeIndices = null;
            if (data.Armature != null && data.Armature.Bones.Count > 0)
            {
                (skinIndex, boneNodeIndices) = AddSkin(gltf, buffer, data.Armature, rootNodeIndex);
            }

            var gltfMaterialIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var texCache = ExportAllTextures(gltf, texDir, data.Materials);

            // Group Trinity submeshes by mesh shape so Blender sees one mesh object per mesh shape
            // (with multiple primitives/material slots), instead of one mesh object per material part.
            // This keeps the vertex pool shared per shape, matching TRMSH/TRMBF.
            int shapeIndex = 0;
            foreach (var shapeGroup in data.Submeshes
                         .Where(s => s.Positions.Length > 0 && s.Indices.Length > 0)
                         .GroupBy(s => GetShapeName(s.Name), StringComparer.OrdinalIgnoreCase))
            {
                var submeshes = shapeGroup.ToList();
                if (submeshes.Count == 0)
                {
                    continue;
                }

                // Choose a canonical submesh to source the shared vertex streams.
                // (All parts in a Trinity mesh shape share the same vertex pool; indices differ.)
                bool anySkinning = submeshes.Any(s => s.HasSkinning);
                bool anyTangents = submeshes.Any(s => s.HasTangents);
                var canonical =
                    submeshes.FirstOrDefault(s => (!anySkinning || s.HasSkinning) && (!anyTangents || s.HasTangents)) ??
                    submeshes.FirstOrDefault(s => !anySkinning || s.HasSkinning) ??
                    submeshes.FirstOrDefault(s => !anyTangents || s.HasTangents) ??
                    submeshes[0];

                int meshIndex = AddMeshShape(gltf, buffer, canonical, submeshes, shapeIndex, gltfMaterialIndex, materialByName, texCache, texDir);

                var node = new GltfNode { Name = shapeGroup.Key, Mesh = meshIndex };
                if (skinIndex.HasValue && anySkinning)
                {
                    node.Skin = skinIndex.Value;
                }
                int nodeIndex = gltf.Nodes.Count;
                gltf.Nodes.Add(node);
                gltf.Nodes[rootNodeIndex].Children!.Add(nodeIndex);

                shapeIndex++;
            }

            if (animations != null && animations.Count > 0 && data.Armature != null && boneNodeIndices != null)
            {
                AddAnimations(gltf, buffer, data.Armature, boneNodeIndices, animations);
            }

            var binBytes = buffer.ToArray();
            File.WriteAllBytes(binPath, binBytes);
            gltf.Buffers.Add(new GltfBuffer { Uri = binName, ByteLength = binBytes.Length });

            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
            var json = JsonSerializer.Serialize(gltf, jsonOptions);
            File.WriteAllText(gltfPath, json);
        }
    }
}
