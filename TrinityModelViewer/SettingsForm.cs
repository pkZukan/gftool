using System;
using System.Drawing;
using System.Windows.Forms;

namespace TrinityModelViewer
{
    public class SettingsForm : Form
    {
        private readonly CheckBox darkModeCheckBox;
        private readonly CheckBox loadLodsCheckBox;
        private readonly CheckBox debugLogsCheckBox;
        private readonly CheckBox autoLoadAnimationsCheckBox;
        private readonly Button okButton;
        private readonly Button cancelButton;

        public bool DarkModeEnabled => darkModeCheckBox.Checked;
        public bool LoadAllLodsEnabled => loadLodsCheckBox.Checked;
        public bool DebugLogsEnabled => debugLogsCheckBox.Checked;
        public bool AutoLoadAnimationsEnabled => autoLoadAnimationsCheckBox.Checked;

        public SettingsForm(bool darkModeEnabled, bool loadAllLodsEnabled, bool debugLogsEnabled, bool autoLoadAnimationsEnabled)
        {
            Text = "Settings";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(360, 178);

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

            debugLogsCheckBox = new CheckBox
            {
                Text = "Enable debug logs",
                Checked = debugLogsEnabled,
                AutoSize = true,
                Location = new Point(16, 72)
            };

            autoLoadAnimationsCheckBox = new CheckBox
            {
                Text = "Auto load animations",
                Checked = autoLoadAnimationsEnabled,
                AutoSize = true,
                Location = new Point(16, 100)
            };

            okButton = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                Location = new Point(184, 132),
                Size = new Size(75, 26)
            };

            cancelButton = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                Location = new Point(265, 132),
                Size = new Size(75, 26)
            };

            Controls.Add(darkModeCheckBox);
            Controls.Add(loadLodsCheckBox);
            Controls.Add(debugLogsCheckBox);
            Controls.Add(autoLoadAnimationsCheckBox);
            Controls.Add(okButton);
            Controls.Add(cancelButton);

            AcceptButton = okButton;
            CancelButton = cancelButton;
        }
    }
}
