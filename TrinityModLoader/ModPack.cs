using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Trinity
{
    public class ModPack
    {
        public List<IModEntry> mods { get; set; } = new List<IModEntry>();

        public const string romfsRel = @"\romfs\";
        public const string settingsRel = @"\settings.json";

        public ModPack(){}

        public void Save(string path)
        {
            var json = JsonSerializer.Serialize<ModPack>(this, new JsonSerializerOptions { WriteIndented = true});
            File.WriteAllText(Path.Join(path, settingsRel), json);
        }
    }
}
