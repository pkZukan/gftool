using GFTool.Renderer.Core;
using OpenTK.Compute.OpenCL;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System.Drawing;

namespace GFTool.Renderer.Core.Graphics
{
    public class GBuffer : IDisposable
    {
        public enum GBufferType
        {
            GBUFFER_TYPE_ALBEDO,
            GBUFFER_TYPE_NORMAL,
            GBUFFER_TYPE_SPECULAR,
            GBUFFER_TYPE_AO,
            GBUFFER_MAX
        };

        public enum DisplayType
        {
            DISPLAY_ALL,
            DISPLAY_ALBEDO,
            DISPLAY_NORMAL,
            DISPLAY_SPECULAR,
            DISPLAY_AO,
            DISPLAY_DEPTH,
            DISPLAY_TOON,
            DISPLAY_LEGACY
        }
        private int fbo = 0;
        private int[] textures;
        private int depthTex;

        private int screenGeom;
        private int quadVBO;

        private int width, height;

        public DisplayType DisplayMode { get; set; } = DisplayType.DISPLAY_ALL;

        public int DepthTexture => depthTex;
        public int Width => width;
        public int Height => height;

        public int GetTexture(GBufferType type)
        {
            return textures[(int)type];
        }

        public GBuffer(int width, int height)
        {
            this.width = width;
            this.height = height;

            //Gen FBO
            GL.GenFramebuffers(1, out fbo);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, fbo);

            // Gen GBuffer textures.
            // Use RGBA so we can pack additional material properties (roughness/metallic/etc) like the game’s deferred path.
            textures = new int[(int)GBufferType.GBUFFER_MAX];
            GL.GenTextures(textures.Length, textures);
            for (int i = 0; i < textures.Length; i++)
            {
                GL.BindTexture(TextureTarget.Texture2D, textures[i]);
                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba16f, width, height, 0, PixelFormat.Rgba, PixelType.HalfFloat, IntPtr.Zero);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
                GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0 + i, TextureTarget.Texture2D, textures[i], 0);
            }

            // Depth texture for reliable sampling and blitting
            GL.GenTextures(1, out depthTex);
            GL.BindTexture(TextureTarget.Texture2D, depthTex);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.DepthComponent24, width, height, 0, PixelFormat.DepthComponent, PixelType.Float, IntPtr.Zero);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, TextureTarget.Texture2D, depthTex, 0);

            if (GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != FramebufferErrorCode.FramebufferComplete)
            {
                throw new Exception("Framebuffer is not complete: " + GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer).ToString());
            }

            //Tell opengl which attachments are used for the framebuffer
            DrawBuffersEnum[] attachments = {
                DrawBuffersEnum.ColorAttachment0,
                DrawBuffersEnum.ColorAttachment1,
                DrawBuffersEnum.ColorAttachment2,
                DrawBuffersEnum.ColorAttachment3
            };
            GL.DrawBuffers(attachments.Length, attachments);

            BindDefaultFB();
            CreateScreenQuad();
        }

        public void BindFBO()
        {
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, fbo);
            GL.ClearColor(Color.FromArgb(45, 45, 45));
        }

        public void BindDefaultFB()
        {
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }

        public void Clear()
        {
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        }

        void CreateScreenQuad()
        {
            // Define vertices for a full screen quad (NDC coordinates)
            float[] vertices = {
                // Positions        // Texture Coords
                -1.0f,  1.0f,  0.0f, 1.0f,  // Top left
                -1.0f, -1.0f,  0.0f, 0.0f,  // Bottom left
                 1.0f, -1.0f,  1.0f, 0.0f,  // Bottom right

                -1.0f,  1.0f,  0.0f, 1.0f,  // Top left
                 1.0f, -1.0f,  1.0f, 0.0f,  // Bottom right
                 1.0f,  1.0f,  1.0f, 1.0f   // Top right
            };

            // Generate and bind a Vertex Array Object (VAO) and Vertex Buffer Object (VBO)
            GL.GenVertexArrays(1, out screenGeom);
            GL.GenBuffers(1, out quadVBO);
            GL.BindVertexArray(screenGeom);
            GL.BindBuffer(BufferTarget.ArrayBuffer, quadVBO);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);

            // Define vertex attribute pointers for position and texture coordinates
            int stride = 4 * sizeof(float);
            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, stride, 0);
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, stride, 2 * sizeof(float));
            GL.EnableVertexAttribArray(1);

            // Draw the quad
            GL.DrawArrays(PrimitiveType.Triangles, 0, 6);

            // Clean up
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindVertexArray(0);
        }


        public void Draw(int ssaoTexture, bool enableSsao, float cameraNear, float cameraFar, Matrix4 view, Matrix4 projection, Vector3 cameraPos)
        {
            // FBO is bound for read and the default framebuffer is bound for write
            GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, fbo);
            GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, 0); //Bind default framebuf for write
            Clear();

            var gbShader = ShaderPool.Instance.GetShader("gbuffer");
            if (gbShader == null)
            {
                MessageHandler.Instance.AddMessage(MessageType.ERROR, "Failed to load 'gbuffer' shader; skipping final pass.");
                return;
            }
            gbShader.Bind();

            // Copy depth buffer over for grid/overlay depth testing
            GL.BlitFramebuffer(0, 0, width, height, 0, 0, width, height, ClearBufferMask.DepthBufferBit, BlitFramebufferFilter.Nearest);

            Matrix4.Invert(view, out var invView);
            Matrix4.Invert(projection, out var invProjection);
            gbShader.SetMatrix4("InvView", invView);
            gbShader.SetMatrix4("InvProjection", invProjection);
            gbShader.SetVector3("CameraPos", cameraPos);
            gbShader.SetVector3("LightDirection", RenderOptions.WorldLightDirection);
            gbShader.SetVector3("LightColor", new Vector3(1.0f, 1.0f, 1.0f));
            gbShader.SetVector3("AmbientColor", new Vector3(0.25f, 0.25f, 0.25f));
            gbShader.SetFloat("LightWrap", RenderOptions.LightWrap);

            //Set frame textures
            string[] frameList = new string[]
            {
                "albedoTexture",
                "normalTexture",
                "specularTexture",
                "aoTexture",
                "ssaoTexture",
                "depthTexture"
            };

            //Attach frames
            int i = 0;
            foreach (var frame in frameList)
            {
                GL.ActiveTexture(TextureUnit.Texture0 + i);
                if (i == (int)GBufferType.GBUFFER_MAX)
                {
                    GL.BindTexture(TextureTarget.Texture2D, ssaoTexture);
                }
                else if (i == (int)GBufferType.GBUFFER_MAX + 1)
                {
                    GL.BindTexture(TextureTarget.Texture2D, depthTex);
                }
                else
                {
                    GL.BindTexture(TextureTarget.Texture2D, textures[i]);
                }
                gbShader.SetInt(frame, i++);
            }

            //Set bools for visibility
            gbShader.SetBool("useAlbedo", DisplayMode == DisplayType.DISPLAY_ALL || DisplayMode == DisplayType.DISPLAY_ALBEDO || DisplayMode == DisplayType.DISPLAY_TOON || DisplayMode == DisplayType.DISPLAY_LEGACY);
            gbShader.SetBool("useNormal", DisplayMode == DisplayType.DISPLAY_ALL || DisplayMode == DisplayType.DISPLAY_NORMAL || DisplayMode == DisplayType.DISPLAY_TOON || DisplayMode == DisplayType.DISPLAY_LEGACY);
            gbShader.SetBool("useSpecular", DisplayMode == DisplayType.DISPLAY_ALL || DisplayMode == DisplayType.DISPLAY_SPECULAR || DisplayMode == DisplayType.DISPLAY_LEGACY);
            gbShader.SetBool("useAO", DisplayMode == DisplayType.DISPLAY_ALL || DisplayMode == DisplayType.DISPLAY_AO || DisplayMode == DisplayType.DISPLAY_TOON || DisplayMode == DisplayType.DISPLAY_LEGACY);
            gbShader.SetBool("useSSAO", enableSsao && DisplayMode == DisplayType.DISPLAY_ALL);
            gbShader.SetBool("useToon", DisplayMode == DisplayType.DISPLAY_TOON);
            gbShader.SetBool("useLegacy", DisplayMode == DisplayType.DISPLAY_LEGACY);
            gbShader.SetBool("useDepth", DisplayMode == DisplayType.DISPLAY_DEPTH);
            gbShader.SetFloat("CameraNear", cameraNear);
            gbShader.SetFloat("CameraFar", cameraFar);

            //Draw screen quad
            GL.Disable(EnableCap.DepthTest);
            GL.DepthMask(false);
            RenderFullscreenQuad();
            GL.DepthMask(true);
            GL.Enable(EnableCap.DepthTest);

            gbShader.Unbind();
        }

        public void RenderFullscreenQuad()
        {
            GL.BindVertexArray(screenGeom);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
            GL.BindVertexArray(0);
        }

        public void Dispose()
        {
            GL.DeleteVertexArray(screenGeom);
            GL.DeleteFramebuffer(fbo);
            GL.DeleteTexture(depthTex);
            foreach (var tex in textures)
                GL.DeleteTexture(tex);
        }
    }
}
