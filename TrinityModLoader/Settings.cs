using System.Text.Json;

namespace Trinity
{
    public class ModEntry 
    { 
        public string Path { get; set; }
        public bool IsChecked { get; set; }
    }
    public class Settings
    {
        public string archiveDir { get; set; } = "";
        public bool autoloadTrpfd { get; set; } = true;
        public string outputDir { get; set; } = @"\romfs\";
        public List<ModEntry> mods { get; set; } = Array.Empty<ModEntry>().ToList();

        public void Save() 
        {
            var json = JsonSerializer.Serialize<Settings>(this, new JsonSerializerOptions() { WriteIndented = true });
            File.WriteAllText("settings.json", json);
        }
    }
}
