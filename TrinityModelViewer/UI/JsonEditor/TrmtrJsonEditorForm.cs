using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace TrinityModelViewer
{
    internal sealed class TrmtrJsonEditorForm : Form
    {
        private readonly TextBox textBox;
        private readonly Button applyButton;
        private readonly Button saveButton;
        private readonly Button cancelButton;
        private readonly string originalText;

        public string JsonText => textBox.Text ?? string.Empty;

        public TrmtrJsonEditorForm(string title, string jsonText)
        {
            Text = title;
            FormBorderStyle = FormBorderStyle.Sizable;
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(720, 480);
            ClientSize = new Size(980, 720);

            originalText = jsonText ?? string.Empty;

            textBox = new TextBox
            {
                Multiline = true,
                ScrollBars = ScrollBars.Both,
                WordWrap = false,
                AcceptsReturn = true,
                AcceptsTab = true,
                Dock = DockStyle.Fill,
                Font = new Font(FontFamily.GenericMonospace, 9f),
                Text = originalText
            };

            var bottomPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 40
            };

            applyButton = new Button
            {
                Text = "Apply",
                DialogResult = DialogResult.OK,
                Anchor = AnchorStyles.Right | AnchorStyles.Bottom,
                Location = new Point(ClientSize.Width - 270, 8),
                Size = new Size(80, 24)
            };

            saveButton = new Button
            {
                Text = "Save As...",
                Anchor = AnchorStyles.Right | AnchorStyles.Bottom,
                Location = new Point(ClientSize.Width - 180, 8),
                Size = new Size(90, 24)
            };
            saveButton.Click += (_, _) => SaveToFile();

            cancelButton = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Anchor = AnchorStyles.Right | AnchorStyles.Bottom,
                Location = new Point(ClientSize.Width - 80, 8),
                Size = new Size(70, 24)
            };

            bottomPanel.Controls.Add(applyButton);
            bottomPanel.Controls.Add(saveButton);
            bottomPanel.Controls.Add(cancelButton);

            Controls.Add(textBox);
            Controls.Add(bottomPanel);

            AcceptButton = applyButton;
            CancelButton = cancelButton;
        }

        private void SaveToFile()
        {
            using var sfd = new SaveFileDialog();
            sfd.Title = "Save TRMTR JSON";
            sfd.Filter = "JSON (*.json)|*.json|All files (*.*)|*.*";
            sfd.FileName = "material.trmtr.json";
            if (sfd.ShowDialog(this) != DialogResult.OK || string.IsNullOrWhiteSpace(sfd.FileName))
            {
                return;
            }

            try
            {
                File.WriteAllText(sfd.FileName, JsonText);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Save failed:\n{ex.Message}", "Save TRMTR JSON", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
