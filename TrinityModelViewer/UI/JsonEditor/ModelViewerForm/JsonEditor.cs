using GFTool.Renderer.Core;
using GFTool.Renderer.Scene.GraphicsObjects;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using TrinityModelViewer.UI.JsonEditor;
using JsonEditorEntry = TrinityModelViewer.UI.JsonEditor.JsonEditorService.JsonEditorEntry;

namespace TrinityModelViewer
{
    public partial class ModelViewerForm
    {
        private readonly JsonEditorService jsonEditorService = new JsonEditorService();

        private void SetupJsonEditorTab()
        {
            if (jsonEditorTabPage != null)
            {
                return;
            }

            jsonEditorTabPage = new TabPage { Text = "Json Editor" };

            var root = new Panel { Dock = DockStyle.Fill };

            var header = new Panel { Dock = DockStyle.Bottom, Height = 28 };
            var addJsonFileButton = new Button
            {
                Text = "Add file...",
                Dock = DockStyle.Right,
                Width = 90
            };
            addJsonFileButton.Click += (s, e) => AddJsonEditorFile();
            refreshJsonFilesButton = new Button
            {
                Text = "Refresh",
                Dock = DockStyle.Right,
                Width = 90
            };
            refreshJsonFilesButton.Click += (s, e) => RefreshJsonEditorFileList();
            header.Controls.Add(refreshJsonFilesButton);
            header.Controls.Add(addJsonFileButton);

            jsonFilesGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                RowHeadersVisible = false,
                ColumnHeadersVisible = true,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoGenerateColumns = false,
                ClipboardCopyMode = DataGridViewClipboardCopyMode.Disable
            };

            jsonFilesGrid.EnableHeadersVisualStyles = false;

            jsonFilesGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Type",
                HeaderText = "Type",
                Width = 70,
                ReadOnly = true
            });
            jsonFilesGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Model",
                HeaderText = "File",
                Width = 160,
                ReadOnly = true
            });
            jsonFilesGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Path",
                HeaderText = "Path",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                ReadOnly = true
            });

            jsonFilesGrid.ColumnHeadersHeight = 24;

            jsonFilesGrid.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex >= 0)
                {
                    OpenSelectedJsonEntry();
                }
            };
            jsonFilesGrid.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    OpenSelectedJsonEntry();
                    e.Handled = true;
                }
            };

            var contextMenu = new ContextMenuStrip();
            var editMenuItem = new ToolStripMenuItem("Edit...");
            editMenuItem.Click += (s, e) => OpenSelectedJsonEntry();
            contextMenu.Items.Add(editMenuItem);
            contextMenu.Items.Add(new ToolStripSeparator());

            var copyPathMenuItem = new ToolStripMenuItem("Copy path");
            copyPathMenuItem.Click += (s, e) =>
            {
                var entry = GetSelectedJsonEditorEntry();
                if (!string.IsNullOrWhiteSpace(entry.Path))
                {
                    Clipboard.SetText(entry.Path);
                }
            };
            contextMenu.Items.Add(copyPathMenuItem);

            jsonFilesGrid.ContextMenuStrip = contextMenu;
            jsonFilesGrid.CellMouseDown += (s, e) =>
            {
                if (e.Button != MouseButtons.Right || e.RowIndex < 0)
                {
                    return;
                }

                jsonFilesGrid.ClearSelection();
                jsonFilesGrid.Rows[e.RowIndex].Selected = true;
                jsonFilesGrid.CurrentCell = jsonFilesGrid.Rows[e.RowIndex].Cells[Math.Max(0, e.ColumnIndex)];
            };

            root.Controls.Add(header);
            root.Controls.Add(jsonFilesGrid);

            jsonEditorTabPage.Controls.Add(root);
            leftTabs.TabPages.Add(jsonEditorTabPage);

            leftTabs.SelectedIndexChanged += (s, e) =>
            {
                if (leftTabs.SelectedTab == jsonEditorTabPage)
                {
                    RefreshJsonEditorFileList();
                }
            };

            ApplyTheme(jsonEditorTabPage);
        }

        private void RefreshJsonEditorFileList()
        {
            if (jsonFilesGrid == null)
            {
                return;
            }

            var entries = jsonEditorService.EnumerateFlatbufferEntriesInScene(
                    sceneModelManager.ModelSourcePaths,
                    message => MessageHandler.Instance.AddMessage(MessageType.LOG, message))
                .OrderBy(e => e.Type, StringComparer.OrdinalIgnoreCase)
                .ThenBy(e => Path.GetFileNameWithoutExtension(e.Path), StringComparer.OrdinalIgnoreCase)
                .ThenBy(e => e.Path, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            jsonFilesGrid.SuspendLayout();
            try
            {
                jsonFilesGrid.Rows.Clear();
                foreach (var e in entries)
                {
                    int rowIndex = jsonFilesGrid.Rows.Add(e.Type, Path.GetFileNameWithoutExtension(e.Path), e.Path);
                    jsonFilesGrid.Rows[rowIndex].Tag = e;
                }
            }
            finally
            {
                jsonFilesGrid.ResumeLayout();
            }
        }

        private void AddJsonEditorFile()
        {
            using var ofd = new OpenFileDialog();
            ofd.Title = "Add FlatBuffer file to Json Editor";
            ofd.Filter =
                "Trinity FlatBuffers|*.trmdl;*.trmsh;*.trmbf;*.trskl;*.trmtr;*.trmmt|All files|*.*";
            ofd.Multiselect = true;

            if (ofd.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            EnsureJsonEditorTabSelected();

            if (jsonFilesGrid == null)
            {
                return;
            }

            var existingPaths = new List<string>();
            foreach (DataGridViewRow row in jsonFilesGrid.Rows)
            {
                if (row.Tag is JsonEditorEntry entry && !string.IsNullOrWhiteSpace(entry.Path))
                {
                    existingPaths.Add(entry.Path);
                }
            }

            var defaultModel = currentMaterialsModel ?? sceneModelManager.ModelSourcePaths.Keys.FirstOrDefault();
            foreach (var entry in jsonEditorService.CreateManualEntries(ofd.FileNames ?? Array.Empty<string>(), existingPaths, defaultModel))
            {
                int rowIndex = jsonFilesGrid.Rows.Add(entry.Type, Path.GetFileNameWithoutExtension(entry.Path), entry.Path);
                jsonFilesGrid.Rows[rowIndex].Tag = entry;
            }
        }

        private void EnsureJsonEditorTabSelected()
        {
            SetupJsonEditorTab();
            if (jsonEditorTabPage != null)
            {
                leftTabs.SelectedTab = jsonEditorTabPage;
            }
        }

        private void OpenSelectedJsonEntry()
        {
            var entry = GetSelectedJsonEditorEntry();
            if (string.IsNullOrWhiteSpace(entry.Type))
            {
                return;
            }

            OpenFlatbufferJsonEditor(entry);
        }

        private JsonEditorEntry GetSelectedJsonEditorEntry()
        {
            if (jsonFilesGrid == null)
            {
                return default;
            }

            DataGridViewRow? row = null;
            if (jsonFilesGrid.SelectedRows.Count > 0)
            {
                row = jsonFilesGrid.SelectedRows[0];
            }
            else if (jsonFilesGrid.CurrentRow != null)
            {
                row = jsonFilesGrid.CurrentRow;
            }

            return row?.Tag is JsonEditorEntry entry ? entry : default;
        }

        private void OpenFlatbufferJsonEditor(JsonEditorEntry entry)
        {
            if (!File.Exists(entry.Path))
            {
                MessageBox.Show(this, $"File not found:\n{entry.Path}", "Json Editor", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            byte[] bytes;
            try
            {
                bytes = File.ReadAllBytes(entry.Path);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Read failed:\n{ex.Message}", "Json Editor", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string json;
            string kind = entry.Type;
            try
            {
                json = jsonEditorService.BuildFlatbufferJson(kind, bytes);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"JSON conversion failed:\n{ex.Message}", "Json Editor", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            bool allowApplyExport = kind == "TRMTR" || kind == "TRMMT";
            using var editor = new FlatbufferJsonEditorForm($"{kind} JSON - {Path.GetFileName(entry.Path)}", entry.Path, json, allowApplyExport, allowApplyExport);
            ApplyTheme(editor);
            editor.ApplyRequested += (_, editedJson) =>
            {
                try
                {
                    ApplyFlatbufferJsonToScene(kind, entry, editedJson);
                    RequestMaterialPreviewUpdate();
                    UpdateMaterialVariationsGrid();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, $"Apply failed:\n{ex.Message}", "Json Editor", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            editor.ExportRequested += (_, editedJson) =>
            {
                try
                {
                    ExportFlatbufferFromJson(kind, entry, editedJson);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, $"Export failed:\n{ex.Message}", "Json Editor", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            editor.ExportReserializeRequested += (_, editedJson) =>
            {
                try
                {
                    ExportFlatbufferFromJsonReserialize(kind, entry, editedJson);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, $"Reserialize export failed:\n{ex.Message}", "Json Editor", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            editor.ShowDialog(this);
        }

        private void ApplyFlatbufferJsonToScene(string kind, JsonEditorEntry entry, string json)
        {
            if (entry.Model == null)
            {
                return;
            }

            if (kind == "TRMTR")
            {
                var trmtr = System.Text.Json.JsonSerializer.Deserialize<Trinity.Core.Flatbuffers.TR.Model.TrmtrFile>(json);
                if (trmtr == null)
                {
                    throw new InvalidOperationException("JSON did not parse into TRMTR.");
                }

                var doc = TrmtrJsonConverter.ToJsonDocument(trmtr);
                TrmtrJsonConverter.ApplyToRuntimeModel(doc, entry.Model, out _, out _);
                return;
            }

            if (kind == "TRMMT")
            {
                var meta = System.Text.Json.JsonSerializer.Deserialize<Trinity.Core.Flatbuffers.TR.Model.TrmmtMetadataFile>(json);
                if (meta == null || meta.ItemList == null || meta.ItemList.Length == 0)
                {
                    throw new NotSupportedException("Apply currently supports TRMMT variation/metadata files only.");
                }

                ApplyTrmmtMetadataJsonToModel(entry.Model, meta);
                return;
            }

            throw new NotSupportedException("Apply is only implemented for TRMTR/TRMMT in Tier 1.");
        }

        private void ApplyTrmmtMetadataJsonToModel(Model model, Trinity.Core.Flatbuffers.TR.Model.TrmmtMetadataFile meta)
        {
            var item = SelectActiveTrmmtMetaItem(model, meta);
            if (item?.ParamList == null || item.ParamList.Length == 0)
            {
                return;
            }

            foreach (var p in item.ParamList)
            {
                if (p == null || !p.UseNoAnime || string.IsNullOrWhiteSpace(p.Name) || p.NoAnimeParam?.MaterialList == null)
                {
                    continue;
                }

                int selected = p.OverrideDefaultValue >= 0 ? p.OverrideDefaultValue : 0;
                model.TrySetMaterialVariantParam(p.Name, selected);

                int variationCount = p.NoAnimeParam.VariationCount;
                int idx = Math.Clamp(selected, 0, Math.Max(0, variationCount - 1));

                foreach (var m in p.NoAnimeParam.MaterialList)
                {
                    if (m == null || string.IsNullOrWhiteSpace(m.MaterialName))
                    {
                        continue;
                    }

                    foreach (var fp in m.FloatParamList ?? Array.Empty<Trinity.Core.Flatbuffers.TR.Model.TrmmtMetaFloatParams>())
                    {
                        if (fp == null || string.IsNullOrWhiteSpace(fp.Name) || fp.Values == null || fp.Values.Length == 0)
                        {
                            continue;
                        }
                        float v = fp.Values[Math.Clamp(idx, 0, fp.Values.Length - 1)];
                        model.TrySetMaterialMetadataValueOverride(p.Name, m.MaterialName, fp.Name, v);
                    }

                    foreach (var ip in m.IntParamList ?? Array.Empty<Trinity.Core.Flatbuffers.TR.Model.TrmmtMetaIntParams>())
                    {
                        if (ip == null || string.IsNullOrWhiteSpace(ip.Name) || ip.Values == null || ip.Values.Length == 0)
                        {
                            continue;
                        }
                        int v = ip.Values[Math.Clamp(idx, 0, ip.Values.Length - 1)];
                        model.TrySetMaterialMetadataValueOverride(p.Name, m.MaterialName, ip.Name, v);
                    }

                    foreach (var v3p in m.Float3ParamList ?? Array.Empty<Trinity.Core.Flatbuffers.TR.Model.TrmmtMetaFloat3Params>())
                    {
                        if (v3p == null || string.IsNullOrWhiteSpace(v3p.Name) || v3p.Values == null || v3p.Values.Length == 0)
                        {
                            continue;
                        }
                        var v3 = v3p.Values[Math.Clamp(idx, 0, v3p.Values.Length - 1)];
                        model.TrySetMaterialMetadataValueOverride(p.Name, m.MaterialName, v3p.Name, new Vector3(v3.X, v3.Y, v3.Z));
                    }

                    foreach (var v4p in m.Float4ParamList ?? Array.Empty<Trinity.Core.Flatbuffers.TR.Model.TrmmtMetaFloat4Params>())
                    {
                        if (v4p == null || string.IsNullOrWhiteSpace(v4p.Name) || v4p.Values == null || v4p.Values.Length == 0)
                        {
                            continue;
                        }
                        var v4 = v4p.Values[Math.Clamp(idx, 0, v4p.Values.Length - 1)];
                        model.TrySetMaterialMetadataValueOverride(p.Name, m.MaterialName, v4p.Name, new Vector4(v4.W, v4.X, v4.Y, v4.Z));
                    }
                }
            }
        }

        private static Trinity.Core.Flatbuffers.TR.Model.TrmmtMetaItem? SelectActiveTrmmtMetaItem(Model model, Trinity.Core.Flatbuffers.TR.Model.TrmmtMetadataFile meta)
        {
            if (meta.ItemList == null || meta.ItemList.Length == 0)
            {
                return null;
            }

            if (!string.IsNullOrWhiteSpace(model.CurrentMaterialSetName))
            {
                var byName = meta.ItemList.FirstOrDefault(i => string.Equals(i?.Name, model.CurrentMaterialSetName, StringComparison.OrdinalIgnoreCase));
                if (byName != null)
                {
                    return byName;
                }
            }

            if (!string.IsNullOrWhiteSpace(model.CurrentMaterialFilePath))
            {
                var fileName = Path.GetFileName(model.CurrentMaterialFilePath);
                foreach (var item in meta.ItemList)
                {
                    if (item?.MaterialPathList == null)
                    {
                        continue;
                    }

                    if (item.MaterialPathList.Any(p => string.Equals(Path.GetFileName(p), fileName, StringComparison.OrdinalIgnoreCase)))
                    {
                        return item;
                    }
                }
            }

            return meta.ItemList.Length == 1 ? meta.ItemList[0] : meta.ItemList[0];
        }

        private void ExportFlatbufferFromJson(string kind, JsonEditorEntry entry, string json)
        {
            if (kind != "TRMTR" && kind != "TRMMT")
            {
                MessageBox.Show(this, "Tier 1 export is implemented for TRMTR/TRMMT only.", "Json Editor", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (entry.Model == null)
            {
                return;
            }

            ApplyFlatbufferJsonToScene(kind, entry, json);

            using var sfd = new SaveFileDialog();
            sfd.Title = $"Export {kind} from JSON";
            sfd.Filter = kind == "TRMTR" ? "TRMTR (*.trmtr)|*.trmtr" : "TRMMT (*.trmmt)|*.trmmt";
            sfd.FileName = Path.GetFileName(entry.Path);
            if (sfd.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            if (kind == "TRMTR")
            {
                TrinityModelViewer.Export.TrmtrBinaryPatcher.ExportEditedTrmtrPreserveAllFields(entry.Path, entry.Model, sfd.FileName);
            }
            else
            {
                TrinityModelViewer.Export.TrmmtBinaryPatcher.ExportEditedTrmmtPreserveAllFields(entry.Path, entry.Model, sfd.FileName);
            }

            MessageBox.Show(this, $"Exported:\n{sfd.FileName}", "Json Editor", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ExportFlatbufferFromJsonReserialize(string kind, JsonEditorEntry entry, string json)
        {
            if (kind != "TRMTR" && kind != "TRMMT")
            {
                MessageBox.Show(this, "Reserialize export is implemented for TRMTR/TRMMT only.", "Json Editor", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var sfd = new SaveFileDialog();
            sfd.Title = $"Reserialize {kind} from JSON";
            sfd.Filter = kind == "TRMTR" ? "TRMTR (*.trmtr)|*.trmtr" : "TRMMT (*.trmmt)|*.trmmt";
            sfd.FileName = Path.GetFileName(entry.Path);
            if (sfd.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            byte[] bytes;
            if (kind == "TRMTR")
            {
                var trmtr = System.Text.Json.JsonSerializer.Deserialize<Trinity.Core.Flatbuffers.TR.Model.TrmtrFile>(json);
                if (trmtr == null)
                {
                    throw new InvalidOperationException("JSON did not parse into TRMTR.");
                }
                bytes = Trinity.Core.Utils.FlatBufferConverter.SerializeFrom(trmtr);
            }
            else
            {
                // TRMMT has two flavors; prefer metadata if it looks like a metadata file.
                Trinity.Core.Flatbuffers.TR.Model.TrmmtMetadataFile? meta = null;
                try
                {
                    meta = System.Text.Json.JsonSerializer.Deserialize<Trinity.Core.Flatbuffers.TR.Model.TrmmtMetadataFile>(json);
                }
                catch
                {
                    meta = null;
                }

                if (meta?.ItemList != null && meta.ItemList.Length > 0)
                {
                    bytes = Trinity.Core.Utils.FlatBufferConverter.SerializeFrom(meta);
                }
                else
                {
                    var setMap = System.Text.Json.JsonSerializer.Deserialize<Trinity.Core.Flatbuffers.TR.Model.TrmmtFile>(json);
                    if (setMap == null)
                    {
                        throw new InvalidOperationException("JSON did not parse into TRMMT.");
                    }
                    bytes = Trinity.Core.Utils.FlatBufferConverter.SerializeFrom(setMap);
                }
            }

            Directory.CreateDirectory(Path.GetDirectoryName(sfd.FileName) ?? ".");
            File.WriteAllBytes(sfd.FileName, bytes);
            MessageBox.Show(this, $"Exported:\n{sfd.FileName}", "Json Editor", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void SyncJsonEditorToCurrentMaterialSelection()
        {
            if (jsonFilesGrid == null || jsonEditorTabPage == null)
            {
                return;
            }

            if (leftTabs.SelectedTab != jsonEditorTabPage)
            {
                return;
            }

            RefreshJsonEditorFileList();
        }
    }
}
