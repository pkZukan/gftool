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
        private static (ModelMesh[] Meshes, TRBoundingBox Lod0Bounds, HashSet<string> MaterialNames) ExportTrinityMeshesFromReferenceTemplate(
            TRMDL referenceTrmdl,
            string referenceDir,
            string outputDir,
            string outputBase,
            string referenceTrmdlPath,
            IReadOnlyList<TrinityPrimitive> gltfPrims)
        {
            var referenceBase = Path.GetFileNameWithoutExtension(referenceTrmdlPath);

            string RemapRelativePath(string? relativePath)
            {
                if (string.IsNullOrWhiteSpace(relativePath))
                {
                    return string.Empty;
                }

                var normalized = relativePath.Replace('\\', '/');
                int lastSlash = normalized.LastIndexOf('/');
                var dir = lastSlash > 0 ? normalized.Substring(0, lastSlash) : string.Empty;
                var file = lastSlash >= 0 ? normalized.Substring(lastSlash + 1) : normalized;
                var ext = Path.GetExtension(file);
                var stem = Path.GetFileNameWithoutExtension(file);

                if (!string.IsNullOrWhiteSpace(referenceBase) && stem.StartsWith(referenceBase, StringComparison.OrdinalIgnoreCase))
                {
                    stem = outputBase + stem.Substring(referenceBase.Length);
                    var remapped = stem + ext;
                    return string.IsNullOrWhiteSpace(dir) ? remapped : $"{dir}/{remapped}";
                }

                return normalized;
            }

            if (referenceTrmdl.Meshes == null || referenceTrmdl.Meshes.Length == 0)
            {
                throw new InvalidOperationException("Reference TRMDL has no meshes.");
            }

            var outMeshes = new ModelMesh[referenceTrmdl.Meshes.Length];
            var materialNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            TRBoundingBox lod0Bounds = new TRBoundingBox
            {
                MinBound = new Vector3f { X = 0, Y = 0, Z = 0 },
                MaxBound = new Vector3f { X = 0, Y = 0, Z = 0 }
            };

            for (int meshIndex = 0; meshIndex < referenceTrmdl.Meshes.Length; meshIndex++)
            {
                var refMeshRel = referenceTrmdl.Meshes[meshIndex]?.PathName;
                if (string.IsNullOrWhiteSpace(refMeshRel))
                {
                    throw new InvalidOperationException($"Reference TRMDL mesh entry {meshIndex} has no PathName.");
                }

                var refTrmshPath = Path.Combine(referenceDir, refMeshRel);
                if (!File.Exists(refTrmshPath))
                {
                    throw new FileNotFoundException("Reference TRMSH not found.", refTrmshPath);
                }

                var refTrmsh = FlatBufferConverter.DeserializeFrom<TRMSH>(refTrmshPath);
                if (refTrmsh == null)
                {
                    throw new InvalidOperationException($"Failed to deserialize reference TRMSH: {refTrmshPath}");
                }

                var refTrmbfRel = refTrmsh.bufferFilePath;
                if (string.IsNullOrWhiteSpace(refTrmbfRel))
                {
                    throw new InvalidOperationException($"Reference TRMSH '{refMeshRel}' has no bufferFilePath.");
                }

                var refTrmbfPath = Path.Combine(referenceDir, refTrmbfRel);
                if (!File.Exists(refTrmbfPath))
                {
                    throw new FileNotFoundException("Reference TRMBF not found.", refTrmbfPath);
                }

                var outTrmshRel = RemapRelativePath(refMeshRel);
                var outTrmbfRel = RemapRelativePath(refTrmbfRel);
                outMeshes[meshIndex] = new ModelMesh { PathName = outTrmshRel };

                if (meshIndex == 0)
                {
                    var refTrmbf = FlatBufferConverter.DeserializeFrom<TRMBF>(refTrmbfPath);
                    var (outTrmsh, outTrmbf, bounds) = BuildTrinityMeshFilesFromTemplate(gltfPrims, refTrmsh, refTrmbf, outTrmbfRel, materialNames);
                    lod0Bounds = bounds;

                    var outTrmshPath = Path.Combine(outputDir, outTrmshRel);
                    var outTrmbfPath = Path.Combine(outputDir, outTrmbfRel);
                    var outTrmshDir = Path.GetDirectoryName(outTrmshPath);
                    if (!string.IsNullOrWhiteSpace(outTrmshDir))
                    {
                        Directory.CreateDirectory(outTrmshDir);
                    }
                    var outTrmbfDir = Path.GetDirectoryName(outTrmbfPath);
                    if (!string.IsNullOrWhiteSpace(outTrmbfDir))
                    {
                        Directory.CreateDirectory(outTrmbfDir);
                    }

                    File.WriteAllBytes(outTrmshPath, FlatBufferConverter.SerializeFrom(outTrmsh));
                    File.WriteAllBytes(outTrmbfPath, FlatBufferConverter.SerializeFrom(outTrmbf));
                }
                else
                {
                    // Copy non-LOD0 mesh files verbatim but remap the bufferFilePath so the output folder is self-contained.
                    // This avoids mismatches in TRMDL LOD references without needing to regenerate other LOD meshes.
                    var outTrmsh = new TRMSH
                    {
                        Version = refTrmsh.Version,
                        Meshes = refTrmsh.Meshes,
                        bufferFilePath = outTrmbfRel
                    };

                    var outTrmshPath = Path.Combine(outputDir, outTrmshRel);
                    var outTrmbfPath = Path.Combine(outputDir, outTrmbfRel);
                    var outTrmshDir = Path.GetDirectoryName(outTrmshPath);
                    if (!string.IsNullOrWhiteSpace(outTrmshDir))
                    {
                        Directory.CreateDirectory(outTrmshDir);
                    }
                    var outTrmbfDir = Path.GetDirectoryName(outTrmbfPath);
                    if (!string.IsNullOrWhiteSpace(outTrmbfDir))
                    {
                        Directory.CreateDirectory(outTrmbfDir);
                    }

                    File.WriteAllBytes(outTrmshPath, FlatBufferConverter.SerializeFrom(outTrmsh));
                    File.Copy(refTrmbfPath, outTrmbfPath, overwrite: true);
                }
            }

            return (outMeshes, lod0Bounds, materialNames);
        }
    }
}
