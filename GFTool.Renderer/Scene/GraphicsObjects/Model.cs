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
    public class Model : RefObject, IDisposable
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
            public required IReadOnlyList<Vector2[]> UVSets { get; init; }
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
                    UVSets = i < UvSets.Count ? UvSets[i] : Array.Empty<Vector2[]>(),
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
        public string SourcePath { get; private set; }

        private int[] VAOs;
        private int[] VBOs;
        private int[] EBOs;

        private List<Vector3[]> Positions = new List<Vector3[]>();
        private List<Vector3[]> Normals = new List<Vector3[]>();
        private List<Vector2[]> UVs = new List<Vector2[]>();
        private List<Vector2[][]> UvSets = new List<Vector2[][]>();
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
        private List<bool> SubmeshVisible = new List<bool>();
        private List<bool> DefaultSubmeshVisible = new List<bool>();

        private Material[] materials;
        private List<string> MaterialNames = new List<string>();
        private Dictionary<string, Material> materialMap = new Dictionary<string, Material>(StringComparer.OrdinalIgnoreCase);
        private List<string> SubmeshNames = new List<string>();

        private Armature? armature;
        private ActionClipAnimation? appliedActionClip;
        private bool disposed;
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
            SourcePath = Path.GetFullPath(model);
            modelMat = Matrix4.Identity;
            modelPath = new PathString(model);

            DiagnosticLog.Section($"Load model: {Name}");
            DiagnosticLog.Write($"TRMDL path: {model}");
            DiagnosticLog.Write($"TRMDL file: {DescribeFile(model)}");
            DiagnosticLog.Write($"Load all LODs: {loadAllLods}");
            var mdl = FlatBufferConverter.DeserializeFrom<TRMDL>(model);
            DiagnosticLog.Write($"TRMDL parsed: meshes={mdl.Meshes?.Length ?? 0}, materials={mdl.Materials?.Length ?? 0}, skeleton={(mdl.Skeleton?.PathName ?? "<none>")}");
            if (mdl.Meshes == null || mdl.Meshes.Length == 0)
            {
                throw new InvalidDataException("This TRMDL has no mesh entries. It is probably a locator/helper resource, not a renderable model.");
            }

            //Meshes
            if (loadAllLods)
            {
                for (int i = 0; i < mdl.Meshes.Length; i++)
                {
                    var mesh = mdl.Meshes[i];
                    DiagnosticLog.Write($"Referenced mesh[{i}]: {mesh.PathName}");
                    ParseMesh(modelPath.Combine(mesh.PathName));
                }
            }
            else
            {
                var mesh = mdl.Meshes[0]; //LOD0
                DiagnosticLog.Write($"Referenced mesh[LOD0]: {mesh.PathName}");
                ParseMesh(modelPath.Combine(mesh.PathName));
            }

            baseSkeletonCategoryHint = GuessBaseSkeletonCategoryFromMesh(mdl.Meshes != null && mdl.Meshes.Length > 0 ? mdl.Meshes[0].PathName : null);

            //Materials
            for (int i = 0; i < mdl.Materials.Length; i++)
            {
                var mat = mdl.Materials[i];
                DiagnosticLog.Write($"Referenced material[{i}]: {mat}");
                ParseMaterial(modelPath.Combine(mat));
            }

            //Skeleton
            if (mdl.Skeleton != null)
            {
                DiagnosticLog.Write($"Referenced skeleton: {mdl.Skeleton.PathName}");
                ParseArmature(modelPath.Combine(mdl.Skeleton.PathName));
            }
            else
            {
                DiagnosticLog.Write("Referenced skeleton: <none>");
            }
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

        private void ParseMeshBuffer(TRVertexDeclaration vertDesc, TRBuffer[] vertexBuffers, TRBuffer indexBuf, TRIndexFormat polyType, long start, long count, TRBoneWeight[]? boneWeights, string meshName, string materialName, bool defaultVisible)
        {
            if (vertexBuffers == null || vertexBuffers.Length == 0)
            {
                DiagnosticLog.Write($"Mesh buffer skipped: mesh={meshName}, material={materialName}, reason=no vertex buffers");
                return;
            }

            var posElement = vertDesc.vertexElements.FirstOrDefault(e => e.vertexUsage == TRVertexUsage.POSITION);
            if (posElement == null)
            {
                DiagnosticLog.Write($"Mesh buffer skipped: mesh={meshName}, material={materialName}, reason=no POSITION element");
                return;
            }

            var posBuffer = GetVertexBuffer(vertexBuffers, posElement.vertexElementLayer);
            if (posBuffer == null)
            {
                DiagnosticLog.Write($"Mesh buffer skipped: mesh={meshName}, material={materialName}, reason=POSITION layer {posElement.vertexElementLayer} missing");
                return;
            }

            var posStride = GetStride(vertDesc, posElement.vertexElementSizeIndex);
            if (posStride <= 0)
            {
                DiagnosticLog.Write($"Mesh buffer skipped: mesh={meshName}, material={materialName}, reason=invalid POSITION stride index {posElement.vertexElementSizeIndex}");
                return;
            }

            int vertexCount = posBuffer.Bytes.Length / posStride;
            if (vertexCount <= 0)
            {
                DiagnosticLog.Write($"Mesh buffer skipped: mesh={meshName}, material={materialName}, reason=vertex count <= 0");
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
            var uvStreams = new List<Vector2[]>();
            int blendIndexElementIndex = -1;
            int blendWeightElementIndex = -1;
            int texCoordElementIndex = -1;

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
                int? texCoordStreamIndex = null;
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
                else if (att.vertexUsage == TRVertexUsage.TEX_COORD)
                {
                    texCoordElementIndex++;
                    EnsureUvStream(uvStreams, texCoordElementIndex, vertexCount);
                    texCoordStreamIndex = texCoordElementIndex;
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
                            var uvValue = ReadVector2(buffer.Bytes, offset, att.vertexFormat);
                            uv[v] = uvValue;
                            if (texCoordStreamIndex.HasValue)
                            {
                                uvStreams[texCoordStreamIndex.Value][v] = uvValue;
                            }
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

            if (hasBlendWeights)
            {
                NormalizeBlendWeights(blendWeights);
                LogDetailedSkinWeights(meshName, materialName, blendIndexStreams.Count, blendWeightStreams.Count, blendWeights);
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
            UvSets.Add(hasUvs ? uvStreams.Select(stream => stream.ToArray()).ToArray() : Array.Empty<Vector2[]>());
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
            SubmeshVisible.Add(defaultVisible);
            DefaultSubmeshVisible.Add(defaultVisible);

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

            int texCoordElements = vertDesc.vertexElements.Count(e => e.vertexUsage == TRVertexUsage.TEX_COORD);
            DiagnosticLog.Write(
                $"Submesh data: mesh={meshName}, material={materialName}, vertices={vertexCount}, indices={indices.Count}, indexFormat={polyType}, " +
                $"hasNormals={hasNormals}, hasUVs={hasUvs}, texCoordElements={texCoordElements}, hasColors={hasColors}, hasTangents={hasTangents}, hasBinormals={hasBinormals}, hasSkinning={HasSkinning.LastOrDefault()}");
            if (texCoordElements > 1)
            {
                DiagnosticLog.Write($"UV warning: mesh={meshName}, material={materialName} has {texCoordElements} TEX_COORD elements. Renderer still uses the legacy UV array; export preserves each TEX_COORD set.");
            }
            DiagnosticLog.Write($"UV summary: mesh={meshName}, material={materialName}, {SummarizeUvArray(uv, hasUvs)}");
            DiagnosticLog.Write($"UV indexed summary: mesh={meshName}, material={materialName}, {SummarizeIndexedUvArray(uv, indices, hasUvs)}");
        }

        private void ParseMesh(string file)
        {
            DiagnosticLog.Section($"Parse mesh file: {Path.GetFileName(file)}");
            DiagnosticLog.Write($"TRMSH path: {file}");
            DiagnosticLog.Write($"TRMSH file: {DescribeFile(file)}");
            var msh = FlatBufferConverter.DeserializeFrom<TRMSH>(file);
            var bufferPath = modelPath.Combine(msh.bufferFilePath);
            DiagnosticLog.Write($"TRMSH parsed: version={msh.Version}, meshCount={msh.Meshes?.Length ?? 0}, bufferFilePath={msh.bufferFilePath}");
            DiagnosticLog.Write($"TRMBF path: {bufferPath}");
            DiagnosticLog.Write($"TRMBF file: {DescribeFile(bufferPath)}");
            var buffers = FlatBufferConverter.DeserializeFrom<TRMBF>(bufferPath).TRMeshBuffers;
            DiagnosticLog.Write($"TRMBF parsed: meshBufferCount={buffers?.Length ?? 0}");
            var shapeCnt = msh.Meshes.Count();
            for (int i = 0; i < shapeCnt; i++)
            {
                var meshShape = msh.Meshes[i];
                bool defaultVisible = IsDefaultVisibleMeshShape(meshShape.Name, msh.Meshes);
                var vertBufs = buffers[i].VertexBuffer;
                var indexBuf = buffers[i].IndexBuffer[0]; //LOD0
                var polyType = meshShape.IndexType;
                int boneWeightCount = meshShape.boneWeight?.Length ?? 0;
                DiagnosticLog.Write($"Mesh shape[{i}]: name={meshShape.Name}, parts={meshShape.meshParts?.Length ?? 0}, declarations={meshShape.vertexDeclaration?.Length ?? 0}, vertexBuffers={vertBufs?.Length ?? 0}, indexBuffers={buffers[i].IndexBuffer?.Length ?? 0}, indexType={polyType}, boneWeights={boneWeightCount}");
                if (!defaultVisible)
                {
                    DiagnosticLog.Write($"Mesh shape default hidden: name={meshShape.Name}, reason=secondary mesh variant");
                }
                for (int d = 0; d < (meshShape.vertexDeclaration?.Length ?? 0); d++)
                {
                    LogVertexDeclaration(meshShape.Name, d, meshShape.vertexDeclaration[d]);
                }

                foreach (var part in meshShape.meshParts)
                {
                    MaterialNames.Add(part.MaterialName);
                    SubmeshNames.Add($"{meshShape.Name}:{part.MaterialName}");
                    int declIndex = part.vertexDeclarationIndex;
                    if (declIndex < 0 || declIndex >= meshShape.vertexDeclaration.Length)
                    {
                        declIndex = 0;
                    }
                    DiagnosticLog.Write($"Mesh part: mesh={meshShape.Name}, material={part.MaterialName}, indexOffset={part.indexOffset}, indexCount={part.indexCount}, declarationIndex={part.vertexDeclarationIndex}->{declIndex}");
                    ParseMeshBuffer(meshShape.vertexDeclaration[declIndex], vertBufs, indexBuf, meshShape.IndexType, part.indexOffset, part.indexCount, meshShape.boneWeight, meshShape.Name, part.MaterialName, defaultVisible);
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

        private static bool IsDefaultVisibleMeshShape(string? meshName, TRMesh[] meshes)
        {
            if (string.IsNullOrWhiteSpace(meshName))
            {
                return true;
            }

            if (!TryGetMeshShapeVariant(meshName, out string groupKey, out char variant))
            {
                return true;
            }

            if (variant == 'a')
            {
                return true;
            }

            bool hasPrimaryVariant = meshes?.Any(mesh =>
                !string.Equals(mesh?.Name, meshName, StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrWhiteSpace(mesh?.Name) &&
                TryGetMeshShapeVariant(mesh.Name, out string otherGroupKey, out char otherVariant) &&
                string.Equals(otherGroupKey, groupKey, StringComparison.OrdinalIgnoreCase) &&
                otherVariant == 'a') == true;

            return !hasPrimaryVariant;
        }

        private static bool TryGetMeshShapeVariant(string meshName, out string groupKey, out char variant)
        {
            groupKey = string.Empty;
            variant = '\0';

            if (string.IsNullOrWhiteSpace(meshName))
            {
                return false;
            }

            string lower = meshName.ToLowerInvariant();
            const string suffix = "_mesh_shape";
            if (!lower.EndsWith(suffix, StringComparison.Ordinal))
            {
                return false;
            }

            string stem = lower.Substring(0, lower.Length - suffix.Length);
            int lastUnderscore = stem.LastIndexOf('_');
            if (lastUnderscore < 0 || lastUnderscore >= stem.Length - 1)
            {
                return false;
            }

            string variantPart = stem.Substring(lastUnderscore + 1);
            if (variantPart.Length != 1)
            {
                return false;
            }

            char ch = variantPart[0];
            if (ch < 'a' || ch > 'z')
            {
                return false;
            }

            groupKey = stem.Substring(0, lastUnderscore);
            variant = ch;
            return true;
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

        private static void EnsureUvStream(List<Vector2[]> streams, int index, int vertexCount)
        {
            while (streams.Count <= index)
            {
                streams.Add(new Vector2[vertexCount]);
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
                float total = w0 + w1 + w2 + w3;
                if (total > 0.000001f)
                {
                    w0 /= total;
                    w1 /= total;
                    w2 /= total;
                    w3 /= total;
                }

                collapsedIndices[v] = new Vector4(
                    top.Length > 0 ? top[0].Key : 0,
                    top.Length > 1 ? top[1].Key : 0,
                    top.Length > 2 ? top[2].Key : 0,
                    top.Length > 3 ? top[3].Key : 0);
                collapsedWeights[v] = new Vector4(w0, w1, w2, w3);
            }
        }

        private static void NormalizeBlendWeights(Vector4[] weights)
        {
            if (weights == null)
            {
                return;
            }

            for (int i = 0; i < weights.Length; i++)
            {
                var w = weights[i];
                float total = w.X + w.Y + w.Z + w.W;
                if (total <= 0.000001f)
                {
                    continue;
                }

                weights[i] = new Vector4(
                    w.X / total,
                    w.Y / total,
                    w.Z / total,
                    w.W / total);
            }
        }

        private static void LogDetailedSkinWeights(string meshName, string materialName, int indexStreamCount, int weightStreamCount, Vector4[] weights)
        {
            if (!IsDetailedSkinMesh(meshName, materialName) || weights == null || weights.Length == 0)
            {
                return;
            }

            float minSum = float.MaxValue;
            float maxSum = float.MinValue;
            int zeroSum = 0;
            int sampleCount = Math.Min(weights.Length, 2048);
            for (int i = 0; i < sampleCount; i++)
            {
                var w = weights[i];
                float sum = w.X + w.Y + w.Z + w.W;
                minSum = MathF.Min(minSum, sum);
                maxSum = MathF.Max(maxSum, sum);
                if (sum <= 0.000001f)
                {
                    zeroSum++;
                }
            }

            DiagnosticLog.Write(
                $"[Skin] Weight detail mesh={meshName}, material={materialName}, indexStreams={indexStreamCount}, weightStreams={weightStreamCount}, sample={sampleCount}, sumRange=({minSum:0.######}, {maxSum:0.######}), zeroSum={zeroSum}");
        }

        private static bool IsDetailedSkinMesh(string meshName, string materialName)
        {
            return ContainsSkinDetailToken(meshName) || ContainsSkinDetailToken(materialName);
        }

        private static bool ContainsSkinDetailToken(string text)
        {
            return !string.IsNullOrWhiteSpace(text) &&
                   (text.Contains("face", StringComparison.OrdinalIgnoreCase) ||
                    text.Contains("mouth", StringComparison.OrdinalIgnoreCase) ||
                    text.Contains("lip", StringComparison.OrdinalIgnoreCase) ||
                    text.Contains("eye", StringComparison.OrdinalIgnoreCase));
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

        private static string DescribeFile(string path)
        {
            try
            {
                var info = new FileInfo(path);
                if (!info.Exists)
                {
                    return "missing";
                }

                return $"exists, bytes={info.Length}, modified={info.LastWriteTime:O}";
            }
            catch (Exception ex)
            {
                return $"unavailable: {ex.Message}";
            }
        }

        private static void LogVertexDeclaration(string meshName, int declarationIndex, TRVertexDeclaration declaration)
        {
            var elementCount = declaration.vertexElements?.Length ?? 0;
            var strideCount = declaration.vertexElementSizes?.Length ?? 0;
            var texCoordCount = declaration.vertexElements?.Count(e => e.vertexUsage == TRVertexUsage.TEX_COORD) ?? 0;
            DiagnosticLog.Write($"Vertex declaration: mesh={meshName}, index={declarationIndex}, elements={elementCount}, strideEntries={strideCount}, texCoordElements={texCoordCount}");

            if (declaration.vertexElementSizes != null)
            {
                for (int i = 0; i < declaration.vertexElementSizes.Length; i++)
                {
                    DiagnosticLog.Write($"  stride[{i}]={declaration.vertexElementSizes[i].elementSize}");
                }
            }

            if (declaration.vertexElements == null)
            {
                return;
            }

            for (int i = 0; i < declaration.vertexElements.Length; i++)
            {
                var element = declaration.vertexElements[i];
                var stride = GetStride(declaration, element.vertexElementSizeIndex);
                DiagnosticLog.Write(
                    $"  element[{i}]: usage={element.vertexUsage}, layer={element.vertexElementLayer}, format={element.vertexFormat}, offset={element.vertexElementOffset}, strideIndex={element.vertexElementSizeIndex}, stride={stride}");
            }
        }

        private static void LogMaterialData(TRMaterial mat, string sourceKind)
        {
            if (mat == null)
            {
                DiagnosticLog.Write($"Material ({sourceKind}): <null>");
                return;
            }

            var shaderName = mat.Shader?.FirstOrDefault()?.Name ?? "<none>";
            DiagnosticLog.Write(
                $"Material ({sourceKind}): name={mat.Name}, shader={shaderName}, textures={mat.Textures?.Length ?? 0}, samplers={mat.Samplers?.Length ?? 0}, " +
                $"floatParams={mat.FloatParams?.Length ?? 0}, vec2Params={mat.Vec2fParams?.Length ?? 0}, vec3Params={mat.Vec3fParams?.Length ?? 0}, vec4Params={mat.Vec4fParams?.Length ?? 0}");

            if (mat.Textures != null)
            {
                for (int i = 0; i < mat.Textures.Length; i++)
                {
                    var tex = mat.Textures[i];
                    DiagnosticLog.Write($"  texture[{i}]: name={tex?.Name}, file={tex?.File}, slot={tex?.Slot}");
                }
            }

            if (mat.Samplers != null)
            {
                for (int i = 0; i < mat.Samplers.Length; i++)
                {
                    var sampler = mat.Samplers[i];
                    DiagnosticLog.Write($"  sampler[{i}]: repeatU={sampler.RepeatU}, repeatV={sampler.RepeatV}, repeatW={sampler.RepeatW}, states={sampler.State0},{sampler.State1},{sampler.State2},{sampler.State3},{sampler.State4},{sampler.State5},{sampler.State6},{sampler.State7},{sampler.State8}");
                }
            }

            if (mat.Shader?.FirstOrDefault()?.Values != null)
            {
                foreach (var param in mat.Shader.First().Values.Where(p => p != null))
                {
                    DiagnosticLog.Write($"  shader option: {param.Name}={param.Value}");
                }
            }

            if (mat.FloatParams != null)
            {
                foreach (var param in mat.FloatParams.Where(p => p != null))
                {
                    DiagnosticLog.Write($"  float param: {param.Name}={param.Value}");
                }
            }

            if (mat.Vec2fParams != null)
            {
                foreach (var param in mat.Vec2fParams.Where(p => p != null))
                {
                    DiagnosticLog.Write($"  vec2 param: {param.Name}=({param.Value.X}, {param.Value.Y})");
                }
            }

            if (mat.Vec3fParams != null)
            {
                foreach (var param in mat.Vec3fParams.Where(p => p != null))
                {
                    DiagnosticLog.Write($"  vec3 param: {param.Name}=({param.Value.X}, {param.Value.Y}, {param.Value.Z})");
                }
            }

            if (mat.Vec4fParams != null)
            {
                foreach (var param in mat.Vec4fParams.Where(p => p != null))
                {
                    DiagnosticLog.Write($"  vec4 param: {param.Name}=({param.Value.X}, {param.Value.Y}, {param.Value.Z}, {param.Value.W})");
                }
            }
        }

        private static string SummarizeUvArray(Vector2[] uvs, bool hasUvs)
        {
            if (!hasUvs || uvs == null || uvs.Length == 0)
            {
                return "uv=<none>";
            }

            float minU = float.PositiveInfinity;
            float minV = float.PositiveInfinity;
            float maxU = float.NegativeInfinity;
            float maxV = float.NegativeInfinity;
            int nonZero = 0;
            for (int i = 0; i < uvs.Length; i++)
            {
                var uv = uvs[i];
                minU = Math.Min(minU, uv.X);
                minV = Math.Min(minV, uv.Y);
                maxU = Math.Max(maxU, uv.X);
                maxV = Math.Max(maxV, uv.Y);
                if (Math.Abs(uv.X) > 0.000001f || Math.Abs(uv.Y) > 0.000001f)
                {
                    nonZero++;
                }
            }

            var samples = uvs.Take(Math.Min(5, uvs.Length))
                .Select(uv => $"({uv.X:0.#####},{uv.Y:0.#####})");
            return $"uvCount={uvs.Length}, nonZero={nonZero}, rangeU={minU:0.#####}..{maxU:0.#####}, rangeV={minV:0.#####}..{maxV:0.#####}, first={string.Join(" ", samples)}";
        }

        private static string SummarizeIndexedUvArray(Vector2[] uvs, IReadOnlyList<uint> indices, bool hasUvs)
        {
            if (!hasUvs || uvs == null || uvs.Length == 0 || indices == null || indices.Count == 0)
            {
                return "uv=<none>";
            }

            float minU = float.PositiveInfinity;
            float minV = float.PositiveInfinity;
            float maxU = float.NegativeInfinity;
            float maxV = float.NegativeInfinity;
            int used = 0;
            int outOfRange = 0;
            var samples = new List<string>();

            foreach (var rawIndex in indices)
            {
                if (rawIndex >= uvs.Length)
                {
                    continue;
                }

                var uv = uvs[rawIndex];
                minU = Math.Min(minU, uv.X);
                minV = Math.Min(minV, uv.Y);
                maxU = Math.Max(maxU, uv.X);
                maxV = Math.Max(maxV, uv.Y);
                if (uv.X < 0f || uv.X > 1f || uv.Y < 0f || uv.Y > 1f)
                {
                    outOfRange++;
                }
                if (samples.Count < 5)
                {
                    samples.Add($"({uv.X:0.#####},{uv.Y:0.#####})");
                }
                used++;
            }

            if (used == 0)
            {
                return "uv=<none>";
            }

            return $"indexedCount={used}, outOf0To1={outOfRange}, rangeU={minU:0.#####}..{maxU:0.#####}, rangeV={minV:0.#####}..{maxV:0.#####}, first={string.Join(" ", samples)}";
        }

        private void ParseMaterial(string file)
        {
            DiagnosticLog.Section($"Parse material file: {Path.GetFileName(file)}");
            DiagnosticLog.Write($"Material path: {file}");
            DiagnosticLog.Write($"Material file: {DescribeFile(file)}");
            List<Material> matlist = new List<Material>();
            var materialPath = new PathString(file);

            TRMTR? trmtrFallback = null;
            try
            {
                trmtrFallback = FlatBufferConverter.DeserializeFrom<TRMTR>(file);
                DiagnosticLog.Write($"TRMTR fallback parse: ok, materials={trmtrFallback.Materials?.Length ?? 0}");
            }
            catch
            {
                trmtrFallback = null;
                DiagnosticLog.Write("TRMTR fallback parse: failed");
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

            Trinity.Core.Flatbuffers.Gfx2.Material? gfxMaterials = null;
            try
            {
                gfxMaterials = FlatBufferConverter.DeserializeFrom<Trinity.Core.Flatbuffers.Gfx2.Material>(file);
                DiagnosticLog.Write($"Gfx2 material parse: ok, itemCount={gfxMaterials?.ItemList?.Length ?? 0}");
            }
            catch (Exception ex)
            {
                DiagnosticLog.WriteException("Gfx2 material parse failed", ex);
            }
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

                    LogMaterialData(trmat, "Gfx2");
                    matlist.Add(new Material(materialPath, trmat, IsPokemonModel()));
                }

                materials = matlist.ToArray();
                HarmonizeSkinToneMaterials();
                BuildMaterialMap();
                return;
            }

            var mats = FlatBufferConverter.DeserializeFrom<TRMTR>(file);
            DiagnosticLog.Write($"Using TRMTR materials: count={mats.Materials?.Length ?? 0}");
            foreach (var mat in mats.Materials)
            {
                LogMaterialData(mat, "TRMTR");
                matlist.Add(new Material(materialPath, mat, IsPokemonModel()));
            }
            materials = matlist.ToArray();
            HarmonizeSkinToneMaterials();
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

        public void ApplyMeshVariantVisibility(char preferredVariant, string reason)
        {
            ApplyMeshVariantVisibility(Array.Empty<string>(), preferredVariant, reason);
        }

        public void ApplyMeshVariantVisibility(IReadOnlyList<string> trackNames, char fallbackVariant, string reason)
        {
            fallbackVariant = char.ToLowerInvariant(fallbackVariant);
            if (fallbackVariant < 'a' || fallbackVariant > 'z')
            {
                fallbackVariant = 'a';
            }

            var groups = new Dictionary<string, Dictionary<char, List<int>>>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < SubmeshNames.Count; i++)
            {
                string meshName = GetMeshShapeNameFromSubmeshName(SubmeshNames[i]);
                if (!TryGetMeshShapeVariant(meshName, out string groupKey, out char variant))
                {
                    continue;
                }

                if (!groups.TryGetValue(groupKey, out var variants))
                {
                    variants = new Dictionary<char, List<int>>();
                    groups[groupKey] = variants;
                }

                if (!variants.TryGetValue(variant, out var indices))
                {
                    indices = new List<int>();
                    variants[variant] = indices;
                }

                indices.Add(i);
            }

            foreach (var group in groups)
            {
                if (group.Value.Count <= 1)
                {
                    continue;
                }

                char selected = SelectMeshVariantFromAnimationTracks(group.Key, group.Value.Keys, trackNames, fallbackVariant, out string selectionSource);

                foreach (var variant in group.Value)
                {
                    bool visible = variant.Key == selected;
                    foreach (int submeshIndex in variant.Value)
                    {
                        if (submeshIndex < 0 || submeshIndex >= SubmeshVisible.Count)
                        {
                            continue;
                        }

                        bool visibilityChanged = SubmeshVisible[submeshIndex] != visible;
                        bool baselineChanged = submeshIndex >= DefaultSubmeshVisible.Count ||
                                               DefaultSubmeshVisible[submeshIndex] != visible;
                        if (!visibilityChanged && !baselineChanged)
                        {
                            continue;
                        }

                        SubmeshVisible[submeshIndex] = visible;
                        if (submeshIndex < DefaultSubmeshVisible.Count)
                        {
                            // Variant selection is the baseline for the next animation. Action clips
                            // may override it temporarily, but reset/switch must return to this variant.
                            DefaultSubmeshVisible[submeshIndex] = visible;
                        }
                        string submeshName = submeshIndex < SubmeshNames.Count ? SubmeshNames[submeshIndex] : $"Submesh {submeshIndex}";
                        DiagnosticLog.Write($"Mesh variant visibility: model={Name}, group={group.Key}, fallback={fallbackVariant}, selected={selected}, source={selectionSource}, submesh={submeshName}, visible={visible}, reason={reason}");
                    }
                }
            }
        }

        private char SelectMeshVariantFromAnimationTracks(string groupKey, IEnumerable<char> variants, IReadOnlyList<string>? trackNames, char fallbackVariant, out string selectionSource)
        {
            selectionSource = string.Empty;
            var available = variants.Distinct().OrderBy(x => x).ToList();
            char bestVariant = '\0';
            int bestScore = 0;
            bool hasTie = false;

            foreach (char variant in available)
            {
                int score = CountVariantTrackMatches(groupKey, variant, trackNames);
                if (score > bestScore)
                {
                    bestVariant = variant;
                    bestScore = score;
                    hasTie = false;
                }
                else if (score == bestScore && score > 0)
                {
                    hasTie = true;
                }
            }

            if (bestScore > 0 && !hasTie)
            {
                selectionSource = $"animation-tracks:{bestScore}";
                return bestVariant;
            }

            if (bestScore > 0 && hasTie)
            {
                selectionSource = "animation-tracks-tie";
            }
            else if (available.Contains(fallbackVariant))
            {
                selectionSource = "fallback";
                return fallbackVariant;
            }

            if (available.Contains('a'))
            {
                selectionSource = selectionSource.Length > 0 ? selectionSource + "+a" : "fallback-a";
                return 'a';
            }

            selectionSource = selectionSource.Length > 0 ? selectionSource + "+first" : "fallback-first";
            return available.First();
        }

        private int CountVariantTrackMatches(string groupKey, char variant, IReadOnlyList<string>? trackNames)
        {
            if (trackNames == null || trackNames.Count == 0)
            {
                return 0;
            }

            string fullVariantPrefix = $"{groupKey}_{variant}".ToLowerInvariant();
            string shortGroupKey = GetShortMeshVariantGroupKey(groupKey);
            string shortVariantPrefix = string.IsNullOrWhiteSpace(shortGroupKey)
                ? string.Empty
                : $"{shortGroupKey}_{variant}".ToLowerInvariant();

            int matches = 0;
            foreach (string trackName in trackNames)
            {
                string track = NormalizeTrackNameForVariantMatch(trackName);
                if (string.IsNullOrWhiteSpace(track))
                {
                    continue;
                }

                if (IsVariantTrackMatch(track, fullVariantPrefix) ||
                    (!string.IsNullOrWhiteSpace(shortVariantPrefix) && IsVariantTrackMatch(track, shortVariantPrefix)))
                {
                    matches++;
                }
            }

            return matches;
        }

        private string GetShortMeshVariantGroupKey(string groupKey)
        {
            string lower = groupKey.ToLowerInvariant();
            string modelPrefix = Name.ToLowerInvariant() + "_";
            if (lower.StartsWith(modelPrefix, StringComparison.Ordinal))
            {
                return lower.Substring(modelPrefix.Length);
            }

            return lower;
        }

        private static string NormalizeTrackNameForVariantMatch(string trackName)
        {
            if (string.IsNullOrWhiteSpace(trackName))
            {
                return string.Empty;
            }

            string name = trackName.Trim().ToLowerInvariant();

            int lastColon = name.LastIndexOf(':');
            if (lastColon >= 0 && lastColon < name.Length - 1)
            {
                name = name.Substring(lastColon + 1);
            }

            int lastPipe = name.LastIndexOf('|');
            if (lastPipe >= 0 && lastPipe < name.Length - 1)
            {
                name = name.Substring(lastPipe + 1);
            }

            int lastSlash = Math.Max(name.LastIndexOf('/'), name.LastIndexOf('\\'));
            if (lastSlash >= 0 && lastSlash < name.Length - 1)
            {
                name = name.Substring(lastSlash + 1);
            }

            return name.Trim();
        }

        private static bool IsVariantTrackMatch(string trackName, string variantPrefix)
        {
            return string.Equals(trackName, variantPrefix, StringComparison.OrdinalIgnoreCase) ||
                   trackName.StartsWith(variantPrefix + "_", StringComparison.OrdinalIgnoreCase) ||
                   trackName.StartsWith(variantPrefix + ".", StringComparison.OrdinalIgnoreCase);
        }

        private static string GetMeshShapeNameFromSubmeshName(string submeshName)
        {
            if (string.IsNullOrWhiteSpace(submeshName))
            {
                return string.Empty;
            }

            int split = submeshName.IndexOf(':');
            return split > 0 ? submeshName.Substring(0, split) : submeshName;
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

        private bool IsPokemonModel()
        {
            return Name.StartsWith("pm", StringComparison.OrdinalIgnoreCase);
        }

        private void HarmonizeSkinToneMaterials()
        {
            if (materials == null || IsPokemonModel())
            {
                return;
            }

            var bodySkin = materials.FirstOrDefault(material => material?.IsBodySkin == true);
            if (bodySkin == null)
            {
                return;
            }

            foreach (var faceSkin in materials.Where(material => material?.IsFaceSkin == true))
            {
                faceSkin.SetSkinToneSource(bodySkin);
            }
        }

        private void ParseArmature(string file)
        {
            DiagnosticLog.Section($"Parse skeleton file: {Path.GetFileName(file)}");
            DiagnosticLog.Write($"TRSKL path: {file}");
            DiagnosticLog.Write($"TRSKL file: {DescribeFile(file)}");
            var skel = FlatBufferConverter.DeserializeFrom<TRSKL>(file);
            DiagnosticLog.Write($"TRSKL parsed: transformNodes={skel.TransformNodes?.Length ?? 0}, jointInfos={skel.JointInfos?.Length ?? 0}, helperBones={skel.HelperBones?.Length ?? 0}, skinningPaletteOffset={skel.SkinningPaletteOffset}, baseHint={(baseSkeletonCategoryHint ?? "<none>")}");
            var merged = TryLoadAndMergeBaseSkeleton(skel, file, baseSkeletonCategoryHint);
            armature = new Armature(merged ?? skel, file);
            DiagnosticLog.Write($"Armature built: bones={armature.Bones.Count}, usedMergedSkeleton={merged != null}");
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

                var mode = SelectBlendIndexRemapMode(
                    i,
                    boneWeights,
                    maxIndexBefore,
                    useJointInfo,
                    useSkinPalette,
                    useBoneMeta,
                    autoMap,
                    skinPalette);
                LogBlendIndexMappingSample(i, source, i < BlendWeights.Count ? BlendWeights[i] : null, mode);

                if (i < BlendMeshNames.Count && BlendMeshNames[i].Contains("_eye_mesh_shape", StringComparison.OrdinalIgnoreCase))
                {
                    string palette = boneWeights == null
                        ? "<none>"
                        : string.Join(", ", boneWeights.Select((weight, index) =>
                        {
                            int rig = weight.RigIndex;
                            int node = rig >= 0 && rig < armature.JointInfoCount ? armature.MapJointInfoIndex(rig) : -1;
                            string boneName = node >= 0 && node < armature.Bones.Count ? armature.Bones[node].Name : "<out-of-range>";
                            return $"joint{rig}->node{node}:{boneName}";
                        }));
                    string samples = string.Join(", ", source.Take(8).Select(value => $"({value.X},{value.Y},{value.Z},{value.W})"));
                    DiagnosticLog.Write($"[EyeSkin] mesh={BlendMeshNames[i]}, mode={mode}, sourceIndices={samples}, boneWeights={palette}");
                }

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

        private void LogBlendIndexMappingSample(
            int submeshIndex,
            Vector4[] source,
            Vector4[]? weights,
            BlendIndexRemapMode mode)
        {
            if (armature == null || source == null || source.Length == 0)
            {
                return;
            }

            var usedIndices = new HashSet<int>();
            for (int vertexIndex = 0; vertexIndex < source.Length; vertexIndex++)
            {
                var index = source[vertexIndex];
                var weight = weights != null && vertexIndex < weights.Length
                    ? weights[vertexIndex]
                    : Vector4.One;
                AddWeightedRigIndex(usedIndices, index.X, weight.X);
                AddWeightedRigIndex(usedIndices, index.Y, weight.Y);
                AddWeightedRigIndex(usedIndices, index.Z, weight.Z);
                AddWeightedRigIndex(usedIndices, index.W, weight.W);
            }

            string meshName = submeshIndex < BlendMeshNames.Count
                ? BlendMeshNames[submeshIndex]
                : $"Submesh {submeshIndex}";
            string samples = string.Join(
                ", ",
                usedIndices.OrderBy(index => index).Take(24).Select(index =>
                {
                    string directName = index >= 0 && index < armature.Bones.Count
                        ? armature.Bones[index].Name
                        : "<out-of-range>";
                    int mappedIndex = index >= 0 && index < armature.JointInfoCount
                        ? armature.MapJointInfoIndex(index)
                        : index;
                    string mappedName = mappedIndex >= 0 && mappedIndex < armature.Bones.Count
                        ? armature.Bones[mappedIndex].Name
                        : "<out-of-range>";
                    return $"{index}:direct={directName}->jointNode{mappedIndex}:{mappedName}";
                }));
            DiagnosticLog.Write($"[Skin] Mapping sample mesh={meshName}, mode={mode}, used={usedIndices.Count}: {samples}");
        }

        private BlendIndexRemapMode SelectBlendIndexRemapMode(
            int submeshIndex,
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

            var source = BlendIndiciesOriginal[submeshIndex];
            var weights = submeshIndex < BlendWeights.Count ? BlendWeights[submeshIndex] : null;
            string meshName = submeshIndex < BlendMeshNames.Count ? BlendMeshNames[submeshIndex] : $"Submesh {submeshIndex}";

            if (canMapJointInfo &&
                MatchesMeshRigSummary(source, weights, boneWeights, out int sourceRigCount, out int summaryRigCount))
            {
                var jointInfoScore = ScoreBlendIndexMapping(source, weights, BlendIndexRemapMode.JointInfo, boneWeights, skinPalette);
                LogBlendIndexRemapPick(
                    submeshIndex,
                    maxIndexBefore,
                    boneWeights,
                    skinPalette,
                    BlendIndexRemapMode.JointInfo,
                    jointInfoScore,
                    $"jointInfoRigSummary(source={sourceRigCount},summary={summaryRigCount})");
                return BlendIndexRemapMode.JointInfo;
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
                    LogBlendIndexRemapPick(
                        submeshIndex,
                        maxIndexBefore,
                        boneWeights,
                        skinPalette,
                        BlendIndexRemapMode.JointInfo,
                        ScoreBlendIndexMapping(source, weights, BlendIndexRemapMode.JointInfo, boneWeights, skinPalette),
                        "jointInfoHeuristic");
                    return BlendIndexRemapMode.JointInfo;
                }
            }

            // Auto mode tries each applicable mapping and picks the one with the fewest
            // out of range and non influencer indices (weights ignore unused channels).
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

            if (canMapJointInfo) consider(BlendIndexRemapMode.JointInfo);
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

            string remapMessage =
                $"[Skin] Remap pick mesh={meshName} maxIndex={maxIndexBefore} boneWeights={(boneWeights?.Length ?? 0)} jointInfo={armature.JointInfoCount} palette={skinPalette.Length} boneMeta={armature.BoneMetaCount} mode={bestMode} score=(oor={bestScore.outOfRange}, nonInfluencer={bestScore.nonInfluencer})";
            DiagnosticLog.Write(remapMessage);
            if (MessageHandler.Instance.DebugLogsEnabled)
            {
                MessageHandler.Instance.AddMessage(
                    MessageType.LOG,
                    remapMessage);
            }

            return bestMode;
        }

        private static bool MatchesMeshRigSummary(
            Vector4[] indices,
            Vector4[]? weights,
            TRBoneWeight[]? boneWeights,
            out int sourceRigCount,
            out int summaryRigCount)
        {
            var sourceRigIndices = new HashSet<int>();
            var summaryRigIndices = new HashSet<int>();

            if (boneWeights != null)
            {
                foreach (var boneWeight in boneWeights)
                {
                    if (boneWeight.RigIndex >= 0 && boneWeight.RigWeight > 0f)
                    {
                        summaryRigIndices.Add(boneWeight.RigIndex);
                    }
                }
            }

            for (int vertexIndex = 0; vertexIndex < indices.Length; vertexIndex++)
            {
                var index = indices[vertexIndex];
                var weight = weights != null && vertexIndex < weights.Length
                    ? weights[vertexIndex]
                    : Vector4.One;

                AddWeightedRigIndex(sourceRigIndices, index.X, weight.X);
                AddWeightedRigIndex(sourceRigIndices, index.Y, weight.Y);
                AddWeightedRigIndex(sourceRigIndices, index.Z, weight.Z);
                AddWeightedRigIndex(sourceRigIndices, index.W, weight.W);
            }

            sourceRigCount = sourceRigIndices.Count;
            summaryRigCount = summaryRigIndices.Count;
            return sourceRigCount > 0 && sourceRigIndices.SetEquals(summaryRigIndices);
        }

        private static void AddWeightedRigIndex(HashSet<int> rigIndices, float index, float weight)
        {
            if (weight > 0.0001f)
            {
                int rounded = (int)MathF.Round(index);
                if (rounded >= 0)
                {
                    rigIndices.Add(rounded);
                }
            }
        }

        private void LogBlendIndexRemapPick(
            int submeshIndex,
            int maxIndexBefore,
            TRBoneWeight[]? boneWeights,
            int[] skinPalette,
            BlendIndexRemapMode mode,
            (int outOfRange, int nonInfluencer) score,
            string reason)
        {
            if (armature == null)
            {
                return;
            }

            string meshName = submeshIndex < BlendMeshNames.Count ? BlendMeshNames[submeshIndex] : $"Submesh {submeshIndex}";
            string message =
                $"[Skin] Remap pick mesh={meshName} maxIndex={maxIndexBefore} boneWeights={(boneWeights?.Length ?? 0)} jointInfo={armature.JointInfoCount} palette={skinPalette.Length} boneMeta={armature.BoneMetaCount} mode={mode} score=(oor={score.outOfRange}, nonInfluencer={score.nonInfluencer}) reason={reason}";
            DiagnosticLog.Write(message);
            if (MessageHandler.Instance.DebugLogsEnabled)
            {
                MessageHandler.Instance.AddMessage(MessageType.LOG, message);
            }
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
                var primaryUvs = i < UvSets.Count && UvSets[i].Length > 0 && UvSets[i][0].Length == UVs[i].Length
                    ? UvSets[i][0]
                    : UVs[i];
                var primaryUvSize = primaryUvs.Length * Vector2.SizeInBytes;
                var colorSize = Colors[i].Length * Vector4.SizeInBytes;
                var tangentSize = Tangents[i].Length * Vector4.SizeInBytes;
                var binormalSize = Binormals[i].Length * Vector3.SizeInBytes;
                var blendIndexSize = BlendIndicies[i].Length * Vector4.SizeInBytes;
                var blendWeightSize = BlendWeights[i].Length * Vector4.SizeInBytes;
                var totalSize = vertSize + normSize + uvSize + primaryUvSize + colorSize + tangentSize + binormalSize + blendIndexSize + blendWeightSize;

                blendIndexOffsets[i] = vertSize + normSize + uvSize + primaryUvSize + colorSize + tangentSize + binormalSize;
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
                GL.BufferSubData(BufferTarget.ArrayBuffer, offset, primaryUvSize, primaryUvs.SelectMany(x => x.ToBytes()).ToArray()); offset += primaryUvSize;  // Primary TexCoords
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

                // Primary UV attribute. Attribute 2 keeps the legacy UV stream so
                // existing shaders remain unchanged; Fire can consume both sets.
                GL.VertexAttribPointer(8, 2, VertexAttribPointerType.Float, false, Vector2.SizeInBytes, offset); offset += primaryUvSize;
                GL.EnableVertexAttribArray(8);

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
            Matrix4[] boneWorldMatrices = null;
            bool canSkin = armature != null && armature.Bones.Count > 0;
            int boneCount = 0;
            if (canSkin)
            {
                boneWorldMatrices = armature.GetWorldMatrices();
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
                if (i < SubmeshVisible.Count && !SubmeshVisible[i])
                {
                    continue;
                }

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
                        var eyePointLightPosition = ResolveEyePointLightPosition(mat.EyePointLightIndex, boneWorldMatrices);
                        mat.Use(view, modelMat, proj, HasVertexColors[i], HasTangents[i], HasBinormals[i], HasUnitUvDomain(i), eyePointLightPosition);
                        mat.ApplySkinning(canSkin && HasSkinning[i], boneCount, skinMatrices);
                    }
                    else if (!RenderOptions.TransparentPass)
                    {
                        continue;
                    }
                }

                // Draw the geometry
                GL.BindVertexArray(VAOs[i]);
                bool requiresDoubleSidedDrawing = IsPokemonModel() ||
                    (i < SubmeshNames.Count &&
                     GetMeshShapeNameFromSubmeshName(SubmeshNames[i]).Contains("_eyelash_", StringComparison.OrdinalIgnoreCase));
                bool restoreCullFace = requiresDoubleSidedDrawing && GL.IsEnabled(EnableCap.CullFace);
                if (restoreCullFace)
                {
                    GL.Disable(EnableCap.CullFace);
                }
                if (!RenderOptions.TransparentPass)
                {
                    GL.DrawElements(PrimitiveType.Triangles, Indices[i].Length, DrawElementsType.UnsignedInt, 0);
                }
                else if (materials != null && materials.Length > 0 && materialMap.TryGetValue(MaterialNames[i], out var mat) && mat.IsTransparent)
                {
                    GL.DrawElements(PrimitiveType.Triangles, Indices[i].Length, DrawElementsType.UnsignedInt, 0);
                }
                if (restoreCullFace)
                {
                    GL.Enable(EnableCap.CullFace);
                }

                GL.BindVertexArray(0);
            }
        }

        private Vector3? ResolveEyePointLightPosition(int pointLightIndex, Matrix4[] boneWorldMatrices)
        {
            if (pointLightIndex <= 0 || armature == null || boneWorldMatrices == null)
            {
                return null;
            }

            var boneName = $"pointlight{pointLightIndex}";
            for (int i = 0; i < armature.Bones.Count && i < boneWorldMatrices.Length; i++)
            {
                if (!string.Equals(armature.Bones[i].Name, boneName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var modelPosition = boneWorldMatrices[i].ExtractTranslation();
                return Vector3.TransformPosition(modelPosition, modelMat);
            }

            return null;
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

        public void ApplyAnimation(
            Animation animation,
            float frame,
            Animation? additiveOverlay = null,
            float additiveOverlayFrame = 0f,
            Animation? mouthOverlay = null,
            float mouthOverlayFrame = 0f,
            Animation? upperFaceOverlay = null,
            float upperFaceOverlayFrame = 0f)
        {
            armature?.ApplyAnimation(
                animation,
                frame,
                additiveOverlay,
                additiveOverlayFrame,
                mouthOverlay,
                mouthOverlayFrame,
                upperFaceOverlay,
                upperFaceOverlayFrame);
            ApplyActionClip(
                upperFaceOverlay?.ActionClip ?? mouthOverlay?.ActionClip ?? additiveOverlay?.ActionClip ?? animation.ActionClip,
                upperFaceOverlay?.ActionClip != null
                    ? upperFaceOverlayFrame
                    : mouthOverlay?.ActionClip != null
                        ? mouthOverlayFrame
                        : additiveOverlay?.ActionClip != null
                            ? additiveOverlayFrame
                            : frame);
        }

        public void ResetPose()
        {
            armature?.ResetPose();
            ResetActionClipState();
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            ResetActionClipState();

            foreach (var material in materials ?? Array.Empty<Material>())
            {
                material?.Dispose();
            }
            materials = Array.Empty<Material>();
            materialMap.Clear();

            foreach (var vao in VAOs ?? Array.Empty<int>())
            {
                if (vao != 0)
                {
                    GL.DeleteVertexArray(vao);
                }
            }
            foreach (var vbo in VBOs ?? Array.Empty<int>())
            {
                if (vbo != 0)
                {
                    GL.DeleteBuffer(vbo);
                }
            }
            foreach (var ebo in EBOs ?? Array.Empty<int>())
            {
                if (ebo != 0)
                {
                    GL.DeleteBuffer(ebo);
                }
            }

            VAOs = Array.Empty<int>();
            VBOs = Array.Empty<int>();
            EBOs = Array.Empty<int>();
            DiagnosticLog.Write($"Model disposed: name={Name}");
        }

        private void ApplyActionClip(ActionClipAnimation? clip, float frame)
        {
            if (!ReferenceEquals(appliedActionClip, clip))
            {
                ResetActionClipState();
                appliedActionClip = clip;
                if (clip != null)
                {
                    DiagnosticLog.Write($"Action clip apply: model={Name}, file={Path.GetFileName(clip.SourcePath)}, visibilityTracks={clip.VisibilityTracks.Count}, materialVector4Tracks={clip.Vector4Tracks.Count}");
                }
            }

            if (clip == null)
            {
                return;
            }

            foreach (var track in clip.VisibilityTracks)
            {
                bool visible = track.Sample(frame);
                for (int i = 0; i < SubmeshNames.Count && i < SubmeshVisible.Count; i++)
                {
                    if (string.Equals(GetMeshShapeNameFromSubmeshName(SubmeshNames[i]), track.TargetName, StringComparison.OrdinalIgnoreCase))
                    {
                        SubmeshVisible[i] = visible;
                    }
                }
            }

            bool closedEyelidVisible = clip.VisibilityTracks.Any(track =>
                IsClosedEyelashTarget(track.TargetName) && track.Sample(frame));
            if (closedEyelidVisible)
            {
                for (int i = 0; i < SubmeshNames.Count && i < SubmeshVisible.Count; i++)
                {
                    var shapeName = GetMeshShapeNameFromSubmeshName(SubmeshNames[i]);
                    if (shapeName.Contains("_eye_mesh_shape", StringComparison.OrdinalIgnoreCase))
                    {
                        SubmeshVisible[i] = false;
                    }
                }
            }

            foreach (var track in clip.Vector4Tracks)
            {
                if (!HasActionClipTarget(track.TargetName) || !materialMap.TryGetValue(track.MaterialName, out var material))
                {
                    continue;
                }

                _ = material.TryGetShaderVector4(track.ParameterName, out var fallback);
                material.SetAnimatedVector4(track.ParameterName, track.Sample(frame, fallback));
            }
        }

        private static bool IsClosedEyelashTarget(string targetName)
        {
            if (string.IsNullOrWhiteSpace(targetName))
            {
                return false;
            }

            const string marker = "_eyelash_";
            int markerIndex = targetName.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            int variantIndex = markerIndex + marker.Length;
            if (markerIndex < 0 || variantIndex >= targetName.Length)
            {
                return false;
            }

            char variant = char.ToLowerInvariant(targetName[variantIndex]);
            // Only a real one-letter mesh variant (for example eyelash_b_mesh_shape)
            // replaces the normal open-eye mesh. "eyelash_mesh_shape" has no variant.
            bool hasVariantBoundary = variantIndex + 1 == targetName.Length || targetName[variantIndex + 1] == '_';
            return hasVariantBoundary && variant != 'a';
        }

        private bool HasActionClipTarget(string targetName)
        {
            if (string.IsNullOrWhiteSpace(targetName))
            {
                return false;
            }

            return SubmeshNames.Any(name =>
                string.Equals(GetMeshShapeNameFromSubmeshName(name), targetName, StringComparison.OrdinalIgnoreCase));
        }

        private void ResetActionClipState()
        {
            for (int i = 0; i < SubmeshVisible.Count; i++)
            {
                SubmeshVisible[i] = i < DefaultSubmeshVisible.Count ? DefaultSubmeshVisible[i] : true;
            }

            foreach (var material in materials ?? Array.Empty<Material>())
            {
                material?.ClearAnimationOverrides();
            }
            appliedActionClip = null;
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

        private bool HasUnitUvDomain(int submeshIndex)
        {
            if (submeshIndex < 0 || submeshIndex >= UVs.Count || submeshIndex >= Indices.Count)
            {
                return true;
            }

            var uvs = UVs[submeshIndex];
            foreach (var index in Indices[submeshIndex])
            {
                if (index >= uvs.Length)
                {
                    continue;
                }

                var uv = uvs[index];
                if (uv.X < -0.0001f || uv.X > 1.0001f || uv.Y < -0.0001f || uv.Y > 1.0001f)
                {
                    return false;
                }
            }

            return true;
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

            // The selection wireframe is drawn over the same triangles as the
            // shaded mesh. Pull the lines forward so animation and camera
            // movement cannot make the two coplanar depth values flicker.
            bool cullFaceWasEnabled = GL.IsEnabled(EnableCap.CullFace);
            bool polygonOffsetLineWasEnabled = GL.IsEnabled(EnableCap.PolygonOffsetLine);
            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
            GL.Disable(EnableCap.CullFace);
            GL.Enable(EnableCap.PolygonOffsetLine);
            GL.PolygonOffset(-1.0f, -1.0f);
            GL.LineWidth(1.5f);
            GL.DrawElements(PrimitiveType.Triangles, indexCount, DrawElementsType.UnsignedInt, 0);
            GL.LineWidth(1.0f);
            if (!polygonOffsetLineWasEnabled)
            {
                GL.Disable(EnableCap.PolygonOffsetLine);
            }
            if (cullFaceWasEnabled)
            {
                GL.Enable(EnableCap.CullFace);
            }
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
