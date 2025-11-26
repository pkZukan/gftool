using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using TrinityModLoader.Models.ModEntry;
using TrinityModLoader.Models.ModPack;

namespace TrinityModLoader.UI
{
    internal class CheckedModListBox : CheckedListBox
    {
        private ModPack? _modPack = null;

        private readonly BindingList<ModDisplayEntry> _displayEntries = new BindingList<ModDisplayEntry>();

        public CheckedModListBox()
        {
            DataSource = _displayEntries;
            DisplayMember = nameof(ModDisplayEntry.DisplayName);
            CheckOnClick = false;
        }

        public ModPack? ModPack
        {
            get => _modPack;
            set
            {
                _modPack = value;
                RefreshDisplayEntries();
            }
        }

        public void LoadModPack(ModPack modPack)
        {
            ModPack = modPack;
        }

        public void RefreshDisplayEntries()
        {
            BeginUpdate();
            _displayEntries.Clear();

            if (_modPack != null)
            {
                _modPack.mods = _modPack.mods.Where(x => x.Exists()).ToList();

                foreach (var mod in _modPack.mods)
                {
                    var entry = new ModDisplayEntry(mod);
                    _displayEntries.Add(entry);
                }
            }

            for (int i = 0; i < _displayEntries.Count; i++)
            {
                SetItemChecked(i, _displayEntries[i].IsEnabled);
            }

            EndUpdate();
        }

        public void SaveModPack(string modPackLocation)
        {
            if (_modPack == null)
            {
                return;
            }

            _modPack.Save(modPackLocation);
        }

        public void AddMod(IModEntry mod)
        {
            if (_modPack == null) return;

            _modPack.mods.Add(mod);
            RefreshDisplayEntries();

            SelectedIndex = _displayEntries.Count - 1;
        }

        public void MoveMod(int modIndex, int toIndex)
        {
            if (_modPack == null || modIndex < 0 || modIndex >= _modPack.mods.Count) return;

            var entry = _modPack.mods[modIndex];
            _modPack.mods.RemoveAt(modIndex);
            _modPack.mods.Insert(toIndex, entry);

            RefreshDisplayEntries();

            SelectedIndex = toIndex;
        }

        public void DeleteMod(int modIndex)
        {
            if (_modPack == null || modIndex < 0 || modIndex >= _modPack.mods.Count) return;

            _modPack.mods.RemoveAt(modIndex);

            RefreshDisplayEntries();
        }

        public void EnableMod(int modIndex, bool enabled)
        {
            if (_modPack == null || modIndex < 0 || modIndex >= _modPack.mods.Count) return;

            _modPack.mods[modIndex].IsEnabled = enabled;

            SetItemChecked(modIndex, enabled);
        }

        public void EnableAll(bool enabled)
        {
            if (_modPack == null) return;

            BeginUpdate();
            for (int i = 0; i < _modPack.mods.Count; i++)
            {
                EnableMod(i, enabled);
            }
            EndUpdate();
        }

        protected override void OnItemCheck(ItemCheckEventArgs ice)
        {
            base.OnItemCheck(ice);

            if (_modPack != null && ice.Index >= 0 && ice.Index < _displayEntries.Count)
            {
                var displayEntry = (ModDisplayEntry)Items[ice.Index];
                displayEntry.IsEnabled = ice.NewValue == CheckState.Checked;
            }
        }
    }
}