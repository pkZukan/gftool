using GFTool.Renderer;
using GFTool.Renderer.Core;
using GFTool.Renderer.Core.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Trinity.Core.Assets;
using Trinity.Core.Cache;

namespace TrinityModelViewer
{
    public partial class ModelViewerForm
    {
        private void OpenSettings()
        {
            using var dialog = new SettingsForm(
                settings.DarkMode,
                settings.LoadAllLods,
                settings.AutoGenerateLodsOnExport,
                settings.ExportModelPcBaseOnExport,
                settings.DebugLogs,
                settings.AutoLoadAnimations,
                settings.AutoLoadFirstGfpakModel,
                settings.ShowMultipleModels,
                settings.ShaderGame,
                settings.EnableExtractedOutFallback,
                settings.ActiveExtractedGame,
                settings.ZaExtractedOutRoot,
                settings.SvExtractedOutRoot);
            ApplyTheme(dialog);
            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            settings.DarkMode = dialog.DarkModeEnabled;
            settings.LoadAllLods = dialog.LoadAllLodsEnabled;
            settings.AutoGenerateLodsOnExport = dialog.AutoGenerateLodsOnExportEnabled;
            settings.ExportModelPcBaseOnExport = dialog.ExportModelPcBaseOnExportEnabled;
            settings.DebugLogs = dialog.DebugLogsEnabled;
            settings.AutoLoadAnimations = dialog.AutoLoadAnimationsEnabled;
            settings.AutoLoadFirstGfpakModel = dialog.AutoLoadFirstGfpakModelEnabled;
            settings.ShowMultipleModels = dialog.ShowMultipleModelsEnabled;
            settings.ShaderGame = dialog.ShaderGameSelection;
            settings.EnableExtractedOutFallback = dialog.ExtractedOutFallbackEnabled;
            settings.ActiveExtractedGame = dialog.ActiveExtractedGameSelection;
            settings.ZaExtractedOutRoot = dialog.ZaExtractedOutRoot;
            settings.SvExtractedOutRoot = dialog.SvExtractedOutRoot;
            settings.Save();
            MessageHandler.Instance.DebugLogsEnabled = settings.DebugLogs;
            ApplyTheme();
            ApplyRenderSettings();
            ApplyModelVisibility(GetSelectedModelFromSceneTree());
            if (renderCtrl?.renderer != null && renderCtrl.renderer.HasActiveAnimation())
            {
                UpdateAnimationTargetsForCurrentMode();
            }
        }

        private void OpenHelp()
        {
            const string message =
                "Controls:\n" +
                "- Right Mouse Drag: Orbit camera\n" +
                "- Ctrl + Right Mouse Drag: Dolly (zoom)\n" +
                "- Left Mouse Drag: Pan camera\n" +
                "- WASD: Move camera\n" +
                "- Q/E: Move down/up\n" +
                "- Shift: Slow movement (0.2x)\n" +
                "- Ctrl: Fast movement (2x)";

            MessageBox.Show(this, message, "Controls", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private async void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Trinity Model files (*.trmdl)|*.trmdl|All files (*.*)|*.*";
            if (ofd.ShowDialog(this) != DialogResult.OK) return;

            BeginModelLoad();
            try
            {
                ClearAll();
                await AddModelToSceneAsync(ofd.FileName);
            }
            finally
            {
                EndModelLoad();
            }
        }

        private async void importToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Trinity Model files (*.trmdl)|*.trmdl|All files (*.*)|*.*";
            ofd.Multiselect = true;
            if (ofd.ShowDialog(this) != DialogResult.OK) return;

            BeginModelLoad();
            try
            {
                foreach (var file in ofd.FileNames.Where(f => !string.IsNullOrWhiteSpace(f)))
                {
                    await AddModelToSceneAsync(file);
                }
            }
            finally
            {
                EndModelLoad();
            }
        }

        private void wireframeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            renderCtrl.renderer.SetWireframe(wireframeToolStripMenuItem.CheckState == CheckState.Checked);
            renderCtrl.Invalidate();
        }

        private void showSkeletonToolStripMenuItem_Click(object sender, EventArgs e)
        {
            settings.ShowSkeleton = showSkeletonToolStripMenuItem.Checked;
            settings.Save();
            ApplyRenderSettings();
        }

        private void ApplyRenderSettingsToMenu()
        {
            shadingLitToolStripMenuItem.Checked = settings.DisplayShading == ViewerSettings.ShadingMode.Lit;
            shadingToonToolStripMenuItem.Checked = settings.DisplayShading == ViewerSettings.ShadingMode.Toon;
            shadingLegacyToolStripMenuItem.Checked = settings.DisplayShading == ViewerSettings.ShadingMode.Legacy;
            displayAllToolStripMenuItem.Checked = string.Equals(settings.DisplayBuffer, "All", StringComparison.OrdinalIgnoreCase);
            displayAlbedoToolStripMenuItem.Checked = string.Equals(settings.DisplayBuffer, "Albedo", StringComparison.OrdinalIgnoreCase);
            displayNormalToolStripMenuItem.Checked = string.Equals(settings.DisplayBuffer, "Normal", StringComparison.OrdinalIgnoreCase);
            displaySpecularToolStripMenuItem.Checked = string.Equals(settings.DisplayBuffer, "Specular", StringComparison.OrdinalIgnoreCase);
            displayAOToolStripMenuItem.Checked = string.Equals(settings.DisplayBuffer, "AO", StringComparison.OrdinalIgnoreCase);
            displayDepthToolStripMenuItem.Checked = string.Equals(settings.DisplayBuffer, "Depth", StringComparison.OrdinalIgnoreCase);
            shaderDebugOffToolStripMenuItem.Checked = settings.ShaderDebugMode == 0;
            shaderDebugIepTextureToolStripMenuItem.Checked = settings.ShaderDebugMode == 1;
            shaderDebugIepLayerMaskToolStripMenuItem.Checked = settings.ShaderDebugMode == 2;
            shaderDebugIepUvToolStripMenuItem.Checked = settings.ShaderDebugMode == 3;
            shaderDebugIepUv01ToolStripMenuItem.Checked = settings.ShaderDebugMode == 4;
            shaderDebugIkLayerMask1ToolStripMenuItem.Checked = settings.ShaderDebugMode == 20;
            shaderDebugIkLayerMask2ToolStripMenuItem.Checked = settings.ShaderDebugMode == 21;
            shaderDebugIkLayerMask3ToolStripMenuItem.Checked = settings.ShaderDebugMode == 22;
            shaderDebugIkLayerMask4ToolStripMenuItem.Checked = settings.ShaderDebugMode == 23;
            showSkeletonToolStripMenuItem.Checked = settings.ShowSkeleton;
            useRareTrmtrMaterialsToolStripMenuItem.Checked = settings.UseRareTrmtrMaterials;
            if (deterministicSkinningToolStripMenuItem != null)
            {
                deterministicSkinningToolStripMenuItem.Checked = settings.DeterministicSkinningAndAnimation;
            }
            if (useBackupIkCharacterShaderToolStripMenuItem != null)
            {
                useBackupIkCharacterShaderToolStripMenuItem.Checked = settings.UseBackupIkCharacterShader;
            }
            if (perfHudToolStripMenuItem != null)
            {
                perfHudToolStripMenuItem.Checked = settings.EnablePerfHud;
            }
            if (perfSpikeLogToolStripMenuItem != null)
            {
                perfSpikeLogToolStripMenuItem.Checked = settings.EnablePerfSpikeLog;
            }
            if (vsyncToolStripMenuItem != null)
            {
                vsyncToolStripMenuItem.Checked = settings.EnableVsync;
            }
            if (useVertexColorsToolStripMenuItem != null)
            {
                useVertexColorsToolStripMenuItem.Checked = settings.EnableVertexColors;
            }
            if (enableNormalMapsToolStripMenuItem != null)
            {
                enableNormalMapsToolStripMenuItem.Checked = settings.EnableNormalMaps;
            }
            if (enableAOToolStripMenuItem != null)
            {
                enableAOToolStripMenuItem.Checked = settings.EnableAO;
            }
        }

        private void ApplyRenderSettings()
        {
            if (renderCtrl?.renderer == null) return;
            try
            {
                renderCtrl.SetVsync(settings.EnableVsync);
            }
            catch
            {
                // Ignore if GLControl doesn't support runtime vsync toggling.
            }
            renderCtrl.renderer.SetNormalMapsEnabled(settings.EnableNormalMaps);
            renderCtrl.renderer.SetAOEnabled(settings.EnableAO);
            renderCtrl.renderer.SetVertexColorsEnabled(settings.EnableVertexColors);
            renderCtrl.renderer.SetFlipNormalY(settings.FlipNormalY);
            renderCtrl.renderer.SetReconstructNormalZ(settings.ReconstructNormalZ);

            RenderOptions.UseTrsklInverseBind = settings.UseTrsklInverseBind;
            RenderOptions.AutoMapBlendIndices = settings.AutoMapBlendIndices;
            RenderOptions.MapBlendIndicesViaJointInfo = settings.MapBlendIndicesViaJointInfo;
            RenderOptions.DeterministicSkinningAndAnimation = settings.DeterministicSkinningAndAnimation;
            RenderOptions.SwapBlendOrder = settings.SwapBlendOrder;
            RenderOptions.TransposeSkinMatrices = settings.TransposeSkinMatrices;
            RenderOptions.MapBlendIndicesViaBoneMeta = false;
            RenderOptions.MapBlendIndicesViaSkinningPalette = false;
            RenderOptions.UseSkinningPaletteMatrices = false;
            RenderOptions.UseJointInfoMatrices = false;
            RenderOptions.UseRareTrmtrMaterials = settings.UseRareTrmtrMaterials;
            RenderOptions.UseBackupIkCharacterShader = settings.UseBackupIkCharacterShader;
            RenderOptions.ShaderGame = ParseShaderGameSetting(settings.ShaderGame);
            RenderOptions.ShaderDebugMode = settings.ShaderDebugMode;
            RenderOptions.EnablePerfHud = settings.EnablePerfHud;
            RenderOptions.EnablePerfSpikeLog = settings.EnablePerfSpikeLog;
            RenderOptions.EnableExtractedOutFallback = settings.EnableExtractedOutFallback;
            RenderOptions.ExtractedOutRoot = ResolveActiveExtractedOutRoot(settings);
            RenderOptions.ExtractedOutGame = settings.ActiveExtractedGame ?? "ZA";

            var display = ResolveGBufferDisplay(settings);
            renderCtrl.renderer.SetGBufferDisplayMode(display);
            renderCtrl.renderer.SetSkeletonVisible(settings.ShowSkeleton);
            renderCtrl.Invalidate();
        }

        private static string ResolveActiveExtractedOutRoot(ViewerSettings settings)
        {
            if (settings == null)
            {
                return string.Empty;
            }

            string active = settings.ActiveExtractedGame?.Trim() ?? string.Empty;
            string za = settings.ZaExtractedOutRoot?.Trim() ?? string.Empty;
            string sv = settings.SvExtractedOutRoot?.Trim() ?? string.Empty;

            if (string.Equals(active, "SV", StringComparison.OrdinalIgnoreCase))
            {
                return !string.IsNullOrWhiteSpace(sv) ? sv : za;
            }

            return !string.IsNullOrWhiteSpace(za) ? za : sv;
        }

        private static GBuffer.DisplayType ResolveGBufferDisplay(ViewerSettings settings)
        {
            var buffer = (settings.DisplayBuffer ?? "All").Trim();
            return buffer switch
            {
                "Albedo" => GBuffer.DisplayType.DISPLAY_ALBEDO,
                "Normal" => GBuffer.DisplayType.DISPLAY_NORMAL,
                "Specular" => GBuffer.DisplayType.DISPLAY_SPECULAR,
                "AO" => GBuffer.DisplayType.DISPLAY_AO,
                "Depth" => GBuffer.DisplayType.DISPLAY_DEPTH,
                _ => settings.DisplayShading == ViewerSettings.ShadingMode.Toon
                    ? GBuffer.DisplayType.DISPLAY_TOON
                    : settings.DisplayShading == ViewerSettings.ShadingMode.Legacy
                        ? GBuffer.DisplayType.DISPLAY_LEGACY
                        : GBuffer.DisplayType.DISPLAY_ALL
            };
        }

        private static ShaderGame ParseShaderGameSetting(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return ShaderGame.Auto;
            }

            return value.Trim() switch
            {
                "ZA" => ShaderGame.ZA,
                "SCVI" => ShaderGame.SCVI,
                "LA" => ShaderGame.LA,
                _ => ShaderGame.Auto
            };
        }

        private void useRareTrmtrMaterialsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            settings.UseRareTrmtrMaterials = useRareTrmtrMaterialsToolStripMenuItem.Checked;
            settings.Save();
            ApplyRenderSettingsToMenu();
            ApplyRenderSettings();
            ReloadLoadedModels();
        }

        private void useVertexColorsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            settings.EnableVertexColors = useVertexColorsToolStripMenuItem.Checked;
            settings.Save();
            ApplyRenderSettingsToMenu();
            ApplyRenderSettings();
        }

        private void enableNormalMapsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            settings.EnableNormalMaps = enableNormalMapsToolStripMenuItem.Checked;
            settings.Save();
            ApplyRenderSettingsToMenu();
            ApplyRenderSettings();
        }

        private void enableAOToolStripMenuItem_Click(object sender, EventArgs e)
        {
            settings.EnableAO = enableAOToolStripMenuItem.Checked;
            settings.Save();
            ApplyRenderSettingsToMenu();
            ApplyRenderSettings();
        }

        private async void ReloadLoadedModels()
        {
            if (sceneModelManager.LoadedModelPaths.Count == 0)
            {
                return;
            }

            var files = sceneModelManager.LoadedModelPaths.ToList();
            BeginModelLoad();
            try
            {
                ClearAll();
                foreach (var file in files)
                {
                    try
                    {
                        await AddModelToSceneAsync(file);
                    }
                    catch
                    {
                        // Ignore individual load failures during reload.
                    }
                }
            }
            finally
            {
                EndModelLoad();
            }
        }

        private void shadingLitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            settings.DisplayShading = ViewerSettings.ShadingMode.Lit;
            settings.DisplayBuffer = "All";
            settings.Save();
            ApplyRenderSettingsToMenu();
            ApplyRenderSettings();
        }

        private void shadingToonToolStripMenuItem_Click(object sender, EventArgs e)
        {
            settings.DisplayShading = ViewerSettings.ShadingMode.Toon;
            settings.DisplayBuffer = "All";
            settings.Save();
            ApplyRenderSettingsToMenu();
            ApplyRenderSettings();
        }

        private void shadingLegacyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            settings.DisplayShading = ViewerSettings.ShadingMode.Legacy;
            settings.DisplayBuffer = "All";
            settings.Save();
            ApplyRenderSettingsToMenu();
            ApplyRenderSettings();
        }

        private void displayAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            settings.DisplayBuffer = "All";
            settings.Save();
            ApplyRenderSettingsToMenu();
            ApplyRenderSettings();
        }

        private void displayAlbedoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            settings.DisplayBuffer = "Albedo";
            settings.DisplayShading = ViewerSettings.ShadingMode.Lit;
            settings.Save();
            ApplyRenderSettingsToMenu();
            ApplyRenderSettings();
        }

        private void displayNormalToolStripMenuItem_Click(object sender, EventArgs e)
        {
            settings.DisplayBuffer = "Normal";
            settings.DisplayShading = ViewerSettings.ShadingMode.Lit;
            settings.Save();
            ApplyRenderSettingsToMenu();
            ApplyRenderSettings();
        }

        private void displaySpecularToolStripMenuItem_Click(object sender, EventArgs e)
        {
            settings.DisplayBuffer = "Specular";
            settings.DisplayShading = ViewerSettings.ShadingMode.Lit;
            settings.Save();
            ApplyRenderSettingsToMenu();
            ApplyRenderSettings();
        }

        private void displayAOToolStripMenuItem_Click(object sender, EventArgs e)
        {
            settings.DisplayBuffer = "AO";
            settings.DisplayShading = ViewerSettings.ShadingMode.Lit;
            settings.Save();
            ApplyRenderSettingsToMenu();
            ApplyRenderSettings();
        }

        private void displayDepthToolStripMenuItem_Click(object sender, EventArgs e)
        {
            settings.DisplayBuffer = "Depth";
            settings.DisplayShading = ViewerSettings.ShadingMode.Lit;
            settings.Save();
            ApplyRenderSettingsToMenu();
            ApplyRenderSettings();
        }

        private void shaderDebugOffToolStripMenuItem_Click(object sender, EventArgs e)
        {
            settings.ShaderDebugMode = 0;
            settings.Save();
            ApplyRenderSettingsToMenu();
            ApplyRenderSettings();
        }

        private void shaderDebugIepTextureToolStripMenuItem_Click(object sender, EventArgs e)
        {
            settings.ShaderDebugMode = 1;
            settings.Save();
            ApplyRenderSettingsToMenu();
            ApplyRenderSettings();
        }

        private void shaderDebugIepLayerMaskToolStripMenuItem_Click(object sender, EventArgs e)
        {
            settings.ShaderDebugMode = 2;
            settings.Save();
            ApplyRenderSettingsToMenu();
            ApplyRenderSettings();
        }

        private void shaderDebugIepUvToolStripMenuItem_Click(object sender, EventArgs e)
        {
            settings.ShaderDebugMode = 3;
            settings.Save();
            ApplyRenderSettingsToMenu();
            ApplyRenderSettings();
        }

        private void shaderDebugIepUv01ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            settings.ShaderDebugMode = 4;
            settings.Save();
            ApplyRenderSettingsToMenu();
            ApplyRenderSettings();
        }

        private void shaderDebugIkLayerMask1ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            settings.ShaderDebugMode = 20;
            settings.Save();
            ApplyRenderSettingsToMenu();
            ApplyRenderSettings();
        }

        private void shaderDebugIkLayerMask2ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            settings.ShaderDebugMode = 21;
            settings.Save();
            ApplyRenderSettingsToMenu();
            ApplyRenderSettings();
        }

        private void shaderDebugIkLayerMask3ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            settings.ShaderDebugMode = 22;
            settings.Save();
            ApplyRenderSettingsToMenu();
            ApplyRenderSettings();
        }

        private void shaderDebugIkLayerMask4ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            settings.ShaderDebugMode = 23;
            settings.Save();
            ApplyRenderSettingsToMenu();
            ApplyRenderSettings();
        }
    }
}
