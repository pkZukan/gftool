using System;
using System.Drawing;
using System.Windows.Forms;

namespace TrinityModelViewer
{
    public sealed class LoadingForm : Form
    {
        private readonly Label messageLabel;
        private readonly ProgressBar progressBar;
        private readonly Button cancelButton;
        private bool isIndeterminate = true;

        public event EventHandler? CancelRequested;

        public LoadingForm()
        {
            Text = "Loading...";
            FormBorderStyle = FormBorderStyle.FixedToolWindow;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ControlBox = false;
            TopMost = true;
            ClientSize = new Size(420, 110);

            messageLabel = new Label
            {
                Dock = DockStyle.Top,
                Height = 34,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 8, 10, 0),
                Text = "Loading model..."
            };

            cancelButton = new Button
            {
                Text = "Cancel",
                Dock = DockStyle.Right,
                Width = 90
            };
            cancelButton.Click += (_, __) =>
            {
                cancelButton.Enabled = false;
                cancelButton.Text = "Canceling...";
                CancelRequested?.Invoke(this, EventArgs.Empty);
            };

            progressBar = new ProgressBar
            {
                Dock = DockStyle.Fill,
                Minimum = 0,
                Maximum = 100,
                Value = 0,
                Style = ProgressBarStyle.Marquee,
                MarqueeAnimationSpeed = 30,
                Margin = new Padding(10)
            };

            var padPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10, 0, 10, 10) };
            var bottomPanel = new Panel { Dock = DockStyle.Bottom, Height = 34, Padding = new Padding(0, 6, 0, 0) };
            bottomPanel.Controls.Add(cancelButton);
            padPanel.Controls.Add(progressBar);
            padPanel.Controls.Add(bottomPanel);

            Controls.Add(padPanel);
            Controls.Add(messageLabel);
        }

        public void SetMessage(string message)
        {
            messageLabel.Text = string.IsNullOrWhiteSpace(message) ? "Loading model..." : message;
        }

        public void SetProgress(int percent)
        {
            if (isIndeterminate)
            {
                return;
            }

            percent = Math.Clamp(percent, 0, 100);
            if (progressBar.Value != percent)
            {
                progressBar.Value = percent;
            }
        }

        public void SetIndeterminate(bool enabled)
        {
            if (isIndeterminate == enabled)
            {
                return;
            }

            isIndeterminate = enabled;
            if (enabled)
            {
                progressBar.Style = ProgressBarStyle.Marquee;
                progressBar.MarqueeAnimationSpeed = 30;
            }
            else
            {
                progressBar.Style = ProgressBarStyle.Continuous;
                progressBar.MarqueeAnimationSpeed = 0;
            }
        }

        public void ResetCancel()
        {
            cancelButton.Enabled = true;
            cancelButton.Text = "Cancel";
        }
    }
}
