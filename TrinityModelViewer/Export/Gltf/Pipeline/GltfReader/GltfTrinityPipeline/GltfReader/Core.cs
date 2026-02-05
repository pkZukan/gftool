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
        private static partial class GltfReader
        {
            internal sealed class GltfDocument
            {
                public string Directory = string.Empty;
                public JsonDocument Json = null!;
                public byte[][] Buffers = Array.Empty<byte[]>();
            }

            public static GltfDocument Load(string path)
            {
                var dir = Path.GetDirectoryName(path) ?? Environment.CurrentDirectory;
                var json = JsonDocument.Parse(File.ReadAllText(path));
                var buffers = LoadBuffers(json.RootElement, dir);
                return new GltfDocument { Directory = dir, Json = json, Buffers = buffers };
            }

            public static List<TrinityPrimitive> ExtractMeshPrimitives(GltfDocument doc, Dictionary<string, int> boneNameToJointInfo)
            {
                var root = doc.Json.RootElement;
                var nodes = GetArray(root, "nodes");
                var meshes = GetArray(root, "meshes");
                var materials = GetArray(root, "materials");
                var skins = GetArray(root, "skins");

                var result = new List<TrinityPrimitive>();
                for (int nodeIndex = 0; nodeIndex < nodes.Count; nodeIndex++)
                {
                    var node = nodes[nodeIndex];
                    if (!TryGetInt(node, "mesh", out int meshIndex))
                    {
                        continue;
                    }

                    int? skinIndex = TryGetInt(node, "skin", out int s) ? s : null;
                    string nodeName = TryGetString(node, "name") ?? $"node_{nodeIndex}";

                    if (meshIndex < 0 || meshIndex >= meshes.Count)
                    {
                        continue;
                    }

                    var mesh = meshes[meshIndex];
                    var primitives = GetArray(mesh, "primitives");
                    for (int primIndex = 0; primIndex < primitives.Count; primIndex++)
                    {
                        var prim = primitives[primIndex];
                        var tri = ReadPrimitive(doc, prim, nodeName, primIndex, materials, skins, skinIndex, boneNameToJointInfo);
                        if (tri != null)
                        {
                            result.Add(tri);
                        }
                    }
                }

                return result;
            }

            public static Dictionary<string, string?> ExtractBaseColorTextures(GltfDocument doc)
            {
                var root = doc.Json.RootElement;
                var materials = GetArray(root, "materials");
                var textures = GetArray(root, "textures");
                var images = GetArray(root, "images");

                var map = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < materials.Count; i++)
                {
                    var mat = materials[i];
                    string name = TryGetString(mat, "name") ?? $"material_{i}";

                    if (!mat.TryGetProperty("pbrMetallicRoughness", out var pbr))
                    {
                        map[name] = null;
                        continue;
                    }

                    if (!pbr.TryGetProperty("baseColorTexture", out var bct) || !TryGetInt(bct, "index", out int texIndex))
                    {
                        map[name] = null;
                        continue;
                    }

                    if (texIndex < 0 || texIndex >= textures.Count)
                    {
                        map[name] = null;
                        continue;
                    }

                    var tex = textures[texIndex];
                    if (!TryGetInt(tex, "source", out int imgIndex) || imgIndex < 0 || imgIndex >= images.Count)
                    {
                        map[name] = null;
                        continue;
                    }

                    var img = images[imgIndex];
                    map[name] = TryGetString(img, "uri");
                }

                return map;
            }

            private static TrinityPrimitive? ReadPrimitive(
                GltfDocument doc,
                JsonElement prim,
                string nodeName,
                int primIndex,
                IReadOnlyList<JsonElement> materials,
                IReadOnlyList<JsonElement> skins,
                int? nodeSkinIndex,
                Dictionary<string, int> boneNameToJointInfo)
            {
                int? templateShapeIndex = null;
                int? templatePartIndex = null;
                if (prim.TryGetProperty("extras", out var extras) &&
                    extras.ValueKind == JsonValueKind.Object &&
                    extras.TryGetProperty("trinity", out var trinity) &&
                    trinity.ValueKind == JsonValueKind.Object)
                {
                    if (TryGetInt(trinity, "shapeIndex", out int shapeIndex) && shapeIndex >= 0)
                    {
                        templateShapeIndex = shapeIndex;
                    }
                    if (TryGetInt(trinity, "partIndex", out int partIndex) && partIndex >= 0)
                    {
                        templatePartIndex = partIndex;
                    }
                }

                if (!prim.TryGetProperty("attributes", out var attrs))
                {
                    return null;
                }

                if (!TryGetInt(attrs, "POSITION", out int posAccessor))
                {
                    return null;
                }

                var positions = ReadVec3(doc, posAccessor);
                if (positions.Length == 0)
                {
                    return null;
                }

                Vector3[] normals = Array.Empty<Vector3>();
                if (TryGetInt(attrs, "NORMAL", out int normAccessor))
                {
                    normals = ReadVec3(doc, normAccessor);
                }

                Vector4[] tangents = Array.Empty<Vector4>();
                if (TryGetInt(attrs, "TANGENT", out int tanAccessor))
                {
                    tangents = ReadVec4(doc, tanAccessor);
                }

                Vector2[] uv0 = Array.Empty<Vector2>();
                if (TryGetInt(attrs, "TEXCOORD_0", out int uvAccessor))
                {
                    uv0 = ReadVec2(doc, uvAccessor);
                    // `GltfExporter` flips V on export (uv.y = 1 - y). Undo it on import so round-trips
                    if (uv0.Length > 0)
                    {
                        for (int i = 0; i < uv0.Length; i++)
                        {
                            uv0[i] = new Vector2(uv0[i].X, 1f - uv0[i].Y);
                        }
                    }
                }

                uint[] indices;
                if (TryGetInt(prim, "indices", out int indexAccessor))
                {
                    indices = ReadIndices(doc, indexAccessor);
                }
                else
                {
                    // No index buffer: emit a trivial sequence.
                    indices = Enumerable.Range(0, positions.Length).Select(i => (uint)i).ToArray();
                }

                string materialName = "Material";
                if (TryGetInt(prim, "material", out int matIndex) && matIndex >= 0 && matIndex < materials.Count)
                {
                    materialName = TryGetString(materials[matIndex], "name") ?? $"material_{matIndex}";
                }

                Vector4[] colors = Array.Empty<Vector4>();
                if (TryGetInt(attrs, "COLOR_0", out int colorAccessor))
                {
                    colors = ReadColorVec4(doc, colorAccessor);
                }

                int jointsAccessor = -1;
                int weightsAccessor = -1;
                bool hasSkinning = nodeSkinIndex.HasValue &&
                                   TryGetInt(attrs, "JOINTS_0", out jointsAccessor) &&
                                   TryGetInt(attrs, "WEIGHTS_0", out weightsAccessor);

                Vector4i[] joints = Array.Empty<Vector4i>();
                Vector4[] weights = Array.Empty<Vector4>();
                if (hasSkinning)
                {
                    joints = ReadVec4UShort(doc, jointsAccessor);
                    weights = ReadWeights(doc, weightsAccessor);

                    if (joints.Length != positions.Length || weights.Length != positions.Length)
                    {
                        // Mismatched streams: drop skinning.
                        hasSkinning = false;
                        joints = Array.Empty<Vector4i>();
                        weights = Array.Empty<Vector4>();
                    }
                    else
                    {
                        MapJointsInPlace(doc, skins, nodeSkinIndex!.Value, boneNameToJointInfo, joints, weights);
                    }
                }

                string primName;
                if (!string.IsNullOrWhiteSpace(materialName) && !string.Equals(materialName, "Material", StringComparison.OrdinalIgnoreCase))
                {
                    primName = $"{nodeName}:{materialName}";
                }
                else
                {
                    primName = $"{nodeName}:{primIndex}";
                }

                return new TrinityPrimitive
                {
                    Name = primName,
                    MaterialName = materialName,
                    TemplateShapeIndex = templateShapeIndex,
                    TemplatePartIndex = templatePartIndex,
                    SourceNodeName = nodeName,
                    SourcePrimitiveIndex = primIndex,
                    Positions = positions,
                    Normals = normals,
                    Tangents = tangents,
                    Colors = colors,
                    Uv0 = uv0,
                    HasSkinning = hasSkinning,
                    JointIndices = joints,
                    JointWeights = weights,
                    Indices = indices
                };
            }

            private static void MapJointsInPlace(
                GltfDocument doc,
                IReadOnlyList<JsonElement> skins,
                int skinIndex,
                Dictionary<string, int> boneNameToJointInfo,
                Vector4i[] joints,
                Vector4[] weights)
            {
                if (skinIndex < 0 || skinIndex >= skins.Count)
                {
                    return;
                }

                var skin = skins[skinIndex];
                var jointNodes = GetIntArray(skin, "joints");
                var nodes = GetArray(doc.Json.RootElement, "nodes");

                int ResolveMappedJoint(int jointSlot)
                {
                    if (jointSlot < 0 || jointSlot >= jointNodes.Count)
                    {
                        return -1;
                    }

                    int nodeIndex = jointNodes[jointSlot];
                    if (nodeIndex < 0 || nodeIndex >= nodes.Count)
                    {
                        return -1;
                    }

                    var node = nodes[nodeIndex];
                    var name = TryGetString(node, "name");
                    if (name == null)
                    {
                        return -1;
                    }

                    return boneNameToJointInfo.TryGetValue(name, out var ji) ? ji : -1;
                }

                for (int i = 0; i < joints.Length; i++)
                {
                    var j = joints[i];
                    var w = weights[i];

                    int j0 = ResolveMappedJoint(j.X);
                    int j1 = ResolveMappedJoint(j.Y);
                    int j2 = ResolveMappedJoint(j.Z);
                    int j3 = ResolveMappedJoint(j.W);

                    float w0 = w.X;
                    float w1 = w.Y;
                    float w2 = w.Z;
                    float w3 = w.W;

                    if (j0 < 0) { j0 = 0; w0 = 0; }
                    if (j1 < 0) { j1 = 0; w1 = 0; }
                    if (j2 < 0) { j2 = 0; w2 = 0; }
                    if (j3 < 0) { j3 = 0; w3 = 0; }

                    float sum = w0 + w1 + w2 + w3;
                    if (sum <= 0.000001f)
                    {
                        j0 = 0;
                        w0 = 1;
                        w1 = w2 = w3 = 0;
                        sum = 1;
                    }

                    float inv = 1.0f / sum;
                    weights[i] = new Vector4(w0 * inv, w1 * inv, w2 * inv, w3 * inv);
                    joints[i] = new Vector4i(j0, j1, j2, j3);
                }
            }
        }
    }
}
