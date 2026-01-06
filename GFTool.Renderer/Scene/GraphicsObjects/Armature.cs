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
    public class Armature
    {
        public const int MaxSkinBones = 192;
        private Matrix4[]? activePoseWorld;

        public class Bone
        {
            public string Name { get; private set; }
            public Transform Transform { get; private set; } = new Transform();
            public Matrix4 InverseBindWorld { get; set; } = Matrix4.Identity;
            public Matrix4 JointInverseBindWorld { get; set; } = Matrix4.Identity;
            public bool HasJointInverseBind { get; set; }
            public Vector3 RestPosition { get; private set; }
            public Quaternion RestRotation { get; private set; }
            public Vector3 RestScale { get; private set; }
            public Matrix4 RestLocalMatrix { get; private set; }
            public Matrix4 RestInvParentMatrix { get; set; }
            public Vector3 RestEuler { get; private set; }
            public bool UseSegmentScaleCompensate { get; set; }

            public int ParentIndex { get; set; }
            public bool Skinning { get; set; }
            public Bone Parent;
            public List<Bone> Children;

            public Bone(TRTransformNode node, bool skinning)
            {
                Name = node.Name;
                Transform.Position = new Vector3(node.Transform.Translate.X, node.Transform.Translate.Y, node.Transform.Translate.Z);
                RestEuler = new Vector3(node.Transform.Rotate.X, node.Transform.Rotate.Y, node.Transform.Rotate.Z);
                Transform.Rotation = FromEulerXYZ(RestEuler);
                Transform.Scale = new Vector3(node.Transform.Scale.X, node.Transform.Scale.Y, node.Transform.Scale.Z);
                RestPosition = Transform.Position;
                RestRotation = Transform.Rotation;
                RestScale = Transform.Scale;
                RestLocalMatrix = Matrix4.CreateScale(RestScale)
                                 * Matrix4.CreateFromQuaternion(RestRotation)
                                 * Matrix4.CreateTranslation(RestPosition);
                RestInvParentMatrix = Matrix4.Identity;
                ParentIndex = node.ParentNodeIndex;
                Skinning = skinning;
                HasJointInverseBind = false;
                UseSegmentScaleCompensate = false;
                Parent = null;
                Children = new List<Bone>();
            }

            private static Quaternion FromEulerXYZ(Vector3 euler)
            {
                // Euler XYZ: X, then Y, then Z.
                // Quaternion multiply order ends up: q = qZ * qY * qX.
                var qx = Quaternion.FromAxisAngle(Vector3.UnitX, euler.X);
                var qy = Quaternion.FromAxisAngle(Vector3.UnitY, euler.Y);
                var qz = Quaternion.FromAxisAngle(Vector3.UnitZ, euler.Z);
                var q = qz * qy * qx;
                q.Normalize();
                return q;
            }

            public void AddChild(Bone bone)
            {
                bone.Parent = this;
                Children.Add(bone);
            }

            public void ResetPose()
            {
                Transform.Position = RestPosition;
                Transform.Rotation = RestRotation;
                Transform.Scale = RestScale;
            }
        }

        public List<Bone> Bones = new List<Bone>();
        public IReadOnlyList<int> ParentIndices => parentIndices;
        private readonly List<int> parentIndices = new List<int>();
        private int[] jointInfoToNode = Array.Empty<int>();
        public int JointInfoCount => jointInfoToNode.Length;
        public int BoneMetaCount => 0;
        private readonly int skinningPaletteOffset;

        public Armature(TRSKL skel, string? sourcePath = null)
        {
            skinningPaletteOffset = skel.SkinningPaletteOffset;

            foreach (var transNode in skel.TransformNodes)
            {
                // Skinning, SSC, and inverse bind data are populated from TRSKL joint info (or JSON override).
                var bone = new Bone(transNode, skinning: false);
                Bones.Add(bone);
                parentIndices.Add(transNode.ParentNodeIndex);
            }

            ApplyJointInfoFromTrskl(skel);
            ApplyJointInfoFromJson(sourcePath);

            for (int i = 0; i < Bones.Count; i++)
            {
                var parentIndex = Bones[i].ParentIndex;
                if (parentIndex >= 0 && parentIndex < Bones.Count && parentIndex != i)
                {
                    Bones[parentIndex].AddChild(Bones[i]);
                }
            }

            UpdateRestParentMatrices();
            ComputeInverseBindMatrices(RenderOptions.UseTrsklInverseBind);
        }

        private void ApplyJointInfoFromTrskl(TRSKL skel)
        {
            if (skel.JointInfos == null || skel.JointInfos.Length == 0 || skel.TransformNodes == null || skel.TransformNodes.Length == 0)
            {
                return;
            }

            jointInfoToNode = new int[skel.JointInfos.Length];
            for (int i = 0; i < jointInfoToNode.Length; i++)
            {
                jointInfoToNode[i] = -1;
            }

            int count = Math.Min(Bones.Count, skel.TransformNodes.Length);
            for (int i = 0; i < count; i++)
            {
                var node = skel.TransformNodes[i];
                int jointId = node.JointInfoIndex;
                if (jointId < 0 || jointId >= skel.JointInfos.Length)
                {
                    continue;
                }

                jointInfoToNode[jointId] = i;
                ApplyTrsklJointInfoToBone(Bones[i], skel.JointInfos[jointId]);
            }
        }

        private void ApplyTrsklJointInfoToBone(Bone bone, TRJointInfo joint)
        {
            bone.UseSegmentScaleCompensate = joint.SegmentScaleCompensate;
            bone.Skinning = joint.InfluenceSkinning;

            if (joint.InverseBindPoseMatrix != null)
            {
                bone.JointInverseBindWorld = CreateMatrixFromAxis(
                    new Vector3(joint.InverseBindPoseMatrix.AxisX.X, joint.InverseBindPoseMatrix.AxisX.Y, joint.InverseBindPoseMatrix.AxisX.Z),
                    new Vector3(joint.InverseBindPoseMatrix.AxisY.X, joint.InverseBindPoseMatrix.AxisY.Y, joint.InverseBindPoseMatrix.AxisY.Z),
                    new Vector3(joint.InverseBindPoseMatrix.AxisZ.X, joint.InverseBindPoseMatrix.AxisZ.Y, joint.InverseBindPoseMatrix.AxisZ.Z),
                    new Vector3(joint.InverseBindPoseMatrix.AxisW.X, joint.InverseBindPoseMatrix.AxisW.Y, joint.InverseBindPoseMatrix.AxisW.Z));
                bone.HasJointInverseBind = true;
            }
        }

        private void UpdateRestParentMatrices()
        {
            for (int i = 0; i < Bones.Count; i++)
            {
                var bone = Bones[i];
                if (bone.ParentIndex >= 0 && bone.ParentIndex < Bones.Count && bone.ParentIndex != i)
                {
                    bone.RestInvParentMatrix = Matrix4.Invert(Bones[bone.ParentIndex].RestLocalMatrix);
                }
                else
                {
                    bone.RestInvParentMatrix = Matrix4.Identity;
                }
            }
        }

        public Vector3[] GetWorldPositions()
        {
            var worldMatrices = GetWorldMatrices();
            var positions = new Vector3[worldMatrices.Length];
            for (int i = 0; i < worldMatrices.Length; i++)
            {
                positions[i] = worldMatrices[i].ExtractTranslation();
            }
            return positions;
        }

        public Matrix4[] GetWorldMatrices()
        {
            if (activePoseWorld != null && activePoseWorld.Length == Bones.Count)
            {
                return (Matrix4[])activePoseWorld.Clone();
            }

            var world = new Matrix4[Bones.Count];
            var computed = new bool[Bones.Count];
            for (int i = 0; i < Bones.Count; i++)
            {
                world[i] = ComputeWorldMatrix(i, computed, world);
            }
            return world;
        }

        public Matrix4[] GetSkinMatrices(int maxBones)
        {
            int boneCount = Math.Min(Bones.Count, maxBones);
            var world = GetSkinWorldMatrices();
            var result = new Matrix4[Math.Max(maxBones, boneCount)];
            for (int i = 0; i < boneCount; i++)
            {
                // Row vector math: v' = v * invBind * poseWorld. Matrices are transposed on upload for GLSL.
                var bone = Bones[i];
                var invBind = bone.HasJointInverseBind ? bone.JointInverseBindWorld : bone.InverseBindWorld;
                result[i] = invBind * world[i];
            }
            for (int i = boneCount; i < result.Length; i++)
            {
                result[i] = Matrix4.Identity;
            }
            return result;
        }

        public Matrix4[] GetSkinMatricesForPalette(int[] palette, int maxBones, out int boneCount)
        {
            boneCount = 0;
            if (palette == null || palette.Length == 0)
            {
                return new Matrix4[maxBones];
            }

            var world = GetSkinWorldMatrices();
            boneCount = Math.Min(palette.Length, maxBones);
            var result = new Matrix4[Math.Max(maxBones, boneCount)];
            for (int i = 0; i < boneCount; i++)
            {
                int boneIndex = palette[i];
                if (boneIndex < 0 || boneIndex >= Bones.Count)
                {
                    result[i] = Matrix4.Identity;
                }
                else
                {
                    var bone = Bones[boneIndex];
                    var invBind = bone.HasJointInverseBind ? bone.JointInverseBindWorld : bone.InverseBindWorld;
                    result[i] = invBind * world[boneIndex];
                }
            }
            for (int i = boneCount; i < result.Length; i++)
            {
                result[i] = Matrix4.Identity;
            }
            return result;
        }

        public Matrix4[] GetSkinMatricesForJointInfo(int maxBones, out int boneCount)
        {
            boneCount = 0;
            if (jointInfoToNode == null || jointInfoToNode.Length == 0)
            {
                return new Matrix4[maxBones];
            }

            var world = GetSkinWorldMatrices();
            boneCount = Math.Min(jointInfoToNode.Length, maxBones);
            var result = new Matrix4[Math.Max(maxBones, boneCount)];
            for (int i = 0; i < boneCount; i++)
            {
                int boneIndex = jointInfoToNode[i];
                if (boneIndex < 0 || boneIndex >= Bones.Count)
                {
                    result[i] = Matrix4.Identity;
                    continue;
                }

                var bone = Bones[boneIndex];
                var invBind = bone.HasJointInverseBind ? bone.JointInverseBindWorld : bone.InverseBindWorld;
                result[i] = invBind * world[boneIndex];
            }
            for (int i = boneCount; i < result.Length; i++)
            {
                result[i] = Matrix4.Identity;
            }
            return result;
        }

        private Matrix4[] GetSkinWorldMatrices()
        {
            if (activePoseWorld != null && activePoseWorld.Length == Bones.Count)
            {
                // When an animation is active, skin directly from the computed pose world matrices to
                // avoid TRS decomposition and recomposition artifacts (especially with SSC and nonuniform scale).
                return activePoseWorld;
            }

            var world = new Matrix4[Bones.Count];
            var computed = new bool[Bones.Count];
            for (int i = 0; i < Bones.Count; i++)
            {
                world[i] = ComputeSkinWorldMatrix(i, computed, world);
            }
            return world;
        }

        private Matrix4 ComputeSkinWorldMatrix(int index, bool[] computed, Matrix4[] world)
        {
            if (computed[index])
            {
                return world[index];
            }

            var bone = Bones[index];
            var local = Matrix4.CreateScale(bone.Transform.Scale) *
                        Matrix4.CreateFromQuaternion(bone.Transform.Rotation) *
                        Matrix4.CreateTranslation(bone.Transform.Position);

            if (bone.ParentIndex >= 0 && bone.ParentIndex < Bones.Count && bone.ParentIndex != index)
            {
                if (bone.UseSegmentScaleCompensate)
                {
                    var parent = Bones[bone.ParentIndex];
                    local *= Matrix4.CreateScale(
                        parent.Transform.Scale.X != 0f ? 1f / parent.Transform.Scale.X : 1f,
                        parent.Transform.Scale.Y != 0f ? 1f / parent.Transform.Scale.Y : 1f,
                        parent.Transform.Scale.Z != 0f ? 1f / parent.Transform.Scale.Z : 1f);
                }
                var parentWorld = ComputeSkinWorldMatrix(bone.ParentIndex, computed, world);
                world[index] = local * parentWorld;
            }
            else
            {
                world[index] = local;
            }

            computed[index] = true;
            return world[index];
        }

        private Matrix4 ComputeWorldMatrix(int index, bool[] computed, Matrix4[] world)
        {
            if (computed[index])
            {
                return world[index];
            }

            var bone = Bones[index];
            var local = Matrix4.CreateScale(bone.Transform.Scale) *
                        Matrix4.CreateFromQuaternion(bone.Transform.Rotation) *
                        Matrix4.CreateTranslation(bone.Transform.Position);

            if (bone.ParentIndex >= 0 && bone.ParentIndex < Bones.Count && bone.ParentIndex != index)
            {
                if (bone.UseSegmentScaleCompensate)
                {
                    var parent = Bones[bone.ParentIndex];
                    local *= Matrix4.CreateScale(
                        parent.Transform.Scale.X != 0f ? 1f / parent.Transform.Scale.X : 1f,
                        parent.Transform.Scale.Y != 0f ? 1f / parent.Transform.Scale.Y : 1f,
                        parent.Transform.Scale.Z != 0f ? 1f / parent.Transform.Scale.Z : 1f);
                }
                var parentWorld = ComputeWorldMatrix(bone.ParentIndex, computed, world);
                world[index] = local * parentWorld;
            }
            else
            {
                world[index] = local;
            }

            computed[index] = true;
            return world[index];
        }

        public bool IsVisibleBone(int index)
        {
            var bone = Bones[index];
            return bone.Skinning || bone.Children.Count > 0;
        }

        public void ResetPose()
        {
            foreach (var bone in Bones)
            {
                bone.ResetPose();
            }
            activePoseWorld = null;
        }

        public void ApplyAnimation(Animation animation, float frame)
        {
            if (animation == null)
            {
                return;
            }

            var poseWorld = new Matrix4[Bones.Count];
            var computed = new bool[Bones.Count];

            for (int i = 0; i < Bones.Count; i++)
            {
                poseWorld[i] = ComputePoseWorld(i, animation, frame, poseWorld, computed);
            }

            // Keep the exact pose world matrices for skinning (avoids TRS extract/rebuild artifacts).
            activePoseWorld = poseWorld;

            for (int i = 0; i < Bones.Count; i++)
            {
                var bone = Bones[i];
                Matrix4 local = poseWorld[i];
                if (bone.ParentIndex >= 0 && bone.ParentIndex < Bones.Count && bone.ParentIndex != i)
                {
                    var parentWorld = poseWorld[bone.ParentIndex];
                    local = poseWorld[i] * Matrix4.Invert(parentWorld);
                }

                bone.Transform.Position = local.ExtractTranslation();
                bone.Transform.Rotation = local.ExtractRotation();
                bone.Transform.Scale = local.ExtractScale();
            }
        }

        private Matrix4 ComputePoseWorld(int index, Animation animation, float frame, Matrix4[] poseWorld, bool[] computed)
        {
            if (computed[index])
            {
                return poseWorld[index];
            }

            var bone = Bones[index];
            // Evaluate animation tracks relative to the skeleton rest local pose.
            // Bind matrices (joint and meta) are for skinning. Using them as an animation base pose
            // can cause visible flips and offsets when toggling bind matrix modes.
            Matrix4 restLocal = bone.RestLocalMatrix;
            var baseLoc = restLocal.ExtractTranslation();
            var baseRot = restLocal.ExtractRotation();
            var baseScale = restLocal.ExtractScale();

            Vector3? scale;
            Quaternion? rotation;
            Vector3? translation;
            if (!animation.TryGetPose(bone.Name, frame, out scale, out rotation, out translation))
            {
                scale = null;
                rotation = null;
                translation = null;
            }

            var loc = translation ?? baseLoc;
            var rot = rotation ?? baseRot;
            // Animation scale tracks are local scale values; applying them in world space introduces shearing/stretching.
            var localScale = scale ?? baseScale;

            var matrix = Matrix4.CreateScale(localScale)
                        * Matrix4.CreateFromQuaternion(rot)
                        * Matrix4.CreateTranslation(loc);

            Matrix4 world = matrix;
            if (bone.ParentIndex >= 0 && bone.ParentIndex < Bones.Count && bone.ParentIndex != index)
            {
                var parentWorld = ComputePoseWorld(bone.ParentIndex, animation, frame, poseWorld, computed);
                if (bone.UseSegmentScaleCompensate)
                {
                    var parent = Bones[bone.ParentIndex];
                    Matrix4 parentLocal = parentWorld;
                    if (parent.ParentIndex >= 0 && parent.ParentIndex < Bones.Count && parent.ParentIndex != bone.ParentIndex)
                    {
                        var grandParentWorld = ComputePoseWorld(parent.ParentIndex, animation, frame, poseWorld, computed);
                        parentLocal = parentWorld * Matrix4.Invert(grandParentWorld);
                    }

                    var parentScale = parentLocal.ExtractScale();
                    matrix *= Matrix4.CreateScale(
                        parentScale.X != 0f ? 1f / parentScale.X : 1f,
                        parentScale.Y != 0f ? 1f / parentScale.Y : 1f,
                        parentScale.Z != 0f ? 1f / parentScale.Z : 1f);
                }
                world = matrix * parentWorld;
            }

            computed[index] = true;
            poseWorld[index] = world;
            return world;
        }

        public int GetVisibleParentIndex(int index)
        {
            var parent = Bones[index].ParentIndex;
            while (parent >= 0 && parent < Bones.Count && !IsVisibleBone(parent))
            {
                parent = Bones[parent].ParentIndex;
            }
            return parent;
        }

        public int MapJointInfoIndex(int jointInfoIndex)
        {
            if (jointInfoIndex < 0 || jointInfoIndex >= jointInfoToNode.Length)
            {
                return 0;
            }

            int mapped = jointInfoToNode[jointInfoIndex];
            return mapped >= 0 ? mapped : 0;
        }

        public int MapBoneMetaIndex(int boneMetaIndex)
        {
            // BoneMeta mapping is not present in the TRSKL schema used by this viewer.
            return 0;
        }

        public int[] BuildSkinningPalette()
        {
            if (jointInfoToNode == null || jointInfoToNode.Length == 0)
            {
                return Array.Empty<int>();
            }

            // Joint info index space is treated as the palette. palette[jointId] maps to a node index.
            // Dual skeleton cases use `skinning_palette_offset`, but base and local are not merged here.
            var palette = new int[jointInfoToNode.Length];
            for (int i = 0; i < palette.Length; i++)
            {
                int nodeIndex = jointInfoToNode[i];
                palette[i] = nodeIndex >= 0 ? nodeIndex : 0;
            }
            return palette;
        }

        private void ComputeInverseBindMatrices(bool useTrsklInverseBind)
        {
            if (Bones.Count == 0)
            {
                return;
            }

            var bindWorld = new Matrix4[Bones.Count];
            var computed = new bool[Bones.Count];
            for (int i = 0; i < Bones.Count; i++)
            {
                bindWorld[i] = ComputeBindWorld(i, useTrsklInverseBind, bindWorld, computed);
            }

            for (int i = 0; i < Bones.Count; i++)
            {
                if (useTrsklInverseBind && Bones[i].HasJointInverseBind)
                {
                    Bones[i].InverseBindWorld = Bones[i].JointInverseBindWorld;
                }
                else
                {
                    Bones[i].InverseBindWorld = Matrix4.Invert(bindWorld[i]);
                }
            }

        }

        private Matrix4 ComputeBindWorld(int index, bool useTrsklInverseBind, Matrix4[] world, bool[] computed)
        {
            if (computed[index])
            {
                return world[index];
            }

            var bone = Bones[index];
            Matrix4 local;
            if (useTrsklInverseBind && bone.HasJointInverseBind)
            {
                local = Matrix4.Invert(bone.JointInverseBindWorld);
            }
            else
            {
                local = bone.RestLocalMatrix;
            }

            if (bone.ParentIndex >= 0 && bone.ParentIndex < Bones.Count && bone.ParentIndex != index)
            {
                if (bone.UseSegmentScaleCompensate)
                {
                    var parent = Bones[bone.ParentIndex];
                    local *= Matrix4.CreateScale(
                        parent.RestScale.X != 0f ? 1f / parent.RestScale.X : 1f,
                        parent.RestScale.Y != 0f ? 1f / parent.RestScale.Y : 1f,
                        parent.RestScale.Z != 0f ? 1f / parent.RestScale.Z : 1f);
                }
                var parentWorld = ComputeBindWorld(bone.ParentIndex, useTrsklInverseBind, world, computed);
                world[index] = local * parentWorld;
            }
            else
            {
                world[index] = local;
            }

            computed[index] = true;
            return world[index];
        }

        private static Matrix4 CreateMatrixFromAxis(Vector3 axisX, Vector3 axisY, Vector3 axisZ, Vector3 axisW)
        {
            // TRSKL JSON axis x/y/z/w are rows; translation lives in axis w.
            return new Matrix4(
                axisX.X, axisX.Y, axisX.Z, 0f,
                axisY.X, axisY.Y, axisY.Z, 0f,
                axisZ.X, axisZ.Y, axisZ.Z, 0f,
                axisW.X, axisW.Y, axisW.Z, 1f);
        }

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
                bone.HasJointInverseBind = true;
            }
            bone.Skinning = joint.InfluenceSkinning;
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
            var candidate = Path.Combine(dir, $"{baseName}.trskl.json");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            candidate = Path.Combine(dir, $"{baseName}.json");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            var workingDir = FindWorkingDir();
            if (!string.IsNullOrWhiteSpace(workingDir))
            {
                candidate = Path.Combine(workingDir, "ExampleFiles", $"{baseName}.trskl.json");
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }

            return null;
        }

        private static string? FindWorkingDir()
        {
            var dir = AppContext.BaseDirectory;
            for (int i = 0; i < 6; i++)
            {
                var candidate = Path.Combine(dir, "WorkingDir");
                if (Directory.Exists(candidate))
                {
                    return candidate;
                }

                var parent = Directory.GetParent(dir);
                if (parent == null)
                {
                    break;
                }
                dir = parent.FullName;
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
