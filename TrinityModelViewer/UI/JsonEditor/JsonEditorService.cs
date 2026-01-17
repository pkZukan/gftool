using GFTool.Renderer.Scene.GraphicsObjects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Trinity.Core.Utils;

namespace TrinityModelViewer.UI.JsonEditor
{
    internal sealed class JsonEditorService
    {
        internal readonly record struct JsonEditorEntry
        {
            public string Type { get; init; }
            public string ModelName { get; init; }
            public string Path { get; init; }
            public Model? Model { get; init; }
        }

        public IEnumerable<JsonEditorEntry> EnumerateFlatbufferEntriesInScene(
            IReadOnlyDictionary<Model, string> modelSourcePaths,
            Action<string>? log = null)
        {
            var results = new List<JsonEditorEntry>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var kv in modelSourcePaths)
            {
                var model = kv.Key;
                var trmdlPath = kv.Value;
                if (model == null || string.IsNullOrWhiteSpace(trmdlPath) || !File.Exists(trmdlPath))
                {
                    continue;
                }

                string modelName = model.Name ?? Path.GetFileNameWithoutExtension(trmdlPath);

                void Add(string type, string p)
                {
                    if (string.IsNullOrWhiteSpace(p) || !File.Exists(p) || !seen.Add(p))
                    {
                        return;
                    }

                    results.Add(new JsonEditorEntry { Type = type, ModelName = modelName, Path = p, Model = model });
                }

                Add("TRMDL", trmdlPath);

                Trinity.Core.Flatbuffers.TR.Model.TRMDL? trmdl = null;
                try
                {
                    trmdl = FlatBufferConverter.DeserializeFrom<Trinity.Core.Flatbuffers.TR.Model.TRMDL>(File.ReadAllBytes(trmdlPath));
                }
                catch (Exception ex)
                {
                    log?.Invoke($"[JsonEditor] TRMDL decode failed '{trmdlPath}': {ex.Message}");
                }

                if (trmdl == null)
                {
                    continue;
                }

                var baseDir = Path.GetDirectoryName(trmdlPath) ?? ".";

                if (trmdl.Skeleton?.PathName != null)
                {
                    var skel = ResolveRelativePath(baseDir, trmdl.Skeleton.PathName);
                    Add("TRSKL", skel);
                }

                foreach (var mesh in trmdl.Meshes ?? Array.Empty<Trinity.Core.Flatbuffers.TR.Model.ModelMesh>())
                {
                    if (string.IsNullOrWhiteSpace(mesh?.PathName))
                    {
                        continue;
                    }

                    var trmshPath = ResolveRelativePath(baseDir, mesh.PathName);
                    Add("TRMSH", trmshPath);

                    try
                    {
                        var trmsh = FlatBufferConverter.DeserializeFrom<Trinity.Core.Flatbuffers.TR.Model.TRMSH>(File.ReadAllBytes(trmshPath));
                        if (trmsh?.bufferFilePath != null)
                        {
                            var trmbfPath = ResolveRelativePath(baseDir, trmsh.bufferFilePath);
                            Add("TRMBF", trmbfPath);
                        }
                    }
                    catch
                    {
                        // ignore
                    }
                }

                foreach (var mat in trmdl.Materials ?? Array.Empty<string>())
                {
                    if (string.IsNullOrWhiteSpace(mat))
                    {
                        continue;
                    }

                    var trmtrPath = ResolveRelativePath(baseDir, mat);
                    Add("TRMTR", trmtrPath);

                    var trmmtAdjacent = Path.ChangeExtension(trmtrPath, ".trmmt");
                    Add("TRMMT", trmmtAdjacent);
                }

                var trmmtPath = Path.ChangeExtension(trmdlPath, ".trmmt");
                Add("TRMMT", trmmtPath);
                AddTrmtrReferencesFromTrmmt(baseDir, trmmtPath, Add);
            }

            return results;
        }

        public IEnumerable<JsonEditorEntry> CreateManualEntries(
            IEnumerable<string> filePaths,
            IEnumerable<string> existingPaths,
            Model? defaultModel)
        {
            var seen = new HashSet<string>(existingPaths ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);

            foreach (var path in filePaths ?? Array.Empty<string>())
            {
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path) || !seen.Add(path))
                {
                    continue;
                }

                yield return new JsonEditorEntry
                {
                    Type = GetKindFromPath(path),
                    ModelName = Path.GetFileNameWithoutExtension(path),
                    Path = path,
                    Model = defaultModel
                };
            }
        }

        public string BuildFlatbufferJson(string kind, byte[] bytes)
        {
            var opts = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };

            return kind switch
            {
                "TRMDL" => Trinity.Core.Flatbuffers.Reflections.FlatbufferReflectionJsonDumper.Dump(
                    bytes,
                    Trinity.Core.Flatbuffers.Reflections.PokeDocsBfbsRegistry.GetModelSchema(
                        Trinity.Core.Flatbuffers.Reflections.PokeDocsGame.SV,
                        Trinity.Core.Flatbuffers.Reflections.PokeDocsModelSchema.Trmdl)),
                "TRMSH" => Trinity.Core.Flatbuffers.Reflections.FlatbufferReflectionJsonDumper.Dump(
                    bytes,
                    Trinity.Core.Flatbuffers.Reflections.PokeDocsBfbsRegistry.GetModelSchema(
                        Trinity.Core.Flatbuffers.Reflections.PokeDocsGame.SV,
                        Trinity.Core.Flatbuffers.Reflections.PokeDocsModelSchema.Trmsh)),
                "TRMBF" => Trinity.Core.Flatbuffers.Reflections.FlatbufferReflectionJsonDumper.Dump(
                    bytes,
                    Trinity.Core.Flatbuffers.Reflections.PokeDocsBfbsRegistry.GetModelSchema(
                        Trinity.Core.Flatbuffers.Reflections.PokeDocsGame.SV,
                        Trinity.Core.Flatbuffers.Reflections.PokeDocsModelSchema.Trmbf)),
                "TRSKL" => Trinity.Core.Flatbuffers.Reflections.FlatbufferReflectionJsonDumper.Dump(
                    bytes,
                    Trinity.Core.Flatbuffers.Reflections.PokeDocsBfbsRegistry.GetModelSchema(
                        Trinity.Core.Flatbuffers.Reflections.PokeDocsGame.SV,
                        Trinity.Core.Flatbuffers.Reflections.PokeDocsModelSchema.Trskl)),
                "TRMTR" => System.Text.Json.JsonSerializer.Serialize(
                    FlatBufferConverter.DeserializeFrom<Trinity.Core.Flatbuffers.TR.Model.TrmtrFile>(bytes), opts),
                "TRMMT" => BuildTrmmtJson(bytes, opts),
                _ => throw new NotSupportedException($"Unsupported kind: {kind}")
            };

            static string BuildTrmmtJson(byte[] bytes2, System.Text.Json.JsonSerializerOptions opts2)
            {
                try
                {
                    var meta = FlatBufferConverter.DeserializeFrom<Trinity.Core.Flatbuffers.TR.Model.TrmmtMetadataFile>(bytes2);
                    if (meta?.ItemList != null && meta.ItemList.Length > 0)
                    {
                        return System.Text.Json.JsonSerializer.Serialize(meta, opts2);
                    }
                }
                catch
                {
                    // ignore
                }

                return System.Text.Json.JsonSerializer.Serialize(
                    FlatBufferConverter.DeserializeFrom<Trinity.Core.Flatbuffers.TR.Model.TrmmtFile>(bytes2), opts2);
            }
        }

        private static void AddTrmtrReferencesFromTrmmt(string baseDir, string trmmtPath, Action<string, string> add)
        {
            if (string.IsNullOrWhiteSpace(trmmtPath) || !File.Exists(trmmtPath))
            {
                return;
            }

            byte[] bytes;
            try
            {
                bytes = File.ReadAllBytes(trmmtPath);
            }
            catch
            {
                return;
            }

            try
            {
                var meta = FlatBufferConverter.DeserializeFrom<Trinity.Core.Flatbuffers.TR.Model.TrmmtMetadataFile>(bytes);
                if (meta?.ItemList != null && meta.ItemList.Length > 0)
                {
                    foreach (var item in meta.ItemList)
                    {
                        foreach (var p in item?.MaterialPathList ?? Array.Empty<string>())
                        {
                            if (string.IsNullOrWhiteSpace(p))
                            {
                                continue;
                            }

                            add("TRMTR", ResolveRelativePath(baseDir, p));
                        }
                    }
                    return;
                }
            }
            catch
            {
                // ignore and try set-mapping flavor
            }

            try
            {
                var setMap = FlatBufferConverter.DeserializeFrom<Trinity.Core.Flatbuffers.TR.Model.TrmmtFile>(bytes);
                foreach (var entry in setMap?.Material ?? Array.Empty<Trinity.Core.Flatbuffers.TR.Model.TrmmtEntry>())
                {
                    foreach (var p in entry?.MaterialNames ?? Array.Empty<string>())
                    {
                        if (string.IsNullOrWhiteSpace(p))
                        {
                            continue;
                        }

                        add("TRMTR", ResolveRelativePath(baseDir, p));
                    }
                }
            }
            catch
            {
                // ignore
            }
        }

        private static string ResolveRelativePath(string baseDir, string raw)
        {
            raw = raw.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
            if (Path.IsPathRooted(raw))
            {
                return raw;
            }
            return Path.GetFullPath(Path.Combine(baseDir, raw));
        }

        private static string GetKindFromPath(string path)
        {
            string kind = Path.GetExtension(path).TrimStart('.').ToUpperInvariant();
            return kind == "TRSKL1" ? "TRSKL" : kind;
        }
    }
}
