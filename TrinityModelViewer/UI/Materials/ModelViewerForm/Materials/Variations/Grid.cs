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
        private void materialVariationsGrid_CurrentCellDirtyStateChanged(object? sender, EventArgs e)
        {
            if (materialVariationsGrid == null)
            {
                return;
            }

            if (materialVariationsGrid.IsCurrentCellDirty)
            {
                materialVariationsGrid.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        }

        private void materialVariationsGrid_CellValueChanged(object? sender, DataGridViewCellEventArgs e)
        {
            if (materialVariationsGrid == null || isUpdatingMaterialVariationsGrid || currentMaterial == null)
            {
                return;
            }

            if (e.RowIndex < 0 || e.ColumnIndex < 0)
            {
                return;
            }

            var col = materialVariationsGrid.Columns[e.ColumnIndex];
            if (!string.Equals(col.Name, "ParamValue", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var row = materialVariationsGrid.Rows[e.RowIndex];
            var name = row.Cells["ParamName"].Value?.ToString() ?? string.Empty;
            var type = row.Cells["ParamType"].Value?.ToString() ?? string.Empty;
            var rawValue = row.Cells["ParamValue"].Value?.ToString() ?? string.Empty;
            var mdl = currentMaterialsModel;
            bool isMetadataDriven = mdl != null &&
                                    row.Tag is Model.MaterialMetadataOverrideEntry metaEntry &&
                                    !string.IsNullOrWhiteSpace(metaEntry.MetadataParamName) &&
                                    !string.IsNullOrWhiteSpace(metaEntry.MetadataMaterialName);

            if (string.IsNullOrWhiteSpace(name))
            {
                return;
            }

            row.ErrorText = string.Empty;
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                if (isMetadataDriven)
                {
                    var meta = (Model.MaterialMetadataOverrideEntry)row.Tag!;
                    mdl!.ClearMaterialMetadataValueOverride(meta.MetadataParamName!, meta.MetadataMaterialName ?? currentMaterial.Name, name);
                    UpdateMaterialVariationsGrid();
                    RequestMaterialPreviewUpdate();
                    return;
                }

                currentMaterial.ClearUniformOverride(name);
                currentMaterialsModel?.TryClearMaterialUniformOverride(currentMaterial.Name, name);
                if (name.StartsWith("BaseColorIndex", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(name, "ColorTableDivideNumber", StringComparison.OrdinalIgnoreCase))
                {
                    currentMaterial.RefreshColorTableOverridesFromUniformOverrides();
                    UpdateMaterialVariationsGrid();
                }
                return;
            }

            try
            {
                if (!materialsEditorService.TryParseUniformValueForVariationsGrid(type, rawValue, out var parsedValue, out var errorText))
                {
                    row.ErrorText = errorText ?? "Invalid value.";
                    return;
                }

                if (isMetadataDriven && parsedValue != null)
                {
                    var meta = (Model.MaterialMetadataOverrideEntry)row.Tag!;
                    mdl!.TrySetMaterialMetadataValueOverride(meta.MetadataParamName!, meta.MetadataMaterialName ?? currentMaterial.Name, name, parsedValue);
                    UpdateMaterialVariationsGrid();
                    RequestMaterialPreviewUpdate();
                    return;
                }

                if (parsedValue != null)
                {
                    currentMaterial.SetUniformOverride(name, parsedValue);
                    currentMaterialsModel?.TrySetMaterialUniformOverride(currentMaterial.Name, name, parsedValue);
                }

                if (name.StartsWith("BaseColorIndex", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(name, "ColorTableDivideNumber", StringComparison.OrdinalIgnoreCase))
                {
                    currentMaterial.RefreshColorTableOverridesFromUniformOverrides();
                    UpdateMaterialVariationsGrid();
                }
            }
            catch (Exception ex)
            {
                row.ErrorText = ex.Message;
            }
        }

        private void materialVariationsGrid_CellMouseClick(object? sender, DataGridViewCellMouseEventArgs e)
        {
            if (materialVariationsGrid == null || currentMaterial == null || isUpdatingMaterialVariationsGrid)
            {
                return;
            }

            if (e.RowIndex < 0 || e.ColumnIndex < 0)
            {
                return;
            }

            var col = materialVariationsGrid.Columns[e.ColumnIndex];
            if (!string.Equals(col.Name, "ParamValue", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var row = materialVariationsGrid.Rows[e.RowIndex];
            var name = row.Cells["ParamName"].Value?.ToString() ?? string.Empty;
            var type = row.Cells["ParamType"].Value?.ToString() ?? string.Empty;

            const int swatchWidth = 22;
            bool clickedSwatch = e.X <= swatchWidth;

            if (TryGetBaseColorIndexSuffix(name, out int suffix))
            {
                var tex = currentMaterial.Textures.FirstOrDefault(t => string.Equals(t.Name, "ColorTableMap", StringComparison.OrdinalIgnoreCase));
                if (tex == null)
                {
                    return;
                }

                using var bmp = tex.LoadPreviewBitmap();
                if (bmp == null)
                {
                    return;
                }

                if (!currentMaterial.TryGetShaderParamIntEffective("ColorTableDivideNumber", out int divide) || divide <= 0)
                {
                    divide = Math.Max(1, bmp.Width / 2);
                }

                int cols = Math.Max(1, Math.Min(bmp.Width / 2, divide));
                int initial = 1;
                if (row.Cells["ParamValue"].Value is string s && int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out int idx))
                {
                    initial = Math.Clamp(idx, 1, cols);
                }

                using var dialog = new ColorTablePickerForm((Bitmap)bmp.Clone(), cols, initial);
                ApplyTheme(dialog);
                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                int selectedIndex = dialog.SelectedIndex1Based;
                var mdl = currentMaterialsModel;
                if (mdl != null &&
                    row.Tag is Model.MaterialMetadataOverrideEntry metaEntry &&
                    !string.IsNullOrWhiteSpace(metaEntry.MetadataParamName) &&
                    !string.IsNullOrWhiteSpace(metaEntry.MetadataMaterialName))
                {
                    mdl.TrySetMaterialMetadataValueOverride(metaEntry.MetadataParamName!, metaEntry.MetadataMaterialName ?? currentMaterial.Name, name, selectedIndex);
                }
                else
                {
                    currentMaterial.SetUniformOverride(name, selectedIndex);
                    currentMaterialsModel?.TrySetMaterialUniformOverride(currentMaterial.Name, name, selectedIndex);
                    currentMaterial.RefreshColorTableOverridesFromUniformOverrides();
                }
                UpdateMaterialVariationsGrid();
                RequestMaterialPreviewUpdate();
                return;
            }

            if (!clickedSwatch || !materialsEditorService.IsColorVec4Param(name, type))
            {
                return;
            }

            string currentText = row.Cells["ParamValue"].Value?.ToString() ?? string.Empty;
            if (!materialsEditorService.TryParseVec4(currentText, out var v))
            {
                v = new Vector4(1f, 1f, 1f, 1f);
            }

            using var colorDialog = new ColorDialog
            {
                FullOpen = true,
                AnyColor = true,
                Color = Color.FromArgb(
                    materialsEditorService.ClampToByte(v.W),
                    materialsEditorService.ClampToByte(v.X),
                    materialsEditorService.ClampToByte(v.Y),
                    materialsEditorService.ClampToByte(v.Z))
            };
            if (colorDialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            var c = colorDialog.Color;
            float a = v.W;
            string newText =
                $"{(c.R / 255f).ToString("0.####", CultureInfo.InvariantCulture)}, {(c.G / 255f).ToString("0.####", CultureInfo.InvariantCulture)}, {(c.B / 255f).ToString("0.####", CultureInfo.InvariantCulture)}, {a.ToString("0.####", CultureInfo.InvariantCulture)}";

            isUpdatingMaterialVariationsGrid = true;
            try
            {
                row.Cells["ParamValue"].Value = newText;
            }
            finally
            {
                isUpdatingMaterialVariationsGrid = false;
            }

            if (materialsEditorService.TryParseVec4(newText, out var parsed))
            {
                currentMaterial.SetUniformOverride(name, parsed);
                currentMaterialsModel?.TrySetMaterialUniformOverride(currentMaterial.Name, name, parsed);
                UpdateMaterialVariationsGrid();
                RequestMaterialPreviewUpdate();
            }
        }

        private void materialVariationsGrid_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (materialVariationsGrid == null || currentMaterial == null)
            {
                return;
            }

            if (e.RowIndex < 0 || e.ColumnIndex < 0)
            {
                return;
            }

            var col = materialVariationsGrid.Columns[e.ColumnIndex];
            if (!string.Equals(col.Name, "ParamValue", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var row = materialVariationsGrid.Rows[e.RowIndex];
            var name = row.Cells["ParamName"].Value?.ToString() ?? string.Empty;

            if (TryGetBaseColorIndexSuffix(name, out int suffix))
            {
                string layerName = $"BaseColorLayer{suffix}";
                Color swatchColor;
                if (currentMaterial.TryGetUniformOverride(layerName, out var overrideValue) && overrideValue is Vector4 ov)
                {
                    swatchColor = Color.FromArgb(
                        materialsEditorService.ClampToByte(ov.W),
                        materialsEditorService.ClampToByte(ov.X),
                        materialsEditorService.ClampToByte(ov.Y),
                        materialsEditorService.ClampToByte(ov.Z));
                }
                else
                {
                    var vec4 = currentMaterial.Vec4Parameters.FirstOrDefault(v => string.Equals(v.Name, layerName, StringComparison.OrdinalIgnoreCase));
                    if (vec4 == null)
                    {
                        return;
                    }

                    var v = vec4.Value;
                    var linear = new Vector3(v.W, v.X, v.Y);
                    swatchColor = ColorFromLinearRgb(linear);
                }

                e.PaintBackground(e.CellBounds, true);

                var bounds = e.CellBounds;
                int pad = 4;
                int size = Math.Min(bounds.Height - pad * 2, 14);
                var swatchRect = new Rectangle(bounds.Left + pad, bounds.Top + (bounds.Height - size) / 2, size, size);
                using (var b = new SolidBrush(swatchColor))
                {
                    e.Graphics.FillRectangle(b, swatchRect);
                }
                using (var p = new Pen(Color.FromArgb(120, 0, 0, 0)))
                {
                    e.Graphics.DrawRectangle(p, swatchRect);
                }

                var textRect = new Rectangle(swatchRect.Right + 6, bounds.Top, bounds.Width - (swatchRect.Width + 12), bounds.Height);
                TextRenderer.DrawText(
                    e.Graphics,
                    Convert.ToString(e.FormattedValue, CultureInfo.InvariantCulture) ?? string.Empty,
                    e.CellStyle.Font,
                    textRect,
                    e.CellStyle.ForeColor,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.Left);

                e.Handled = true;
                return;
            }

            string type = row.Cells["ParamType"].Value?.ToString() ?? string.Empty;
            if (!materialsEditorService.IsColorVec4Param(name, type))
            {
                return;
            }

            string text = e.FormattedValue?.ToString() ?? string.Empty;
            if (!materialsEditorService.TryParseVec4(text, out var vv))
            {
                return;
            }

            const int width = 22;
            const int pad2 = 3;

            e.PaintBackground(e.CellBounds, e.State.HasFlag(DataGridViewElementStates.Selected));

            var swatchRect2 = new Rectangle(
                e.CellBounds.Left + pad2,
                e.CellBounds.Top + pad2,
                width - pad2 * 2,
                e.CellBounds.Height - pad2 * 2);

            using (var brush = new SolidBrush(Color.FromArgb(
                       materialsEditorService.ClampToByte(vv.W),
                       materialsEditorService.ClampToByte(vv.X),
                       materialsEditorService.ClampToByte(vv.Y),
                       materialsEditorService.ClampToByte(vv.Z))))
            {
                e.Graphics.FillRectangle(brush, swatchRect2);
            }
            e.Graphics.DrawRectangle(Pens.Black, swatchRect2);

            var textRect2 = new Rectangle(
                e.CellBounds.Left + width,
                e.CellBounds.Top,
                e.CellBounds.Width - width,
                e.CellBounds.Height);

            TextRenderer.DrawText(
                e.Graphics,
                text,
                e.CellStyle.Font,
                textRect2,
                e.CellStyle.ForeColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

            e.Handled = true;
        }

        private static bool TryGetBaseColorIndexSuffix(string name, out int suffix)
        {
            suffix = 0;
            if (string.IsNullOrWhiteSpace(name) || !name.StartsWith("BaseColorIndex", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var tail = name.Substring("BaseColorIndex".Length);
            return int.TryParse(tail, NumberStyles.Integer, CultureInfo.InvariantCulture, out suffix) && suffix >= 1 && suffix <= 4;
        }

        private static Color ColorFromLinearRgb(Vector3 linear)
        {
            static byte ToSrgbByte(float c)
            {
                c = Math.Clamp(c, 0f, 1f);
                float srgb = (c <= 0.0031308f)
                    ? (c * 12.92f)
                    : (1.055f * MathF.Pow(c, 1f / 2.4f) - 0.055f);
                return (byte)Math.Clamp((int)MathF.Round(srgb * 255f), 0, 255);
            }

            return Color.FromArgb(255, ToSrgbByte(linear.X), ToSrgbByte(linear.Y), ToSrgbByte(linear.Z));
        }

        private bool TryUpdateMaterialVariantSelectors(IReadOnlyList<Model.MaterialVariantParam> variants)
        {
            if (materialVariantTable == null || materialVariantSelectors.Count == 0)
            {
                return false;
            }

            if (materialVariantSelectors.Count != variants.Count)
            {
                return false;
            }

            foreach (var v in variants)
            {
                if (!materialVariantSelectors.TryGetValue(v.Name, out var selector))
                {
                    return false;
                }

                if (materialVariantVariationCounts.TryGetValue(v.Name, out var existingCount) && existingCount != v.VariationCount)
                {
                    return false;
                }

                bool shouldBeCombo = v.VariationCount <= 256;
                if (shouldBeCombo && selector is not ComboBox)
                {
                    return false;
                }
                if (!shouldBeCombo && selector is not NumericUpDown)
                {
                    return false;
                }
            }

            isUpdatingMaterialVariantUi = true;
            try
            {
                using var redrawScope = new RedrawScope((Control?)materialVariationsPanel ?? materialTabs);
                foreach (var v in variants)
                {
                    if (!materialVariantSelectors.TryGetValue(v.Name, out var selector))
                    {
                        continue;
                    }

                    int maxIndex = Math.Max(0, v.VariationCount - 1);
                    int selected = Math.Clamp(v.SelectedIndex, 0, maxIndex);

                    if (selector is ComboBox combo)
                    {
                        if (combo.Items.Count != v.VariationCount)
                        {
                            return false;
                        }

                        if (combo.SelectedIndex != selected)
                        {
                            combo.SelectedIndex = selected;
                        }
                    }
                    else if (selector is NumericUpDown numeric)
                    {
                        if (numeric.Maximum != maxIndex)
                        {
                            numeric.Maximum = maxIndex;
                        }
                        if ((int)numeric.Value != selected)
                        {
                            numeric.Value = selected;
                        }
                    }

                    if (materialVariantCountLabels.TryGetValue(v.Name, out var countLabel))
                    {
                        countLabel.Text = $"/ {maxIndex}";
                    }
                }
            }
            finally
            {
                isUpdatingMaterialVariantUi = false;
            }

            return true;
        }

        private void materialVariantNumericUpDown_ValueChanged(object? sender, EventArgs e)
        {
            if (isUpdatingMaterialVariantUi)
            {
                return;
            }

            if (sender is not NumericUpDown numeric)
            {
                return;
            }

            if (numeric.Tag is not string name || string.IsNullOrWhiteSpace(name))
            {
                return;
            }

            int index = (int)numeric.Value;

            var mdl = currentMaterialsModel;
            if (mdl == null)
            {
                return;
            }

            if (!mdl.TrySetMaterialVariantParam(name, index))
            {
                return;
            }

            if (currentMaterial != null)
            {
                PopulateMaterialDetails(currentMaterial);
            }
            else
            {
                RequestMaterialPreviewUpdate();
            }
        }

        private void materialVariantComboBox_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (isUpdatingMaterialVariantUi)
            {
                return;
            }

            if (sender is not ComboBox combo)
            {
                return;
            }

            if (combo.Tag is not string name || string.IsNullOrWhiteSpace(name))
            {
                return;
            }

            if (combo.SelectedItem is not string selectedText ||
                !int.TryParse(selectedText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var index))
            {
                return;
            }

            var mdl = currentMaterialsModel;
            if (mdl == null)
            {
                return;
            }

            if (!mdl.TrySetMaterialVariantParam(name, index))
            {
                return;
            }

            if (currentMaterial != null)
            {
                PopulateMaterialDetails(currentMaterial);
            }
            else
            {
                RequestMaterialPreviewUpdate();
            }
        }

        private void materialSetComboBox_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (isUpdatingMaterialSetUi)
            {
                return;
            }

            if (materialSetComboBox?.SelectedItem is not string setName)
            {
                return;
            }

            var mdl = currentMaterialsModel;
            if (mdl == null)
            {
                return;
            }

            var priorSetName = mdl.CurrentMaterialSetName;
            var priorMaterialName = currentMaterial?.Name;

            using var redrawScope = new RedrawScope(materialTabs);
            using var redrawScope2 = new RedrawScope(materialList);

            if (!mdl.TrySetMaterialSet(setName))
            {
                if (!string.IsNullOrWhiteSpace(priorSetName) &&
                    !string.Equals(priorSetName, setName, StringComparison.OrdinalIgnoreCase))
                {
                    isUpdatingMaterialSetUi = true;
                    try
                    {
                        materialSetComboBox.SelectedItem = priorSetName;
                    }
                    finally
                    {
                        isUpdatingMaterialSetUi = false;
                    }
                }
                return;
            }

            PopulateMaterials(mdl);

            if (!string.IsNullOrWhiteSpace(priorMaterialName))
            {
                foreach (ListViewItem item in materialList.Items)
                {
                    if (item.Tag is Material mat && string.Equals(mat.Name, priorMaterialName, StringComparison.OrdinalIgnoreCase))
                    {
                        item.Selected = true;
                        item.Focused = true;
                        item.EnsureVisible();
                        break;
                    }
                }
            }

            SyncJsonEditorToCurrentMaterialSelection();
        }

        private static class NativeMethods
        {
            public const int WM_SETREDRAW = 0x000B;

            [DllImport("user32.dll")]
            public static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
        }

        private sealed class RedrawScope : IDisposable
        {
            private readonly Control control;
            private bool disposed;

            public RedrawScope(Control control)
            {
                this.control = control;
                if (!OperatingSystem.IsWindows() || !control.IsHandleCreated)
                {
                    return;
                }

                NativeMethods.SendMessage(control.Handle, NativeMethods.WM_SETREDRAW, IntPtr.Zero, IntPtr.Zero);
            }

            public void Dispose()
            {
                if (disposed)
                {
                    return;
                }

                disposed = true;

                if (!OperatingSystem.IsWindows() || !control.IsHandleCreated)
                {
                    return;
                }

                NativeMethods.SendMessage(control.Handle, NativeMethods.WM_SETREDRAW, new IntPtr(1), IntPtr.Zero);
                control.Invalidate(true);
                control.Update();
            }
        }

    }
}
