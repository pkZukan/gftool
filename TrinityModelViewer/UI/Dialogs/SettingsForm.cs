using System;
using System.Drawing;
using System.Windows.Forms;

namespace TrinityModelViewer
{
    public class SettingsForm : Form
    {
        private readonly CheckBox darkModeCheckBox;
        private readonly CheckBox loadLodsCheckBox;
        private readonly CheckBox autoGenerateLodsOnExportCheckBox;
        private readonly CheckBox exportModelPcBaseOnExportCheckBox;
        private readonly CheckBox debugLogsCheckBox;
        private readonly CheckBox autoLoadAnimationsCheckBox;
        private readonly CheckBox autoLoadFirstGfpakModelCheckBox;
        private readonly CheckBox showMultipleModelsCheckBox;
        private readonly ComboBox shaderGameComboBox;
        private readonly CheckBox extractedFallbackCheckBox;
        private readonly ComboBox extractedGameComboBox;
        private readonly TextBox zaOutRootTextBox;
        private readonly TextBox svOutRootTextBox;
        private readonly Button okButton;
        private readonly Button cancelButton;

        public bool DarkModeEnabled => darkModeCheckBox.Checked;
        public bool LoadAllLodsEnabled => loadLodsCheckBox.Checked;
        public bool AutoGenerateLodsOnExportEnabled => autoGenerateLodsOnExportCheckBox.Checked;
        public bool ExportModelPcBaseOnExportEnabled => exportModelPcBaseOnExportCheckBox.Checked;
        public bool DebugLogsEnabled => debugLogsCheckBox.Checked;
        public bool AutoLoadAnimationsEnabled => autoLoadAnimationsCheckBox.Checked;
        public bool AutoLoadFirstGfpakModelEnabled => autoLoadFirstGfpakModelCheckBox.Checked;
        public bool ShowMultipleModelsEnabled => showMultipleModelsCheckBox.Checked;
        public string ShaderGameSelection => shaderGameComboBox.SelectedItem?.ToString() ?? "Auto";
        public bool ExtractedOutFallbackEnabled => extractedFallbackCheckBox.Checked;
        public string ActiveExtractedGameSelection => extractedGameComboBox.SelectedItem?.ToString() ?? "ZA";
        public string ZaExtractedOutRoot => zaOutRootTextBox.Text ?? string.Empty;
        public string SvExtractedOutRoot => svOutRootTextBox.Text ?? string.Empty;

        public SettingsForm(
            bool darkModeEnabled,
            bool loadAllLodsEnabled,
            bool autoGenerateLodsOnExportEnabled,
            bool exportModelPcBaseOnExportEnabled,
            bool debugLogsEnabled,
            bool autoLoadAnimationsEnabled,
            bool autoLoadFirstGfpakModelEnabled,
            bool showMultipleModelsEnabled,
            string shaderGameSelection,
            bool extractedOutFallbackEnabled,
            string activeExtractedGameSelection,
            string zaExtractedOutRoot,
            string svExtractedOutRoot)
        {
            Text = "Settings";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(540, 468);

            darkModeCheckBox = new CheckBox
            {
                Text = "Enable dark mode",
                Checked = darkModeEnabled,
                AutoSize = true,
                Location = new Point(16, 16)
            };

            loadLodsCheckBox = new CheckBox
            {
                Text = "Load all LODs",
                Checked = loadAllLodsEnabled,
                AutoSize = true,
                Location = new Point(16, 44)
            };

            autoGenerateLodsOnExportCheckBox = new CheckBox
            {
                Text = "Auto-generate LODs on export (temporary: duplicate LOD0)",
                Checked = autoGenerateLodsOnExportEnabled,
                AutoSize = true,
                Location = new Point(16, 72)
            };

            exportModelPcBaseOnExportCheckBox = new CheckBox
            {
                Text = "Export model_pc_base/p0_base.trskl on export (protag preview helper)",
                Checked = exportModelPcBaseOnExportEnabled,
                AutoSize = true,
                Location = new Point(16, 100)
            };

            debugLogsCheckBox = new CheckBox
            {
                Text = "Enable debug logs",
                Checked = debugLogsEnabled,
                AutoSize = true,
                Location = new Point(16, 128)
            };

            autoLoadAnimationsCheckBox = new CheckBox
            {
                Text = "Auto load animations",
                Checked = autoLoadAnimationsEnabled,
                AutoSize = true,
                Location = new Point(16, 156)
            };

            autoLoadFirstGfpakModelCheckBox = new CheckBox
            {
                Text = "Auto load first model when opening GFPAK",
                Checked = autoLoadFirstGfpakModelEnabled,
                AutoSize = true,
                Location = new Point(16, 184)
            };

            showMultipleModelsCheckBox = new CheckBox
            {
                Text = "Show multiple models at once",
                Checked = showMultipleModelsEnabled,
                AutoSize = true,
                Location = new Point(16, 212)
            };

            var shaderGameLabel = new Label
            {
                Text = "Shader mapping (Auto detects ZA vs SCVI; GFPAK forces LA):",
                AutoSize = true,
                Location = new Point(16, 240)
            };

            shaderGameComboBox = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(16, 262),
                Size = new Size(508, 23)
            };
            shaderGameComboBox.Items.AddRange(new object[] { "Auto", "SCVI", "ZA", "LA" });
            var desired = string.IsNullOrWhiteSpace(shaderGameSelection) ? "Auto" : shaderGameSelection.Trim();
            int idx = shaderGameComboBox.FindStringExact(desired);
            shaderGameComboBox.SelectedIndex = idx >= 0 ? idx : 0;

            var extractedGroup = new GroupBox
            {
                Text = "Extracted Game Assets (Fallback)",
                Location = new Point(16, 294),
                Size = new Size(508, 132)
            };

            extractedFallbackCheckBox = new CheckBox
            {
                Text = "Use extracted game files when local assets are missing",
                Checked = extractedOutFallbackEnabled,
                AutoSize = true,
                Location = new Point(12, 22)
            };

            var extractedGameLabel = new Label
            {
                Text = "Active game root:",
                AutoSize = true,
                Location = new Point(12, 50)
            };

            extractedGameComboBox = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(120, 46),
                Size = new Size(120, 23)
            };
            extractedGameComboBox.Items.AddRange(new object[] { "ZA", "SV" });
            var desiredGame = string.IsNullOrWhiteSpace(activeExtractedGameSelection) ? "ZA" : activeExtractedGameSelection.Trim();
            int gameIdx = extractedGameComboBox.FindStringExact(desiredGame);
            extractedGameComboBox.SelectedIndex = gameIdx >= 0 ? gameIdx : 0;

            var zaLabel = new Label
            {
                Text = "ZA out root:",
                AutoSize = true,
                Location = new Point(12, 78)
            };
            zaOutRootTextBox = new TextBox
            {
                Location = new Point(120, 74),
                Size = new Size(320, 23),
                Text = zaExtractedOutRoot ?? string.Empty
            };
            var zaBrowse = new Button
            {
                Text = "...",
                Location = new Point(446, 73),
                Size = new Size(44, 25)
            };
            zaBrowse.Click += (s, e) => BrowseForFolder(zaOutRootTextBox);

            var svLabel = new Label
            {
                Text = "SV out root:",
                AutoSize = true,
                Location = new Point(12, 106)
            };
            svOutRootTextBox = new TextBox
            {
                Location = new Point(120, 102),
                Size = new Size(320, 23),
                Text = svExtractedOutRoot ?? string.Empty
            };
            var svBrowse = new Button
            {
                Text = "...",
                Location = new Point(446, 101),
                Size = new Size(44, 25)
            };
            svBrowse.Click += (s, e) => BrowseForFolder(svOutRootTextBox);

            extractedGroup.Controls.Add(extractedFallbackCheckBox);
            extractedGroup.Controls.Add(extractedGameLabel);
            extractedGroup.Controls.Add(extractedGameComboBox);
            extractedGroup.Controls.Add(zaLabel);
            extractedGroup.Controls.Add(zaOutRootTextBox);
            extractedGroup.Controls.Add(zaBrowse);
            extractedGroup.Controls.Add(svLabel);
            extractedGroup.Controls.Add(svOutRootTextBox);
            extractedGroup.Controls.Add(svBrowse);

            okButton = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                Location = new Point(348, 434),
                Size = new Size(75, 26)
            };

            cancelButton = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                Location = new Point(429, 434),
                Size = new Size(75, 26)
            };

            Controls.Add(darkModeCheckBox);
            Controls.Add(loadLodsCheckBox);
            Controls.Add(autoGenerateLodsOnExportCheckBox);
            Controls.Add(exportModelPcBaseOnExportCheckBox);
            Controls.Add(debugLogsCheckBox);
            Controls.Add(autoLoadAnimationsCheckBox);
            Controls.Add(autoLoadFirstGfpakModelCheckBox);
            Controls.Add(showMultipleModelsCheckBox);
            Controls.Add(shaderGameLabel);
            Controls.Add(shaderGameComboBox);
            Controls.Add(extractedGroup);
            Controls.Add(okButton);
            Controls.Add(cancelButton);

            AcceptButton = okButton;
            CancelButton = cancelButton;
        }

        private void BrowseForFolder(TextBox target)
        {
            using var fbd = new FolderBrowserDialog();
            fbd.Description = "Select the extracted game 'out' directory";
            if (!string.IsNullOrWhiteSpace(target.Text) && System.IO.Directory.Exists(target.Text))
            {
                fbd.SelectedPath = target.Text;
            }

            if (fbd.ShowDialog(this) == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
            {
                target.Text = fbd.SelectedPath;
            }
        }
    }
}
