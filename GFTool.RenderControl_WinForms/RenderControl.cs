using GFTool.Renderer.Core;
using GFTool.Renderer;
using System.ComponentModel;
using OpenTK.GLControl;
using Timer = System.Windows.Forms.Timer;
using Microsoft.VisualBasic.Devices;

namespace GFTool.RenderControl_WinForms
{
    public partial class RenderControl : GLControl
    {
        public RenderContext renderer { get; private set; }
        public Timer timer { get; private set; }

        Point prevMousePos;

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
        }

        protected override void OnMouseHover(EventArgs e)
        {
            base.OnMouseHover(e);

            Focus();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (!IsDesignMode()) 
                renderer?.Update();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (IsDesignMode()) return;

            Point mousePos = e.Location;

            if (mousePos == prevMousePos) return;

            int deltaX = (mousePos.X - prevMousePos.X);
            int deltaY = (mousePos.Y - prevMousePos.Y);

            prevMousePos = mousePos;
            if ((e.Button & MouseButtons.Left) != 0)
            {
                renderer?.RotateCamera(deltaX, deltaY);
                Invalidate();
            }
        }

        private void movementTimer_Tick(object sender, EventArgs e)
        {
            if (!IsDesignMode()) 
            {
                renderer?.UpdateMovementControls();
                Invalidate();
            }
        }
    }
}
