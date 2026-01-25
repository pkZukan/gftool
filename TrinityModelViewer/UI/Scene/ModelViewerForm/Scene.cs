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
        private void sceneTree_MouseUp(object sender, MouseEventArgs e)
        {
            Point ClickPoint = new Point(e.X, e.Y);
            TreeNode ClickNode = sceneTree.GetNodeAt(ClickPoint);
            sceneTree.SelectedNode = ClickNode;
            if (ClickNode == null) return;

            if (e.Button == MouseButtons.Right)
            {
                ConfigureSceneContextMenu(ClickNode);
                Point ScreenPoint = sceneTree.PointToScreen(ClickPoint);
                Point FormPoint = this.PointToClient(ScreenPoint);
                sceneTreeCtxtMenu.Show(this, FormPoint);
            }
        }

        private void ConfigureSceneContextMenu(TreeNode node)
        {
            bool isModelRoot = (node.Tag as NodeTag)?.Type == NodeType.ModelRoot;
            exportToolStripMenuItem.Visible = isModelRoot;
            deleteToolStripMenuItem.Visible = isModelRoot;
            toggleModelVisibilityToolStripMenuItem.Visible = isModelRoot;
            if (isModelRoot && modelMap.TryGetValue(node, out var mdl) && mdl != null)
            {
                toggleModelVisibilityToolStripMenuItem.Text = sceneModelManager.IsHidden(mdl) ? "Unhide" : "Hide";
            }
        }

        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var selected = sceneTree.SelectedNode;
            if (selected != null)
            {
                modelMap.TryGetValue(selected, out var mdl);
                if (mdl == null) return;

                renderCtrl.renderer.RemoveSceneModel(mdl);
                sceneTree.Nodes.Remove(selected);
                modelMap.Remove(selected);
                sceneModelManager.SetHidden(mdl, hidden: false);
                if (settings.ShowMultipleModels && renderCtrl.renderer.HasActiveAnimation())
                {
                    renderCtrl.renderer.SetAnimationTargets(modelMap.Values);
                }
                materialList.Items.Clear();
                ClearMaterialDetails();
            }
        }

        private void toggleModelVisibilityToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var selected = sceneTree.SelectedNode;
            if (selected == null)
            {
                return;
            }

            if ((selected.Tag as NodeTag)?.Type != NodeType.ModelRoot)
            {
                return;
            }

            if (!modelMap.TryGetValue(selected, out var mdl) || mdl == null)
            {
                return;
            }

            sceneModelManager.SetHidden(mdl, !sceneModelManager.IsHidden(mdl));

            ApplyModelVisibility(GetSelectedModelFromSceneTree());
        }

        private TreeNode? FindModelRootNode(Model model)
        {
            foreach (TreeNode node in sceneTree.Nodes)
            {
                if (node.Tag is NodeTag tag && tag.Type == NodeType.ModelRoot && ReferenceEquals(tag.Model, model))
                {
                    return node;
                }
            }
            return null;
        }

        private void RemoveModelNode(TreeNode modelRootNode)
        {
            if (modelRootNode?.Tag is not NodeTag tag || tag.Type != NodeType.ModelRoot)
            {
                return;
            }

            if (!modelMap.TryGetValue(modelRootNode, out var mdl) || mdl == null)
            {
                sceneTree.Nodes.Remove(modelRootNode);
                return;
            }

            renderCtrl.renderer.RemoveSceneModel(mdl);
            sceneTree.Nodes.Remove(modelRootNode);
            modelMap.Remove(modelRootNode);
            sceneModelManager.SetHidden(mdl, hidden: false);
            sceneModelManager.RemoveModelSourcePath(mdl);
            gltfImportContextByModel.Remove(mdl);

            if (settings.ShowMultipleModels && renderCtrl.renderer.HasActiveAnimation())
            {
                renderCtrl.renderer.SetAnimationTargets(modelMap.Values);
            }

            if (ReferenceEquals(currentMaterialsModel, mdl))
            {
                materialList.Items.Clear();
                ClearMaterialDetails();
            }
        }

        private void PopulateSubmeshes(TreeNode node, Model mdl)
        {
            node.Nodes.Clear();
            var meshesNode = new TreeNode("Meshes")
            {
                Tag = new NodeTag
                {
                    Type = NodeType.MeshGroup,
                    Model = mdl
                }
            };
            meshesNode.Nodes.Add(new TreeNode("..."));
            node.Nodes.Add(meshesNode);

            var armatureNode = new TreeNode("Armature")
            {
                Tag = new NodeTag
                {
                    Type = NodeType.ArmatureGroup,
                    Model = mdl
                }
            };
            armatureNode.Nodes.Add(new TreeNode("..."));
            node.Nodes.Add(armatureNode);
            node.Expand();
        }

        private void sceneTree_AfterSelect(object? sender, TreeViewEventArgs e)
        {
            ClearSubmeshSelections();
            renderCtrl.Invalidate();
            if (e.Node == null)
            {
                ApplyModelVisibility(null);
                return;
            }

            if (e.Node.Tag is not NodeTag tag)
            {
                ApplyModelVisibility(null);
                return;
            }

            ApplyModelVisibility(tag.Model);
            if (renderCtrl?.renderer != null && renderCtrl.renderer.HasActiveAnimation())
            {
                UpdateAnimationTargetsForCurrentMode();
            }

            if (tag.Type == NodeType.Mesh && tag.SubmeshIndices != null && tag.SubmeshIndices.Count > 0)
            {
                int sub = GetFirstVisibleSubmesh(tag.Model, tag.SubmeshIndices);
                if (sub >= 0)
                {
                    tag.Model.SetSelectedSubmesh(sub);
                }
                renderCtrl.Invalidate();
                return;
            }

            if (tag.Type == NodeType.Material && tag.SubmeshIndices != null && tag.SubmeshIndices.Count > 0)
            {
                int sub = GetFirstVisibleSubmesh(tag.Model, tag.SubmeshIndices);
                if (sub >= 0)
                {
                    tag.Model.SetSelectedSubmesh(sub);
                }
                renderCtrl.Invalidate();
                if (!string.IsNullOrWhiteSpace(tag.MaterialName))
                {
                    SelectMaterialByName(tag.MaterialName);
                }
            }
        }

        private static int GetFirstVisibleSubmesh(Model model, List<int> indices)
        {
            for (int i = 0; i < indices.Count; i++)
            {
                int idx = indices[i];
                if (model.IsSubmeshVisible(idx))
                {
                    return idx;
                }
            }
            return -1;
        }

        private Model? GetSelectedModelFromSceneTree()
        {
            var selected = sceneTree.SelectedNode;
            if (selected == null)
            {
                return null;
            }

            var node = selected;
            while (node != null)
            {
                if (modelMap.TryGetValue(node, out var m) && m != null)
                {
                    return m;
                }

                node = node.Parent;
            }

            return null;
        }

        private void ApplyModelVisibility(Model? focusedModel)
        {
            bool showAll = settings.ShowMultipleModels;
            foreach (var mdl in modelMap.Values)
            {
                bool isHidden = sceneModelManager.IsHidden(mdl);
                bool shouldShow = !isHidden && (showAll || focusedModel == null || ReferenceEquals(mdl, focusedModel));
                mdl.SetVisible(shouldShow);
            }

            renderCtrl.Invalidate();
        }

        private void sceneTree_NodeMouseDoubleClick(object? sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Node == null)
            {
                return;
            }

            if (e.Node.Tag is not NodeTag tag)
            {
                return;
            }

            if (tag.Type == NodeType.Material && !string.IsNullOrWhiteSpace(tag.MaterialName))
            {
                SelectMaterialByName(tag.MaterialName);
            }
        }

        private void sceneTree_NodeMouseClick(object? sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Button != MouseButtons.Right || e.Node == null)
            {
                return;
            }

            sceneTree.SelectedNode = e.Node;
            contextMenuNode = e.Node;

            if (e.Node.Tag is not NodeTag tag)
            {
                return;
            }

            if ((tag.Type != NodeType.Mesh && tag.Type != NodeType.Material) || tag.SubmeshIndices == null || tag.SubmeshIndices.Count == 0)
            {
                return;
            }

            EnsureSceneTreeContextMenu();
            UpdateSceneTreeContextMenuForMesh(tag, e.Node);
            sceneTreeContextMenu?.Show(sceneTree, e.Location);
        }

        private void EnsureSceneTreeContextMenu()
        {
            if (sceneTreeContextMenu != null)
            {
                return;
            }

            sceneTreeContextMenu = new ContextMenuStrip();
            toggleMeshVisibilityMenuItem = new ToolStripMenuItem("Hide Mesh");
            toggleMeshVisibilityMenuItem.Click += (sender, e) => ToggleMeshVisibilityFromContextMenu();
            sceneTreeContextMenu.Items.Add(toggleMeshVisibilityMenuItem);

            assignMeshMaterialMenuItem = new ToolStripMenuItem("Assign Material...");
            assignMeshMaterialMenuItem.Click += (sender, e) => AssignMeshMaterialFromContextMenu();
            sceneTreeContextMenu.Items.Add(assignMeshMaterialMenuItem);

            sceneTreeContextMenu.Items.Add(new ToolStripSeparator());

            uvOverridesContextMenuItem = new ToolStripMenuItem("UV Overrides");
            layerMaskUvContextMenuItem = new ToolStripMenuItem("Layer Mask");
            layerMaskUvMaterialContextMenuItem = new ToolStripMenuItem("Material");
            layerMaskUv0ContextMenuItem = new ToolStripMenuItem("UV0");
            layerMaskUv1ContextMenuItem = new ToolStripMenuItem("UV1");
            layerMaskUvMaterialContextMenuItem.Click += layerMaskUvMaterialToolStripMenuItem_Click;
            layerMaskUv0ContextMenuItem.Click += layerMaskUv0ToolStripMenuItem_Click;
            layerMaskUv1ContextMenuItem.Click += layerMaskUv1ToolStripMenuItem_Click;
            layerMaskUvContextMenuItem.DropDownItems.AddRange(new ToolStripItem[]
            {
                layerMaskUvMaterialContextMenuItem,
                layerMaskUv0ContextMenuItem,
                layerMaskUv1ContextMenuItem
            });

            aoUvContextMenuItem = new ToolStripMenuItem("AO");
            aoUvMaterialContextMenuItem = new ToolStripMenuItem("Material");
            aoUv0ContextMenuItem = new ToolStripMenuItem("UV0");
            aoUv1ContextMenuItem = new ToolStripMenuItem("UV1");
            aoUvMaterialContextMenuItem.Click += aoUvMaterialToolStripMenuItem_Click;
            aoUv0ContextMenuItem.Click += aoUv0ToolStripMenuItem_Click;
            aoUv1ContextMenuItem.Click += aoUv1ToolStripMenuItem_Click;
            aoUvContextMenuItem.DropDownItems.AddRange(new ToolStripItem[]
            {
                aoUvMaterialContextMenuItem,
                aoUv0ContextMenuItem,
                aoUv1ContextMenuItem
            });

            uvOverridesContextMenuItem.DropDownItems.AddRange(new ToolStripItem[]
            {
                layerMaskUvContextMenuItem,
                aoUvContextMenuItem
            });
            sceneTreeContextMenu.Items.Add(uvOverridesContextMenuItem);
            UpdateUvOverrideMenuChecks();
        }

        private void AssignMeshMaterialFromContextMenu()
        {
            if (contextMenuNode?.Tag is not NodeTag tag)
            {
                return;
            }

            if ((tag.Type != NodeType.Mesh && tag.Type != NodeType.Material) || tag.SubmeshIndices == null || tag.SubmeshIndices.Count == 0)
            {
                return;
            }

            var names = tag.Model.GetMaterials()
                .Select(m => m?.Name)
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (names.Count == 0)
            {
                MessageBox.Show(this, "This model has no loaded materials to assign.", "Assign Material", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string initial = string.Empty;
            if (tag.Type == NodeType.Material && !string.IsNullOrWhiteSpace(tag.MaterialName))
            {
                initial = tag.MaterialName!;
            }
            var existing = tag.Model.GetSubmeshMaterials();
            if (tag.SubmeshIndices[0] >= 0 && tag.SubmeshIndices[0] < existing.Count)
            {
                initial = existing[tag.SubmeshIndices[0]] ?? string.Empty;
            }

            if (!TryPromptSelect("Assign Material", "Material:", names, initial, out var selected))
            {
                return;
            }

            foreach (var idx in tag.SubmeshIndices)
            {
                tag.Model.SetSubmeshMaterialName(idx, selected);
            }

            RefreshSceneTreeAfterMaterialReassignment(tag.Model, contextMenuNode);
            renderCtrl.Invalidate();
        }

        private void RefreshSceneTreeAfterMaterialReassignment(Model model, TreeNode? sourceNode)
        {
            var meshNode = FindOwningMeshNode(sourceNode);
            if (meshNode == null || meshNode.Tag is not NodeTag meshTag || meshTag.Type != NodeType.Mesh)
            {
                RefreshSceneTreeForModel(model);
                return;
            }

            RefreshMeshNodeMaterials(meshNode, model);
        }

        private static TreeNode? FindOwningMeshNode(TreeNode? node)
        {
            var current = node;
            while (current != null)
            {
                if (current.Tag is NodeTag tag && tag.Type == NodeType.Mesh)
                {
                    return current;
                }
                current = current.Parent;
            }
            return null;
        }

        private void RefreshMeshNodeMaterials(TreeNode meshNode, Model model)
        {
            if (meshNode.Tag is not NodeTag meshTag || meshTag.Type != NodeType.Mesh)
            {
                return;
            }

            string meshName = meshTag.MeshName ?? meshNode.Text;
            var entry = BuildMeshEntries(model).FirstOrDefault(e => string.Equals(e.Name, meshName, StringComparison.OrdinalIgnoreCase));
            if (entry == null)
            {
                RefreshSceneTreeForModel(model);
                return;
            }

            bool wasMeshExpanded = meshNode.IsExpanded;
            bool wasMaterialsExpanded = false;
            TreeNode? materialsNode = null;
            foreach (TreeNode child in meshNode.Nodes)
            {
                if (child.Tag is NodeTag childTag && childTag.Type == NodeType.MaterialsGroup)
                {
                    materialsNode = child;
                    wasMaterialsExpanded = child.IsExpanded;
                    break;
                }
            }

            meshTag.SubmeshIndices = entry.SubmeshIndices;
            meshTag.MaterialMap = entry.MaterialMap;
            bool hidden = true;
            for (int i = 0; i < entry.SubmeshIndices.Count; i++)
            {
                if (model.IsSubmeshVisible(entry.SubmeshIndices[i]))
                {
                    hidden = false;
                    break;
                }
            }
            meshNode.ForeColor = hidden ? Color.Gray : Color.Empty;

            if (materialsNode != null && materialsNode.Tag is NodeTag materialsTag)
            {
                materialsTag.SubmeshIndices = entry.SubmeshIndices;
                materialsTag.MaterialMap = entry.MaterialMap;

                materialsNode.Nodes.Clear();
                materialsNode.Nodes.Add(new TreeNode("..."));
                if (wasMaterialsExpanded)
                {
                    EnsureMaterialNodes(materialsNode, materialsTag);
                    materialsNode.Expand();
                }
            }
            else if (wasMeshExpanded)
            {
                meshNode.Nodes.Clear();
                EnsureMaterialsGroupNode(meshNode, meshTag);
                meshNode.Expand();
                if (meshNode.Nodes.Count > 0 && meshNode.Nodes[0].Tag is NodeTag t && t.Type == NodeType.MaterialsGroup)
                {
                    if (wasMaterialsExpanded)
                    {
                        EnsureMaterialNodes(meshNode.Nodes[0], t);
                        meshNode.Nodes[0].Expand();
                    }
                }
            }

            if (wasMeshExpanded)
            {
                meshNode.Expand();
            }
        }

        private void RefreshSceneTreeForModel(Model model)
        {
            foreach (TreeNode node in sceneTree.Nodes)
            {
                if (node.Tag is NodeTag tag && tag.Type == NodeType.ModelRoot && ReferenceEquals(tag.Model, model))
                {
                    PopulateSubmeshes(node, model);
                    return;
                }
            }
        }

        private bool TryPromptSelect(string title, string label, IReadOnlyList<string> options, string initialValue, out string selected)
        {
            selected = string.Empty;
            if (options == null || options.Count == 0)
            {
                return false;
            }

            using var form = new Form
            {
                Text = title,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MinimizeBox = false,
                MaximizeBox = false,
                ShowInTaskbar = false,
                ClientSize = new Size(420, 115)
            };

            var lbl = new Label
            {
                Text = label,
                AutoSize = true,
                Location = new Point(12, 15)
            };
            form.Controls.Add(lbl);

            var combo = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(12, 38),
                Width = form.ClientSize.Width - 24
            };

            foreach (var option in options)
            {
                combo.Items.Add(option);
            }

            int initialIndex = 0;
            if (!string.IsNullOrWhiteSpace(initialValue))
            {
                for (int i = 0; i < options.Count; i++)
                {
                    if (string.Equals(options[i], initialValue, StringComparison.OrdinalIgnoreCase))
                    {
                        initialIndex = i;
                        break;
                    }
                }
            }
            combo.SelectedIndex = Math.Clamp(initialIndex, 0, options.Count - 1);
            form.Controls.Add(combo);

            var ok = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Location = new Point(form.ClientSize.Width - 180, 76),
                Width = 80
            };
            var cancel = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Location = new Point(form.ClientSize.Width - 92, 76),
                Width = 80
            };
            form.Controls.Add(ok);
            form.Controls.Add(cancel);
            form.AcceptButton = ok;
            form.CancelButton = cancel;

            if (form.ShowDialog(this) != DialogResult.OK)
            {
                return false;
            }

            selected = combo.SelectedItem?.ToString() ?? string.Empty;
            return !string.IsNullOrWhiteSpace(selected);
        }

        private void UpdateSceneTreeContextMenuForMesh(NodeTag tag, TreeNode node)
        {
            if (toggleMeshVisibilityMenuItem == null)
            {
                return;
            }

            bool hidden = IsMeshHidden(tag);
            var hideLabel = tag.Type == NodeType.Material ? "Section" : "Mesh";
            toggleMeshVisibilityMenuItem.Text = hidden ? $"Unhide {hideLabel}" : $"Hide {hideLabel}";
            toggleMeshVisibilityMenuItem.Tag = tag;

            if (assignMeshMaterialMenuItem != null)
            {
                assignMeshMaterialMenuItem.Tag = tag;
                assignMeshMaterialMenuItem.Text = tag.Type == NodeType.Material
                    ? "Reassign Material..."
                    : "Assign Material (All Sections)...";
            }

            node.ForeColor = hidden ? Color.Gray : Color.Empty;
            UpdateUvOverrideMenuChecks();
        }

        private bool IsMeshHidden(NodeTag tag)
        {
            if (tag.SubmeshIndices == null || tag.SubmeshIndices.Count == 0)
            {
                return false;
            }

            foreach (var idx in tag.SubmeshIndices)
            {
                if (tag.Model.IsSubmeshVisible(idx))
                {
                    return false;
                }
            }

            return true;
        }

        private void ToggleMeshVisibilityFromContextMenu()
        {
            if (contextMenuNode?.Tag is not NodeTag tag)
            {
                return;
            }

            if ((tag.Type != NodeType.Mesh && tag.Type != NodeType.Material) || tag.SubmeshIndices == null || tag.SubmeshIndices.Count == 0)
            {
                return;
            }

            bool hidden = IsMeshHidden(tag);
            bool newVisible = hidden;
            foreach (var idx in tag.SubmeshIndices)
            {
                tag.Model.SetSubmeshVisible(idx, newVisible);
            }

            contextMenuNode.ForeColor = newVisible ? Color.Empty : Color.Gray;
            ClearSubmeshSelections();
            renderCtrl.Invalidate();
        }

        private void ClearSubmeshSelections()
        {
            foreach (var mdl in modelMap.Values)
            {
                mdl.SetSelectedSubmesh(-1);
            }
        }

        private void ModelViewerForm_DragEnter(object? sender, DragEventArgs e)
        {
            if (e.Data?.GetData(DataFormats.FileDrop) is string[] files &&
                files.Any(path =>
                    string.Equals(Path.GetExtension(path), ".trmdl", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(Path.GetExtension(path), ".gfpak", StringComparison.OrdinalIgnoreCase)))
            {
                e.Effect = DragDropEffects.Copy;
            }
        }

        private async void ModelViewerForm_DragDrop(object? sender, DragEventArgs e)
        {
            if (e.Data?.GetData(DataFormats.FileDrop) is not string[] files || files.Length == 0)
            {
                return;
            }

            var gfpakFiles = files
                .Where(path => string.Equals(Path.GetExtension(path), ".gfpak", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (gfpakFiles.Count > 0)
            {
                if (gfpakFiles.Count > 1)
                {
                    MessageBox.Show(this, "Please drop a single GFPAK file at a time.", "GFPAK",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

                await OpenGfpakAsync(gfpakFiles[0]);
                return;
            }

            var modelFiles = files
                .Where(path => string.Equals(Path.GetExtension(path), ".trmdl", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (modelFiles.Count == 0)
            {
                return;
            }

            BeginModelLoad();
            try
            {
                ClearAll();
                foreach (var modelFile in modelFiles)
                {
                    await AddModelToSceneAsync(modelFile);
                }
            }
            finally
            {
                EndModelLoad();
            }
        }
    }
}
