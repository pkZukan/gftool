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
using TrinityModelViewer.Scene;
using Trinity.Core.Assets;
using Trinity.Core.Cache;
using Trinity.Core.Utils;

namespace TrinityModelViewer
{
    public partial class ModelViewerForm
    {
        private readonly AnimationAutoLoader animationAutoLoader = new AnimationAutoLoader();

        private void TryAutoLoadAnimationsFromGfpak(IAssetProvider provider)
        {
            if (!settings.AutoLoadAnimations)
            {
                return;
            }

            var animPaths = provider.EnumerateEntries()
                .Select(e => e.Path)
                .Where(p =>
                    !string.IsNullOrWhiteSpace(p) &&
                    (p.EndsWith(".tranm", StringComparison.OrdinalIgnoreCase) ||
                     p.EndsWith(".gfbanm", StringComparison.OrdinalIgnoreCase)))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (animPaths.Count == 0)
            {
                if (MessageHandler.Instance.DebugLogsEnabled)
                {
                    MessageHandler.Instance.AddMessage(MessageType.LOG, "[Anim] GFPAK AutoLoad: no named .tranm/.gfbanm entries found (GFPAKHashCache.bin may be missing).");
                }
                return;
            }

            const int maxToLoad = 500;
            int loaded = 0;

            foreach (var animPath in animPaths.Take(maxToLoad))
            {
                string key = $"{provider.DisplayName}::{animPath}";
                if (!loadedAnimationPaths.Add(key))
                {
                    continue;
                }

                try
                {
                    var bytes = provider.ReadAllBytes(animPath);
                    var animFile = FlatBufferConverter.DeserializeFrom<GfAnim.Animation>(bytes);
                    var anim = new GFTool.Renderer.Scene.GraphicsObjects.Animation(animFile, Path.GetFileNameWithoutExtension(animPath), animPath);
                    animations.Add(anim);
                    var item = new ListViewItem(anim.Name) { Tag = anim };
                    animationsList.Items.Add(item);
                    loaded++;

                    if (MessageHandler.Instance.DebugLogsEnabled)
                    {
                        MessageHandler.Instance.AddMessage(MessageType.LOG, $"[Anim] Loaded '{anim.Name}' from GFPAK path='{animPath}' frames={anim.FrameCount} fps={anim.FrameRate} tracks={anim.TrackCount}");
                    }
                }
                catch (Exception ex)
                {
                    if (MessageHandler.Instance.DebugLogsEnabled)
                    {
                        MessageHandler.Instance.AddMessage(MessageType.WARNING, $"[Anim] GFPAK AutoLoad: failed path='{animPath}': {ex.Message}");
                    }
                }
            }

            if (loaded > 0)
            {
                animationsList.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
            }
        }

        private void TryAutoLoadAnimations(string trmdlPath)
        {
            if (!settings.AutoLoadAnimations)
            {
                return;
            }

            CancellationTokenSource cts;
            lock (animationLoadGate)
            {
                autoLoadAnimationsCts?.Cancel();
                autoLoadAnimationsCts?.Dispose();
                autoLoadAnimationsCts = new CancellationTokenSource();
                cts = autoLoadAnimationsCts;
            }

            Action<string>? debugLog = MessageHandler.Instance.DebugLogsEnabled
                ? message => MessageHandler.Instance.AddMessage(MessageType.LOG, message)
                : null;

            var animDirs = animationAutoLoader.BuildSearchDirectories(settings, trmdlPath, debugLog);
            if (animDirs.Count == 0)
            {
                if (MessageHandler.Instance.DebugLogsEnabled)
                {
                    MessageHandler.Instance.AddMessage(MessageType.LOG, $"[Anim] AutoLoad: no animation directory found for '{trmdlPath}'");
                }
                return;
            }

            _ = AutoLoadAnimationsAsync(animDirs.Distinct(StringComparer.OrdinalIgnoreCase).ToList(), cts.Token);
        }

        private async Task AutoLoadAnimationsAsync(IReadOnlyList<string> animDirs, CancellationToken token)
        {
            if (animDirs == null || animDirs.Count == 0)
            {
                return;
            }

            const int maxToLoadTotal = 500;
            HashSet<string> existing;
            lock (animationLoadGate)
            {
                existing = new HashSet<string>(loadedAnimationPaths, StringComparer.OrdinalIgnoreCase);
            }

            List<AnimationAutoLoader.LoadedAnimation> loaded;
            try
            {
                loaded = await animationAutoLoader.LoadAnimationsFromDirectoriesAsync(animDirs, maxToLoadTotal, existing, token);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (loaded.Count == 0)
            {
                return;
            }

            if (IsDisposed)
            {
                return;
            }

            try
            {
                BeginInvoke(new Action(() =>
                {
                    if (IsDisposed)
                    {
                        return;
                    }

                    foreach (var (path, anim) in loaded)
                    {
                        if (!loadedAnimationPaths.Add(path))
                        {
                            continue;
                        }

                        animations.Add(anim);
                        animationsList.Items.Add(new ListViewItem(anim.Name) { Tag = anim });
                    }

                    animationsList.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);

                    if (MessageHandler.Instance.DebugLogsEnabled)
                    {
                        MessageHandler.Instance.AddMessage(
                            MessageType.LOG,
                            $"[Anim] AutoLoad: loaded {loaded.Count} animations from {animDirs.Count} directories (max {maxToLoadTotal})");
                    }
                }));
            }
            catch
            {
                // Ignore UI invoke failures on shutdown.
            }
        }
    }
}
