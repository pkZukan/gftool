using System;
using System.Drawing;
using System.Windows.Forms;

namespace TrinityModelViewer
{
    internal sealed class FlatbufferJsonEditorForm : Form
    {
        private readonly TextBox textBox;
        private readonly Panel findPanel;
        private readonly TextBox findTextBox;
        private int lastFindIndex;

        public event EventHandler<string>? ApplyRequested;
        public event EventHandler<string>? ExportRequested;
        public event EventHandler<string>? ExportReserializeRequested;

        public FlatbufferJsonEditorForm(string title, string path, string json, bool allowApply = true, bool allowExport = true)
        {
            Text = string.IsNullOrWhiteSpace(title) ? "Json Editor" : title;
            StartPosition = FormStartPosition.CenterParent;
            Width = 980;
            Height = 760;

            var header = new Label
            {
                Dock = DockStyle.Top,
                Height = 22,
                Text = path ?? string.Empty,
                AutoEllipsis = true
            };

            textBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ScrollBars = ScrollBars.Both,
                Font = new Font(FontFamily.GenericMonospace, 9f),
                WordWrap = false,
                Text = json ?? string.Empty
            };

            findPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 28,
                Visible = false
            };

            var findLabel = new Label
            {
                AutoSize = true,
                Text = "Find:",
                Left = 6,
                Top = 7
            };

            findTextBox = new TextBox
            {
                Left = 44,
                Top = 3,
                Width = 420,
                Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right
            };
            findTextBox.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    FindNext(forward: true);
                    e.Handled = true;
                }
                else if (e.KeyCode == Keys.Escape)
                {
                    HideFindPanel();
                    e.Handled = true;
                }
            };

            var findNextButton = new Button
            {
                Text = "Next",
                Width = 70,
                Height = 22,
                Top = 2,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            findNextButton.Left = findPanel.Width - (findNextButton.Width + 80);
            findNextButton.Click += (s, e) => FindNext(forward: true);

            var findCloseButton = new Button
            {
                Text = "Close",
                Width = 70,
                Height = 22,
                Top = 2,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            findCloseButton.Left = findPanel.Width - (findCloseButton.Width + 6);
            findCloseButton.Click += (s, e) => HideFindPanel();

            findPanel.Resize += (s, e) =>
            {
                findCloseButton.Left = findPanel.Width - (findCloseButton.Width + 6);
                findNextButton.Left = findCloseButton.Left - (findNextButton.Width + 6);
                findTextBox.Width = Math.Max(140, findNextButton.Left - 10 - findTextBox.Left);
            };

            findPanel.Controls.Add(findLabel);
            findPanel.Controls.Add(findTextBox);
            findPanel.Controls.Add(findNextButton);
            findPanel.Controls.Add(findCloseButton);

            var buttonPanel = new Panel { Dock = DockStyle.Bottom, Height = 34 };

            var applyButton = new Button { Text = "Apply", Dock = DockStyle.Left, Width = 90 };
            applyButton.Click += (s, e) => ApplyRequested?.Invoke(this, textBox.Text);
            applyButton.Enabled = allowApply;

            var exportButton = new Button { Text = "Export...", Dock = DockStyle.Left, Width = 90 };
            exportButton.Click += (s, e) => ExportRequested?.Invoke(this, textBox.Text);
            exportButton.Enabled = allowExport;

            var exportReserializeButton = new Button { Text = "Reserialize...", Dock = DockStyle.Left, Width = 110 };
            exportReserializeButton.Click += (s, e) => ExportReserializeRequested?.Invoke(this, textBox.Text);
            exportReserializeButton.Enabled = allowExport;

            var closeButton = new Button { Text = "Close", Dock = DockStyle.Right, Width = 90 };
            closeButton.Click += (s, e) => Close();

            buttonPanel.Controls.Add(applyButton);
            buttonPanel.Controls.Add(exportButton);
            buttonPanel.Controls.Add(exportReserializeButton);
            buttonPanel.Controls.Add(closeButton);

            Controls.Add(textBox);
            Controls.Add(findPanel);
            Controls.Add(buttonPanel);
            Controls.Add(header);

            KeyPreview = true;
            KeyDown += OnEditorKeyDown;
            textBox.KeyDown += OnEditorKeyDown;
        }

        private void OnEditorKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.F)
            {
                ShowFindPanel();
                e.Handled = true;
                return;
            }

            if (e.KeyCode == Keys.F3)
            {
                FindNext(forward: !e.Shift);
                e.Handled = true;
                return;
            }

            if (e.KeyCode == Keys.Escape && findPanel.Visible)
            {
                HideFindPanel();
                e.Handled = true;
            }
        }

        private void ShowFindPanel()
        {
            findPanel.Visible = true;
            findPanel.BringToFront();

            // Seed with current selection if any.
            if (string.IsNullOrWhiteSpace(findTextBox.Text) && textBox.SelectionLength > 0)
            {
                findTextBox.Text = textBox.SelectedText;
                lastFindIndex = textBox.SelectionStart;
            }
            else
            {
                lastFindIndex = textBox.SelectionStart;
            }

            findTextBox.Focus();
            findTextBox.SelectAll();
        }

        private void HideFindPanel()
        {
            findPanel.Visible = false;
            textBox.Focus();
        }

        private void FindNext(bool forward)
        {
            string query = findTextBox.Text ?? string.Empty;
            if (string.IsNullOrEmpty(query))
            {
                return;
            }

            string haystack = textBox.Text ?? string.Empty;
            if (haystack.Length == 0)
            {
                return;
            }

            int start = Math.Clamp(lastFindIndex, 0, haystack.Length);
            int idx = forward
                ? haystack.IndexOf(query, start, StringComparison.OrdinalIgnoreCase)
                : haystack.LastIndexOf(query, start, StringComparison.OrdinalIgnoreCase);

            if (idx < 0 && start != 0)
            {
                // wrap
                idx = forward
                    ? haystack.IndexOf(query, 0, StringComparison.OrdinalIgnoreCase)
                    : haystack.LastIndexOf(query, haystack.Length - 1, StringComparison.OrdinalIgnoreCase);
            }

            if (idx < 0)
            {
                return;
            }

            textBox.SelectionStart = idx;
            textBox.SelectionLength = query.Length;
            textBox.ScrollToCaret();
            textBox.Focus();

            lastFindIndex = forward ? idx + query.Length : Math.Max(0, idx - 1);
        }
    }
}
