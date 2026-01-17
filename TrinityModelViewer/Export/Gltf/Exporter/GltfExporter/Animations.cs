using GFTool.Renderer.Scene.GraphicsObjects;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TrinityModelViewer.Export
{
    internal static partial class GltfExporter
    {
        private static void AddAnimations(
            GltfRoot gltf,
            BinaryBufferBuilder buffer,
            Armature armature,
            int[] boneNodeIndices,
            IReadOnlyList<GFTool.Renderer.Scene.GraphicsObjects.Animation> animations)
        {
            int boneCount = armature.Bones.Count;
            int nodeCount = Math.Min(boneCount, boneNodeIndices.Length);

            foreach (var animation in animations)
            {
                if (animation == null) continue;
                int frameCount = (int)animation.FrameCount;
                if (frameCount <= 0) continue;
                float fps = animation.FrameRate > 0 ? animation.FrameRate : 30f;

                var times = new float[frameCount];
                for (int f = 0; f < frameCount; f++)
                {
                    times[f] = f / fps;
                }
                int timeAcc = AddAccessorScalarFloat(gltf, buffer, times);

                var gltfAnim = new GltfAnimation { Name = animation.Name };

                for (int i = 0; i < nodeCount; i++)
                {
                    var bone = armature.Bones[i];
                    if (!animation.HasTrack(bone.Name))
                    {
                        continue;
                    }

                    var restLocal = bone.RestLocalMatrix;
                    var baseLoc = restLocal.ExtractTranslation();
                    var baseRot = restLocal.ExtractRotation();
                    var baseScale = restLocal.ExtractScale();

                    var tOut = new Vector3[frameCount];
                    var rOut = new Vector4[frameCount];
                    var sOut = new Vector3[frameCount];
                    bool usedT = false, usedR = false, usedS = false;

                    for (int f = 0; f < frameCount; f++)
                    {
                        if (animation.TryGetPose(bone.Name, f, out var scale, out var rotation, out var translation))
                        {
                            if (translation.HasValue) usedT = true;
                            if (rotation.HasValue) usedR = true;
                            if (scale.HasValue) usedS = true;

                            var tr = translation ?? baseLoc;
                            var sc = scale ?? baseScale;
                            var rq = rotation ?? baseRot;
                            rq.Normalize();

                            tOut[f] = tr;
                            rOut[f] = new Vector4(rq.X, rq.Y, rq.Z, rq.W);
                            sOut[f] = sc;
                        }
                        else
                        {
                            tOut[f] = baseLoc;
                            rOut[f] = new Vector4(baseRot.X, baseRot.Y, baseRot.Z, baseRot.W);
                            sOut[f] = baseScale;
                        }
                    }

                    int nodeIndex = boneNodeIndices[i];

                    if (usedT)
                    {
                        int outAcc = AddAccessorVec3(gltf, buffer, tOut, target: null);
                        int samp = gltfAnim.Samplers.Count;
                        gltfAnim.Samplers.Add(new GltfAnimationSampler { Input = timeAcc, Output = outAcc, Interpolation = "LINEAR" });
                        gltfAnim.Channels.Add(new GltfAnimationChannel { Sampler = samp, Target = new GltfAnimationChannelTarget { Node = nodeIndex, Path = "translation" } });
                    }

                    if (usedR)
                    {
                        int outAcc = AddAccessorVec4(gltf, buffer, rOut, target: null);
                        int samp = gltfAnim.Samplers.Count;
                        gltfAnim.Samplers.Add(new GltfAnimationSampler { Input = timeAcc, Output = outAcc, Interpolation = "LINEAR" });
                        gltfAnim.Channels.Add(new GltfAnimationChannel { Sampler = samp, Target = new GltfAnimationChannelTarget { Node = nodeIndex, Path = "rotation" } });
                    }

                    if (usedS)
                    {
                        int outAcc = AddAccessorVec3(gltf, buffer, sOut, target: null);
                        int samp = gltfAnim.Samplers.Count;
                        gltfAnim.Samplers.Add(new GltfAnimationSampler { Input = timeAcc, Output = outAcc, Interpolation = "LINEAR" });
                        gltfAnim.Channels.Add(new GltfAnimationChannel { Sampler = samp, Target = new GltfAnimationChannelTarget { Node = nodeIndex, Path = "scale" } });
                    }
                }

                if (gltfAnim.Channels.Count > 0)
                {
                    gltf.Animations.Add(gltfAnim);
                }
            }
        }

        public static void ExportAnimation(Armature armature, GFTool.Renderer.Scene.GraphicsObjects.Animation animation, string gltfPath)
        {
            if (armature == null) throw new ArgumentNullException(nameof(armature));
            if (animation == null) throw new ArgumentNullException(nameof(animation));
            if (string.IsNullOrWhiteSpace(gltfPath)) throw new ArgumentException("Missing output path.", nameof(gltfPath));

            int boneCount = armature.Bones.Count;
            if (boneCount == 0) throw new InvalidOperationException("Armature has no bones.");

            int frameCount = (int)animation.FrameCount;
            if (frameCount <= 0) throw new InvalidOperationException("Animation has no frames.");
            float fps = animation.FrameRate > 0 ? animation.FrameRate : 30f;

            var outDir = Path.GetDirectoryName(gltfPath) ?? Environment.CurrentDirectory;
            Directory.CreateDirectory(outDir);
            var baseName = Path.GetFileNameWithoutExtension(gltfPath);
            var binName = $"{baseName}.bin";
            var binPath = Path.Combine(outDir, binName);

            var buffer = new BinaryBufferBuilder();
            var gltf = new GltfRoot();
            gltf.Asset = new GltfAsset { Version = "2.0", Generator = "TrinityModelViewer" };

            int sceneIndex = 0;
            gltf.Scene = sceneIndex;
            var scene = new GltfScene();
            gltf.Scenes.Add(scene);

            int rootNodeIndex = gltf.Nodes.Count;
            gltf.Nodes.Add(new GltfNode { Name = baseName, Children = new List<int>() });
            scene.Nodes.Add(rootNodeIndex);

            int[] boneNodeIndices = AddSkeletonNodes(gltf, armature, rootNodeIndex);

            // Some importers only create an Armature object when a skin is present.
            // For animation only exports, a tiny dummy skinned mesh is included so the skeleton
            // imports as an armature instead of a hierarchy of empties.
            int skinIndex = AddSkinOnly(gltf, buffer, armature, boneNodeIndices);
            AddDummySkinnedMesh(gltf, buffer, rootNodeIndex, skinIndex);

            var times = new float[frameCount];
            for (int f = 0; f < frameCount; f++)
            {
                times[f] = f / fps;
            }
            int timeAcc = AddAccessorScalarFloat(gltf, buffer, times);

            var gltfAnim = new GltfAnimation { Name = animation.Name };

            for (int i = 0; i < boneCount; i++)
            {
                var bone = armature.Bones[i];
                if (!animation.HasTrack(bone.Name))
                {
                    continue;
                }

                var tOut = new Vector3[frameCount];
                var rOut = new Vector4[frameCount];
                var sOut = new Vector3[frameCount];
                bool usedT = false, usedR = false, usedS = false;

                for (int f = 0; f < frameCount; f++)
                {
                    if (animation.TryGetPose(bone.Name, f, out var scale, out var rotation, out var translation))
                    {
                        if (translation.HasValue) usedT = true;
                        if (rotation.HasValue) usedR = true;
                        if (scale.HasValue) usedS = true;

                        var tr = translation ?? bone.RestPosition;
                        var sc = scale ?? bone.RestScale;
                        var rq = rotation ?? bone.RestRotation;
                        rq.Normalize();

                        tOut[f] = tr;
                        rOut[f] = new Vector4(rq.X, rq.Y, rq.Z, rq.W);
                        sOut[f] = sc;
                    }
                    else
                    {
                        tOut[f] = bone.RestPosition;
                        rOut[f] = new Vector4(bone.RestRotation.X, bone.RestRotation.Y, bone.RestRotation.Z, bone.RestRotation.W);
                        sOut[f] = bone.RestScale;
                    }
                }

                int nodeIndex = boneNodeIndices[i];

                if (usedT)
                {
                    int outAcc = AddAccessorVec3(gltf, buffer, tOut, target: null);
                    int samp = gltfAnim.Samplers.Count;
                    gltfAnim.Samplers.Add(new GltfAnimationSampler { Input = timeAcc, Output = outAcc, Interpolation = "LINEAR" });
                    gltfAnim.Channels.Add(new GltfAnimationChannel { Sampler = samp, Target = new GltfAnimationChannelTarget { Node = nodeIndex, Path = "translation" } });
                }

                if (usedR)
                {
                    int outAcc = AddAccessorVec4(gltf, buffer, rOut, target: null);
                    int samp = gltfAnim.Samplers.Count;
                    gltfAnim.Samplers.Add(new GltfAnimationSampler { Input = timeAcc, Output = outAcc, Interpolation = "LINEAR" });
                    gltfAnim.Channels.Add(new GltfAnimationChannel { Sampler = samp, Target = new GltfAnimationChannelTarget { Node = nodeIndex, Path = "rotation" } });
                }

                if (usedS)
                {
                    int outAcc = AddAccessorVec3(gltf, buffer, sOut, target: null);
                    int samp = gltfAnim.Samplers.Count;
                    gltfAnim.Samplers.Add(new GltfAnimationSampler { Input = timeAcc, Output = outAcc, Interpolation = "LINEAR" });
                    gltfAnim.Channels.Add(new GltfAnimationChannel { Sampler = samp, Target = new GltfAnimationChannelTarget { Node = nodeIndex, Path = "scale" } });
                }
            }

            gltf.Animations.Add(gltfAnim);

            var binBytes = buffer.ToArray();
            File.WriteAllBytes(binPath, binBytes);
            gltf.Buffers.Add(new GltfBuffer { Uri = binName, ByteLength = binBytes.Length });

            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
            var json = JsonSerializer.Serialize(gltf, jsonOptions);
            File.WriteAllText(gltfPath, json);
        }
    }
}
