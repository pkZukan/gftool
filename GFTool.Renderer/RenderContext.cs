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
using System.Drawing;
using System.Diagnostics;

namespace GFTool.Renderer
{
    public class RenderContext : IDisposable
    {
        private IGraphicsContext viewport = null;
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
        private Animation? activeAdditiveOverlay;
        private Animation? activeMouthOverlay;
        private Animation? activeUpperFaceOverlay;
        private double animationTimeSeconds;
        private double additiveOverlayTimeSeconds;
        private double mouthOverlayTimeSeconds;
        private double upperFaceOverlayTimeSeconds;
        private bool holdMouthOverlayAtFirstFrame;
        private bool loopMouthOverlay;
        private long lastAnimationTicks;

        public bool AllowUserInput = true;

        public RenderContext(IGLFWGraphicsContext ctxt, int width, int height)
        {
            Width = width;
            Height = height;
            viewport = ctxt;

            RenderOptions.EnableNormalMaps = true;
            RenderOptions.EnableAO = true;
            RenderOptions.EnableVertexColors = false;
            RenderOptions.FlipNormalY = false;
            RenderOptions.ReconstructNormalZ = false;

            //Create camera and add to root scene
            camera = new Camera(Width, Height);
            SceneGraph.Instance.GetRoot().AddChild(camera);
            SceneGraph.Instance.GetRoot().AddChild(new Grid());

            GL.Enable(EnableCap.DepthTest);
            GL.ClearDepth(1.0f);
            GL.DepthFunc(DepthFunction.Lequal);
            GL.Enable(EnableCap.CullFace);
            GL.FrontFace(FrontFaceDirection.Ccw);
            GL.CullFace(CullFaceMode.Back);
            GL.ClearColor(Color.Gray);

            //Set viewport size
            Resize(Width, Height);
        }

        //Render loop
        public void Update()
        {
            if (viewport == null) return;

            //Update VP mat
            camera.Update();

            UpdateAnimation();

            if (wireframeEnabled)
            {
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
                GL.ClearColor(Color.Gray);
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
            if (activeAdditiveOverlay != null)
            {
                additiveOverlayTimeSeconds += deltaSeconds;
            }
            if (activeMouthOverlay != null && !holdMouthOverlayAtFirstFrame)
            {
                mouthOverlayTimeSeconds += deltaSeconds;
                if (!loopMouthOverlay && HasAnimationFinished(activeMouthOverlay, mouthOverlayTimeSeconds))
                {
                    mouthOverlayTimeSeconds = GetAnimationEndTime(activeMouthOverlay);
                    DiagnosticLog.Write($"Mouth animation overlay completed and held: name={activeMouthOverlay.Name}");
                }
            }

            if (activeUpperFaceOverlay != null)
            {
                upperFaceOverlayTimeSeconds += deltaSeconds;
                if (HasAnimationFinished(activeUpperFaceOverlay, upperFaceOverlayTimeSeconds))
                {
                    DiagnosticLog.Write($"Upper-face animation overlay completed: name={activeUpperFaceOverlay.Name}");
                    activeUpperFaceOverlay = null;
                    upperFaceOverlayTimeSeconds = 0;
                }
            }

            ApplyAnimationFrame();
        }

        private void ApplyAnimationFrame()
        {
            if (activeAnimation == null)
            {
                return;
            }

            float frame = activeAnimation.GetFrame((float)animationTimeSeconds);
            float additiveOverlayFrame = activeAdditiveOverlay?.GetFrame((float)additiveOverlayTimeSeconds) ?? 0f;
            float mouthOverlayFrame = activeMouthOverlay?.GetFrame((float)mouthOverlayTimeSeconds, loopMouthOverlay) ?? 0f;
            float upperFaceOverlayFrame = activeUpperFaceOverlay?.GetFrame((float)upperFaceOverlayTimeSeconds) ?? 0f;
            foreach (var c in SceneGraph.Instance.GetRoot().children)
            {
                if (c is Model model)
                {
                    model.ApplyAnimation(
                        activeAnimation,
                        frame,
                        activeAdditiveOverlay,
                        additiveOverlayFrame,
                        activeMouthOverlay,
                        mouthOverlayFrame,
                        activeUpperFaceOverlay,
                        upperFaceOverlayFrame);
                }
            }

        }

        private void GeometryPass()
        {
            RenderOptions.TransparentPass = false;
            //TODO: Traverse scene and only draw geometry (eventually)
            foreach (var c in SceneGraph.Instance.GetRoot().children)
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

            foreach (var c in SceneGraph.Instance.GetRoot().children)
            {
                if (c is Model model)
                {
                    model.Draw(camera.viewMat, camera.projMat);
                }
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

            foreach (var c in SceneGraph.Instance.GetRoot().children)
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

            foreach (var c in SceneGraph.Instance.GetRoot().children)
            {
                if (c is Grid grid)
                {
                    grid.Draw(camera.viewMat, camera.projMat);
                }
            }
        }

        private void OutlinePass()
        {
            RenderOptions.OutlinePass = true;
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            GL.DepthMask(false);
            GL.Enable(EnableCap.DepthTest);
            GL.DepthFunc(DepthFunction.Lequal);

            foreach (var c in SceneGraph.Instance.GetRoot().children)
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

        public void UpdateMovementControls(float deltaSeconds)
        {
            if (!AllowUserInput) return;
            if (deltaSeconds <= 0f) return;

            float x = 0;
            float y = 0;
            float z = 0;

            if (KeyboardControls.Forward)
                x = 1.0f;
            else if (KeyboardControls.Backward)
                x = -1.0f;
            if (KeyboardControls.Right)
                z = 1.0f;
            else if (KeyboardControls.Left)
                z = -1.0f;
            if (KeyboardControls.Up)
                y = 1.0f;
            else if (KeyboardControls.Down)
                y = -1.0f;

            camera.ApplyMovement(x, y, z, deltaSeconds);
        }

        public void RotateCamera(float dx, float dy)
        {
            if (!AllowUserInput) return;
            camera.ApplyRotationalDelta(dx, dy);
        }

        public void PanCamera(float dx, float dy)
        {
            if (!AllowUserInput) return;
            camera.ApplyPan(dx, dy);
        }

        public void DollyCamera(float delta)
        {
            if (!AllowUserInput) return;
            camera.ApplyDolly(delta);
        }

        public Model AddSceneModel(string file, bool loadAllLods = false)
        {
            //TODO: Probably figure out how we're adding shit to child nodes (assuming necessary at this level)

            var mdl = new Model(file, loadAllLods);
            SceneGraph.Instance.GetRoot().AddChild(mdl);

            return mdl;
        }

        public void RemoveSceneModel(Model mdl)
        {
            var root = SceneGraph.Instance.GetRoot();
            if (root.children.Contains(mdl))
            {
                root.children.Remove(mdl);
                mdl.Dispose();
            }
        }

        public void ClearScene()
        {
            activeAnimation = null;
            activeAdditiveOverlay = null;
            activeMouthOverlay = null;
            activeUpperFaceOverlay = null;
            animationTimeSeconds = 0;
            additiveOverlayTimeSeconds = 0;
            mouthOverlayTimeSeconds = 0;
            upperFaceOverlayTimeSeconds = 0;
            holdMouthOverlayAtFirstFrame = false;
            loopMouthOverlay = false;
            lastAnimationTicks = 0;
            var root = SceneGraph.Instance.GetRoot();
            var models = root.children.OfType<Model>().ToArray();
            foreach (var model in models)
            {
                root.children.Remove(model);
                model.Dispose();
            }

            Texture.ClearCache();
            ResetModelGraphicsState();
            DiagnosticLog.Write($"Scene cleared: disposedModels={models.Length}, remainingChildren={root.children.Count}, textureCacheEntries={Texture.CacheEntryCount}");
        }

        private static void ResetModelGraphicsState()
        {
            GL.GetInteger(GetPName.MaxCombinedTextureImageUnits, out int textureUnitCount);
            textureUnitCount = Math.Clamp(textureUnitCount, 1, 32);
            for (int unit = 0; unit < textureUnitCount; unit++)
            {
                GL.ActiveTexture(TextureUnit.Texture0 + unit);
                GL.BindTexture(TextureTarget.Texture2D, 0);
                GL.BindTexture(TextureTarget.TextureCubeMap, 0);
            }

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindVertexArray(0);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
            GL.UseProgram(0);
            DiagnosticLog.Write($"OpenGL model state reset: textureUnits={textureUnitCount}");
        }

        public void PlayAnimation(
            Animation animation,
            bool holdFacialOverlayAtFirstFrame = false,
            bool loopFacialOverlay = false)
        {
            bool playAsFacialOverlay = animation.IsFacialOverlay &&
                                       activeAnimation != null &&
                                       !activeAnimation.IsFacialOverlay;
            if (playAsFacialOverlay)
            {
                if (animation.AllowsMouthPoseTracks)
                {
                    activeMouthOverlay = animation;
                    mouthOverlayTimeSeconds = 0;
                    holdMouthOverlayAtFirstFrame = holdFacialOverlayAtFirstFrame;
                    loopMouthOverlay = loopFacialOverlay;
                }

                if (animation.AllowsUpperFacePoseTracks)
                {
                    activeUpperFaceOverlay = animation;
                    upperFaceOverlayTimeSeconds = 0;
                }
            }
            else
            {
                activeAnimation = null;
                activeAdditiveOverlay = null;
                activeMouthOverlay = null;
                activeUpperFaceOverlay = null;
                animationTimeSeconds = 0;
                additiveOverlayTimeSeconds = 0;
                mouthOverlayTimeSeconds = 0;
                upperFaceOverlayTimeSeconds = 0;
                holdMouthOverlayAtFirstFrame = false;
                loopMouthOverlay = false;
                lastAnimationTicks = 0;
                ResetAnimatedModels();

                activeAnimation = animation;
            }
            lastAnimationTicks = 0;
            ApplyAnimationFrame();
            DiagnosticLog.Write(
                $"Animation {(playAsFacialOverlay ? "overlay" : "play")}: name={animation.Name}, " +
                $"frames={animation.FrameCount}, fps={animation.FrameRate}, tracks={animation.TrackCount}, " +
                $"mouthTracks={animation.MouthPoseTrackCount}, activeMouthTracks={animation.ActiveMouthPoseTrackCount}, " +
                $"embeddedMouth={animation.UsesEmbeddedMouthPoseTracks}, " +
                $"zeroEndpointTracks={animation.ZeroEndpointPlaceholderTrackCount}, " +
                $"animatedMiddleTracks={animation.AnimatedBetweenPlaceholderEndpointsTrackCount}, " +
                $"zeroEndpointEncoding={animation.UsesZeroEndpointPlaceholderEncoding}, additive={animation.UsesAdditivePoseEncoding}, " +
                $"mouthLayer={animation.AllowsMouthPoseTracks}, upperFaceLayer={animation.AllowsUpperFacePoseTracks}, " +
                $"holdFirstFrame={holdFacialOverlayAtFirstFrame}, loopFacialOverlay={loopFacialOverlay}");
            if (MessageHandler.Instance.DebugLogsEnabled)
            {
                MessageHandler.Instance.AddMessage(MessageType.LOG, $"[Anim] Play '{animation.Name}' frames={animation.FrameCount} fps={animation.FrameRate} tracks={animation.TrackCount}");
            }

            foreach (var c in SceneGraph.Instance.GetRoot().children)
            {
                if (c is Model model)
                {
                    var armature = model.Armature;
                    if (armature == null)
                    {
                        DiagnosticLog.Write($"Animation play match: animation={animation.Name}, model={model.Name}, no armature");
                        if (MessageHandler.Instance.DebugLogsEnabled)
                        {
                            MessageHandler.Instance.AddMessage(MessageType.LOG, $"[Anim] Model '{model.Name}': no armature");
                        }
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

                    DiagnosticLog.Write($"Animation play match: animation={animation.Name}, model={model.Name}, bones={armature.Bones.Count}, trackMatches={matches}");
                    LogSuppressedFacialPoseTracks(animation, armature);
                    LogFacialTrackSamples(animation, armature);
                    LogAttachmentTrackSamples(animation, armature);
                    if (MessageHandler.Instance.DebugLogsEnabled)
                    {
                        MessageHandler.Instance.AddMessage(MessageType.LOG, $"[Anim] Model '{model.Name}': bones={armature.Bones.Count} trackMatches={matches}");
                    }
                }
            }
        }

        public void PlayAdditiveAnimation(Animation animation)
        {
            if (activeAnimation == null || animation?.UsesAdditivePoseEncoding != true)
            {
                DiagnosticLog.Write(
                    $"Additive animation rejected: name={animation?.Name ?? "<null>"}, " +
                    $"hasBase={activeAnimation != null}, additiveEncoding={animation?.UsesAdditivePoseEncoding == true}");
                return;
            }

            activeAdditiveOverlay = animation;
            additiveOverlayTimeSeconds = 0;
            lastAnimationTicks = 0;
            ApplyAnimationFrame();
            DiagnosticLog.Write(
                $"Additive animation overlay: base={activeAnimation.Name}, additive={animation.Name}, " +
                $"frames={animation.FrameCount}, fps={animation.FrameRate}, tracks={animation.TrackCount}, " +
                $"activeTracks={animation.AnimatedBetweenPlaceholderEndpointsTrackCount}, " +
                $"activeMouthTracks={animation.ActiveMouthPoseTrackCount}");
        }

        private static void LogSuppressedFacialPoseTracks(Animation animation, Armature armature)
        {
            if (!RenderOptions.SuppressLayeredFacialPoseTracks)
            {
                return;
            }

            var suppressed = armature.Bones
                .Where(bone => Animation.IsLayeredFacialPoseBoneName(bone.Name) &&
                               animation.HasTrack(bone.Name) &&
                               !animation.ShouldApplyPoseTrack(bone.Name))
                .Select(bone => bone.Name)
                .Take(24)
                .ToArray();
            if (suppressed.Length == 0)
            {
                return;
            }

            DiagnosticLog.Write(
                $"Layered facial pose tracks suppressed: animation={animation.Name}, count={suppressed.Length}, bones={string.Join(", ", suppressed)}");
        }

        private static void LogFacialTrackSamples(Animation animation, Armature armature)
        {
            if (animation == null || armature == null)
            {
                return;
            }

            var diagnosticBones = armature.Bones
                .Where(bone => IsFacialDiagnosticBone(bone.Name) && animation.HasTrack(bone.Name))
                .Take(24)
                .ToArray();
            if (diagnosticBones.Length == 0 &&
                !ContainsToken(animation.Name, "face") &&
                !ContainsToken(animation.Name, "mouth"))
            {
                return;
            }

            float endFrame = Math.Max(0f, animation.FrameCount > 0 ? animation.FrameCount - 1 : 0f);
            float middleFrame = endFrame * 0.5f;
            int logged = 0;
            foreach (var bone in diagnosticBones)
            {
                LogFacialTrackSample(animation, bone, 0f);
                if (middleFrame > 0f && middleFrame < endFrame)
                {
                    LogFacialTrackSample(animation, bone, middleFrame);
                }
                if (endFrame > 0f)
                {
                    LogFacialTrackSample(animation, bone, endFrame);
                }

                logged++;
            }

            DiagnosticLog.Write(
                $"Facial animation diagnostics: animation={animation.Name}, sampledBones={logged}, " +
                $"frameMiddle={middleFrame}, frameEnd={endFrame}");
        }

        private static void LogFacialTrackSample(Animation animation, Armature.Bone bone, float frame)
        {
            animation.TryGetPose(bone.Name, frame, out var scale, out var rotation, out var translation);
            DiagnosticLog.Write(
                $"  facial track sample: animation={animation.Name}, bone={bone.Name}, frame={frame}, " +
                $"restT={FormatVector3(bone.RestPosition)}, animT={FormatNullableVector3(translation)}, " +
                $"restS={FormatVector3(bone.RestScale)}, animS={FormatNullableVector3(scale)}, " +
                $"restR={FormatQuaternion(bone.RestRotation)}, animR={FormatNullableQuaternion(rotation)}");
        }

        private static void LogAttachmentTrackSamples(Animation animation, Armature armature)
        {
            var bones = armature.Bones
                .Where(bone => IsAttachmentDiagnosticBone(bone.Name) && animation.HasTrack(bone.Name))
                .Take(40)
                .ToArray();
            foreach (var bone in bones)
            {
                animation.TryGetPose(bone.Name, 0f, out var scale, out var rotation, out var translation);
                DiagnosticLog.Write(
                    $"  attachment track sample: animation={animation.Name}, bone={bone.Name}, " +
                    $"restT={FormatVector3(bone.RestPosition)}, animT={FormatNullableVector3(translation)}, " +
                    $"restS={FormatVector3(bone.RestScale)}, animS={FormatNullableVector3(scale)}, " +
                    $"restR={FormatQuaternion(bone.RestRotation)}, animR={FormatNullableQuaternion(rotation)}");
            }

            if (bones.Length > 0)
            {
                DiagnosticLog.Write($"Attachment animation diagnostics: animation={animation.Name}, sampledBones={bones.Length}");
            }
        }

        private static bool IsAttachmentDiagnosticBone(string name)
        {
            return ContainsToken(name, "hair") ||
                   ContainsToken(name, "hat") ||
                   ContainsToken(name, "cloth") ||
                   ContainsToken(name, "skirt") ||
                   ContainsToken(name, "attach") ||
                   ContainsToken(name, "obj");
        }

        private static bool IsFacialDiagnosticBone(string name)
        {
            return Animation.IsLayeredFacialPoseBoneName(name);
        }

        private static bool ContainsToken(string text, string token)
        {
            return !string.IsNullOrWhiteSpace(text) &&
                   text.Contains(token, StringComparison.OrdinalIgnoreCase);
        }

        private static string FormatNullableVector3(OpenTK.Mathematics.Vector3? value)
        {
            return value.HasValue ? FormatVector3(value.Value) : "<none>";
        }

        private static string FormatVector3(OpenTK.Mathematics.Vector3 value)
        {
            return $"({value.X:0.#####}, {value.Y:0.#####}, {value.Z:0.#####})";
        }

        private static string FormatNullableQuaternion(OpenTK.Mathematics.Quaternion? value)
        {
            if (!value.HasValue)
            {
                return "<none>";
            }

            return FormatQuaternion(value.Value);
        }

        private static string FormatQuaternion(OpenTK.Mathematics.Quaternion value)
        {
            return $"({value.X:0.#####}, {value.Y:0.#####}, {value.Z:0.#####}, {value.W:0.#####})";
        }

        public void ApplyMeshVariantVisibility(char preferredVariant, string reason)
        {
            foreach (var c in SceneGraph.Instance.GetRoot().children)
            {
                if (c is Model model)
                {
                    model.ApplyMeshVariantVisibility(preferredVariant, reason);
                }
            }
        }

        public void ApplyMeshVariantVisibility(Animation animation, char fallbackVariant, string reason)
        {
            foreach (var c in SceneGraph.Instance.GetRoot().children)
            {
                if (c is Model model)
                {
                    model.ApplyMeshVariantVisibility(animation.TrackNames, fallbackVariant, reason);
                }
            }
        }

        public void StopAnimation()
        {
            activeAnimation = null;
            activeAdditiveOverlay = null;
            activeMouthOverlay = null;
            activeUpperFaceOverlay = null;
            animationTimeSeconds = 0;
            additiveOverlayTimeSeconds = 0;
            mouthOverlayTimeSeconds = 0;
            upperFaceOverlayTimeSeconds = 0;
            holdMouthOverlayAtFirstFrame = false;
            loopMouthOverlay = false;
            lastAnimationTicks = 0;
            ResetAnimatedModels();
        }

        private static bool HasAnimationFinished(Animation animation, double timeSeconds)
        {
            if (animation.LoopType == Animation.PlayType.Looped || animation.FrameCount <= 1)
            {
                return false;
            }

            double frameRate = animation.FrameRate > 0 ? animation.FrameRate : 30.0;
            return timeSeconds * frameRate >= animation.FrameCount - 1;
        }

        private static double GetAnimationEndTime(Animation animation)
        {
            double frameRate = animation.FrameRate > 0 ? animation.FrameRate : 30.0;
            return animation.FrameCount > 1 ? (animation.FrameCount - 1) / frameRate : 0;
        }

        private static void ResetAnimatedModels()
        {
            foreach (var c in SceneGraph.Instance.GetRoot().children)
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
            //Create GBuffer
            gbuffer = new GBuffer(width, height);
            CreateSsaoTargets(width, height);

            GL.Viewport(0, 0, width, height);
            camera?.Resize(width, height);
        }

        public void Dispose()
        {
            StopAnimation();
            ClearScene();
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
