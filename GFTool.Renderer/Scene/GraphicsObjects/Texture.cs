using BnTxx;
using GFTool.Core.Utils;
using GFTool.Renderer.Core;
using OpenTK.Graphics.OpenGL4;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Trinity.Core.Flatbuffers.TR.Model;

namespace GFTool.Renderer.Scene.GraphicsObjects
{
    public class Texture : IDisposable
    {
        private class CachedTexture
        {
            public int TextureId;
            public int RefCount;
        }

        private static readonly Dictionary<string, CachedTexture> cache = new Dictionary<string, CachedTexture>(StringComparer.OrdinalIgnoreCase);
        private static readonly object cacheLock = new object();

        public string Name { get; private set; }
        public string SourceFile { get; private set; }
        public uint Slot { get; private set; }
        public Bitmap tex { get; private set; }
        public int textureId { get; private set; }

        private readonly string cacheKey;
        private readonly string texturePath;
        private readonly string altTexturePath;
        private readonly string preferredName;

        public Texture(PathString modelPath, TRTexture img)
        {
            Name = img.Name;
            SourceFile = img.File;
            Slot = img.Slot;
            string texturePath;
            try
            {
                texturePath = Path.GetFullPath(modelPath.Combine(img.File));
            }
            catch
            {
                texturePath = modelPath.Combine(img.File);
            }
            var preferredName = Path.GetFileNameWithoutExtension(img.File);
            this.texturePath = texturePath;
            try
            {
                altTexturePath = Path.GetFullPath(modelPath.Combine(Path.GetFileName(img.File)));
            }
            catch
            {
                altTexturePath = modelPath.Combine(Path.GetFileName(img.File));
            }
            this.preferredName = preferredName;
            var keyPath = File.Exists(texturePath) ? texturePath : (File.Exists(altTexturePath) ? altTexturePath : texturePath);
            cacheKey = $"{keyPath}|{preferredName}";

            lock (cacheLock)
            {
                if (cache.TryGetValue(cacheKey, out var cached))
                {
                    cached.RefCount++;
                    textureId = cached.TextureId;
                    return;
                }
            }
        }

        public void EnsureLoaded()
        {
            if (textureId > 0)
            {
                return;
            }

            try
            {
                if (!File.Exists(texturePath) && File.Exists(altTexturePath))
                {
                    tex = BNTX.LoadFromFile(altTexturePath, preferredName);
                }

                if (tex == null && File.Exists(texturePath))
                {
                    tex = BNTX.LoadFromFile(texturePath, preferredName);
                }
            }
            catch
            {
                tex = null;
            }

            if (tex == null)
            {
                tex = new Bitmap(32, 32);
                MessageHandler.Instance.AddMessage(MessageType.WARNING, string.Format("Failed to load texture: {0}", SourceFile));
            }

            int id = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, id);

            BitmapData bitmapData = tex.LockBits(new Rectangle(0, 0, tex.Width, tex.Height), ImageLockMode.ReadOnly, tex.PixelFormat);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, bitmapData.Width, bitmapData.Height, 0, OpenTK.Graphics.OpenGL4.PixelFormat.Bgra, PixelType.UnsignedByte, bitmapData.Scan0);
            tex.UnlockBits(bitmapData);

            // Set texture parameters
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

            tex.Dispose();
            tex = null;

            lock (cacheLock)
            {
                cache[cacheKey] = new CachedTexture { TextureId = id, RefCount = 1 };
            }

            textureId = id;
        }

        public Bitmap? LoadPreviewBitmap()
        {
            try
            {
                if (File.Exists(texturePath))
                {
                    return BNTX.LoadFromFile(texturePath, preferredName);
                }

                if (File.Exists(altTexturePath))
                {
                    return BNTX.LoadFromFile(altTexturePath, preferredName);
                }
            }
            catch
            {
                return null;
            }

            return null;
        }

        public bool TryGetResolvedSourcePath(out string path)
        {
            if (File.Exists(texturePath))
            {
                path = texturePath;
                return true;
            }

            if (File.Exists(altTexturePath))
            {
                path = altTexturePath;
                return true;
            }

            path = texturePath;
            return false;
        }

        public void Dispose()
        {
            tex?.Dispose();
            tex = null;
            if (textureId <= 0)
            {
                return;
            }

            lock (cacheLock)
            {
                if (!cache.TryGetValue(cacheKey, out var cached))
                {
                    GL.DeleteTexture(textureId);
                    textureId = 0;
                    return;
                }

                cached.RefCount--;
                if (cached.RefCount <= 0)
                {
                    GL.DeleteTexture(cached.TextureId);
                    cache.Remove(cacheKey);
                }
            }
            textureId = 0;
        }
    }
}
