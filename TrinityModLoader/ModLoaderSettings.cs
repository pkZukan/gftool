using System.Configuration;
using System.Text.Json;
using System.Text.Json.Serialization;
using Trinity.Core.Flatbuffers.TR.ResourceDictionary;
using TrinityModLoader;

namespace Trinity
{
    public class Settings
    {

        public List<string> recentMods { get; set; } = new List<string>();
        public string romfsDir { get; set; } = "";

        public const string trpfdRel = @"\arc\data.trpfd";
        public const string trpfsRel = @"\arc\data.trpfs";

        public Settings()
        {
        }

        public void Save(string path)
        {
            var json = JsonSerializer.Serialize<Settings>(this, new JsonSerializerOptions() { WriteIndented = true });
            File.WriteAllText(path, json);
        }
    }

    public static class ModLoaderSettings
    {
        private static string _settingsPath;
        public static Settings _settings;

        public static void Open(string path = "settings.json")
        {
            _settingsPath = path;

            if (!File.Exists(path))
            {
                _settings = new Settings();
            }
            else
            {
                _settings = JsonSerializer.Deserialize<Settings>(File.ReadAllText(path));
            }
        }

        public static void AddRecentModPack(string path) {
            if (_settings.recentMods.Contains(path)) _settings.recentMods.Remove(path);
            _settings.recentMods.Add(path);
        }

        public static List<string> GetRecentModPacks()
        {
            return _settings.recentMods;
        }

        public static string GetRomFSPath()
        {
            return _settings.romfsDir;
        }

        public static void SetRomFSPath(string path)
        {
            _settings.romfsDir = path;
        }

        public static void Save()
        {
            if (_settings != null)
            {
                _settings.Save(_settingsPath);
            }
        }
    }
}
