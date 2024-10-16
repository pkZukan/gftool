using Trinity.Core.Utils;
using Trinity.Core.Math.Hash;
using Trinity.Core.Cache;
using Trinity.Core.Flatbuffers.TR.ResourceDictionary;
using System.Diagnostics;
using System.Text.Json;

namespace TrinityModLoader
{
    public partial class TrinityMainWindow : Form
    {
        public static string titleText = "Trinity Mod Loader";

        public CustomFileDescriptor? customFileDescriptor = null;
        public string modPackLocation = String.Empty;
        public ModPack? modPack = null;

        public TrinityMainWindow()
        {
            InitializeComponent();
            InitializeSettings();
            InitializeLastModpacks();
        }

        private void NoValidRomFS()
        {
            MessageBox.Show("Trinity Mod Loader requires a valid RomFS path to function.");
            Environment.Exit(0);
        }
        private void InitializeSettings()
        {
            ModLoaderSettings.Open();

            var romfsDir = ModLoaderSettings.GetRomFSPath();

            if (romfsDir == "" || !Directory.Exists(romfsDir))
            {
                MessageBox.Show("Please set your RomFS path.", "Missing RomFS");

                var folderBrowser = new FolderBrowserDialog();
                if (folderBrowser.ShowDialog() != DialogResult.OK)
                {
                    NoValidRomFS();
                }

                ModLoaderSettings.SetRomFSPath(folderBrowser.SelectedPath);

                if (ParseFileDescriptor(Path.Join(ModLoaderSettings.GetRomFSPath(), FilepathSettings.trpfdRel)) != DialogResult.OK)
                {
                    NoValidRomFS();
                }

                ModLoaderSettings.Save();
            }
            else
            {
                if (!TryLoadFileDescriptor(Path.Join(romfsDir, FilepathSettings.trpfdRel), out customFileDescriptor))
                {
                    NoValidRomFS();
                };
            }

            openModWindowMenuItem.Checked = ModLoaderSettings.GetOpenModWindow();
        }

        private void InitializeLastModpacks()
        {
            var recentModPacks = ModLoaderSettings.GetRecentModPacks();

            if (recentModPacks == null || recentModPacks.Count == 0)
            {
                this.Text = $"{titleText} - No Modpack Loaded";
                return;
            }

            //We do a reverse for-loop here in case a user deletes a recent folder
            //I'd much rather remove these than have the user click on one and
            //then display a pop-up telling them the mod can't be found

            for (int i = recentModPacks.Count - 1; i > -1; i--)
            {
                var modPackPath = recentModPacks[i];
                if (!Directory.Exists(modPackPath))
                {
                    recentModPacks.Remove(modPackPath);
                    continue;
                }
                else
                {
                    var modMenuItem = new ToolStripMenuItem() { Text = modPackPath };
                    modMenuItem.Click += (s, e) =>
                    {
                        LoadModPack(modPackPath);
                    };

                    OpenModPackMenuItems.DropDownItems.Add(modMenuItem);
                }
            }

            if (recentModPacks == null || recentModPacks.Count == 0)
            {
                this.Text = $"{titleText} - No Modpack Loaded";
                return;
            }

            LoadModPack(recentModPacks.Last());

        }

        private void LoadModPack(string path)
        {
            if (modPackLocation != null || modPackLocation != String.Empty)
            {
                ModLoaderSettings.AddRecentModPack(path);

            }

            modList.Items.Clear();
            modPackLocation = path;

            modPack = JsonSerializer.Deserialize<ModPack>(File.ReadAllText(Path.Join(path, ModPack.settingsRel)));

            addFolderMod.Enabled = true;
            addPackedMod.Enabled = true;

            modPack.mods = modPack.mods.Where(x => x.Exists()).ToList();
            if (modPack.mods.Count > 0) applyModsBut.Enabled = true;

            foreach (var mod in modPack.mods)
            {
                var name = Path.GetFileName(mod.ModPath);
                var ind = modList.Items.Add(mod.ModPath);
                modList.SetItemChecked(ind, mod.IsEnabled);
            }

            this.Text = $"{titleText} - {modPackLocation}";
        }

        private void CreateModPack(string path)
        {
            modPackLocation = path;

            modPack = new ModPack()
            {
                mods = new List<IModEntry>(),
            };

            modList.Items.Clear();

            addFolderMod.Enabled = true;
            addPackedMod.Enabled = true;
            applyModsBut.Enabled = false;

            this.Text = $"{titleText} - {modPackLocation}";

            modPack.Save(path);
        }

        private DialogResult ParseFileDescriptor(string file = "")
        {
            if (!TryLoadFileDescriptor(file, out customFileDescriptor))
            {
                return DialogResult.Abort;
            }

            if (customFileDescriptor.HasUnusedFiles())
            {
                MessageBox.Show("This is a modified TRPFD.\n Please provide an unmodified TRPFD.");
                return DialogResult.Abort;
            }

            return DialogResult.OK;
        }

        private bool TryLoadFileDescriptor(string file, out CustomFileDescriptor customFileDescriptor)
        {
            if (!File.Exists(file))
            {
                MessageBox.Show($"No TRPFD found in the provided RomFS folder.\nUsually it's in {file}");
                customFileDescriptor = null;
                return false;
            }

            customFileDescriptor = FlatBufferConverter.DeserializeFrom<CustomFileDescriptor>(file);
            if (customFileDescriptor == null)
            {
                MessageBox.Show("Failed to load TRPFD.");
                return false;
            }

            return true;
        }

        private void MoveMod(int modIndex, int toIndex)
        {
            var item = modList.Items[modIndex];
            var entry = modPack.mods[modIndex];

            modList.Items.RemoveAt(modIndex);
            modList.Items.Insert(toIndex, item);
            modPack.mods.RemoveAt(modIndex);
            modPack.mods.Insert(toIndex, entry);
        }

        void RemoveMod(IModEntry mod)
        {
            foreach (var f in mod.FetchFiles())
            {
                customFileDescriptor?.AddFile(GFFNV.Hash(f));
            }
        }

        void ApplyMod(IModEntry mod, string lfsDir)
        {
            mod.Extract(lfsDir);

            foreach (var f in mod.FetchFiles())
            {
                var fhash = GFFNV.Hash(f);
                customFileDescriptor?.RemoveFile(fhash);
            }
        }

        private void AddModToList(IModEntry mod)
        {
            if (modList.Items.Contains(mod.ModPath))
            {
                var ow = MessageBox.Show("Mod already exists, do you want to overwrite?", "Mod exists", MessageBoxButtons.YesNo);
                if (ow == DialogResult.No) return;

                var oldMod = modPack.mods.Where(modPack => modPack.ModPath == mod.ModPath).FirstOrDefault();
                modPack.mods.Remove(oldMod);
                modList.Items.Remove(mod.ModPath);
            }

            if (applyModsBut.Enabled == false) applyModsBut.Enabled = true;

            mod.IsEnabled = true;
            modPack.mods.Add(mod);
            modList.Items.Add(mod.ModPath, true);
        }

        void SerializeTRPFD(string fileOut)
        {
            var file = new System.IO.FileInfo(fileOut);
            if (!file.Directory.Exists) file.Directory.Create();

            var trpfd = FlatBufferConverter.SerializeFrom<CustomFileDescriptor>(customFileDescriptor);
            File.WriteAllBytes(fileOut, trpfd);
        }

        private void PopulateMetaData()
        {
            var mod = modPack.mods[modList.SelectedIndex];
            var toml = mod.FetchToml();

            modPropertyGrid.SelectedObject = toml;
            //if (toml.Count >= 3)
            //{
            //    modNameDataLbl.Text = toml["display_name"].ToString();
            //    modDescriptionBox.Text = toml["description"].ToString();
            //    versionDataLbl.Text = toml["version"].ToString();
            //}
            //else
            //{
            //    modNameDataLbl.Text = modList.Items[modList.SelectedIndex].ToString();
            //    modDescriptionBox.Text = "None";
            //    versionDataLbl.Text = "Unknown";
            //}

        }

        private void ClearMetaData()
        {
            modPropertyGrid.SelectedObject = null;
        }

        #region UTIL
        private void ThreadSafe(MethodInvoker method)
        {
            if (InvokeRequired)
                Invoke(method);
            else
                method();
        }
        #endregion

        #region UI_HANDLERS
        private void addPackedMod_Click(object sender, EventArgs e)
        {
            var openFileDialog = new OpenFileDialog()
            {
                Filter = "Zip Files (*.zip)|*.zip|Rar Files (*.rar)|*.rar|All (*.*)|*.*"
            };
            if (openFileDialog.ShowDialog() != DialogResult.OK) return;

            var mod = new PackedModEntry()
            {
                ModPath = openFileDialog.FileName,
            };

            AddModToList(mod);
        }
        private void addFolderMod_Click(object sender, EventArgs e)
        {
            var folderBrowserDialog = new FolderBrowserDialog();
            if (folderBrowserDialog.ShowDialog() != DialogResult.OK) return;

            var mod = new FolderModEntry()
            {
                ModPath = folderBrowserDialog.SelectedPath,
            };

            AddModToList(mod);
        }

        private void modList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (modList.SelectedIndex >= 0)
            {
                PopulateMetaData();
            }
        }

        private void applyModsBut_Click(object sender, EventArgs e)
        {
            if (modPack == null)
            {
                MessageBox.Show("No Modpack Loaded");
            }

            string layeredFSLocation = Path.Join(modPackLocation, ModPack.romfsRel);

            if (Directory.Exists(layeredFSLocation))
                Directory.Delete(layeredFSLocation, true);

            Directory.CreateDirectory(layeredFSLocation);

            for (int i = 0; i < modList.Items.Count; i++)
            {
                var mod = modPack.mods[i];
                if (mod.IsEnabled)
                    ApplyMod(mod, layeredFSLocation);
                else
                    RemoveMod(mod);
            }

            SerializeTRPFD(Path.Join(layeredFSLocation, FilepathSettings.trpfdRel));

            //TODO: Make a Mod Merger class that enumerates file conflicts/merges
            MessageBox.Show("Mods Applied!");

            if (openModWindowMenuItem.Checked == true)
            {
                var filePath = Path.GetFullPath(modPackLocation);
                Process.Start("explorer.exe", string.Format("\"{0}\"", filePath));
            }
        }
        private void modOrderUp_Click(object sender, EventArgs e)
        {
            if (modList.SelectedIndex < 0 || modList.SelectedIndex == 0) return;
            MoveMod(modList.SelectedIndex, modList.SelectedIndex - 1);
        }
        private void modOrderDown_Click(object sender, EventArgs e)
        {
            if (modList.SelectedIndex < 0 || modList.SelectedIndex >= modList.Items.Count - 1) return;
            MoveMod(modList.SelectedIndex, modList.SelectedIndex + 1);
        }
        private void deleteModButton_Click(object sender, EventArgs e)
        {
            modPack.mods.RemoveAt(modList.SelectedIndex);
            modList.Items.Remove(modList.Items[modList.SelectedIndex]);
            ClearMetaData();
        }
        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new About().ShowDialog();
        }

        private void openRomFSFolderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var folderBrowser = new FolderBrowserDialog();
            if (folderBrowser.ShowDialog() != DialogResult.OK) return;

            ModLoaderSettings.SetRomFSPath(folderBrowser.SelectedPath);

            if (ParseFileDescriptor(Path.Join(ModLoaderSettings.GetRomFSPath(), FilepathSettings.trpfdRel)) != DialogResult.OK)
            {
                return;
            }

            ModLoaderSettings.Save();
        }

        private void newModPackMenuItem_Click(object sender, EventArgs e)
        {
            var folderBrowserDialog = new FolderBrowserDialog() { ShowNewFolderButton = true };
            if (folderBrowserDialog.ShowDialog() != DialogResult.OK) return;

            if (File.Exists(folderBrowserDialog.SelectedPath + ModPack.settingsRel))
            {
                var dialogResult = MessageBox.Show("A ModPack already exists in this folder, would you like to load it?", "ModPack found", MessageBoxButtons.YesNo);

                if (dialogResult == DialogResult.Yes)
                {
                    LoadModPack(folderBrowserDialog.SelectedPath);
                }
                else
                {
                    return;
                }
            }

            else
            {
                CreateModPack(folderBrowserDialog.SelectedPath);
            }

        }

        private void ChooseModPackMenuItem_Click(object sender, EventArgs e)
        {
            var folderBrowserDialog = new FolderBrowserDialog();
            if (folderBrowserDialog.ShowDialog() != DialogResult.OK) return;

            if (!File.Exists(folderBrowserDialog.SelectedPath + ModPack.settingsRel))
            {
                var dialogResult = MessageBox.Show("No ModPack exists in this folder, would you like to create one?", "No ModPack found", MessageBoxButtons.YesNo);

                if (dialogResult == DialogResult.Yes)
                {
                    CreateModPack(folderBrowserDialog.SelectedPath);
                }
                else
                {
                    return;
                }
            }

            LoadModPack(folderBrowserDialog.SelectedPath);
        }

        private void TrinityMainWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (modPack != null)
            {
                modPack.Save(modPackLocation);
                ModLoaderSettings.AddRecentModPack(modPackLocation);
            }
            ModLoaderSettings.SetOpenModWindow(openModWindowMenuItem.Checked);
            ModLoaderSettings.Save();

        }

        private void modList_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            var itemCheck = !modList.GetItemChecked(e.Index);
            modPack.mods[e.Index].IsEnabled = itemCheck;
        }

        private void modList_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                modList.SelectedIndex = modList.IndexFromPoint(e.Location);
                if (modList.SelectedIndex != -1)
                {
                    basicContext.Show(modList, e.Location);
                }
            }
        }

        private void openModWindowMenuItem_Click(object sender, EventArgs e)
        {
            openModWindowMenuItem.Checked = !openModWindowMenuItem.Checked;
        }
    }
    #endregion
}