using SharpCompress.Readers;
using Tomlyn.Model;
using Tomlyn;
using SharpCompress.Common;
using System.Text.Json.Serialization;
using System.ComponentModel;

namespace Trinity
{
    [JsonDerivedType(typeof(PackedModEntry), "PackedModEntry")]
    [JsonDerivedType(typeof(FolderModEntry), "FolderModEntry")]
    public interface IModEntry
    {
        public string ModPath { get; set; }
        public string URL { get; set; }
        public bool IsEnabled { get; set; }

        public string[] FetchFiles();
        public ModData FetchToml();

        public void Extract(string path);
        public bool Exists();
    }

    public class ModData
    {
        [Category("Mod Data")]
        [DisplayName("Name")]
        public string display_name { get; set; }
        public string description { get; set; }
        public string version { get; set; }


    }
    public class PackedModEntry : IModEntry
    {
        public string ModPath { get; set; }
        public string URL { get; set; }
        public bool IsEnabled { get; set; }

        public bool Exists()
        {
            return File.Exists(ModPath);
        }

        public void Extract(string path)
        {
            using Stream stream = File.OpenRead(ModPath);
            using var reader = ReaderFactory.Open(stream);
            while (reader.MoveToNextEntry())
            {
                if (!reader.Entry.IsDirectory)
                {
                    var entry = reader.Entry.Key;
                    if (entry.Contains("info.toml")) continue;
                    reader.WriteEntryToDirectory(path, new ExtractionOptions() { ExtractFullPath = true, Overwrite = true });
                }
            }
        }

        public string[] FetchFiles()
        {
            using Stream stream = File.OpenRead(ModPath);
            using var reader = ReaderFactory.Open(stream);
            var list = new List<string>();

            while (reader.MoveToNextEntry())
            {
                if (!reader.Entry.IsDirectory)
                {
                    var entry = reader.Entry.Key;
                    if (entry.Contains("info.toml")) continue;
                    list.Add(entry.Replace("\\", "/"));
                }
            }

            return list.ToArray();
        }

        public ModData FetchToml()
        {
            var toml = "";

            using Stream stream = File.OpenRead(Path.Join(ModPath));
            using var reader = ReaderFactory.Open(stream);
            while (reader.MoveToNextEntry())
            {
                if (!reader.Entry.IsDirectory)
                {
                    var entry = reader.Entry.Key.Replace('\\', '/');
                    if (entry.EndsWith("info.toml"))
                    {
                        using var entryStream = reader.OpenEntryStream();
                        using var r = new StreamReader(entryStream);
                        toml = r.ReadToEnd();
                    }
                }
            }
            return Toml.ToModel<ModData>(toml);
        }
    }
    public class FolderModEntry : IModEntry
    {
        public string ModPath { get; set; }
        public string URL { get; set; }
        public bool IsEnabled { get; set; }

        static IEnumerable<string> WalkDirectory(string directoryPath)
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
            return WalkDirectory(ModPath).ToArray();
        }

        public ModData FetchToml()
        {
            var toml = "";

            var tomlInfoPath = $"{ModPath}/info.toml";
            if (File.Exists(tomlInfoPath))
            {
                toml = File.ReadAllText(tomlInfoPath);
            }

            return Toml.ToModel<ModData>(toml);
        }
    }
}
