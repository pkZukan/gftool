using GFTool.Core.Utils;
using GFTool.Renderer.Core;
using GFTool.Renderer.Core.Graphics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using Trinity.Core.Flatbuffers.TR.Model;
using Trinity.Core.Utils;
using System.IO;
using System;
using Trinity.Core.Assets;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;


namespace GFTool.Renderer.Scene.GraphicsObjects
{
	    public partial class Model : RefObject
	    {
        private void ResolveRigidParentAttachments()
        {
            var effectiveArmature = GetEffectiveArmature();
            if (effectiveArmature == null || effectiveArmature.Bones.Count == 0 || SubmeshParentNodeNames.Count == 0)
            {
                rigidParentBoneIndexBySubmesh = null;
                return;
            }

            int submeshCount = Positions.Count;
            rigidParentBoneIndexBySubmesh = new int[submeshCount];
            Array.Fill(rigidParentBoneIndexBySubmesh, -1);

            // Resolve parent bone names for rigid (non-skinned) submeshes.
            for (int i = 0; i < submeshCount && i < SubmeshParentNodeNames.Count; i++)
            {
                if (HasSkinning.Count > i && HasSkinning[i])
                {
                    continue;
                }

                int boneIndex = -1;
                string? resolvedName = null;
                var candidates = EnumerateRigidAttachmentCandidates(i).ToArray();
                foreach (var candidate in candidates)
                {
                    if (string.IsNullOrWhiteSpace(candidate))
                    {
                        continue;
                    }

                    boneIndex = FindBoneIndexByName(effectiveArmature, candidate);
                    if (boneIndex >= 0)
                    {
                        resolvedName = candidate;
                        break;
                    }
                }

                if (boneIndex >= 0)
                {
                    rigidParentBoneIndexBySubmesh[i] = boneIndex;
                    if (MessageHandler.Instance.DebugLogsEnabled && !string.IsNullOrWhiteSpace(resolvedName))
                    {
                        MessageHandler.Instance.AddMessage(
                            MessageType.LOG,
                            $"[Skin] Rigid attach resolved model={Name} submesh={i} bone='{resolvedName}' index={boneIndex}");
                    }
                }
                else if (MessageHandler.Instance.DebugLogsEnabled)
                {
                    MessageHandler.Instance.AddMessage(
                        MessageType.LOG,
                        $"[Skin] Rigid attach unresolved model={Name} submesh={i} candidates='{string.Join(", ", candidates.Where(s => !string.IsNullOrWhiteSpace(s)))}'");
                }
            }

            if (MessageHandler.Instance.DebugLogsEnabled)
            {
                int attached = 0;
                for (int i = 0; i < rigidParentBoneIndexBySubmesh.Length; i++)
                {
                    if (rigidParentBoneIndexBySubmesh[i] >= 0)
                    {
                        attached++;
                    }
                }

                if (attached > 0)
                {
                    MessageHandler.Instance.AddMessage(MessageType.LOG, $"[Skin] Rigid attachments: model={Name} attachedSubmeshes={attached}");
                }
            }
        }

        private IEnumerable<string?> EnumerateRigidAttachmentCandidates(int submeshIndex)
        {
            if (submeshIndex < 0)
            {
                yield break;
            }

            if (submeshIndex < SubmeshParentNodeNames.Count)
            {
                foreach (var c in ExpandAttachmentNameCandidates(SubmeshParentNodeNames[submeshIndex]))
                {
                    yield return c;
                }
            }

            // Fallback: use the mesh name prefix from our stored submesh name ("<mesh>:<material>").
            if (submeshIndex < SubmeshNames.Count)
            {
                string meshName = SubmeshNames[submeshIndex];
                int colon = meshName.IndexOf(':');
                if (colon > 0)
                {
                    meshName = meshName.Substring(0, colon);
                }

                foreach (var c in ExpandAttachmentNameCandidates(meshName))
                {
                    yield return c;
                }
            }
        }

        private static IEnumerable<string?> ExpandAttachmentNameCandidates(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                yield break;
            }

            string current = name.Trim();
            yield return current;

            if (current.EndsWith("_shape", StringComparison.OrdinalIgnoreCase) && current.Length > 6)
            {
                current = current.Substring(0, current.Length - 6);
                yield return current;
            }

            for (int lod = 0; lod <= 3; lod++)
            {
                string suffix = $"_lod{lod}";
                if (current.EndsWith(suffix, StringComparison.OrdinalIgnoreCase) && current.Length > suffix.Length)
                {
                    yield return current.Substring(0, current.Length - suffix.Length);
                    break;
                }
            }
        }

        private static int FindBoneIndexByName(Armature armature, string name)
        {
            if (armature.Bones.Count == 0 || string.IsNullOrWhiteSpace(name))
            {
                return -1;
            }

            string trimmed = name.Trim();
            for (int i = 0; i < armature.Bones.Count; i++)
            {
                if (string.Equals(armature.Bones[i].Name, trimmed, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }

            // Fall back to normalized matching (mirrors Animation's normalization).
            string normalized = NormalizeBoneName(trimmed);
            if (!string.IsNullOrWhiteSpace(normalized))
            {
                for (int i = 0; i < armature.Bones.Count; i++)
                {
                    if (string.Equals(NormalizeBoneName(armature.Bones[i].Name), normalized, StringComparison.OrdinalIgnoreCase))
                    {
                        return i;
                    }
                }
            }

            return -1;
        }

        private static string NormalizeBoneName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return string.Empty;
            }

            name = name.Trim();
            int lastColon = name.LastIndexOf(':');
            if (lastColon >= 0 && lastColon < name.Length - 1)
            {
                name = name.Substring(lastColon + 1);
            }

            int lastPipe = name.LastIndexOf('|');
            if (lastPipe >= 0 && lastPipe < name.Length - 1)
            {
                name = name.Substring(lastPipe + 1);
            }

            int lastSlash = Math.Max(name.LastIndexOf('/'), name.LastIndexOf('\\'));
            if (lastSlash >= 0 && lastSlash < name.Length - 1)
            {
                name = name.Substring(lastSlash + 1);
            }

            return name.Trim();
        }

        private static string? FirstNonEmpty(params string?[] values)
        {
            for (int i = 0; i < values.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(values[i]))
                {
                    return values[i];
                }
            }

            return null;
        }
	    }
}
