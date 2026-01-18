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
        private void UpdateUvPreview()
        {
            RequestMaterialPreviewUpdate();
        }

        private void materialUvSetCombo_SelectedIndexChanged(object? sender, EventArgs e)
        {
            RequestMaterialPreviewUpdate();
        }

        private void materialUvWrapModeCombo_SelectedIndexChanged(object? sender, EventArgs e)
        {
            RequestMaterialPreviewUpdate();
        }

        private Texture? GetSelectedTexture()
        {
            if (materialTexturesGrid.SelectedRows.Count > 0 &&
                materialTexturesGrid.SelectedRows[0].Tag is Texture selected)
            {
                return selected;
            }

            if (materialTexturesGrid.Rows.Count > 0 && materialTexturesGrid.Rows[0].Tag is Texture first)
            {
                return first;
            }

            return null;
        }

        private void RequestMaterialPreviewUpdate()
        {
            previewUpdateCts?.Cancel();
            previewUpdateCts?.Dispose();
            previewUpdateCts = new CancellationTokenSource();
            var token = previewUpdateCts.Token;
            int serial = Interlocked.Increment(ref previewUpdateSerial);

            if (currentMaterialsModel == null || currentMaterial == null)
            {
                SetTexturePreview(null, ownsImage: true);
                SetUvPreview(null, ownsImage: true);
                return;
            }

            var texture = GetSelectedTexture();
            var desiredWrapLabel = texture == null
                ? "Wrap: Auto (Sampler)"
                : $"Wrap: Auto (Sampler: {texture.WrapS}/{texture.WrapT})";
            if (!string.Equals(uvWrapAutoItem.Text, desiredWrapLabel, StringComparison.Ordinal))
            {
                uvWrapAutoItem.Text = desiredWrapLabel;
                materialUvWrapModeCombo.Refresh();
            }

            var channel = GetSelectedTexturePreviewChannel();
            var uvIndex = Math.Clamp(materialUvSetCombo.SelectedIndex, 0, 1);
            var uvSets = currentMaterialsModel.GetUvSetsForMaterial(currentMaterial.Name, uvIndex);
            var uvScaleOffset = GetUvScaleOffset(currentMaterial);
            var (wrapU, wrapV) = ResolveUvPreviewWrapModes(texture);

            string? textureKey = texture?.CacheKey;
            string uvKey = textureKey == null
                ? $"uv|{RuntimeHelpers.GetHashCode(currentMaterialsModel)}|{currentMaterial.Name}|{uvIndex}|wrap{(int)wrapU}{(int)wrapV}|st{uvScaleOffset.X:0.####},{uvScaleOffset.Y:0.####},{uvScaleOffset.Z:0.####},{uvScaleOffset.W:0.####}"
                : $"uv|{RuntimeHelpers.GetHashCode(currentMaterialsModel)}|{currentMaterial.Name}|{uvIndex}|{textureKey}|wrap{(int)wrapU}{(int)wrapV}|st{uvScaleOffset.X:0.####},{uvScaleOffset.Y:0.####},{uvScaleOffset.Z:0.####},{uvScaleOffset.W:0.####}";

            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(50, token);

                    if (token.IsCancellationRequested)
                    {
                        return;
                    }

                    Bitmap? sourceBitmap = null;
                    if (texture != null && textureKey != null)
                    {
                        if (!texturePreviewCache.TryGet(textureKey, out sourceBitmap))
                        {
                            sourceBitmap = texture.LoadPreviewBitmap();
                            if (sourceBitmap != null)
                            {
                                texturePreviewCache.Put(textureKey, sourceBitmap);
                            }
                        }
                    }

                    if (token.IsCancellationRequested)
                    {
                        return;
                    }

                    Bitmap? uvBitmap = null;
                    if (uvSets.Count > 0)
                    {
                        if (!uvPreviewCache.TryGet(uvKey, out uvBitmap))
                        {
                            uvBitmap = (Bitmap)BuildUvPreview(sourceBitmap, uvSets, uvScaleOffset, wrapU, wrapV);
                            uvPreviewCache.Put(uvKey, uvBitmap);
                        }
                    }

                    if (token.IsCancellationRequested)
                    {
                        return;
                    }

                    BeginInvoke((Action)(() =>
                    {
                        if (IsDisposed || serial != Volatile.Read(ref previewUpdateSerial))
                        {
                            return;
                        }

                        if (sourceBitmap != null)
                        {
                            SetTexturePreview(sourceBitmap, ownsImage: false);

                            if (channel != TexturePreviewChannel.Rgba)
                            {
                                UpdateTexturePreviewDisplay();
                            }
                        }
                        else
                        {
                            SetTexturePreview(null, ownsImage: true);
                        }

                        if (uvBitmap != null)
                        {
                            SetUvPreview(uvBitmap, ownsImage: false);
                        }
                        else if (sourceBitmap != null)
                        {
                            SetUvPreview(sourceBitmap, ownsImage: false);
                        }
                        else
                        {
                            SetUvPreview(null, ownsImage: true);
                        }
                    }));
                }
                catch (OperationCanceledException)
                {
                    // Ignore.
                }
                catch
                {
                    // Ignore preview failures; selection changes can race disposal.
                }
            }, token);
        }

        private static Vector4 GetUvScaleOffset(Material material)
        {
            if (material.TryGetUniformOverride("UVScaleOffset", out var overrideValue) && overrideValue is Vector4 overridden)
            {
                return overridden;
            }

            foreach (var param in material.Vec4Parameters)
            {
                if (string.Equals(param.Name, "UVScaleOffset", StringComparison.OrdinalIgnoreCase))
                {
                    return new Vector4(param.Value.W, param.Value.X, param.Value.Y, param.Value.Z);
                }
            }

            return new Vector4(1f, 1f, 0f, 0f);
        }

        private enum UvPreviewWrapMode
        {
            Repeat = 0,
            MirroredRepeat = 1,
            Clamp = 2
        }

        private (UvPreviewWrapMode WrapU, UvPreviewWrapMode WrapV) ResolveUvPreviewWrapModes(Texture? texture)
        {
            static UvPreviewWrapMode FromWrap(TextureWrapMode mode) => mode switch
            {
                TextureWrapMode.MirroredRepeat => UvPreviewWrapMode.MirroredRepeat,
                TextureWrapMode.Repeat => UvPreviewWrapMode.Repeat,
                _ => UvPreviewWrapMode.Clamp
            };

            var selected = materialUvWrapModeCombo.SelectedIndex;
            if (selected == 1) return (UvPreviewWrapMode.Repeat, UvPreviewWrapMode.Repeat);
            if (selected == 2) return (UvPreviewWrapMode.MirroredRepeat, UvPreviewWrapMode.MirroredRepeat);
            if (selected == 3) return (UvPreviewWrapMode.Clamp, UvPreviewWrapMode.Clamp);

            if (texture == null) return (UvPreviewWrapMode.Repeat, UvPreviewWrapMode.Repeat);

            return (FromWrap(texture.WrapS), FromWrap(texture.WrapT));
        }

        private Image BuildUvPreview(Bitmap? sourceBitmap, IReadOnlyList<Model.UvSet> uvSets, Vector4 uvScaleOffset, UvPreviewWrapMode wrapU, UvPreviewWrapMode wrapV)
        {
            var baseBitmap = new Bitmap(
                sourceBitmap?.Width ?? 256,
                sourceBitmap?.Height ?? 256,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            using var g = Graphics.FromImage(baseBitmap);
            g.Clear(Color.FromArgb(30, 30, 30));
            if (sourceBitmap != null)
            {
                g.DrawImage(sourceBitmap, 0, 0, baseBitmap.Width, baseBitmap.Height);
            }
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using var pen = new Pen(Color.FromArgb(255, 255, 220, 40), 2.0f);

            var width = baseBitmap.Width;
            var height = baseBitmap.Height;

            foreach (var set in uvSets)
            {
                var uvs = set.Uvs;
                var indices = set.Indices;

                if (indices.Length < 3)
                {
                    continue;
                }

                for (int i = 0; i + 2 < indices.Length; i += 3)
                {
                    var i0 = (int)indices[i];
                    var i1 = (int)indices[i + 1];
                    var i2 = (int)indices[i + 2];
                    if (i0 >= uvs.Length || i1 >= uvs.Length || i2 >= uvs.Length)
                    {
                        continue;
                    }

                    var p0 = UvToPoint(uvs[i0], uvScaleOffset, width, height, wrapU, wrapV);
                    var p1 = UvToPoint(uvs[i1], uvScaleOffset, width, height, wrapU, wrapV);
                    var p2 = UvToPoint(uvs[i2], uvScaleOffset, width, height, wrapU, wrapV);

                    g.DrawLine(pen, p0, p1);
                    g.DrawLine(pen, p1, p2);
                    g.DrawLine(pen, p2, p0);
                }
            }

            return baseBitmap;
        }

        private static PointF UvToPoint(Vector2 uv, Vector4 uvScaleOffset, int width, int height, UvPreviewWrapMode wrapU, UvPreviewWrapMode wrapV)
        {
            var t = TransformUv(uv, uvScaleOffset, wrapU, wrapV);
            float u = t.X;
            float v = t.Y;

            if (float.IsNaN(u) || float.IsInfinity(u)) u = 0.5f;
            if (float.IsNaN(v) || float.IsInfinity(v)) v = 0.5f;

            u = Math.Clamp(u, 0f, 1f);
            v = Math.Clamp(v, 0f, 1f);

            float x = u * (width - 1);
            float y = (1f - v) * (height - 1);
            return new PointF(x, y);
        }

        private static Vector2 TransformUv(Vector2 uv, Vector4 uvScaleOffset, UvPreviewWrapMode wrapU, UvPreviewWrapMode wrapV)
        {
            var scaleX = Math.Abs(uvScaleOffset.X) < 0.0001f ? 1f : uvScaleOffset.X;
            var scaleY = Math.Abs(uvScaleOffset.Y) < 0.0001f ? 1f : uvScaleOffset.Y;
            float u = uv.X * scaleX + uvScaleOffset.Z;
            float v = uv.Y * scaleY + uvScaleOffset.W;

            static float WrapRepeat(float value)
            {
                return value - (float)Math.Floor(value);
            }

            static float WrapMirroredRepeat(float value)
            {
                int tile = (int)Math.Floor(value);
                float f = value - tile;
                if ((tile & 1) != 0)
                {
                    f = 1f - f;
                }
                return f;
            }

            static float Wrap(float value, UvPreviewWrapMode mode)
            {
                return mode switch
                {
                    UvPreviewWrapMode.MirroredRepeat => WrapMirroredRepeat(value),
                    UvPreviewWrapMode.Clamp => Math.Clamp(value, 0f, 1f),
                    _ => WrapRepeat(value)
                };
            }

            u = Wrap(u, wrapU);
            v = Wrap(v, wrapV);
            return new Vector2(u, v);
        }

        private void SetUvPreview(Image? image, bool ownsImage)
        {
            if (ownsUvPreviewImage)
            {
                uvPreviewImage?.Dispose();
            }

            uvPreviewImage = image;
            ownsUvPreviewImage = ownsImage;
            materialUvPreview.Image = image;
        }

        private static bool IsUvParamName(string name)
        {
            return name.Contains("UV", StringComparison.OrdinalIgnoreCase);
        }
    }
}
