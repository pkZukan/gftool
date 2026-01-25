using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Trinity.Core.Assets;

namespace TrinityModelViewer
{
    public sealed class GfpakBrowserForm : Form
    {
        private readonly IAssetProvider provider;
        private readonly TextBox filterTextBox;
        private readonly TextBox openPathTextBox;
        private readonly ListView listView;
        private readonly Button openButton;
        private readonly Button cancelButton;
        private readonly Label hintLabel;
        private List<AssetEntry> entries = new List<AssetEntry>();

        public string? SelectedModelPath { get; private set; }

        public GfpakBrowserForm(IAssetProvider provider)
        {
            this.provider = provider;

            Text = $"Open from GFPAK - {provider.DisplayName}";
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(720, 520);
            Size = new Size(900, 620);

            hintLabel = new Label
            {
                AutoSize = false,
                Dock = DockStyle.Top,
                Height = 48,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 6, 10, 6),
                Text = "If `GFPAKHashCache.bin` is available, names will be shown and you can double-click a `.trmdl`.\nIf not, paste a model path inside the pack (or import a hash list) and open it by path."
            };

            filterTextBox = new TextBox
            {
                Dock = DockStyle.Top,
                PlaceholderText = "Filter (requires names; otherwise filters hash)…"
            };
            filterTextBox.TextChanged += (s, e) => RebuildList();

            listView = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                HideSelection = false
            };
            listView.Columns.Add("Path", 560);
            listView.Columns.Add("Hash", 240);
            listView.DoubleClick += (s, e) => TryAcceptSelectedListItem();

            openPathTextBox = new TextBox
            {
                Dock = DockStyle.Bottom,
                PlaceholderText = "Open by path inside pack (example: field_graphic/.../model.trmdl)"
            };

            var bottomPanel = new Panel { Dock = DockStyle.Bottom, Height = 44 };
            openButton = new Button { Text = "Open", Anchor = AnchorStyles.Right | AnchorStyles.Bottom, Size = new Size(100, 28) };
            cancelButton = new Button { Text = "Cancel", Anchor = AnchorStyles.Right | AnchorStyles.Bottom, Size = new Size(100, 28), DialogResult = DialogResult.Cancel };
            openButton.Location = new Point(bottomPanel.Width - 220, 8);
            cancelButton.Location = new Point(bottomPanel.Width - 110, 8);
            bottomPanel.Resize += (s, e) =>
            {
                openButton.Location = new Point(bottomPanel.Width - 220, 8);
                cancelButton.Location = new Point(bottomPanel.Width - 110, 8);
            };
            openButton.Click += (s, e) => TryAccept();

            bottomPanel.Controls.Add(openButton);
            bottomPanel.Controls.Add(cancelButton);

            Controls.Add(listView);
            Controls.Add(filterTextBox);
            Controls.Add(hintLabel);
            Controls.Add(openPathTextBox);
            Controls.Add(bottomPanel);

            AcceptButton = openButton;
            CancelButton = cancelButton;

            LoadEntries();
        }

        private void LoadEntries()
        {
            entries = provider.EnumerateEntries().ToList();
            RebuildList();
        }

        private void RebuildList()
        {
            string filter = filterTextBox.Text?.Trim() ?? string.Empty;
            listView.BeginUpdate();
            try
            {
                listView.Items.Clear();

                IEnumerable<AssetEntry> filtered = entries;
                if (!string.IsNullOrWhiteSpace(filter))
                {
                    filtered = filtered.Where(e =>
                        (!string.IsNullOrEmpty(e.Path) && e.Path.Contains(filter, StringComparison.OrdinalIgnoreCase)) ||
                        $"0x{e.PathHash:X16}".Contains(filter, StringComparison.OrdinalIgnoreCase));
                }

                // Prefer showing models first when names exist.
                filtered = filtered
                    .OrderByDescending(e => e.Path != null && e.Path.EndsWith(".trmdl", StringComparison.OrdinalIgnoreCase))
                    .ThenBy(e => e.Path ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(e => e.PathHash);

                foreach (var entry in filtered.Take(10000))
                {
                    string displayPath = entry.Path ?? "(unknown path)";
                    string hash = $"0x{entry.PathHash:X16}";

                    var item = new ListViewItem(displayPath);
                    item.SubItems.Add(hash);
                    item.Tag = entry;
                    listView.Items.Add(item);
                }
            }
            finally
            {
                listView.EndUpdate();
            }
        }

        private void TryAcceptSelectedListItem()
        {
            if (listView.SelectedItems.Count == 0)
            {
                return;
            }

            var entry = (AssetEntry)listView.SelectedItems[0].Tag;
            if (!string.IsNullOrWhiteSpace(entry.Path))
            {
                SelectedModelPath = entry.Path;
                DialogResult = DialogResult.OK;
                Close();
                return;
            }

            // If we don't know the path, user must provide it.
            openPathTextBox.Focus();
        }

        private void TryAccept()
        {
            string directPath = openPathTextBox.Text?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(directPath))
            {
                SelectedModelPath = directPath;
                DialogResult = DialogResult.OK;
                Close();
                return;
            }

            TryAcceptSelectedListItem();
        }
    }
}
