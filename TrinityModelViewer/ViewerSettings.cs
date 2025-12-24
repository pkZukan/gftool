using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace TrinityModelViewer
{
    public class ViewerSettings
    {
        public enum ShadingMode
        {
            Lit = 0,
            Toon = 1,
            Legacy = 2
        }

        public bool DarkMode { get; set; }
        public bool EnableNormalMaps { get; set; } = true;
        public bool EnableAO { get; set; } = true;
        public bool EnableVertexColors { get; set; } = false;
        public bool FlipNormalY { get; set; } = false;
        public bool ReconstructNormalZ { get; set; } = false;
        public ShadingMode DisplayShading { get; set; } = ShadingMode.Legacy;
        public bool ShowSkeleton { get; set; } = false;
        public bool LoadAllLods { get; set; } = false;
        public bool DebugLogs { get; set; } = false;
        public bool AutoLoadAnimations { get; set; } = false;
        public string LastModelPath { get; set; } = string.Empty;

        public static ViewerSettings Load()
        {
            try
            {
                var path = GetSettingsPath();
                if (!File.Exists(path))
                {
                    return new ViewerSettings();
                }

                var json = File.ReadAllText(path);
                var root = JsonNode.Parse(json) as JsonObject;
                if (root != null)
                {
                    var viewerNode = root["modelViewer"];
                    if (viewerNode != null)
                    {
                        return viewerNode.Deserialize<ViewerSettings>() ?? new ViewerSettings();
                    }
                }

                return JsonSerializer.Deserialize<ViewerSettings>(json) ?? new ViewerSettings();
            }
            catch
            {
                return new ViewerSettings();
            }
        }

        public void Save()
        {
            try
            {
                var path = GetSettingsPath();
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

                root["modelViewer"] = JsonSerializer.SerializeToNode(this);
                var json = root.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(path, json);
            }
            catch
            {
                // Ignore settings write failures.
            }
        }

        private static string GetSettingsPath()
        {
            return Path.Combine(Directory.GetCurrentDirectory(), "settings.json");
        }
    }
}
