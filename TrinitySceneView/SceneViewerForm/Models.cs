using GFTool.Core.Flatbuffers.TR.Scene.Components;
using GFTool.Core.Flatbuffers.TR.Scene;
using GFTool.Renderer;
using GFTool.Renderer.Core;
using GFTool.Renderer.Core.Graphics;
using GFTool.Renderer.Scene.GraphicsObjects;
using OpenTK.Mathematics;
using System.Drawing;
using System.Text;
using Trinity.Core.Utils;
using Point = System.Drawing.Point;
using GFTool.Renderer.Core;


namespace TrinitySceneView
{
    public partial class SceneViewerForm : Form
    {
        private void TryLoadSceneModels(string sceneFile)
        {
            if (renderCtrl?.renderer == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(assetRoot) || !Directory.Exists(assetRoot))
            {
                MessageHandler.Instance.AddMessage(MessageType.WARNING, "[Scene] Asset root not set; use File -> Set Asset Root... to enable model loading.");
                return;
            }

            renderCtrl.renderer.ClearScene();
            ClearModelsList();

            TRSCN? trscn;
            try
            {
                trscn = FlatBufferConverter.DeserializeFrom<TRSCN>(sceneFile);
            }
            catch (Exception ex)
            {
                MessageHandler.Instance.AddMessage(MessageType.ERROR, $"[Scene] Failed to parse scene '{sceneFile}': {ex.Message}");
                return;
            }

            var loadedScenes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var spawnedPositions = new List<Vector3>();
            LoadSceneRecursive(sceneFile, trscn, loadedScenes, spawnedPositions);

            if (spawnedPositions.Count > 0)
            {
                var min = new Vector3(float.PositiveInfinity);
                var max = new Vector3(float.NegativeInfinity);
                foreach (var p in spawnedPositions)
                {
                    min = Vector3.ComponentMin(min, p);
                    max = Vector3.ComponentMax(max, p);
                }

                var center = (min + max) * 0.5f;
                var radius = (max - min).Length * 0.5f;
                // Start close for small props, but auto-dolly out for large scenes.
                var distance = MathF.Max(2.5f, radius * 2.5f);
                renderCtrl.renderer.FocusCamera(center, distance);
                ApplySceneClipPlanes(center, radius);

                if (MessageHandler.Instance.DebugLogsEnabled)
                {
                    MessageHandler.Instance.AddMessage(
                        MessageType.LOG,
                        $"[Scene] Focus camera at {center} (models={spawnedPositions.Count}, radius≈{radius:0.###}, dist≈{distance:0.###}).");
                }
            }

            renderCtrl.Invalidate();
        }

        private void ClearModelsList()
        {
            suppressModelListEvents = true;
            try
            {
                modelsListView.Items.Clear();
            }
            finally
            {
                suppressModelListEvents = false;
            }
        }

        private void ApplySceneClipPlanes(Vector3 center, float radius)
        {
            if (renderCtrl?.renderer == null)
            {
                return;
            }

            if (!config.LargeClipPlanes)
            {
                renderCtrl.renderer.SetCameraClipPlanes(0.1f, 100.0f);
                return;
            }

            // Keep far clip generous for large-world Trinity scenes.
            var far = MathF.Max(10_000.0f, radius * 200.0f);
            renderCtrl.renderer.SetCameraClipPlanes(0.1f, far);
        }

        private void LoadSceneRecursive(string sceneFile, TRSCN trscn, HashSet<string> loadedScenes, List<Vector3> spawnedPositions)
        {
            if (loadedScenes.Contains(sceneFile))
            {
                return;
            }
            loadedScenes.Add(sceneFile);

            int spawned = 0;
            if (trscn.Chunks != null)
            {
                foreach (var chunk in trscn.Chunks)
                {
                    spawned += ProcessChunk(sceneFile, chunk, loadedScenes, spawnedPositions);
                }
            }

            if (spawned > 0)
            {
                MessageHandler.Instance.AddMessage(MessageType.LOG, $"[Scene] Spawned {spawned} model(s) from '{Path.GetFileName(sceneFile)}'.");
            }
        }

        private int ProcessChunk(string sceneFile, SceneChunk chunk, HashSet<string> loadedScenes, List<Vector3> spawnedPositions)
        {
            if (chunk == null || string.IsNullOrWhiteSpace(chunk.Type))
            {
                return 0;
            }

            int spawned = 0;
            if (chunk.Type == nameof(SubScene))
            {
                try
                {
                    var sub = FlatBufferConverter.DeserializeFrom<SubScene>(chunk.Data);
                    if (!string.IsNullOrWhiteSpace(sub.Filepath))
                    {
                        var resolved = ResolveSceneReference(sceneFile, sub.Filepath);
                        if (resolved != null)
                        {
                            var subScn = FlatBufferConverter.DeserializeFrom<TRSCN>(resolved);
                            LoadSceneRecursive(resolved, subScn, loadedScenes, spawnedPositions);
                        }
                        else
                        {
                            MessageHandler.Instance.AddMessage(MessageType.WARNING, $"[Scene] Missing SubScene file: {sub.Filepath} (from {Path.GetFileName(sceneFile)})");
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageHandler.Instance.AddMessage(MessageType.WARNING, $"[Scene] Failed to parse SubScene chunk: {ex.Message}");
                }
            }
            else if (chunk.Type == nameof(trinity_SceneObject))
            {
                spawned += TrySpawnSceneObject(sceneFile, chunk, spawnedPositions);
            }

            if (chunk.Children != null)
            {
                foreach (var child in chunk.Children)
                {
                    spawned += ProcessChunk(sceneFile, child, loadedScenes, spawnedPositions);
                }
            }

            return spawned;
        }

        private int TrySpawnSceneObject(string sceneFile, SceneChunk chunk, List<Vector3> spawnedPositions)
        {
            trinity_SceneObject? sceneObject;
            try
            {
                sceneObject = FlatBufferConverter.DeserializeFrom<trinity_SceneObject>(chunk.Data);
            }
            catch
            {
                return 0;
            }

            if (sceneObject == null || chunk.Children == null || chunk.Children.Length == 0)
            {
                return 0;
            }

            int spawned = 0;
            foreach (var child in chunk.Children)
            {
                if (child?.Type != nameof(trinity_ModelComponent))
                {
                    continue;
                }

                trinity_ModelComponent? modelComponent;
                try
                {
                    modelComponent = FlatBufferConverter.DeserializeFrom<trinity_ModelComponent>(child.Data);
                }
                catch
                {
                    continue;
                }

                if (modelComponent == null || string.IsNullOrWhiteSpace(modelComponent.FilePath))
                {
                    continue;
                }

                var resolved = ResolveModelPath(modelComponent.FilePath);
                if (resolved == null)
                {
                    MessageHandler.Instance.AddMessage(
                        MessageType.WARNING,
                        $"[Scene] Missing model file: {modelComponent.FilePath} (SceneObject={sceneObject.Name}, scene={Path.GetFileName(sceneFile)})");
                    continue;
                }

                Model model;
                try
                {
                    model = renderCtrl.renderer.AddSceneModel(resolved);
                }
                catch (Exception ex)
                {
                    MessageHandler.Instance.AddMessage(
                        MessageType.WARNING,
                        $"[Scene] Failed to load model '{resolved}' (SceneObject={sceneObject.Name}): {ex.GetType().Name}: {ex.Message}");
                    continue;
                }
                var mat = BuildModelMatrix(sceneObject.Srt, config.SpawnModelsAtOrigin, config.RotateModels180X, out var position, out var scale);
                model.SetModelMatrix(mat);
                spawnedPositions.Add(position);
                AddModelToList(sceneObject.Name, modelComponent.FilePath, model);

                if (MessageHandler.Instance.DebugLogsEnabled)
                {
                    MessageHandler.Instance.AddMessage(
                        MessageType.LOG,
                        $"[Scene] Model '{sceneObject.Name}' -> '{modelComponent.FilePath}' pos={position} scale={scale}{(config.SpawnModelsAtOrigin ? " (origin override)" : "")}");
                }
                spawned++;
            }

            return spawned;
        }

        private void AddModelToList(string? sceneObjectName, string modelPath, Model model)
        {
            string name = string.IsNullOrWhiteSpace(sceneObjectName)
                ? Path.GetFileNameWithoutExtension(modelPath)
                : sceneObjectName;

            var item = new ListViewItem(name)
            {
                Checked = model.IsVisible,
                Tag = model
            };
            item.SubItems.Add(modelPath);

            suppressModelListEvents = true;
            try
            {
                modelsListView.Items.Add(item);
            }
            finally
            {
                suppressModelListEvents = false;
            }
        }

        private void modelsListView_ItemChecked(object sender, ItemCheckedEventArgs e)
        {
            if (suppressModelListEvents)
            {
                return;
            }

            if (e.Item?.Tag is Model model)
            {
                model.SetVisible(e.Item.Checked);
                renderCtrl.Invalidate();
            }
        }

        private Matrix4 BuildModelMatrix(trinity_Transform? srt, bool forceOrigin, bool rotate180x, out Vector3 position, out Vector3 scaleOut)
        {
            position = Vector3.Zero;
            scaleOut = Vector3.One;
            if (srt == null)
            {
                if (!rotate180x)
                {
                    return Matrix4.Identity;
                }

                return Matrix4.CreateRotationX(MathHelper.Pi);
            }

            var scale = srt.Scale != null
                ? new Vector3(srt.Scale.X, srt.Scale.Y, srt.Scale.Z)
                : Vector3.One;
            scaleOut = scale;

            Quaternion rot = Quaternion.Identity;
            if (srt.Rotate != null)
            {
                // Flatbuffers Transform.Rotate uses WXYZ ordering.
                rot = new Quaternion(srt.Rotate.X, srt.Rotate.Y, srt.Rotate.Z, srt.Rotate.W);
                rot.Normalize();
            }

            var trans = srt.Translate != null
                ? new Vector3(srt.Translate.X, srt.Translate.Y, srt.Translate.Z)
                : Vector3.Zero;

            if (forceOrigin)
            {
                trans = Vector3.Zero;
            }
            position = trans;

            // OpenGL / OpenTK use column-vector math: world = M * local.
            // Scene transforms are authored as SRT, so build as T * R * S.
            var baseMat =
                Matrix4.CreateTranslation(trans) *
                Matrix4.CreateFromQuaternion(rot) *
                Matrix4.CreateScale(scale);

            if (!rotate180x)
            {
                return baseMat;
            }

            // Apply additional global rotation at the root to match alternate coordinate conventions.
            return Matrix4.CreateRotationX(MathHelper.Pi) * baseMat;
        }

        private string? ResolveModelPath(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || string.IsNullOrWhiteSpace(assetRoot))
            {
                return null;
            }

            // Allow absolute paths.
            if (Path.IsPathRooted(filePath))
            {
                if (File.Exists(filePath))
                {
                    return filePath;
                }

                // Some scene files embed absolute authoring paths. If the file isn't present, try to
                // re-root under the configured asset root by stripping to a known content folder.
                var rerooted = TryRerootUnderAssetRoot(filePath, assetRoot);
                if (rerooted != null)
                {
                    return rerooted;
                }
            }

            var normalized = filePath.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
            var combined = Path.GetFullPath(Path.Combine(assetRoot, normalized));
            if (File.Exists(combined))
            {
                return combined;
            }

            return null;
        }

        private static string? TryRerootUnderAssetRoot(string rootedPath, string assetRoot)
        {
            if (string.IsNullOrWhiteSpace(rootedPath) || string.IsNullOrWhiteSpace(assetRoot))
            {
                return null;
            }

            string normalized = rootedPath.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
            // Prefer earlier matches (closest to drive root).
            string[] knownRoots =
            {
                "field_graphic",
                "field",
                "common",
                "legend",
                "model",
                "effect",
                "ui"
            };

            foreach (var root in knownRoots)
            {
                string needle = Path.DirectorySeparatorChar + root + Path.DirectorySeparatorChar;
                int idx = normalized.IndexOf(needle, StringComparison.OrdinalIgnoreCase);
                if (idx < 0)
                {
                    continue;
                }

                // Keep the root folder name itself (e.g. "field\...").
                string relative = normalized.Substring(idx + 1);
                string candidate = Path.GetFullPath(Path.Combine(assetRoot, relative));
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }

            return null;
        }

        private string? ResolveSceneReference(string sceneFile, string referencedPath)
        {
            if (string.IsNullOrWhiteSpace(sceneFile) || string.IsNullOrWhiteSpace(referencedPath))
            {
                return null;
            }

            string baseDir = Path.GetDirectoryName(sceneFile) ?? string.Empty;
            string normalized = referencedPath.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);

            // Try direct path relative to the scene file.
            string candidate = Path.GetFullPath(Path.Combine(baseDir, normalized));
            if (File.Exists(candidate))
            {
                return candidate;
            }

            // Try common versioned suffixes: *_0.ext and *_1.ext.
            string ext = Path.GetExtension(candidate);
            if (!string.IsNullOrWhiteSpace(ext))
            {
                // If the reference already includes an explicit variant suffix, don't try to append another.
                string fileNameNoExt = Path.GetFileNameWithoutExtension(candidate);
                if (fileNameNoExt.EndsWith("_0", StringComparison.OrdinalIgnoreCase) ||
                    fileNameNoExt.EndsWith("_1", StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }

                string noExt = candidate.Substring(0, candidate.Length - ext.Length);
                var variants = preferredSceneVariant.HasValue
                    ? new[] { preferredSceneVariant.Value, preferredSceneVariant.Value == 0 ? 1 : 0 }
                    : new[] { 0, 1 };

                foreach (int variant in variants)
                {
                    string withVariant = $"{noExt}_{variant}{ext}";
                    if (File.Exists(withVariant))
                    {
                        return withVariant;
                    }
                }
            }

            return null;
        }

        private static int? TryDetectVariantFromPath(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return null;
            }

            string name = Path.GetFileNameWithoutExtension(path);
            if (name.EndsWith("_0", StringComparison.OrdinalIgnoreCase)) return 0;
            if (name.EndsWith("_1", StringComparison.OrdinalIgnoreCase)) return 1;
            return null;
        }
    }
}
