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
        public RenderContext renderer { get; private set; }
        public Timer timer { get; private set; }
        public event EventHandler? RendererReady;

        Point prevMousePos;
        private bool useIdleRenderLoop = false;
        private bool isDragging = false;
        private bool skipNextDelta = false;
        private long lastUpdateTicks = 0;
        private float pendingRotateX = 0f;
        private float pendingRotateY = 0f;
        private float pendingPanX = 0f;
        private float pendingPanY = 0f;
        private float pendingDolly = 0f;
        private float ctrlSpeedMultiplier = 2.0f;
        private bool vsyncEnabled = false;

        private const float CtrlSpeedMin = 1.0f;
        private const float CtrlSpeedMax = 200.0f;
        private const float CtrlSpeedStepFactor = 1.25f;
        private const float MouseWheelDollyStep = 0.25f;

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
                renderer = new RenderContext(Context, Width, Height);

            if (!IsDesignMode())
            {
                RendererReady?.Invoke(this, EventArgs.Empty);
                // Default to VSync off (can be toggled by the viewer).
                SetVsync(vsyncEnabled);
                useIdleRenderLoop = true;
                timer.Enabled = false;
                Application.Idle += RenderLoop;
            }
        }

        public void SetVsync(bool enabled)
        {
            bool changed = vsyncEnabled != enabled;
            vsyncEnabled = enabled;
            try
            {
                MakeCurrent();
            }
            catch
            {
                // Ignore; swap interval changes are best-effort.
            }

            try
            {
                var type = GetType();
                var vsyncProp = type.GetProperty("VSync", BindingFlags.Instance | BindingFlags.Public);
                if (vsyncProp != null && vsyncProp.CanWrite)
                {
                    vsyncProp.SetValue(this, enabled);
                }

                var swapProp = type.GetProperty("SwapInterval", BindingFlags.Instance | BindingFlags.Public);
                if (swapProp != null && swapProp.CanWrite)
                {
                    swapProp.SetValue(this, enabled ? 1 : 0);
                }

                var contextProp = type.GetProperty("Context", BindingFlags.Instance | BindingFlags.Public);
                var context = contextProp?.GetValue(this);
                if (context != null)
                {
                    var ctxType = context.GetType();
                    var ctxSwap = ctxType.GetProperty("SwapInterval", BindingFlags.Instance | BindingFlags.Public);
                    if (ctxSwap != null && ctxSwap.CanWrite)
                    {
                        ctxSwap.SetValue(context, enabled ? 1 : 0);
                    }
                }

                // Intentionally no log spam here; caller can confirm via perf counters if needed.
            }
            catch
            {
                // Ignore if not supported by this GLControl build.
            }
        }

        private int? TryGetSwapInterval()
        {
            try
            {
                var type = GetType();
                var swapProp = type.GetProperty("SwapInterval", BindingFlags.Instance | BindingFlags.Public);
                if (swapProp != null && swapProp.CanRead)
                {
                    if (swapProp.GetValue(this) is int interval)
                    {
                        return interval;
                    }
                }

                var contextProp = type.GetProperty("Context", BindingFlags.Instance | BindingFlags.Public);
                var context = contextProp?.GetValue(this);
                if (context != null)
                {
                    var ctxType = context.GetType();
                    var ctxSwap = ctxType.GetProperty("SwapInterval", BindingFlags.Instance | BindingFlags.Public);
                    if (ctxSwap != null && ctxSwap.CanRead)
                    {
                        if (ctxSwap.GetValue(context) is int interval)
                        {
                            return interval;
                        }
                    }
                }
            }
            catch
            {
                // Ignore.
            }

            return null;
        }

        protected override void OnMouseHover(EventArgs e)
        {
            base.OnMouseHover(e);

            Focus();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (!IsDesignMode() && !useIdleRenderLoop)
                renderer?.Update();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (IsDesignMode() || renderer == null)
            {
                return;
            }

            MakeCurrent();
            renderer.Resize(Math.Max(1, Width), Math.Max(1, Height));
            Invalidate();
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

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            if (IsDesignMode()) return;
            if (!Focused) return;

            int steps = e.Delta / 120;
            if (steps == 0) return;

            var modifiers = Control.ModifierKeys;
            if ((modifiers & Keys.Control) != 0)
            {
                float before = ctrlSpeedMultiplier;
                if (steps > 0)
                {
                    for (int i = 0; i < steps; i++)
                    {
                        ctrlSpeedMultiplier *= CtrlSpeedStepFactor;
                    }
                }
                else
                {
                    for (int i = 0; i < -steps; i++)
                    {
                        ctrlSpeedMultiplier /= CtrlSpeedStepFactor;
                    }
                }

                ctrlSpeedMultiplier = Math.Clamp(ctrlSpeedMultiplier, CtrlSpeedMin, CtrlSpeedMax);
                if (Math.Abs(ctrlSpeedMultiplier - before) > 0.0001f && MessageHandler.Instance.DebugLogsEnabled)
                {
                    MessageHandler.Instance.AddMessage(MessageType.LOG, $"[View] Ctrl speed multiplier: {ctrlSpeedMultiplier:0.##}x");
                }

                return;
            }

            pendingDolly += steps * MouseWheelDollyStep;
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
            if (!IsDesignMode())
            {
                float deltaSeconds = GetDeltaSeconds();
                renderer?.UpdateMovementControls(deltaSeconds * GetMovementSpeedMultiplier());
                Invalidate();
            }
        }

        private void RenderLoop(object? sender, EventArgs e)
        {
            if (IsDesignMode() || renderer == null) return;

            while (IsApplicationIdle())
            {
                float deltaSeconds = GetDeltaSeconds();
                renderer.UpdateMovementControls(deltaSeconds * GetMovementSpeedMultiplier());

                ApplyPendingCameraInput();
                renderer.Update();
            }
        }

        private void ApplyPendingCameraInput()
        {
            if (renderer == null) return;

            float speedMultiplier = GetMouseSpeedMultiplier();
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

        private float GetMovementSpeedMultiplier()
        {
            var modifiers = Control.ModifierKeys;
            if ((modifiers & Keys.Control) != 0)
            {
                return ctrlSpeedMultiplier;
            }
            if ((modifiers & Keys.Shift) != 0)
            {
                return 0.2f;
            }
            return 1.0f;
        }

        private static float GetMouseSpeedMultiplier()
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

        // Legacy helper removed: VSync is controlled by the viewer menu now.

        private bool IsApplicationIdle()
        {
            return !PeekMessage(out _, IntPtr.Zero, 0, 0, 0);
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
