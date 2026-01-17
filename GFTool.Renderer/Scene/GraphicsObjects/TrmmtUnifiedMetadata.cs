using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Trinity.Core.Flatbuffers.TR.Model;
using Trinity.Core.Utils;

namespace GFTool.Renderer.Scene.GraphicsObjects
{
    internal sealed class TrmmtUnifiedMetadata
    {
        public string SourcePath { get; }
        public TrmmtFile? SetMetadata { get; }
        public TrmmtMetadataFile? ParamMetadata { get; }

        private readonly IReadOnlyList<string> setNames;
        private readonly Dictionary<string, string[]> trmtrCandidatesBySetName;

        private TrmmtUnifiedMetadata(
            string sourcePath,
            TrmmtFile? setMetadata,
            TrmmtMetadataFile? paramMetadata,
            IReadOnlyList<string> setNames,
            Dictionary<string, string[]> trmtrCandidatesBySetName)
        {
            SourcePath = sourcePath;
            SetMetadata = setMetadata;
            ParamMetadata = paramMetadata;
            this.setNames = setNames;
            this.trmtrCandidatesBySetName = trmtrCandidatesBySetName;
        }

        public static TrmmtUnifiedMetadata? TryParse(string path, byte[] bytes, bool debugLogsEnabled, Action<string>? log)
        {
            if (string.IsNullOrWhiteSpace(path) || bytes == null || bytes.Length == 0)
            {
                return null;
            }

            TrmmtFile? setMeta = null;
            TrmmtMetadataFile? paramMeta = null;

            try
            {
                setMeta = FlatBufferConverter.DeserializeFrom<TrmmtFile>(bytes);
            }
            catch
            {
                setMeta = null;
            }

            try
            {
                paramMeta = FlatBufferConverter.DeserializeFrom<TrmmtMetadataFile>(bytes);
            }
            catch
            {
                paramMeta = null;
            }

            if (!LooksLikeMaterialSetMetadata(setMeta))
            {
                setMeta = null;
            }

            if (!LooksLikeMaterialParamMetadata(paramMeta))
            {
                paramMeta = null;
            }

            if (setMeta == null && paramMeta == null)
            {
                return null;
            }

            var names = new List<string>();
            var candidates = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

            void AddSetName(string? rawName, int fallbackIndex)
            {
                string name = NormalizeSetName(rawName, fallbackIndex);
                if (!names.Contains(name, StringComparer.OrdinalIgnoreCase))
                {
                    names.Add(name);
                }
            }

            void AddCandidate(string setName, string? pathValue)
            {
                if (string.IsNullOrWhiteSpace(pathValue))
                {
                    return;
                }

                if (!pathValue.EndsWith(".trmtr", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                if (!candidates.TryGetValue(setName, out var list))
                {
                    list = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    candidates[setName] = list;
                }

                list.Add(pathValue);
            }

            if (setMeta?.Material != null)
            {
                for (int i = 0; i < setMeta.Material.Length; i++)
                {
                    var item = setMeta.Material[i];
                    if (item == null)
                    {
                        continue;
                    }

                    string setName = NormalizeSetName(item.Name, i);
                    AddSetName(setName, i);

                    foreach (var m in item.MaterialNames ?? Array.Empty<string>())
                    {
                        AddCandidate(setName, m);
                    }
                }
            }

            if (paramMeta?.ItemList != null)
            {
                for (int i = 0; i < paramMeta.ItemList.Length; i++)
                {
                    var item = paramMeta.ItemList[i];
                    if (item == null)
                    {
                        continue;
                    }

                    string setName = NormalizeSetName(item.Name, i);
                    AddSetName(setName, i);

                    foreach (var m in item.MaterialPathList ?? Array.Empty<string>())
                    {
                        AddCandidate(setName, m);
                    }
                }
            }

            var finalCandidates = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in candidates)
            {
                finalCandidates[kv.Key] = kv.Value.Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
            }

            names.Sort(StringComparer.OrdinalIgnoreCase);

            if (debugLogsEnabled && log != null)
            {
                log($"[TRMMT] Parsed '{path}': sets={setMeta?.Material?.Length ?? 0} metaItems={paramMeta?.ItemList?.Length ?? 0} unifiedSets={names.Count}");
            }

            return new TrmmtUnifiedMetadata(path, setMeta, paramMeta, names, finalCandidates);
        }

        public IReadOnlyList<string> GetSetNames()
        {
            return setNames;
        }

        public IEnumerable<string> EnumerateTrmtrCandidatesForSet(string setName)
        {
            if (string.IsNullOrWhiteSpace(setName))
            {
                return Array.Empty<string>();
            }

            return trmtrCandidatesBySetName.TryGetValue(setName, out var list) ? list : Array.Empty<string>();
        }

        public TrmmtMetaItem? FindParamItem(string? setName, string? currentMaterialFilePath)
        {
            var meta = ParamMetadata;
            if (meta?.ItemList == null || meta.ItemList.Length == 0)
            {
                return null;
            }

            if (!string.IsNullOrWhiteSpace(setName))
            {
                var byName = meta.ItemList.FirstOrDefault(i => string.Equals(NormalizeSetName(i?.Name, 0), setName, StringComparison.OrdinalIgnoreCase));
                if (byName != null)
                {
                    return byName;
                }
            }

            if (!string.IsNullOrWhiteSpace(currentMaterialFilePath))
            {
                var fileName = Path.GetFileName(currentMaterialFilePath);
                foreach (var item in meta.ItemList)
                {
                    if (item?.MaterialPathList == null || item.MaterialPathList.Length == 0)
                    {
                        continue;
                    }

                    if (item.MaterialPathList.Any(p => string.Equals(Path.GetFileName(p), fileName, StringComparison.OrdinalIgnoreCase)))
                    {
                        return item;
                    }
                }
            }

            return meta.ItemList.Length == 1 ? meta.ItemList[0] : null;
        }

        public string? InferSetNameFromCurrentTrmtr(string? currentMaterialFilePath)
        {
            if (string.IsNullOrWhiteSpace(currentMaterialFilePath))
            {
                return null;
            }

            string fileName = Path.GetFileName(currentMaterialFilePath);

            if (SetMetadata?.Material != null)
            {
                for (int i = 0; i < SetMetadata.Material.Length; i++)
                {
                    var item = SetMetadata.Material[i];
                    if (item?.MaterialNames == null)
                    {
                        continue;
                    }

                    if (item.MaterialNames.Any(p => string.Equals(Path.GetFileName(p), fileName, StringComparison.OrdinalIgnoreCase)))
                    {
                        return NormalizeSetName(item.Name, i);
                    }
                }
            }

            if (ParamMetadata?.ItemList != null)
            {
                for (int i = 0; i < ParamMetadata.ItemList.Length; i++)
                {
                    var item = ParamMetadata.ItemList[i];
                    if (item?.MaterialPathList == null)
                    {
                        continue;
                    }

                    if (item.MaterialPathList.Any(p => string.Equals(Path.GetFileName(p), fileName, StringComparison.OrdinalIgnoreCase)))
                    {
                        return NormalizeSetName(item.Name, i);
                    }
                }
            }

            return null;
        }

        private static string NormalizeSetName(string? name, int fallbackIndex)
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                return name!;
            }

            // Keep a stable placeholder when the source doesn't name the set.
            return fallbackIndex == 0 ? "default" : $"set_{fallbackIndex}";
        }

        private static bool LooksLikeMaterialSetMetadata(TrmmtFile? meta)
        {
            if (meta?.Material == null || meta.Material.Length == 0)
            {
                return false;
            }

            foreach (var item in meta.Material)
            {
                if (item == null)
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(item.Name))
                {
                    return true;
                }

                if (item.MaterialNames != null && item.MaterialNames.Any(p => !string.IsNullOrWhiteSpace(p) && p.EndsWith(".trmtr", StringComparison.OrdinalIgnoreCase)))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool LooksLikeMaterialParamMetadata(TrmmtMetadataFile? meta)
        {
            if (meta?.ItemList == null || meta.ItemList.Length == 0)
            {
                return false;
            }

            foreach (var item in meta.ItemList)
            {
                if (item == null)
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(item.Name))
                {
                    return true;
                }

                if (item.MaterialPathList != null && item.MaterialPathList.Any(p => !string.IsNullOrWhiteSpace(p) && p.EndsWith(".trmtr", StringComparison.OrdinalIgnoreCase)))
                {
                    return true;
                }

                if (item.ParamList != null && item.ParamList.Any(p => p != null && p.UseNoAnime && (p.NoAnimeParam?.VariationCount ?? 0) > 0))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
