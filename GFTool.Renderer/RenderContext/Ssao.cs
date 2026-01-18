using GFTool.Renderer.Core;
using GFTool.Renderer.Core.Graphics;
using GFTool.Renderer.Scene;
using GFTool.Renderer.Scene.GraphicsObjects;
using OpenTK;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System.Collections.Generic;
using System.Drawing;
using System.Diagnostics;
using System.Linq;
using Trinity.Core.Assets;
using GFTool.Renderer.Core;

namespace GFTool.Renderer
{
    public partial class RenderContext : IDisposable
    {
        private void CreateSsaoTargets(int width, int height)
        {
            DeleteSsaoTargets();

            GL.GenFramebuffers(1, out ssaoFbo);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, ssaoFbo);

            ssaoTexture = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, ssaoTexture);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.R16f, width, height, 0, PixelFormat.Red, PixelType.Float, IntPtr.Zero);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, ssaoTexture, 0);

            DrawBuffersEnum[] attachments = { DrawBuffersEnum.ColorAttachment0 };
            GL.DrawBuffers(1, attachments);

            GL.GenFramebuffers(1, out ssaoBlurFbo);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, ssaoBlurFbo);

            ssaoBlurTexture = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, ssaoBlurTexture);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.R16f, width, height, 0, PixelFormat.Red, PixelType.Float, IntPtr.Zero);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, ssaoBlurTexture, 0);

            GL.DrawBuffers(1, attachments);

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }

        private void DeleteSsaoTargets()
        {
            if (ssaoFbo != 0)
            {
                GL.DeleteFramebuffer(ssaoFbo);
                ssaoFbo = 0;
            }
            if (ssaoBlurFbo != 0)
            {
                GL.DeleteFramebuffer(ssaoBlurFbo);
                ssaoBlurFbo = 0;
            }
            if (ssaoTexture != 0)
            {
                GL.DeleteTexture(ssaoTexture);
                ssaoTexture = 0;
            }
            if (ssaoBlurTexture != 0)
            {
                GL.DeleteTexture(ssaoBlurTexture);
                ssaoBlurTexture = 0;
            }
        }

        private void RenderSsao()
        {
            if (gbuffer == null)
                return;

            var ssaoShader = ShaderPool.Instance.GetShader("ssao");
            if (ssaoShader == null)
            {
                ssaoAvailable = false;
                return;
            }
            ssaoShader.Bind();

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, ssaoFbo);
            GL.Clear(ClearBufferMask.ColorBufferBit);

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, gbuffer.GetTexture(GBuffer.GBufferType.GBUFFER_TYPE_NORMAL));
            ssaoShader.SetInt("normalTexture", 0);

            GL.ActiveTexture(TextureUnit.Texture1);
            GL.BindTexture(TextureTarget.Texture2D, gbuffer.DepthTexture);
            ssaoShader.SetInt("depthTexture", 1);

            ssaoShader.SetVector2("texelSize", new Vector2(1.0f / gbuffer.Width, 1.0f / gbuffer.Height));
            ssaoShader.SetFloat("radius", 6.0f);
            ssaoShader.SetFloat("bias", 0.02f);
            ssaoShader.SetFloat("nearPlane", camera.NearPlane);
            ssaoShader.SetFloat("farPlane", camera.FarPlane);

            gbuffer.RenderFullscreenQuad();

            ssaoShader.Unbind();

            var blurShader = ShaderPool.Instance.GetShader("ssao_blur");
            if (blurShader == null)
            {
                ssaoShader.Unbind();
                ssaoAvailable = false;
                return;
            }
            blurShader.Bind();

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, ssaoBlurFbo);
            GL.Clear(ClearBufferMask.ColorBufferBit);

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, ssaoTexture);
            blurShader.SetInt("ssaoTexture", 0);
            blurShader.SetVector2("texelSize", new Vector2(1.0f / gbuffer.Width, 1.0f / gbuffer.Height));

            gbuffer.RenderFullscreenQuad();

            blurShader.Unbind();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            ssaoAvailable = true;
        }
    }
}
