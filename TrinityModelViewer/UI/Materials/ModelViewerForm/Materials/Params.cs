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
        private void materialParamsGrid_CurrentCellDirtyStateChanged(object? sender, EventArgs e)
        {
            if (materialParamsGrid.IsCurrentCellDirty)
            {
                materialParamsGrid.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        }

        private void materialParamsGrid_CellClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (suppressMaterialParamCellClickOnce)
            {
                suppressMaterialParamCellClickOnce = false;
                return;
            }

            if (e.RowIndex < 0 || e.ColumnIndex < 0)
            {
                return;
            }

            if (!string.Equals(materialParamsGrid.Columns[e.ColumnIndex].Name, "ParamValue", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (materialParamsGrid.Rows[e.RowIndex].Cells[e.ColumnIndex] is DataGridViewComboBoxCell)
            {
                materialParamsGrid.CurrentCell = materialParamsGrid.Rows[e.RowIndex].Cells[e.ColumnIndex];
                materialParamsGrid.BeginEdit(true);
                if (materialParamsGrid.EditingControl is ComboBox combo)
                {
                    combo.DroppedDown = true;
                }
            }
        }

        private void materialParamsGrid_CellMouseClick(object? sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0)
            {
                return;
            }

            if (!string.Equals(materialParamsGrid.Columns[e.ColumnIndex].Name, "ParamValue", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            const int swatchWidth = 22;
            if (e.X > swatchWidth)
            {
                return;
            }

            var row = materialParamsGrid.Rows[e.RowIndex];
            string name = row.Cells["ParamName"].Value?.ToString() ?? string.Empty;
            string type = row.Cells["ParamType"].Value?.ToString() ?? string.Empty;

            if (TryGetBaseColorIndexSuffix(name, out int suffix))
            {
                if (row.Cells["ParamValue"].ReadOnly || currentMaterial == null)
                {
                    return;
                }

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
                    return;
                }

                int initialIndex = 1;
                if (row.Cells["ParamValue"].Value is string s &&
                    int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsedIndex) &&
                    parsedIndex > 0)
                {
                    initialIndex = parsedIndex;
                }
                else if (currentMaterial.TryGetShaderParamIntEffective(name, out int effectiveIndex) && effectiveIndex > 0)
                {
                    initialIndex = effectiveIndex;
                }

                using var pickerDialog = new ColorTablePickerForm(bmp, divide, initialIndex);
                ApplyTheme(pickerDialog);
                if (pickerDialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                int selectedIndex = pickerDialog.SelectedIndex1Based;
                suppressMaterialParamCellClickOnce = true;
                row.Cells["ParamValue"].Value = selectedIndex.ToString(CultureInfo.InvariantCulture);
                return;
            }

            if (!materialsEditorService.IsColorVec4Param(name, type))
            {
                return;
            }

            if (row.Cells["ParamValue"].ReadOnly || currentMaterial == null)
            {
                return;
            }

            string currentText = row.Cells["ParamValue"].Value?.ToString() ?? string.Empty;
            if (!materialsEditorService.TryParseVec4(currentText, out var v))
            {
                v = new Vector4(1f, 1f, 1f, 1f);
            }

            using var dialog = new ColorDialog
            {
                FullOpen = true,
                AnyColor = true,
                Color = Color.FromArgb(
                    materialsEditorService.ClampToByte(v.W),
                    materialsEditorService.ClampToByte(v.X),
                    materialsEditorService.ClampToByte(v.Y),
                    materialsEditorService.ClampToByte(v.Z))
            };

            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            var c = dialog.Color;
            float a = v.W;
            string newText =
                $"{(c.R / 255f).ToString("0.####", CultureInfo.InvariantCulture)}, {(c.G / 255f).ToString("0.####", CultureInfo.InvariantCulture)}, {(c.B / 255f).ToString("0.####", CultureInfo.InvariantCulture)}, {a.ToString("0.####", CultureInfo.InvariantCulture)}";

            suppressMaterialParamCellClickOnce = true;
            row.Cells["ParamValue"].Value = newText;
        }

        private void materialParamsGrid_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0)
            {
                return;
            }

            if (!string.Equals(materialParamsGrid.Columns[e.ColumnIndex].Name, "ParamValue", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var row = materialParamsGrid.Rows[e.RowIndex];
            string name = row.Cells["ParamName"].Value?.ToString() ?? string.Empty;
            string type = row.Cells["ParamType"].Value?.ToString() ?? string.Empty;

            if (TryGetBaseColorIndexSuffix(name, out int suffix) && currentMaterial != null)
            {
                string layerName = $"BaseColorLayer{suffix}";
                Vector4 layerColor;
                if (currentMaterial.TryGetUniformOverride(layerName, out var overrideValue) && overrideValue is Vector4 ov)
                {
                    layerColor = ov;
                }
                else
                {
                    var vec4 = currentMaterial.Vec4Parameters.FirstOrDefault(p => string.Equals(p.Name, layerName, StringComparison.OrdinalIgnoreCase));
                    if (vec4 == null)
                    {
                        return;
                    }
                    var vv = vec4.Value;
                    layerColor = new Vector4(vv.W, vv.X, vv.Y, vv.Z);
                }

                string displayText = e.FormattedValue?.ToString() ?? string.Empty;

                const int indexSwatchWidth = 22;
                const int indexSwatchPadding = 3;

                e.PaintBackground(e.CellBounds, e.State.HasFlag(DataGridViewElementStates.Selected));

                var indexSwatchRect = new Rectangle(
                    e.CellBounds.Left + indexSwatchPadding,
                    e.CellBounds.Top + indexSwatchPadding,
                    indexSwatchWidth - indexSwatchPadding * 2,
                    e.CellBounds.Height - indexSwatchPadding * 2);

                using (var brush = new SolidBrush(ColorFromLinearRgb(new Vector3(layerColor.X, layerColor.Y, layerColor.Z))))
                {
                    e.Graphics.FillRectangle(brush, indexSwatchRect);
                }
                e.Graphics.DrawRectangle(Pens.Black, indexSwatchRect);

                var displayTextRect = new Rectangle(
                    e.CellBounds.Left + indexSwatchWidth,
                    e.CellBounds.Top,
                    e.CellBounds.Width - indexSwatchWidth,
                    e.CellBounds.Height);

                TextRenderer.DrawText(
                    e.Graphics,
                    displayText,
                    e.CellStyle.Font,
                    displayTextRect,
                    e.CellStyle.ForeColor,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

                e.Handled = true;
                return;
            }

            if (!materialsEditorService.IsColorVec4Param(name, type))
            {
                return;
            }

            string text = e.FormattedValue?.ToString() ?? string.Empty;
            if (!materialsEditorService.TryParseVec4(text, out var v))
            {
                return;
            }

            const int swatchWidth = 22;
            const int padding = 3;

            e.PaintBackground(e.CellBounds, e.State.HasFlag(DataGridViewElementStates.Selected));

            var swatchRect = new Rectangle(
                e.CellBounds.Left + padding,
                e.CellBounds.Top + padding,
                swatchWidth - padding * 2,
                e.CellBounds.Height - padding * 2);

                using (var brush = new SolidBrush(Color.FromArgb(
                           materialsEditorService.ClampToByte(v.W),
                           materialsEditorService.ClampToByte(v.X),
                           materialsEditorService.ClampToByte(v.Y),
                           materialsEditorService.ClampToByte(v.Z))))
                {
                    e.Graphics.FillRectangle(brush, swatchRect);
                }
            e.Graphics.DrawRectangle(Pens.Black, swatchRect);

            var textRect = new Rectangle(
                e.CellBounds.Left + swatchWidth,
                e.CellBounds.Top,
                e.CellBounds.Width - swatchWidth,
                e.CellBounds.Height);

            TextRenderer.DrawText(
                e.Graphics,
                text,
                e.CellStyle.Font,
                textRect,
                e.CellStyle.ForeColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

            e.Handled = true;
        }

        private void materialParamsGrid_CellBeginEdit(object? sender, DataGridViewCellCancelEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0)
            {
                return;
            }

            if (!string.Equals(materialParamsGrid.Columns[e.ColumnIndex].Name, "ParamValue", StringComparison.OrdinalIgnoreCase))
            {
                e.Cancel = true;
                return;
            }

            var type = materialParamsGrid.Rows[e.RowIndex].Cells["ParamType"].Value?.ToString() ?? string.Empty;
            if (string.Equals(type, "Name", StringComparison.OrdinalIgnoreCase))
            {
                e.Cancel = true;
            }
        }

        private void materialParamsGrid_CellValueChanged(object? sender, DataGridViewCellEventArgs e)
        {
            if (isUpdatingMaterialGrids || currentMaterial == null)
            {
                return;
            }

            if (e.RowIndex < 0 || e.ColumnIndex < 0)
            {
                return;
            }

            var col = materialParamsGrid.Columns[e.ColumnIndex];
            if (!string.Equals(col.Name, "ParamValue", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var row = materialParamsGrid.Rows[e.RowIndex];
            var name = row.Cells["ParamName"].Value?.ToString() ?? string.Empty;
            var type = row.Cells["ParamType"].Value?.ToString() ?? string.Empty;
            var rawValue = row.Cells["ParamValue"].Value?.ToString() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(name) || string.Equals(type, "Name", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            row.ErrorText = string.Empty;
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                currentMaterial.ClearUniformOverride(name);
                currentMaterialsModel?.TryClearMaterialUniformOverride(currentMaterial.Name, name);
                RefreshMaterialParamRowFromCurrentMaterial(row);
                return;
            }

            try
            {
                if (!materialsEditorService.TryParseUniformValue(type, rawValue, out var parsedValue, out var errorText))
                {
                    row.ErrorText = errorText ?? "Invalid value.";
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
                    RequestMaterialPreviewUpdate();
                }

                UpdateMaterialUvRowIfPresent(name, type, rawValue);
                UpdateUvPreview();
            }
            catch (Exception ex)
            {
                row.ErrorText = ex.Message;
            }
        }

        private void UpdateMaterialUvRowIfPresent(string name, string type, string displayValue)
        {
            if (!IsUvParamName(name))
            {
                return;
            }

            for (int i = 0; i < materialUvGrid.Rows.Count; i++)
            {
                var rowName = materialUvGrid.Rows[i].Cells["UvName"].Value?.ToString() ?? string.Empty;
                if (!string.Equals(rowName, name, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                materialUvGrid.Rows[i].Cells["UvValue"].Value = displayValue;
                return;
            }
        }

        private void RefreshMaterialParamRowFromCurrentMaterial(DataGridViewRow row)
        {
            UpdateUvPreview();
        }

        private void ConfigureParamValueCell(int rowIndex, string name, string type, string value)
        {
            if (rowIndex < 0 || rowIndex >= materialParamsGrid.Rows.Count)
            {
                return;
            }

            if (!string.Equals(type, "Option", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (!materialsEditorService.IsLikelyBoolOption(name, value))
            {
                return;
            }

            var row = materialParamsGrid.Rows[rowIndex];
            var cell = new DataGridViewComboBoxCell();
            cell.DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton;
            cell.DisplayStyleForCurrentCellOnly = false;
            cell.Items.Add("False");
            cell.Items.Add("True");
            string normalized = value;
            if (value == "0") normalized = "False";
            if (value == "1") normalized = "True";
            if (normalized.Equals("true", StringComparison.OrdinalIgnoreCase)) normalized = "True";
            if (normalized.Equals("false", StringComparison.OrdinalIgnoreCase)) normalized = "False";
            cell.Value = normalized;
            row.Cells["ParamValue"] = cell;
        }

        private string FormatOverrideValue(string type, object value) => materialsEditorService.FormatOverrideValue(type, value);

    }
}
