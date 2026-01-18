using GFTool.Renderer.Core;
using GFTool.Renderer.Scene.GraphicsObjects;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TrinityModelViewer.Export
{
    internal static partial class GltfExporter
    {
        private static string GetShapeName(string submeshName)
        {
            if (string.IsNullOrWhiteSpace(submeshName))
            {
                return "Mesh";
            }

            int colon = submeshName.IndexOf(':');
            if (colon > 0)
            {
                return submeshName.Substring(0, colon);
            }

            return submeshName;
        }

        private static int AddMeshShape(
            GltfRoot gltf,
            BinaryBufferBuilder buffer,
            Model.ExportSubmesh canonical,
            IReadOnlyList<Model.ExportSubmesh> parts,
            int shapeIndex,
            Dictionary<string, int> gltfMaterialIndex,
            Dictionary<string, Material> materialByName,
            Dictionary<string, int> textureCache,
            string texDir)
        {
            int vertexCount = canonical.Positions.Length;

            var positions = canonical.Positions;
            var normals = canonical.Normals.Length == vertexCount
                ? canonical.Normals
                : Enumerable.Repeat(Vector3.UnitY, vertexCount).ToArray();
            var uvs = canonical.UVs.Length == vertexCount
                ? canonical.UVs.Select(uv => new Vector2(uv.X, 1f - uv.Y)).ToArray()
                : Enumerable.Repeat(Vector2.Zero, vertexCount).ToArray();

            bool hasTangents = parts.Any(p => p.HasTangents) && canonical.Tangents.Length == vertexCount;
            var tangents = hasTangents ? canonical.Tangents : Array.Empty<Vector4>();

            bool hasSkinning = parts.Any(p => p.HasSkinning);
            var joints = canonical.BlendIndices.Length == vertexCount
                ? canonical.BlendIndices
                : Enumerable.Repeat(Vector4.Zero, vertexCount).ToArray();
            var weights = canonical.BlendWeights.Length == vertexCount
                ? canonical.BlendWeights
                : Enumerable.Repeat(new Vector4(1, 0, 0, 0), vertexCount).ToArray();

            int posAcc = AddAccessorVec3(gltf, buffer, positions, target: 34962, includeMinMax: true);
            int nrmAcc = AddAccessorVec3(gltf, buffer, normals, target: 34962);
            int uvAcc = AddAccessorVec2(gltf, buffer, uvs, target: 34962);

            int? tanAcc = null;
            if (hasTangents)
            {
                tanAcc = AddAccessorVec4(gltf, buffer, tangents, target: 34962);
            }

            int? jointAcc = null;
            int? weightAcc = null;
            if (hasSkinning)
            {
                jointAcc = AddAccessorUShort4(gltf, buffer, joints, target: 34962);
                weightAcc = AddAccessorVec4(gltf, buffer, weights, target: 34962);
            }

            var gltfMesh = new GltfMesh
            {
                Name = GetShapeName(canonical.Name),
                Primitives = new List<GltfPrimitive>()
            };

            for (int partIndex = 0; partIndex < parts.Count; partIndex++)
            {
                var part = parts[partIndex];
                int materialIndex = GetOrCreateMaterial(gltf, gltfMaterialIndex, materialByName, textureCache, part.MaterialName, texDir);
                int idxAcc = AddAccessorIndices(gltf, buffer, part.Indices);

                var prim = new GltfPrimitive
                {
                    Attributes = new Dictionary<string, int>
                    {
                        ["POSITION"] = posAcc,
                        ["NORMAL"] = nrmAcc,
                        ["TEXCOORD_0"] = uvAcc
                    },
                    Indices = idxAcc,
                    Material = materialIndex,
                    Extras = new Dictionary<string, object>
                    {
                        ["trinity"] = new Dictionary<string, object>
                        {
                            ["shapeIndex"] = shapeIndex,
                            ["partIndex"] = partIndex,
                            ["templateSubmeshName"] = part.Name,
                            ["templateMaterialName"] = part.MaterialName
                        }
                    }
                };

                if (tanAcc.HasValue)
                {
                    prim.Attributes["TANGENT"] = tanAcc.Value;
                }

                if (jointAcc.HasValue && weightAcc.HasValue)
                {
                    prim.Attributes["JOINTS_0"] = jointAcc.Value;
                    prim.Attributes["WEIGHTS_0"] = weightAcc.Value;
                }

                gltfMesh.Primitives!.Add(prim);
            }

            int meshIndex = gltf.Meshes.Count;
            gltf.Meshes.Add(gltfMesh);
            return meshIndex;
        }

        private static int AddMesh(GltfRoot gltf, BinaryBufferBuilder buffer, Model.ExportSubmesh sub, int materialIndex)
        {
            int vertexCount = sub.Positions.Length;

            var positions = sub.Positions;
            var normals = sub.Normals.Length == vertexCount ? sub.Normals : Enumerable.Repeat(Vector3.UnitY, vertexCount).ToArray();
            var uvs = sub.UVs.Length == vertexCount
                ? sub.UVs.Select(uv => new Vector2(uv.X, 1f - uv.Y)).ToArray()
                : Enumerable.Repeat(Vector2.Zero, vertexCount).ToArray();
            var tangents = sub.Tangents.Length == vertexCount ? sub.Tangents : Array.Empty<Vector4>();
            var joints = sub.BlendIndices.Length == vertexCount ? sub.BlendIndices : Enumerable.Repeat(Vector4.Zero, vertexCount).ToArray();
            var weights = sub.BlendWeights.Length == vertexCount ? sub.BlendWeights : Enumerable.Repeat(new Vector4(1, 0, 0, 0), vertexCount).ToArray();

            int posAcc = AddAccessorVec3(gltf, buffer, positions, target: 34962, includeMinMax: true);
            int nrmAcc = AddAccessorVec3(gltf, buffer, normals, target: 34962);
            int uvAcc = AddAccessorVec2(gltf, buffer, uvs, target: 34962);

            int? tanAcc = null;
            if (sub.HasTangents && tangents.Length == vertexCount)
            {
                tanAcc = AddAccessorVec4(gltf, buffer, tangents, target: 34962);
            }

            int? jointAcc = null;
            int? weightAcc = null;
            if (sub.HasSkinning)
            {
                jointAcc = AddAccessorUShort4(gltf, buffer, joints, target: 34962);
                weightAcc = AddAccessorVec4(gltf, buffer, weights, target: 34962);
            }

            int idxAcc = AddAccessorIndices(gltf, buffer, sub.Indices);

            var prim = new GltfPrimitive
            {
                Attributes = new Dictionary<string, int>
                {
                    ["POSITION"] = posAcc,
                    ["NORMAL"] = nrmAcc,
                    ["TEXCOORD_0"] = uvAcc
                },
                Indices = idxAcc,
                Material = materialIndex
            };

            if (tanAcc.HasValue)
            {
                prim.Attributes["TANGENT"] = tanAcc.Value;
            }

            if (jointAcc.HasValue && weightAcc.HasValue)
            {
                prim.Attributes["JOINTS_0"] = jointAcc.Value;
                prim.Attributes["WEIGHTS_0"] = weightAcc.Value;
            }

            var mesh = new GltfMesh
            {
                Name = sub.Name,
                Primitives = new List<GltfPrimitive> { prim }
            };
            int meshIndex = gltf.Meshes.Count;
            gltf.Meshes.Add(mesh);
            return meshIndex;
        }
    }
}
