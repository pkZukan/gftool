using GFTool.Core.Utils;
using GFTool.Renderer.Core;
using GFTool.Renderer.Core.Graphics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using Trinity.Core.Flatbuffers.TR.Model;
using Trinity.Core.Utils;
using System.IO;
using System;
using Trinity.Core.Assets;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;


namespace GFTool.Renderer.Scene.GraphicsObjects
{
	    public partial class Model : RefObject
	    {
        private void ParseMeshBuffer(TRVertexDeclaration vertDesc, TRBuffer[] vertexBuffers, TRBuffer indexBuf, TRIndexFormat polyType, long start, long count, TRBoneWeight[]? boneWeights, string meshName)
        {
            if (vertexBuffers == null || vertexBuffers.Length == 0)
            {
                return;
            }

            var posElement = vertDesc.vertexElements.FirstOrDefault(e => e.Usage == TRVertexUsage.POSITION);
            if (posElement == null)
            {
                return;
            }

            // TRMSH vertex elements use:
            // - Slot: which vertex buffer/stride to read from
            // - Layer: semantic "layer" (ex: TEX_COORD0/1, COLOR0/1, BLEND_INDEX0/1, ...)
            //
            // Some external TRMSH/TRMBF tooling treats AttributeLayer as the UV set index.
            // Using Layer as the vertex buffer index causes UV1+ (and other layered attributes) to be read
            // from the wrong buffer (often out-of-range), making them appear missing/zeroed.
            var posBuffer = GetVertexBuffer(vertexBuffers, posElement.Slot);
            if (posBuffer == null)
            {
                return;
            }

            var posStride = GetStride(vertDesc, posElement.Slot);
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
            TRVertexFormat? blendIndexFormat = null;
            TRVertexFormat? blendWeightFormat = null;
            blendIndexStats = null;

            List<uint> indices = new List<uint>();
            long currPos = 0;

            var blendIndexStreams = new List<Vector4[]>();
            var blendWeightStreams = new List<Vector4[]>();

            var uvStreams = new List<Vector2[]>();
            bool colorElementConsumed = false;

            for (int i = 0; i < vertDesc.vertexElements.Length; i++)
            {
                var att = vertDesc.vertexElements[i];
                var buffer = GetVertexBuffer(vertexBuffers, att.Slot);
                if (buffer == null)
                {
                    continue;
                }

                var stride = GetStride(vertDesc, att.Slot);
                if (stride <= 0)
                {
                    continue;
                }

                int? blendIndexStreamIndex = null;
                int? blendWeightStreamIndex = null;
                int? uvStreamIndex = null;
                if (att.Usage == TRVertexUsage.BLEND_INDEX)
                {
                    int layer = Math.Max(att.Layer, 0);
                    EnsureBlendStream(blendIndexStreams, layer, vertexCount);
                    blendIndexStreamIndex = layer;
                }
                else if (att.Usage == TRVertexUsage.BLEND_WEIGHTS)
                {
                    int layer = Math.Max(att.Layer, 0);
                    EnsureBlendStream(blendWeightStreams, layer, vertexCount);
                    blendWeightStreamIndex = layer;
                }
                else if (att.Usage == TRVertexUsage.TEX_COORD)
                {
                    int layer = Math.Max(att.Layer, 0);
                    EnsureUvStream(uvStreams, layer, vertexCount);
                    uvStreamIndex = layer;
                }

                for (int v = 0; v < vertexCount; v++)
                {
                    int offset = (v * stride) + att.Offset;
                    if (!HasBytes(buffer.Bytes, offset, att.Format))
                    {
                        continue;
                    }

                    switch (att.Usage)
                    {
                        case TRVertexUsage.POSITION:
                            pos[v] = ReadVector3(buffer.Bytes, offset, att.Format);
                            break;
                        case TRVertexUsage.NORMAL:
                            norm[v] = ReadNormal(buffer.Bytes, offset, att.Format);
                            hasNormals = true;
                            break;
                        case TRVertexUsage.TEX_COORD:
                            if (uvStreamIndex.HasValue)
                            {
                                uvStreams[uvStreamIndex.Value][v] = ReadVector2(buffer.Bytes, offset, att.Format);
                            }
                            hasUvs = true;
                            break;
                        case TRVertexUsage.COLOR:
                            if (colorElementConsumed)
                            {
                                break;
                            }
                            color[v] = ReadColor(buffer.Bytes, offset, att.Format);
                            hasColors = true;
                            colorElementConsumed = true;
                            break;
                        case TRVertexUsage.TANGENT:
                            tangent[v] = ReadTangent(buffer.Bytes, offset, att.Format);
                            hasTangents = true;
                            break;
                        case TRVertexUsage.BINORMAL:
                            binormal[v] = ReadNormal(buffer.Bytes, offset, att.Format);
                            hasBinormals = true;
                            break;
                        case TRVertexUsage.BLEND_INDEX:
                            if (blendIndexStreamIndex.HasValue)
                            {
                                blendIndexStreams[blendIndexStreamIndex.Value][v] = ReadBlendIndices(buffer.Bytes, offset, att.Format);
                            }
                            hasBlendIndices = true;
                            blendIndexFormat ??= att.Format;
                            break;
                        case TRVertexUsage.BLEND_WEIGHTS:
                            if (blendWeightStreamIndex.HasValue)
                            {
                                blendWeightStreams[blendWeightStreamIndex.Value][v] = ReadBlendWeights(buffer.Bytes, offset, att.Format);
                            }
                            hasBlendWeights = true;
                            blendWeightFormat ??= att.Format;
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

            Vector2[] uv0 = uv;
            Vector2[] uv1 = new Vector2[vertexCount];
            if (hasUvs && uvStreams.Count > 0)
            {
                uv0 = uvStreams[0];
                if (uvStreams.Count > 1)
                {
                    uv1 = uvStreams[1];
                }
            }

            bool hasUv1 = false;
            if (hasUvs && uvStreams.Count > 1)
            {
                for (int v = 0; v < uv1.Length; v++)
                {
                    if (uv1[v].LengthSquared > 0.0000001f)
                    {
                        hasUv1 = true;
                        break;
                    }
                }
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
            UVs.Add(hasUvs ? uv0 : new Vector2[vertexCount]);
            UVs2.Add(hasUvs ? uv1 : new Vector2[vertexCount]);
            HasUv1.Add(hasUv1);
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
            if (MessageHandler.Instance.DebugLogsEnabled && (hasBlendIndices || hasBlendWeights))
            {
                MessageHandler.Instance.AddMessage(
                    MessageType.LOG,
                    $"[SkinFmt] mesh={meshName} blendIndexFmt={(blendIndexFormat?.ToString() ?? "<none>")} blendWeightFmt={(blendWeightFormat?.ToString() ?? "<none>")}");
            }

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
            var msh = LoadFlat<TRMSH>(file);
            var buffers = LoadFlat<TRMBF>(modelPath.Combine(msh.bufferFilePath)).TRMeshBuffers;
            var shapeCnt = msh.Meshes.Count();
            for (int i = 0; i < shapeCnt; i++)
            {
                var meshShape = msh.Meshes[i];
                var vertBufs = buffers[i].VertexBuffer;
                var indexBuf = buffers[i].IndexBuffer[0]; //LOD0
                var polyType = meshShape.IndexType;
                int boneWeightCount = meshShape.boneWeight?.Length ?? 0;

                if (MessageHandler.Instance.DebugLogsEnabled)
                {
                    MessageHandler.Instance.AddMessage(
                        MessageType.LOG,
                        $"[TRMSH] mesh={meshShape.Name} unk7='{meshShape.MeshUnk7}' meshName='{meshShape.MeshName}' parts={(meshShape.meshParts?.Length ?? 0)}");
                }

                foreach (var part in meshShape.meshParts)
                {
                    MaterialNames.Add(part.MaterialName);
                    SubmeshNames.Add($"{meshShape.Name}:{part.MaterialName}");
                    SubmeshParentNodeNames.Add(FirstNonEmpty(meshShape.MeshUnk7, meshShape.MeshName));
                    int declIndex = part.vertexDeclarationIndex;
                    if (declIndex < 0 || declIndex >= meshShape.vertexDeclaration.Length)
                    {
                        declIndex = 0;
                    }
                    ParseMeshBuffer(meshShape.vertexDeclaration[declIndex], vertBufs, indexBuf, meshShape.IndexType, part.indexOffset, part.indexCount, meshShape.boneWeight, meshShape.Name);
                    if (MessageHandler.Instance.DebugLogsEnabled)
                    {
                        int submeshIndex = Positions.Count - 1;
                        bool hasSkin = HasSkinning.Count > submeshIndex && HasSkinning[submeshIndex];
                        string? parentName = SubmeshParentNodeNames.Count > submeshIndex ? SubmeshParentNodeNames[submeshIndex] : null;
                        MessageHandler.Instance.AddMessage(
                            MessageType.LOG,
                            $"[TRMSH] submesh={submeshIndex} name='{SubmeshNames[submeshIndex]}' skinning={hasSkin} parent='{parentName}'");
                    }
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

	    }
}
