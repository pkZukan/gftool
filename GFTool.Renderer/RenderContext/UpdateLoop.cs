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
        public void Update()
        {
            if (viewport == null) return;

            bool perfEnabled = RenderOptions.EnablePerfHud || RenderOptions.EnablePerfSpikeLog;
            long perfFrameStart = perfEnabled ? Stopwatch.GetTimestamp() : 0;
            float msUpdateAnimation = 0, msGeometry = 0, msGeometryFinishWait = 0, msLighting = 0, msFinal = 0, msGrid = 0, msSkeleton = 0, msTransparent = 0, msOutline = 0, msPresent = 0;
            long allocGeoStart = 0;
            long allocGeoDelta = 0;
            long allocFrameStart = 0;
            long allocFrameDelta = 0;
            int gc0Start = 0;
            int gc1Start = 0;
            int gc2Start = 0;
            int gc0Delta = 0;
            int gc1Delta = 0;
            int gc2Delta = 0;
            long allocAnimStart = 0;
            long allocLightStart = 0;
            long allocFinalStart = 0;
            long allocMiscStart = 0;
            long allocPresentStart = 0;

            PerfCounters.Enabled = perfEnabled;
            PerfCounters.BeginFrame();

            if (perfEnabled)
            {
                try
                {
                    allocFrameStart = GC.GetAllocatedBytesForCurrentThread();
                }
                catch
                {
                    allocFrameStart = 0;
                }

                try
                {
                    gc0Start = GC.CollectionCount(0);
                    gc1Start = GC.CollectionCount(1);
                    gc2Start = GC.CollectionCount(2);
                }
                catch
                {
                    gc0Start = gc1Start = gc2Start = 0;
                }
            }

            //Update VP mat
            camera.Update();

            //Bind viewport
            viewport.MakeCurrent();
            ProcessAsyncGlWork();

            if (perfEnabled)
            {
                long t0 = Stopwatch.GetTimestamp();
                allocAnimStart = GetAllocatedBytesSafe();
                UpdateAnimation();
                msUpdateAnimation = (float)((Stopwatch.GetTimestamp() - t0) * 1000.0 / Stopwatch.Frequency);
                long allocAnimEnd = GetAllocatedBytesSafe();
                lastAllocAnimBytes = allocAnimStart != 0 && allocAnimEnd != 0 ? allocAnimEnd - allocAnimStart : 0;
            }
            else
            {
                UpdateAnimation();
            }

            if (wireframeEnabled)
            {
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
                GL.ClearColor(DefaultBackgroundColor);
                GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
                GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
                GL.Disable(EnableCap.CullFace);
                if (perfEnabled)
                {
                    long t0 = Stopwatch.GetTimestamp();
                    GeometryPass();
                    msGeometry = (float)((Stopwatch.GetTimestamp() - t0) * 1000.0 / Stopwatch.Frequency);
                }
                else
                {
                    GeometryPass();
                }
                GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
                GL.Enable(EnableCap.CullFace);
                Render();
                return;
            }

            //Bind GBuf and clear it
            gbuffer.BindFBO();
            gbuffer.Clear();

            //Various passes
            if (perfEnabled)
            {
                long t0;
                t0 = Stopwatch.GetTimestamp();
                try
                {
                    allocGeoStart = GC.GetAllocatedBytesForCurrentThread();
                }
                catch
                {
                    allocGeoStart = 0;
                }
                GeometryPass();
                msGeometry = (float)((Stopwatch.GetTimestamp() - t0) * 1000.0 / Stopwatch.Frequency);
                if (allocGeoStart != 0)
                {
                    try
                    {
                        allocGeoDelta = GC.GetAllocatedBytesForCurrentThread() - allocGeoStart;
                    }
                    catch
                    {
                        allocGeoDelta = 0;
                    }
                }
                lastAllocGeoBytes = allocGeoDelta;

                if (RenderOptions.EnablePerfSpikeLog && msGeometry >= Math.Max(1.0f, RenderOptions.PerfSpikeThresholdMs))
                {
                    long finish0 = Stopwatch.GetTimestamp();
                    GL.Finish();
                    msGeometryFinishWait = (float)((Stopwatch.GetTimestamp() - finish0) * 1000.0 / Stopwatch.Frequency);
                }

                t0 = Stopwatch.GetTimestamp();
                allocLightStart = GetAllocatedBytesSafe();
                LightingPass();
                msLighting = (float)((Stopwatch.GetTimestamp() - t0) * 1000.0 / Stopwatch.Frequency);
                long allocLightEnd = GetAllocatedBytesSafe();
                lastAllocLightBytes = allocLightStart != 0 && allocLightEnd != 0 ? allocLightEnd - allocLightStart : 0;

                t0 = Stopwatch.GetTimestamp();
                allocFinalStart = GetAllocatedBytesSafe();
                FinalPass();
                msFinal = (float)((Stopwatch.GetTimestamp() - t0) * 1000.0 / Stopwatch.Frequency);
                long allocFinalEnd = GetAllocatedBytesSafe();
                lastAllocFinalBytes = allocFinalStart != 0 && allocFinalEnd != 0 ? allocFinalEnd - allocFinalStart : 0;

                allocMiscStart = GetAllocatedBytesSafe();
                t0 = Stopwatch.GetTimestamp();
                GridPass();
                msGrid = (float)((Stopwatch.GetTimestamp() - t0) * 1000.0 / Stopwatch.Frequency);
                long allocAfterGrid = GetAllocatedBytesSafe();
                lastAllocGridBytes = allocMiscStart != 0 && allocAfterGrid != 0 ? allocAfterGrid - allocMiscStart : 0;

                t0 = Stopwatch.GetTimestamp();
                SkeletonPass();
                msSkeleton = (float)((Stopwatch.GetTimestamp() - t0) * 1000.0 / Stopwatch.Frequency);
                long allocAfterSkeleton = GetAllocatedBytesSafe();
                lastAllocSkeletonBytes = allocAfterGrid != 0 && allocAfterSkeleton != 0 ? allocAfterSkeleton - allocAfterGrid : 0;

                t0 = Stopwatch.GetTimestamp();
                TransparentPass();
                msTransparent = (float)((Stopwatch.GetTimestamp() - t0) * 1000.0 / Stopwatch.Frequency);
                long allocAfterTransparent = GetAllocatedBytesSafe();
                lastAllocTransparentBytes = allocAfterSkeleton != 0 && allocAfterTransparent != 0 ? allocAfterTransparent - allocAfterSkeleton : 0;

                t0 = Stopwatch.GetTimestamp();
                OutlinePass();
                msOutline = (float)((Stopwatch.GetTimestamp() - t0) * 1000.0 / Stopwatch.Frequency);
                long allocMiscEnd = GetAllocatedBytesSafe();
                lastAllocOutlineBytes = allocAfterTransparent != 0 && allocMiscEnd != 0 ? allocMiscEnd - allocAfterTransparent : 0;
                lastAllocMiscBytes = allocMiscStart != 0 && allocMiscEnd != 0 ? allocMiscEnd - allocMiscStart : 0;
            }
            else
            {
                GeometryPass();
                LightingPass();
                FinalPass();
                GridPass();
                SkeletonPass();
                TransparentPass();
                OutlinePass();
            }

            //Render
            gbuffer.BindDefaultFB();
            if (perfEnabled)
            {
                long t0 = Stopwatch.GetTimestamp();
                allocPresentStart = GetAllocatedBytesSafe();
                Render();
                msPresent = (float)((Stopwatch.GetTimestamp() - t0) * 1000.0 / Stopwatch.Frequency);
                long allocPresentEnd = GetAllocatedBytesSafe();
                lastAllocPresentBytes = allocPresentStart != 0 && allocPresentEnd != 0 ? allocPresentEnd - allocPresentStart : 0;
            }
            else
            {
                Render();
            }

            if (perfEnabled)
            {
                float frameMs = (float)((Stopwatch.GetTimestamp() - perfFrameStart) * 1000.0 / Stopwatch.Frequency);
                lastPerfFrame = new PerfFrameTiming(frameMs, msUpdateAnimation, msGeometry, msGeometryFinishWait, msLighting, msFinal, msGrid, msSkeleton, msTransparent, msOutline, msPresent);
                lastPerfStats = new PerfFrameStats(allocGeoDelta, PerfCounters.GetSnapshot());

                if (allocFrameStart != 0)
                {
                    try
                    {
                        allocFrameDelta = GC.GetAllocatedBytesForCurrentThread() - allocFrameStart;
                    }
                    catch
                    {
                        allocFrameDelta = 0;
                    }
                }
                try
                {
                    gc0Delta = GC.CollectionCount(0) - gc0Start;
                    gc1Delta = GC.CollectionCount(1) - gc1Start;
                    gc2Delta = GC.CollectionCount(2) - gc2Start;
                }
                catch
                {
                    gc0Delta = gc1Delta = gc2Delta = 0;
                }

                if (RenderOptions.EnablePerfSpikeLog &&
                    frameMs >= Math.Max(1.0f, RenderOptions.PerfSpikeThresholdMs))
                {
                    long now = Stopwatch.GetTimestamp();
                    // Throttle spikes to avoid log spam if the renderer is consistently slow.
                    if (now - lastSpikeLogTicks > Stopwatch.Frequency / 2)
                    {
                        lastSpikeLogTicks = now;
                        var c = lastPerfStats.Counters;
                        bool isIdleFrame =
                            c.DrawCalls == 0 &&
                            msGeometry < 0.01f &&
                            msLighting < 0.01f &&
                            msFinal < 0.01f &&
                            msTransparent < 0.01f &&
                            msOutline < 0.01f;
                        if (isIdleFrame)
                        {
                            return;
                        }
                        MessageHandler.Instance.AddMessage(
                            MessageType.LOG,
                            $"[Perf] Spike {frameMs:0.0}ms (anim {msUpdateAnimation:0.0} adv {lastAnimAdvanceMs:0.0} apply {lastAnimApplyMs:0.0} models {lastAnimModelCount} " +
                            $"geo {msGeometry:0.0}+finish {msGeometryFinishWait:0.0} alloc {allocGeoDelta / 1024.0:0.0}KB frameAlloc {allocFrameDelta / 1024.0:0.0}KB gc {gc0Delta}/{gc1Delta}/{gc2Delta}) " +
                            $"present {msPresent:0.0}ms drawCalls {c.DrawCalls} tris {c.Triangles} mats {c.MaterialUses} texBinds {c.TextureBinds} skinUploads {c.SkinMatrixUploads}");
                    }
                }
            }
        }

        private void UpdateAnimation()
        {
            if (activeAnimation == null)
            {
                lastAnimAdvanceMs = 0;
                lastAnimApplyMs = 0;
                lastAnimModelCount = 0;
                lastAnimAllocAdvanceBytes = 0;
                lastAnimAllocApplyBytes = 0;
                lastAnimAllocApplyMaxBytes = 0;
                lastAnimAllocApplyMaxModelName = string.Empty;
                lastAnimAllocApplyMaxPoseBytes = 0;
                lastAnimAllocApplyMaxWriteBytes = 0;
                return;
            }

            bool perfEnabled = RenderOptions.EnablePerfHud || RenderOptions.EnablePerfSpikeLog;
            long now = Stopwatch.GetTimestamp();
            long perfStart = perfEnabled ? now : 0;
            long allocStart = (perfEnabled && RenderOptions.EnablePerfSpikeLog) ? GetAllocatedBytesSafe() : 0;
            if (lastAnimationTicks == 0)
            {
                lastAnimationTicks = now;
                lastAnimAdvanceMs = 0;
                lastAnimApplyMs = 0;
                lastAnimModelCount = 0;
                lastAnimAllocAdvanceBytes = 0;
                lastAnimAllocApplyBytes = 0;
                lastAnimAllocApplyMaxBytes = 0;
                lastAnimAllocApplyMaxModelName = string.Empty;
                lastAnimAllocApplyMaxPoseBytes = 0;
                lastAnimAllocApplyMaxWriteBytes = 0;
                return;
            }

            if (animationPaused)
            {
                lastAnimationTicks = now;
                lastAnimAdvanceMs = 0;
                lastAnimApplyMs = 0;
                lastAnimModelCount = 0;
                lastAnimAllocAdvanceBytes = 0;
                lastAnimAllocApplyBytes = 0;
                lastAnimAllocApplyMaxBytes = 0;
                lastAnimAllocApplyMaxModelName = string.Empty;
                lastAnimAllocApplyMaxPoseBytes = 0;
                lastAnimAllocApplyMaxWriteBytes = 0;
                return;
            }

            double deltaSeconds = (now - lastAnimationTicks) / (double)Stopwatch.Frequency;
            lastAnimationTicks = now;
            animationTimeSeconds += deltaSeconds;

            var duration = GetActiveAnimationDurationSeconds();
            if (duration > 0)
            {
                bool looping = loopAnimationOverride || activeAnimation.LoopType == Animation.PlayType.Looped;
                if (looping)
                {
                    animationTimeSeconds %= duration;
                    if (animationTimeSeconds < 0)
                    {
                        animationTimeSeconds += duration;
                    }
                }
                else if (animationTimeSeconds >= duration)
                {
                    // For one-shot animations, snap back to the start and pause once we reach the end.
                    animationTimeSeconds = 0;
                    animationPaused = true;
                    lastAnimationTicks = now;
                    if (perfEnabled)
                    {
                        long beforeApply = Stopwatch.GetTimestamp();
                        lastAnimAdvanceMs = (float)((beforeApply - perfStart) * 1000.0 / Stopwatch.Frequency);
                        long apply0 = Stopwatch.GetTimestamp();
                        ApplyAnimationFrameToScene(0);
                        lastAnimApplyMs = (float)((Stopwatch.GetTimestamp() - apply0) * 1000.0 / Stopwatch.Frequency);
                    }
                    else
                    {
                        ApplyAnimationFrameToScene(0);
                    }
                    return;
                }
            }

            float frame = GetAnimationFrame(activeAnimation, (float)animationTimeSeconds);
            if (perfEnabled)
            {
                long beforeApply = Stopwatch.GetTimestamp();
                lastAnimAdvanceMs = (float)((beforeApply - perfStart) * 1000.0 / Stopwatch.Frequency);
                if (allocStart != 0)
                {
                    long allocBeforeApply = GetAllocatedBytesSafe();
                    lastAnimAllocAdvanceBytes = allocBeforeApply != 0 ? allocBeforeApply - allocStart : 0;
                }
                long apply0 = Stopwatch.GetTimestamp();
                long allocApplyStart = allocStart != 0 ? GetAllocatedBytesSafe() : 0;
                ApplyAnimationFrameToScene(frame);
                lastAnimApplyMs = (float)((Stopwatch.GetTimestamp() - apply0) * 1000.0 / Stopwatch.Frequency);
                if (allocApplyStart != 0)
                {
                    long allocApplyEnd = GetAllocatedBytesSafe();
                    lastAnimAllocApplyBytes = allocApplyEnd != 0 ? allocApplyEnd - allocApplyStart : 0;
                }
            }
            else
            {
                ApplyAnimationFrameToScene(frame);
            }

        }

        private void ApplyAnimationFrameToScene(float frame)
        {
            if (activeAnimation == null)
            {
                return;
            }

            bool probeAlloc = RenderOptions.EnablePerfSpikeLog;
            int modelCount = 0;
            long maxAllocBytes = 0;
            string maxAllocModelName = string.Empty;
            long maxAllocPoseBytes = 0;
            long maxAllocWriteBytes = 0;
            foreach (var model in EnumerateAnimationTargetModels())
            {
                modelCount++;
                long alloc0 = probeAlloc ? GetAllocatedBytesSafe() : 0;
                model.ApplyAnimation(activeAnimation, frame, activeAnimationFallback);
                if (probeAlloc && alloc0 != 0)
                {
                    long alloc1 = GetAllocatedBytesSafe();
                    long delta = alloc1 != 0 ? alloc1 - alloc0 : 0;
                    if (delta > maxAllocBytes)
                    {
                        maxAllocBytes = delta;
                        maxAllocModelName = model.Name ?? string.Empty;
                        maxAllocPoseBytes = model.LastAnimAllocPoseComputeBytes;
                        maxAllocWriteBytes = model.LastAnimAllocWriteBackBytes;
                    }
                }
            }
            lastAnimModelCount = modelCount;
            if (probeAlloc)
            {
                lastAnimAllocApplyMaxBytes = maxAllocBytes;
                lastAnimAllocApplyMaxModelName = maxAllocModelName;
                lastAnimAllocApplyMaxPoseBytes = maxAllocBytes > 0 ? maxAllocPoseBytes : 0;
                lastAnimAllocApplyMaxWriteBytes = maxAllocBytes > 0 ? maxAllocWriteBytes : 0;
            }
        }

        private IEnumerable<Model> EnumerateAnimationTargetModels()
        {
            if (!useAnimationTargets)
            {
                foreach (var c in SceneGraph.Instance.GetRoot().children)
                {
                    if (c is Model model)
                    {
                        yield return model;
                    }
                }
                yield break;
            }

            for (int i = 0; i < animationTargets.Count; i++)
            {
                yield return animationTargets[i];
            }
        }

        private float GetAnimationFrame(Animation animation, float timeSeconds)
        {
            if (!loopAnimationOverride || animation.LoopType == Animation.PlayType.Looped)
            {
                return animation.GetFrame(timeSeconds);
            }

            float frameRate = animation.FrameRate > 0 ? animation.FrameRate : 30f;
            float frame = timeSeconds * frameRate;
            if (animation.FrameCount > 0)
            {
                frame %= animation.FrameCount;
                frame = Math.Clamp(frame, 0f, Math.Max(0f, animation.FrameCount - 1));
            }
            return frame;
        }

    }
}
