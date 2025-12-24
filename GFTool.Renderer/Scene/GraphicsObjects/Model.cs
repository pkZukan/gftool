using GFTool.Core.Utils;
using GFTool.Renderer.Core;
using GFTool.Renderer.Core.Graphics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using Trinity.Core.Flatbuffers.TR.Model;
using Trinity.Core.Flatbuffers.Gfx2;
using Trinity.Core.Utils;
using System.IO;
using System;

namespace GFTool.Renderer.Scene.GraphicsObjects
{
    public class Model : RefObject
    {
        private enum BlendIndexRemapMode
        {
            None,
            BoneWeights,
            JointInfo,
            SkinningPalette,
            BoneMeta
        }

        public sealed class ExportSubmesh
        {
            public required string Name { get; init; }
            public required string MaterialName { get; init; }
            public required Vector3[] Positions { get; init; }
            public required Vector3[] Normals { get; init; }
            public required Vector2[] UVs { get; init; }
            public required Vector4[] Colors { get; init; }
            public required Vector4[] Tangents { get; init; }
            public required Vector3[] Binormals { get; init; }
            public required Vector4[] BlendIndices { get; init; }
            public required Vector4[] BlendWeights { get; init; }
            public required uint[] Indices { get; init; }
            public required bool HasVertexColors { get; init; }
            public required bool HasTangents { get; init; }
            public required bool HasBinormals { get; init; }
            public required bool HasSkinning { get; init; }
        }

        public sealed class ExportData
        {
            public required string Name { get; init; }
            public required IReadOnlyList<ExportSubmesh> Submeshes { get; init; }
            public required Armature? Armature { get; init; }
            public required IReadOnlyList<Material> Materials { get; init; }
        }

        public ExportData CreateExportData()
        {
            var subs = new List<ExportSubmesh>(Positions.Count);
            int count = Positions.Count;
            for (int i = 0; i < count; i++)
            {
                string submeshName = i < SubmeshNames.Count ? SubmeshNames[i] : $"Submesh {i}";
                string materialName = i < MaterialNames.Count ? MaterialNames[i] : string.Empty;
                subs.Add(new ExportSubmesh
                {
                    Name = submeshName,
                    MaterialName = materialName,
                    Positions = Positions[i],
                    Normals = i < Normals.Count ? Normals[i] : Array.Empty<Vector3>(),
                    UVs = i < UVs.Count ? UVs[i] : Array.Empty<Vector2>(),
                    Colors = i < Colors.Count ? Colors[i] : Array.Empty<Vector4>(),
                    Tangents = i < Tangents.Count ? Tangents[i] : Array.Empty<Vector4>(),
                    Binormals = i < Binormals.Count ? Binormals[i] : Array.Empty<Vector3>(),
                    BlendIndices = i < BlendIndicies.Count ? BlendIndicies[i] : Array.Empty<Vector4>(),
                    BlendWeights = i < BlendWeights.Count ? BlendWeights[i] : Array.Empty<Vector4>(),
                    Indices = i < Indices.Count ? Indices[i] : Array.Empty<uint>(),
                    HasVertexColors = i < HasVertexColors.Count && HasVertexColors[i],
                    HasTangents = i < HasTangents.Count && HasTangents[i],
                    HasBinormals = i < HasBinormals.Count && HasBinormals[i],
                    HasSkinning = i < HasSkinning.Count && HasSkinning[i]
                });
            }

            return new ExportData
            {
                Name = Name,
                Submeshes = subs,
                Armature = armature,
                Materials = materials ?? Array.Empty<Material>()
            };
        }

        public readonly struct UvSet
        {
            public UvSet(Vector2[] uvs, uint[] indices, string submeshName)
            {
                Uvs = uvs;
                Indices = indices;
                SubmeshName = submeshName;
            }

            public Vector2[] Uvs { get; }
            public uint[] Indices { get; }
            public string SubmeshName { get; }
        }
        private PathString modelPath;
        private string? baseSkeletonCategoryHint;

        public string Name { get; private set; }

        private int[] VAOs;
        private int[] VBOs;
        private int[] EBOs;

        private List<Vector3[]> Positions = new List<Vector3[]>();
        private List<Vector3[]> Normals = new List<Vector3[]>();
        private List<Vector2[]> UVs = new List<Vector2[]>();
        private List<Vector4[]> Colors = new List<Vector4[]>();
        private List<Vector4[]> Tangents = new List<Vector4[]>();
        private List<Vector3[]> Binormals = new List<Vector3[]>();
        private List<Vector4[]> BlendIndicies = new List<Vector4[]>();
        private List<Vector4[]> BlendWeights = new List<Vector4[]>();
        private List<TRBoneWeight[]?> BlendBoneWeights = new List<TRBoneWeight[]?>();
        private List<Vector4[]> BlendIndiciesOriginal = new List<Vector4[]>();
        private List<string> BlendMeshNames = new List<string>();

        private List<uint[]> Indices = new List<uint[]>();
        private List<bool> HasVertexColors = new List<bool>();
        private List<bool> HasTangents = new List<bool>();
        private List<bool> HasBinormals = new List<bool>();
        private List<bool> HasSkinning = new List<bool>();

        private Material[] materials;
        private List<string> MaterialNames = new List<string>();
        private Dictionary<string, Material> materialMap = new Dictionary<string, Material>(StringComparer.OrdinalIgnoreCase);
        private List<string> SubmeshNames = new List<string>();

        private Armature? armature;
        public Armature? Armature => armature;
        private static int skeletonVao;
        private static int skeletonVbo;
        private static readonly float[] unitBoneVerts = BuildUnitBoneVerts();

        private Matrix4 modelMat;
        private int selectedSubmeshIndex = -1;
        private BlendIndexStats blendIndexStats;
        private int[] blendIndexOffsets;
        private int[] blendIndexByteSizes;
        public bool IsVisible { get; private set; } = true;

        public Model(string model, bool loadAllLods)
        {
            Name = Path.GetFileNameWithoutExtension(model);
            modelMat = Matrix4.Identity;
            modelPath = new PathString(model);

            var mdl = FlatBufferConverter.DeserializeFrom<TRMDL>(model);

            //Meshes
            if (loadAllLods)
            {
                foreach (var mesh in mdl.Meshes)
                {
                    ParseMesh(modelPath.Combine(mesh.PathName));
                }
            }
            else
            {
                var mesh = mdl.Meshes[0]; //LOD0
                ParseMesh(modelPath.Combine(mesh.PathName));
            }

            baseSkeletonCategoryHint = GuessBaseSkeletonCategoryFromMesh(mdl.Meshes != null && mdl.Meshes.Length > 0 ? mdl.Meshes[0].PathName : null);

            //Materials
            foreach (var mat in mdl.Materials)
            {
                ParseMaterial(modelPath.Combine(mat));
            }

            //Skeleton
            if (mdl.Skeleton != null)
                ParseArmature(modelPath.Combine(mdl.Skeleton.PathName));
        }

        private static string? GuessBaseSkeletonCategoryFromMesh(string? meshPathName)
        {
            if (string.IsNullOrWhiteSpace(meshPathName))
            {
                return null;
            }

            string file = Path.GetFileName(meshPathName);
            if (file.StartsWith("p0", StringComparison.OrdinalIgnoreCase) ||
                file.StartsWith("p1", StringComparison.OrdinalIgnoreCase) ||
                file.StartsWith("p2", StringComparison.OrdinalIgnoreCase))
            {
                return "Protag";
            }

            // Common NPC prefixes
            if (file.StartsWith("bu_", StringComparison.OrdinalIgnoreCase)) return "CommonNPCbu";
            if (file.StartsWith("dm_", StringComparison.OrdinalIgnoreCase)) return "CommonNPCdm";
            if (file.StartsWith("df_", StringComparison.OrdinalIgnoreCase)) return "CommonNPCdf";
            if (file.StartsWith("em_", StringComparison.OrdinalIgnoreCase)) return "CommonNPCem";
            if (file.StartsWith("fm_", StringComparison.OrdinalIgnoreCase)) return "CommonNPCfm";
            if (file.StartsWith("ff_", StringComparison.OrdinalIgnoreCase)) return "CommonNPCff";
            if (file.StartsWith("gm_", StringComparison.OrdinalIgnoreCase)) return "CommonNPCgm";
            if (file.StartsWith("gf_", StringComparison.OrdinalIgnoreCase)) return "CommonNPCgf";
            if (file.StartsWith("rv_", StringComparison.OrdinalIgnoreCase)) return "CommonNPCrv";

            return null;
        }

        private void ParseMeshBuffer(TRVertexDeclaration vertDesc, TRBuffer[] vertexBuffers, TRBuffer indexBuf, TRIndexFormat polyType, long start, long count, TRBoneWeight[]? boneWeights, string meshName)
        {
            if (vertexBuffers == null || vertexBuffers.Length == 0)
            {
                return;
            }

            var posElement = vertDesc.vertexElements.FirstOrDefault(e => e.vertexUsage == TRVertexUsage.POSITION);
            if (posElement == null)
            {
                return;
            }

            var posBuffer = GetVertexBuffer(vertexBuffers, posElement.vertexElementLayer);
            if (posBuffer == null)
            {
                return;
            }

            var posStride = GetStride(vertDesc, posElement.vertexElementSizeIndex);
            if (posStride <= 0)
            {
                return;
            }

            int vertexCount = posBuffer.Bytes.Length / posStride;
            if (vertexCount <= 0)
            {
                return;
            }

            Vector3[] pos = new Vector3[vertexCount];
            Vector3[] norm = new Vector3[vertexCount];
            Vector2[] uv = new Vector2[vertexCount];
            Vector4[] color = new Vector4[vertexCount];
            Vector4[] tangent = new Vector4[vertexCount];
            Vector3[] binormal = new Vector3[vertexCount];
            Vector4[] blendIndices = new Vector4[vertexCount];
            Vector4[] blendWeights = new Vector4[vertexCount];
            bool hasNormals = false;
            bool hasUvs = false;
            bool hasColors = false;
            bool hasTangents = false;
            bool hasBinormals = false;
            bool hasBlendIndices = false;
            bool hasBlendWeights = false;
            blendIndexStats = null;

            List<uint> indices = new List<uint>();
            long currPos = 0;

            var blendIndexStreams = new List<Vector4[]>();
            var blendWeightStreams = new List<Vector4[]>();
            int blendIndexElementIndex = -1;
            int blendWeightElementIndex = -1;

            for (int i = 0; i < vertDesc.vertexElements.Length; i++)
            {
                var att = vertDesc.vertexElements[i];
                var buffer = GetVertexBuffer(vertexBuffers, att.vertexElementLayer);
                if (buffer == null)
                {
                    continue;
                }

                var stride = GetStride(vertDesc, att.vertexElementSizeIndex);
                if (stride <= 0)
                {
                    continue;
                }

                int? blendIndexStreamIndex = null;
                int? blendWeightStreamIndex = null;
                if (att.vertexUsage == TRVertexUsage.BLEND_INDEX)
                {
                    blendIndexElementIndex++;
                    EnsureBlendStream(blendIndexStreams, blendIndexElementIndex, vertexCount);
                    blendIndexStreamIndex = blendIndexElementIndex;
                }
                else if (att.vertexUsage == TRVertexUsage.BLEND_WEIGHTS)
                {
                    blendWeightElementIndex++;
                    EnsureBlendStream(blendWeightStreams, blendWeightElementIndex, vertexCount);
                    blendWeightStreamIndex = blendWeightElementIndex;
                }

                for (int v = 0; v < vertexCount; v++)
                {
                    int offset = (v * stride) + att.vertexElementOffset;
                    if (!HasBytes(buffer.Bytes, offset, att.vertexFormat))
                    {
                        continue;
                    }

                    switch (att.vertexUsage)
                    {
                        case TRVertexUsage.POSITION:
                            pos[v] = ReadVector3(buffer.Bytes, offset, att.vertexFormat);
                            break;
                        case TRVertexUsage.NORMAL:
                            norm[v] = ReadNormal(buffer.Bytes, offset, att.vertexFormat);
                            hasNormals = true;
                            break;
                        case TRVertexUsage.TEX_COORD:
                            uv[v] = ReadVector2(buffer.Bytes, offset, att.vertexFormat);
                            hasUvs = true;
                            break;
                        case TRVertexUsage.COLOR:
                            color[v] = ReadColor(buffer.Bytes, offset, att.vertexFormat);
                            hasColors = true;
                            break;
                        case TRVertexUsage.TANGENT:
                            tangent[v] = ReadTangent(buffer.Bytes, offset, att.vertexFormat);
                            hasTangents = true;
                            break;
                        case TRVertexUsage.BINORMAL:
                            binormal[v] = ReadNormal(buffer.Bytes, offset, att.vertexFormat);
                            hasBinormals = true;
                            break;
                        case TRVertexUsage.BLEND_INDEX:
                            if (blendIndexStreamIndex.HasValue)
                            {
                                blendIndexStreams[blendIndexStreamIndex.Value][v] = ReadBlendIndices(buffer.Bytes, offset, att.vertexFormat);
                            }
                            hasBlendIndices = true;
                            break;
                        case TRVertexUsage.BLEND_WEIGHTS:
                            if (blendWeightStreamIndex.HasValue)
                            {
                                blendWeightStreams[blendWeightStreamIndex.Value][v] = ReadBlendWeights(buffer.Bytes, offset, att.vertexFormat);
                            }
                            hasBlendWeights = true;
                            break;
                    }
                }
            }

            if (hasBlendIndices && blendIndexStreams.Count > 0)
            {
                blendIndices = blendIndexStreams[0];
            }

            if (hasBlendWeights && blendWeightStreams.Count > 0)
            {
                blendWeights = blendWeightStreams[0];
            }

            // Some meshes carry multiple BLEND INDEX and BLEND WEIGHTS streams (usually 8 influences).
            // Shaders only support 4, so the top 4 weights per vertex are kept.
            if ((blendIndexStreams.Count > 1 || blendWeightStreams.Count > 1) && hasBlendIndices && hasBlendWeights)
            {
                int streamCount = Math.Min(blendIndexStreams.Count, blendWeightStreams.Count);
                if (streamCount > 1)
                {
                    CollapseBlendStreams(blendIndexStreams, blendWeightStreams, streamCount, out blendIndices, out blendWeights);
                }
            }

            if (hasBlendIndices)
            {
                int maxIndex = 0;
                for (int v = 0; v < vertexCount; v++)
                {
                    var idx = blendIndices[v];
                    maxIndex = Math.Max(maxIndex, (int)MathF.Max(MathF.Max(idx.X, idx.Y), MathF.Max(idx.Z, idx.W)));
                }

                blendIndexStats = new BlendIndexStats
                {
                    VertexCount = vertexCount,
                    MaxIndex = maxIndex
                };
            }

            Positions.Add(pos);
            Normals.Add(hasNormals ? norm : new Vector3[vertexCount]);
            UVs.Add(hasUvs ? uv : new Vector2[vertexCount]);
            if (!hasColors)
            {
                for (int v = 0; v < color.Length; v++)
                {
                    color[v] = Vector4.One;
                }
            }
            Colors.Add(color);
            HasVertexColors.Add(hasColors);
            if (!hasTangents)
            {
                for (int v = 0; v < tangent.Length; v++)
                {
                    tangent[v] = new Vector4(1f, 0f, 0f, 1f);
                }
            }
            Tangents.Add(tangent);
            HasTangents.Add(hasTangents);
            if (!hasBinormals)
            {
                for (int v = 0; v < binormal.Length; v++)
                {
                    binormal[v] = Vector3.UnitY;
                }
            }
            Binormals.Add(binormal);
            HasBinormals.Add(hasBinormals);
            BlendIndicies.Add(blendIndices);
            BlendIndiciesOriginal.Add(blendIndices.ToArray());
            BlendWeights.Add(blendWeights);
            BlendBoneWeights.Add(boneWeights);
            BlendMeshNames.Add(meshName);
            HasSkinning.Add(hasBlendIndices && hasBlendWeights);

            //Parse index buffer
            using (var indBuf = new BinaryReader(new MemoryStream(indexBuf.Bytes)))
            {
                int indsize = (1 << (int)polyType);
                currPos = start * indsize;
                indBuf.BaseStream.Position = currPos;
                while (currPos < (start + count) * indsize)
                {
                    switch (polyType)
                    {
                        case TRIndexFormat.BYTE: indices.Add(indBuf.ReadByte()); break;
                        case TRIndexFormat.SHORT: indices.Add(indBuf.ReadUInt16()); break;
                        case TRIndexFormat.INT: indices.Add(indBuf.ReadUInt32()); break;
                    }
                    currPos += indsize;
                }
                Indices.Add(indices.ToArray());
            }

        }

        private void ParseMesh(string file)
        {
            var msh = FlatBufferConverter.DeserializeFrom<TRMSH>(file);
            var buffers = FlatBufferConverter.DeserializeFrom<TRMBF>(modelPath.Combine(msh.bufferFilePath)).TRMeshBuffers;
            var shapeCnt = msh.Meshes.Count();
            for (int i = 0; i < shapeCnt; i++)
            {
                var meshShape = msh.Meshes[i];
                var vertBufs = buffers[i].VertexBuffer;
                var indexBuf = buffers[i].IndexBuffer[0]; //LOD0
                var polyType = meshShape.IndexType;
                int boneWeightCount = meshShape.boneWeight?.Length ?? 0;

                foreach (var part in meshShape.meshParts)
                {
                    MaterialNames.Add(part.MaterialName);
                    SubmeshNames.Add($"{meshShape.Name}:{part.MaterialName}");
                    int declIndex = part.vertexDeclarationIndex;
                    if (declIndex < 0 || declIndex >= meshShape.vertexDeclaration.Length)
                    {
                        declIndex = 0;
                    }
                    ParseMeshBuffer(meshShape.vertexDeclaration[declIndex], vertBufs, indexBuf, meshShape.IndexType, part.indexOffset, part.indexCount, meshShape.boneWeight, meshShape.Name);
                }

                if (blendIndexStats != null)
                {
                    if (MessageHandler.Instance.DebugLogsEnabled)
                    {
                        MessageHandler.Instance.AddMessage(
                            MessageType.LOG,
                            $"[Skin] Mesh={meshShape.Name} verts={blendIndexStats.VertexCount} jointsMax={blendIndexStats.MaxIndex} boneWeights={boneWeightCount} armBones={(armature?.Bones.Count ?? 0)}");
                    }
                }
            }

        }

        private class BlendIndexStats
        {
            public int VertexCount;
            public int MaxIndex;
        }

        private static float MapBlendIndex(float value, TRBoneWeight[] boneWeights)
        {
            int index = (int)MathF.Round(value);
            if (index >= 0 && index < boneWeights.Length)
            {
                int rigIndex = boneWeights[index].RigIndex;
                return rigIndex >= 0 ? rigIndex : value;
            }
            return value;
        }

        private static TRBuffer? GetVertexBuffer(TRBuffer[] buffers, int index)
        {
            if (buffers == null || index < 0 || index >= buffers.Length)
            {
                return null;
            }
            return buffers[index];
        }

        private static int GetStride(TRVertexDeclaration vertDesc, int sizeIndex)
        {
            if (vertDesc.vertexElementSizes == null || sizeIndex < 0 || sizeIndex >= vertDesc.vertexElementSizes.Length)
            {
                return 0;
            }
            return vertDesc.vertexElementSizes[sizeIndex].elementSize;
        }

        private static bool HasBytes(byte[] buffer, int offset, TRVertexFormat format)
        {
            int size = format switch
            {
                TRVertexFormat.X32_Y32_Z32_FLOAT => 12,
                TRVertexFormat.X32_Y32_FLOAT => 8,
                TRVertexFormat.W32_X32_Y32_Z32_FLOAT => 16,
                TRVertexFormat.W32_X32_Y32_Z32_UNSIGNED => 16,
                TRVertexFormat.W16_X16_Y16_Z16_FLOAT => 8,
                TRVertexFormat.W16_X16_Y16_Z16_UNSIGNED_NORMALIZED => 8,
                TRVertexFormat.R8_G8_B8_A8_UNSIGNED_NORMALIZED => 4,
                TRVertexFormat.W8_X8_Y8_Z8_UNSIGNED => 4,
                _ => 0
            };
            return size > 0 && offset >= 0 && offset + size <= buffer.Length;
        }

        private static void EnsureBlendStream(List<Vector4[]> streams, int index, int vertexCount)
        {
            while (streams.Count <= index)
            {
                streams.Add(new Vector4[vertexCount]);
            }
        }

        private static void CollapseBlendStreams(
            List<Vector4[]> indexStreams,
            List<Vector4[]> weightStreams,
            int streamCount,
            out Vector4[] collapsedIndices,
            out Vector4[] collapsedWeights)
        {
            int vertexCount = indexStreams[0].Length;
            collapsedIndices = new Vector4[vertexCount];
            collapsedWeights = new Vector4[vertexCount];

            for (int v = 0; v < vertexCount; v++)
            {
                var totals = new Dictionary<int, float>();

                for (int s = 0; s < streamCount; s++)
                {
                    var idx = indexStreams[s][v];
                    var w = weightStreams[s][v];
                    AccumulateInfluence(totals, (int)MathF.Round(idx.X), w.X);
                    AccumulateInfluence(totals, (int)MathF.Round(idx.Y), w.Y);
                    AccumulateInfluence(totals, (int)MathF.Round(idx.Z), w.Z);
                    AccumulateInfluence(totals, (int)MathF.Round(idx.W), w.W);
                }

                if (totals.Count == 0)
                {
                    collapsedIndices[v] = Vector4.Zero;
                    collapsedWeights[v] = Vector4.Zero;
                    continue;
                }

                var top = totals
                    .OrderByDescending(kv => kv.Value)
                    .Take(4)
                    .ToArray();

                float w0 = top.Length > 0 ? top[0].Value : 0f;
                float w1 = top.Length > 1 ? top[1].Value : 0f;
                float w2 = top.Length > 2 ? top[2].Value : 0f;
                float w3 = top.Length > 3 ? top[3].Value : 0f;

                collapsedIndices[v] = new Vector4(
                    top.Length > 0 ? top[0].Key : 0,
                    top.Length > 1 ? top[1].Key : 0,
                    top.Length > 2 ? top[2].Key : 0,
                    top.Length > 3 ? top[3].Key : 0);
                collapsedWeights[v] = new Vector4(w0, w1, w2, w3);
            }
        }

        private static void AccumulateInfluence(Dictionary<int, float> totals, int index, float weight)
        {
            if (weight <= 0f)
            {
                return;
            }

            if (totals.TryGetValue(index, out var current))
            {
                totals[index] = current + weight;
            }
            else
            {
                totals[index] = weight;
            }
        }

        private static Vector3 ReadVector3(byte[] buffer, int offset, TRVertexFormat format)
        {
            switch (format)
            {
                case TRVertexFormat.X32_Y32_Z32_FLOAT:
                    return new Vector3(BitConverter.ToSingle(buffer, offset), BitConverter.ToSingle(buffer, offset + 4), BitConverter.ToSingle(buffer, offset + 8));
                case TRVertexFormat.W32_X32_Y32_Z32_FLOAT:
                    return new Vector3(BitConverter.ToSingle(buffer, offset + 4), BitConverter.ToSingle(buffer, offset + 8), BitConverter.ToSingle(buffer, offset + 12));
                case TRVertexFormat.W16_X16_Y16_Z16_FLOAT:
                    return new Vector3(ReadHalf(buffer, offset), ReadHalf(buffer, offset + 2), ReadHalf(buffer, offset + 4));
                case TRVertexFormat.W16_X16_Y16_Z16_UNSIGNED_NORMALIZED:
                    return new Vector3(ReadUnorm16(buffer, offset), ReadUnorm16(buffer, offset + 2), ReadUnorm16(buffer, offset + 4));
                case TRVertexFormat.R8_G8_B8_A8_UNSIGNED_NORMALIZED:
                case TRVertexFormat.W8_X8_Y8_Z8_UNSIGNED:
                    return new Vector3(ReadUnorm8(buffer, offset), ReadUnorm8(buffer, offset + 1), ReadUnorm8(buffer, offset + 2));
                default:
                    return Vector3.Zero;
            }
        }

        private static Vector3 ReadNormal(byte[] buffer, int offset, TRVertexFormat format)
        {
            switch (format)
            {
                case TRVertexFormat.W16_X16_Y16_Z16_FLOAT:
                    return new Vector3(ReadHalf(buffer, offset), ReadHalf(buffer, offset + 2), ReadHalf(buffer, offset + 4));
                case TRVertexFormat.W16_X16_Y16_Z16_UNSIGNED_NORMALIZED:
                    return new Vector3(ReadSnorm16(buffer, offset), ReadSnorm16(buffer, offset + 2), ReadSnorm16(buffer, offset + 4));
                case TRVertexFormat.R8_G8_B8_A8_UNSIGNED_NORMALIZED:
                case TRVertexFormat.W8_X8_Y8_Z8_UNSIGNED:
                    return new Vector3(ReadSnorm8(buffer, offset), ReadSnorm8(buffer, offset + 1), ReadSnorm8(buffer, offset + 2));
                case TRVertexFormat.X32_Y32_Z32_FLOAT:
                    return new Vector3(BitConverter.ToSingle(buffer, offset), BitConverter.ToSingle(buffer, offset + 4), BitConverter.ToSingle(buffer, offset + 8));
                default:
                    return Vector3.UnitZ;
            }
        }

        private static Vector2 ReadVector2(byte[] buffer, int offset, TRVertexFormat format)
        {
            switch (format)
            {
                case TRVertexFormat.X32_Y32_FLOAT:
                    return new Vector2(BitConverter.ToSingle(buffer, offset), BitConverter.ToSingle(buffer, offset + 4));
                case TRVertexFormat.W16_X16_Y16_Z16_FLOAT:
                    return new Vector2(ReadHalf(buffer, offset), ReadHalf(buffer, offset + 2));
                case TRVertexFormat.W16_X16_Y16_Z16_UNSIGNED_NORMALIZED:
                    return new Vector2(ReadUnorm16(buffer, offset), ReadUnorm16(buffer, offset + 2));
                case TRVertexFormat.R8_G8_B8_A8_UNSIGNED_NORMALIZED:
                case TRVertexFormat.W8_X8_Y8_Z8_UNSIGNED:
                    return new Vector2(ReadUnorm8(buffer, offset), ReadUnorm8(buffer, offset + 1));
                default:
                    return Vector2.Zero;
            }
        }

        private static Vector4 ReadColor(byte[] buffer, int offset, TRVertexFormat format)
        {
            switch (format)
            {
                case TRVertexFormat.R8_G8_B8_A8_UNSIGNED_NORMALIZED:
                case TRVertexFormat.W8_X8_Y8_Z8_UNSIGNED:
                    return new Vector4(
                        ReadUnorm8(buffer, offset),
                        ReadUnorm8(buffer, offset + 1),
                        ReadUnorm8(buffer, offset + 2),
                        ReadUnorm8(buffer, offset + 3));
                case TRVertexFormat.W16_X16_Y16_Z16_UNSIGNED_NORMALIZED:
                    return new Vector4(
                        ReadUnorm16(buffer, offset),
                        ReadUnorm16(buffer, offset + 2),
                        ReadUnorm16(buffer, offset + 4),
                        ReadUnorm16(buffer, offset + 6));
                case TRVertexFormat.W16_X16_Y16_Z16_FLOAT:
                    return new Vector4(
                        ReadHalf(buffer, offset),
                        ReadHalf(buffer, offset + 2),
                        ReadHalf(buffer, offset + 4),
                        ReadHalf(buffer, offset + 6));
                default:
                    return Vector4.One;
            }
        }

        private static Vector4 ReadTangent(byte[] buffer, int offset, TRVertexFormat format)
        {
            switch (format)
            {
                case TRVertexFormat.W32_X32_Y32_Z32_FLOAT:
                    return new Vector4(
                        BitConverter.ToSingle(buffer, offset + 4),
                        BitConverter.ToSingle(buffer, offset + 8),
                        BitConverter.ToSingle(buffer, offset + 12),
                        BitConverter.ToSingle(buffer, offset));
                case TRVertexFormat.X32_Y32_Z32_FLOAT:
                    return new Vector4(
                        BitConverter.ToSingle(buffer, offset),
                        BitConverter.ToSingle(buffer, offset + 4),
                        BitConverter.ToSingle(buffer, offset + 8),
                        1f);
                case TRVertexFormat.W16_X16_Y16_Z16_FLOAT:
                    return new Vector4(
                        ReadHalf(buffer, offset),
                        ReadHalf(buffer, offset + 2),
                        ReadHalf(buffer, offset + 4),
                        ReadHalf(buffer, offset + 6));
                case TRVertexFormat.W16_X16_Y16_Z16_UNSIGNED_NORMALIZED:
                    return new Vector4(
                        ReadSnorm16(buffer, offset),
                        ReadSnorm16(buffer, offset + 2),
                        ReadSnorm16(buffer, offset + 4),
                        ReadSnorm16(buffer, offset + 6));
                case TRVertexFormat.R8_G8_B8_A8_UNSIGNED_NORMALIZED:
                case TRVertexFormat.W8_X8_Y8_Z8_UNSIGNED:
                    return new Vector4(
                        ReadSnorm8(buffer, offset),
                        ReadSnorm8(buffer, offset + 1),
                        ReadSnorm8(buffer, offset + 2),
                        ReadSnorm8(buffer, offset + 3));
                default:
                    return new Vector4(1f, 0f, 0f, 1f);
            }
        }

        private static Vector4 ReadBlendIndices(byte[] buffer, int offset, TRVertexFormat format)
        {
            switch (format)
            {
                case TRVertexFormat.W8_X8_Y8_Z8_UNSIGNED:
                    return new Vector4(
                        buffer[offset + 1],
                        buffer[offset + 2],
                        buffer[offset + 3],
                        buffer[offset]);
                case TRVertexFormat.W16_X16_Y16_Z16_UNSIGNED_NORMALIZED:
                    return new Vector4(
                        BitConverter.ToUInt16(buffer, offset + 2),
                        BitConverter.ToUInt16(buffer, offset + 4),
                        BitConverter.ToUInt16(buffer, offset + 6),
                        BitConverter.ToUInt16(buffer, offset));
                case TRVertexFormat.W32_X32_Y32_Z32_UNSIGNED:
                    return new Vector4(
                        BitConverter.ToUInt32(buffer, offset + 4),
                        BitConverter.ToUInt32(buffer, offset + 8),
                        BitConverter.ToUInt32(buffer, offset + 12),
                        BitConverter.ToUInt32(buffer, offset));
                case TRVertexFormat.W32_X32_Y32_Z32_FLOAT:
                    return new Vector4(
                        BitConverter.ToSingle(buffer, offset + 4),
                        BitConverter.ToSingle(buffer, offset + 8),
                        BitConverter.ToSingle(buffer, offset + 12),
                        BitConverter.ToSingle(buffer, offset));
                default:
                    return Vector4.Zero;
            }
        }

        private static Vector4 ReadBlendWeights(byte[] buffer, int offset, TRVertexFormat format)
        {
            switch (format)
            {
                case TRVertexFormat.W16_X16_Y16_Z16_UNSIGNED_NORMALIZED:
                    return new Vector4(
                        ReadUnorm16(buffer, offset + 2),
                        ReadUnorm16(buffer, offset + 4),
                        ReadUnorm16(buffer, offset + 6),
                        ReadUnorm16(buffer, offset));
                case TRVertexFormat.R8_G8_B8_A8_UNSIGNED_NORMALIZED:
                case TRVertexFormat.W8_X8_Y8_Z8_UNSIGNED:
                    return new Vector4(
                        ReadUnorm8(buffer, offset + 1),
                        ReadUnorm8(buffer, offset + 2),
                        ReadUnorm8(buffer, offset + 3),
                        ReadUnorm8(buffer, offset));
                case TRVertexFormat.W32_X32_Y32_Z32_FLOAT:
                    return new Vector4(
                        BitConverter.ToSingle(buffer, offset + 4),
                        BitConverter.ToSingle(buffer, offset + 8),
                        BitConverter.ToSingle(buffer, offset + 12),
                        BitConverter.ToSingle(buffer, offset));
                default:
                    return Vector4.Zero;
            }
        }

        private static float ReadHalf(byte[] buffer, int offset)
        {
            ushort raw = BitConverter.ToUInt16(buffer, offset);
            return (float)BitConverter.UInt16BitsToHalf(raw);
        }

        private static float ReadUnorm16(byte[] buffer, int offset)
        {
            return BitConverter.ToUInt16(buffer, offset) / 65535f;
        }

        private static float ReadSnorm16(byte[] buffer, int offset)
        {
            return (BitConverter.ToUInt16(buffer, offset) / 65535f) * 2f - 1f;
        }

        private static float ReadUnorm8(byte[] buffer, int offset)
        {
            return buffer[offset] / 255f;
        }

        private static float ReadSnorm8(byte[] buffer, int offset)
        {
            return (buffer[offset] / 255f) * 2f - 1f;
        }

        private void ParseMaterial(string file)
        {
            List<Material> matlist = new List<Material>();
            var materialPath = new PathString(file);

            TRMTR? trmtrFallback = null;
            try
            {
                trmtrFallback = FlatBufferConverter.DeserializeFrom<TRMTR>(file);
            }
            catch
            {
                trmtrFallback = null;
            }

            Dictionary<string, TRMaterial> trmtrByName = new Dictionary<string, TRMaterial>(StringComparer.OrdinalIgnoreCase);
            if (trmtrFallback?.Materials != null)
            {
                foreach (var mat in trmtrFallback.Materials)
                {
                    if (!string.IsNullOrEmpty(mat?.Name))
                    {
                        trmtrByName[mat.Name] = mat;
                    }
                }
            }

            var gfxMaterials = FlatBufferConverter.DeserializeFrom<Trinity.Core.Flatbuffers.Gfx2.Material>(file);
            if (gfxMaterials?.ItemList != null && gfxMaterials.ItemList.Length > 0)
            {
                foreach (var item in gfxMaterials.ItemList)
                {
                    var shaderName = item?.TechniqueList?.FirstOrDefault()?.Name ?? "Standard";
                    var shaderParams = new List<TRStringParameter>();

                    if (item?.TechniqueList != null)
                    {
                        foreach (var technique in item.TechniqueList)
                        {
                            if (technique?.ShaderOptions == null) continue;
                            foreach (var opt in technique.ShaderOptions)
                            {
                                if (opt == null) continue;
                                shaderParams.Add(new TRStringParameter { Name = opt.Name, Value = opt.Choice });
                            }
                        }
                    }

                    if (item?.IntParamList != null)
                    {
                        foreach (var p in item.IntParamList)
                        {
                            if (p == null) continue;
                            shaderParams.Add(new TRStringParameter { Name = p.Name, Value = p.Value.ToString() });
                        }
                    }

                    var textures = item?.TextureParamList?
                        .Select(t => new TRTexture
                        {
                            Name = t.Name,
                            File = t.FilePath,
                            Slot = (uint)Math.Max(0, t.SamplerId)
                        })
                        .ToArray() ?? Array.Empty<TRTexture>();

                    var trmat = new TRMaterial
                    {
                        Name = item?.Name ?? "Material",
                        Shader = new[] { new TRMaterialShader { Name = shaderName, Values = shaderParams.ToArray() } },
                        Textures = textures,
                        FloatParams = item?.FloatParamList?
                            .Select(p => new TRFloatParameter { Name = p.Name, Value = p.Value })
                            .ToArray(),
                        Vec2fParams = item?.Vector2fParamList?
                            .Select(p => new TRVec2fParameter { Name = p.Name, Value = p.Value })
                            .ToArray(),
                        Vec3fParams = item?.Vector3fParamList?
                            .Select(p => new TRVec3fParameter { Name = p.Name, Value = p.Value })
                            .ToArray(),
                        Vec4fParams = item?.Vector4fParamList?
                            .Select(p => new TRVec4fParameter { Name = p.Name, Value = p.Value })
                            .ToArray(),
                    };

                    if (trmtrByName.TryGetValue(trmat.Name, out var fallbackMat))
                    {
                        trmat.Samplers = fallbackMat.Samplers;
                    }

                    matlist.Add(new Material(materialPath, trmat));
                }

                materials = matlist.ToArray();
                BuildMaterialMap();
                return;
            }

            var mats = FlatBufferConverter.DeserializeFrom<TRMTR>(file);
            foreach (var mat in mats.Materials)
            {
                matlist.Add(new Material(materialPath, mat));
            }
            materials = matlist.ToArray();
            BuildMaterialMap();
        }

        public IReadOnlyList<Material> GetMaterials()
        {
            return materials ?? Array.Empty<Material>();
        }

        public Armature? GetArmature()
        {
            return armature;
        }

        public IReadOnlyList<string> GetSubmeshNames()
        {
            return SubmeshNames;
        }

        public IReadOnlyList<string> GetSubmeshMaterials()
        {
            return MaterialNames;
        }

        public IReadOnlyList<UvSet> GetUvSetsForMaterial(string materialName)
        {
            var result = new List<UvSet>();
            if (string.IsNullOrWhiteSpace(materialName))
            {
                return result;
            }

            var count = Math.Min(MaterialNames.Count, Math.Min(UVs.Count, Indices.Count));
            for (int i = 0; i < count; i++)
            {
                if (MatchesMaterial(MaterialNames[i], materialName))
                {
                    var submeshName = i < SubmeshNames.Count ? SubmeshNames[i] : $"Submesh {i}";
                    result.Add(new UvSet(UVs[i], Indices[i], submeshName));
                }
            }

            return result;
        }

        private static bool MatchesMaterial(string name, string target)
        {
            if (string.Equals(name, target, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(target))
            {
                return false;
            }

            if (name.Contains(':'))
            {
                name = name.Split(':')[0];
            }

            if (target.Contains(':'))
            {
                target = target.Split(':')[0];
            }

            return name.StartsWith(target, StringComparison.OrdinalIgnoreCase) ||
                   target.StartsWith(name, StringComparison.OrdinalIgnoreCase);
        }

        private void BuildMaterialMap()
        {
            materialMap.Clear();
            if (materials == null) return;
            foreach (var mat in materials)
            {
                if (mat == null || string.IsNullOrEmpty(mat.Name)) continue;
                if (!materialMap.ContainsKey(mat.Name))
                {
                    materialMap.Add(mat.Name, mat);
                }
            }
        }

        private void ParseArmature(string file)
        {
            var skel = FlatBufferConverter.DeserializeFrom<TRSKL>(file);
            var merged = TryLoadAndMergeBaseSkeleton(skel, file, baseSkeletonCategoryHint);
            armature = new Armature(merged ?? skel, file);
            ApplyBlendIndexMapping(
                RenderOptions.MapBlendIndicesViaJointInfo,
                RenderOptions.MapBlendIndicesViaSkinningPalette,
                RenderOptions.MapBlendIndicesViaBoneMeta,
                RenderOptions.AutoMapBlendIndices);
        }

        private TRSKL? TryLoadAndMergeBaseSkeleton(TRSKL localSkel, string localSkelPath, string? category)
        {
            if (localSkel == null || string.IsNullOrWhiteSpace(localSkelPath) || string.IsNullOrWhiteSpace(category))
            {
                return null;
            }

            var localDir = Path.GetDirectoryName(localSkelPath);
            if (string.IsNullOrWhiteSpace(localDir))
            {
                return null;
            }

            var basePath = ResolveBaseTrsklPath(localDir, category);
            if (string.IsNullOrWhiteSpace(basePath) || !File.Exists(basePath))
            {
                return null;
            }

            try
            {
                var baseSkel = FlatBufferConverter.DeserializeFrom<TRSKL>(basePath);
                var merged = MergeBaseAndLocalSkeletons(baseSkel, localSkel);
                if (MessageHandler.Instance.DebugLogsEnabled)
                {
                    MessageHandler.Instance.AddMessage(
                        MessageType.LOG,
                        $"[TRSKL] baseMerge category={category} base='{basePath}' local='{localSkelPath}' nodes={baseSkel.TransformNodes.Length}+{localSkel.TransformNodes.Length} joints={baseSkel.JointInfos.Length}+{localSkel.JointInfos.Length}");
                }
                return merged;
            }
            catch (Exception ex)
            {
                if (MessageHandler.Instance.DebugLogsEnabled)
                {
                    MessageHandler.Instance.AddMessage(
                        MessageType.WARNING,
                        $"[TRSKL] baseMerge failed category={category} base='{basePath}' local='{localSkelPath}': {ex.Message}");
                }
                return null;
            }
        }

        private static string? ResolveBaseTrsklPath(string modelDir, string category)
        {
            // Known base skeleton search paths (SVProtag renamed to Protag).
            string[] rels = category switch
            {
                "Protag" => new[]
                {
                    "../../model_pc_base/model/p0_base.trskl",
                    "../../../../p2/model/base/p2_base0001_00_default/p2_base0001_00_default.trskl",
                    "../../p2/p2_base0001_00_default/p2_base0001_00_default.trskl"
                },
                "CommonNPCbu" => new[] { "../../../model_cc_base/bu/bu_base.trskl", "../../base/cc_base0001_00_young_m/cc_base0001_00_young_m.trskl" },
                "CommonNPCdm" or "CommonNPCdf" => new[] { "../../../model_cc_base/dm/dm_base.trskl", "../../base/cc_base0001_00_young_m/cc_base0001_00_young_m.trskl" },
                "CommonNPCem" => new[] { "../../../model_cc_base/em/em_base.trskl", "../../base/cc_base0001_00_young_m/cc_base0001_00_young_m.trskl" },
                "CommonNPCfm" or "CommonNPCff" => new[] { "../../../model_cc_base/fm/fm_base.trskl", "../../base/cc_base0001_00_young_m/cc_base0001_00_young_m.trskl" },
                "CommonNPCgm" or "CommonNPCgf" => new[] { "../../../model_cc_base/gm/gm_base.trskl", "../../base/cc_base0001_00_young_m/cc_base0001_00_young_m.trskl" },
                "CommonNPCrv" => new[] { "../../../model_cc_base/rv/rv_base.trskl", "../../base/cc_base0001_00_young_m/cc_base0001_00_young_m.trskl" },
                _ => Array.Empty<string>()
            };

            foreach (var rel in rels)
            {
                var full = Path.GetFullPath(Path.Combine(modelDir, rel));
                if (File.Exists(full))
                {
                    return full;
                }
            }

            return null;
        }

        private static TRSKL MergeBaseAndLocalSkeletons(TRSKL baseSkel, TRSKL localSkel)
        {
            // Merge strategy for the TRSKL flavor with `node_list` and `joint_info_list`.
            // Local nodes and joint infos are appended after the base skeleton.
            // ParentNodeName is resolved to a base node index when present.
            // ParentNodeIndex is treated as local space and is offset by the base node count otherwise.
            // JointInfoIndex is offset by the base joint count.
            int baseNodeCount = baseSkel.TransformNodes?.Length ?? 0;
            int baseJointCount = baseSkel.JointInfos?.Length ?? 0;

            var mergedNodes = new List<TRTransformNode>(baseNodeCount + (localSkel.TransformNodes?.Length ?? 0));
            var mergedJoints = new List<TRJointInfo>(baseJointCount + (localSkel.JointInfos?.Length ?? 0));

            var baseIndexByName = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            if (baseSkel.TransformNodes != null)
            {
                for (int i = 0; i < baseSkel.TransformNodes.Length; i++)
                {
                    var n = baseSkel.TransformNodes[i];
                    mergedNodes.Add(n);
                    if (!string.IsNullOrWhiteSpace(n?.Name))
                    {
                        baseIndexByName[n.Name] = i;
                    }
                }
            }

            if (baseSkel.JointInfos != null)
            {
                mergedJoints.AddRange(baseSkel.JointInfos);
            }

            if (localSkel.JointInfos != null)
            {
                mergedJoints.AddRange(localSkel.JointInfos);
            }

            if (localSkel.TransformNodes != null)
            {
                for (int i = 0; i < localSkel.TransformNodes.Length; i++)
                {
                    var node = localSkel.TransformNodes[i];
                    if (node == null)
                    {
                        continue;
                    }

                    int parentIndex = node.ParentNodeIndex;
                    string parentName = node.ParentNodeName ?? string.Empty;
                    if (!string.IsNullOrWhiteSpace(parentName) && baseIndexByName.TryGetValue(parentName, out int baseParent))
                    {
                        parentIndex = baseParent;
                    }
                    else if (parentIndex >= 0)
                    {
                        parentIndex = parentIndex + baseNodeCount;
                    }

                    int jointIndex = node.JointInfoIndex;
                    if (jointIndex >= 0)
                    {
                        jointIndex = jointIndex + baseJointCount;
                    }

                    mergedNodes.Add(new TRTransformNode
                    {
                        Name = node.Name,
                        Transform = node.Transform,
                        ScalePivot = node.ScalePivot,
                        RotatePivot = node.RotatePivot,
                        ParentNodeIndex = parentIndex,
                        JointInfoIndex = jointIndex,
                        ParentNodeName = node.ParentNodeName,
                        Priority = node.Priority,
                        PriorityPass = node.PriorityPass,
                        IgnoreParentRotation = node.IgnoreParentRotation
                    });
                }
            }

            return new TRSKL
            {
                Version = baseSkel.Version != 0 ? baseSkel.Version : localSkel.Version,
                TransformNodes = mergedNodes.ToArray(),
                JointInfos = mergedJoints.ToArray(),
                HelperBones = baseSkel.HelperBones?.Length > 0 ? baseSkel.HelperBones : (localSkel.HelperBones ?? Array.Empty<TRHelperBoneInfo>()),
                SkinningPaletteOffset = baseJointCount,
                IsInteriorMap = baseSkel.IsInteriorMap || localSkel.IsInteriorMap
            };
        }

        private void ApplyBlendIndexMapping(bool useJointInfo, bool useSkinPalette, bool useBoneMeta, bool autoMap)
        {
            if (armature == null)
            {
                return;
            }

            var skinPalette = useSkinPalette ? armature.BuildSkinningPalette() : Array.Empty<int>();

            for (int i = 0; i < BlendIndiciesOriginal.Count; i++)
            {
                var source = BlendIndiciesOriginal[i];
                var boneWeights = i < BlendBoneWeights.Count ? BlendBoneWeights[i] : null;
                var mapped = new Vector4[source.Length];
                int maxIndexBefore = GetMaxIndex(source);

                bool canRemapViaBoneWeights = false;
                if (boneWeights != null && boneWeights.Length > 0 && maxIndexBefore < boneWeights.Length)
                {
                    int outOfRangeWeights = 0;
                    int sampleCount = Math.Min(source.Length, 512);
                    for (int v = 0; v < sampleCount; v++)
                    {
                        var idx = source[v];
                        CountOutOfRange(boneWeights, (int)MathF.Round(idx.X), ref outOfRangeWeights);
                        CountOutOfRange(boneWeights, (int)MathF.Round(idx.Y), ref outOfRangeWeights);
                        CountOutOfRange(boneWeights, (int)MathF.Round(idx.Z), ref outOfRangeWeights);
                        CountOutOfRange(boneWeights, (int)MathF.Round(idx.W), ref outOfRangeWeights);
                    }
                    canRemapViaBoneWeights = outOfRangeWeights == 0;
                }

                var mode = SelectBlendIndexRemapMode(
                    i,
                    canRemapViaBoneWeights,
                    boneWeights,
                    maxIndexBefore,
                    useJointInfo,
                    useSkinPalette,
                    useBoneMeta,
                    autoMap,
                    skinPalette);

                for (int v = 0; v < source.Length; v++)
                {
                    var idx = source[v];
                    if (mode == BlendIndexRemapMode.BoneWeights && boneWeights != null)
                    {
                        idx = new Vector4(
                            MapBlendIndex(idx.X, boneWeights),
                            MapBlendIndex(idx.Y, boneWeights),
                            MapBlendIndex(idx.Z, boneWeights),
                            MapBlendIndex(idx.W, boneWeights));
                    }

                    if (mode == BlendIndexRemapMode.JointInfo)
                    {
                        mapped[v] = new Vector4(
                            (int)MathF.Round(idx.X) >= 0 && (int)MathF.Round(idx.X) < armature.JointInfoCount ? armature.MapJointInfoIndex((int)MathF.Round(idx.X)) : idx.X,
                            (int)MathF.Round(idx.Y) >= 0 && (int)MathF.Round(idx.Y) < armature.JointInfoCount ? armature.MapJointInfoIndex((int)MathF.Round(idx.Y)) : idx.Y,
                            (int)MathF.Round(idx.Z) >= 0 && (int)MathF.Round(idx.Z) < armature.JointInfoCount ? armature.MapJointInfoIndex((int)MathF.Round(idx.Z)) : idx.Z,
                            (int)MathF.Round(idx.W) >= 0 && (int)MathF.Round(idx.W) < armature.JointInfoCount ? armature.MapJointInfoIndex((int)MathF.Round(idx.W)) : idx.W);
                    }
                    else if (mode == BlendIndexRemapMode.SkinningPalette)
                    {
                        int ix = (int)MathF.Round(idx.X);
                        int iy = (int)MathF.Round(idx.Y);
                        int iz = (int)MathF.Round(idx.Z);
                        int iw = (int)MathF.Round(idx.W);
                        mapped[v] = new Vector4(
                            ix >= 0 && ix < skinPalette.Length ? skinPalette[ix] : idx.X,
                            iy >= 0 && iy < skinPalette.Length ? skinPalette[iy] : idx.Y,
                            iz >= 0 && iz < skinPalette.Length ? skinPalette[iz] : idx.Z,
                            iw >= 0 && iw < skinPalette.Length ? skinPalette[iw] : idx.W);
                    }
                    else if (mode == BlendIndexRemapMode.BoneMeta)
                    {
                        mapped[v] = new Vector4(
                            (int)MathF.Round(idx.X) >= 0 && (int)MathF.Round(idx.X) < armature.BoneMetaCount ? armature.MapBoneMetaIndex((int)MathF.Round(idx.X)) : idx.X,
                            (int)MathF.Round(idx.Y) >= 0 && (int)MathF.Round(idx.Y) < armature.BoneMetaCount ? armature.MapBoneMetaIndex((int)MathF.Round(idx.Y)) : idx.Y,
                            (int)MathF.Round(idx.Z) >= 0 && (int)MathF.Round(idx.Z) < armature.BoneMetaCount ? armature.MapBoneMetaIndex((int)MathF.Round(idx.Z)) : idx.Z,
                            (int)MathF.Round(idx.W) >= 0 && (int)MathF.Round(idx.W) < armature.BoneMetaCount ? armature.MapBoneMetaIndex((int)MathF.Round(idx.W)) : idx.W);
                    }
                    else
                    {
                        mapped[v] = idx;
                    }
                }

                BlendIndicies[i] = mapped;
                UpdateBlendIndicesBuffer(i);
            }
        }

        private BlendIndexRemapMode SelectBlendIndexRemapMode(
            int submeshIndex,
            bool canRemapViaBoneWeights,
            TRBoneWeight[]? boneWeights,
            int maxIndexBefore,
            bool useJointInfo,
            bool useSkinPalette,
            bool useBoneMeta,
            bool autoMap,
            int[] skinPalette)
        {
            if (armature == null)
            {
                return BlendIndexRemapMode.None;
            }

            if (canRemapViaBoneWeights && boneWeights != null)
            {
                return BlendIndexRemapMode.BoneWeights;
            }

            bool canMapJointInfo = useJointInfo && armature.JointInfoCount > 0;
            bool canMapSkinPalette = useSkinPalette && skinPalette.Length > 0;
            bool canMapBoneMeta = useBoneMeta && armature.BoneMetaCount > 0;

            if (!autoMap)
            {
                if (canMapJointInfo) return BlendIndexRemapMode.JointInfo;
                if (canMapSkinPalette) return BlendIndexRemapMode.SkinningPalette;
                if (canMapBoneMeta) return BlendIndexRemapMode.BoneMeta;
                return BlendIndexRemapMode.None;
            }

            // Heuristic: if indices live in joint info space (common when Bones.Count > JointInfoCount),
            // mapping is required but can be indistinguishable from "None" by out of range scoring
            // because bind pose looks correct for any indices when all skin mats are identity.
            //
            // Joint info mapping is preferred when indices fit in joint info count,
            // bone count is much larger than joint info count,
            // and mapping is not a trivial identity map for the observed range.
            if (armature.JointInfoCount > 0 &&
                maxIndexBefore >= 0 &&
                maxIndexBefore < armature.JointInfoCount &&
                (armature.Bones.Count - armature.JointInfoCount) >= 16)
            {
                bool mappingIsIdentity = true;
                int sampleMax = Math.Min(maxIndexBefore, Math.Min(armature.JointInfoCount - 1, 64));
                for (int i = 0; i <= sampleMax; i++)
                {
                    if (armature.MapJointInfoIndex(i) != i)
                    {
                        mappingIsIdentity = false;
                        break;
                    }
                }

                if (!mappingIsIdentity)
                {
                    return BlendIndexRemapMode.JointInfo;
                }
            }

            // Auto mode tries each applicable mapping and picks the one with the fewest
            // out of range and non influencer indices (weights ignore unused channels).
            var source = BlendIndiciesOriginal[submeshIndex];
            var weights = submeshIndex < BlendWeights.Count ? BlendWeights[submeshIndex] : null;

            (int outOfRange, int nonInfluencer) bestScore = ScoreBlendIndexMapping(source, weights, BlendIndexRemapMode.None, boneWeights, skinPalette);
            BlendIndexRemapMode bestMode = BlendIndexRemapMode.None;

            void consider(BlendIndexRemapMode candidate)
            {
                var score = ScoreBlendIndexMapping(source, weights, candidate, boneWeights, skinPalette);
                if (score.outOfRange < bestScore.outOfRange ||
                    (score.outOfRange == bestScore.outOfRange && score.nonInfluencer < bestScore.nonInfluencer))
                {
                    bestScore = score;
                    bestMode = candidate;
                }
            }

            if (armature.JointInfoCount > 0) consider(BlendIndexRemapMode.JointInfo);
            if (skinPalette.Length > 0) consider(BlendIndexRemapMode.SkinningPalette);
            if (armature.BoneMetaCount > 0) consider(BlendIndexRemapMode.BoneMeta);

            // Tie breaker prefers mappings over None when scores are identical, since "None" can
            // look correct in bind pose even if indices are in the wrong index space.
            if (bestMode == BlendIndexRemapMode.None)
            {
                var jointScore = armature.JointInfoCount > 0 ? ScoreBlendIndexMapping(source, weights, BlendIndexRemapMode.JointInfo, boneWeights, skinPalette) : (int.MaxValue, int.MaxValue);
                if (jointScore == bestScore)
                {
                    bestMode = BlendIndexRemapMode.JointInfo;
                }
                else if (skinPalette.Length > 0)
                {
                    var palScore = ScoreBlendIndexMapping(source, weights, BlendIndexRemapMode.SkinningPalette, boneWeights, skinPalette);
                    if (palScore == bestScore)
                    {
                        bestMode = BlendIndexRemapMode.SkinningPalette;
                    }
                }
            }

            if (MessageHandler.Instance.DebugLogsEnabled)
            {
                string meshName = submeshIndex < BlendMeshNames.Count ? BlendMeshNames[submeshIndex] : $"Submesh {submeshIndex}";
                MessageHandler.Instance.AddMessage(
                    MessageType.LOG,
                    $"[Skin] Remap pick mesh={meshName} maxIndex={maxIndexBefore} boneWeights={(boneWeights?.Length ?? 0)} jointInfo={armature.JointInfoCount} palette={skinPalette.Length} boneMeta={armature.BoneMetaCount} mode={bestMode} score=(oor={bestScore.outOfRange}, nonInfluencer={bestScore.nonInfluencer})");
            }

            return bestMode;
        }

        private (int outOfRange, int nonInfluencer) ScoreBlendIndexMapping(
            Vector4[] indices,
            Vector4[]? weights,
            BlendIndexRemapMode mode,
            TRBoneWeight[]? boneWeights,
            int[] skinPalette)
        {
            if (armature == null || indices == null || indices.Length == 0)
            {
                return (0, 0);
            }

            int outOfRange = 0;
            int nonInfluencer = 0;
            int sampleCount = Math.Min(indices.Length, 2048);

            for (int v = 0; v < sampleCount; v++)
            {
                var idx = indices[v];
                var w = weights != null && v < weights.Length ? weights[v] : Vector4.One;

                ScoreComponent(idx.X, w.X);
                ScoreComponent(idx.Y, w.Y);
                ScoreComponent(idx.Z, w.Z);
                ScoreComponent(idx.W, w.W);
            }

            return (outOfRange, nonInfluencer);

            void ScoreComponent(float value, float weight)
            {
                if (weight <= 0.0001f)
                {
                    return;
                }

                int mapped = MapBlendIndexComponent(value, mode, boneWeights, skinPalette);
                if (mapped < 0 || mapped >= armature.Bones.Count)
                {
                    outOfRange++;
                    return;
                }

                if (!armature.Bones[mapped].Skinning)
                {
                    nonInfluencer++;
                }
            }
        }

        private int MapBlendIndexComponent(float value, BlendIndexRemapMode mode, TRBoneWeight[]? boneWeights, int[] skinPalette)
        {
            if (armature == null)
            {
                return 0;
            }

            int index = (int)MathF.Round(value);
            if (index < 0)
            {
                return index;
            }

            switch (mode)
            {
                case BlendIndexRemapMode.BoneWeights:
                    if (boneWeights == null || index >= boneWeights.Length)
                    {
                        return index;
                    }
                    return boneWeights[index].RigIndex;
                case BlendIndexRemapMode.JointInfo:
                    if (index >= armature.JointInfoCount)
                    {
                        return index;
                    }
                    return armature.MapJointInfoIndex(index);
                case BlendIndexRemapMode.SkinningPalette:
                    if (skinPalette == null || index >= skinPalette.Length)
                    {
                        return index;
                    }
                    return skinPalette[index];
                case BlendIndexRemapMode.BoneMeta:
                    if (index >= armature.BoneMetaCount)
                    {
                        return index;
                    }
                    return armature.MapBoneMetaIndex(index);
                default:
                    return index;
            }
        }

        private void UpdateBlendIndicesBuffer(int submeshIndex)
        {
            if (VBOs == null || submeshIndex < 0 || submeshIndex >= VBOs.Length ||
                blendIndexOffsets == null || blendIndexByteSizes == null ||
                submeshIndex >= blendIndexOffsets.Length || submeshIndex >= blendIndexByteSizes.Length)
            {
                return;
            }

            var indices = BlendIndicies[submeshIndex];
            GL.BindBuffer(BufferTarget.ArrayBuffer, VBOs[submeshIndex]);
            GL.BufferSubData(BufferTarget.ArrayBuffer, (IntPtr)blendIndexOffsets[submeshIndex], blendIndexByteSizes[submeshIndex], indices.SelectMany(x => x.ToBytes()).ToArray());
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        }

        private static int GetMaxIndex(Vector4[] indices)
        {
            int maxIndex = 0;
            for (int v = 0; v < indices.Length; v++)
            {
                var idx = indices[v];
                maxIndex = Math.Max(maxIndex, (int)MathF.Max(MathF.Max(idx.X, idx.Y), MathF.Max(idx.Z, idx.W)));
            }
            return maxIndex;
        }

        private static void CountOutOfRange(TRBoneWeight[] boneWeights, int index, ref int outOfRange)
        {
            if (index < 0 || index >= boneWeights.Length)
            {
                outOfRange++;
            }
        }

        public override void Setup()
        {
            var submeshCnt = Positions.Count;
            VAOs = new int[submeshCnt];

            VBOs = new int[submeshCnt];
            EBOs = new int[Indices.Count()];
            blendIndexOffsets = new int[submeshCnt];
            blendIndexByteSizes = new int[submeshCnt];

            for (int i = 0; i < submeshCnt; i++)
            {
                // VAO
                GL.GenVertexArrays(1, out VAOs[i]);
                GL.BindVertexArray(VAOs[i]);

                // Sizes
                var vertSize = Positions[i].Length * Vector3.SizeInBytes;
                var normSize = Normals[i].Length * Vector3.SizeInBytes;
                var uvSize = UVs[i].Length * Vector2.SizeInBytes;
                var colorSize = Colors[i].Length * Vector4.SizeInBytes;
                var tangentSize = Tangents[i].Length * Vector4.SizeInBytes;
                var binormalSize = Binormals[i].Length * Vector3.SizeInBytes;
                var blendIndexSize = BlendIndicies[i].Length * Vector4.SizeInBytes;
                var blendWeightSize = BlendWeights[i].Length * Vector4.SizeInBytes;
                var totalSize = vertSize + normSize + uvSize + colorSize + tangentSize + binormalSize + blendIndexSize + blendWeightSize;

                blendIndexOffsets[i] = vertSize + normSize + uvSize + colorSize + tangentSize + binormalSize;
                blendIndexByteSizes[i] = blendIndexSize;

                //VBO
                GL.GenBuffers(1, out VBOs[i]);
                GL.BindBuffer(BufferTarget.ArrayBuffer, VBOs[i]);
                GL.BufferData(BufferTarget.ArrayBuffer, totalSize, IntPtr.Zero, BufferUsageHint.StaticDraw);

                //Upload vertex data to the buffer
                IntPtr offset = IntPtr.Zero;
                GL.BufferSubData(BufferTarget.ArrayBuffer, offset, vertSize, Positions[i].SelectMany(x => x.ToBytes()).ToArray()); offset += vertSize;          // Verts
                GL.BufferSubData(BufferTarget.ArrayBuffer, offset, normSize, Normals[i].SelectMany(x => x.ToBytes()).ToArray()); offset += normSize;            // Normals
                GL.BufferSubData(BufferTarget.ArrayBuffer, offset, uvSize, UVs[i].SelectMany(x => x.ToBytes()).ToArray()); offset += uvSize;                    // TexCoords
                GL.BufferSubData(BufferTarget.ArrayBuffer, offset, colorSize, Colors[i].SelectMany(x => x.ToBytes()).ToArray()); offset += colorSize;          // Colors
                GL.BufferSubData(BufferTarget.ArrayBuffer, offset, tangentSize, Tangents[i].SelectMany(x => x.ToBytes()).ToArray()); offset += tangentSize;    // Tangents
                GL.BufferSubData(BufferTarget.ArrayBuffer, offset, binormalSize, Binormals[i].SelectMany(x => x.ToBytes()).ToArray()); offset += binormalSize; // Binormals
                GL.BufferSubData(BufferTarget.ArrayBuffer, offset, blendIndexSize, BlendIndicies[i].SelectMany(x => x.ToBytes()).ToArray()); offset += blendIndexSize; // Blend indices
                GL.BufferSubData(BufferTarget.ArrayBuffer, offset, blendWeightSize, BlendWeights[i].SelectMany(x => x.ToBytes()).ToArray()); offset += blendWeightSize; // Blend weights

                // EBO (indices)
                GL.GenBuffers(1, out EBOs[i]);
                GL.BindBuffer(BufferTarget.ElementArrayBuffer, EBOs[i]);
                GL.BufferData(BufferTarget.ElementArrayBuffer, Indices[i].Length * sizeof(uint), Indices[i].ToArray(), BufferUsageHint.StaticDraw);

                offset = IntPtr.Zero;

                // Pos attribute
                GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, Vector3.SizeInBytes, offset); offset += vertSize;
                GL.EnableVertexAttribArray(0);

                // Norm attribute
                GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, Vector3.SizeInBytes, offset); offset += normSize;
                GL.EnableVertexAttribArray(1);

                // UV attribute
                GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, Vector2.SizeInBytes, offset); offset += uvSize;
                GL.EnableVertexAttribArray(2);

                // Color attribute
                GL.VertexAttribPointer(3, 4, VertexAttribPointerType.Float, false, Vector4.SizeInBytes, offset); offset += colorSize;
                GL.EnableVertexAttribArray(3);

                // Tangent attribute
                GL.VertexAttribPointer(4, 4, VertexAttribPointerType.Float, false, Vector4.SizeInBytes, offset); offset += tangentSize;
                GL.EnableVertexAttribArray(4);

                // Binormal attribute
                GL.VertexAttribPointer(5, 3, VertexAttribPointerType.Float, false, Vector3.SizeInBytes, offset); offset += binormalSize;
                GL.EnableVertexAttribArray(5);

                // Blend indices attribute
                GL.VertexAttribPointer(6, 4, VertexAttribPointerType.Float, false, Vector4.SizeInBytes, offset); offset += blendIndexSize;
                GL.EnableVertexAttribArray(6);

                // Blend weights attribute
                GL.VertexAttribPointer(7, 4, VertexAttribPointerType.Float, false, Vector4.SizeInBytes, offset); offset += blendWeightSize;
                GL.EnableVertexAttribArray(7);

                //Clear bindings
                GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
                GL.BindVertexArray(0);
            }

            //Grab any errors from setup
            ErrorCode error = ErrorCode.NoError;
            while ((error = GL.GetError()) != ErrorCode.NoError)
            {
                MessageHandler.Instance.AddMessage(MessageType.ERROR, string.Format("Error in model \"{0}\": {1}", Name, error.ToString()));
            }

            base.Setup();
        }

        public override void Draw(Matrix4 view, Matrix4 proj)
        {
            if (!IsVisible)
            {
                return;
            }

            Matrix4[] skinMatrices = null;
            bool canSkin = armature != null && armature.Bones.Count > 0;
            int boneCount = 0;
            if (canSkin)
            {
                if (RenderOptions.UseJointInfoMatrices)
                {
                    skinMatrices = armature.GetSkinMatricesForJointInfo(Armature.MaxSkinBones, out boneCount);
                }
                else if (RenderOptions.UseSkinningPaletteMatrices)
                {
                    var palette = armature.BuildSkinningPalette();
                    skinMatrices = armature.GetSkinMatricesForPalette(palette, Armature.MaxSkinBones, out boneCount);
                }
                else
                {
                    boneCount = Math.Min(armature.Bones.Count, Armature.MaxSkinBones);
                    skinMatrices = armature.GetSkinMatrices(Armature.MaxSkinBones);
                }

            }

            for (int i = 0; i < VAOs.Length; i++)
            {
                if (RenderOptions.OutlinePass)
                {
                    if (i == selectedSubmeshIndex)
                    {
                        GL.BindVertexArray(VAOs[i]);
                        DrawOutline(view, proj, Indices[i].Length, canSkin && HasSkinning[i], boneCount, skinMatrices);
                        GL.BindVertexArray(0);
                    }
                    continue;
                }
                //Bind appropriate mat
                if (materials != null && materials.Length > 0)
                {
                    if (!materialMap.TryGetValue(MaterialNames[i], out var mat))
                    {
                        mat = materials[0];
                    }
                    bool drawOpaque = !RenderOptions.TransparentPass && !mat.IsTransparent;
                    bool drawTransparent = RenderOptions.TransparentPass && mat.IsTransparent;
                    if (drawOpaque || drawTransparent)
                    {
                        mat.Use(view, modelMat, proj, HasVertexColors[i], HasTangents[i], HasBinormals[i]);
                        mat.ApplySkinning(canSkin && HasSkinning[i], boneCount, skinMatrices);
                    }
                    else if (!RenderOptions.TransparentPass)
                    {
                        continue;
                    }
                }

                // Draw the geometry
                GL.BindVertexArray(VAOs[i]);
                if (!RenderOptions.TransparentPass)
                {
                    GL.DrawElements(PrimitiveType.Triangles, Indices[i].Length, DrawElementsType.UnsignedInt, 0);
                }
                else if (materials != null && materials.Length > 0 && materialMap.TryGetValue(MaterialNames[i], out var mat) && mat.IsTransparent)
                {
                    GL.DrawElements(PrimitiveType.Triangles, Indices[i].Length, DrawElementsType.UnsignedInt, 0);
                }

                GL.BindVertexArray(0);
            }
        }

        public void DrawSkeleton(Matrix4 view, Matrix4 proj)
        {
            if (!IsVisible)
            {
                return;
            }

            if (armature == null || armature.Bones.Count == 0)
            {
                return;
            }

            var shader = ShaderPool.Instance.GetShader("Lines");
            if (shader == null)
            {
                return;
            }

            var positions = armature.GetWorldPositions();
            var boneVerts = new List<float>();

            for (int i = 0; i < positions.Length; i++)
            {
                if (!armature.IsVisibleBone(i))
                {
                    continue;
                }

                var head = positions[i];
                bool added = false;

                // Draw to each visible child (better for hands/fingers where the first child is often a helper/roll node).
                var bone = armature.Bones[i];
                foreach (var child in bone.Children)
                {
                    int childIndex = armature.Bones.IndexOf(child);
                    if (childIndex < 0 || childIndex >= positions.Length)
                    {
                        continue;
                    }
                    if (!armature.IsVisibleBone(childIndex))
                    {
                        continue;
                    }

                    var tail = positions[childIndex];
                    if ((tail - head).LengthSquared < 0.0001f)
                    {
                        continue;
                    }

                    boneVerts.Add(head.X);
                    boneVerts.Add(head.Y);
                    boneVerts.Add(head.Z);
                    boneVerts.Add(tail.X);
                    boneVerts.Add(tail.Y);
                    boneVerts.Add(tail.Z);
                    added = true;
                }

                // Leaf bone: extension along the parent to child direction keeps the line stable.
                if (!added)
                {
                    int parent = armature.GetVisibleParentIndex(i);
                    Vector3 dir = Vector3.UnitY;
                    if (parent >= 0 && parent < positions.Length)
                    {
                        var d = head - positions[parent];
                        if (d.LengthSquared > 0.000001f)
                        {
                            dir = Vector3.Normalize(d);
                        }
                    }

                    var tail = head + dir * 0.05f;
                    boneVerts.Add(head.X);
                    boneVerts.Add(head.Y);
                    boneVerts.Add(head.Z);
                    boneVerts.Add(tail.X);
                    boneVerts.Add(tail.Y);
                    boneVerts.Add(tail.Z);
                }
            }

            if (boneVerts.Count == 0)
            {
                return;
            }

            if (skeletonVao == 0)
            {
                skeletonVao = GL.GenVertexArray();
                skeletonVbo = GL.GenBuffer();
                GL.BindVertexArray(skeletonVao);
                GL.BindBuffer(BufferTarget.ArrayBuffer, skeletonVbo);
                GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
                GL.EnableVertexAttribArray(0);
                GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
                GL.BindVertexArray(0);
            }

            GL.BindVertexArray(skeletonVao);
            GL.BindBuffer(BufferTarget.ArrayBuffer, skeletonVbo);
            GL.BufferData(BufferTarget.ArrayBuffer, boneVerts.Count * sizeof(float), boneVerts.ToArray(), BufferUsageHint.DynamicDraw);

            shader.Bind();
            shader.SetMatrix4("model", modelMat);
            shader.SetMatrix4("view", view);
            shader.SetMatrix4("projection", proj);
            shader.SetVector4("color", new Vector4(1.0f, 0.85f, 0.1f, 1.0f));

            GL.DrawArrays(PrimitiveType.Lines, 0, boneVerts.Count / 3);

            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindVertexArray(0);
        }

        public void ApplyAnimation(Animation animation, float frame)
        {
            armature?.ApplyAnimation(animation, frame);
        }

        public void ResetPose()
        {
            armature?.ResetPose();
        }


        private static void AppendBoneMesh(List<float> verts, Vector3 head, Vector3 tail)
        {
            var dir = tail - head;
            var len = dir.Length;
            if (len < 0.0001f)
            {
                return;
            }

            var basis = BuildBasis(dir / len);
            var radius = MathF.Max(0.01f, len * 0.07f);

            for (int i = 0; i < unitBoneVerts.Length; i += 3)
            {
                var local = new Vector3(unitBoneVerts[i], unitBoneVerts[i + 1], unitBoneVerts[i + 2]);
                local.X *= radius;
                local.Z *= radius;
                local.Y *= len;

                var world = head + basis * local;
                verts.Add(world.X);
                verts.Add(world.Y);
                verts.Add(world.Z);
            }
        }

        private static Matrix3 BuildBasis(Vector3 direction)
        {
            var up = MathF.Abs(Vector3.Dot(direction, Vector3.UnitY)) > 0.9f
                ? Vector3.UnitX
                : Vector3.UnitY;

            var x = Vector3.Normalize(Vector3.Cross(up, direction));
            var z = Vector3.Normalize(Vector3.Cross(direction, x));
            return new Matrix3(x.X, x.Y, x.Z,
                               direction.X, direction.Y, direction.Z,
                               z.X, z.Y, z.Z);
        }

        private static float[] BuildUnitBoneVerts()
        {
            var head = new Vector3(0f, 0f, 0f);
            var tail = new Vector3(0f, 1f, 0f);
            var a = new Vector3(1f, 0.5f, 0f);
            var b = new Vector3(-1f, 0.5f, 0f);
            var c = new Vector3(0f, 0.5f, 1f);
            var d = new Vector3(0f, 0.5f, -1f);

            return new[]
            {
                head.X, head.Y, head.Z, a.X, a.Y, a.Z, c.X, c.Y, c.Z,
                head.X, head.Y, head.Z, c.X, c.Y, c.Z, b.X, b.Y, b.Z,
                head.X, head.Y, head.Z, b.X, b.Y, b.Z, d.X, d.Y, d.Z,
                head.X, head.Y, head.Z, d.X, d.Y, d.Z, a.X, a.Y, a.Z,
                tail.X, tail.Y, tail.Z, c.X, c.Y, c.Z, a.X, a.Y, a.Z,
                tail.X, tail.Y, tail.Z, b.X, b.Y, b.Z, c.X, c.Y, c.Z,
                tail.X, tail.Y, tail.Z, d.X, d.Y, d.Z, b.X, b.Y, b.Z,
                tail.X, tail.Y, tail.Z, a.X, a.Y, a.Z, d.X, d.Y, d.Z,
            };
        }

        private void DrawOutline(Matrix4 view, Matrix4 proj, int indexCount, bool enableSkinning, int boneCount, Matrix4[] skinMatrices)
        {
            var outlineShader = ShaderPool.Instance.GetShader("Outline");
            if (outlineShader == null)
            {
                return;
            }

            outlineShader.Bind();
            outlineShader.SetMatrix4("model", modelMat);
            outlineShader.SetMatrix4("view", view);
            outlineShader.SetMatrix4("projection", proj);
            outlineShader.SetBoolIfExists("EnableSkinning", enableSkinning);
            outlineShader.SetIntIfExists("BoneCount", enableSkinning ? boneCount : 0);
            outlineShader.SetBoolIfExists("SwapBlendOrder", RenderOptions.SwapBlendOrder);
            if (enableSkinning)
            {
                outlineShader.SetMatrix4ArrayIfExists("Bones", skinMatrices, RenderOptions.TransposeSkinMatrices);
            }
            outlineShader.SetVector3("OutlineColor", RenderOptions.OutlineColor);
            outlineShader.SetFloat("OutlineAlpha", RenderOptions.OutlineAlpha);

            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
            GL.Disable(EnableCap.CullFace);
            GL.LineWidth(1.5f);
            GL.DrawElements(PrimitiveType.Triangles, indexCount, DrawElementsType.UnsignedInt, 0);
            GL.LineWidth(1.0f);
            GL.Enable(EnableCap.CullFace);
            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);

            outlineShader.Unbind();
        }

        public void SetSelectedSubmesh(int index)
        {
            selectedSubmeshIndex = index;
        }

        public void SetVisible(bool visible)
        {
            IsVisible = visible;
        }
    }
}
