using System.Configuration;
using System.Text.Json;
using System.Text.Json.Serialization;
using Trinity.Core.Flatbuffers.TR.ResourceDictionary;

namespace TrinityFileExplorer
{
    public class FilepathSettings
    {
        public string romfsDir { get; set; } = "";
        public string lastPath { get; set; } = "";
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

    public static class ExplorerSettings
    {
        private static string _settingsPath;
        public static FilepathSettings _filePathSettings;

        public static void Open(string path = "settings.json")
        {
            _settingsPath = path;

            if (!File.Exists(path))
            {
                _filePathSettings = new FilepathSettings();
            }
            else
            {
                _filePathSettings = JsonSerializer.Deserialize<FilepathSettings>(File.ReadAllText(path));
            }
        }

        public static string GetRomFSPath()
        {
            return _filePathSettings.romfsDir;
        }

        public static void SetRomFSPath(string path)
        {
            _filePathSettings.romfsDir = path;
        }

        public static void SetLastPath(string path)
        { _filePathSettings.lastPath = path;}

        public static string GetLastPath()
        {
            return _filePathSettings.lastPath;
        }

        public static void Save()
        {
            if (_filePathSettings == null) return;
            _filePathSettings.Save(_settingsPath);
        }
    }
}
