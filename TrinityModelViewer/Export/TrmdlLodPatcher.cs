using System;
using System.IO;
using Trinity.Core.Flatbuffers.TR.Model;
using Trinity.Core.Utils;

namespace TrinityModelViewer.Export
{
    internal static class TrmdlLodPatcher
    {
        public static bool ForceAllLodsToUseMesh0(string trmdlPath, out string error)
        {
            error = string.Empty;
            if (string.IsNullOrWhiteSpace(trmdlPath))
            {
                error = "Missing TRMDL path.";
                return false;
            }

            if (!File.Exists(trmdlPath))
            {
                error = "TRMDL not found.";
                return false;
            }

            TRMDL? trmdl;
            try
            {
                trmdl = FlatBufferConverter.DeserializeFrom<TRMDL>(trmdlPath);
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }

            if (trmdl?.Meshes == null || trmdl.Meshes.Length == 0)
            {
                error = "TRMDL has no meshes.";
                return false;
            }

            var lod0Path = trmdl.Meshes[0]?.PathName;
            if (string.IsNullOrWhiteSpace(lod0Path))
            {
                error = "TRMDL mesh 0 has no path.";
                return false;
            }

            bool changed = false;
            for (int i = 1; i < trmdl.Meshes.Length; i++)
            {
                var mesh = trmdl.Meshes[i];
                if (mesh == null)
                {
                    continue;
                }

                if (!string.Equals(mesh.PathName, lod0Path, StringComparison.OrdinalIgnoreCase))
                {
                    mesh.PathName = lod0Path;
                    changed = true;
                }
            }

            if (trmdl.LODs != null)
            {
                foreach (var lod in trmdl.LODs)
                {
                    if (lod?.Entries == null)
                    {
                        continue;
                    }

                    foreach (var entry in lod.Entries)
                    {
                        if (entry == null)
                        {
                            continue;
                        }

                        if (entry.Index != 0)
                        {
                            entry.Index = 0;
                            changed = true;
                        }
                    }
                }
            }

            if (!changed)
            {
                return true;
            }

            try
            {
                File.WriteAllBytes(trmdlPath, FlatBufferConverter.SerializeFrom(trmdl));
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }
    }
}
