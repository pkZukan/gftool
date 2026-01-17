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
        public override void Setup()
        {
            BeginGpuSetup();
            while (!StepGpuSetup())
            {
                // Intentionally empty; this preserves the original synchronous behavior.
            }
        }

        public override void Draw(Matrix4 view, Matrix4 proj)
        {
            if (!IsVisible)
            {
                return;
            }
            if (!IsGpuSetupComplete)
            {
                return;
            }

            PerfCounters.RecordModelDraw();

            Matrix4[]? nodeSkinMatrices = null;
            Matrix4[]? jointInfoSkinMatrices = null;
            Matrix4[]? paletteSkinMatrices = null;
            var skinArmature = GetEffectiveArmature();
            bool canSkin = skinArmature != null && skinArmature.Bones.Count > 0;
            int nodeBoneCount = 0;
            int jointInfoBoneCount = 0;
            int paletteBoneCount = 0;

            bool AnySubmeshNeedsNodeSkinMatrices()
            {
                if (blendIndexRemapModes == null)
                {
                    return false;
                }

                for (int i = 0; i < blendIndexRemapModes.Length; i++)
                {
                    switch (blendIndexRemapModes[i])
                    {
                        case BlendIndexRemapMode.JointInfo:
                        case BlendIndexRemapMode.SkinningPalette:
                        case BlendIndexRemapMode.BoneMeta:
                            return true;
                    }
                }

                return false;
            }

            // Rigid attachments are resolved in node index space, but the active skin matrix array may be
            // joint-info or palette indexed. Always compute node-indexed skin matrices for rigid attachments.
            bool needsNodeSkinMatrices = canSkin && (rigidParentBoneIndexBySubmesh != null || AnySubmeshNeedsNodeSkinMatrices());
            if (needsNodeSkinMatrices)
            {
                nodeBoneCount = Math.Min(skinArmature!.Bones.Count, Armature.MaxSkinBones);
                nodeSkinMatrices = skinArmature.GetSkinMatrices(Armature.MaxSkinBones);
            }

            if (canSkin && RenderOptions.UseJointInfoMatrices)
            {
                jointInfoSkinMatrices = skinArmature!.GetSkinMatricesForJointInfo(Armature.MaxSkinBones, out jointInfoBoneCount);
            }

            if (canSkin && RenderOptions.UseSkinningPaletteMatrices)
            {
                var palette = skinArmature!.BuildSkinningPalette();
                paletteSkinMatrices = skinArmature.GetSkinMatricesForPalette(palette, Armature.MaxSkinBones, out paletteBoneCount);
            }

            void ResolveSkinningForSubmesh(int submeshIndex, out Matrix4[]? matrices, out int boneCount)
            {
                matrices = null;
                boneCount = 0;
                if (!canSkin || !HasSkinning[submeshIndex])
                {
                    return;
                }

                var mode = blendIndexRemapModes != null && submeshIndex < blendIndexRemapModes.Length
                    ? blendIndexRemapModes[submeshIndex]
                    : BlendIndexRemapMode.None;

                // If we remapped indices into node space, we must also skin from node-indexed matrices.
                if (mode == BlendIndexRemapMode.JointInfo ||
                    mode == BlendIndexRemapMode.SkinningPalette ||
                    mode == BlendIndexRemapMode.BoneMeta)
                {
                    matrices = nodeSkinMatrices ?? skinArmature!.GetSkinMatrices(Armature.MaxSkinBones);
                    boneCount = nodeBoneCount != 0 ? nodeBoneCount : Math.Min(skinArmature!.Bones.Count, Armature.MaxSkinBones);
                    return;
                }

                if (RenderOptions.UseJointInfoMatrices && jointInfoSkinMatrices != null)
                {
                    matrices = jointInfoSkinMatrices;
                    boneCount = jointInfoBoneCount;
                    return;
                }

                if (RenderOptions.UseSkinningPaletteMatrices && paletteSkinMatrices != null)
                {
                    matrices = paletteSkinMatrices;
                    boneCount = paletteBoneCount;
                    return;
                }

                matrices = nodeSkinMatrices ?? skinArmature!.GetSkinMatrices(Armature.MaxSkinBones);
                boneCount = nodeBoneCount != 0 ? nodeBoneCount : Math.Min(skinArmature!.Bones.Count, Armature.MaxSkinBones);
            }

            for (int i = 0; i < VAOs.Length; i++)
            {
                if (submeshVisible != null && i < submeshVisible.Length && !submeshVisible[i])
                {
                    continue;
                }

                PerfCounters.RecordSubmeshDraw();

                if (RenderOptions.OutlinePass)
                {
                    if (i == selectedSubmeshIndex)
                    {
                        ResolveSkinningForSubmesh(i, out var outlineSkinMatrices, out var outlineBoneCount);
                        GL.BindVertexArray(VAOs[i]);
                        DrawOutline(view, proj, Indices[i].Length, canSkin && HasSkinning[i], outlineBoneCount, outlineSkinMatrices ?? Array.Empty<Matrix4>());
                        GL.BindVertexArray(0);
                    }
                    continue;
                }
                //Bind appropriate mat
                if (materials != null && materials.Length > 0)
                {
                    if (!materialMap.TryGetValue(MaterialNames[i], out var mat))
                    {
                        mat = materials[0];
                    }
                    bool drawOpaque = !RenderOptions.TransparentPass && !mat.IsTransparent;
                    bool drawTransparent = RenderOptions.TransparentPass && mat.IsTransparent;
                    if (drawOpaque || drawTransparent)
                    {
                        Matrix4 submeshModelMat = modelMat;
                        if (!HasSkinning[i] &&
                            rigidParentBoneIndexBySubmesh != null &&
                            i < rigidParentBoneIndexBySubmesh.Length)
                        {
                            int boneIndex = rigidParentBoneIndexBySubmesh[i];
                            if (boneIndex >= 0 && nodeSkinMatrices != null && boneIndex < nodeSkinMatrices.Length)
                            {
                                submeshModelMat = modelMat * nodeSkinMatrices[boneIndex];
                            }
                        }

                        var (layerMaskOverride, aoOverride) = GetUvOverrides(i);
                        mat.Use(view, submeshModelMat, proj, HasVertexColors[i], HasTangents[i], HasBinormals[i], i < HasUv1.Count && HasUv1[i], layerMaskOverride, aoOverride);
                        ResolveSkinningForSubmesh(i, out var submeshSkinMatrices, out var submeshBoneCount);
                        mat.ApplySkinning(canSkin && HasSkinning[i], submeshBoneCount, submeshSkinMatrices ?? Array.Empty<Matrix4>());
                    }
                    else if (!RenderOptions.TransparentPass)
                    {
                        continue;
                    }
                }

                // Draw the geometry
                GL.BindVertexArray(VAOs[i]);
                if (!RenderOptions.TransparentPass)
                {
                    PerfCounters.RecordDrawCall(Indices[i].Length);
                    GL.DrawElements(PrimitiveType.Triangles, Indices[i].Length, DrawElementsType.UnsignedInt, 0);
                }
                else if (materials != null && materials.Length > 0 && materialMap.TryGetValue(MaterialNames[i], out var mat) && mat.IsTransparent)
                {
                    PerfCounters.RecordDrawCall(Indices[i].Length);
                    GL.DrawElements(PrimitiveType.Triangles, Indices[i].Length, DrawElementsType.UnsignedInt, 0);
                }

                GL.BindVertexArray(0);
            }
        }
	    }
}
