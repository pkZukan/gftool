using BnTxx;
using GFTool.Renderer.Scene.GraphicsObjects;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace GFTool.RenderControl_WinForms;

public static class TexturePreviewExtensions
{
    public static Bitmap? LoadPreviewBitmap(this Texture texture)
    {
        try
        {
            if (!texture.TryGetResolvedSourcePath(out var path)) return null;
            var pixels = BNTX.LoadFromFile(path, Path.GetFileNameWithoutExtension(texture.SourceFile));
            return pixels == null ? null : pixels.ToBitmap();
        }
        catch { return null; }
    }

    public static Bitmap ToBitmap(this TexturePixels pixels)
    {
        var bitmap = new Bitmap(pixels.Width, pixels.Height, PixelFormat.Format32bppArgb);
        try
        {
            var data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            try
            {
                int rowBytes = checked(pixels.Width * 4);
                for (int y = 0; y < pixels.Height; y++)
                    Marshal.Copy(pixels.Bgra, y * rowBytes, IntPtr.Add(data.Scan0, y * data.Stride), rowBytes);
            }
            finally { bitmap.UnlockBits(data); }
            return bitmap;
        }
        catch
        {
            bitmap.Dispose();
            throw;
        }
    }
}
