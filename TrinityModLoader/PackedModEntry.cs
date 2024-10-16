using SharpCompress.Readers;
using Tomlyn;
using SharpCompress.Common;

namespace TrinityModLoader
{
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
}
