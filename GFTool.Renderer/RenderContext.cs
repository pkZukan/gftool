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
        private static readonly Color DefaultBackgroundColor = Color.FromArgb(45, 45, 45);

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
        private Animation? activeAnimationFallback;
        private double animationTimeSeconds;
        private long lastAnimationTicks;
        private bool animationPaused;
        private bool loopAnimationOverride;
        private readonly List<Model> animationTargets = new List<Model>();
        private bool useAnimationTargets;

        public bool AllowUserInput = true;

        public readonly struct PerfFrameTiming
        {
            public readonly float FrameMs;
            public readonly float UpdateAnimationMs;
            public readonly float GeometryMs;
            public readonly float GeometryFinishWaitMs;
            public readonly float LightingMs;
            public readonly float FinalMs;
            public readonly float GridMs;
            public readonly float SkeletonMs;
            public readonly float TransparentMs;
            public readonly float OutlineMs;
            public readonly float PresentMs;

            public PerfFrameTiming(
                float frameMs,
                float updateAnimationMs,
                float geometryMs,
                float geometryFinishWaitMs,
                float lightingMs,
                float finalMs,
                float gridMs,
                float skeletonMs,
                float transparentMs,
                float outlineMs,
                float presentMs)
            {
                FrameMs = frameMs;
                UpdateAnimationMs = updateAnimationMs;
                GeometryMs = geometryMs;
                GeometryFinishWaitMs = geometryFinishWaitMs;
                LightingMs = lightingMs;
                FinalMs = finalMs;
                GridMs = gridMs;
                SkeletonMs = skeletonMs;
                TransparentMs = transparentMs;
                OutlineMs = outlineMs;
                PresentMs = presentMs;
            }
        }

        public readonly struct PerfFrameStats
        {
            public readonly long GeometryAllocBytes;
            public readonly PerfCounters.Snapshot Counters;

            public PerfFrameStats(long geometryAllocBytes, PerfCounters.Snapshot counters)
            {
                GeometryAllocBytes = geometryAllocBytes;
                Counters = counters;
            }
        }

        private PerfFrameTiming lastPerfFrame;
        public PerfFrameTiming LastPerfFrame => lastPerfFrame;
        private PerfFrameStats lastPerfStats;
        public PerfFrameStats LastPerfStats => lastPerfStats;

        private long lastSpikeLogTicks;
        private float lastAnimAdvanceMs;
        private float lastAnimApplyMs;
        private int lastAnimModelCount;
        private long lastAllocAnimBytes;
        private long lastAllocGeoBytes;
        private long lastAllocLightBytes;
        private long lastAllocFinalBytes;
        private long lastAllocMiscBytes;
        private long lastAllocPresentBytes;
        private long lastAllocGridBytes;
        private long lastAllocSkeletonBytes;
        private long lastAllocTransparentBytes;
        private long lastAllocOutlineBytes;
        private long lastAllocGeoModelsBytes;
        private long lastAllocGeoMaxModelBytes;
        private string lastAllocGeoMaxModelName = string.Empty;
        private long lastAnimAllocAdvanceBytes;
        private long lastAnimAllocApplyBytes;
        private long lastAnimAllocApplyMaxBytes;
        private string lastAnimAllocApplyMaxModelName = string.Empty;
        private long lastAnimAllocApplyMaxPoseBytes;
        private long lastAnimAllocApplyMaxWriteBytes;

        private static long GetAllocatedBytesSafe()
        {
            try
            {
                return GC.GetAllocatedBytesForCurrentThread();
            }
            catch
            {
                return 0;
            }
        }

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
            GL.ClearColor(DefaultBackgroundColor);

            //Set viewport size
            Resize(Width, Height);
        }

        //Render loop
    }
}
