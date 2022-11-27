using System.Diagnostics;

namespace GFTool.FilesystemExplorer
{
    public partial class About : Form
    {
        public About()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "http://discord.gg/A99eGRF",
                UseShellExecute = true
            });
        }
    }
}
