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
        public string modelViewerExePath { get; set; } = "";
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
        private static string _settingsPath = "settings.json";
        public static FilepathSettings _filePathSettings = new FilepathSettings();

        public static void Open(string path = "settings.json")
        {
            _settingsPath = path;

            if (!File.Exists(path))
            {
                _filePathSettings = new FilepathSettings();
            }
            else
            {
                _filePathSettings = JsonSerializer.Deserialize<FilepathSettings>(File.ReadAllText(path)) ?? new FilepathSettings();
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
        { _filePathSettings.lastPath = path; }

        public static string GetLastPath()
        {
            return _filePathSettings.lastPath;
        }

        public static void Save()
        {
            _filePathSettings.Save(_settingsPath);
        }

        public static string GetModelViewerExePath()
        {
            return _filePathSettings.modelViewerExePath;
        }

        public static void SetModelViewerExePath(string path)
        {
            _filePathSettings.modelViewerExePath = path;
        }
    }
}
