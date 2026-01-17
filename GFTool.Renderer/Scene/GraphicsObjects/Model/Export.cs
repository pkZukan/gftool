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
	    }
}
