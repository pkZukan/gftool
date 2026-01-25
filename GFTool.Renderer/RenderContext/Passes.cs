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
        private void GeometryPass()
        {
            RenderOptions.TransparentPass = false;
            //TODO: Traverse scene and only draw geometry (eventually)
            bool probeAlloc = RenderOptions.EnablePerfSpikeLog;
            long allocModelsSum = 0;
            long allocMax = 0;
            string allocMaxName = string.Empty;
            foreach (var c in SceneGraph.Instance.GetRoot().children)
            {
                if (c is Model model)
                {
                    long alloc0 = probeAlloc ? GetAllocatedBytesSafe() : 0;
                    model.Draw(camera.viewMat, camera.projMat);
                    if (probeAlloc && alloc0 != 0)
                    {
                        long alloc1 = GetAllocatedBytesSafe();
                        long delta = alloc1 != 0 ? alloc1 - alloc0 : 0;
                        allocModelsSum += delta;
                        if (delta > allocMax)
                        {
                            allocMax = delta;
                            allocMaxName = model.Name ?? string.Empty;
                        }
                    }
                }
            }
            if (probeAlloc)
            {
                lastAllocGeoModelsBytes = allocModelsSum;
                lastAllocGeoMaxModelBytes = allocMax;
                lastAllocGeoMaxModelName = allocMaxName;
            }
        }

        private void LightingPass()
        {
            RenderSsao();
        }

        private void FinalPass()
        {
            gbuffer.Draw(ssaoBlurTexture, ssaoAvailable, camera.NearPlane, camera.FarPlane, camera.viewMat, camera.projMat, camera.Transform.Position);
        }

        private void TransparentPass()
        {
            RenderOptions.TransparentPass = true;
            Material.ResetTransparentBlendStateCache();
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

    }
}
