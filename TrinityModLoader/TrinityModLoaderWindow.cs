using Trinity.Core.Utils;
using Trinity.Core.Math.Hash;
using Trinity.Core.Flatbuffers.TR.ResourceDictionary;
using System.Diagnostics;
using System.Text.Json;
using TrinityModLoader.Models.ModEntry;
using TrinityModLoader.Models.Settings;
using TrinityModLoader.Models.ModPack;

namespace TrinityModLoader
{
    public partial class TrinityModLoaderWindow : Form
    {
        public static string titleText = "Trinity Mod Loader";
        public string modPackLocation = String.Empty;

        #region Window Logic

        public TrinityModLoaderWindow()
        {
            InitializeSettings();
            InitializeComponent();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            InitializeLastModpacks();
            openModWindowMenuItem.Checked = ModLoaderSettings.GetOpenModWindow();
        }

        private void TrinityMainWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (modList.ModPack != null)
            {
                modList.SaveModPack(modPackLocation);
                ModLoaderSettings.AddRecentModPack(modPackLocation);
            }
            ModLoaderSettings.SetOpenModWindow(openModWindowMenuItem.Checked);
            ModLoaderSettings.Save();
        }

        private void InitializeSettings()
        {
            ModLoaderSettings.Open();

            var romfsDir = ModLoaderSettings.GetRomFSPath();

            if (string.IsNullOrEmpty(romfsDir) && Directory.Exists(romfsDir) && CheckRomFS())
            {
                return;
            }

            MessageBox.Show("Please set your RomFS path.", "Missing RomFS");

            var folderBrowser = new FolderBrowserDialog();
            if (folderBrowser.ShowDialog() != DialogResult.OK)
            {
                NoValidRomFS();
                return;
            }

            ModLoaderSettings.SetRomFSPath(folderBrowser.SelectedPath);

            if (!CheckRomFS())
            {
                NoValidRomFS();
            }
                
            ModLoaderSettings.Save();

        }

        private bool CheckRomFS()
        {
            var trpfd_dir = Path.Join(ModLoaderSettings.GetRomFSPath(), FilepathSettings.trpfdRel);
            if (!File.Exists(trpfd_dir))
            {
                MessageBox.Show("No TRPFD found in RomFS folder.");
                return false;
            }

            CustomFileDescriptor? customFileDescriptor = null;

            if (TryLoadFileDescriptor(trpfd_dir, out customFileDescriptor) == false)
            {
                MessageBox.Show("Invalid TRPFD found in RomFS folder.\n");
                return false;
            }

            if (customFileDescriptor.HasUnusedFiles())
            {
                MessageBox.Show("This is a modified TRPFD.\n Please provide an unmodified TRPFD.");
                return false;
            }

            return true;
        }

        private void InitializeLastModpacks()
        {
            var recentModPacks = ModLoaderSettings.GetRecentModPacks();

            if (recentModPacks == null || recentModPacks.Count == 0)
            {
                this.Text = $"{titleText} - No Modpack Loaded";
                RefreshUIState();
                return;
            }

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

                    openModPackMenuItems.DropDownItems.Add(modMenuItem);
                }
            }

            if (recentModPacks == null || recentModPacks.Count == 0)
            {
                this.Text = $"{titleText} - No Modpack Loaded";
                RefreshUIState();
                return;
            }

            LoadModPack(recentModPacks.Last());
        }

        private void DisableUIState()
        {
            modList.Enabled = false;
            addFolderMod.Enabled = false;
            addPackedMod.Enabled = false;
            applyModsBut.Enabled = false;
            enableAllButton.Enabled = false;
            disableAllButton.Enabled = false;
            modOrderUpButton.Enabled = false;
            modOrderDownButton.Enabled = false;
            refreshModButton.Enabled = false;
        }
 
        private void RefreshUIState()
        {
            DisableUIState();

            if (modList.ModPack == null)
            {
                return;
            }

            modList.Enabled = true;
            addFolderMod.Enabled = true;
            addPackedMod.Enabled = true;

            if (modList.ModPack.mods.Count > 0)
            {
                applyModsBut.Enabled = true;
                enableAllButton.Enabled = true;
                disableAllButton.Enabled = true;
                modOrderUpButton.Enabled = true;
                modOrderDownButton.Enabled = true;
            }

            if (modList.SelectedIndex >= 0)
            {
                refreshModButton.Enabled = true;
            }
        }

        #endregion

        #region Utils

        private void NoValidRomFS()
        {
            MessageBox.Show("Trinity Mod Loader requires a valid RomFS path to function.");
            this.Close();
        }

        private void LoadModPack(string path)
        {
            modPackLocation = path;
            if (string.IsNullOrEmpty(modPackLocation))
            {
                MessageBox.Show("Failed to load ModPack settings.");
                return;
            }

            ModPack modPack;
            try
            {
                modPack = JsonSerializer.Deserialize<ModPack>(File.ReadAllText(Path.Join(path, ModPack.settingsRel)));
            }
            catch
            {
                MessageBox.Show("Failed to load ModPack settings.");
                return;
            }

            modList.ModPack = modPack;

            ModLoaderSettings.AddRecentModPack(path);

            RefreshUIState();
            this.Text = $"{titleText} - {modPackLocation}";
        }

        private void CreateModPack(string path)
        {
            modPackLocation = path;

            ModPack modPack = new ModPack()
            {
                mods = new List<IModEntry>(),
            };

            modList.ModPack = modPack;
            modList.SaveModPack(path);

            this.Text = $"{titleText} - {modPackLocation}";
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

        private void AddModToList(IModEntry mod)
        {
            if (modList.Items.Contains(mod.ModPath))
            {
                var ow = MessageBox.Show("Mod already exists, do you want to overwrite?", "Mod exists", MessageBoxButtons.YesNo);
                if (ow == DialogResult.No) return;
                modList.DeleteMod(modList.Items.IndexOf(mod.ModPath));
            }
            modList.AddMod(mod);
        }

        private void ApplyMod(IModEntry mod, string lfsDir, CustomFileDescriptor customFileDescriptor)
        {
            mod.Extract(lfsDir);

            foreach (var f in mod.FetchFiles())
            {
                var fhash = GFFNV.Hash(f);
                lock (customFileDescriptor)
                {
                    customFileDescriptor?.RemoveFile(fhash);
                }
            }
        }

        private bool ApplyMods()
        {
            CustomFileDescriptor? customFileDescriptor = null;

            if (TryLoadFileDescriptor(Path.Join(ModLoaderSettings.GetRomFSPath(), FilepathSettings.trpfdRel), out customFileDescriptor) == false)
            {
                return false;
            }

            string layeredFSLocation = Path.Join(modPackLocation, ModPack.romfsRel);

            if (Directory.Exists(layeredFSLocation))
                Directory.Delete(layeredFSLocation, true);

            Directory.CreateDirectory(layeredFSLocation);

            foreach (int i in modList.CheckedIndices)
            {
                IModEntry mod = modList.ModPack.mods[i];
                if (mod == null) continue;
                ApplyMod(mod, layeredFSLocation, customFileDescriptor);

            }

            SerializeTRPFD(Path.Join(layeredFSLocation, FilepathSettings.trpfdRel), customFileDescriptor);

            return true;
        }

        void SerializeTRPFD(string fileOut, CustomFileDescriptor customFileDescriptor)
        {
            var file = new System.IO.FileInfo(fileOut);
            if (!file.Directory.Exists) file.Directory.Create();

            var trpfd = FlatBufferConverter.SerializeFrom<CustomFileDescriptor>(customFileDescriptor);
            File.WriteAllBytes(fileOut, trpfd);
        }

        private void RefreshModView()
        {
            if (modList.SelectedIndex < 0 || modList.SelectedIndex >= modList.Items.Count)
            {
                ClearMetaData();
                fileView.Items.Clear();
            }
            else
            {
                PopulateMetaData();
                PopulateFileData();
            }
            RefreshUIState();
        }

        private void PopulateMetaData()
        {
            var mod = modList.ModPack.mods[modList.SelectedIndex];
            var modData = mod.FetchModData();

            if (modData != null)
            {
                ModNameLabel.Text = modData.display_name.ToString();
                ModAuthorLabel.Text = modData.author_name.ToString();
                ModVersionLabel.Text = modData.version.ToString();
                ModPathLabel.Text = mod.ModPath.ToString();
                ModDescriptionBox.Text = modData.description.ToString();
            }
        }

        private void ClearMetaData()
        {
            ModAuthorLabel.Text = null;
            ModNameLabel.Text = null;
            ModPathLabel.Text = null;
            ModDescriptionBox.Text = null;
            ModVersionLabel.Text = null; // Added clearing version label
        }

        private void PopulateFileData()
        {

            fileView.BeginUpdate();
            fileView.Items.Clear();

            var mod = modList.ModPack.mods[modList.SelectedIndex];
            var files = mod.FetchFiles();

            foreach (string file in files)
            {
                fileView.Items.Add(file);
            }
            fileView.EndUpdate();
        }

        #endregion

        #region UI/Event Handlers

        private async void applyModsBut_Click(object sender, EventArgs e)
        {
            if (modList.ModPack == null)
            {
                MessageBox.Show("No Modpack Loaded");
                return;
            }

            DisableUIState();

            try
            {
                bool success = await Task.Run(() => ApplyMods());

                if (!success)
                {
                    MessageBox.Show("Mods could not be applied.");
                }
                else
                {
                    MessageBox.Show("Mods Applied!");

                    if (openModWindowMenuItem.Checked)
                    {
                        var filePath = Path.GetFullPath(modPackLocation);
                        Process.Start("explorer.exe", string.Format("\"{0}\"", filePath));
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}");
            }
            finally
            {
                RefreshUIState();
            }
        }

        private void addPackedMod_Click(object sender, EventArgs e)
        {
            if (modList.ModPack == null)
            {
                MessageBox.Show("No Modpack Loaded");
                return;
            }

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
            if (modList.ModPack == null)
            {
                MessageBox.Show("No Modpack Loaded");
                return;
            }

            var folderBrowserDialog = new FolderBrowserDialog();
            if (folderBrowserDialog.ShowDialog() != DialogResult.OK) return;

            var mod = new FolderModEntry()
            {
                ModPath = folderBrowserDialog.SelectedPath,
            };

            AddModToList(mod);
        }

        private void modOrderUp_Click(object sender, EventArgs e)
        {
            if (modList.SelectedIndex <= 0) return;
            modList.MoveMod(modList.SelectedIndex, modList.SelectedIndex - 1);
            RefreshModView();
        }

        private void modOrderDown_Click(object sender, EventArgs e)
        {
            if (modList.SelectedIndex < 0 || modList.SelectedIndex >= modList.Items.Count - 1) return;
            modList.MoveMod(modList.SelectedIndex, modList.SelectedIndex + 1);
            RefreshModView();
        }

        private void deleteModButton_Click(object sender, EventArgs e)
        {
            if (modList.SelectedIndex < 0) return;
            modList.DeleteMod(modList.SelectedIndex);
            RefreshModView();
        }

        private void modList_SelectedIndexChanged(object sender, EventArgs e)
        {
            RefreshModView();
        }

        private void modList_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                modList.SelectedIndex = modList.IndexFromPoint(e.Location);
                if (modList.SelectedIndex != -1)
                {
                    listContext.Show(modList, e.Location);
                }
            }
        }

        private void newModPackMenuItem_Click(object sender, EventArgs e)
        {
            var folderBrowserDialog = new FolderBrowserDialog() { ShowNewFolderButton = true };
            if (folderBrowserDialog.ShowDialog() != DialogResult.OK) return;

            if (File.Exists(Path.Join(folderBrowserDialog.SelectedPath, ModPack.settingsRel)))
            {
                var dialogResult = MessageBox.Show("A ModPack already exists in this folder, would you like to load it?", "ModPack found", MessageBoxButtons.YesNo);
                if (dialogResult != DialogResult.Yes) return;
                LoadModPack(folderBrowserDialog.SelectedPath);
                return;
            }

            CreateModPack(folderBrowserDialog.SelectedPath);
        }

        private void chooseModPackMenuItem_Click(object sender, EventArgs e)
        {
            var folderBrowserDialog = new FolderBrowserDialog();
            if (folderBrowserDialog.ShowDialog() != DialogResult.OK) return;

            if (!File.Exists(Path.Join(folderBrowserDialog.SelectedPath, ModPack.settingsRel)))
            {
                var dialogResult = MessageBox.Show("No ModPack exists in this folder, would you like to create one?", "No ModPack found", MessageBoxButtons.YesNo);
                if (dialogResult != DialogResult.Yes) return;
                CreateModPack(folderBrowserDialog.SelectedPath);
            }

            LoadModPack(folderBrowserDialog.SelectedPath);
        }

        private void openRomFSFolderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var folderBrowser = new FolderBrowserDialog();
            if (folderBrowser.ShowDialog() != DialogResult.OK) return;

            ModLoaderSettings.SetRomFSPath(folderBrowser.SelectedPath);

            if (!CheckRomFS())
            {
                NoValidRomFS();
                return;
            }

            ModLoaderSettings.Save();
        }

        private void openModWindowMenuItem_Click(object sender, EventArgs e)
        {
            openModWindowMenuItem.Checked = !openModWindowMenuItem.Checked;
        }

        private void refreshModButton_Click(object sender, EventArgs e)
        {
            RefreshModView();
        }

        private void enableAllButton_Click(object sender, EventArgs e)
        {
            modList.EnableAll(true);
        }

        private void disableAllButton_Click(object sender, EventArgs e)
        {
            modList.EnableAll(false);
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new About().ShowDialog();
        }

        #endregion
    }
}