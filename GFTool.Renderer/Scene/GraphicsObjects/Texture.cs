using BnTxx;
using GFTool.Core.Utils;
using GFTool.Renderer.Core;
using OpenTK.Graphics.OpenGL4;
using System.Drawing;
using System.Drawing.Imaging;
using Trinity.Core.Flatbuffers.TR.Model;

namespace GFTool.Renderer.Scene.GraphicsObjects
{
    public class Texture : IDisposable
    {
        public string Name { get; private set; }
        public Bitmap tex { get; private set; }
        public int textureId { get; private set; }

        public Texture(PathString modelPath, TRTexture img)
        {
            Name = img.Name;
            
            tex = BNTX.LoadFromFile(modelPath.Combine(img.File));

            if (tex == null)
            {
                tex = new Bitmap(32, 32);
                MessageHandler.Instance.AddMessage(MessageType.ERROR, string.Format("Failed to load texture: {0}", img.Name));
            }

            textureId = Generate();
        }

        private int Generate()
        {
            if (tex == null) return -1;

            int id = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, id);

            BitmapData bitmapData = tex.LockBits(new Rectangle(0, 0, tex.Width, tex.Height), ImageLockMode.ReadOnly, tex.PixelFormat);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, bitmapData.Width, bitmapData.Height, 0, OpenTK.Graphics.OpenGL4.PixelFormat.Bgra, PixelType.UnsignedByte, bitmapData.Scan0);

            // Set texture parameters
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

            return id;
        }

        public void Dispose()
        {
            tex?.Dispose();
            GL.DeleteTexture(textureId);
        }
    }
}
