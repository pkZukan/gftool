using System.Configuration;
using System.Text.Json;
using System.Text.Json.Serialization;
using Trinity.Core.Flatbuffers.TR.ResourceDictionary;
using TrinityModLoader;

namespace TrinityModLoader
{
    public class FilepathSettings
    {

        public List<string> recentMods { get; set; } = new List<string>();
        public string romfsDir { get; set; } = "";
        public bool openModWindow { get; set; }
        public const string trpfdRel = @"\arc\data.trpfd";
        public const string trpfsRel = @"\arc\data.trpfs";

        public FilepathSettings()
        {
        }

        public void Save(string path)
        {
            var json = JsonSerializer.Serialize<FilepathSettings>(this, new JsonSerializerOptions() { WriteIndented = true });
            File.WriteAllText(path, json);
        }
    }

    public static class ModLoaderSettings
    {
        private static string _settingsPath;
        public static FilepathSettings _settings;

        public static void Open(string path = "settings.json")
        {
            _settingsPath = path;

            if (!File.Exists(path))
            {
                _settings = new FilepathSettings();
            }
            else
            {
                _settings = JsonSerializer.Deserialize<FilepathSettings>(File.ReadAllText(path));
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

        public static void SetOpenModWindow(bool value)
        {
            _settings.openModWindow = value;
        }

        public static bool GetOpenModWindow()
        {
            return _settings.openModWindow;
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
            if (_settings == null) return;
            _settings.Save(_settingsPath);
        }
    }
}
