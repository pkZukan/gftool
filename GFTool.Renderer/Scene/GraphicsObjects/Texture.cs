using GFTool.Core.Middleware;
using GFTool.Renderer.Utils;
using OpenTK.Audio.OpenAL;
using OpenTK.Graphics.OpenGL4;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;

namespace GFTool.Renderer.Scene.GraphicsObjects
{
    public class Texture : IDisposable
    {
        public string Name { get; private set; }
        public BNTX tex { get; private set; }
        public int textureId { get; private set; }

        public Texture(string name, BNTX img)
        {
            Name = name;
            tex = img;
            textureId = Generate();
        }

        private int Generate()
        {
            int id = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, id);

            // Specify the texture data
            var glFormat = Util.BntxFormatToGL4(tex.Format);
            GL.TexImage2D(TextureTarget.Texture2D, 0, glFormat.Item1, tex.Width, tex.Height, 0, glFormat.Item2, PixelType.UnsignedByte, tex.Images[0]);

            // Set texture parameters
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

            return id;
        }

        public void Dispose()
        {
            if (tex != null)
            {
                tex = null;
            }

            GL.DeleteTexture(textureId);
        }
    }
}
