using TrinityModLoader.Models.ModEntry;

namespace TrinityModLoader.UI
{
    public class ModDisplayEntry
    {
        public IModEntry Mod { get; private set; }

        public string DisplayName => Mod.FetchModData().display_name;

        public bool IsEnabled
        {
            get => Mod.IsEnabled;
            set => Mod.IsEnabled = value;
        }

        public ModDisplayEntry(IModEntry mod)
        {
            Mod = mod;
        }
    }
}