using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using OpenTK.Mathematics;
using Trinity.Core.Flatbuffers.TR.Model;
using Trinity.Core.Flatbuffers.Utils;
using Trinity.Core.Utils;


namespace TrinityModelViewer.Export
{
    internal static partial class GltfTrinityPipeline
    {
        private static void AddSiblingTrmtrVariants(string referenceDir, List<(string RelativePath, string SourcePath)> trmtrFilesToCopy)
        {
            var existing = new HashSet<string>(trmtrFilesToCopy.Select(t => t.RelativePath.Replace('\\', '/')), StringComparer.OrdinalIgnoreCase);

            foreach (var (rel, srcPath) in trmtrFilesToCopy.ToArray())
            {
                if (string.IsNullOrWhiteSpace(rel))
                {
                    continue;
                }

                var normalizedRel = rel.Replace('\\', '/');
                var dirRel = Path.GetDirectoryName(normalizedRel)?.Replace('\\', '/') ?? string.Empty;
                var fileName = Path.GetFileName(normalizedRel);
                var baseStem = Path.GetFileNameWithoutExtension(fileName);
                if (string.IsNullOrWhiteSpace(baseStem))
                {
                    continue;
                }

                var dirOnDisk = string.IsNullOrWhiteSpace(dirRel) ? referenceDir : Path.Combine(referenceDir, dirRel);
                if (!Directory.Exists(dirOnDisk))
                {
                    continue;
                }

                string prefix = baseStem + "_";
                foreach (var full in Directory.EnumerateFiles(dirOnDisk, prefix + "*.trmtr"))
                {
                    var variantName = Path.GetFileName(full);
                    if (string.IsNullOrWhiteSpace(variantName))
                    {
                        continue;
                    }

                    var variantRel = string.IsNullOrWhiteSpace(dirRel) ? variantName : $"{dirRel}/{variantName}";
                    if (existing.Add(variantRel))
                    {
                        trmtrFilesToCopy.Add((variantRel, full));
                    }
                }
            }
        }

        private static void CopyMaterialMetadata(string referenceDir, string outputDir, string outBase, string[] referenceMaterialPaths)
        {
            // Copy `<material>.trmmt` next to each referenced TRMTR if it exists.
            for (int i = 0; i < referenceMaterialPaths.Length; i++)
            {
                var rel = referenceMaterialPaths[i];
                if (string.IsNullOrWhiteSpace(rel))
                {
                    continue;
                }

                var relTrmmt = Path.ChangeExtension(rel, ".trmmt");
                if (string.IsNullOrWhiteSpace(relTrmmt))
                {
                    continue;
                }

                var src = Path.Combine(referenceDir, relTrmmt);
                if (!File.Exists(src))
                {
                    continue;
                }

                var dst = Path.Combine(outputDir, relTrmmt);
                var dstDir = Path.GetDirectoryName(dst);
                if (!string.IsNullOrWhiteSpace(dstDir))
                {
                    Directory.CreateDirectory(dstDir);
                }

                File.Copy(src, dst, overwrite: true);

                // Also populate the preferred `<outBase>.trmmt` if it's not already present.
                var preferredDst = Path.Combine(outputDir, outBase + ".trmmt");
                if (!File.Exists(preferredDst))
                {
                    try
                    {
                        File.Copy(src, preferredDst, overwrite: false);
                    }
                    catch
                    {
                        // Ignore.
                    }
                }
            }
        }
    }
}
