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

        public void FocusCamera(Vector3 target, float? distance = null)
        {
            camera.SetOrbitTarget(target, distance);

            if (distance.HasValue)
            {
                // Many Trinity scenes are authored at large world scales; keep far plane large enough
                // so focused content isn't clipped.
                camera.FarPlane = Math.Max(camera.FarPlane, distance.Value * 10.0f);
                camera.UpdateProjMatrix();
            }
        }

        public void SetCameraClipPlanes(float nearPlane, float farPlane)
        {
            camera.NearPlane = Math.Max(0.0001f, nearPlane);
            camera.FarPlane = Math.Max(camera.NearPlane + 0.001f, farPlane);
            camera.UpdateProjMatrix();
        }

    }
}
