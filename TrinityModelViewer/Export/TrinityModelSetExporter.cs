using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Trinity.Core.Flatbuffers.TR.Model;
using Trinity.Core.Flatbuffers.Utils;
using Trinity.Core.Utils;

namespace TrinityModelViewer.Export
{
    internal static class TrinityModelSetExporter
    {
        public static void ExportCopy(string referenceTrmdlPath, string outputTrmdlPath)
        {
            if (string.IsNullOrWhiteSpace(referenceTrmdlPath)) throw new ArgumentException("Missing reference TRMDL path.", nameof(referenceTrmdlPath));
            if (string.IsNullOrWhiteSpace(outputTrmdlPath)) throw new ArgumentException("Missing output TRMDL path.", nameof(outputTrmdlPath));

            if (!File.Exists(referenceTrmdlPath)) throw new FileNotFoundException("Reference TRMDL not found.", referenceTrmdlPath);

            var referenceDir = Path.GetDirectoryName(referenceTrmdlPath) ?? Environment.CurrentDirectory;
            var outputDir = Path.GetDirectoryName(outputTrmdlPath) ?? Environment.CurrentDirectory;
            Directory.CreateDirectory(outputDir);

            var trmdl = FlatBufferConverter.DeserializeFrom<TRMDL>(referenceTrmdlPath);
            if (trmdl == null)
            {
                throw new InvalidOperationException($"Failed to deserialize TRMDL: {referenceTrmdlPath}");
            }

            var relPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            void AddRel(string? rel)
            {
                if (string.IsNullOrWhiteSpace(rel))
                {
                    return;
                }

                rel = rel.Replace('\\', '/');
                relPaths.Add(rel);
            }

            if (trmdl.Meshes != null)
            {
                foreach (var mesh in trmdl.Meshes)
                {
                    AddRel(mesh?.PathName);
                }
            }

            AddRel(trmdl.Skeleton?.PathName);

            if (trmdl.Materials != null)
            {
                foreach (var mat in trmdl.Materials)
                {
                    AddRel(mat);
                }
            }


            // Copy TRMBF dependencies referenced from TRMSH.
            foreach (var meshRel in relPaths.Where(p => p.EndsWith(".trmsh", StringComparison.OrdinalIgnoreCase)).ToList())
            {
                var trmshPath = Path.Combine(referenceDir, meshRel);
                if (!File.Exists(trmshPath))
                {
                    continue;
                }

                try
                {
                    var trmsh = FlatBufferConverter.DeserializeFrom<TRMSH>(trmshPath);
                    AddRel(trmsh?.bufferFilePath);
                }
                catch
                {
                    // Ignore; exporting unchanged still works if the TRMBF is already included or not required by the caller.
                }
            }

            // Copy the TRMDL to the selected output filename (do not rewrite internal paths).
            File.Copy(referenceTrmdlPath, outputTrmdlPath, overwrite: true);

            // Copy referenced dependencies with their original relative paths.
            foreach (var rel in relPaths)
            {
                var src = Path.Combine(referenceDir, rel);
                if (!File.Exists(src))
                {
                    continue;
                }

                var dst = Path.Combine(outputDir, rel);
                var dstDir = Path.GetDirectoryName(dst);
                if (!string.IsNullOrWhiteSpace(dstDir))
                {
                    Directory.CreateDirectory(dstDir);
                }

                if (!string.Equals(Path.GetFullPath(src), Path.GetFullPath(dst), StringComparison.OrdinalIgnoreCase))
                {
                    File.Copy(src, dst, overwrite: true);
                }
            }
        }
    }
}
