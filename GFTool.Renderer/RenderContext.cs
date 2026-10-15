using GFTool.Renderer.Core;
using GFTool.Renderer.Core.Graphics;
using GFTool.Renderer.Scene;
using GFTool.Renderer.Scene.GraphicsObjects;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System.Diagnostics;

namespace GFTool.Renderer
{
    public class RenderContext : IDisposable
    {
        private readonly RenderSurface viewport;
        private readonly List<Model> models = new();
        private readonly Grid grid = new();
        public IReadOnlyList<Model> Models => models.AsReadOnly();
        private int Width, Height;

        GBuffer gbuffer;
        private Camera camera;
        private bool wireframeEnabled = false;
        private int ssaoFbo;
        private int ssaoBlurFbo;
        private int ssaoTexture;
        private int ssaoBlurTexture;
        private bool ssaoAvailable;
        private Animation? activeAnimation;
        private double animationTimeSeconds;
        private long lastAnimationTicks;
        private readonly Dictionary<string, Model> sharedModels = new(StringComparer.OrdinalIgnoreCase);
        public int SharedModelResourceCount => sharedModels.Count;


        public RenderContext(RenderSurface surface, int width, int height)
        {
            Width = width;
            Height = height;
            viewport = surface;
            viewport.MakeCurrent();

            RenderOptions.EnableNormalMaps = true;
            RenderOptions.EnableAO = true;
            RenderOptions.EnableVertexColors = false;
            RenderOptions.FlipNormalY = false;
            RenderOptions.ReconstructNormalZ = false;

            // Create viewport resources.
            camera = new Camera(Width, Height);
            grid.Setup();

            GL.Enable(EnableCap.DepthTest);
            GL.ClearDepth(1.0f);
            GL.DepthFunc(DepthFunction.Lequal);
            GL.ClearColor(0.5f, 0.5f, 0.5f, 1f);

            //Set viewport size
            Resize(Width, Height);
        }

        //Render loop
        public void Update()
        {
            if (viewport == null) return;

            //Update VP mat
            camera.Update();

            //Bind viewport
            viewport.MakeCurrent();

            UpdateAnimation();

            if (wireframeEnabled)
            {
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
                GL.ClearColor(0.5f, 0.5f, 0.5f, 1f);
                GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
                GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
                GL.Disable(EnableCap.CullFace);
                GeometryPass();
                GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
                GL.Enable(EnableCap.CullFace);
                Render();
                return;
            }

            //Bind GBuf and clear it
            gbuffer.BindFBO();
            gbuffer.Clear();

            //Various passes
            GeometryPass();
            LightingPass();
            FinalPass();
            GridPass();
            SkeletonPass();
            TransparentPass();
            OutlinePass();

            //Render
            gbuffer.BindDefaultFB();
            Render();
        }

        private void UpdateAnimation()
        {
            if (activeAnimation == null)
            {
                return;
            }

            long now = Stopwatch.GetTimestamp();
            if (lastAnimationTicks == 0)
            {
                lastAnimationTicks = now;
                return;
            }

            double deltaSeconds = (now - lastAnimationTicks) / (double)Stopwatch.Frequency;
            lastAnimationTicks = now;
            animationTimeSeconds += deltaSeconds;

            float frame = activeAnimation.GetFrame((float)animationTimeSeconds);
            foreach (var c in models)
            {
                if (c is Model model)
                {
                    model.ApplyAnimation(activeAnimation, frame);
                }
            }

        }

        private void GeometryPass()
        {
            RenderOptions.TransparentPass = false;
            foreach (var c in models)
            {
                if (c is Model model)
                {
                    model.Draw(camera.viewMat, camera.projMat);
                }
            }
        }

        private void LightingPass()
        {
            RenderSsao();
        }

        private void FinalPass()
        {
            gbuffer.Draw(ssaoBlurTexture, ssaoAvailable);
        }

        private void TransparentPass()
        {
            RenderOptions.TransparentPass = true;
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            GL.DepthMask(false);

            var transparentModels = models
                .Where(model => model.IsVisible && model.HasTransparentMaterials)
                .OrderByDescending(model => model.TryGetWorldBounds(out var min, out var max)
                    ? (((min + max) * 0.5f) - camera.Transform.Position).LengthSquared : 0);
            foreach (var model in transparentModels)
            {
                model.Draw(camera.viewMat, camera.projMat);
            }

            GL.DepthMask(true);
            GL.Disable(EnableCap.Blend);
            RenderOptions.TransparentPass = false;
        }

        private void SkeletonPass()
        {
            if (!RenderOptions.ShowSkeleton)
            {
                return;
            }

            GL.Disable(EnableCap.DepthTest);
            GL.Disable(EnableCap.CullFace);

            foreach (var c in models)
            {
                if (c is Model model)
                {
                    model.DrawSkeleton(camera.viewMat, camera.projMat);
                }
            }

            GL.Enable(EnableCap.CullFace);
            GL.Enable(EnableCap.DepthTest);
        }

        private void GridPass()
        {
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            GL.Enable(EnableCap.DepthTest);
            GL.DepthMask(true);

            grid.Draw(camera.viewMat, camera.projMat);
        }

        private void OutlinePass()
        {
            RenderOptions.OutlinePass = true;
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            GL.DepthMask(false);
            GL.Enable(EnableCap.DepthTest);
            GL.DepthFunc(DepthFunction.Lequal);

            foreach (var c in models)
            {
                if (c is Model model)
                {
                    model.Draw(camera.viewMat, camera.projMat);
                }
            }

            GL.DepthFunc(DepthFunction.Lequal);
            GL.DepthMask(true);
            RenderOptions.OutlinePass = false;
        }

        public void MoveCamera(float forward, float up, float right, float deltaSeconds)
        {
            if (deltaSeconds > 0f) camera.ApplyMovement(forward, up, right, deltaSeconds);
        }

        public void RotateCamera(float dx, float dy)
        {
            camera.ApplyRotationalDelta(dx, dy);
        }

        public void PanCamera(float dx, float dy)
        {
            camera.ApplyPan(dx, dy);
        }

        public void DollyCamera(float delta)
        {
            camera.ApplyDolly(delta);
        }

        public Model AddModel(string file, bool loadAllLods = false, string? assetRoot = null, bool reuseResources = false)
        {
            viewport.MakeCurrent();
            string? key = reuseResources ? $"{Path.GetFullPath(file)}\0{loadAllLods}\0{assetRoot}" : null;
            if (key != null && sharedModels.TryGetValue(key, out var cached) && !cached.ResourcesReleased)
            {
                var instance = cached.CreateInstance();
                models.Add(instance);
                return instance;
            }
            var mdl = new Model(file, loadAllLods, assetRoot);
            try
            {
                // Set up GPU resources before exposing the model to render passes.
                mdl.Setup();
                models.Add(mdl);
                if (key != null) { mdl.SharedResourceKey = key; sharedModels[key] = mdl; }
                return mdl;
            }
            catch { mdl.Dispose(); throw; }
        }

        public void RemoveModel(Model mdl)
        {
            if (models.Contains(mdl))
            {
                viewport.MakeCurrent();
                models.Remove(mdl);
                mdl.Dispose();
                if (mdl.SharedResourceKey != null && mdl.ResourcesReleased) sharedModels.Remove(mdl.SharedResourceKey);
            }
        }

        public void ClearModels()
        {
            StopAnimation();
            foreach (var model in models.OfType<Model>().ToArray()) RemoveModel(model);
        }

        public bool FrameModels(IEnumerable<Model> models)
        {
            var min = new Vector3(float.PositiveInfinity);
            var max = new Vector3(float.NegativeInfinity);
            bool found = false;
            foreach (var model in models)
            {
                if (!model.TryGetWorldBounds(out var modelMin, out var modelMax)) continue;
                min = Vector3.ComponentMin(min, modelMin);
                max = Vector3.ComponentMax(max, modelMax);
                found = true;
            }
            if (found) camera.FrameBounds(min, max);
            return found;
        }

        public void PlayAnimation(Animation animation)
        {
            activeAnimation = animation;
            animationTimeSeconds = 0;
            lastAnimationTicks = 0;
            if (MessageHandler.Instance.DebugLogsEnabled)
            {
                MessageHandler.Instance.AddMessage(MessageType.LOG, $"[Anim] Play '{animation.Name}' frames={animation.FrameCount} fps={animation.FrameRate} tracks={animation.TrackCount}");

                foreach (var c in models)
                {
                    if (c is Model model)
                    {
                        var armature = model.Armature;
                        if (armature == null)
                        {
                            MessageHandler.Instance.AddMessage(MessageType.LOG, $"[Anim] Model '{model.Name}': no armature");
                            continue;
                        }

                        int matches = 0;
                        foreach (var bone in armature.Bones)
                        {
                            if (animation.HasTrack(bone.Name))
                            {
                                matches++;
                            }
                        }

                        MessageHandler.Instance.AddMessage(MessageType.LOG, $"[Anim] Model '{model.Name}': bones={armature.Bones.Count} trackMatches={matches}");
                    }
                }
            }
        }

        public void StopAnimation()
        {
            activeAnimation = null;
            animationTimeSeconds = 0;
            lastAnimationTicks = 0;
            foreach (var c in models)
            {
                if (c is Model model)
                {
                    model.ResetPose();
                }
            }
        }

        public Transform GetCameraTransform()
        {
            return camera.Transform;
        }

        public void SetGBufferDisplayMode(GBuffer.DisplayType displayType)
        {
            gbuffer.DisplayMode = displayType;
            RenderOptions.LegacyMode = displayType == GBuffer.DisplayType.DISPLAY_LEGACY;
        }

        public void SetWireframe(bool b)
        {
            wireframeEnabled = b;
        }

        public void SetNormalMapsEnabled(bool enabled)
        {
            RenderOptions.EnableNormalMaps = enabled;
        }

        public void SetAOEnabled(bool enabled)
        {
            RenderOptions.EnableAO = enabled;
        }

        public void SetVertexColorsEnabled(bool enabled)
        {
            RenderOptions.EnableVertexColors = enabled;
        }

        public void SetFlipNormalY(bool enabled)
        {
            RenderOptions.FlipNormalY = enabled;
        }

        public void SetReconstructNormalZ(bool enabled)
        {
            RenderOptions.ReconstructNormalZ = enabled;
        }

        public void SetSkeletonVisible(bool enabled)
        {
            RenderOptions.ShowSkeleton = enabled;
        }

        private void Render()
        {
            viewport.SwapBuffers();
        }

        public void Resize(int width, int height)
        {
            viewport.MakeCurrent();
            gbuffer?.Dispose();
            //Create GBuffer
            gbuffer = new GBuffer(width, height);
            CreateSsaoTargets(width, height);

            GL.Viewport(0, 0, width, height);
            camera?.Resize(width, height);
        }

        public void Dispose()
        {
            viewport.MakeCurrent();
            ClearModels();
            grid.Dispose();
            gbuffer.Dispose();
            DeleteSsaoTargets();
        }

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
