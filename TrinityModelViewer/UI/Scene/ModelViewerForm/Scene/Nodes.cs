using GFTool.Renderer.Scene.GraphicsObjects;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace TrinityModelViewer
{
    public partial class ModelViewerForm
    {
        private void sceneTree_BeforeExpand(object? sender, TreeViewCancelEventArgs e)
        {
            if (e.Node == null)
            {
                return;
            }

            if (e.Node.Tag is not NodeTag tag)
            {
                return;
            }

            switch (tag.Type)
            {
                case NodeType.MeshGroup:
                    EnsureMeshNodes(e.Node, tag.Model);
                    break;
                case NodeType.Mesh:
                    EnsureMaterialsGroupNode(e.Node, tag);
                    break;
                case NodeType.MaterialsGroup:
                    EnsureMaterialNodes(e.Node, tag);
                    break;
                case NodeType.ArmatureGroup:
                    EnsureArmatureNodes(e.Node, tag.Model);
                    break;
                case NodeType.ArmatureBone:
                    EnsureArmatureChildNodes(e.Node, tag);
                    break;
            }
        }

        private void EnsureMeshNodes(TreeNode meshesNode, Model mdl)
        {
            ClearPlaceholderNode(meshesNode);
            if (meshesNode.Nodes.Count > 0)
            {
                return;
            }

            foreach (var entry in BuildMeshEntries(mdl))
            {
                bool hidden = true;
                for (int i = 0; i < entry.SubmeshIndices.Count; i++)
                {
                    if (mdl.IsSubmeshVisible(entry.SubmeshIndices[i]))
                    {
                        hidden = false;
                        break;
                    }
                }

                var meshNode = new TreeNode(entry.Name)
                {
                    Tag = new NodeTag
                    {
                        Type = NodeType.Mesh,
                        Model = mdl,
                        MeshName = entry.Name,
                        SubmeshIndices = entry.SubmeshIndices,
                        MaterialMap = entry.MaterialMap
                    },
                    ForeColor = hidden ? Color.Gray : Color.Empty
                };
                meshNode.Nodes.Add(new TreeNode("..."));
                meshesNode.Nodes.Add(meshNode);
            }
        }

        private void EnsureMaterialsGroupNode(TreeNode meshNode, NodeTag meshTag)
        {
            ClearPlaceholderNode(meshNode);
            foreach (TreeNode child in meshNode.Nodes)
            {
                if (child.Tag is NodeTag tag && tag.Type == NodeType.MaterialsGroup)
                {
                    return;
                }
            }

            var materialsNode = new TreeNode("Materials")
            {
                Tag = new NodeTag
                {
                    Type = NodeType.MaterialsGroup,
                    Model = meshTag.Model,
                    MeshName = meshTag.MeshName,
                    SubmeshIndices = meshTag.SubmeshIndices,
                    MaterialMap = meshTag.MaterialMap
                }
            };
            materialsNode.Nodes.Add(new TreeNode("..."));
            meshNode.Nodes.Add(materialsNode);
        }

        private void EnsureMaterialNodes(TreeNode materialsNode, NodeTag materialsTag)
        {
            ClearPlaceholderNode(materialsNode);
            if (materialsTag.MaterialMap == null)
            {
                return;
            }

            if (materialsNode.Nodes.Count > 0)
            {
                return;
            }

            foreach (var kvp in materialsTag.MaterialMap)
            {
                var materialNode = new TreeNode(kvp.Key)
                {
                    Tag = new NodeTag
                    {
                        Type = NodeType.Material,
                        Model = materialsTag.Model,
                        MaterialName = kvp.Key,
                        SubmeshIndices = kvp.Value
                    }
                };
                materialsNode.Nodes.Add(materialNode);
            }
        }

        private static void ClearPlaceholderNode(TreeNode node)
        {
            for (int i = node.Nodes.Count - 1; i >= 0; i--)
            {
                if (node.Nodes[i].Text == "...")
                {
                    node.Nodes.RemoveAt(i);
                }
            }
        }

        private static List<MeshEntry> BuildMeshEntries(Model mdl)
        {
            var entries = new Dictionary<string, MeshEntry>(StringComparer.OrdinalIgnoreCase);
            var submeshNames = mdl.GetSubmeshNames();
            var submeshMaterials = mdl.GetSubmeshMaterials();
            var count = Math.Min(submeshNames.Count, submeshMaterials.Count);

            for (int i = 0; i < count; i++)
            {
                var displayName = submeshNames[i];
                var colonIndex = displayName.IndexOf(':');
                if (colonIndex > -1)
                {
                    displayName = displayName.Substring(0, colonIndex);
                }

                if (!entries.TryGetValue(displayName, out var entry))
                {
                    entry = new MeshEntry { Name = displayName };
                    entries[displayName] = entry;
                }

                entry.SubmeshIndices.Add(i);
                var materialName = submeshMaterials[i] ?? string.Empty;
                if (!entry.MaterialMap.TryGetValue(materialName, out var indices))
                {
                    indices = new List<int>();
                    entry.MaterialMap[materialName] = indices;
                }
                indices.Add(i);
            }

            return entries.Values.ToList();
        }

        private void EnsureArmatureNodes(TreeNode armatureNode, Model mdl)
        {
            ClearPlaceholderNode(armatureNode);
            if (armatureNode.Nodes.Count > 0)
            {
                return;
            }

            var armature = mdl.GetArmature();
            if (armature == null || armature.Bones.Count == 0)
            {
                return;
            }

            for (int i = 0; i < armature.Bones.Count; i++)
            {
                var parent = armature.Bones[i].ParentIndex;
                if (parent >= 0 && parent < armature.Bones.Count && parent != i)
                {
                    continue;
                }

                armatureNode.Nodes.Add(CreateBoneNode(mdl, armature, i));
            }
        }

        private void EnsureArmatureChildNodes(TreeNode boneNode, NodeTag boneTag)
        {
            ClearPlaceholderNode(boneNode);
            if (boneNode.Nodes.Count > 0)
            {
                return;
            }

            var armature = boneTag.Model.GetArmature();
            if (armature == null || boneTag.BoneIndex == null)
            {
                return;
            }

            foreach (var child in armature.Bones[boneTag.BoneIndex.Value].Children)
            {
                var childIndex = armature.Bones.IndexOf(child);
                if (childIndex < 0)
                {
                    continue;
                }

                boneNode.Nodes.Add(CreateBoneNode(boneTag.Model, armature, childIndex));
            }
        }

        private static TreeNode CreateBoneNode(Model mdl, Armature armature, int boneIndex)
        {
            var bone = armature.Bones[boneIndex];
            var node = new TreeNode(bone.Name)
            {
                Tag = new NodeTag
                {
                    Type = NodeType.ArmatureBone,
                    Model = mdl,
                    BoneIndex = boneIndex
                }
            };

            if (bone.Children.Count > 0)
            {
                node.Nodes.Add(new TreeNode("..."));
            }

            return node;
        }

    }
}
