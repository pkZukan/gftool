using SharpCompress.Common;
using Tomlyn;

namespace TrinityModLoader.Models.ModEntry
{
    public class FolderModEntry : IModEntry
    {
        public string ModPath { get; set; }
        public string URL { get; set; }
        public bool IsEnabled { get; set; }

        public IEnumerable<string> WalkDirectory(string directoryPath)
        {
            foreach (var filePath in Directory.EnumerateFiles(directoryPath))
            {
                yield return filePath;
            }

            foreach (var subdirectoryPath in Directory.EnumerateDirectories(directoryPath))
            {
                foreach (var filePath in WalkDirectory(subdirectoryPath))
                {
                    yield return filePath;
                }
            }
        }

        public bool Exists()
        {
            return Directory.Exists(ModPath);
        }

        public void Extract(string path)
        {
            foreach (var folderFile in WalkDirectory(ModPath))
            {
                if (File.Exists(folderFile))
                {
                    if (folderFile.Contains("info.toml")) continue;
                    var destination = $"{path}/{Path.GetRelativePath(ModPath, folderFile)}";
                    Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                    File.Copy(folderFile, destination, true);
                }
            }
        }

        public string[] FetchFiles()
        {
            var files = new List<string>();
            var rawFiles = WalkDirectory(ModPath);

            foreach (var filePath in rawFiles)
            {
                if (filePath.Contains("info.toml")) continue;
                files.Add(filePath.Replace(ModPath + "\\", "").Replace("\\", "/"));
            }

            return files.ToArray();
        }

        public ModData FetchModData()
        {
            var toml = "";

            var tomlInfoPath = $"{ModPath}/info.toml";

            if (!File.Exists(tomlInfoPath))
            {
                return new ModData
                {
                    description = "No info.toml found."
                };
            }

            toml = File.ReadAllText(tomlInfoPath);
            return Toml.ToModel<ModData>(toml);
        }
    }
}
