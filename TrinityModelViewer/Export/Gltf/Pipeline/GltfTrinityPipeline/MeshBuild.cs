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
        private sealed class TrinityPrimitive
        {
            public string Name = "Mesh";
            public string MaterialName = "Material";
            public int? TemplateShapeIndex;
            public int? TemplatePartIndex;
            public string? SourceNodeName;
            public int SourcePrimitiveIndex;
            public Vector3[] Positions = Array.Empty<Vector3>();
            public Vector3[] Normals = Array.Empty<Vector3>();
            public Vector4[] Tangents = Array.Empty<Vector4>();
            public Vector4[] Colors = Array.Empty<Vector4>();
            public Vector2[] Uv0 = Array.Empty<Vector2>();
            public Vector4i[] JointIndices = Array.Empty<Vector4i>();
            public Vector4 Weights = Vector4.UnitX;
            public Vector4[] JointWeights = Array.Empty<Vector4>();
            public uint[] Indices = Array.Empty<uint>();
            public bool HasSkinning;
        }

        private readonly struct Bounds3
        {
            public Bounds3(Vector3 min, Vector3 max)
            {
                Min = min;
                Max = max;
            }

            public Vector3 Min { get; }
            public Vector3 Max { get; }
        }

        private static (TRMSH Trmsh, TRMBF Trmbf, TRBoundingBox Bounds) BuildTrinityMeshFilesFromTemplate(
            IReadOnlyList<TrinityPrimitive> prims,
            TRMSH templateTrmsh,
            TRMBF templateTrmbf,
            string outTrmbfPath,
            HashSet<string> materialNames)
        {
            if (templateTrmsh.Meshes == null || templateTrmsh.Meshes.Length == 0)
            {
                throw new InvalidOperationException("Template TRMSH has no meshes.");
            }
            if (templateTrmbf.TRMeshBuffers == null || templateTrmbf.TRMeshBuffers.Length == 0)
            {
                throw new InvalidOperationException("Template TRMBF has no mesh buffers.");
            }
            if (templateTrmsh.Meshes.Length != templateTrmbf.TRMeshBuffers.Length)
            {
                throw new InvalidOperationException($"Template TRMSH/TRMBF mismatch: meshes={templateTrmsh.Meshes.Length} buffers={templateTrmbf.TRMeshBuffers.Length}");
            }

            var primBySubmesh = new Dictionary<string, TrinityPrimitive>(StringComparer.OrdinalIgnoreCase);
            var primByTemplateIndex = new Dictionary<(int ShapeIndex, int PartIndex), TrinityPrimitive>();
            foreach (var prim in prims)
            {
                if (prim.TemplateShapeIndex.HasValue && prim.TemplatePartIndex.HasValue)
                {
                    primByTemplateIndex[(prim.TemplateShapeIndex.Value, prim.TemplatePartIndex.Value)] = prim;
                }

                if (!string.IsNullOrWhiteSpace(prim.SourceNodeName))
                {
                    primBySubmesh[$"{prim.SourceNodeName}:{prim.SourcePrimitiveIndex}"] = prim;
                }

                var key = TryGetImportedNodeName(prim.Name) ?? prim.Name;
                if (!primBySubmesh.ContainsKey(key))
                {
                    primBySubmesh[key] = prim;
                }
            }

            var outMeshes = new TRMesh[templateTrmsh.Meshes.Length];
            var outBuffers = new TRModelBuffer[templateTrmbf.TRMeshBuffers.Length];

            var globalMin = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
            var globalMax = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);

            for (int meshShapeIndex = 0; meshShapeIndex < templateTrmsh.Meshes.Length; meshShapeIndex++)
            {
                var meshShape = templateTrmsh.Meshes[meshShapeIndex];
                var meshBuffers = templateTrmbf.TRMeshBuffers[meshShapeIndex];
                if (meshShape == null || meshBuffers == null)
                {
                    throw new InvalidOperationException($"Template has null mesh or buffer at index {meshShapeIndex}.");
                }

                var parts = meshShape.meshParts ?? Array.Empty<TRMeshPart>();
                if (parts.Length == 0)
                {
                    outMeshes[meshShapeIndex] = meshShape;
                    outBuffers[meshShapeIndex] = meshBuffers;
                    continue;
                }

                // Each part is exported as a glTF primitive, with Trinity's mesh shape as the node name.
                // `GltfReader` names primitives as "{MeshName}:{MaterialName}" so we can map them back deterministically.
                var primsForShape = new List<(TRMeshPart Part, TrinityPrimitive Prim)>();
                for (int partIndex = 0; partIndex < parts.Length; partIndex++)
                {
                    var part = parts[partIndex];
                    var submeshKey = $"{meshShape.Name}:{part.MaterialName}";
                    if (!primByTemplateIndex.TryGetValue((meshShapeIndex, partIndex), out var prim) &&
                        !primBySubmesh.TryGetValue(submeshKey, out prim) &&
                        !primBySubmesh.TryGetValue($"{meshShape.Name}:{partIndex}", out prim))
                    {
                        throw new InvalidOperationException($"glTF is missing mesh part '{submeshKey}' required by the template TRMSH.");
                    }

                    primsForShape.Add((part, prim));
                    if (!string.IsNullOrWhiteSpace(prim.MaterialName))
                    {
                        materialNames.Add(prim.MaterialName);
                    }
                }

                // Determine the template vertex count from its first vertex stream stride/length.
                var templateVertexBuffers = meshBuffers.VertexBuffer ?? Array.Empty<TRBuffer>();
                if (templateVertexBuffers.Length == 0)
                {
                    throw new InvalidOperationException($"Template mesh '{meshShape.Name}' has no vertex buffers.");
                }

                var decls = meshShape.vertexDeclaration ?? Array.Empty<TRVertexDeclaration>();
                if (decls.Length == 0)
                {
                    throw new InvalidOperationException($"Template mesh '{meshShape.Name}' has no vertexDeclaration.");
                }

                int streamCount = templateVertexBuffers.Length;

                // Validate strides across declarations and allocate buffers.
                int[] strides = new int[streamCount];
                for (int stream = 0; stream < streamCount; stream++)
                {
                    strides[stream] = GetStride(decls[0], stream);
                    if (strides[stream] <= 0)
                    {
                        throw new InvalidOperationException($"Template mesh '{meshShape.Name}' has invalid stride for stream {stream}.");
                    }
                }
                for (int d = 1; d < decls.Length; d++)
                {
                    for (int stream = 0; stream < streamCount; stream++)
                    {
                        int stride = GetStride(decls[d], stream);
                        if (stride != strides[stream])
                        {
                            throw new InvalidOperationException(
                                $"Template mesh '{meshShape.Name}' has inconsistent stride for stream {stream} across vertex declarations ({strides[stream]} vs {stride}).");
                        }
                    }
                }

                int templateVertexCount = templateVertexBuffers[0]?.Bytes?.Length > 0 ? templateVertexBuffers[0].Bytes.Length / strides[0] : 0;
                if (templateVertexCount <= 0)
                {
                    throw new InvalidOperationException($"Template mesh '{meshShape.Name}' has an empty vertex buffer for stream 0.");
                }
                for (int stream = 1; stream < streamCount; stream++)
                {
                    var buf = templateVertexBuffers[stream];
                    if (buf?.Bytes == null)
                    {
                        continue;
                    }
                    int count = buf.Bytes.Length / strides[stream];
                    if (count != templateVertexCount)
                    {
                        throw new InvalidOperationException(
                            $"Template mesh '{meshShape.Name}' has inconsistent vertex counts across vertex streams (stream0={templateVertexCount}, stream{stream}={count}).");
                    }
                }

                static bool PositionsExactlyEqual(Vector3[] a, Vector3[] b)
                {
                    if (ReferenceEquals(a, b))
                    {
                        return true;
                    }
                    if (a.Length != b.Length)
                    {
                        return false;
                    }
                    for (int i = 0; i < a.Length; i++)
                    {
                        if (a[i] != b[i])
                        {
                            return false;
                        }
                    }
                    return true;
                }

                // used a shared vertex pool. To keep the pipeline deterministic, merge such primitives into a single
                // vertex pool by concatenating their vertices and remapping indices.
                //
                // We detect this by checking whether the vertex streams are truly shared across primitives; if not,
                // concatenation is used to preserve the (primitive-local) vertex order and indices.
                bool needsConcatenation = false;
                var firstPrim = primsForShape[0].Prim;
                for (int i = 1; i < primsForShape.Count; i++)
                {
                    var prim = primsForShape[i].Prim;
                    if (prim.Positions.Length != firstPrim.Positions.Length || !PositionsExactlyEqual(prim.Positions, firstPrim.Positions))
                    {
                        needsConcatenation = true;
                        break;
                    }
                }
                var partInfos = new List<(TRMeshPart Part, TrinityPrimitive Prim, int VertexBase, int LocalVertexCount)>(primsForShape.Count);
                TrinityPrimitive canonical;
                int vertexCount;
                if (!needsConcatenation)
                {
                    canonical = primsForShape[0].Prim;
                    vertexCount = canonical.Positions.Length;
                    for (int i = 0; i < primsForShape.Count; i++)
                    {
                        partInfos.Add((primsForShape[i].Part, primsForShape[i].Prim, 0, vertexCount));
                    }
                }
                else
                {
                    int total = 0;
                    for (int i = 0; i < primsForShape.Count; i++)
                    {
                        int count = primsForShape[i].Prim.Positions.Length;
                        if (count <= 0)
                        {
                            throw new InvalidOperationException($"glTF mesh '{meshShape.Name}' has an empty primitive '{primsForShape[i].Prim.Name}'.");
                        }
                        total += count;
                    }

                    vertexCount = total;

                    bool needsNormals = DeclHas(decls, TRVertexUsage.NORMAL, 0);
                    bool needsUv0 = DeclHas(decls, TRVertexUsage.TEX_COORD, 0);
                    bool needsSkin = DeclHas(decls, TRVertexUsage.BLEND_INDEX, 0) || DeclHas(decls, TRVertexUsage.BLEND_WEIGHTS, 0);

                    var positions = new Vector3[vertexCount];
                    var normals = needsNormals ? new Vector3[vertexCount] : Array.Empty<Vector3>();
                    var tangents = new Vector4[vertexCount];
                    var colors = new Vector4[vertexCount];
                    var uv0 = needsUv0 ? new Vector2[vertexCount] : Array.Empty<Vector2>();
                    var joints = needsSkin ? new Vector4i[vertexCount] : Array.Empty<Vector4i>();
                    var weights = needsSkin ? new Vector4[vertexCount] : Array.Empty<Vector4>();

                    // Defaults for missing streams.
                    if (needsNormals)
                    {
                        for (int i = 0; i < normals.Length; i++) normals[i] = Vector3.UnitZ;
                    }
                    for (int i = 0; i < tangents.Length; i++) tangents[i] = new Vector4(1f, 0f, 0f, 1f);
                    for (int i = 0; i < colors.Length; i++) colors[i] = Vector4.One;
                    if (needsUv0)
                    {
                        for (int i = 0; i < uv0.Length; i++) uv0[i] = Vector2.Zero;
                    }
                    if (needsSkin)
                    {
                        for (int i = 0; i < weights.Length; i++) weights[i] = new Vector4(1f, 0f, 0f, 0f);
                    }

                    int cursor = 0;
                    bool anySkinning = false;
                    for (int i = 0; i < primsForShape.Count; i++)
                    {
                        var prim = primsForShape[i].Prim;
                        int localCount = prim.Positions.Length;
                        partInfos.Add((primsForShape[i].Part, prim, cursor, localCount));

                        Array.Copy(prim.Positions, 0, positions, cursor, localCount);

                        if (needsNormals && prim.Normals != null && prim.Normals.Length == localCount)
                        {
                            Array.Copy(prim.Normals, 0, normals, cursor, localCount);
                        }
                        if (prim.Tangents != null && prim.Tangents.Length == localCount)
                        {
                            Array.Copy(prim.Tangents, 0, tangents, cursor, localCount);
                        }
                        if (prim.Colors != null && prim.Colors.Length == localCount)
                        {
                            Array.Copy(prim.Colors, 0, colors, cursor, localCount);
                        }
                        if (needsUv0 && prim.Uv0 != null && prim.Uv0.Length == localCount)
                        {
                            Array.Copy(prim.Uv0, 0, uv0, cursor, localCount);
                        }
                        if (needsSkin && prim.HasSkinning && prim.JointIndices != null && prim.JointWeights != null &&
                            prim.JointIndices.Length == localCount && prim.JointWeights.Length == localCount)
                        {
                            Array.Copy(prim.JointIndices, 0, joints, cursor, localCount);
                            Array.Copy(prim.JointWeights, 0, weights, cursor, localCount);
                            anySkinning = true;
                        }

                        cursor += localCount;
                    }

                    canonical = new TrinityPrimitive
                    {
                        Name = meshShape.Name,
                        MaterialName = string.Empty,
                        Positions = positions,
                        Normals = normals,
                        Tangents = tangents,
                        Colors = colors,
                        Uv0 = uv0,
                        HasSkinning = needsSkin && anySkinning,
                        JointIndices = joints,
                        JointWeights = weights,
                        Indices = Array.Empty<uint>()
                    };
                }

                if (vertexCount <= 0)
                {
                    throw new InvalidOperationException($"glTF mesh '{meshShape.Name}' has no vertices.");
                }

                bool vertexCountChanged = vertexCount != templateVertexCount;

                // Safety: if the mesh contains morph targets, changing vertex count would invalidate morph buffers.
                if (vertexCountChanged && meshBuffers.MorphTargets != null && meshBuffers.MorphTargets.Length > 0)
                {
                    throw new InvalidOperationException(
                        $"Mesh '{meshShape.Name}' contains morph targets; vertex-count changes are not supported for morph-enabled meshes yet.");
                }

                // Safety: if the mesh has multiple LOD index buffers in this TRMBF entry, changing vertex count would invalidate the other index buffers.
                var templateIndexBuffers = meshBuffers.IndexBuffer ?? Array.Empty<TRBuffer>();
                if (vertexCountChanged && templateIndexBuffers.Length > 1)
                {
                    throw new InvalidOperationException(
                        $"Mesh '{meshShape.Name}' contains multiple index buffers (LOD variants). Vertex-count changes are not supported for this mesh yet.");
                }

                static bool DeclHas(TRVertexDeclaration[] declList, TRVertexUsage usage, int layer)
                {
                    foreach (var d in declList)
                    {
                        if (d?.vertexElements == null)
                        {
                            continue;
                        }

                        foreach (var e in d.vertexElements)
                        {
                            if (e == null)
                            {
                                continue;
                            }

                            if (e.Usage == usage && (int)e.Layer == layer)
                            {
                                return true;
                            }
                        }
                    }

                    return false;
                }

                if (vertexCountChanged)
                {
                    // If the template declares a stream, require the glTF to provide it when adding/removing vertices.
                    // Otherwise, new vertices would be written with undefined/default values.
                    if (DeclHas(decls, TRVertexUsage.NORMAL, 0) && (canonical.Normals == null || canonical.Normals.Length != vertexCount))
                    {
                        throw new InvalidOperationException($"Mesh '{meshShape.Name}' requires normals; the imported glTF is missing NORMAL.");
                    }
                    if (DeclHas(decls, TRVertexUsage.TEX_COORD, 0) && (canonical.Uv0 == null || canonical.Uv0.Length != vertexCount))
                    {
                        throw new InvalidOperationException($"Mesh '{meshShape.Name}' requires UV0; the imported glTF is missing TEXCOORD_0.");
                    }
                    bool requiresJoints = DeclHas(decls, TRVertexUsage.BLEND_INDEX, 0);
                    bool requiresWeights = DeclHas(decls, TRVertexUsage.BLEND_WEIGHTS, 0);
                    if ((requiresJoints || requiresWeights) && !canonical.HasSkinning)
                    {
                        throw new InvalidOperationException(
                            $"Mesh '{meshShape.Name}' is skinned in the template, but the imported glTF has no skinning (JOINTS_0/WEIGHTS_0). " +
                            "Make sure the mesh remains skinned (armature/skin modifier) and you export with skinning enabled.");
                    }
                }

                var outVertexBuffers = new TRBuffer[streamCount];
                for (int stream = 0; stream < streamCount; stream++)
                {
                    int newLen = vertexCount * strides[stream];
                    var bytes = new byte[newLen];

                    bool preserveTemplateBytes = !needsConcatenation && !vertexCountChanged;
                    if (preserveTemplateBytes)
                    {
                        // Preserve existing per-vertex data for attributes we don't currently re-export (UV1+, binormals, etc.)
                        // only when the vertex pool is unchanged (same count, same layout).
                        var src = templateVertexBuffers[stream]?.Bytes;
                        if (src != null && src.Length > 0)
                        {
                            Buffer.BlockCopy(src, 0, bytes, 0, Math.Min(src.Length, bytes.Length));
                        }
                    }

                    outVertexBuffers[stream] = new TRBuffer { Bytes = bytes };
                }

                // Populate declared attributes.
                foreach (var decl in decls)
                {
                    foreach (var el in decl.vertexElements ?? Array.Empty<TRVertexElement>())
                    {
                        if (el == null)
                        {
                            continue;
                        }

                        int stream = el.Slot;
                        if (stream < 0 || stream >= streamCount)
                        {
                            continue;
                        }

                        WriteVertexElement(
                            outVertexBuffers[stream].Bytes,
                            strides[stream],
                            el,
                            canonical,
                            vertexCount);
                    }
                }

                // Build a single LOD0 index buffer by concatenating parts in template order and updating offsets.
                var indexType = meshShape.IndexType;
                int indexSize = indexType switch
                {
                    TRIndexFormat.BYTE => 1,
                    TRIndexFormat.SHORT => 2,
                    TRIndexFormat.INT => 4,
                    _ => 4
                };
                if (vertexCountChanged && indexType == TRIndexFormat.BYTE && vertexCount > byte.MaxValue)
                {
                    throw new InvalidOperationException(
                        $"Mesh '{meshShape.Name}' uses BYTE indices but the edited mesh has {vertexCount} vertices. Reduce vertex count below 255, or keep topology unchanged.");
                }
                if (vertexCountChanged && indexType == TRIndexFormat.SHORT && vertexCount > ushort.MaxValue)
                {
                    throw new InvalidOperationException(
                        $"Mesh '{meshShape.Name}' uses SHORT indices but the edited mesh has {vertexCount} vertices. Reduce vertex count below 65535, or keep topology unchanged.");
                }

                var outParts = new TRMeshPart[parts.Length];
                int runningIndexOffset = 0;
                var indexBytes = new List<byte>(capacity: primsForShape.Sum(p => p.Prim.Indices.Length) * indexSize);
                for (int p = 0; p < parts.Length; p++)
                {
                    var (part, prim, vertexBase, localVertexCount) = needsConcatenation ? partInfos[p] : (primsForShape[p].Part, primsForShape[p].Prim, 0, vertexCount);
                    if (prim.Indices.Length == 0)
                    {
                        throw new InvalidOperationException($"glTF mesh part '{meshShape.Name}:{part.MaterialName}' has no indices.");
                    }

                    foreach (var idx in prim.Indices)
                    {
                        if (idx >= (uint)localVertexCount)
                        {
                            throw new InvalidOperationException(
                                $"glTF mesh part '{meshShape.Name}:{part.MaterialName}' references out-of-range vertex index {idx} (vertexCount={localVertexCount}).");
                        }

                        uint adjusted = (uint)vertexBase + idx;
                        if (adjusted >= (uint)vertexCount)
                        {
                            throw new InvalidOperationException(
                                $"glTF mesh part '{meshShape.Name}:{part.MaterialName}' produced an invalid remapped index {adjusted} (vertexCount={vertexCount}).");
                        }

                        switch (indexType)
                        {
                            case TRIndexFormat.BYTE:
                                if (adjusted > byte.MaxValue)
                                {
                                    throw new InvalidOperationException($"Index {adjusted} does not fit in BYTE indices for mesh '{meshShape.Name}'.");
                                }
                                indexBytes.Add((byte)adjusted);
                                break;
                            case TRIndexFormat.SHORT:
                                if (adjusted > ushort.MaxValue)
                                {
                                    throw new InvalidOperationException($"Index {adjusted} does not fit in SHORT indices for mesh '{meshShape.Name}'.");
                                }
                                indexBytes.Add((byte)(adjusted & 0xFF));
                                indexBytes.Add((byte)((adjusted >> 8) & 0xFF));
                                break;
                            case TRIndexFormat.INT:
                            default:
                                indexBytes.Add((byte)(adjusted & 0xFF));
                                indexBytes.Add((byte)((adjusted >> 8) & 0xFF));
                                indexBytes.Add((byte)((adjusted >> 16) & 0xFF));
                                indexBytes.Add((byte)((adjusted >> 24) & 0xFF));
                                break;
                        }
                    }

                    outParts[p] = new TRMeshPart
                    {
                        indexCount = prim.Indices.Length,
                        indexOffset = runningIndexOffset,
                        unk3 = part.unk3,
                        MaterialName = prim.MaterialName,
                        vertexDeclarationIndex = part.vertexDeclarationIndex
                    };

                    runningIndexOffset += prim.Indices.Length;
                }

                var bounds = ComputeBounds(canonical.Positions);
                globalMin = Vector3.ComponentMin(globalMin, bounds.Min);
                globalMax = Vector3.ComponentMax(globalMax, bounds.Max);

                outMeshes[meshShapeIndex] = new TRMesh
                {
                    Name = meshShape.Name,
                    boundingBox = new TRBoundingBox
                    {
                        MinBound = new Vector3f { X = bounds.Min.X, Y = bounds.Min.Y, Z = bounds.Min.Z },
                        MaxBound = new Vector3f { X = bounds.Max.X, Y = bounds.Max.Y, Z = bounds.Max.Z }
                    },
                    IndexType = meshShape.IndexType,
                    vertexDeclaration = meshShape.vertexDeclaration,
                    meshParts = outParts,
                    res0 = meshShape.res0,
                    res1 = meshShape.res1,
                    res2 = meshShape.res2,
                    res3 = meshShape.res3,
                    clipSphere = new Sphere
                    {
                        X = (bounds.Min.X + bounds.Max.X) * 0.5f,
                        Y = (bounds.Min.Y + bounds.Max.Y) * 0.5f,
                        Z = (bounds.Min.Z + bounds.Max.Z) * 0.5f,
                        Radius = (bounds.Max - bounds.Min).Length * 0.5f
                    },
                    boneWeight = BuildBoneWeights(meshShape, canonical, vertexCount, vertexCountChanged),
                    MeshUnk7 = meshShape.MeshUnk7,
                    MeshName = meshShape.MeshName
                };

                var outIndexBuffers = templateIndexBuffers.Length > 0
                    ? templateIndexBuffers.Select((buf, i) => i == 0 ? new TRBuffer { Bytes = indexBytes.ToArray() } : buf).ToArray()
                    : new[] { new TRBuffer { Bytes = indexBytes.ToArray() } };

                outBuffers[meshShapeIndex] = new TRModelBuffer
                {
                    IndexBuffer = outIndexBuffers,
                    VertexBuffer = outVertexBuffers,
                    MorphTargets = meshBuffers.MorphTargets ?? Array.Empty<TRMorphTarget>()
                };
            }

            var resultBounds = new TRBoundingBox
            {
                MinBound = new Vector3f { X = globalMin.X, Y = globalMin.Y, Z = globalMin.Z },
                MaxBound = new Vector3f { X = globalMax.X, Y = globalMax.Y, Z = globalMax.Z }
            };

            var outTrmsh = new TRMSH
            {
                Version = templateTrmsh.Version,
                Meshes = outMeshes,
                bufferFilePath = outTrmbfPath.Replace('\\', '/')
            };

            var outTrmbf = new TRMBF
            {
                Field_00 = templateTrmbf.Field_00,
                TRMeshBuffers = outBuffers
            };

            return (outTrmsh, outTrmbf, resultBounds);
        }

        private static TRBoneWeight[]? BuildBoneWeights(TRMesh meshShape, TrinityPrimitive canonical, int vertexCount, bool vertexCountChanged)
        {
            // Many Trinity meshes (notably protag clothing with connected skinning palettes) rely on the template's
            // `boneWeight` table semantics for interpreting BLEND_INDEX. Rebuilding this table from glTF joints can
            // silently change the expected index space (boneWeight table index vs joint info vs palette), causing
            // remap heuristics and incorrect deformation. Preserve the template's table unless there wasn't one.
            if (meshShape.boneWeight != null && meshShape.boneWeight.Length > 0)
            {
                return meshShape.boneWeight;
            }

            // If there is no skinning on the imported primitive, keep whatever the template had.
            if (!canonical.HasSkinning || canonical.JointIndices == null || canonical.JointWeights == null)
            {
                return meshShape.boneWeight;
            }

            // If the template had no bone weights and the vertex count didn't change, leave it alone to avoid surprises.
            if ((meshShape.boneWeight == null || meshShape.boneWeight.Length == 0) && !vertexCountChanged)
            {
                return meshShape.boneWeight;
            }

            if (canonical.JointIndices.Length != vertexCount || canonical.JointWeights.Length != vertexCount)
            {
                return meshShape.boneWeight;
            }

            var totals = new Dictionary<int, float>();
            for (int i = 0; i < vertexCount; i++)
            {
                var j = canonical.JointIndices[i];
                var w = canonical.JointWeights[i];

                void Add(int joint, float weight)
                {
                    if (weight <= 0.0000001f)
                    {
                        return;
                    }
                    if (joint < 0)
                    {
                        return;
                    }
                    totals[joint] = totals.TryGetValue(joint, out var existing) ? existing + weight : weight;
                }

                Add(j.X, w.X);
                Add(j.Y, w.Y);
                Add(j.Z, w.Z);
                Add(j.W, w.W);
            }

            if (totals.Count == 0)
            {
                return meshShape.boneWeight;
            }

            return totals
                .OrderBy(kvp => kvp.Key)
                .Select(kvp => new TRBoneWeight { RigIndex = kvp.Key, RigWeight = kvp.Value })
                .ToArray();
        }

        private static string? TryGetImportedNodeName(string? importedName)
        {
            if (string.IsNullOrWhiteSpace(importedName))
            {
                return null;
            }

            int last = importedName.LastIndexOf('_');
            if (last <= 0 || last >= importedName.Length - 1)
            {
                return importedName;
            }

            var suffix = importedName.Substring(last + 1);
            if (!suffix.All(char.IsDigit))
            {
                return importedName;
            }

            return importedName.Substring(0, last);
        }

        private static Bounds3 ComputeBounds(Vector3[] positions)
        {
            var min = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
            var max = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
            for (int i = 0; i < positions.Length; i++)
            {
                min = Vector3.ComponentMin(min, positions[i]);
                max = Vector3.ComponentMax(max, positions[i]);
            }
            return new Bounds3(min, max);
        }
    }
}
