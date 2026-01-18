using GFTool.Renderer.Core;
using GFTool.Renderer.Scene.GraphicsObjects;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Trinity.Core.Assets;
using TrinityModelViewer.Export;

namespace TrinityModelViewer
{
    public partial class ModelViewerForm
    {
        private void EnsureTextureGridContextMenu()
        {
            if (textureGridContextMenu != null)
            {
                return;
            }

            textureGridContextMenu = new ContextMenuStrip();
            var import = new ToolStripMenuItem("Import...");
            import.Click += (sender, e) => ImportSelectedTexture();
            textureGridContextMenu.Items.Add(import);
            var export = new ToolStripMenuItem("Export...");
            export.Click += (sender, e) => ExportSelectedTexture();
            textureGridContextMenu.Items.Add(export);
        }

        private void materialTexturesGrid_MouseUp(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
            {
                return;
            }

            var hit = materialTexturesGrid.HitTest(e.X, e.Y);
            if (hit.Type != DataGridViewHitTestType.Cell || hit.RowIndex < 0)
            {
                return;
            }

            materialTexturesGrid.ClearSelection();
            materialTexturesGrid.Rows[hit.RowIndex].Selected = true;

            if (materialTexturesGrid.Rows[hit.RowIndex].Tag is not Texture)
            {
                return;
            }

            EnsureTextureGridContextMenu();
            textureGridContextMenu?.Show(materialTexturesGrid, new Point(e.X, e.Y));
        }

        private void ExportSelectedTexture()
        {
            var texture = GetSelectedTexture();
            if (texture == null)
            {
                MessageBox.Show(this, "No texture selected.", "Export Texture", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string baseName;
            try
            {
                baseName = Path.GetFileNameWithoutExtension(texture.SourceFile);
                if (string.IsNullOrWhiteSpace(baseName))
                {
                    baseName = texture.Name;
                }
            }
            catch
            {
                baseName = texture.Name;
            }

            using var sfd = new SaveFileDialog();
            sfd.Title = "Export Texture";
            sfd.Filter = "PNG image (*.png)|*.png|BNTX texture (*.bntx)|*.bntx";
            sfd.FileName = baseName + ".png";
            if (sfd.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            string outPath = sfd.FileName;
            string ext = Path.GetExtension(outPath).ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(ext))
            {
                ext = sfd.FilterIndex == 2 ? ".bntx" : ".png";
                outPath += ext;
            }

            try
            {
                if (ext == ".bntx")
                {
                    if (!texture.TryGetResolvedSourcePath(out var sourcePath) || !File.Exists(sourcePath))
                    {
                        MessageBox.Show(this, "Source BNTX file was not found on disk.", "Export Texture", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    File.Copy(sourcePath, outPath, overwrite: true);
                }
                else
                {
                    using var bmp = texture.LoadPreviewBitmap();
                    if (bmp == null)
                    {
                        MessageBox.Show(this, "Texture could not be decoded.", "Export Texture", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    bmp.Save(outPath, System.Drawing.Imaging.ImageFormat.Png);
                }

                MessageBox.Show(this, $"Exported:\n{outPath}", "Export Texture", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Export failed:\n{ex.Message}", "Export Texture", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ImportSelectedTexture()
        {
            var texture = GetSelectedTexture();
            if (texture == null)
            {
                MessageBox.Show(this, "No texture selected.", "Import Texture", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var ofd = new OpenFileDialog();
            ofd.Title = "Import Texture (replaces in-memory)";
            ofd.Filter = "Image files (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp|All files (*.*)|*.*";
            if (ofd.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            try
            {
                using var bmp = new Bitmap(ofd.FileName);
                if (!texture.TryReplaceFromImage(bmp, out var error))
                {
                    MessageBox.Show(this, $"Import failed:\n{error}", "Import Texture", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                InvalidateMaterialPreviewCaches(texture.CacheKey);
                RequestMaterialPreviewUpdate();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Import failed:\n{ex.Message}", "Import Texture", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void InvalidateMaterialPreviewCaches(string textureCacheKey)
        {
            if (string.IsNullOrWhiteSpace(textureCacheKey))
            {
                return;
            }

            texturePreviewCache.RemoveWhere(k => string.Equals(k, textureCacheKey, StringComparison.OrdinalIgnoreCase));
            textureChannelCache.RemoveWhere(k => k.StartsWith(textureCacheKey + "|", StringComparison.OrdinalIgnoreCase));
            uvPreviewCache.RemoveWhere(k => k.Contains("|" + textureCacheKey + "|", StringComparison.OrdinalIgnoreCase));
        }

    }
}
