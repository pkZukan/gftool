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
        private void exportToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var selected = sceneTree.SelectedNode;
            if (selected == null) return;
            if ((selected.Tag as NodeTag)?.Type != NodeType.ModelRoot) return;
            if (!modelMap.TryGetValue(selected, out var mdl) || mdl == null) return;

            ShowGltfExportImportDialog(mdl);
        }

        private void ShowGltfExportImportDialog(Model mdl)
        {
            using var dialog = new Form
            {
                Text = "glTF Export / Import",
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MinimizeBox = false,
                MaximizeBox = false,
                ShowInTaskbar = false,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(12)
            };

            var label = new Label
            {
                AutoSize = true,
                MaximumSize = new Size(560, 0),
                Text =
                    $"Model: {mdl.Name}\n\n" +
                    "- Export glTF: writes a .gltf/.bin and textures\n" +
                    "- Import glTF: converts to Trinity and loads it into the scene\n" +
                    "- File -> Export Trinity... exports the imported model set"
            };

            var exportButton = new Button
            {
                Text = "Export glTF...",
                Size = new Size(160, 28)
            };
            exportButton.Click += (_, __) =>
            {
                dialog.DialogResult = DialogResult.OK;
                dialog.Tag = "export";
                dialog.Close();
            };
            dialog.Controls.Add(exportButton);

            var importButton = new Button
            {
                Text = "Import glTF...",
                Size = new Size(160, 28)
            };
            importButton.Click += (_, __) =>
            {
                dialog.DialogResult = DialogResult.OK;
                dialog.Tag = "import";
                dialog.Close();
            };
            dialog.Controls.Add(importButton);

            var cancel = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Size = new Size(100, 28)
            };
            dialog.CancelButton = cancel;

            var buttonRow = new FlowLayoutPanel
            {
                AutoSize = true,
                WrapContents = false,
                FlowDirection = FlowDirection.LeftToRight,
                Margin = new Padding(0, 10, 0, 0),
                Anchor = AnchorStyles.Right
            };
            buttonRow.Controls.Add(exportButton);
            buttonRow.Controls.Add(importButton);
            buttonRow.Controls.Add(cancel);

            var layout = new TableLayoutPanel
            {
                AutoSize = true,
                ColumnCount = 1,
                RowCount = 2,
                Dock = DockStyle.Fill,
                Margin = new Padding(0)
            };
            layout.Controls.Add(label, 0, 0);
            layout.Controls.Add(buttonRow, 0, 1);
            dialog.Controls.Add(layout);

            ApplyTheme(dialog);
            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            var action = dialog.Tag as string;
            if (string.Equals(action, "export", StringComparison.OrdinalIgnoreCase))
            {
                ExportModelAsGltf(mdl);
                return;
            }

            if (string.Equals(action, "import", StringComparison.OrdinalIgnoreCase))
            {
                ImportGltf(mdl);
            }
        }

        private void ExportModelAsGltf(Model mdl)
        {
            using var sfd = new SaveFileDialog();
            sfd.Filter = "glTF 2.0 (*.gltf)|*.gltf";
            sfd.FileName = $"{mdl.Name}.gltf";
            if (sfd.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            try
            {
                Export.GltfExporter.ExportModel(mdl, sfd.FileName);
                MessageBox.Show(this, $"Exported:\n{sfd.FileName}", "Export glTF", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Export failed:\n{ex.Message}", "Export glTF", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void ImportGltf(Model referenceModel)
        {
            if (!sceneModelManager.TryGetModelSourcePath(referenceModel, out var referenceTrmdlPath) || string.IsNullOrWhiteSpace(referenceTrmdlPath))
            {
                MessageBox.Show(this, "This model doesn't have a disk source path (it may be loaded from a GFPAK).", "Import glTF",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var ofd = new OpenFileDialog();
            ofd.Title = "Select glTF (.gltf)";
            ofd.Filter = "glTF 2.0 (*.gltf)|*.gltf|All files (*.*)|*.*";
            if (ofd.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            string gltfPath = ofd.FileName;
            string tempDir = Path.Combine(Path.GetTempPath(), "TrinityModelViewer", "gltf_import", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            var fallbackRoot = TryResolveDomainRoot(referenceTrmdlPath) ?? (Path.GetDirectoryName(referenceTrmdlPath) ?? string.Empty);
            string outTrmdl = BuildGltfImportOutputTrmdlPath(tempDir, referenceTrmdlPath, fallbackRoot);
            var outDir = Path.GetDirectoryName(outTrmdl);
            if (!string.IsNullOrWhiteSpace(outDir))
            {
                Directory.CreateDirectory(outDir);
            }

            try
            {
                TrinityModelViewer.Export.GltfTrinityPipeline.Export(
                    referenceTrmdlPath,
                    gltfPath,
                    outTrmdl,
                    patchBaseColorTextures: false,
                    exportModelPcBaseOnExport: settings.ExportModelPcBaseOnExport);

                var rootNode = FindModelRootNode(referenceModel);
                if (rootNode != null)
                {
                    RemoveModelNode(rootNode);
                }

                var importProvider = !string.IsNullOrWhiteSpace(fallbackRoot)
                    ? new Trinity.Core.Assets.OverlayDiskAssetProvider(tempDir, fallbackRoot)
                    : null;

                var importedModel = await AddModelToSceneAsync(outTrmdl, assetProvider: importProvider, transient: true);
                if (importedModel == null)
                {
                    return;
                }
                importedModel.ReplaceMaterials(referenceModel.GetMaterials());
                PopulateMaterials(importedModel);
                gltfImportContextByModel[importedModel] = (referenceTrmdlPath, gltfPath);
                // Preserve the original on-disk Trinity file set in the Json Editor after replacing the model with a
                // transient glTF preview model. The preview export may not include editable materials, so keep the
                // reference TRMDL/TRMTR/TRMMT paths discoverable.
                sceneModelManager.SetModelSourcePath(importedModel, referenceTrmdlPath);
                RefreshJsonEditorFileList();

                MessageBox.Show(this,
                    "Imported glTF into the scene (replaced the selected model).\n\n" +
                    "Topology edits (vertex count changes) are supported for meshes without morph targets; export will warn/error if a mesh can't be written back safely.\n\n" +
                    "Use File -> Export Trinity... when ready.",
                    "Import glTF",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Import failed:\n{ex.Message}", "Import glTF", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static string BuildGltfImportOutputTrmdlPath(string tempRoot, string referenceTrmdlPath, string fallbackRoot)
        {
            if (string.IsNullOrWhiteSpace(tempRoot) || string.IsNullOrWhiteSpace(referenceTrmdlPath) || string.IsNullOrWhiteSpace(fallbackRoot))
            {
                return Path.Combine(tempRoot, $"{Path.GetFileNameWithoutExtension(referenceTrmdlPath)}.trmdl");
            }

            try
            {
                // Mirror the reference layout under tempRoot so relative paths like ../../share/... stay within the overlay root.
                var rel = Path.GetRelativePath(fallbackRoot, referenceTrmdlPath);
                if (string.IsNullOrWhiteSpace(rel) || rel.StartsWith("..", StringComparison.Ordinal))
                {
                    return Path.Combine(tempRoot, $"{Path.GetFileNameWithoutExtension(referenceTrmdlPath)}.trmdl");
                }
                return Path.Combine(tempRoot, rel);
            }
            catch
            {
                return Path.Combine(tempRoot, $"{Path.GetFileNameWithoutExtension(referenceTrmdlPath)}.trmdl");
            }
        }

        private static string? TryResolveDomainRoot(string referenceTrmdlPath)
        {
            if (string.IsNullOrWhiteSpace(referenceTrmdlPath))
            {
                return null;
            }

            string normalized = referenceTrmdlPath.Replace('\\', '/');
            string[] tokens =
            {
                "/ik_chara/",
                "/chara/",
                "/pokemon/",
                "/motion_pc_base/",
                "/motion_pc/",
                "/motion_cc_base/",
                "/motion_cc_",
                "/share/"
            };

            int best = -1;
            foreach (var token in tokens)
            {
                int idx = normalized.IndexOf(token, StringComparison.OrdinalIgnoreCase);
                if (idx >= 0 && (best < 0 || idx < best))
                {
                    best = idx;
                }
            }

            if (best < 0)
            {
                return null;
            }

            string rootPart = normalized.Substring(0, best);
            if (string.IsNullOrWhiteSpace(rootPart))
            {
                return null;
            }

            // Convert back to platform separators and normalize to a directory.
            rootPart = rootPart.Replace('/', Path.DirectorySeparatorChar);
            try
            {
                return Path.GetFullPath(rootPart);
            }
            catch
            {
                return rootPart;
            }
        }

    }
}
