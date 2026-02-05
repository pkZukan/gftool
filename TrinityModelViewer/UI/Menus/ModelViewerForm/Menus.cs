using GFTool.Renderer;
using GFTool.Renderer.Core;
using GFTool.Renderer.Core.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Trinity.Core.Assets;
using Trinity.Core.Cache;

namespace TrinityModelViewer
{
    public partial class ModelViewerForm
    {
        private void AddSettingsMenu()
        {
            var settingsMenu = new ToolStripMenuItem("Settings");
            settingsMenu.Click += (s, e) => OpenSettings();
            int viewIndex = menuStrip1.Items.IndexOf(viewToolStripMenuItem);
            if (viewIndex >= 0 && viewIndex < menuStrip1.Items.Count - 1)
            {
                menuStrip1.Items.Insert(viewIndex + 1, settingsMenu);
            }
            else
            {
                menuStrip1.Items.Add(settingsMenu);
            }

            var helpMenu = new ToolStripMenuItem("Help");
            helpMenu.Click += (s, e) => OpenHelp();
            int settingsIndex = menuStrip1.Items.IndexOf(settingsMenu);
            if (settingsIndex >= 0 && settingsIndex < menuStrip1.Items.Count - 1)
            {
                menuStrip1.Items.Insert(settingsIndex + 1, helpMenu);
            }
            else
            {
                menuStrip1.Items.Add(helpMenu);
            }
        }

        private void AddToolsMenu()
        {
            if (toolsToolStripMenuItem != null)
            {
                return;
            }

            toolsToolStripMenuItem = new ToolStripMenuItem("Tools");

            var gfpakMenu = new ToolStripMenuItem("GFPAK Hash Cache");
            var importHashList = new ToolStripMenuItem("Import Hash List (.txt)...");
            importHashList.Click += (s, e) => ImportGfpakHashList();
            var reloadCache = new ToolStripMenuItem("Reload Cache (GFPAKHashCache.bin)");
            reloadCache.Click += (s, e) =>
            {
                try
                {
                    GFPakHashCache.Open();
                    MessageBox.Show(this, $"GFPAKHashCache.bin loaded. ({GFPakHashCache.Count} entries)", "GFPAK Hash Cache",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, $"Failed to load GFPAKHashCache.bin:\n{ex.Message}", "GFPAK Hash Cache",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            gfpakMenu.DropDownItems.Add(importHashList);
            gfpakMenu.DropDownItems.Add(reloadCache);

            toolsToolStripMenuItem.DropDownItems.Add(gfpakMenu);

            int fileIndex = menuStrip1.Items.IndexOf(fileToolStripMenuItem);
            if (fileIndex >= 0 && fileIndex < menuStrip1.Items.Count - 1)
            {
                menuStrip1.Items.Insert(fileIndex + 1, toolsToolStripMenuItem);
            }
            else
            {
                menuStrip1.Items.Add(toolsToolStripMenuItem);
            }
        }

        private void AddGfpakMenuItems()
        {
            if (openGfpakToolStripMenuItem != null)
            {
                return;
            }

            openGfpakToolStripMenuItem = new ToolStripMenuItem("Open GFPAK...");
            openGfpakToolStripMenuItem.Click += async (s, e) => await OpenGfpakFromDialogAsync();

            int insertIndex = fileToolStripMenuItem.DropDownItems.IndexOf(importToolStripMenuItem);
            if (insertIndex >= 0)
            {
                fileToolStripMenuItem.DropDownItems.Insert(insertIndex + 1, openGfpakToolStripMenuItem);
            }
            else
            {
                fileToolStripMenuItem.DropDownItems.Add(openGfpakToolStripMenuItem);
            }
        }

        private void AddTrinityExportMenuItems()
        {
            if (exportTrinityToolStripMenuItem != null)
            {
                return;
            }

            exportTrinityToolStripMenuItem = new ToolStripMenuItem("Export Trinity...");
            exportTrinityToolStripMenuItem.Click += (s, e) => ExportTrinityFromSelection();

            exportTrinityPatchToolStripMenuItem = new ToolStripMenuItem("Export Trinity (Edited Only)...");
            exportTrinityPatchToolStripMenuItem.Click += (s, e) => ExportTrinityPatchFromSelection();

            int insertIndex = -1;
            if (lastModelToolStripMenuItem != null)
            {
                insertIndex = fileToolStripMenuItem.DropDownItems.IndexOf(lastModelToolStripMenuItem);
            }
            if (insertIndex < 0 && recentModelsToolStripMenuItem != null)
            {
                insertIndex = fileToolStripMenuItem.DropDownItems.IndexOf(recentModelsToolStripMenuItem);
            }
            if (insertIndex < 0 && openGfpakToolStripMenuItem != null)
            {
                insertIndex = fileToolStripMenuItem.DropDownItems.IndexOf(openGfpakToolStripMenuItem);
            }
            if (insertIndex < 0)
            {
                insertIndex = fileToolStripMenuItem.DropDownItems.IndexOf(importToolStripMenuItem);
            }

            if (insertIndex >= 0)
            {
                int next = insertIndex + 1;
                fileToolStripMenuItem.DropDownItems.Insert(next, new ToolStripSeparator());
                fileToolStripMenuItem.DropDownItems.Insert(next + 1, exportTrinityToolStripMenuItem);
                fileToolStripMenuItem.DropDownItems.Insert(next + 2, exportTrinityPatchToolStripMenuItem);
            }
            else
            {
                fileToolStripMenuItem.DropDownItems.Add(new ToolStripSeparator());
                fileToolStripMenuItem.DropDownItems.Add(exportTrinityToolStripMenuItem);
                fileToolStripMenuItem.DropDownItems.Add(exportTrinityPatchToolStripMenuItem);
            }
        }

        private void AddSkinningMenuItems()
        {
            if (skinningToolStripMenuItem != null)
            {
                return;
            }

            skinningToolStripMenuItem = new ToolStripMenuItem("Skinning");

            deterministicSkinningToolStripMenuItem = new ToolStripMenuItem("Deterministic skinning/animation (no heuristics)")
            {
                CheckOnClick = true,
                Checked = settings.DeterministicSkinningAndAnimation
            };
            deterministicSkinningToolStripMenuItem.CheckedChanged += (s, e) =>
            {
                settings.DeterministicSkinningAndAnimation = deterministicSkinningToolStripMenuItem.Checked;
                settings.Save();
                ApplyRenderSettings();
                ReloadLoadedModels();
            };

            var autoMap = new ToolStripMenuItem("Auto map blend indices") { CheckOnClick = true, Checked = settings.AutoMapBlendIndices };
            autoMap.CheckedChanged += (s, e) =>
            {
                settings.AutoMapBlendIndices = autoMap.Checked;
                settings.Save();
                ApplyRenderSettings();
                ReloadLoadedModels();
            };

            var mapViaJointInfo = new ToolStripMenuItem("Remap indices via joint info") { CheckOnClick = true, Checked = settings.MapBlendIndicesViaJointInfo };
            mapViaJointInfo.CheckedChanged += (s, e) =>
            {
                settings.MapBlendIndicesViaJointInfo = mapViaJointInfo.Checked;
                settings.Save();
                ApplyRenderSettings();
                ReloadLoadedModels();
            };

            var useTrsklInvBind = new ToolStripMenuItem("Use TRSKL inverse binds") { CheckOnClick = true, Checked = settings.UseTrsklInverseBind };
            useTrsklInvBind.CheckedChanged += (s, e) =>
            {
                settings.UseTrsklInverseBind = useTrsklInvBind.Checked;
                settings.Save();
                ApplyRenderSettings();
                ReloadLoadedModels();
            };

            var swapBlendOrder = new ToolStripMenuItem("Swap blend order (WXYZ)") { CheckOnClick = true, Checked = settings.SwapBlendOrder };
            swapBlendOrder.CheckedChanged += (s, e) =>
            {
                settings.SwapBlendOrder = swapBlendOrder.Checked;
                settings.Save();
                ApplyRenderSettings();
                renderCtrl.Invalidate();
            };

            var transposeSkinMatrices = new ToolStripMenuItem("Transpose skin matrices") { CheckOnClick = true, Checked = settings.TransposeSkinMatrices };
            transposeSkinMatrices.CheckedChanged += (s, e) =>
            {
                settings.TransposeSkinMatrices = transposeSkinMatrices.Checked;
                settings.Save();
                ApplyRenderSettings();
                renderCtrl.Invalidate();
            };

            var reloadModels = new ToolStripMenuItem("Reload loaded models");
            reloadModels.Click += (s, e) => ReloadLoadedModels();

            skinningToolStripMenuItem.DropDownItems.Add(deterministicSkinningToolStripMenuItem);
            skinningToolStripMenuItem.DropDownItems.Add(new ToolStripSeparator());
            skinningToolStripMenuItem.DropDownItems.Add(autoMap);
            skinningToolStripMenuItem.DropDownItems.Add(mapViaJointInfo);
            skinningToolStripMenuItem.DropDownItems.Add(useTrsklInvBind);
            skinningToolStripMenuItem.DropDownItems.Add(new ToolStripSeparator());
            skinningToolStripMenuItem.DropDownItems.Add(swapBlendOrder);
            skinningToolStripMenuItem.DropDownItems.Add(transposeSkinMatrices);
            skinningToolStripMenuItem.DropDownItems.Add(new ToolStripSeparator());
            skinningToolStripMenuItem.DropDownItems.Add(reloadModels);

            viewToolStripMenuItem.DropDownItems.Add(new ToolStripSeparator());
            viewToolStripMenuItem.DropDownItems.Add(skinningToolStripMenuItem);
        }

        private void AddShaderDevMenuItems()
        {
            if (useBackupIkCharacterShaderToolStripMenuItem != null)
            {
                return;
            }

            useBackupIkCharacterShaderToolStripMenuItem = new ToolStripMenuItem("Use backup IkCharacter shader")
            {
                CheckOnClick = true,
                Checked = settings.UseBackupIkCharacterShader
            };

            useBackupIkCharacterShaderToolStripMenuItem.CheckedChanged += (s, e) =>
            {
                settings.UseBackupIkCharacterShader = useBackupIkCharacterShaderToolStripMenuItem.Checked;
                settings.Save();
                ApplyRenderSettings();

                ShaderPool.Instance.Invalidate("IkCharacter");
                ReloadLoadedModels();
            };

            viewToolStripMenuItem.DropDownItems.Add(useBackupIkCharacterShaderToolStripMenuItem);

            perfHudToolStripMenuItem = new ToolStripMenuItem("Performance HUD")
            {
                CheckOnClick = true,
                Checked = settings.EnablePerfHud
            };
            perfHudToolStripMenuItem.CheckedChanged += (s, e) =>
            {
                settings.EnablePerfHud = perfHudToolStripMenuItem.Checked;
                settings.Save();
                ApplyRenderSettings();
                if (perfHudLabel != null)
                {
                    perfHudLabel.Visible = settings.EnablePerfHud;
                }
            };

            perfSpikeLogToolStripMenuItem = new ToolStripMenuItem("Log performance spikes")
            {
                CheckOnClick = true,
                Checked = settings.EnablePerfSpikeLog
            };
            perfSpikeLogToolStripMenuItem.CheckedChanged += (s, e) =>
            {
                settings.EnablePerfSpikeLog = perfSpikeLogToolStripMenuItem.Checked;
                settings.Save();
                ApplyRenderSettings();
            };

            vsyncToolStripMenuItem = new ToolStripMenuItem("VSync")
            {
                CheckOnClick = true,
                Checked = settings.EnableVsync
            };
            vsyncToolStripMenuItem.CheckedChanged += (s, e) =>
            {
                settings.EnableVsync = vsyncToolStripMenuItem.Checked;
                settings.Save();
                ApplyRenderSettings();
            };

            viewToolStripMenuItem.DropDownItems.Add(new ToolStripSeparator());
            viewToolStripMenuItem.DropDownItems.Add(perfHudToolStripMenuItem);
            viewToolStripMenuItem.DropDownItems.Add(perfSpikeLogToolStripMenuItem);
            viewToolStripMenuItem.DropDownItems.Add(vsyncToolStripMenuItem);
        }

        private void ImportGfpakHashList()
        {
            using var ofd = new OpenFileDialog();
            ofd.Filter = "Hash list (*.txt)|*.txt|All files (*.*)|*.*";
            if (ofd.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            try
            {
                GFPakHashCache.Open();
                var lines = File.ReadAllLines(ofd.FileName).ToList();
                GFPakHashCache.AddHashFromList(lines);
                GFPakHashCache.Save();
                MessageBox.Show(this, $"GFPAKHashCache.bin updated. ({GFPakHashCache.Count} entries)", "GFPAK Hash Cache",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Failed to import hash list:\n{ex.Message}", "GFPAK Hash Cache",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task OpenGfpakFromDialogAsync()
        {
            using var ofd = new OpenFileDialog();
            ofd.Filter = "GFPAK (*.gfpak)|*.gfpak|All files (*.*)|*.*";
            if (ofd.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            await OpenGfpakAsync(ofd.FileName);
        }

        private async Task OpenGfpakAsync(string gfpakPath)
        {
            if (string.IsNullOrWhiteSpace(gfpakPath))
            {
                return;
            }

            IAssetProvider provider;
            try
            {
                provider = new GfpakAssetProvider(gfpakPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Failed to open GFPAK:\n{ex.Message}", "GFPAK", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string? modelPath = null;
            if (settings.AutoLoadFirstGfpakModel)
            {
                modelPath = FindFirstTrmdlPath(provider);
            }

            if (string.IsNullOrWhiteSpace(modelPath))
            {
                using var browser = new GfpakBrowserForm(provider);
                if (browser.ShowDialog(this) != DialogResult.OK || string.IsNullOrWhiteSpace(browser.SelectedModelPath))
                {
                    provider.Dispose();
                    return;
                }

                modelPath = browser.SelectedModelPath;
            }

            BeginModelLoad();
            try
            {
                ClearAll();
                var mdl = await AddModelToSceneAsync(modelPath!, provider);
                if (mdl != null)
                {
                    TryAutoLoadAnimationsFromGfpak(provider);
                }
                else
                {
                    provider.Dispose();
                }
            }
            catch (DllNotFoundException ex)
            {
                provider.Dispose();
                MessageBox.Show(this,
                    $"This GFPAK entry appears to require Oodle decompression.\n\nPlace `oo2core_8_win64.dll` next to the executable, then try again.\n\n{ex.Message}",
                    "Missing Oodle", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                provider.Dispose();
                MessageBox.Show(this, $"Failed to load model from GFPAK:\n{ex.Message}", "GFPAK", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                EndModelLoad();
            }
        }

        private static string? FindFirstTrmdlPath(IAssetProvider provider)
        {
            return provider.EnumerateEntries()
                .Select(e => e.Path)
                .Where(p => !string.IsNullOrWhiteSpace(p) && p.EndsWith(".trmdl", StringComparison.OrdinalIgnoreCase))
                .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault();
        }

        private void AddLastModelMenu()
        {
            lastModelToolStripMenuItem = new ToolStripMenuItem("Last Model");
            lastModelToolStripMenuItem.Click += async (s, e) => await OpenLastModelAsync();

            int insertIndex = recentModelsToolStripMenuItem != null
                ? fileToolStripMenuItem.DropDownItems.IndexOf(recentModelsToolStripMenuItem)
                : -1;
            if (insertIndex < 0 && openGfpakToolStripMenuItem != null)
            {
                insertIndex = fileToolStripMenuItem.DropDownItems.IndexOf(openGfpakToolStripMenuItem);
            }
            if (insertIndex < 0)
            {
                insertIndex = fileToolStripMenuItem.DropDownItems.IndexOf(importToolStripMenuItem);
            }
            if (insertIndex >= 0)
            {
                fileToolStripMenuItem.DropDownItems.Insert(insertIndex + 1, lastModelToolStripMenuItem);
            }
            else
            {
                fileToolStripMenuItem.DropDownItems.Add(lastModelToolStripMenuItem);
            }

            UpdateLastModelMenu();
        }

        private void AddRecentModelsMenu()
        {
            recentModelsToolStripMenuItem = new ToolStripMenuItem("Recents");
            int insertIndex = openGfpakToolStripMenuItem != null
                ? fileToolStripMenuItem.DropDownItems.IndexOf(openGfpakToolStripMenuItem)
                : -1;
            if (insertIndex < 0)
            {
                insertIndex = fileToolStripMenuItem.DropDownItems.IndexOf(importToolStripMenuItem);
            }
            if (insertIndex >= 0)
            {
                fileToolStripMenuItem.DropDownItems.Insert(insertIndex + 1, new ToolStripSeparator());
                fileToolStripMenuItem.DropDownItems.Insert(insertIndex + 2, recentModelsToolStripMenuItem);
            }
            else
            {
                fileToolStripMenuItem.DropDownItems.Add(new ToolStripSeparator());
                fileToolStripMenuItem.DropDownItems.Add(recentModelsToolStripMenuItem);
            }

            UpdateRecentModelsMenu();
        }

        private void UpdateRecentModelsMenu()
        {
            if (recentModelsToolStripMenuItem == null)
            {
                return;
            }

            recentModelsToolStripMenuItem.DropDownItems.Clear();

            var recents = settings.RecentModelPaths
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(5)
                .ToList();

            if (recents.Count == 0)
            {
                recentModelsToolStripMenuItem.Enabled = false;
                recentModelsToolStripMenuItem.DropDownItems.Add(new ToolStripMenuItem("(empty)") { Enabled = false });
                return;
            }

            recentModelsToolStripMenuItem.Enabled = true;

            for (int i = 0; i < recents.Count; i++)
            {
                string path = recents[i];
                string label = $"{i + 1}. {Path.GetFileName(path)}";
                var item = new ToolStripMenuItem(label)
                {
                    ToolTipText = path
                };
                item.Click += async (s, e) => await OpenRecentModelAsync(path);
                recentModelsToolStripMenuItem.DropDownItems.Add(item);
            }

            recentModelsToolStripMenuItem.DropDownItems.Add(new ToolStripSeparator());
            var clearItem = new ToolStripMenuItem("Clear Recents");
            clearItem.Click += (s, e) =>
            {
                settings.RecentModelPaths.Clear();
                settings.Save();
                UpdateRecentModelsMenu();
            };
            recentModelsToolStripMenuItem.DropDownItems.Add(clearItem);
        }

        private async Task OpenRecentModelAsync(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                settings.RecentModelPaths.RemoveAll(p => string.Equals(p, path, StringComparison.OrdinalIgnoreCase));
                settings.Save();
                UpdateRecentModelsMenu();
                MessageBox.Show(this, $"File not found:\n{path}", "Recents", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            BeginModelLoad();
            try
            {
                ClearAll();
                await AddModelToSceneAsync(path);
            }
            finally
            {
                EndModelLoad();
            }
        }

        private void AddRecentModel(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return;
            }

            string full;
            try
            {
                full = Path.GetFullPath(filePath);
            }
            catch
            {
                full = filePath;
            }

            settings.RecentModelPaths.RemoveAll(p => string.Equals(p, full, StringComparison.OrdinalIgnoreCase));
            settings.RecentModelPaths.Insert(0, full);
            if (settings.RecentModelPaths.Count > 5)
            {
                settings.RecentModelPaths.RemoveRange(5, settings.RecentModelPaths.Count - 5);
            }
            settings.Save();
            UpdateRecentModelsMenu();
        }

        private void UpdateLastModelMenu()
        {
            if (lastModelToolStripMenuItem == null)
            {
                return;
            }

            bool hasPath = !string.IsNullOrWhiteSpace(settings.LastModelPath) && File.Exists(settings.LastModelPath);
            lastModelToolStripMenuItem.Enabled = hasPath;
            lastModelToolStripMenuItem.ToolTipText = hasPath ? settings.LastModelPath : "No previous model found";
        }

        private async Task OpenLastModelAsync()
        {
            if (string.IsNullOrWhiteSpace(settings.LastModelPath) || !File.Exists(settings.LastModelPath))
            {
                UpdateLastModelMenu();
                return;
            }

            BeginModelLoad();
            try
            {
                ClearAll();
                await AddModelToSceneAsync(settings.LastModelPath);
            }
            finally
            {
                EndModelLoad();
            }
        }
    }
}
