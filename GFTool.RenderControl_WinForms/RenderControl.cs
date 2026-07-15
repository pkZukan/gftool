using GFTool.Renderer.Core;
using GFTool.Renderer;
using System.ComponentModel;
using OpenTK.GLControl;
using Timer = System.Windows.Forms.Timer;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Reflection;

namespace GFTool.RenderControl_WinForms
{
    public partial class RenderControl : GLControl
    {
        public RenderContext renderer { get; private set; } = null!;
        public bool RendererInitialized => renderer != null;
        public Timer timer { get; private set; }
        public event EventHandler? RendererReady;

        Point prevMousePos;
        private bool useIdleRenderLoop = false;
        private bool isDragging = false;
        private bool isRendering = false;
        private bool skipNextDelta = false;
        private bool renderErrorShown = false;
        private bool contextErrorLogged = false;
        private long lastUpdateTicks = 0;
        private float pendingRotateX = 0f;
        private float pendingRotateY = 0f;
        private float pendingPanX = 0f;
        private float pendingPanY = 0f;
        private float pendingDolly = 0f;

        public RenderControl()
        {
            timer = new Timer();
            timer.Tick += movementTimer_Tick;
            timer.Interval = 10;
            timer.Enabled = true;
        }

        private bool IsDesignMode()
        {
            return DesignMode || LicenseManager.UsageMode == LicenseUsageMode.Designtime;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            if (!IsDesignMode())
            {
                try
                {
                    if (!TryMakeCurrent())
                    {
                        throw new InvalidOperationException("OpenGL context is not ready.");
                    }

                    renderer = new RenderContext(Context, Width, Height);
                }
                catch (Exception ex)
                {
                    timer.Enabled = false;
                    MessageHandler.Instance.AddMessage(MessageType.ERROR, $"Renderer failed to initialize: {ex.Message}");
                    MessageBox.Show(
                        $"Renderer failed to initialize:\n\n{ex.Message}\n\nIf this is a release build, make sure the Shaders folder is next to TrinityModelViewer.exe and your GPU driver supports OpenGL 3.3.",
                        "Trinity Model Viewer",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return;
                }
            }

            if (!IsDesignMode())
            {
                RendererReady?.Invoke(this, EventArgs.Empty);
                TryEnableVsync();
                useIdleRenderLoop = false;
                timer.Enabled = true;
            }
        }

        protected override void OnMouseHover(EventArgs e)
        {
            base.OnMouseHover(e);

            Focus();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (IsDesignMode() || useIdleRenderLoop || renderer == null || isRendering || !CanUseContext())
            {
                return;
            }

            try
            {
                isRendering = true;
                if (!TryMakeCurrent())
                {
                    return;
                }

                renderer.Update();
            }
            catch (Exception ex)
            {
                HandleRenderError(ex);
            }
            finally
            {
                isRendering = false;
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (IsDesignMode() || renderer == null || isRendering || !CanUseContext())
            {
                return;
            }

            try
            {
                if (!TryMakeCurrent())
                {
                    return;
                }

                renderer.Resize(Math.Max(1, Width), Math.Max(1, Height));
                Invalidate();
            }
            catch (Exception ex)
            {
                HandleRenderError(ex);
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (IsDesignMode()) return;

            if (!isDragging) return;
            if (!Focused) return;

            Point mousePos = e.Location;

            if (skipNextDelta)
            {
                prevMousePos = mousePos;
                skipNextDelta = false;
                return;
            }

            if (mousePos == prevMousePos) return;

            float deltaX = (mousePos.X - prevMousePos.X) / (float)Math.Max(1, Width);
            float deltaY = (mousePos.Y - prevMousePos.Y) / (float)Math.Max(1, Height);

            if (Math.Abs(deltaX) > 0.1f || Math.Abs(deltaY) > 0.1f)
            {
                prevMousePos = mousePos;
                return;
            }

            prevMousePos = mousePos;
            if ((e.Button & MouseButtons.Right) != 0)
            {
                if ((Control.ModifierKeys & Keys.Control) != 0)
                {
                    pendingDolly += -deltaY;
                }
                else
                {
                    pendingRotateX += deltaX;
                    pendingRotateY += deltaY;
                }
            }
            else if ((e.Button & MouseButtons.Left) != 0)
            {
                pendingPanX += deltaX;
                pendingPanY += -deltaY;
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (IsDesignMode()) return;
            isDragging = true;
            skipNextDelta = true;
            prevMousePos = e.Location;
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (IsDesignMode()) return;
            isDragging = false;
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (IsDesignMode()) return;
            isDragging = false;
        }

        protected override void OnLostFocus(EventArgs e)
        {
            base.OnLostFocus(e);
            if (IsDesignMode()) return;
            isDragging = false;
        }

        private void movementTimer_Tick(object sender, EventArgs e)
        {
            if (!IsDesignMode() && renderer != null && !renderErrorShown && CanUseContext())
            {
                float deltaSeconds = GetDeltaSeconds();
                renderer.UpdateMovementControls(deltaSeconds * GetSpeedMultiplier());
                ApplyPendingCameraInput();
                Invalidate();
            }
        }

        private void RenderLoop(object? sender, EventArgs e)
        {
            if (IsDesignMode() || renderer == null || isRendering || !CanUseContext()) return;

            while (IsApplicationIdle())
            {
                float deltaSeconds = GetDeltaSeconds();
                renderer.UpdateMovementControls(deltaSeconds * GetSpeedMultiplier());

                ApplyPendingCameraInput();
                try
                {
                    isRendering = true;
                    if (!TryMakeCurrent())
                    {
                        return;
                    }

                    renderer.Update();
                }
                catch (Exception ex)
                {
                    HandleRenderError(ex);
                    return;
                }
                finally
                {
                    isRendering = false;
                }
            }
        }

        private void ApplyPendingCameraInput()
        {
            if (renderer == null) return;

            float speedMultiplier = GetSpeedMultiplier();
            if (pendingDolly != 0f)
            {
                renderer.DollyCamera(pendingDolly * speedMultiplier);
                pendingDolly = 0f;
            }

            if (pendingRotateX != 0f || pendingRotateY != 0f)
            {
                renderer.RotateCamera(pendingRotateX * speedMultiplier, pendingRotateY * speedMultiplier);
                pendingRotateX = 0f;
                pendingRotateY = 0f;
            }

            if (pendingPanX != 0f || pendingPanY != 0f)
            {
                renderer.PanCamera(pendingPanX * speedMultiplier, pendingPanY * speedMultiplier);
                pendingPanX = 0f;
                pendingPanY = 0f;
            }
        }

        private static float GetSpeedMultiplier()
        {
            var modifiers = Control.ModifierKeys;
            if ((modifiers & Keys.Control) != 0)
            {
                return 2.0f;
            }
            if ((modifiers & Keys.Shift) != 0)
            {
                return 0.2f;
            }
            return 1.0f;
        }

        private float GetDeltaSeconds()
        {
            long now = Stopwatch.GetTimestamp();
            if (lastUpdateTicks == 0)
            {
                lastUpdateTicks = now;
                return 0f;
            }

            long deltaTicks = now - lastUpdateTicks;
            lastUpdateTicks = now;
            float deltaSeconds = (float)deltaTicks / Stopwatch.Frequency;
            return Math.Clamp(deltaSeconds, 0f, 0.1f);
        }

        private void TryEnableVsync()
        {
            try
            {
                var type = GetType();
                var vsyncProp = type.GetProperty("VSync", BindingFlags.Instance | BindingFlags.Public);
                if (vsyncProp != null && vsyncProp.CanWrite)
                {
                    vsyncProp.SetValue(this, true);
                }

                var swapProp = type.GetProperty("SwapInterval", BindingFlags.Instance | BindingFlags.Public);
                if (swapProp != null && swapProp.CanWrite)
                {
                    swapProp.SetValue(this, 1);
                }

                var contextProp = type.GetProperty("Context", BindingFlags.Instance | BindingFlags.Public);
                var context = contextProp?.GetValue(this);
                if (context != null)
                {
                    var ctxType = context.GetType();
                    var ctxSwap = ctxType.GetProperty("SwapInterval", BindingFlags.Instance | BindingFlags.Public);
                    if (ctxSwap != null && ctxSwap.CanWrite)
                    {
                        ctxSwap.SetValue(context, 1);
                    }
                }
            }
            catch
            {
                // Ignore if not supported by this GLControl build.
            }
        }

        private bool IsApplicationIdle()
        {
            return !PeekMessage(out _, IntPtr.Zero, 0, 0, 0);
        }

        private bool CanUseContext()
        {
            return IsHandleCreated &&
                   !IsDisposed &&
                   !Disposing &&
                   Width > 0 &&
                   Height > 0 &&
                   Context != null;
        }

        private bool TryMakeCurrent()
        {
            if (!CanUseContext())
            {
                return false;
            }

            try
            {
                MakeCurrent();
                return true;
            }
            catch (Exception ex) when (IsContextCurrentError(ex))
            {
                LogContextErrorOnce(ex);
                return false;
            }
        }

        private void HandleRenderError(Exception ex)
        {
            if (IsContextCurrentError(ex))
            {
                LogContextErrorOnce(ex);
                return;
            }

            timer.Enabled = false;
            MessageHandler.Instance.AddMessage(MessageType.ERROR, $"Renderer failed while drawing: {ex.Message}");

            if (renderErrorShown)
            {
                return;
            }

            renderErrorShown = true;
            MessageBox.Show(
                $"Renderer failed while drawing:\n\n{ex.Message}\n\nRendering has been paused instead of closing the app.",
                "Trinity Model Viewer",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }

        private static bool IsContextCurrentError(Exception ex)
        {
            var message = ex.Message ?? string.Empty;
            return message.Contains("Failed to make context current", StringComparison.OrdinalIgnoreCase) ||
                   message.Contains("SwapBuffers", StringComparison.OrdinalIgnoreCase);
        }

        private void LogContextErrorOnce(Exception ex)
        {
            if (contextErrorLogged)
            {
                return;
            }

            contextErrorLogged = true;
            MessageHandler.Instance.AddMessage(MessageType.WARNING, $"Skipped a frame because the OpenGL context was temporarily unavailable: {ex.Message}");
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct NativeMessage
        {
            public IntPtr Handle;
            public uint Msg;
            public IntPtr WParam;
            public IntPtr LParam;
            public uint Time;
            public System.Drawing.Point P;
        }

        [DllImport("user32.dll")]
        private static extern bool PeekMessage(out NativeMessage lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax, uint wRemoveMsg);
    }
}
