using GFTool.Renderer.Core;
using GFTool.Renderer.Scene.GraphicsObjects;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TrinityModelViewer.Export
{
    internal static class GltfExporter
    {
        public static void ExportModel(Model model, string gltfPath)
        {
            ExportModel(model, gltfPath, animations: null);
        }

        public static void ExportModel(Model model, string gltfPath, IReadOnlyList<GFTool.Renderer.Scene.GraphicsObjects.Animation>? animations)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));
            if (string.IsNullOrWhiteSpace(gltfPath)) throw new ArgumentException("Missing output path.", nameof(gltfPath));

            var data = model.CreateExportData();
            if (data.Submeshes.Count == 0) throw new InvalidOperationException("Model has no meshes to export.");

            var outDir = Path.GetDirectoryName(gltfPath) ?? Environment.CurrentDirectory;
            Directory.CreateDirectory(outDir);
            var baseName = Path.GetFileNameWithoutExtension(gltfPath);
            var binName = $"{baseName}.bin";
            var binPath = Path.Combine(outDir, binName);
            var texDir = Path.Combine(outDir, $"{baseName}_textures");
            Directory.CreateDirectory(texDir);

            var materialByName = data.Materials
                .Where(m => m != null && !string.IsNullOrWhiteSpace(m.Name))
                .GroupBy(m => m.Name, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            var buffer = new BinaryBufferBuilder();
            var gltf = new GltfRoot();
            gltf.Asset = new GltfAsset { Version = "2.0", Generator = "TrinityModelViewer" };

            gltf.Samplers.Add(new GltfSampler
            {
                MagFilter = 9729,
                MinFilter = 9729,
                WrapS = 33071,
                WrapT = 33071
            });

            int sceneIndex = 0;
            gltf.Scene = sceneIndex;
            var scene = new GltfScene();
            gltf.Scenes.Add(scene);

            int rootNodeIndex = gltf.Nodes.Count;
            gltf.Nodes.Add(new GltfNode { Name = data.Name, Children = new List<int>() });
            scene.Nodes.Add(rootNodeIndex);

            int? skinIndex = null;
            int[]? boneNodeIndices = null;
            if (data.Armature != null && data.Armature.Bones.Count > 0)
            {
                (skinIndex, boneNodeIndices) = AddSkin(gltf, buffer, data.Armature, rootNodeIndex);
            }

            var gltfMaterialIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var texCache = ExportAllTextures(gltf, texDir, data.Materials);

            foreach (var sub in data.Submeshes)
            {
                if (sub.Positions.Length == 0 || sub.Indices.Length == 0) continue;

                int materialIndex = GetOrCreateMaterial(gltf, gltfMaterialIndex, materialByName, texCache, sub.MaterialName, texDir);
                int meshIndex = AddMesh(gltf, buffer, sub, materialIndex);

                var node = new GltfNode
                {
                    Name = sub.Name,
                    Mesh = meshIndex
                };
                if (skinIndex.HasValue && sub.HasSkinning)
                {
                    node.Skin = skinIndex.Value;
                }
                int nodeIndex = gltf.Nodes.Count;
                gltf.Nodes.Add(node);
                gltf.Nodes[rootNodeIndex].Children!.Add(nodeIndex);
            }

            if (animations != null && animations.Count > 0 && data.Armature != null && boneNodeIndices != null)
            {
                AddAnimations(gltf, buffer, data.Armature, boneNodeIndices, animations);
            }

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

        private static int AddSkinOnly(GltfRoot gltf, BinaryBufferBuilder buffer, Armature armature, int[] boneNodeIndices)
        {
            int boneCount = armature.Bones.Count;
            var invBind = new Matrix4[boneCount];
            for (int i = 0; i < boneCount; i++)
            {
                var b = armature.Bones[i];
                var m = b.HasJointInverseBind ? b.JointInverseBindWorld : b.InverseBindWorld;
                invBind[i] = Matrix4.Transpose(m);
            }

            int accessor = AddAccessorMat4(gltf, buffer, invBind);
            var skin = new GltfSkin
            {
                InverseBindMatrices = accessor,
                Joints = boneNodeIndices.ToList(),
                Skeleton = boneNodeIndices.Length > 0 ? boneNodeIndices[0] : null
            };

            int skinIndex = gltf.Skins.Count;
            gltf.Skins.Add(skin);
            return skinIndex;
        }

        private static void AddDummySkinnedMesh(GltfRoot gltf, BinaryBufferBuilder buffer, int rootNodeIndex, int skinIndex)
        {
            // Single point at origin, weighted 100% to joint 0.
            var positions = new[] { Vector3.Zero };
            var joints = new[] { new Vector4(0, 0, 0, 0) };
            var weights = new[] { new Vector4(1, 0, 0, 0) };
            var indices = new uint[] { 0 };

            int posAcc = AddAccessorVec3(gltf, buffer, positions, target: 34962, includeMinMax: true);
            int jointAcc = AddAccessorUShort4(gltf, buffer, joints, target: 34962);
            int weightAcc = AddAccessorVec4(gltf, buffer, weights, target: 34962);
            int idxAcc = AddAccessorIndices(gltf, buffer, indices);

            var prim = new GltfPrimitive
            {
                Attributes = new Dictionary<string, int>
                {
                    ["POSITION"] = posAcc,
                    ["JOINTS_0"] = jointAcc,
                    ["WEIGHTS_0"] = weightAcc
                },
                Indices = idxAcc,
                Mode = 0 // POINTS
            };

            var mesh = new GltfMesh
            {
                Name = "SkinDummy",
                Primitives = new List<GltfPrimitive> { prim }
            };

            int meshIndex = gltf.Meshes.Count;
            gltf.Meshes.Add(mesh);

            int nodeIndex = gltf.Nodes.Count;
            gltf.Nodes.Add(new GltfNode
            {
                Name = "SkinDummy",
                Mesh = meshIndex,
                Skin = skinIndex
            });

            gltf.Nodes[rootNodeIndex].Children ??= new List<int>();
            gltf.Nodes[rootNodeIndex].Children.Add(nodeIndex);
        }

        private static Dictionary<string, int> ExportAllTextures(GltfRoot gltf, string texDir, IReadOnlyList<Material> materials)
        {
            var cache = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var mat in materials)
            {
                if (mat == null) continue;
                foreach (var tex in mat.Textures)
                {
                    if (tex == null) continue;
                    var key = GetTextureKey(tex);
                    if (cache.ContainsKey(key)) continue;

                    using var bmp = tex.LoadPreviewBitmap();
                    if (bmp == null) continue;

                    string outName = MakeUniqueTextureFileName(usedNames, tex);
                    string outPath = Path.Combine(texDir, outName);
                    bmp.Save(outPath, ImageFormat.Png);

                    int imgIndex = gltf.Images.Count;
                    gltf.Images.Add(new GltfImage { Uri = $"{Path.GetFileName(texDir)}/{outName}" });
                    int texIndex = gltf.Textures.Count;
                    gltf.Textures.Add(new GltfTexture { Sampler = 0, Source = imgIndex, Name = tex.Name });

                    cache[key] = texIndex;
                }
            }

            return cache;
        }

        private static string GetTextureKey(Texture tex)
        {
            return $"{tex.Name}|{tex.SourceFile}";
        }

        private static string MakeUniqueTextureFileName(HashSet<string> usedNames, Texture tex)
        {
            string src = tex.SourceFile ?? string.Empty;
            string baseName = Path.GetFileNameWithoutExtension(src);
            if (string.IsNullOrWhiteSpace(baseName))
            {
                baseName = tex.Name;
            }

            string fileName = $"{baseName}.png";
            fileName = SanitizeFileName(fileName);
            if (usedNames.Add(fileName))
            {
                return fileName;
            }

            for (int i = 2; i < 10000; i++)
            {
                string candidate = SanitizeFileName($"{baseName}_{i}.png");
                if (usedNames.Add(candidate))
                {
                    return candidate;
                }
            }

            // Extremely unlikely.
            return SanitizeFileName($"{baseName}_{Guid.NewGuid():N}.png");
        }

        private static (int skinIndex, int[] boneNodeIndices) AddSkin(GltfRoot gltf, BinaryBufferBuilder buffer, Armature armature, int rootNodeIndex)
        {
            int boneCount = armature.Bones.Count;
            var boneNodeIndices = new int[boneCount];

            for (int i = 0; i < boneCount; i++)
            {
                var bone = armature.Bones[i];
                var node = new GltfNode
                {
                    Name = bone.Name,
                    Translation = new[] { bone.RestPosition.X, bone.RestPosition.Y, bone.RestPosition.Z },
                    Rotation = new[] { bone.RestRotation.X, bone.RestRotation.Y, bone.RestRotation.Z, bone.RestRotation.W },
                    Scale = new[] { bone.RestScale.X, bone.RestScale.Y, bone.RestScale.Z },
                    Children = new List<int>()
                };
                boneNodeIndices[i] = gltf.Nodes.Count;
                gltf.Nodes.Add(node);
            }

            for (int i = 0; i < boneCount; i++)
            {
                int parent = armature.Bones[i].ParentIndex;
                if (parent >= 0 && parent < boneCount && parent != i)
                {
                    gltf.Nodes[boneNodeIndices[parent]].Children!.Add(boneNodeIndices[i]);
                }
                else
                {
                    gltf.Nodes[rootNodeIndex].Children!.Add(boneNodeIndices[i]);
                }
            }

            // Inverse bind matrices prefer joint info when present. Computed inverse bind is used otherwise.
            var invBind = new Matrix4[boneCount];
            for (int i = 0; i < boneCount; i++)
            {
                var b = armature.Bones[i];
                // Renderer uses row vector math (v' = v * M). glTF uses column vector math (v' = M * v).
                // A transpose is applied on export.
                var m = b.HasJointInverseBind ? b.JointInverseBindWorld : b.InverseBindWorld;
                invBind[i] = Matrix4.Transpose(m);
            }

            int accessor = AddAccessorMat4(gltf, buffer, invBind);

            var skin = new GltfSkin
            {
                InverseBindMatrices = accessor,
                Joints = boneNodeIndices.ToList(),
                Skeleton = boneNodeIndices[0]
            };

            int skinIndex = gltf.Skins.Count;
            gltf.Skins.Add(skin);
            return (skinIndex, boneNodeIndices);
        }

        private static int[] AddSkeletonNodes(GltfRoot gltf, Armature armature, int rootNodeIndex)
        {
            int boneCount = armature.Bones.Count;
            var boneNodeIndices = new int[boneCount];

            for (int i = 0; i < boneCount; i++)
            {
                var bone = armature.Bones[i];
                var node = new GltfNode
                {
                    Name = bone.Name,
                    Translation = new[] { bone.RestPosition.X, bone.RestPosition.Y, bone.RestPosition.Z },
                    Rotation = new[] { bone.RestRotation.X, bone.RestRotation.Y, bone.RestRotation.Z, bone.RestRotation.W },
                    Scale = new[] { bone.RestScale.X, bone.RestScale.Y, bone.RestScale.Z },
                    Children = new List<int>()
                };
                boneNodeIndices[i] = gltf.Nodes.Count;
                gltf.Nodes.Add(node);
            }

            for (int i = 0; i < boneCount; i++)
            {
                int parent = armature.Bones[i].ParentIndex;
                if (parent >= 0 && parent < boneCount && parent != i)
                {
                    gltf.Nodes[boneNodeIndices[parent]].Children!.Add(boneNodeIndices[i]);
                }
                else
                {
                    gltf.Nodes[rootNodeIndex].Children!.Add(boneNodeIndices[i]);
                }
            }

            return boneNodeIndices;
        }

        private static int AddMesh(GltfRoot gltf, BinaryBufferBuilder buffer, Model.ExportSubmesh sub, int materialIndex)
        {
            int vertexCount = sub.Positions.Length;

            var positions = sub.Positions;
            var normals = sub.Normals.Length == vertexCount ? sub.Normals : Enumerable.Repeat(Vector3.UnitY, vertexCount).ToArray();
            var uvs = sub.UVs.Length == vertexCount
                ? sub.UVs.Select(uv => new Vector2(uv.X, 1f - uv.Y)).ToArray()
                : Enumerable.Repeat(Vector2.Zero, vertexCount).ToArray();
            var tangents = sub.Tangents.Length == vertexCount ? sub.Tangents : Array.Empty<Vector4>();
            var joints = sub.BlendIndices.Length == vertexCount ? sub.BlendIndices : Enumerable.Repeat(Vector4.Zero, vertexCount).ToArray();
            var weights = sub.BlendWeights.Length == vertexCount ? sub.BlendWeights : Enumerable.Repeat(new Vector4(1, 0, 0, 0), vertexCount).ToArray();

            int posAcc = AddAccessorVec3(gltf, buffer, positions, target: 34962, includeMinMax: true);
            int nrmAcc = AddAccessorVec3(gltf, buffer, normals, target: 34962);
            int uvAcc = AddAccessorVec2(gltf, buffer, uvs, target: 34962);

            int? tanAcc = null;
            if (sub.HasTangents && tangents.Length == vertexCount)
            {
                tanAcc = AddAccessorVec4(gltf, buffer, tangents, target: 34962);
            }

            int? jointAcc = null;
            int? weightAcc = null;
            if (sub.HasSkinning)
            {
                jointAcc = AddAccessorUShort4(gltf, buffer, joints, target: 34962);
                weightAcc = AddAccessorVec4(gltf, buffer, weights, target: 34962);
            }

            int idxAcc = AddAccessorIndices(gltf, buffer, sub.Indices);

            var prim = new GltfPrimitive
            {
                Attributes = new Dictionary<string, int>
                {
                    ["POSITION"] = posAcc,
                    ["NORMAL"] = nrmAcc,
                    ["TEXCOORD_0"] = uvAcc
                },
                Indices = idxAcc,
                Material = materialIndex
            };

            if (tanAcc.HasValue)
            {
                prim.Attributes["TANGENT"] = tanAcc.Value;
            }

            if (jointAcc.HasValue && weightAcc.HasValue)
            {
                prim.Attributes["JOINTS_0"] = jointAcc.Value;
                prim.Attributes["WEIGHTS_0"] = weightAcc.Value;
            }

            var mesh = new GltfMesh
            {
                Name = sub.Name,
                Primitives = new List<GltfPrimitive> { prim }
            };
            int meshIndex = gltf.Meshes.Count;
            gltf.Meshes.Add(mesh);
            return meshIndex;
        }

        private static int GetOrCreateMaterial(
            GltfRoot gltf,
            Dictionary<string, int> gltfMaterialIndex,
            Dictionary<string, Material> materialByName,
            Dictionary<string, int> textureCache,
            string materialName,
            string texDir)
        {
            materialName ??= string.Empty;
            if (gltfMaterialIndex.TryGetValue(materialName, out int existing))
            {
                return existing;
            }

            materialByName.TryGetValue(materialName, out var mat);
            var texByName = mat?.Textures?.ToDictionary(t => t.Name, StringComparer.OrdinalIgnoreCase) ?? new Dictionary<string, Texture>(StringComparer.OrdinalIgnoreCase);

            int? baseColorTex = TryGetTextureIndex(textureCache, texByName, "BaseColorMap");
            int? normalTex = TryGetTextureIndex(textureCache, texByName, "NormalMap");
            int? aoTex = TryGetTextureIndex(textureCache, texByName, "AOMap");

            int? mrTex = TryAddMetallicRoughnessTexture(gltf, texDir, texByName);

            var pbr = new GltfPbrMetallicRoughness();
            if (baseColorTex.HasValue)
            {
                pbr.BaseColorTexture = new GltfTextureInfo { Index = baseColorTex.Value };
            }
            pbr.BaseColorFactor = new[] { 1f, 1f, 1f, 1f };
            pbr.MetallicFactor = 1f;
            pbr.RoughnessFactor = 1f;
            if (mrTex.HasValue)
            {
                pbr.MetallicRoughnessTexture = new GltfTextureInfo { Index = mrTex.Value };
            }

            var gltfMat = new GltfMaterial
            {
                Name = string.IsNullOrWhiteSpace(materialName) ? "Material" : materialName,
                PbrMetallicRoughness = pbr,
                AlphaMode = mat?.IsTransparent == true ? "BLEND" : null,
                DoubleSided = true
            };

            if (normalTex.HasValue)
            {
                gltfMat.NormalTexture = new GltfNormalTextureInfo { Index = normalTex.Value, Scale = 1f };
            }

            if (aoTex.HasValue)
            {
                gltfMat.OcclusionTexture = new GltfOcclusionTextureInfo { Index = aoTex.Value, Strength = 1f };
            }

            int gltfIndex = gltf.Materials.Count;
            gltf.Materials.Add(gltfMat);
            gltfMaterialIndex[materialName] = gltfIndex;
            return gltfIndex;
        }

        private static int? TryGetTextureIndex(Dictionary<string, int> textureCache, Dictionary<string, Texture> texByName, string textureName)
        {
            if (!texByName.TryGetValue(textureName, out var tex) || tex == null)
            {
                return null;
            }

            if (textureCache.TryGetValue(GetTextureKey(tex), out var idx))
            {
                return idx;
            }

            return null;
        }

        private static int? TryAddMetallicRoughnessTexture(GltfRoot gltf, string texDir, Dictionary<string, Texture> texByName)
        {
            texByName.TryGetValue("RoughnessMap", out var roughTex);
            texByName.TryGetValue("MetallicMap", out var metalTex);
            if (roughTex == null && metalTex == null)
            {
                return null;
            }

            using var roughBmp = roughTex?.LoadPreviewBitmap();
            using var metalBmp = metalTex?.LoadPreviewBitmap();
            if (roughBmp == null && metalBmp == null) return null;

            int width = roughBmp?.Width ?? metalBmp!.Width;
            int height = roughBmp?.Height ?? metalBmp!.Height;

            using var outBmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    byte rough = 255;
                    byte metal = 0;
                    if (roughBmp != null)
                    {
                        var c = roughBmp.GetPixel(x * roughBmp.Width / width, y * roughBmp.Height / height);
                        rough = c.R;
                    }
                    if (metalBmp != null)
                    {
                        var c = metalBmp.GetPixel(x * metalBmp.Width / width, y * metalBmp.Height / height);
                        metal = c.R;
                    }
                    // glTF expects roughness in G and metallic in B.
                    outBmp.SetPixel(x, y, Color.FromArgb(255, 0, rough, metal));
                }
            }

            string fileName = "metallicRoughness.png";
            string outPath = Path.Combine(texDir, fileName);
            outBmp.Save(outPath, ImageFormat.Png);

            int imgIndex = gltf.Images.Count;
            gltf.Images.Add(new GltfImage { Uri = $"{Path.GetFileName(texDir)}/{fileName}" });
            int texIndex = gltf.Textures.Count;
            gltf.Textures.Add(new GltfTexture { Sampler = 0, Source = imgIndex, Name = "metallicRoughness" });
            return texIndex;
        }

        private static Bitmap FlipGreenChannel(Bitmap src)
        {
            var dst = new Bitmap(src.Width, src.Height, PixelFormat.Format32bppArgb);
            for (int y = 0; y < src.Height; y++)
            {
                for (int x = 0; x < src.Width; x++)
                {
                    var c = src.GetPixel(x, y);
                    dst.SetPixel(x, y, Color.FromArgb(c.A, c.R, 255 - c.G, c.B));
                }
            }
            return dst;
        }

        private static string SanitizeFileName(string name)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(c, '_');
            }
            return name;
        }

        private static int AddAccessorIndices(GltfRoot gltf, BinaryBufferBuilder buffer, uint[] indices)
        {
            uint max = 0;
            for (int i = 0; i < indices.Length; i++) max = Math.Max(max, indices[i]);

            if (max <= ushort.MaxValue)
            {
                var data = new ushort[indices.Length];
                for (int i = 0; i < indices.Length; i++) data[i] = (ushort)indices[i];
                return AddAccessorScalar(gltf, buffer, data, componentType: 5123, target: 34963);
            }

            return AddAccessorScalar(gltf, buffer, indices, componentType: 5125, target: 34963);
        }

        private static int AddAccessorScalar(GltfRoot gltf, BinaryBufferBuilder buffer, ushort[] values, int componentType, int target)
        {
            var bytes = new byte[values.Length * 2];
            Buffer.BlockCopy(values, 0, bytes, 0, bytes.Length);
            int view = AddBufferView(gltf, buffer, bytes, target);
            int accessorIndex = gltf.Accessors.Count;
            gltf.Accessors.Add(new GltfAccessor
            {
                BufferView = view,
                ByteOffset = 0,
                ComponentType = componentType,
                Count = values.Length,
                Type = "SCALAR"
            });
            return accessorIndex;
        }

        private static int AddAccessorScalar(GltfRoot gltf, BinaryBufferBuilder buffer, uint[] values, int componentType, int target)
        {
            var bytes = new byte[values.Length * 4];
            Buffer.BlockCopy(values, 0, bytes, 0, bytes.Length);
            int view = AddBufferView(gltf, buffer, bytes, target);
            int accessorIndex = gltf.Accessors.Count;
            gltf.Accessors.Add(new GltfAccessor
            {
                BufferView = view,
                ByteOffset = 0,
                ComponentType = componentType,
                Count = values.Length,
                Type = "SCALAR"
            });
            return accessorIndex;
        }

        private static int AddAccessorVec2(GltfRoot gltf, BinaryBufferBuilder buffer, Vector2[] values, int? target)
        {
            var bytes = new byte[values.Length * 8];
            int o = 0;
            for (int i = 0; i < values.Length; i++)
            {
                WriteFloat(bytes, ref o, values[i].X);
                WriteFloat(bytes, ref o, values[i].Y);
            }
            int view = AddBufferView(gltf, buffer, bytes, target);
            int accessorIndex = gltf.Accessors.Count;
            gltf.Accessors.Add(new GltfAccessor
            {
                BufferView = view,
                ByteOffset = 0,
                ComponentType = 5126,
                Count = values.Length,
                Type = "VEC2"
            });
            return accessorIndex;
        }

        private static int AddAccessorVec3(GltfRoot gltf, BinaryBufferBuilder buffer, Vector3[] values, int? target, bool includeMinMax = false)
        {
            var bytes = new byte[values.Length * 12];
            int o = 0;
            float minX = float.PositiveInfinity, minY = float.PositiveInfinity, minZ = float.PositiveInfinity;
            float maxX = float.NegativeInfinity, maxY = float.NegativeInfinity, maxZ = float.NegativeInfinity;
            for (int i = 0; i < values.Length; i++)
            {
                var v = values[i];
                if (includeMinMax)
                {
                    minX = Math.Min(minX, v.X); minY = Math.Min(minY, v.Y); minZ = Math.Min(minZ, v.Z);
                    maxX = Math.Max(maxX, v.X); maxY = Math.Max(maxY, v.Y); maxZ = Math.Max(maxZ, v.Z);
                }
                WriteFloat(bytes, ref o, v.X);
                WriteFloat(bytes, ref o, v.Y);
                WriteFloat(bytes, ref o, v.Z);
            }
            int view = AddBufferView(gltf, buffer, bytes, target);
            int accessorIndex = gltf.Accessors.Count;
            var acc = new GltfAccessor
            {
                BufferView = view,
                ByteOffset = 0,
                ComponentType = 5126,
                Count = values.Length,
                Type = "VEC3"
            };
            if (includeMinMax && values.Length > 0)
            {
                acc.Min = new[] { minX, minY, minZ };
                acc.Max = new[] { maxX, maxY, maxZ };
            }
            gltf.Accessors.Add(acc);
            return accessorIndex;
        }

        private static int AddAccessorVec4(GltfRoot gltf, BinaryBufferBuilder buffer, Vector4[] values, int? target)
        {
            var bytes = new byte[values.Length * 16];
            int o = 0;
            for (int i = 0; i < values.Length; i++)
            {
                var v = values[i];
                WriteFloat(bytes, ref o, v.X);
                WriteFloat(bytes, ref o, v.Y);
                WriteFloat(bytes, ref o, v.Z);
                WriteFloat(bytes, ref o, v.W);
            }
            int view = AddBufferView(gltf, buffer, bytes, target);
            int accessorIndex = gltf.Accessors.Count;
            gltf.Accessors.Add(new GltfAccessor
            {
                BufferView = view,
                ByteOffset = 0,
                ComponentType = 5126,
                Count = values.Length,
                Type = "VEC4"
            });
            return accessorIndex;
        }

        private static int AddAccessorUShort4(GltfRoot gltf, BinaryBufferBuilder buffer, Vector4[] values, int? target)
        {
            var bytes = new byte[values.Length * 8];
            int o = 0;
            for (int i = 0; i < values.Length; i++)
            {
                var v = values[i];
                WriteUShort(bytes, ref o, (ushort)Math.Clamp((int)MathF.Round(v.X), 0, ushort.MaxValue));
                WriteUShort(bytes, ref o, (ushort)Math.Clamp((int)MathF.Round(v.Y), 0, ushort.MaxValue));
                WriteUShort(bytes, ref o, (ushort)Math.Clamp((int)MathF.Round(v.Z), 0, ushort.MaxValue));
                WriteUShort(bytes, ref o, (ushort)Math.Clamp((int)MathF.Round(v.W), 0, ushort.MaxValue));
            }
            int view = AddBufferView(gltf, buffer, bytes, target);
            int accessorIndex = gltf.Accessors.Count;
            gltf.Accessors.Add(new GltfAccessor
            {
                BufferView = view,
                ByteOffset = 0,
                ComponentType = 5123,
                Count = values.Length,
                Type = "VEC4"
            });
            return accessorIndex;
        }

        private static int AddAccessorMat4(GltfRoot gltf, BinaryBufferBuilder buffer, Matrix4[] values)
        {
            var bytes = new byte[values.Length * 64];
            int o = 0;
            for (int i = 0; i < values.Length; i++)
            {
                // glTF matrices are column major. OpenTK.Matrix4 stores M11.. as row and col fields.
                // Values are written explicitly in column major order for clarity.
                var m = values[i];
                WriteFloat(bytes, ref o, m.M11); WriteFloat(bytes, ref o, m.M21); WriteFloat(bytes, ref o, m.M31); WriteFloat(bytes, ref o, m.M41);
                WriteFloat(bytes, ref o, m.M12); WriteFloat(bytes, ref o, m.M22); WriteFloat(bytes, ref o, m.M32); WriteFloat(bytes, ref o, m.M42);
                WriteFloat(bytes, ref o, m.M13); WriteFloat(bytes, ref o, m.M23); WriteFloat(bytes, ref o, m.M33); WriteFloat(bytes, ref o, m.M43);
                WriteFloat(bytes, ref o, m.M14); WriteFloat(bytes, ref o, m.M24); WriteFloat(bytes, ref o, m.M34); WriteFloat(bytes, ref o, m.M44);
            }

            int view = AddBufferView(gltf, buffer, bytes, target: null);
            int accessorIndex = gltf.Accessors.Count;
            gltf.Accessors.Add(new GltfAccessor
            {
                BufferView = view,
                ByteOffset = 0,
                ComponentType = 5126,
                Count = values.Length,
                Type = "MAT4"
            });
            return accessorIndex;
        }

        private static int AddAccessorScalarFloat(GltfRoot gltf, BinaryBufferBuilder buffer, float[] values)
        {
            var bytes = new byte[values.Length * 4];
            Buffer.BlockCopy(values, 0, bytes, 0, bytes.Length);
            int view = AddBufferView(gltf, buffer, bytes, target: null);
            int accessorIndex = gltf.Accessors.Count;
            gltf.Accessors.Add(new GltfAccessor
            {
                BufferView = view,
                ByteOffset = 0,
                ComponentType = 5126,
                Count = values.Length,
                Type = "SCALAR"
            });
            return accessorIndex;
        }

        private static int AddBufferView(GltfRoot gltf, BinaryBufferBuilder buffer, byte[] bytes, int? target)
        {
            var (offset, length) = buffer.Append(bytes, align: 4);
            int viewIndex = gltf.BufferViews.Count;
            gltf.BufferViews.Add(new GltfBufferView
            {
                Buffer = 0,
                ByteOffset = offset,
                ByteLength = length,
                Target = target
            });
            return viewIndex;
        }

        private static void WriteFloat(byte[] dst, ref int offset, float value)
        {
            var b = BitConverter.GetBytes(value);
            Buffer.BlockCopy(b, 0, dst, offset, 4);
            offset += 4;
        }

        private static void WriteUShort(byte[] dst, ref int offset, ushort value)
        {
            dst[offset++] = (byte)(value & 0xFF);
            dst[offset++] = (byte)((value >> 8) & 0xFF);
        }

        private sealed class BinaryBufferBuilder
        {
            private readonly List<byte> _data = new List<byte>(1024 * 1024);

            public (int offset, int length) Append(byte[] bytes, int align)
            {
                Align(align);
                int offset = _data.Count;
                _data.AddRange(bytes);
                return (offset, bytes.Length);
            }

            private void Align(int align)
            {
                int pad = (_data.Count % align) == 0 ? 0 : (align - (_data.Count % align));
                for (int i = 0; i < pad; i++) _data.Add(0);
            }

            public byte[] ToArray() => _data.ToArray();
        }

        private sealed class GltfRoot
        {
            [JsonPropertyName("asset")] public GltfAsset Asset { get; set; } = null!;
            [JsonPropertyName("scene")] public int Scene { get; set; }
            [JsonPropertyName("scenes")] public List<GltfScene> Scenes { get; set; } = new List<GltfScene>();
            [JsonPropertyName("nodes")] public List<GltfNode> Nodes { get; set; } = new List<GltfNode>();
            [JsonPropertyName("meshes")] public List<GltfMesh> Meshes { get; set; } = new List<GltfMesh>();
            [JsonPropertyName("materials")] public List<GltfMaterial> Materials { get; set; } = new List<GltfMaterial>();
            [JsonPropertyName("accessors")] public List<GltfAccessor> Accessors { get; set; } = new List<GltfAccessor>();
            [JsonPropertyName("bufferViews")] public List<GltfBufferView> BufferViews { get; set; } = new List<GltfBufferView>();
            [JsonPropertyName("buffers")] public List<GltfBuffer> Buffers { get; set; } = new List<GltfBuffer>();
            [JsonPropertyName("images")] public List<GltfImage> Images { get; set; } = new List<GltfImage>();
            [JsonPropertyName("textures")] public List<GltfTexture> Textures { get; set; } = new List<GltfTexture>();
            [JsonPropertyName("samplers")] public List<GltfSampler> Samplers { get; set; } = new List<GltfSampler>();
            [JsonPropertyName("skins")] public List<GltfSkin> Skins { get; set; } = new List<GltfSkin>();
            [JsonPropertyName("animations")] public List<GltfAnimation> Animations { get; set; } = new List<GltfAnimation>();
        }

        private sealed class GltfAsset
        {
            [JsonPropertyName("version")] public string Version { get; set; } = "2.0";
            [JsonPropertyName("generator")] public string? Generator { get; set; }
        }

        private sealed class GltfScene
        {
            [JsonPropertyName("nodes")] public List<int> Nodes { get; set; } = new List<int>();
        }

        private sealed class GltfNode
        {
            [JsonPropertyName("name")] public string? Name { get; set; }
            [JsonPropertyName("children")] public List<int>? Children { get; set; }
            [JsonPropertyName("mesh")] public int? Mesh { get; set; }
            [JsonPropertyName("skin")] public int? Skin { get; set; }
            [JsonPropertyName("translation")] public float[]? Translation { get; set; }
            [JsonPropertyName("rotation")] public float[]? Rotation { get; set; }
            [JsonPropertyName("scale")] public float[]? Scale { get; set; }
        }

        private sealed class GltfMesh
        {
            [JsonPropertyName("name")] public string? Name { get; set; }
            [JsonPropertyName("primitives")] public List<GltfPrimitive> Primitives { get; set; } = new List<GltfPrimitive>();
        }

        private sealed class GltfPrimitive
        {
            [JsonPropertyName("attributes")] public Dictionary<string, int> Attributes { get; set; } = new Dictionary<string, int>();
            [JsonPropertyName("indices")] public int? Indices { get; set; }
            [JsonPropertyName("material")] public int? Material { get; set; }
            [JsonPropertyName("mode")] public int Mode { get; set; } = 4; // TRIANGLES
        }

        private sealed class GltfBuffer
        {
            [JsonPropertyName("uri")] public string Uri { get; set; } = string.Empty;
            [JsonPropertyName("byteLength")] public int ByteLength { get; set; }
        }

        private sealed class GltfBufferView
        {
            [JsonPropertyName("buffer")] public int Buffer { get; set; }
            [JsonPropertyName("byteOffset")] public int ByteOffset { get; set; }
            [JsonPropertyName("byteLength")] public int ByteLength { get; set; }
            [JsonPropertyName("target")] public int? Target { get; set; }
        }

        private sealed class GltfAccessor
        {
            [JsonPropertyName("bufferView")] public int BufferView { get; set; }
            [JsonPropertyName("byteOffset")] public int ByteOffset { get; set; }
            [JsonPropertyName("componentType")] public int ComponentType { get; set; }
            [JsonPropertyName("count")] public int Count { get; set; }
            [JsonPropertyName("type")] public string Type { get; set; } = string.Empty;
            [JsonPropertyName("min")] public float[]? Min { get; set; }
            [JsonPropertyName("max")] public float[]? Max { get; set; }
        }

        private sealed class GltfMaterial
        {
            [JsonPropertyName("name")] public string? Name { get; set; }
            [JsonPropertyName("pbrMetallicRoughness")] public GltfPbrMetallicRoughness? PbrMetallicRoughness { get; set; }
            [JsonPropertyName("normalTexture")] public GltfNormalTextureInfo? NormalTexture { get; set; }
            [JsonPropertyName("occlusionTexture")] public GltfOcclusionTextureInfo? OcclusionTexture { get; set; }
            [JsonPropertyName("alphaMode")] public string? AlphaMode { get; set; }
            [JsonPropertyName("doubleSided")] public bool? DoubleSided { get; set; }
        }

        private sealed class GltfPbrMetallicRoughness
        {
            [JsonPropertyName("baseColorFactor")] public float[]? BaseColorFactor { get; set; }
            [JsonPropertyName("baseColorTexture")] public GltfTextureInfo? BaseColorTexture { get; set; }
            [JsonPropertyName("metallicFactor")] public float MetallicFactor { get; set; }
            [JsonPropertyName("roughnessFactor")] public float RoughnessFactor { get; set; }
            [JsonPropertyName("metallicRoughnessTexture")] public GltfTextureInfo? MetallicRoughnessTexture { get; set; }
        }

        private class GltfTextureInfo
        {
            [JsonPropertyName("index")] public int Index { get; set; }
            [JsonPropertyName("texCoord")] public int? TexCoord { get; set; }
        }

        private sealed class GltfNormalTextureInfo : GltfTextureInfo
        {
            [JsonPropertyName("scale")] public float? Scale { get; set; }
        }

        private sealed class GltfOcclusionTextureInfo : GltfTextureInfo
        {
            [JsonPropertyName("strength")] public float? Strength { get; set; }
        }

        private sealed class GltfImage
        {
            [JsonPropertyName("uri")] public string Uri { get; set; } = string.Empty;
        }

        private sealed class GltfTexture
        {
            [JsonPropertyName("name")] public string? Name { get; set; }
            [JsonPropertyName("sampler")] public int? Sampler { get; set; }
            [JsonPropertyName("source")] public int Source { get; set; }
        }

        private sealed class GltfSampler
        {
            [JsonPropertyName("magFilter")] public int? MagFilter { get; set; }
            [JsonPropertyName("minFilter")] public int? MinFilter { get; set; }
            [JsonPropertyName("wrapS")] public int? WrapS { get; set; }
            [JsonPropertyName("wrapT")] public int? WrapT { get; set; }
        }

        private sealed class GltfSkin
        {
            [JsonPropertyName("inverseBindMatrices")] public int? InverseBindMatrices { get; set; }
            [JsonPropertyName("joints")] public List<int> Joints { get; set; } = new List<int>();
            [JsonPropertyName("skeleton")] public int? Skeleton { get; set; }
        }

        private sealed class GltfAnimation
        {
            [JsonPropertyName("name")] public string? Name { get; set; }
            [JsonPropertyName("samplers")] public List<GltfAnimationSampler> Samplers { get; set; } = new List<GltfAnimationSampler>();
            [JsonPropertyName("channels")] public List<GltfAnimationChannel> Channels { get; set; } = new List<GltfAnimationChannel>();
        }

        private sealed class GltfAnimationSampler
        {
            [JsonPropertyName("input")] public int Input { get; set; }
            [JsonPropertyName("output")] public int Output { get; set; }
            [JsonPropertyName("interpolation")] public string? Interpolation { get; set; }
        }

        private sealed class GltfAnimationChannel
        {
            [JsonPropertyName("sampler")] public int Sampler { get; set; }
            [JsonPropertyName("target")] public GltfAnimationChannelTarget Target { get; set; } = null!;
        }

        private sealed class GltfAnimationChannelTarget
        {
            [JsonPropertyName("node")] public int Node { get; set; }
            [JsonPropertyName("path")] public string Path { get; set; } = string.Empty;
        }
    }
}
