using GFTool.Renderer.Core;
using GFTool.Renderer.Scene.GraphicsObjects;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using GfAnim = Trinity.Core.Flatbuffers.GF.Animation;
using Trinity.Core.Assets;
using Trinity.Core.Cache;
using Trinity.Core.Utils;

namespace TrinityModelViewer
{
    public partial class ModelViewerForm
    {
        private void SetupAnimationsList()
        {
            animationsList.View = View.Details;
            animationsList.FullRowSelect = true;
            animationsList.HideSelection = false;
            animationsList.MultiSelect = false;
            animationsList.Columns.Clear();
            animationsList.Columns.Add("Animation", -2);
            animationsList.DoubleClick += animationsList_DoubleClick;
            animationsList.MouseUp += animationsList_MouseUp;

            animationScrubBar.Minimum = 0;
            animationScrubBar.Maximum = 10000;
            animationScrubBar.Value = 0;
            animationScrubBar.Enabled = false;
            animationScrubBar.MouseDown += (sender, e) => isScrubbingAnimation = true;
            animationScrubBar.MouseUp += (sender, e) =>
            {
                isScrubbingAnimation = false;
                SyncAnimationScrubBarFromRenderer();
            };
            animationScrubBar.Scroll += (sender, e) => ApplyAnimationScrubBarToRenderer();
            pauseAnimationButton.Enabled = true;
            stopAnimationButton.Enabled = true;

            EnsureAnimationsContextMenu();

            animationUiTimer.Interval = 16;
            animationUiTimer.Tick += (sender, e) => SyncAnimationScrubBarFromRenderer();
            animationUiTimer.Start();
        }

        private void EnsureAnimationsContextMenu()
        {
            if (animationsContextMenu != null)
            {
                return;
            }

            animationsContextMenu = new ContextMenuStrip();
            exportAnimationMenuItem = new ToolStripMenuItem("Export...");
            exportAnimationMenuItem.Click += exportAnimationButton_Click;
            exportModelWithAnimationMenuItem = new ToolStripMenuItem("Export Model + Anim...");
            exportModelWithAnimationMenuItem.Click += (sender, e) => ExportModelWithSelectedAnimation();
            animationsContextMenu.Items.Add(exportAnimationMenuItem);
            animationsContextMenu.Items.Add(exportModelWithAnimationMenuItem);
        }

        private void animationsList_MouseUp(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
            {
                return;
            }

            var hit = animationsList.HitTest(e.Location);
            if (hit.Item != null)
            {
                animationsList.SelectedItems.Clear();
                hit.Item.Selected = true;
            }

            EnsureAnimationsContextMenu();
            bool hasSelection = GetSelectedAnimation() != null;
            if (exportAnimationMenuItem != null) exportAnimationMenuItem.Enabled = hasSelection;
            if (exportModelWithAnimationMenuItem != null) exportModelWithAnimationMenuItem.Enabled = hasSelection;
            animationsContextMenu?.Show(animationsList, e.Location);
        }

        private void loadAnimationButton_Click(object sender, EventArgs e)
        {
            using var ofd = new OpenFileDialog();
            ofd.Filter = "Trinity Animation files (*.tranm;*.gfbanm)|*.tranm;*.gfbanm|All files (*.*)|*.*";
            ofd.Multiselect = true;
            if (ofd.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            try
            {
                int loaded = 0;
                foreach (var file in ofd.FileNames.Where(f => !string.IsNullOrWhiteSpace(f)))
                {
                    if (!loadedAnimationPaths.Add(file))
                    {
                        continue;
                    }

                    var animFile = FlatBufferConverter.DeserializeFrom<GfAnim.Animation>(file);
                    var anim = new GFTool.Renderer.Scene.GraphicsObjects.Animation(animFile, Path.GetFileNameWithoutExtension(file), file);
                    animations.Add(anim);
                    var item = new ListViewItem(anim.Name) { Tag = anim };
                    animationsList.Items.Add(item);
                    loaded++;

                    if (MessageHandler.Instance.DebugLogsEnabled)
                    {
                        MessageHandler.Instance.AddMessage(MessageType.LOG, $"[Anim] Loaded '{anim.Name}' file='{file}' frames={anim.FrameCount} fps={anim.FrameRate} tracks={anim.TrackCount}");
                    }
                }

                if (loaded > 0)
                {
                    animationsList.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Failed to load animation:\n{ex.Message}", "Animation Load", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void playAnimationButton_Click(object sender, EventArgs e)
        {
            if (renderCtrl?.renderer == null)
            {
                return;
            }

            var anim = GetSelectedAnimation();
            if (anim == null)
            {
                return;
            }

            UpdateAnimationTargetsForCurrentMode();

            renderCtrl.renderer.SetLoopAnimationOverride(loopAnimationCheckBox.Checked);
            renderCtrl.renderer.SetAnimationPaused(false);
            var (primary, fallback) = ResolveAnimationPlayback(anim);
            renderCtrl.renderer.PlayAnimation(primary, fallback);
            pauseAnimationButton.Text = "Pause";
            pauseAnimationButton.Enabled = true;
            stopAnimationButton.Enabled = true;
            animationScrubBar.Enabled = true;
            SyncAnimationScrubBarFromRenderer();
        }

        private void pauseAnimationButton_Click(object sender, EventArgs e)
        {
            if (renderCtrl?.renderer == null)
            {
                return;
            }

            if (!renderCtrl.renderer.HasActiveAnimation())
            {
                return;
            }

            bool paused = renderCtrl.renderer.IsAnimationPaused();
            renderCtrl.renderer.SetAnimationPaused(!paused);
            pauseAnimationButton.Text = paused ? "Pause" : "Resume";
        }

        private void stopAnimationButton_Click(object sender, EventArgs e)
        {
            if (renderCtrl?.renderer == null)
            {
                return;
            }

            renderCtrl.renderer.SetAnimationTargets(null);
            renderCtrl.renderer.StopAnimation();
            animationScrubBar.Value = 0;
            animationScrubBar.Enabled = false;
            pauseAnimationButton.Text = "Pause";
            pauseAnimationButton.Enabled = true;
            stopAnimationButton.Enabled = true;
        }

        private void UpdateAnimationTargetsForCurrentMode()
        {
            if (renderCtrl?.renderer == null)
            {
                return;
            }

            if (settings.ShowMultipleModels)
            {
                renderCtrl.renderer.SetAnimationTargets(modelMap.Values.Where(m => m != null && !sceneModelManager.IsHidden(m)));
                return;
            }

            var focused = GetSelectedModelFromSceneTree();
            renderCtrl.renderer.SetAnimationTargets(focused != null ? new[] { focused } : null);
        }

        private void loopAnimationCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (renderCtrl?.renderer == null)
            {
                return;
            }

            renderCtrl.renderer.SetLoopAnimationOverride(loopAnimationCheckBox.Checked);
        }

        private void exportAnimationButton_Click(object sender, EventArgs e)
        {
            var anim = GetSelectedAnimation();
            if (anim == null)
            {
                MessageBox.Show(this, "Select an animation to export.", "Export Animation", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var mdl = GetModelForAnimationExport();
            if (mdl?.Armature == null || mdl.Armature.Bones.Count == 0)
            {
                MessageBox.Show(this, "Load a model with a skeleton first (animation export needs an armature).", "Export Animation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using var sfd = new SaveFileDialog();
            sfd.Filter = "glTF 2.0 (*.gltf)|*.gltf";
            sfd.FileName = $"{anim.Name}.gltf";
            if (sfd.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            try
            {
                Export.GltfExporter.ExportAnimation(mdl.Armature, anim, sfd.FileName);
                MessageBox.Show(this, $"Exported:\n{sfd.FileName}", "Export Animation", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Export failed:\n{ex.Message}", "Export Animation", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void exportModelWithAnimationsButton_Click(object sender, EventArgs e)
        {
            var mdl = GetModelForAnimationExport();
            if (mdl == null)
            {
                MessageBox.Show(this, "Load a model first.", "Export Model + All Anims", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using var sfd = new SaveFileDialog();
            sfd.Filter = "glTF 2.0 (*.gltf)|*.gltf";
            sfd.FileName = $"{mdl.Name}_with_all_anims.gltf";
            if (sfd.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            try
            {
                var anims = animations.ToArray();
                if (anims.Length == 0)
                {
                    MessageBox.Show(this, "No animations are loaded; exporting the model only.", "Export Model + All Anims", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                Export.GltfExporter.ExportModel(mdl, sfd.FileName, anims);
                MessageBox.Show(this, $"Exported:\n{sfd.FileName}", "Export Model + All Anims", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Export failed:\n{ex.Message}", "Export Model + All Anims", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ExportModelWithSelectedAnimation()
        {
            var anim = GetSelectedAnimation();
            if (anim == null)
            {
                return;
            }

            var mdl = GetModelForAnimationExport();
            if (mdl == null)
            {
                MessageBox.Show(this, "Load a model first.", "Export Model + Anim", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using var sfd = new SaveFileDialog();
            sfd.Filter = "glTF 2.0 (*.gltf)|*.gltf";
            sfd.FileName = $"{mdl.Name}_{anim.Name}.gltf";
            if (sfd.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            try
            {
                Export.GltfExporter.ExportModel(mdl, sfd.FileName, new[] { anim });
                MessageBox.Show(this, $"Exported:\n{sfd.FileName}", "Export Model + Anim", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Export failed:\n{ex.Message}", "Export Model + Anim", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SyncAnimationScrubBarFromRenderer()
        {
            if (renderCtrl?.renderer == null)
            {
                return;
            }

            if (isScrubbingAnimation)
            {
                return;
            }

            if (!renderCtrl.renderer.HasActiveAnimation())
            {
                animationScrubBar.Enabled = false;
                pauseAnimationButton.Text = "Pause";
                return;
            }

            var duration = renderCtrl.renderer.GetActiveAnimationDurationSeconds();
            if (duration <= 0)
            {
                animationScrubBar.Value = 0;
                return;
            }

            var t = renderCtrl.renderer.GetAnimationTimeSeconds();
            var normalized = Math.Clamp(t / duration, 0.0, 1.0);
            int target = (int)Math.Round(normalized * animationScrubBar.Maximum);
            if (target != animationScrubBar.Value)
            {
                animationScrubBar.Value = target;
            }

            pauseAnimationButton.Text = renderCtrl.renderer.IsAnimationPaused() ? "Resume" : "Pause";
        }

        private void ApplyAnimationScrubBarToRenderer()
        {
            if (renderCtrl?.renderer == null)
            {
                return;
            }

            if (!renderCtrl.renderer.HasActiveAnimation())
            {
                return;
            }

            var duration = renderCtrl.renderer.GetActiveAnimationDurationSeconds();
            if (duration <= 0)
            {
                return;
            }

            double normalized = animationScrubBar.Value / (double)animationScrubBar.Maximum;
            renderCtrl.renderer.SetAnimationTimeSeconds(duration * normalized);
            renderCtrl.Invalidate();
        }

        private Model? GetModelForAnimationExport()
        {
            var selected = sceneTree.SelectedNode;
            if (selected != null)
            {
                var node = selected;
                while (node != null)
                {
                    if (modelMap.TryGetValue(node, out var m) && m != null)
                    {
                        return m;
                    }
                    node = node.Parent;
                }
            }

            foreach (var kvp in modelMap)
            {
                if (kvp.Value != null)
                {
                    return kvp.Value;
                }
            }

            return null;
        }

        private void animationsList_DoubleClick(object? sender, EventArgs e)
        {
            if (renderCtrl?.renderer == null)
            {
                return;
            }

            var anim = GetSelectedAnimation();
            if (anim == null)
            {
                return;
            }

            UpdateAnimationTargetsForCurrentMode();

            renderCtrl.renderer.SetLoopAnimationOverride(loopAnimationCheckBox.Checked);
            renderCtrl.renderer.SetAnimationPaused(false);
            var (primary, fallback) = ResolveAnimationPlayback(anim);
            renderCtrl.renderer.PlayAnimation(primary, fallback);
            pauseAnimationButton.Text = "Pause";
            pauseAnimationButton.Enabled = true;
            stopAnimationButton.Enabled = true;
            animationScrubBar.Enabled = true;
            SyncAnimationScrubBarFromRenderer();
        }

        private (Animation primary, Animation? fallback) ResolveAnimationPlayback(Animation selected)
        {
            if (selected == null)
            {
                return (selected!, null);
            }

            if (!LooksLikeOverlayAnimation(selected))
            {
                return (selected, null);
            }

            var fallback = FindBaseFallbackAnimation(selected);
            if (fallback == null)
            {
                return (selected, null);
            }

            if (MessageHandler.Instance.DebugLogsEnabled)
            {
                MessageHandler.Instance.AddMessage(
                    MessageType.LOG,
                    $"[Anim] Overlay: primary='{selected.Name}' fallback='{fallback.Name}'");
            }

            return (selected, fallback);
        }

        private static bool LooksLikeOverlayAnimation(Animation anim)
        {
            static bool HasAnyTrackName(Animation a, params string[] names)
            {
                foreach (var n in names)
                {
                    if (a.HasTrack(n))
                    {
                        return true;
                    }
                }
                return false;
            }

            if (!string.IsNullOrWhiteSpace(anim.SourcePath))
            {
                var lower = anim.SourcePath.Replace('\\', '/').ToLowerInvariant();
                if (lower.Contains("/motion_pc/base/"))
                {
                    return false;
                }
            }

            return !HasAnyTrackName(anim, "waist", "spine_01", "left_leg_01", "right_leg_01", "left_arm_01", "right_arm_01");
        }

        private Animation? FindBaseFallbackAnimation(Animation selected)
        {
            static string GetTailKey(string name)
            {
                var parts = name.Split('_', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 3)
                {
                    return string.Empty;
                }
                return string.Join("_", parts.Skip(2));
            }

            string tail = GetTailKey(selected.Name);
            if (string.IsNullOrWhiteSpace(tail))
            {
                return null;
            }

            Animation? best = null;
            foreach (var anim in animations)
            {
                if (anim == null || ReferenceEquals(anim, selected))
                {
                    continue;
                }

                if (!anim.Name.StartsWith("p0_", StringComparison.OrdinalIgnoreCase) &&
                    !anim.Name.StartsWith("p1_", StringComparison.OrdinalIgnoreCase) &&
                    !anim.Name.StartsWith("p2_", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!string.Equals(GetTailKey(anim.Name), tail, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!anim.HasTrack("waist") && !anim.HasTrack("spine_01"))
                {
                    continue;
                }

                best = anim;
                if (anim.Name.StartsWith("p0_", StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }
            }

            return best;
        }

        private Animation? GetSelectedAnimation()
        {
            if (animationsList.SelectedItems.Count == 0)
            {
                return null;
            }

            return animationsList.SelectedItems[0].Tag as Animation;
        }

    }
}
