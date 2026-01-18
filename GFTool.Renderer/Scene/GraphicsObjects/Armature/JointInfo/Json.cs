using GFTool.Renderer.Core;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Trinity.Core.Flatbuffers.TR.Model;

namespace GFTool.Renderer.Scene.GraphicsObjects
{
    public partial class Armature
    {
        private void ApplyJointInfoFromJson(string? sourcePath)
        {
            var parseResult = LoadJointInfoFromJson(sourcePath);
            if (parseResult == null || parseResult.JointInfos.Count == 0)
            {
                return;
            }

            // Merge and override mode.
            // TRSKL binary provides joint info on most models.
            // Optional JSON sidecar can override flags and matrices, and can be used when TRSKL lacks them.
            if (jointInfoToNode == null || jointInfoToNode.Length == 0)
            {
                jointInfoToNode = new int[parseResult.JointInfos.Count];
                for (int i = 0; i < jointInfoToNode.Length; i++)
                {
                    jointInfoToNode[i] = -1;
                }
            }
            else if (parseResult.JointInfos.Count > jointInfoToNode.Length)
            {
                int oldLen = jointInfoToNode.Length;
                Array.Resize(ref jointInfoToNode, parseResult.JointInfos.Count);
                for (int i = oldLen; i < jointInfoToNode.Length; i++)
                {
                    jointInfoToNode[i] = -1;
                }
            }

            // Map by node name: JSON node list ordering isn't guaranteed to match TRSKL nodes.
            if (parseResult.NodeNames.Length == 0 || parseResult.NodeJointInfoIds.Length == 0)
            {
                return;
            }

            var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            int count = Math.Min(parseResult.NodeNames.Length, parseResult.NodeJointInfoIds.Length);
            for (int i = 0; i < count; i++)
            {
                var name = parseResult.NodeNames[i];
                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                map[name] = parseResult.NodeJointInfoIds[i];
            }

            for (int i = 0; i < Bones.Count; i++)
            {
                var bone = Bones[i];
                if (!map.TryGetValue(bone.Name, out int jointId))
                {
                    continue;
                }

                if (jointId >= 0 && jointId < jointInfoToNode.Length)
                {
                    jointInfoToNode[jointId] = i;
                }

                ApplyJointInfoToBone(bone, parseResult, jointId);
            }
        }

        private void ApplyJointInfoToBone(Bone bone, JointInfoParseResult parseResult, int jointId)
        {
            if (jointId < 0 || jointId >= parseResult.JointInfos.Count)
            {
                return;
            }

            var joint = parseResult.JointInfos[jointId];
            bone.UseSegmentScaleCompensate = joint.SegmentScaleCompensate;
            if (joint.HasInverseBind)
            {
                bone.JointInverseBindWorld = joint.InverseBind;
                bone.JointInverseBindWorldAlt = joint.InverseBind;
                bone.HasJointInverseBind = true;
            }
            bone.Skinning = joint.InfluenceSkinning;
        }

        private void ResolveJointInverseBindConvention()
        {
            if (Bones.Count == 0 || Bones.All(b => !b.HasJointInverseBind))
            {
                return;
            }

            var bindWorld = new Matrix4[Bones.Count];
            var computed = new bool[Bones.Count];
            for (int i = 0; i < Bones.Count; i++)
            {
                bindWorld[i] = ComputeBindWorld(i, useTrsklInverseBind: false, bindWorld, computed);
            }

            int votesRow = 0;
            int votesCol = 0;
            double errRowSum = 0;
            double errColSum = 0;
            int samples = 0;
            List<(int index, float eRow, float eCol)>? worst = null;

            int sampleLimit = Math.Min(Bones.Count, 96);
            for (int i = 0; i < sampleLimit; i++)
            {
                var bone = Bones[i];
                if (!bone.HasJointInverseBind)
                {
                    continue;
                }

                // This renderer constructs matrices in a row-vector convention (v' = v * M),
                // then relies on GL's column-major upload to transpose for GLSL. In that convention
                // the bind world matrix should satisfy: bindWorld * invBind ≈ Identity.
                var checkRow = bindWorld[i] * bone.JointInverseBindWorld;
                var checkCol = bindWorld[i] * bone.JointInverseBindWorldAlt;
                float eRow = MatrixIdentityError(checkRow);
                float eCol = MatrixIdentityError(checkCol);
                errRowSum += eRow;
                errColSum += eCol;
                samples++;
                if (MessageHandler.Instance.DebugLogsEnabled)
                {
                    worst ??= new List<(int index, float eRow, float eCol)>();
                    worst.Add((i, eRow, eCol));
                }
                if (eCol < eRow)
                {
                    votesCol++;
                }
                else
                {
                    votesRow++;
                }
            }

            if (samples == 0)
            {
                return;
            }

            jointInverseBindUsesColumns = votesCol > votesRow;
            if (jointInverseBindUsesColumns)
            {
                for (int i = 0; i < Bones.Count; i++)
                {
                    if (Bones[i].HasJointInverseBind)
                    {
                        Bones[i].JointInverseBindWorld = Bones[i].JointInverseBindWorldAlt;
                    }
                }
            }

            float avgErrRow = (float)(errRowSum / samples);
            float avgErrCol = (float)(errColSum / samples);
            float bestErr = MathF.Min(avgErrRow, avgErrCol);
            // Threshold is intentionally generous; "good" rigs are typically ~0.0-0.2 and bad ones are >> 1.0.
            trsklInverseBindReliable = bestErr <= 0.5f;

            if (MessageHandler.Instance.DebugLogsEnabled)
            {
                MessageHandler.Instance.AddMessage(
                    MessageType.LOG,
                    $"[Bind] JointInvBindConvention={(jointInverseBindUsesColumns ? "Columns" : "Rows")} votes=(row={votesRow}, col={votesCol}) avgErr=(row={avgErrRow:0.###}, col={avgErrCol:0.###}) reliable={trsklInverseBindReliable} paletteOffset={skinningPaletteOffset} bones={Bones.Count} jointInfos={jointInfoToNode.Length}");

                if (!trsklInverseBindReliable && worst != null && worst.Count > 0)
                {
                    foreach (var (index, eRow, eCol) in worst
                        .OrderByDescending(x => MathF.Min(x.eRow, x.eCol))
                        .Take(8))
                    {
                        var bone = Bones[index];
                        MessageHandler.Instance.AddMessage(
                            MessageType.LOG,
                            $"[Bind] Worst bone='{bone.Name}' idx={index} errRow={eRow:0.###} errCol={eCol:0.###} parent={bone.ParentIndex} restT={bone.RestPosition} restS={bone.RestScale}");
                    }
                }
            }
        }

        private static float MatrixIdentityError(in Matrix4 m)
        {
            // L1 error vs identity: cheap and stable for deciding between conventions.
            float e = 0f;
            e += MathF.Abs(m.M11 - 1f) + MathF.Abs(m.M22 - 1f) + MathF.Abs(m.M33 - 1f) + MathF.Abs(m.M44 - 1f);
            e += MathF.Abs(m.M12) + MathF.Abs(m.M13) + MathF.Abs(m.M14);
            e += MathF.Abs(m.M21) + MathF.Abs(m.M23) + MathF.Abs(m.M24);
            e += MathF.Abs(m.M31) + MathF.Abs(m.M32) + MathF.Abs(m.M34);
            e += MathF.Abs(m.M41) + MathF.Abs(m.M42) + MathF.Abs(m.M43);
            return e;
        }

        private static JointInfoParseResult? LoadJointInfoFromJson(string? sourcePath)
        {
            var jsonPath = ResolveTrsklJsonPath(sourcePath);
            if (string.IsNullOrWhiteSpace(jsonPath) || !File.Exists(jsonPath))
            {
                return null;
            }

            try
            {
                var text = File.ReadAllText(jsonPath);
                text = Regex.Replace(text, "\\b([A-Za-z_][A-Za-z0-9_]*)\\b\\s*:", "\"$1\":");
                using var doc = JsonDocument.Parse(text, new JsonDocumentOptions
                {
                    AllowTrailingCommas = true,
                    CommentHandling = JsonCommentHandling.Skip
                });
                var root = doc.RootElement;

                var jointInfos = new List<JointInfoJson>();
                if (root.TryGetProperty("joint_info_list", out var jointList) &&
                    jointList.ValueKind == JsonValueKind.Array)
                {
                    foreach (var entry in jointList.EnumerateArray())
                    {
                        var info = new JointInfoJson
                        {
                            SegmentScaleCompensate = entry.TryGetProperty("segment_scale_compensate", out var ssc) && ssc.GetBoolean(),
                            InfluenceSkinning = !entry.TryGetProperty("influence_skinning", out var inf) || inf.GetBoolean()
                        };

                        if (entry.TryGetProperty("inverse_bind_pose_matrix", out var matrix))
                        {
                            if (TryParseAxisMatrix(matrix, out var inverse))
                            {
                                info.InverseBind = inverse;
                                info.HasInverseBind = true;
                            }
                        }

                        jointInfos.Add(info);
                    }
                }

                var nodeJointIds = Array.Empty<int>();
                var nodeNames = Array.Empty<string>();
                if (root.TryGetProperty("node_list", out var nodeList) &&
                    nodeList.ValueKind == JsonValueKind.Array)
                {
                    int count = nodeList.GetArrayLength();
                    nodeJointIds = new int[count];
                    nodeNames = new string[count];
                    int i = 0;
                    foreach (var node in nodeList.EnumerateArray())
                    {
                        nodeNames[i] = node.TryGetProperty("name", out var name) ? name.GetString() ?? string.Empty : string.Empty;
                        nodeJointIds[i] = node.TryGetProperty("joint_info_id", out var jointId) ? jointId.GetInt32() : -1;
                        i++;
                    }
                }

                return new JointInfoParseResult(jointInfos, nodeJointIds, nodeNames);
            }
            catch
            {
                if (MessageHandler.Instance.DebugLogsEnabled)
                {
                    MessageHandler.Instance.AddMessage(
                        MessageType.WARNING,
                        $"[JointInfo] Failed to parse json: {jsonPath}");
                }
                return null;
            }
        }

        private static bool TryParseAxisMatrix(JsonElement matrix, out Matrix4 result)
        {
            result = Matrix4.Identity;
            if (!matrix.TryGetProperty("axis_x", out var axisX) ||
                !matrix.TryGetProperty("axis_y", out var axisY) ||
                !matrix.TryGetProperty("axis_z", out var axisZ) ||
                !matrix.TryGetProperty("axis_w", out var axisW))
            {
                return false;
            }

            var x = ReadVector3(axisX);
            var y = ReadVector3(axisY);
            var z = ReadVector3(axisZ);
            var w = ReadVector3(axisW);
            result = CreateMatrixFromAxis(x, y, z, w);
            return true;
        }

        private static Vector3 ReadVector3(JsonElement element)
        {
            float x = element.TryGetProperty("x", out var vx) ? vx.GetSingle() : 0f;
            float y = element.TryGetProperty("y", out var vy) ? vy.GetSingle() : 0f;
            float z = element.TryGetProperty("z", out var vz) ? vz.GetSingle() : 0f;
            return new Vector3(x, y, z);
        }

        private static string? ResolveTrsklJsonPath(string? sourcePath)
        {
            if (string.IsNullOrWhiteSpace(sourcePath))
            {
                return null;
            }

            var dir = Path.GetDirectoryName(sourcePath) ?? string.Empty;
            var baseName = Path.GetFileNameWithoutExtension(sourcePath);
            var candidate = Path.Combine(dir, $"{baseName}.json");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            return null;
        }

        private sealed class JointInfoParseResult
        {
            public JointInfoParseResult(List<JointInfoJson> jointInfos, int[] nodeJointInfoIds, string[] nodeNames)
            {
                JointInfos = jointInfos;
                NodeJointInfoIds = nodeJointInfoIds;
                NodeNames = nodeNames;
            }

            public List<JointInfoJson> JointInfos { get; }
            public int[] NodeJointInfoIds { get; }
            public string[] NodeNames { get; }
        }

        private struct JointInfoJson
        {
            public bool SegmentScaleCompensate;
            public bool InfluenceSkinning;
            public bool HasInverseBind;
            public Matrix4 InverseBind;
        }
    }
}
