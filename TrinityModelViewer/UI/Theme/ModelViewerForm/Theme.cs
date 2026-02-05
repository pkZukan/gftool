using System.Drawing;
using System.Windows.Forms;

namespace TrinityModelViewer
{
    public partial class ModelViewerForm
    {
        private void ApplyTheme()
        {
            ApplyTheme(this);
        }

        private void ApplyTheme(Control root)
        {
            if (root == null) return;

            var isDark = settings?.DarkMode == true;
            var back = isDark ? Color.FromArgb(30, 30, 30) : SystemColors.Control;
            var fore = isDark ? Color.Gainsboro : SystemColors.ControlText;
            var panelBack = isDark ? Color.FromArgb(40, 40, 40) : SystemColors.Control;
            var listBack = isDark ? Color.FromArgb(24, 24, 24) : SystemColors.Window;

            ApplyThemeRecursive(root, back, panelBack, listBack, fore, isDark);
        }

        private void ApplyThemeRecursive(Control control, Color back, Color panelBack, Color listBack, Color fore, bool isDark)
        {
            if (control is Form || control is Panel || control is SplitContainer || control is TabPage || control is GroupBox)
            {
                control.BackColor = panelBack;
                control.ForeColor = fore;
            }
            else if (control is PictureBox)
            {
                control.BackColor = listBack;
                control.ForeColor = fore;
            }
            else if (control is ListView || control is TreeView || control is TextBox)
            {
                control.BackColor = listBack;
                control.ForeColor = fore;
            }
            else if (control is DataGridView grid)
            {
                grid.BackgroundColor = listBack;
                grid.GridColor = isDark ? Color.FromArgb(50, 50, 50) : SystemColors.ControlDark;
                grid.DefaultCellStyle.BackColor = listBack;
                grid.DefaultCellStyle.ForeColor = fore;
                grid.DefaultCellStyle.SelectionBackColor = isDark ? Color.FromArgb(60, 90, 120) : SystemColors.Highlight;
                grid.DefaultCellStyle.SelectionForeColor = fore;
                grid.ColumnHeadersDefaultCellStyle.BackColor = panelBack;
                grid.ColumnHeadersDefaultCellStyle.ForeColor = fore;
                grid.EnableHeadersVisualStyles = false;
            }
            else if (control is MenuStrip || control is ToolStrip)
            {
                control.BackColor = back;
                control.ForeColor = fore;
            }
            else if (control is Button || control is CheckBox)
            {
                control.BackColor = back;
                control.ForeColor = fore;
            }
            else
            {
                control.BackColor = back;
                control.ForeColor = fore;
            }

            foreach (Control child in control.Controls)
            {
                ApplyThemeRecursive(child, back, panelBack, listBack, fore, isDark);
            }
        }
    }
}
