using GFTool.Renderer.Core;
using GFTool.Renderer.Scene.GraphicsObjects;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TrinityModelViewer
{
    public partial class ModelViewerForm
    {
        private TabPage? materialVariationsTab;
        private Panel? materialVariationsPanel;
        private DataGridView? materialVariationsGrid;
        private bool isUpdatingMaterialVariationsGrid;
        private Panel? materialSetPanel;
        private Label? materialSetLabel;
        private ComboBox? materialSetComboBox;
        private bool isUpdatingMaterialSetUi;

        private TableLayoutPanel? materialVariantTable;
        private bool isUpdatingMaterialVariantUi;
        private readonly Dictionary<string, Control> materialVariantSelectors = new Dictionary<string, Control>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Label> materialVariantCountLabels = new Dictionary<string, Label>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, int> materialVariantVariationCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        private void EnsureMaterialVariationsTab()
        {
            if (materialVariationsTab != null)
            {
                if (!materialTabs.TabPages.Contains(materialVariationsTab))
                {
                    int insertIndexExisting = materialTabs.TabPages.IndexOf(materialSamplersTab);
                    if (insertIndexExisting < 0)
                    {
                        materialTabs.TabPages.Add(materialVariationsTab);
                    }
                    else
                    {
                        materialTabs.TabPages.Insert(insertIndexExisting + 1, materialVariationsTab);
                    }
                }

                return;
            }

            materialVariationsPanel = new Panel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };

            materialVariationsTab = new TabPage
            {
                Text = "Variations",
                Padding = new Padding(3),
                UseVisualStyleBackColor = false
            };

            materialVariationsGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
                ReadOnly = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false
            };
            materialVariationsGrid.CurrentCellDirtyStateChanged += materialVariationsGrid_CurrentCellDirtyStateChanged;
            materialVariationsGrid.CellValueChanged += materialVariationsGrid_CellValueChanged;

            materialVariationsTab.Controls.Add(materialVariationsGrid);
            materialVariationsTab.Controls.Add(materialVariationsPanel);

            // Ensure the new tab is discoverable even when the tab strip is narrow.
            materialTabs.Multiline = true;

            int insertIndex = materialTabs.TabPages.IndexOf(materialSamplersTab);
            if (insertIndex < 0)
            {
                materialTabs.TabPages.Add(materialVariationsTab);
            }
            else
            {
                materialTabs.TabPages.Insert(insertIndex + 1, materialVariationsTab);
            }

            if (MessageHandler.Instance.DebugLogsEnabled)
            {
                MessageHandler.Instance.AddMessage(
                    MessageType.LOG,
                    $"[UI] Added Variations tab. tabs={string.Join(", ", materialTabs.TabPages.Cast<TabPage>().Select(t => t.Text))}");
            }

            ApplyTheme(materialVariationsTab);
        }

        private void EnsureMaterialSetUi()
        {
            if (materialSetPanel != null)
            {
                return;
            }

            EnsureMaterialVariationsTab();

            materialSetPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 26,
                Visible = false
            };

            materialSetLabel = new Label
            {
                AutoSize = true,
                Text = "Material set:",
                Left = 4,
                Top = 6
            };

            materialSetComboBox = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Left = 82,
                Top = 2,
                Width = 240,
                Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right
            };
            materialSetComboBox.SelectedIndexChanged += materialSetComboBox_SelectedIndexChanged;

            materialSetPanel.Controls.Add(materialSetLabel);
            materialSetPanel.Controls.Add(materialSetComboBox);

            EnsureMaterialVariantUi();

            materialVariationsPanel!.Controls.Add(materialVariantTable!);
            materialVariationsPanel!.Controls.Add(materialSetPanel);

            ApplyTheme(materialVariationsTab);

            EnsureMaterialVariationsGrid();
            UpdateMaterialVariationsGrid();
        }

        private void EnsureMaterialVariantUi()
        {
            if (materialVariantTable != null)
            {
                return;
            }

            materialVariantTable = new TableLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Dock = DockStyle.Top,
                Visible = false,
                Padding = new Padding(4, 2, 4, 2),
                ColumnCount = 3
            };
            materialVariantTable.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            materialVariantTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            materialVariantTable.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        }

        private void EnsureMaterialVariationsGrid()
        {
            if (materialVariationsGrid == null)
            {
                return;
            }

            if (materialVariationsGrid.Columns.Count > 0)
            {
                return;
            }

            materialVariationsGrid.Columns.Clear();
            materialVariationsGrid.Columns.Add("ParamName", "Name");
            materialVariationsGrid.Columns.Add("ParamType", "Type");
            materialVariationsGrid.Columns.Add("ParamValue", "Value");
            materialVariationsGrid.Columns["ParamName"].ReadOnly = true;
            materialVariationsGrid.Columns["ParamType"].ReadOnly = true;
            materialVariationsGrid.Columns["ParamValue"].ReadOnly = false;

            materialVariationsGrid.CellPainting += materialVariationsGrid_CellPainting;
            materialVariationsGrid.CellMouseClick += materialVariationsGrid_CellMouseClick;
        }

        private void UpdateMaterialSetUi()
        {
            EnsureMaterialSetUi();

            if (materialVariationsTab == null || materialSetPanel == null || materialSetComboBox == null)
            {
                return;
            }

            var mdl = currentMaterialsModel;
            if (mdl == null)
            {
                materialSetPanel.Visible = false;
                if (materialVariantTable != null)
                {
                    materialVariantTable.Visible = false;
                }
                return;
            }

            var sets = mdl.GetMaterialSetNames();
            if (MessageHandler.Instance.DebugLogsEnabled)
            {
                MessageHandler.Instance.AddMessage(MessageType.LOG, $"[UI] Material sets={sets.Count} current='{mdl.CurrentMaterialSetName ?? "<none>"}'");
            }

            if (sets.Count > 1)
            {
                isUpdatingMaterialSetUi = true;
                try
                {
                    materialSetComboBox.BeginUpdate();
                    bool sameItems = materialSetComboBox.Items.Count == sets.Count;
                    if (sameItems)
                    {
                        for (int i = 0; i < sets.Count; i++)
                        {
                            if (materialSetComboBox.Items[i] is not string s ||
                                !string.Equals(s, sets[i], StringComparison.OrdinalIgnoreCase))
                            {
                                sameItems = false;
                                break;
                            }
                        }
                    }

                    if (!sameItems)
                    {
                        materialSetComboBox.Items.Clear();
                        foreach (var name in sets)
                        {
                            materialSetComboBox.Items.Add(name);
                        }
                    }

                    var current = mdl.CurrentMaterialSetName;
                    if (!string.IsNullOrWhiteSpace(current))
                    {
                        materialSetComboBox.SelectedItem = sets.FirstOrDefault(s => string.Equals(s, current, StringComparison.OrdinalIgnoreCase)) ?? sets[0];
                    }
                    else
                    {
                        materialSetComboBox.SelectedIndex = 0;
                    }
                }
                finally
                {
                    materialSetComboBox.EndUpdate();
                    isUpdatingMaterialSetUi = false;
                }

                materialSetPanel.Visible = true;
            }
            else
            {
                materialSetPanel.Visible = false;
            }

            UpdateMaterialVariantUi();

            if (MessageHandler.Instance.DebugLogsEnabled)
            {
                bool hasAny = materialSetPanel.Visible || (materialVariantTable?.Visible ?? false);
                MessageHandler.Instance.AddMessage(
                    MessageType.LOG,
                    $"[UI] VariationsTab hasAny={hasAny} tabs={string.Join(", ", materialTabs.TabPages.Cast<TabPage>().Select(t => t.Text))}");
            }

            UpdateMaterialVariationsGrid();
        }

        private void UpdateMaterialVariantUi()
        {
            EnsureMaterialVariantUi();

            if (materialVariantTable == null)
            {
                return;
            }

            var mdl = currentMaterialsModel;
            if (mdl == null)
            {
                materialVariantTable.Visible = false;
                return;
            }

            var variants = mdl.GetMaterialVariantParams();
            if (MessageHandler.Instance.DebugLogsEnabled)
            {
                MessageHandler.Instance.AddMessage(MessageType.LOG, $"[UI] Material variants={variants.Count}");
            }
            if (variants.Count == 0)
            {
                materialVariantTable.Visible = false;
                materialVariantTable.Controls.Clear();
                materialVariantSelectors.Clear();
                materialVariantCountLabels.Clear();
                materialVariantVariationCounts.Clear();
                return;
            }

            if (TryUpdateMaterialVariantSelectors(variants))
            {
                materialVariantTable.Visible = true;
                return;
            }

            isUpdatingMaterialVariantUi = true;
            try
            {
                using var redrawScope = new RedrawScope((Control?)materialVariationsPanel ?? materialTabs);
                materialVariantTable.SuspendLayout();
                materialVariantTable.Controls.Clear();
                materialVariantTable.RowStyles.Clear();
                materialVariantTable.RowCount = 0;
                materialVariantSelectors.Clear();
                materialVariantCountLabels.Clear();
                materialVariantVariationCounts.Clear();

                foreach (var v in variants)
                {
                    int rowIndex = materialVariantTable.RowCount++;
                    materialVariantTable.RowStyles.Add(new RowStyle(SizeType.AutoSize));

                    var label = new Label
                    {
                        AutoSize = true,
                        Text = $"{v.Name}:",
                        Anchor = AnchorStyles.Left,
                        Margin = new Padding(0, 4, 6, 0)
                    };

                    Control selector;
                    if (v.VariationCount <= 256)
                    {
                        var combo = new ComboBox
                        {
                            DropDownStyle = ComboBoxStyle.DropDownList,
                            FlatStyle = FlatStyle.Popup,
                            Anchor = AnchorStyles.Left | AnchorStyles.Right,
                            Tag = v.Name,
                            Margin = new Padding(0, 0, 6, 0)
                        };
                        combo.SelectedIndexChanged += materialVariantComboBox_SelectedIndexChanged;

                        for (int i = 0; i < v.VariationCount; i++)
                        {
                            combo.Items.Add(i.ToString(CultureInfo.InvariantCulture));
                        }

                        combo.SelectedIndex = Math.Clamp(v.SelectedIndex, 0, Math.Max(0, v.VariationCount - 1));
                        ApplyTheme(combo);
                        selector = combo;
                    }
                    else
                    {
                        var numeric = new NumericUpDown
                        {
                            Minimum = 0,
                            Maximum = Math.Max(0, v.VariationCount - 1),
                            Value = Math.Clamp(v.SelectedIndex, 0, Math.Max(0, v.VariationCount - 1)),
                            Anchor = AnchorStyles.Left | AnchorStyles.Right,
                            Tag = v.Name,
                            Margin = new Padding(0, 0, 6, 0)
                        };
                        numeric.ValueChanged += materialVariantNumericUpDown_ValueChanged;
                        ApplyTheme(numeric);
                        selector = numeric;
                    }

                    var countLabel = new Label
                    {
                        AutoSize = true,
                        Text = $"/ {Math.Max(0, v.VariationCount - 1)}",
                        Anchor = AnchorStyles.Left,
                        Margin = new Padding(0, 4, 0, 0)
                    };

                    materialVariantTable.Controls.Add(label, 0, rowIndex);
                    materialVariantTable.Controls.Add(selector, 1, rowIndex);
                    materialVariantTable.Controls.Add(countLabel, 2, rowIndex);

                    materialVariantSelectors[v.Name] = selector;
                    materialVariantCountLabels[v.Name] = countLabel;
                    materialVariantVariationCounts[v.Name] = v.VariationCount;
                }
            }
            finally
            {
                materialVariantTable.ResumeLayout();
                isUpdatingMaterialVariantUi = false;
            }

            materialVariantTable.Visible = true;
            ApplyTheme(materialVariantTable);
        }

        private void UpdateMaterialVariationsGrid()
        {
            if (materialVariationsGrid == null || currentMaterial == null)
            {
                return;
            }

            EnsureMaterialVariationsGrid();

            var mdl = currentMaterialsModel;
            if (mdl == null)
            {
                materialVariationsGrid.Rows.Clear();
                return;
            }

            var entries = mdl.GetMaterialMetadataOverrideEntriesForMaterial(currentMaterial.Name);
            var merged = entries.ToList();
            var existingNames = new HashSet<string>(entries.Select(e => e.Name), StringComparer.OrdinalIgnoreCase);

            foreach (var v4 in currentMaterial.Vec4Parameters)
            {
                if (v4 == null || string.IsNullOrWhiteSpace(v4.Name))
                {
                    continue;
                }

                if (!materialsEditorService.IsColorVec4Param(v4.Name, "Vec4"))
                {
                    continue;
                }

                if (!existingNames.Add(v4.Name))
                {
                    continue;
                }

                object value;
                if (currentMaterial.TryGetUniformOverride(v4.Name, out var overrideValue))
                {
                    value = overrideValue;
                }
                else
                {
                    var vv = v4.Value;
                    value = new Vector4(vv.W, vv.X, vv.Y, vv.Z);
                }

                merged.Add(new Model.MaterialMetadataOverrideEntry
                {
                    Name = v4.Name,
                    Type = "Vec4",
                    Value = value
                });
            }

            // Keep BaseColorLayer*/ShadowingColorLayer* surfaced even when there are no metadata-driven overrides.
            merged.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));

            isUpdatingMaterialVariationsGrid = true;
            try
            {
                materialVariationsGrid.SuspendLayout();
                materialVariationsGrid.Rows.Clear();
                foreach (var e in merged)
                {
                    object value = e.Value;
                    if (currentMaterial.TryGetUniformOverride(e.Name, out var overrideValue))
                    {
                        value = overrideValue;
                    }

                    string display = value switch
                    {
                        Vector2 v2 => $"{v2.X.ToString(CultureInfo.InvariantCulture)}, {v2.Y.ToString(CultureInfo.InvariantCulture)}",
                        Vector3 v3 => $"{v3.X.ToString(CultureInfo.InvariantCulture)}, {v3.Y.ToString(CultureInfo.InvariantCulture)}, {v3.Z.ToString(CultureInfo.InvariantCulture)}",
                        Vector4 v4 => $"{v4.X.ToString(CultureInfo.InvariantCulture)}, {v4.Y.ToString(CultureInfo.InvariantCulture)}, {v4.Z.ToString(CultureInfo.InvariantCulture)}, {v4.W.ToString(CultureInfo.InvariantCulture)}",
                        _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty
                    };

                    int row = materialVariationsGrid.Rows.Add(e.Name, e.Type, display);
                    materialVariationsGrid.Rows[row].Cells["ParamValue"].ReadOnly = false;
                    materialVariationsGrid.Rows[row].Tag = e;
                }
            }
            finally
            {
                materialVariationsGrid.ResumeLayout();
                isUpdatingMaterialVariationsGrid = false;
            }
        }

    }
}
