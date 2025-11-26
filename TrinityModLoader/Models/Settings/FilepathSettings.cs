using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace TrinityModLoader.Models.Settings
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
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions() { WriteIndented = true });
            File.WriteAllText(path, json);
        }
    }
}
