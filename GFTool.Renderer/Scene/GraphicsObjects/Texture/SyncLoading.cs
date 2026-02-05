using BnTxx;
using GFTool.Renderer.Core;
using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace GFTool.Renderer.Scene.GraphicsObjects
{
    public partial class Texture
    {
        private void EnsureLoadedSync()
        {
            if (textureId > 0)
            {
                return;
            }

            Bitmap? overrideBitmap = null;
            lock (cacheLock)
            {
                if (overrideBitmaps.TryGetValue(cacheKey, out var bmp))
                {
                    overrideBitmap = (Bitmap)bmp.Clone();
                }
            }

            bool hadDecodeError = false;
            var triedPaths = new List<string>();

            if (overrideBitmap != null)
            {
                tex?.Dispose();
                tex = overrideBitmap;
            }
            else
            {
                foreach (var candidatePath in EnumerateCandidateTexturePaths())
                {
                    if (string.IsNullOrWhiteSpace(candidatePath))
                    {
                        continue;
                    }

                    triedPaths.Add(candidatePath);

                    try
                    {
                        // Disk path: load directly from filesystem.
                        if (File.Exists(candidatePath) || File.Exists(diskTexturePath) || File.Exists(diskAltTexturePath))
                        {
                            // Prefer the disk paths when they exist to keep existing behavior.
                            var diskPath =
                                File.Exists(diskTexturePath) ? diskTexturePath :
                                (File.Exists(diskAltTexturePath) ? diskAltTexturePath :
                                candidatePath);

                            var ext = Path.GetExtension(diskPath);
                            if (!string.IsNullOrWhiteSpace(ext) &&
                                (ext.Equals(".png", StringComparison.OrdinalIgnoreCase) ||
                                 ext.Equals(".jpg", StringComparison.OrdinalIgnoreCase) ||
                                 ext.Equals(".jpeg", StringComparison.OrdinalIgnoreCase) ||
                                 ext.Equals(".bmp", StringComparison.OrdinalIgnoreCase)))
                            {
                                tex = new Bitmap(diskPath);
                            }
                            else
                            {
                                if (!BNTX.TryLoadFromFile(diskPath, preferredName, out var loaded, out var error) &&
                                    !string.IsNullOrEmpty(error))
                                {
                                    MessageHandler.Instance.AddMessage(MessageType.WARNING, $"Failed to decode texture '{SourceFile}': {error}");
                                    hadDecodeError = true;
                                }
                                tex = loaded;
                            }
                        }
                        else if (assetProvider != null && assetProvider.Exists(candidatePath))
                        {
                            using var s = assetProvider.OpenRead(candidatePath);
                            if (!BNTX.TryLoadFromStream(s, preferredName, out var loaded, out var error) &&
                                !string.IsNullOrEmpty(error))
                            {
                                MessageHandler.Instance.AddMessage(MessageType.WARNING, $"Failed to decode texture '{SourceFile}': {error}");
                                hadDecodeError = true;
                            }
                            tex = loaded;
                        }

                        if (tex != null)
                        {
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        hadDecodeError = true;
                        if (MessageHandler.Instance.DebugLogsEnabled)
                        {
                            MessageHandler.Instance.AddMessage(MessageType.WARNING, $"Exception while loading texture '{SourceFile}' from '{candidatePath}': {ex}");
                        }
                        else
                        {
                            MessageHandler.Instance.AddMessage(MessageType.WARNING, $"Exception while loading texture '{SourceFile}' from '{candidatePath}': {ex.Message}");
                        }
                    }
                }
            }

            if (tex == null)
            {
                tex = new Bitmap(32, 32);
                if (!hadDecodeError)
                {
                    MessageHandler.Instance.AddMessage(MessageType.WARNING, string.Format("Failed to load texture: {0}", SourceFile));
                    if (MessageHandler.Instance.DebugLogsEnabled && triedPaths.Count > 0)
                    {
                        MessageHandler.Instance.AddMessage(MessageType.LOG, $"Texture search paths for '{SourceFile}': {string.Join(" | ", triedPaths)}");
                    }
                }
            }

            int id = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, id);

            BitmapData bitmapData = tex.LockBits(new Rectangle(0, 0, tex.Width, tex.Height), ImageLockMode.ReadOnly, tex.PixelFormat);
            var internalFormat = IsColorTexture(Name) ? PixelInternalFormat.Srgb8Alpha8 : PixelInternalFormat.Rgba8;
            GL.TexImage2D(TextureTarget.Texture2D, 0, internalFormat, bitmapData.Width, bitmapData.Height, 0, OpenTK.Graphics.OpenGL4.PixelFormat.Bgra, PixelType.UnsignedByte, bitmapData.Scan0);
            tex.UnlockBits(bitmapData);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)wrapS);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)wrapT);

            tex.Dispose();
            tex = null;

            lock (cacheLock)
            {
                cache[cacheKey] = new CachedTexture { TextureId = id, RefCount = 1 };
            }

            textureId = id;
        }
    }
}
