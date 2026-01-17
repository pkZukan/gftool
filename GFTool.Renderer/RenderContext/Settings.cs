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
            gbuffer.Dispose();
            DeleteSsaoTargets();
        }

    }
}
