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
        public static void Export(
            string referenceTrmdlPath,
            string gltfPath,
            string outputTrmdlPath,
            bool patchBaseColorTextures = false,
            bool exportModelPcBaseOnExport = true)
        {
            if (string.IsNullOrWhiteSpace(referenceTrmdlPath)) throw new ArgumentException("Missing reference TRMDL path.", nameof(referenceTrmdlPath));
            if (string.IsNullOrWhiteSpace(gltfPath)) throw new ArgumentException("Missing glTF path.", nameof(gltfPath));
            if (string.IsNullOrWhiteSpace(outputTrmdlPath)) throw new ArgumentException("Missing output TRMDL path.", nameof(outputTrmdlPath));

            if (!File.Exists(referenceTrmdlPath)) throw new FileNotFoundException("Reference TRMDL not found.", referenceTrmdlPath);
            if (!File.Exists(gltfPath)) throw new FileNotFoundException("glTF not found.", gltfPath);

            // TRMTR patching currently requires lossless read/write of the full material schema.
            // Our current FlatSharp bindings do not cover all observed fields in ZA materials,
            // and re-serializing can drop required render state, causing game crashes.
            if (patchBaseColorTextures)
            {
                throw new NotSupportedException("TRMTR patching is currently disabled. Export will copy reference TRMTR files verbatim.");
            }

            var referenceDir = Path.GetDirectoryName(referenceTrmdlPath) ?? Environment.CurrentDirectory;
            var outputDir = Path.GetDirectoryName(outputTrmdlPath) ?? Environment.CurrentDirectory;
            Directory.CreateDirectory(outputDir);

            var outBase = Path.GetFileNameWithoutExtension(outputTrmdlPath);
            var outTrmtrName = outBase + ".trmtr";
            var texDirName = $"{outBase}_textures";
            var texOutDir = Path.Combine(outputDir, texDirName);
            if (patchBaseColorTextures)
            {
                Directory.CreateDirectory(texOutDir);
            }

            var referenceTrmdl = FlatBufferConverter.DeserializeFrom<TRMDL>(referenceTrmdlPath);
            var skeletonRelPath = referenceTrmdl.Skeleton?.PathName;
            TRSKL? referenceSkl = null;
            var skeletonSrcPath = TryResolveReferenceSkeletonPath(referenceTrmdlPath, referenceTrmdl, referenceDir);
            if (!string.IsNullOrWhiteSpace(skeletonSrcPath) && File.Exists(skeletonSrcPath))
            {
                referenceSkl = TryLoadMergedReferenceSkeleton(referenceTrmdlPath, referenceTrmdl, referenceDir, skeletonSrcPath) ??
                               FlatBufferConverter.DeserializeFrom<TRSKL>(skeletonSrcPath);
            }

            var boneNameToJointInfoIndex = BuildBoneNameToJointInfoIndex(referenceSkl);

            var gltf = GltfReader.Load(gltfPath);
            var meshes = GltfReader.ExtractMeshPrimitives(gltf, boneNameToJointInfoIndex);
            if (meshes.Count == 0)
            {
                throw new InvalidOperationException("glTF contains no mesh primitives to export.");
            }
            if (meshes.Any(m => m.HasSkinning) && boneNameToJointInfoIndex.Count == 0)
            {
                throw new InvalidOperationException(
                    "Reference skeleton could not be resolved (needed to map skinning joints). " +
                    "Make sure the reference TRMDL's TRSKL (or the shared base skeleton like p0_base.trskl) is present on disk.");
            }

            var (outMeshes, lod0Bounds, materialNames) = ExportTrinityMeshesFromReferenceTemplate(
                referenceTrmdl,
                referenceDir,
                outputDir,
                outBase,
                referenceTrmdlPath,
                meshes);
            var referenceMaterialPaths = (referenceTrmdl.Materials ?? Array.Empty<string>())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToArray();

            var trmtrFilesToCopy = new List<(string RelativePath, string SourcePath)>();
            var trmtrsToWrite = new List<(string RelativePath, TRMTR Trmtr)>();
            if (referenceMaterialPaths.Length > 0)
            {
                foreach (var rel in referenceMaterialPaths)
                {
                    var srcPath = Path.Combine(referenceDir, rel);
                    if (!File.Exists(srcPath))
                    {
                        continue;
                    }

                    // Copy verbatim to avoid schema loss.
                    trmtrFilesToCopy.Add((rel, srcPath));
                }
            }

            // Copy sibling TRMTR variants (material sets) so the preview model can switch sets like the original.
            // Many models have `<base>_00.trmtr`, `<base>_01.trmtr`, etc that are not listed in TRMDL.Materials.
            if (trmtrFilesToCopy.Count > 0)
            {
                AddSiblingTrmtrVariants(referenceDir, trmtrFilesToCopy);
            }

            // Copy TRMMT metadata (variations) alongside the TRMTRs and also provide a preferred `<outBase>.trmmt`
            // so the preview model can find it without relying on the original folder layout.
            CopyMaterialMetadata(referenceDir, outputDir, outBase, referenceMaterialPaths);

            if (trmtrFilesToCopy.Count == 0 && trmtrsToWrite.Count == 0)
            {
                // Fallback: build a minimal TRMTR from glTF names.
                var trmtr = BuildTrinityMaterials(gltf, materialNames, texOutDir, texDirName);
                trmtrsToWrite.Add((outTrmtrName, trmtr));
                referenceMaterialPaths = new[] { outTrmtrName };
            }

            var trmdl = new TRMDL
            {
                Field_00 = referenceTrmdl.Field_00,
                Meshes = outMeshes,
                Skeleton = string.IsNullOrWhiteSpace(skeletonRelPath)
                    ? null
                    : (referenceTrmdl.Skeleton ?? new ModelSkeleton { PathName = skeletonRelPath }),
                Materials = referenceMaterialPaths,
                LODs = referenceTrmdl.LODs,
                Bounds = referenceTrmdl.Bounds ?? lod0Bounds,
                Field_06 = referenceTrmdl.Field_06
            };

            // Write outputs.
            foreach (var (rel, srcPath) in trmtrFilesToCopy)
            {
                var dst = Path.Combine(outputDir, rel);
                var dstDir = Path.GetDirectoryName(dst);
                if (!string.IsNullOrWhiteSpace(dstDir))
                {
                    Directory.CreateDirectory(dstDir);
                }
                File.Copy(srcPath, dst, overwrite: true);
            }
            foreach (var (rel, trmtr) in trmtrsToWrite)
            {
                var dst = Path.Combine(outputDir, rel);
                var dstDir = Path.GetDirectoryName(dst);
                if (!string.IsNullOrWhiteSpace(dstDir))
                {
                    Directory.CreateDirectory(dstDir);
                }
                File.WriteAllBytes(dst, FlatBufferConverter.SerializeFrom(trmtr));
            }
            File.WriteAllBytes(outputTrmdlPath, FlatBufferConverter.SerializeFrom(trmdl));

            // Copy skeleton if output differs from reference folder.
            if (!string.IsNullOrWhiteSpace(skeletonSrcPath) && File.Exists(skeletonSrcPath) && !string.IsNullOrWhiteSpace(skeletonRelPath))
            {
                var dst = Path.Combine(outputDir, skeletonRelPath);
                var dstDir = Path.GetDirectoryName(dst);
                if (!string.IsNullOrWhiteSpace(dstDir))
                {
                    Directory.CreateDirectory(dstDir);
                }
                if (!string.Equals(Path.GetFullPath(skeletonSrcPath), Path.GetFullPath(dst), StringComparison.OrdinalIgnoreCase))
                {
                    File.Copy(skeletonSrcPath, dst, overwrite: true);
                }
            }

            // For protag clothing models that rely on merging p0_base + local skeleton at runtime, ensure the base skeleton is
            // available in a self-contained location under the output folder so the preview loader can find it.
            // (The standard on-disk layout uses ../../model_pc_base/model/p0_base.trskl, which doesn't exist in temp preview folders.)
            if (exportModelPcBaseOnExport && referenceSkl != null)
            {
                TryCopyProtagBaseSkeleton(referenceTrmdlPath, referenceTrmdl, skeletonSrcPath, outputDir);
            }
        }
    }
}
