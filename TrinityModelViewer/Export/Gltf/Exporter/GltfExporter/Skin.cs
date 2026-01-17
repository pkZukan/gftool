using GFTool.Renderer.Scene.GraphicsObjects;
using OpenTK.Mathematics;
using System.Collections.Generic;
using System.Linq;

namespace TrinityModelViewer.Export
{
    internal static partial class GltfExporter
    {
        private static int AddSkinOnly(GltfRoot gltf, BinaryBufferBuilder buffer, Armature armature, int[] boneNodeIndices)
        {
            int boneCount = armature.Bones.Count;
            var invBind = new Matrix4[boneCount];
            for (int i = 0; i < boneCount; i++)
            {
                var b = armature.Bones[i];
                var m = b.HasJointInverseBind ? b.JointInverseBindWorld : b.InverseBindWorld;
                invBind[i] = Matrix4.Transpose(m);
            }

            int accessor = AddAccessorMat4(gltf, buffer, invBind);
            var skin = new GltfSkin
            {
                InverseBindMatrices = accessor,
                Joints = boneNodeIndices.ToList(),
                Skeleton = boneNodeIndices.Length > 0 ? boneNodeIndices[0] : null
            };

            int skinIndex = gltf.Skins.Count;
            gltf.Skins.Add(skin);
            return skinIndex;
        }

        private static void AddDummySkinnedMesh(GltfRoot gltf, BinaryBufferBuilder buffer, int rootNodeIndex, int skinIndex)
        {
            // Single point at origin, weighted 100% to joint 0.
            var positions = new[] { Vector3.Zero };
            var joints = new[] { new Vector4(0, 0, 0, 0) };
            var weights = new[] { new Vector4(1, 0, 0, 0) };
            var indices = new uint[] { 0 };

            int posAcc = AddAccessorVec3(gltf, buffer, positions, target: 34962, includeMinMax: true);
            int jointAcc = AddAccessorUShort4(gltf, buffer, joints, target: 34962);
            int weightAcc = AddAccessorVec4(gltf, buffer, weights, target: 34962);
            int idxAcc = AddAccessorIndices(gltf, buffer, indices);

            var prim = new GltfPrimitive
            {
                Attributes = new Dictionary<string, int>
                {
                    ["POSITION"] = posAcc,
                    ["JOINTS_0"] = jointAcc,
                    ["WEIGHTS_0"] = weightAcc
                },
                Indices = idxAcc,
                Mode = 0 // POINTS
            };

            var mesh = new GltfMesh
            {
                Name = "SkinDummy",
                Primitives = new List<GltfPrimitive> { prim }
            };

            int meshIndex = gltf.Meshes.Count;
            gltf.Meshes.Add(mesh);

            int nodeIndex = gltf.Nodes.Count;
            gltf.Nodes.Add(new GltfNode
            {
                Name = "SkinDummy",
                Mesh = meshIndex,
                Skin = skinIndex
            });

            gltf.Nodes[rootNodeIndex].Children ??= new List<int>();
            gltf.Nodes[rootNodeIndex].Children.Add(nodeIndex);
        }

        private static (int skinIndex, int[] boneNodeIndices) AddSkin(GltfRoot gltf, BinaryBufferBuilder buffer, Armature armature, int rootNodeIndex)
        {
            int boneCount = armature.Bones.Count;
            var boneNodeIndices = new int[boneCount];

            for (int i = 0; i < boneCount; i++)
            {
                var bone = armature.Bones[i];
                var node = new GltfNode
                {
                    Name = bone.Name,
                    Translation = new[] { bone.RestPosition.X, bone.RestPosition.Y, bone.RestPosition.Z },
                    Rotation = new[] { bone.RestRotation.X, bone.RestRotation.Y, bone.RestRotation.Z, bone.RestRotation.W },
                    Scale = new[] { bone.RestScale.X, bone.RestScale.Y, bone.RestScale.Z },
                    Children = new List<int>()
                };
                boneNodeIndices[i] = gltf.Nodes.Count;
                gltf.Nodes.Add(node);
            }

            for (int i = 0; i < boneCount; i++)
            {
                int parent = armature.Bones[i].ParentIndex;
                if (parent >= 0 && parent < boneCount && parent != i)
                {
                    gltf.Nodes[boneNodeIndices[parent]].Children!.Add(boneNodeIndices[i]);
                }
                else
                {
                    gltf.Nodes[rootNodeIndex].Children!.Add(boneNodeIndices[i]);
                }
            }

            // Inverse bind matrices prefer joint info when present. Computed inverse bind is used otherwise.
            var invBind = new Matrix4[boneCount];
            for (int i = 0; i < boneCount; i++)
            {
                var b = armature.Bones[i];
                // Renderer uses row vector math (v' = v * M). glTF uses column vector math (v' = M * v).
                // A transpose is applied on export.
                var m = b.HasJointInverseBind ? b.JointInverseBindWorld : b.InverseBindWorld;
                invBind[i] = Matrix4.Transpose(m);
            }

            int accessor = AddAccessorMat4(gltf, buffer, invBind);

            var skin = new GltfSkin
            {
                InverseBindMatrices = accessor,
                Joints = boneNodeIndices.ToList(),
                Skeleton = boneNodeIndices[0]
            };

            int skinIndex = gltf.Skins.Count;
            gltf.Skins.Add(skin);
            return (skinIndex, boneNodeIndices);
        }

        private static int[] AddSkeletonNodes(GltfRoot gltf, Armature armature, int rootNodeIndex)
        {
            int boneCount = armature.Bones.Count;
            var boneNodeIndices = new int[boneCount];

            for (int i = 0; i < boneCount; i++)
            {
                var bone = armature.Bones[i];
                var node = new GltfNode
                {
                    Name = bone.Name,
                    Translation = new[] { bone.RestPosition.X, bone.RestPosition.Y, bone.RestPosition.Z },
                    Rotation = new[] { bone.RestRotation.X, bone.RestRotation.Y, bone.RestRotation.Z, bone.RestRotation.W },
                    Scale = new[] { bone.RestScale.X, bone.RestScale.Y, bone.RestScale.Z },
                    Children = new List<int>()
                };
                boneNodeIndices[i] = gltf.Nodes.Count;
                gltf.Nodes.Add(node);
            }

            for (int i = 0; i < boneCount; i++)
            {
                int parent = armature.Bones[i].ParentIndex;
                if (parent >= 0 && parent < boneCount && parent != i)
                {
                    gltf.Nodes[boneNodeIndices[parent]].Children!.Add(boneNodeIndices[i]);
                }
                else
                {
                    gltf.Nodes[rootNodeIndex].Children!.Add(boneNodeIndices[i]);
                }
            }

            return boneNodeIndices;
        }
    }
}
