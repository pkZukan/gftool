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
        public const int MaxSkinBones = 192;
        private Matrix4[]? activePoseWorld;
        private Matrix4[]? poseWorldCache;
        private Matrix4[]? poseWorldNoRotCache;
        private Matrix4[]? poseLocalCache;
        private bool[]? poseComputedCache;
        private long lastAllocPoseComputeBytes;
        private long lastAllocWriteBackBytes;
        private Matrix4[]? skinMatricesCache;
        private Matrix4[]? jointInfoSkinMatricesCache;
        private Matrix4[]? paletteSkinMatricesCache;
        private bool jointInverseBindUsesColumns;
        private bool trsklInverseBindReliable = true;
        private string? lastSingularInvertAnimName;
        private readonly HashSet<int> warnedSingularInvertBones = new HashSet<int>();

        public class Bone
        {
            public string Name { get; private set; }
            public Transform Transform { get; private set; } = new Transform();
            public Matrix4 InverseBindWorld { get; set; } = Matrix4.Identity;
            public Matrix4 JointInverseBindWorld { get; set; } = Matrix4.Identity;
            public Matrix4 JointInverseBindWorldAlt { get; set; } = Matrix4.Identity;
            public bool HasJointInverseBind { get; set; }
            public Vector3 ScalePivot { get; private set; }
            public Vector3 RotatePivot { get; private set; }
            public bool IgnoreParentRotation { get; private set; }
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
                ScalePivot = new Vector3(node.ScalePivot.X, node.ScalePivot.Y, node.ScalePivot.Z);
                RotatePivot = new Vector3(node.RotatePivot.X, node.RotatePivot.Y, node.RotatePivot.Z);
                IgnoreParentRotation = node.IgnoreParentRotation;
                RestPosition = Transform.Position;
                RestRotation = Transform.Rotation;
                RestScale = Transform.Scale;
                RestLocalMatrix = BuildLocalMatrix(RestScale, RestRotation, RestPosition, ScalePivot, RotatePivot);
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

        private static Matrix4 BuildLocalMatrix(Vector3 scale, Quaternion rotation, Vector3 translation, Vector3 scalePivot, Vector3 rotatePivot)
        {
            // Row-vector convention: v' = v * M. Multiply matrices in application order.
            // TRS without pivots is: Scale * Rotation * Translation.
            Matrix4 m = Matrix4.Identity;

            var scaleMatrix = Matrix4.CreateScale(scale);
            if (scalePivot != Vector3.Zero)
            {
                m *= Matrix4.CreateTranslation(-scalePivot) * scaleMatrix * Matrix4.CreateTranslation(scalePivot);
            }
            else
            {
                m *= scaleMatrix;
            }

            var rotationMatrix = Matrix4.CreateFromQuaternion(rotation);
            if (rotatePivot != Vector3.Zero)
            {
                m *= Matrix4.CreateTranslation(-rotatePivot) * rotationMatrix * Matrix4.CreateTranslation(rotatePivot);
            }
            else
            {
                m *= rotationMatrix;
            }

            m *= Matrix4.CreateTranslation(translation);
            return m;
        }

        public List<Bone> Bones = new List<Bone>();
        public IReadOnlyList<int> ParentIndices => parentIndices;
        private readonly List<int> parentIndices = new List<int>();
        private int[] jointInfoToNode = Array.Empty<int>();
        private int[] nodeToJointInfo = Array.Empty<int>();
        public int JointInfoCount => jointInfoToNode.Length;
        public int BoneMetaCount => 0;
        private readonly int skinningPaletteOffset;
        private readonly int[]? skinningPaletteOverride;

        public Armature(TRSKL skel, string? sourcePath = null, int[]? skinningPaletteOverride = null)
        {
            skinningPaletteOffset = skel.SkinningPaletteOffset;
            this.skinningPaletteOverride = skinningPaletteOverride;

            foreach (var transNode in skel.TransformNodes)
            {
                // Skinning, SSC, and inverse bind data are populated from TRSKL joint info (or JSON override).
                var bone = new Bone(transNode, skinning: false);
                Bones.Add(bone);
                parentIndices.Add(transNode.ParentNodeIndex);
            }

            ApplyJointInfoFromTrskl(skel);
            ApplyJointInfoFromJson(sourcePath);

            if (MessageHandler.Instance.DebugLogsEnabled)
            {
                int pivotCount = 0;
                int ignoreRotCount = 0;
                var notable = new List<string>();
                foreach (var bone in Bones)
                {
                    bool hasPivot = bone.ScalePivot != Vector3.Zero || bone.RotatePivot != Vector3.Zero;
                    if (hasPivot)
                    {
                        pivotCount++;
                    }
                    if (bone.IgnoreParentRotation)
                    {
                        ignoreRotCount++;
                    }
                    if ((hasPivot || bone.IgnoreParentRotation) && notable.Count < 10)
                    {
                        notable.Add($"{bone.Name}(pivot={hasPivot}, ignoreParentRot={bone.IgnoreParentRotation})");
                    }
                }

                if (pivotCount > 0 || ignoreRotCount > 0)
                {
                    MessageHandler.Instance.AddMessage(
                        MessageType.LOG,
                        $"[TRSKL] Pivots={pivotCount} IgnoreParentRotation={ignoreRotCount}" +
                        (notable.Count > 0 ? $" examples={string.Join(", ", notable)}" : string.Empty));
                }
            }

            for (int i = 0; i < Bones.Count; i++)
            {
                var parentIndex = Bones[i].ParentIndex;
                if (parentIndex >= 0 && parentIndex < Bones.Count && parentIndex != i)
                {
                    Bones[parentIndex].AddChild(Bones[i]);
                }
            }

            UpdateRestParentMatrices();
            ResolveJointInverseBindConvention();
            ComputeInverseBindMatrices(RenderOptions.UseTrsklInverseBind);
        }

    }
}
