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

            nodeToJointInfo = new int[Bones.Count];
            for (int i = 0; i < nodeToJointInfo.Length; i++)
            {
                nodeToJointInfo[i] = -1;
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
                nodeToJointInfo[i] = jointId;
                ApplyTrsklJointInfoToBone(Bones[i], skel.JointInfos[jointId]);
            }
        }

        private void ApplyTrsklJointInfoToBone(Bone bone, TRJointInfo joint)
        {
            bone.UseSegmentScaleCompensate = joint.SegmentScaleCompensate;
            bone.Skinning = joint.InfluenceSkinning;

            if (joint.InverseBindPoseMatrix != null)
            {
                // TRSKL inverse bind axis ordering differs between some pipelines; store both interpretations
                // and pick the one that best matches the rest pose at runtime.
                bone.JointInverseBindWorld = CreateMatrixFromAxisRows(
                    new Vector3(joint.InverseBindPoseMatrix.AxisX.X, joint.InverseBindPoseMatrix.AxisX.Y, joint.InverseBindPoseMatrix.AxisX.Z),
                    new Vector3(joint.InverseBindPoseMatrix.AxisY.X, joint.InverseBindPoseMatrix.AxisY.Y, joint.InverseBindPoseMatrix.AxisY.Z),
                    new Vector3(joint.InverseBindPoseMatrix.AxisZ.X, joint.InverseBindPoseMatrix.AxisZ.Y, joint.InverseBindPoseMatrix.AxisZ.Z),
                    new Vector3(joint.InverseBindPoseMatrix.AxisW.X, joint.InverseBindPoseMatrix.AxisW.Y, joint.InverseBindPoseMatrix.AxisW.Z));
                bone.JointInverseBindWorldAlt = CreateMatrixFromAxisColumns(
                    new Vector3(joint.InverseBindPoseMatrix.AxisX.X, joint.InverseBindPoseMatrix.AxisX.Y, joint.InverseBindPoseMatrix.AxisX.Z),
                    new Vector3(joint.InverseBindPoseMatrix.AxisY.X, joint.InverseBindPoseMatrix.AxisY.Y, joint.InverseBindPoseMatrix.AxisY.Z),
                    new Vector3(joint.InverseBindPoseMatrix.AxisZ.X, joint.InverseBindPoseMatrix.AxisZ.Y, joint.InverseBindPoseMatrix.AxisZ.Z),
                    new Vector3(joint.InverseBindPoseMatrix.AxisW.X, joint.InverseBindPoseMatrix.AxisW.Y, joint.InverseBindPoseMatrix.AxisW.Z));
                bone.HasJointInverseBind = true;
            }
        }

    }
}
