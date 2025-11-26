using Tomlyn.Model;
using System.Text.Json.Serialization;
using System.ComponentModel;

namespace TrinityModLoader.Models.ModEntry
{
    [JsonDerivedType(typeof(PackedModEntry), "PackedModEntry")]
    [JsonDerivedType(typeof(FolderModEntry), "FolderModEntry")]
    public interface IModEntry
    {
        public string ModPath { get; set; }
        public string URL { get; set; }
        public bool IsEnabled { get; set; }

        public string[] FetchFiles();
        public ModData FetchModData();

        public void Extract(string path);
        public bool Exists();
    }

    public class ModData
    {
        [Category("Mod Data")]
        [DisplayName("Name")]
        public string display_name { get; set; } = string.Empty;
        [Category("Mod Data")]
        [DisplayName("Author")]
        public string author_name { get; set; } = string.Empty;
        [Category("Mod Data")]
        [DisplayName("Version")]
        public string version { get; set; } = string.Empty;
        [Category("Mod Description")]
        [DisplayName("Description")]
        public string description { get; set; } = string.Empty;

    }
}
