using GFTool.Renderer.Core;
using GFTool.Renderer.Core.Graphics;
using OpenTK.Mathematics;
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TrinityModelViewer
{
    public partial class ModelViewerForm
    {
        private void messageHandler_Callback(object? sender, GFTool.Renderer.Core.Message e)
        {
            if (IsHandleCreated && InvokeRequired)
            {
                try
                {
                    BeginInvoke((Action)(() => messageHandler_Callback(sender, e)));
                }
                catch
                {
                    // Ignore shutdown races / handle disposal.
                }
                return;
            }

            var item = new ListViewItem();
            item.Name = e.GetHashCode().ToString();
            item.Text = e.Content;
            item.ImageKey = e.Type switch
            {
                MessageType.LOG => "Log",
                MessageType.WARNING => "Warning",
                MessageType.ERROR => "Error"
            };

            if (!messageListView.Items.ContainsKey(e.GetHashCode().ToString()))
            {
                messageListView.Items.Add(item);
                messageListView.EnsureVisible(messageListView.Items.Count - 1);
            }
        }

        private void glCtxt_Paint(object sender, PaintEventArgs e)
        {
            var cam = renderCtrl.renderer.GetCameraTransform();
            statusLbl.Text = string.Format("Camera: Pos={0}, [Quat={1} Euler={2}]", cam.Position.ToString(), cam.Rotation.ToString(), cam.Rotation.ToEulerAngles().ToString());
        }

        private void glCtxt_Load(object sender, EventArgs e)
        {
            MessageHandler.Instance.MessageCallback += messageHandler_Callback;
            var messageIcons = new ImageList();
            messageIcons.Images.Add("Log", SystemIcons.Information.ToBitmap());
            messageIcons.Images.Add("Warning", SystemIcons.Warning.ToBitmap());
            messageIcons.Images.Add("Error", SystemIcons.Error.ToBitmap());
            messageListView.SmallImageList = messageIcons;
            messageListView.FullRowSelect = true;
            messageListView.AutoResizeColumn(0, ColumnHeaderAutoResizeStyle.HeaderSize);
        }

        private async void renderCtrl_RendererReady(object? sender, EventArgs e)
        {
            ApplyRenderSettings();
            WarmupShaders();
            StartFlatSharpWarmup();
            if (startupFiles.Length > 0 && flatSharpWarmupTask != null)
            {
                try
                {
                    await flatSharpWarmupTask;
                }
                catch
                {
                    // Ignore warmup failures; model loading will fall back to on-demand generation.
                }
            }
            await LoadStartupFilesIfAnyAsync();
        }

        private void StartFlatSharpWarmup()
        {
            flatSharpWarmupTask ??= Trinity.Core.Utils.FlatSharpWarmup.EnsureTrinityModelSerializersWarmedUp(msg =>
            {
                if (!MessageHandler.Instance.DebugLogsEnabled)
                {
                    return;
                }

                void LogOnUi()
                {
                    try
                    {
                        MessageHandler.Instance.AddMessage(MessageType.LOG, msg);
                    }
                    catch
                    {
                        // Ignore shutdown races / handle disposal.
                    }
                }

                if (IsHandleCreated && InvokeRequired)
                {
                    try { BeginInvoke((Action)LogOnUi); } catch { }
                }
                else
                {
                    LogOnUi();
                }
            });
        }

        private void WarmupShaders()
        {
            if (shaderWarmupCompleted || renderCtrl == null)
            {
                return;
            }

            shaderWarmupCompleted = true;

            try
            {
                renderCtrl.MakeCurrent();
            }
            catch
            {
                // Best-effort; ShaderPool will fail to compile if GL isn't current.
            }

            string[] warm = new[]
            {
                "IkCharacter",
                "Standard",
                "Transparent",
                "Outline",
                "Hair",
                "Eye",
                "EyeClearCoat",
                "EyeClearCoatForward",
                "gbuffer",
                "ssao",
                "ssao_blur",
            };

            foreach (var name in warm)
            {
                try
                {
                    ShaderPool.Instance.GetShader(name);
                }
                catch
                {
                    // Ignore warmup failures; shaders will be requested on-demand later.
                }
            }

            if (MessageHandler.Instance.DebugLogsEnabled)
            {
                MessageHandler.Instance.AddMessage(MessageType.LOG, $"[Shader] Warmed {warm.Length} shaders");
            }
        }

        private async Task LoadStartupFilesIfAnyAsync()
        {
            if (startupFilesLoaded)
            {
                return;
            }
            startupFilesLoaded = true;

            if (startupFiles.Length == 0)
            {
                return;
            }

            try
            {
                BeginModelLoad();
                try
                {
                    ClearAll();
                    foreach (var path in startupFiles)
                    {
                        if (path.EndsWith(".trmdl", StringComparison.OrdinalIgnoreCase) && File.Exists(path))
                        {
                            await AddModelToSceneAsync(path);
                        }
                    }
                }
                finally
                {
                    EndModelLoad();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Failed to load startup model:\n{ex.Message}", "Startup Load", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BeginModelLoad()
        {
            modelLoadDepth++;
            if (modelLoadDepth != 1)
            {
                return;
            }

            modelLoadCts?.Dispose();
            modelLoadCts = new CancellationTokenSource();

            try
            {
                loadingProgressBar.Visible = true;
                loadingProgressBar.Style = ProgressBarStyle.Continuous;
                loadingProgressBar.MarqueeAnimationSpeed = 0;
                loadingProgressBar.Value = 0;
                loadingProgressBar.Refresh();
            }
            catch
            {
                // Ignore if designer control isn't available for any reason.
            }

            if (loadingForm == null || loadingForm.IsDisposed)
            {
                loadingForm = new LoadingForm();
                ApplyTheme(loadingForm);
            }

            loadingForm.SetMessage("Loading model...");
            loadingForm.CancelRequested -= LoadingForm_CancelRequested;
            loadingForm.CancelRequested += LoadingForm_CancelRequested;
            loadingForm.ResetCancel();
            loadingForm.SetIndeterminate(false);
            loadingForm.SetProgress(0);
            loadingForm.Show(this);
            loadingForm.BringToFront();
            loadingForm.Refresh();
        }

        private void LoadingForm_CancelRequested(object? sender, EventArgs e)
        {
            try
            {
                modelLoadCts?.Cancel();
            }
            catch
            {
                // Ignore.
            }
        }

        private void ReportModelLoadProgress(int percent)
        {
            if (modelLoadDepth <= 0)
            {
                return;
            }

            try
            {
                if (loadingProgressBar.Visible)
                {
                    loadingProgressBar.Style = ProgressBarStyle.Continuous;
                    loadingProgressBar.MarqueeAnimationSpeed = 0;
                    loadingProgressBar.Value = Math.Clamp(percent, 0, 100);
                }
            }
            catch
            {
                // Ignore if designer control isn't available for any reason.
            }

            if (loadingForm != null && !loadingForm.IsDisposed)
            {
                loadingForm.SetIndeterminate(false);
                loadingForm.SetProgress(percent);
                loadingForm.Refresh();
            }
        }

        private void EndModelLoad()
        {
            if (modelLoadDepth <= 0)
            {
                return;
            }

            modelLoadDepth--;
            if (modelLoadDepth != 0)
            {
                return;
            }

            try
            {
                loadingProgressBar.MarqueeAnimationSpeed = 0;
                loadingProgressBar.Visible = false;
            }
            catch
            {
                // Ignore if designer control isn't available for any reason.
            }

            if (loadingForm != null && !loadingForm.IsDisposed)
            {
                loadingForm.CancelRequested -= LoadingForm_CancelRequested;
                loadingForm.Refresh();
                loadingForm.Close();
                loadingForm.Dispose();
            }

            modelLoadCts?.Dispose();
            modelLoadCts = null;
        }

        private void glCtxt_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.W: KeyboardControls.Forward = true; break;
                case Keys.A: KeyboardControls.Left = true; break;
                case Keys.S: KeyboardControls.Backward = true; break;
                case Keys.D: KeyboardControls.Right = true; break;
                case Keys.Q: KeyboardControls.Up = true; break;
                case Keys.E: KeyboardControls.Down = true; break;
            }
        }

        private void glCtxt_KeyUp(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.W: KeyboardControls.Forward = false; break;
                case Keys.A: KeyboardControls.Left = false; break;
                case Keys.S: KeyboardControls.Backward = false; break;
                case Keys.D: KeyboardControls.Right = false; break;
                case Keys.Q: KeyboardControls.Up = false; break;
                case Keys.E: KeyboardControls.Down = false; break;
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            try
            {
                previewUpdateCts?.Cancel();
                previewUpdateCts?.Dispose();
                previewUpdateCts = null;
            }
            catch
            {
                // Ignore shutdown races.
            }

            try
            {
                perfHudTimer?.Stop();
                perfHudTimer?.Dispose();
                perfHudTimer = null;
            }
            catch
            {
                // Ignore.
            }

            try
            {
                if (perfHudLabel != null)
                {
                    perfHudLabel.Dispose();
                    perfHudLabel = null;
                }
            }
            catch
            {
                // Ignore.
            }

            base.OnFormClosed(e);
        }
    }
}
