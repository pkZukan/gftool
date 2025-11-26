using System.Configuration;
using System.Text.Json;
using System.Text.Json.Serialization;
using Trinity.Core.Flatbuffers.TR.ResourceDictionary;

namespace TrinityModLoader.Models.Settings
{
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

        public static void AddRecentModPack(string path)
        {
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
