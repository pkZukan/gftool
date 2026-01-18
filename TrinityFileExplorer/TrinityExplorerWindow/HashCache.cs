using Trinity.Core.Math.Hash;
using Trinity.Core.Cache;
using Trinity.Core.Compression;
using Trinity.Core.Flatbuffers.TR.ResourceDictionary;
using Trinity.Core.Serializers.TR;
using Trinity.Core.Utils;
using System.Linq;
using System.Diagnostics;
using Trinity.Core.Flatbuffers.TR.Model;
using System.Drawing;


namespace TrinityFileExplorer
{
    public partial class TrinityExplorerWindow : Form
    {
        private void InitializeCache()
        {
            if (!File.Exists("GFPAKHashCache.bin"))
            {
                DialogResult dialogResult = MessageBox.Show("No GFPAKHashCache.bin found, do you want to create one?", "Missing Files", MessageBoxButtons.YesNo);

                if (dialogResult == DialogResult.Yes)
                {
                    FetchHashesFromURL();
                }
                else
                {
                    GFPakHashCache.Clear();
                    GFPakHashCache.Save();
                    MessageBox.Show($"Empty GFPAKHashCache.bin created. ({GFPakHashCache.Count} entries)", "Hash Cache", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            else
            {
                GFPakHashCache.Open();
            }
        }

        private static readonly string[] DefaultHashlistUrls =
        {
            "https://raw.githubusercontent.com/pkZukan/PokeDocs/main/SV/Hashlists/FileSystem/hashes_inside_fd.txt",
            "https://raw.githubusercontent.com/pkZukan/PokeDocs/main/ZA/Hashlists/FileSystem/hashes_inside_fd.txt"
        };

        private void FetchHashesFromURL(string fileUrl = "https://raw.githubusercontent.com/pkZukan/PokeDocs/main/SV/Hashlists/FileSystem/hashes_inside_fd.txt")
        {
            FetchHashesFromURLs(new[] { fileUrl });
        }

        private void FetchHashesFromURLs(IEnumerable<string> fileUrls)
        {
            using var httpClient = new HttpClient();

            int success = 0;
            var failures = new List<string>();

            foreach (var url in fileUrls.Where(u => !string.IsNullOrWhiteSpace(u)))
            {
                try
                {
                    HttpResponseMessage response = httpClient.GetAsync(url).GetAwaiter().GetResult();
                    if (!response.IsSuccessStatusCode)
                    {
                        failures.Add(url);
                        continue;
                    }

                    Stream fileStream = response.Content.ReadAsStreamAsync().GetAwaiter().GetResult();
                    using var streamReader = new StreamReader(fileStream);

                    List<string> lines = new List<string>();
                    string? line;
                    while ((line = streamReader.ReadLine()) != null)
                    {
                        lines.Add(line);
                    }
                    GFPakHashCache.AddHashFromList(lines);
                    success++;
                }
                catch
                {
                    failures.Add(url);
                }
            }

            if (success > 0)
            {
                GFPakHashCache.Save();
                string msg = $"GFPAKHashCache.bin updated! ({GFPakHashCache.Count} entries)\nSources: {success}/{fileUrls.Count()}";
                if (failures.Count > 0)
                {
                    msg += "\nFailed:\n" + string.Join("\n", failures);
                }
                MessageBox.Show(msg, "Hash Cache", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                var message_text = "Failed to download latest hashes.\n\nManually download the \"hashes_inside_fd.txt\" file into your Trinity folder.\n\nClick OK to copy the URLs to your clipboard.";

                if (MessageBox.Show(message_text, "Failed to download", MessageBoxButtons.OKCancel) == DialogResult.OK)
                {
                    Clipboard.SetText(string.Join("\n", failures.Count > 0 ? failures : fileUrls));
                }
            }
        }
        private void latestToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FetchHashesFromURLs(DefaultHashlistUrls);
            InitializeExplorer();
        }

        private void fromFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var ofd = new OpenFileDialog();
            if (ofd.ShowDialog() != DialogResult.OK) return;
            GFPakHashCache.AddHashFromList(File.ReadAllLines(ofd.FileName).ToList());
            GFPakHashCache.Save();
            MessageBox.Show($"GFPAKHashCache.bin updated! ({GFPakHashCache.Count} entries)", "Hash Cache", MessageBoxButtons.OK, MessageBoxIcon.Information);
            InitializeExplorer();
        }

        private void fromURLToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FetchHashesFromURLs(DefaultHashlistUrls);
            InitializeExplorer();
        }
    }
}
