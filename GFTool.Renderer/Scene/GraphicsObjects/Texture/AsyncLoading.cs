using BnTxx;
using GFTool.Renderer.Core;
using OpenTK.Graphics.OpenGL4;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;

namespace GFTool.Renderer.Scene.GraphicsObjects
{
    public partial class Texture
    {
        private sealed class DecodedTextureData : IDisposable
        {
            public readonly byte[] Bgra32;
            public readonly int Width;
            public readonly int Height;
            public readonly PixelInternalFormat InternalFormat;

            public DecodedTextureData(byte[] bgra32, int width, int height, PixelInternalFormat internalFormat)
            {
                Bgra32 = bgra32;
                Width = width;
                Height = height;
                InternalFormat = internalFormat;
            }

            public void Dispose()
            {
                // Nothing currently; placeholder for future pooled buffers.
            }
        }

        private static int placeholderTextureId;
        private static readonly object placeholderLock = new object();
        private static SemaphoreSlim? decodeConcurrency;

        private Task<DecodedTextureData?>? decodeTask;
        private volatile bool asyncLoadComplete;

        private static SemaphoreSlim GetDecodeConcurrency()
        {
            var existing = decodeConcurrency;
            if (existing != null)
            {
                return existing;
            }

            lock (placeholderLock)
            {
                decodeConcurrency ??= new SemaphoreSlim(Math.Max(1, RenderOptions.AsyncTextureDecodeConcurrency));
                return decodeConcurrency;
            }
        }

        private void EnsurePlaceholderTexture()
        {
            if (placeholderTextureId > 0)
            {
                return;
            }

            lock (placeholderLock)
            {
                if (placeholderTextureId > 0)
                {
                    return;
                }

                int id = GL.GenTexture();
                GL.BindTexture(TextureTarget.Texture2D, id);
                var pixel = new byte[] { 255, 0, 255, 255 }; // BGRA magenta
                var handle = GCHandle.Alloc(pixel, GCHandleType.Pinned);
                try
                {
                    GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba8, 1, 1, 0, OpenTK.Graphics.OpenGL4.PixelFormat.Bgra, PixelType.UnsignedByte, handle.AddrOfPinnedObject());
                }
                finally
                {
                    handle.Free();
                }
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
                placeholderTextureId = id;
            }
        }

        internal bool IsAsyncLoadComplete => asyncLoadComplete || textureId > 0 && textureId != placeholderTextureId;

        internal void StartAsyncDecodeIfNeeded()
        {
            if (asyncLoadComplete || decodeTask != null)
            {
                return;
            }

            decodeTask = Task.Run(async () =>
            {
                var gate = GetDecodeConcurrency();
                await gate.WaitAsync().ConfigureAwait(false);
                try
                {
                    return DecodeTextureData();
                }
                finally
                {
                    gate.Release();
                }
            });
        }

        internal bool TryUploadDecodedOnGlThread()
        {
            EnsurePlaceholderTexture();
            if (asyncLoadComplete)
            {
                return true;
            }

            if (textureId > 0 && textureId != placeholderTextureId)
            {
                asyncLoadComplete = true;
                return true;
            }

            var task = decodeTask;
            if (task == null)
            {
                return false;
            }

            if (!task.IsCompleted)
            {
                return false;
            }

            if (task.IsFaulted || task.IsCanceled)
            {
                asyncLoadComplete = true;
                return true;
            }

            var decoded = task.Result;
            if (decoded == null)
            {
                asyncLoadComplete = true;
                return true;
            }

            lock (cacheLock)
            {
                if (cache.TryGetValue(cacheKey, out var cached))
                {
                    cached.RefCount++;
                    textureId = cached.TextureId;
                    asyncLoadComplete = true;
                    return true;
                }
            }

            int id = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, id);

            var handle = GCHandle.Alloc(decoded.Bgra32, GCHandleType.Pinned);
            try
            {
                GL.TexImage2D(TextureTarget.Texture2D, 0, decoded.InternalFormat, decoded.Width, decoded.Height, 0, OpenTK.Graphics.OpenGL4.PixelFormat.Bgra, PixelType.UnsignedByte, handle.AddrOfPinnedObject());
            }
            finally
            {
                handle.Free();
                decoded.Dispose();
            }

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)wrapS);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)wrapT);

            lock (cacheLock)
            {
                cache[cacheKey] = new CachedTexture { TextureId = id, RefCount = 1 };
            }

            textureId = id;
            asyncLoadComplete = true;
            return true;
        }

        private DecodedTextureData? DecodeTextureData()
        {
            if (!OperatingSystem.IsWindows())
            {
                return null;
            }

            return DecodeTextureDataWindows();
        }

        [SupportedOSPlatform("windows")]
        private DecodedTextureData? DecodeTextureDataWindows()
        {
            Bitmap? bitmap = null;
            try
            {
                Bitmap? overrideBitmap = null;
                lock (cacheLock)
                {
                    if (overrideBitmaps.TryGetValue(cacheKey, out var bmp))
                    {
                        overrideBitmap = (Bitmap)bmp.Clone();
                    }
                }

                bool hadDecodeError = false;
                var triedPaths = MessageHandler.Instance.DebugLogsEnabled ? new System.Collections.Generic.List<string>() : null;

                if (overrideBitmap != null)
                {
                    bitmap = overrideBitmap;
                }
                else
                {
                    foreach (var candidatePath in EnumerateCandidateTexturePaths())
                    {
                        if (string.IsNullOrWhiteSpace(candidatePath))
                        {
                            continue;
                        }

                        triedPaths?.Add(candidatePath);

                        try
                        {
                            if (File.Exists(candidatePath) || File.Exists(diskTexturePath) || File.Exists(diskAltTexturePath))
                            {
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
                                    bitmap = new Bitmap(diskPath);
                                }
                                else
                                {
                                    if (!BNTX.TryLoadFromFile(diskPath, preferredName, out var loaded, out var error) &&
                                        !string.IsNullOrEmpty(error))
                                    {
                                        MessageHandler.Instance.AddMessage(MessageType.WARNING, $"Failed to decode texture '{SourceFile}': {error}");
                                        hadDecodeError = true;
                                    }
                                    bitmap = loaded;
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
                                bitmap = loaded;
                            }

                            if (bitmap != null)
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

                    if (bitmap == null && !hadDecodeError)
                    {
                        MessageHandler.Instance.AddMessage(MessageType.WARNING, $"Failed to load texture: {SourceFile}");
                        if (MessageHandler.Instance.DebugLogsEnabled && triedPaths != null && triedPaths.Count > 0)
                        {
                            MessageHandler.Instance.AddMessage(MessageType.LOG, $"Texture search paths for '{SourceFile}': {string.Join(" | ", triedPaths)}");
                        }
                    }
                }

                if (bitmap == null)
                {
                    return null;
                }

                using var upload = bitmap.PixelFormat == System.Drawing.Imaging.PixelFormat.Format32bppArgb
                    ? (Bitmap)bitmap.Clone()
                    : bitmap.Clone(new Rectangle(0, 0, bitmap.Width, bitmap.Height), System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                var data = upload.LockBits(new Rectangle(0, 0, upload.Width, upload.Height), ImageLockMode.ReadOnly, upload.PixelFormat);
                try
                {
                    int bytesLen = Math.Abs(data.Stride) * data.Height;
                    var bytes = new byte[bytesLen];
                    Marshal.Copy(data.Scan0, bytes, 0, bytes.Length);
                    var internalFormat = IsColorTexture(Name) ? PixelInternalFormat.Srgb8Alpha8 : PixelInternalFormat.Rgba8;
                    return new DecodedTextureData(bytes, data.Width, data.Height, internalFormat);
                }
                finally
                {
                    upload.UnlockBits(data);
                }
            }
            catch
            {
                return null;
            }
            finally
            {
                bitmap?.Dispose();
            }
        }
    }
}
