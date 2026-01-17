using GFTool.Renderer.Core;
using GFTool.Renderer.Scene.GraphicsObjects;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Trinity.Core.Assets;
using TrinityModelViewer.Export;

namespace TrinityModelViewer
{
    public partial class ModelViewerForm
    {
        private bool shaderWarmupCompleted;
        private Task? flatSharpWarmupTask;

		        private void ExportTrinityFromSelection()
		        {
		            var selected = sceneTree.SelectedNode;
		            if (selected?.Tag is not NodeTag tag || tag.Type != NodeType.ModelRoot)
	            {
	                MessageBox.Show(this, "Select a model root node first (the top-level model entry in the scene tree).", "Export Trinity",
	                    MessageBoxButtons.OK, MessageBoxIcon.Information);
	                return;
	            }

		            bool hasGltfPreview = gltfImportContextByModel.TryGetValue(tag.Model, out var ctx);
		            string? referenceTrmdlPath = hasGltfPreview
		                ? ctx.ReferenceTrmdlPath
		                : (sceneModelManager.TryGetModelSourcePath(tag.Model, out var src) ? src : null);

	            if (string.IsNullOrWhiteSpace(referenceTrmdlPath) || !File.Exists(referenceTrmdlPath))
	            {
	                MessageBox.Show(this, "Could not resolve the source .trmdl path for this model.", "Export Trinity",
	                    MessageBoxButtons.OK, MessageBoxIcon.Error);
	                return;
	            }

	            using var sfd = new SaveFileDialog();
	            sfd.Title = "Export Trinity Model Set (.trmdl)";
	            sfd.Filter = "TRMDL (*.trmdl)|*.trmdl";
	            string initialDir = Environment.CurrentDirectory;
	            if (!string.IsNullOrWhiteSpace(settings.LastExportTrinityDirectory) && Directory.Exists(settings.LastExportTrinityDirectory))
	            {
	                initialDir = settings.LastExportTrinityDirectory;
	            }
		            else
		            {
		                var srcDir = Path.GetDirectoryName(referenceTrmdlPath);
		                if (!string.IsNullOrWhiteSpace(srcDir))
		                {
		                    initialDir = Path.Combine(srcDir, "trinity_export");
		                }
		            }

		            if (!Directory.Exists(initialDir))
		            {
		                Directory.CreateDirectory(initialDir);
		            }

	            sfd.InitialDirectory = initialDir;
	            sfd.FileName = Path.GetFileName(referenceTrmdlPath);
	            if (sfd.ShowDialog(this) != DialogResult.OK)
	            {
	                return;
	            }

		            try
		            {
		                var outFull = Path.GetFullPath(sfd.FileName);
		                var refFull = Path.GetFullPath(referenceTrmdlPath);
		                if (string.Equals(outFull, refFull, StringComparison.OrdinalIgnoreCase))
	                {
	                    MessageBox.Show(this, "Refusing to export over the original imported .trmdl. Pick a different output path.", "Export Trinity",
	                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
	                    return;
	                }

		                if (hasGltfPreview)
		                {
		                    TrinityModelViewer.Export.GltfTrinityPipeline.Export(
		                        referenceTrmdlPath,
		                        ctx.GltfPath,
		                        sfd.FileName,
		                        patchBaseColorTextures: false,
		                        exportModelPcBaseOnExport: settings.ExportModelPcBaseOnExport);
		                }
		                else
		                {
		                    TrinityModelViewer.Export.TrinityModelSetExporter.ExportCopy(referenceTrmdlPath, sfd.FileName);
		                }

		                var patchNotes = new List<string>();
		                if (settings.AutoGenerateLodsOnExport)
		                {
		                    if (TrinityModelViewer.Export.TrmdlLodPatcher.ForceAllLodsToUseMesh0(sfd.FileName, out var lodError))
		                    {
		                        patchNotes.Add("Auto-generate LODs: forced all LODs to use LOD0 mesh (placeholder).");
		                    }
		                    else if (!string.IsNullOrWhiteSpace(lodError))
		                    {
		                        patchNotes.Add($"Auto-generate LODs failed: {lodError}");
		                    }
		                }
		                TryPatchExportedTrinityMaterials(sfd.FileName, tag.Model, patchNotes);

		                settings.LastExportTrinityDirectory = Path.GetDirectoryName(sfd.FileName) ?? settings.LastExportTrinityDirectory;
		                settings.Save();
		                var msg = $"Exported:\n{sfd.FileName}";
		                if (patchNotes.Count > 0)
		                {
		                    msg += "\n\nNotes:\n- " + string.Join("\n- ", patchNotes.Distinct());
		                }
		                MessageBox.Show(this, msg, "Export Trinity", MessageBoxButtons.OK, MessageBoxIcon.Information);
		            }
			            catch (Exception ex)
			            {
			                MessageBox.Show(this, $"Export failed:\n{ex.Message}", "Export Trinity", MessageBoxButtons.OK, MessageBoxIcon.Error);
			            }
			        }

		        private void TryPatchExportedTrinityMaterials(string exportedTrmdlPath, Model model, List<string> notes)
		        {
		            if (model == null) throw new ArgumentNullException(nameof(model));
		            if (notes == null) throw new ArgumentNullException(nameof(notes));

		            bool hasUniformEdits = model.GetMaterials().Any(m => m.HasUniformOverrides);
		            bool hasMetadataEdits = model.HasMaterialMetadataSelectionOverrides || model.HasMaterialMetadataValueOverrides;
		            if (!hasUniformEdits && !hasMetadataEdits)
		            {
		                return;
		            }

		            if (string.IsNullOrWhiteSpace(exportedTrmdlPath) || !File.Exists(exportedTrmdlPath))
		            {
		                notes.Add("Skipped applying runtime material edits (exported TRMDL not found).");
		                return;
		            }

		            Trinity.Core.Flatbuffers.TR.Model.TRMDL? trmdl = null;
		            try
		            {
		                trmdl = Trinity.Core.Utils.FlatBufferConverter.DeserializeFrom<Trinity.Core.Flatbuffers.TR.Model.TRMDL>(exportedTrmdlPath);
		            }
		            catch (Exception ex)
		            {
		                notes.Add($"Failed to read exported TRMDL for material patching: {ex.Message}");
		                return;
		            }

		            if (trmdl == null)
		            {
		                notes.Add("Failed to read exported TRMDL for material patching.");
		                return;
		            }

		            var outputDir = Path.GetDirectoryName(Path.GetFullPath(exportedTrmdlPath)) ?? Environment.CurrentDirectory;
		            var outputDirFull = Path.GetFullPath(outputDir).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;

		            var trmtrPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		            var materialRels = trmdl.Materials ?? Array.Empty<string>();
		            foreach (var relRaw in materialRels)
		            {
		                if (string.IsNullOrWhiteSpace(relRaw))
		                {
		                    continue;
		                }

		                var rel = relRaw.Replace('\\', '/');
		                var abs = Path.GetFullPath(Path.Combine(outputDir, rel));
		                if (!abs.StartsWith(outputDirFull, StringComparison.OrdinalIgnoreCase))
		                {
		                    continue;
		                }

		                trmtrPaths.Add(abs);

		                // Patch sibling material-set variants too (`<stem>_00.trmtr`, etc), if present.
		                try
		                {
		                    var relDir = Path.GetDirectoryName(rel)?.Replace('\\', '/') ?? string.Empty;
		                    var dirAbs = string.IsNullOrWhiteSpace(relDir) ? outputDir : Path.Combine(outputDir, relDir);
		                    if (!Directory.Exists(dirAbs))
		                    {
		                        continue;
		                    }

		                    var stem = Path.GetFileNameWithoutExtension(rel);
		                    if (string.IsNullOrWhiteSpace(stem))
		                    {
		                        continue;
		                    }

		                    foreach (var variant in Directory.EnumerateFiles(dirAbs, stem + "_*.trmtr"))
		                    {
		                        trmtrPaths.Add(Path.GetFullPath(variant));
		                    }
		                }
		                catch
		                {
		                    // Ignore.
		                }
		            }

		            int trmtrProcessed = 0;
		            if (hasUniformEdits && trmtrPaths.Count > 0)
		            {
		                foreach (var path in trmtrPaths)
		                {
		                    if (!File.Exists(path))
		                    {
		                        continue;
		                    }
		                    try
		                    {
		                        TrmtrBinaryPatcher.PatchTrmtrInPlace(path, model);
		                        trmtrProcessed++;
		                    }
		                    catch (Exception ex)
		                    {
		                        notes.Add($"TRMTR patch failed for '{Path.GetFileName(path)}': {ex.Message}");
		                    }
		                }
		            }

			            if (trmtrProcessed > 0)
			            {
			                notes.Add($"Applied Params-tab overrides to {trmtrProcessed} TRMTR file(s).");
			            }
			            if (hasUniformEdits && trmtrProcessed == 0)
			            {
			                notes.Add("No TRMTR files were available to patch with Params-tab overrides.");
			            }
			        }

		        private void ExportTrinityPatchFromSelection()
		        {
		            var selected = sceneTree.SelectedNode;
		            if (selected?.Tag is not NodeTag tag || tag.Type != NodeType.ModelRoot)
	            {
	                MessageBox.Show(this, "Select a model root node first (the top-level model entry in the scene tree).", "Export Trinity (Edited Only)",
	                    MessageBoxButtons.OK, MessageBoxIcon.Information);
	                return;
	            }

		            if (!sceneModelManager.TryGetModelSourcePath(tag.Model, out var referenceTrmdlPath) ||
		                string.IsNullOrWhiteSpace(referenceTrmdlPath) ||
		                !File.Exists(referenceTrmdlPath))
		            {
	                MessageBox.Show(this, "Could not resolve the source .trmdl path for this model.", "Export Trinity (Edited Only)",
	                    MessageBoxButtons.OK, MessageBoxIcon.Error);
	                return;
	            }

			            if (!tag.Model.GetMaterials().Any(m => m.HasUniformOverrides) &&
			                !tag.Model.GetMaterials().SelectMany(m => m.Textures).Any(t => t.IsEdited) &&
			                !tag.Model.HasMaterialMetadataSelectionOverrides &&
			                !tag.Model.HasMaterialMetadataValueOverrides)
			            {
			                MessageBox.Show(this, "No edited materials/textures detected for this model.", "Export Trinity (Edited Only)",
			                    MessageBoxButtons.OK, MessageBoxIcon.Information);
			                return;
		            }

	            using var fbd = new FolderBrowserDialog();
	            fbd.Description = "Select output folder for edited assets (writes only modified files).";
	            var refDir = Path.GetDirectoryName(referenceTrmdlPath) ?? Environment.CurrentDirectory;
	            if (!string.IsNullOrWhiteSpace(settings.LastExportTrinityDirectory) && Directory.Exists(settings.LastExportTrinityDirectory))
	            {
	                fbd.SelectedPath = settings.LastExportTrinityDirectory;
	            }
	            else
	            {
	                fbd.SelectedPath = Path.Combine(refDir, "trinity_export_patch");
	            }

	            if (fbd.ShowDialog(this) != DialogResult.OK || string.IsNullOrWhiteSpace(fbd.SelectedPath))
	            {
	                return;
	            }

	            var outputRoot = fbd.SelectedPath;
	            Directory.CreateDirectory(outputRoot);

	            int exportedCount = 0;
	            var warnings = new List<string>();

	            try
	            {
	                var mdl = tag.Model;

		                if (!string.IsNullOrWhiteSpace(mdl.CurrentMaterialFilePath) &&
		                    File.Exists(mdl.CurrentMaterialFilePath) &&
		                    mdl.GetMaterials().Any(m => m.HasUniformOverrides))
		                {
	                    string rel = Path.GetRelativePath(refDir, mdl.CurrentMaterialFilePath);
	                    if (rel.StartsWith(".."))
	                    {
	                        rel = Path.GetFileName(mdl.CurrentMaterialFilePath);
	                    }
	                    string dst = Path.Combine(outputRoot, rel);
	                    Directory.CreateDirectory(Path.GetDirectoryName(dst) ?? outputRoot);
		                    TrinityModelViewer.Export.EditedMaterialExporter.ExportEditedTrmtr(mdl.CurrentMaterialFilePath, mdl, dst);
		                    exportedCount++;
		                }

			                if (mdl.HasMaterialMetadataSelectionOverrides || mdl.HasMaterialMetadataValueOverrides)
			                {
			                    var trmmtSource = Path.ChangeExtension(referenceTrmdlPath, ".trmmt");
			                    if (!string.IsNullOrWhiteSpace(trmmtSource) && File.Exists(trmmtSource))
			                    {
		                        string rel = Path.GetRelativePath(refDir, trmmtSource);
		                        if (rel.StartsWith(".."))
		                        {
		                            rel = Path.GetFileName(trmmtSource);
		                        }
		                        string dst = Path.Combine(outputRoot, rel);
		                        Directory.CreateDirectory(Path.GetDirectoryName(dst) ?? outputRoot);
		                        TrinityModelViewer.Export.EditedMaterialMetadataExporter.ExportEditedTrmmt(trmmtSource, mdl, dst);
		                        exportedCount++;
		                    }
		                }

	                foreach (var tex in mdl.GetMaterials().SelectMany(m => m.Textures).DistinctBy(t => t.CacheKey))
	                {
	                    if (!tex.IsEdited)
	                    {
	                        continue;
	                    }

	                    if (!tex.TryGetEditedBitmap(out var bmp))
	                    {
	                        continue;
	                    }

	                    using (bmp)
	                    {
	                        string srcPath = string.Empty;
	                        tex.TryGetResolvedSourcePath(out srcPath);
	                        string rel = Path.GetRelativePath(refDir, srcPath);
	                        if (rel.StartsWith(".."))
	                        {
	                            rel = Path.GetFileName(srcPath);
	                        }

	                        string ext = Path.GetExtension(rel);
	                        if (!ext.Equals(".png", StringComparison.OrdinalIgnoreCase) &&
	                            !ext.Equals(".jpg", StringComparison.OrdinalIgnoreCase) &&
	                            !ext.Equals(".jpeg", StringComparison.OrdinalIgnoreCase) &&
	                            !ext.Equals(".bmp", StringComparison.OrdinalIgnoreCase))
	                        {
	                            warnings.Add($"Edited texture '{tex.Name}' was sourced from '{ext}'. Exporting as PNG (BNTX encode not supported).");
	                            rel = Path.ChangeExtension(rel, ".png");
	                        }

	                        string dst = Path.Combine(outputRoot, rel);
	                        Directory.CreateDirectory(Path.GetDirectoryName(dst) ?? outputRoot);
	                        bmp.Save(dst, System.Drawing.Imaging.ImageFormat.Png);
	                        exportedCount++;
	                    }
	                }

	                settings.LastExportTrinityDirectory = outputRoot;
	                settings.Save();
	            }
	            catch (Exception ex)
	            {
	                MessageBox.Show(this, $"Export failed:\n{ex.Message}", "Export Trinity (Edited Only)", MessageBoxButtons.OK, MessageBoxIcon.Error);
	                return;
	            }

	            var msg = $"Exported {exportedCount} edited file(s) to:\n{outputRoot}";
	            if (warnings.Count > 0)
	            {
	                msg += "\n\nNotes:\n- " + string.Join("\n- ", warnings.Distinct());
	            }
	            MessageBox.Show(this, msg, "Export Trinity (Edited Only)", MessageBoxButtons.OK, MessageBoxIcon.Information);
	        }

	        private void ClearAll()
	        {
	            sceneModelManager.DisposeAssetProviders();

	            renderCtrl.renderer.ClearScene();
	            renderCtrl.renderer.StopAnimation();
	            messageListView.Items.Clear();
            materialList.Items.Clear();
            materialList.Columns.Clear();
            modelMap.Clear();
            sceneTree.Nodes.Clear();
            animations.Clear();
            animationsList.Items.Clear();
            loadedAnimationPaths.Clear();
	            currentMaterialsModel = null;
	            currentMaterial = null;
	            ClearMaterialDetails();
	            sceneModelManager.ClearSceneTracking();
	        }

        private void UpdateUvOverrideMenuChecks()
        {
            if (contextMenuNode?.Tag is not NodeTag tag || tag.SubmeshIndices == null || tag.SubmeshIndices.Count == 0)
            {
                return;
            }

            var (layerMaskOverride, aoOverride) = tag.Model.GetUvOverrides(tag.SubmeshIndices[0]);

            if (layerMaskUvMaterialContextMenuItem != null) layerMaskUvMaterialContextMenuItem.Checked = layerMaskOverride == UvSetOverride.Material;
            if (layerMaskUv0ContextMenuItem != null) layerMaskUv0ContextMenuItem.Checked = layerMaskOverride == UvSetOverride.Uv0;
            if (layerMaskUv1ContextMenuItem != null) layerMaskUv1ContextMenuItem.Checked = layerMaskOverride == UvSetOverride.Uv1;

            if (aoUvMaterialContextMenuItem != null) aoUvMaterialContextMenuItem.Checked = aoOverride == UvSetOverride.Material;
            if (aoUv0ContextMenuItem != null) aoUv0ContextMenuItem.Checked = aoOverride == UvSetOverride.Uv0;
            if (aoUv1ContextMenuItem != null) aoUv1ContextMenuItem.Checked = aoOverride == UvSetOverride.Uv1;
        }

        private void layerMaskUvMaterialToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SetMeshLayerMaskUvOverride(UvSetOverride.Material);
        }

        private void layerMaskUv0ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SetMeshLayerMaskUvOverride(UvSetOverride.Uv0);
        }

        private void layerMaskUv1ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SetMeshLayerMaskUvOverride(UvSetOverride.Uv1);
        }

        private void aoUvMaterialToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SetMeshAOUvOverride(UvSetOverride.Material);
        }

        private void aoUv0ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SetMeshAOUvOverride(UvSetOverride.Uv0);
        }

        private void aoUv1ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SetMeshAOUvOverride(UvSetOverride.Uv1);
        }

        private void SetMeshLayerMaskUvOverride(UvSetOverride value)
        {
            if (contextMenuNode?.Tag is not NodeTag tag || tag.SubmeshIndices == null || tag.SubmeshIndices.Count == 0)
            {
                return;
            }

            foreach (var submeshIndex in tag.SubmeshIndices)
            {
                var uvOverrides = tag.Model.GetUvOverrides(submeshIndex);
                tag.Model.SetUvOverrides(submeshIndex, value, uvOverrides.AO);
            }

            UpdateUvOverrideMenuChecks();
            renderCtrl.Invalidate();
        }

        private void SetMeshAOUvOverride(UvSetOverride value)
        {
            if (contextMenuNode?.Tag is not NodeTag tag || tag.SubmeshIndices == null || tag.SubmeshIndices.Count == 0)
            {
                return;
            }

            foreach (var submeshIndex in tag.SubmeshIndices)
            {
                var uvOverrides = tag.Model.GetUvOverrides(submeshIndex);
                tag.Model.SetUvOverrides(submeshIndex, uvOverrides.LayerMask, value);
            }

            UpdateUvOverrideMenuChecks();
            renderCtrl.Invalidate();
        }

        private void ExportEditedMaterialsForCurrentModel()
        {
            var mdl = currentMaterialsModel;
            if (mdl == null)
            {
                MessageBox.Show(this, "No model selected.", "Export Materials", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (string.IsNullOrWhiteSpace(mdl.CurrentMaterialFilePath) || !File.Exists(mdl.CurrentMaterialFilePath))
            {
                MessageBox.Show(this, "Could not resolve the source .trmtr path for the current material set.", "Export Materials", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

		            if (!mdl.GetMaterials().Any(m => m.HasUniformOverrides) &&
		                !mdl.HasMaterialMetadataSelectionOverrides &&
		                !mdl.HasMaterialMetadataValueOverrides)
		            {
		                var r = MessageBox.Show(this, "No edited material parameters detected (no overrides).\nExport anyway?", "Export Materials",
		                    MessageBoxButtons.YesNo, MessageBoxIcon.Question);
		                if (r != DialogResult.Yes)
		                {
                    return;
                }
            }

            using var sfd = new SaveFileDialog();
            sfd.Title = "Export Edited Materials (.trmtr)";
            sfd.Filter = "TRMTR (*.trmtr)|*.trmtr";
            sfd.FileName = Path.GetFileName(mdl.CurrentMaterialFilePath);
            if (sfd.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

		            try
		            {
		                TrinityModelViewer.Export.EditedMaterialExporter.ExportEditedTrmtr(mdl.CurrentMaterialFilePath, mdl, sfd.FileName);
		                string? trmmtOut = null;
		                if (mdl.HasMaterialMetadataSelectionOverrides || mdl.HasMaterialMetadataValueOverrides)
		                {
		                    string? trmmtSource = mdl.LoadedMaterialMetadataPath ?? mdl.PreferredMaterialMetadataPath;
		                    if (string.IsNullOrWhiteSpace(trmmtSource) && !string.IsNullOrWhiteSpace(mdl.CurrentMaterialFilePath))
		                    {
		                        trmmtSource = Path.ChangeExtension(mdl.CurrentMaterialFilePath, ".trmmt");
		                    }

			                    if (string.IsNullOrWhiteSpace(trmmtSource) && sceneModelManager.TryGetModelSourcePath(mdl, out var trmdlSource) && !string.IsNullOrWhiteSpace(trmdlSource))
			                    {
			                        trmmtSource = Path.ChangeExtension(trmdlSource, ".trmmt");
			                    }

		                    if (!string.IsNullOrWhiteSpace(trmmtSource) && File.Exists(trmmtSource))
		                    {
		                        // Keep the exported metadata filename consistent with the imported source.
		                        var outDir = Path.GetDirectoryName(sfd.FileName) ?? Environment.CurrentDirectory;
		                        trmmtOut = Path.Combine(outDir, Path.GetFileName(trmmtSource));
		                        TrinityModelViewer.Export.EditedMaterialMetadataExporter.ExportEditedTrmmt(trmmtSource, mdl, trmmtOut);
		                    }
		                    else
		                    {
		                        MessageHandler.Instance.AddMessage(MessageType.LOG,
		                            $"[Export] Skipped TRMMT export (source missing). Looked for '{trmmtSource ?? "<null>"}'.");
		                    }
		                }

		                var msg = trmmtOut == null
		                    ? $"Exported:\n{sfd.FileName}"
		                    : $"Exported:\n{sfd.FileName}\n{trmmtOut}";
                MessageBox.Show(this, msg, "Export Materials", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Export failed:\n{ex.Message}", "Export Materials", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task<Model?> AddModelToSceneAsync(string filePath, IAssetProvider? assetProvider = null, bool transient = false)
        {
            bool ownsLoad = modelLoadDepth == 0;
            if (ownsLoad)
            {
                BeginModelLoad();
            }

            try
            {
                ReportModelLoadProgress(0);

                var progress = new Progress<float>(p =>
                {
                    int percent = (int)Math.Round(Math.Clamp(p, 0.0f, 1.0f) * 100.0);
                    ReportModelLoadProgress(percent);
                });

                var token = modelLoadCts?.Token ?? CancellationToken.None;
                var mdl = assetProvider == null
                    ? await renderCtrl.renderer.AddSceneModelAsync(filePath, settings.LoadAllLods, token: token, progress: progress)
                    : await renderCtrl.renderer.AddSceneModelAsync(assetProvider, filePath, settings.LoadAllLods, token: token, progress: progress);

                var node = new TreeNode(mdl.Name)
                {
                    Tag = new NodeTag
                    {
                        Type = NodeType.ModelRoot,
                        Model = mdl
                    }
                };
                modelMap.Add(node, mdl);
                sceneTree.Nodes.Add(node);
                PopulateSubmeshes(node, mdl);
                PopulateMaterials(mdl);
                ReportModelLoadProgress(85);

	                if (assetProvider == null)
	                {
	                    TryAutoLoadAnimations(filePath);
	                }
	                else
	                {
	                    sceneModelManager.RegisterAssetProvider(assetProvider);
	                }
                ReportModelLoadProgress(95);

                // Default to "solo" display for the most recently added model unless multi-model display is enabled.
                ApplyModelVisibility(mdl);
                sceneTree.SelectedNode = node;
                node.EnsureVisible();

	                if (assetProvider == null && !transient)
	                {
	                    settings.LastModelPath = filePath;
	                    settings.Save();
	                    UpdateLastModelMenu();
	                    AddRecentModel(filePath);
	                    sceneModelManager.AddLoadedModelPath(filePath);
	                    sceneModelManager.SetModelSourcePath(mdl, filePath);
	                }

                if (settings.ShowMultipleModels && renderCtrl.renderer.HasActiveAnimation())
                {
                    renderCtrl.renderer.SetAnimationTargets(modelMap.Values);
                }

                ReportModelLoadProgress(100);
                return mdl;
            }
            catch (OperationCanceledException)
            {
                MessageHandler.Instance.AddMessage(MessageType.LOG, "[Load] Model load canceled.");
                return null;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Failed to load model:\n{ex.Message}", "Load Model", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
            finally
            {
                if (ownsLoad)
                {
                    EndModelLoad();
                }
            }
        }

        private void SelectMaterialByName(string materialName)
        {
            if (string.IsNullOrWhiteSpace(materialName))
            {
                return;
            }

            foreach (ListViewItem item in materialList.Items)
            {
                if (item.Tag is Material mat && string.Equals(mat.Name, materialName, StringComparison.OrdinalIgnoreCase))
                {
                    item.Selected = true;
                    item.Focused = true;
                    item.EnsureVisible();
                    break;
                }
            }
        }
    }
}
