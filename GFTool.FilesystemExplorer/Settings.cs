using System.Text.Json;

namespace GFTool.FilesystemExplorer
{
    public class Settings
    {
        public string archiveDir { get; set; } = "";
        public bool autoloadTrpfd { get; set; } = true;
        public string outputDir { get; set; } = @"\romfs\";

        public void Save() 
        {
            var json = JsonSerializer.Serialize<Settings>(this, new JsonSerializerOptions() { WriteIndented = true });
            File.WriteAllText("settings.json", json);
        }
    }
}
