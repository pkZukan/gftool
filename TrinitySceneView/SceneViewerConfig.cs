using System.Text.Json;
using System.Text.Json.Nodes;

namespace TrinitySceneView
{
    internal sealed class SceneViewerConfig
    {
        public string? AssetRoot { get; set; }
        public bool DebugLogs { get; set; } = false;
        public bool DarkMode { get; set; } = false;
        public bool SpawnModelsAtOrigin { get; set; } = false;
        public bool LargeClipPlanes { get; set; } = true;
        public bool RotateModels180X { get; set; } = false;

        public static SceneViewerConfig Load()
        {
            try
            {
                var path = GetConfigPath();
                if (!File.Exists(path))
                {
                    return new SceneViewerConfig();
                }

                var json = File.ReadAllText(path);
                var root = JsonNode.Parse(json) as JsonObject;
                if (root != null)
                {
                    var viewerNode = root["sceneViewer"];
                    if (viewerNode != null)
                    {
                        return viewerNode.Deserialize<SceneViewerConfig>() ?? new SceneViewerConfig();
                    }
                }

                return JsonSerializer.Deserialize<SceneViewerConfig>(json) ?? new SceneViewerConfig();
            }
            catch
            {
                return new SceneViewerConfig();
            }
        }

        public void Save()
        {
            try
            {
                var path = GetConfigPath();
                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                var root = new JsonObject();
                if (File.Exists(path))
                {
                    var existing = JsonNode.Parse(File.ReadAllText(path)) as JsonObject;
                    if (existing != null)
                    {
                        root = existing;
                    }
                }

                root["sceneViewer"] = JsonSerializer.SerializeToNode(this);
                var json = root.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(path, json);
            }
            catch
            {
                // Best-effort; ignore config save failures.
            }
        }

        private static string GetConfigPath()
        {
            return Path.Combine(Directory.GetCurrentDirectory(), "settings.json");
        }
    }
}
