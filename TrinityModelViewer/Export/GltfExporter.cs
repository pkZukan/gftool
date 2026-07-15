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
using Trinity.Core.Flatbuffers.TR.Model;

namespace TrinityModelViewer.Export
{
    internal static class GltfExporter
    {
        private const int GlLinear = 9729;
        private const int GlRepeat = 10497;

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
            DiagnosticLog.Write($"Gltf export begin: model={data.Name}, out={gltfPath}, submeshes={data.Submeshes.Count}, materials={data.Materials.Count}, armatureBones={data.Armature?.Bones.Count ?? 0}, animations={animations?.Count ?? 0}");

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
                MagFilter = GlLinear,
                MinFilter = GlLinear,
                WrapS = GlRepeat,
                WrapT = GlRepeat
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
            var textureExport = ExportAllTextures(gltf, texDir, data.Materials);
            var texCache = textureExport.Cache;
            var textureManifestPath = WriteTextureManifest(gltfPath, data.Name, textureExport.Records);
            DiagnosticLog.Write($"Gltf texture export complete: referencedTextureCount={textureExport.Records.Count}, exportedTextureCount={texCache.Count}, texDir={texDir}, manifest={textureManifestPath}");

            foreach (var sub in data.Submeshes)
            {
                if (sub.Positions.Length == 0 || sub.Indices.Length == 0) continue;

                int materialIndex = GetOrCreateMaterial(gltf, gltfMaterialIndex, materialByName, texCache, sub.MaterialName, texDir);
                DiagnosticLog.Write($"Gltf submesh export: name={sub.Name}, material={sub.MaterialName}, vertices={sub.Positions.Length}, indices={sub.Indices.Length}, hasUVs={sub.UVs.Length == sub.Positions.Length}, uvSets={sub.UVSets.Count}, hasSkinning={sub.HasSkinning}, materialIndex={materialIndex}");
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
            WriteBlenderMaterialScript(gltfPath);
            DiagnosticLog.Write($"Gltf export written: gltf={gltfPath}, bin={binPath}, binBytes={binBytes.Length}, nodes={gltf.Nodes.Count}, meshes={gltf.Meshes.Count}, materials={gltf.Materials.Count}, images={gltf.Images.Count}, animations={gltf.Animations.Count}");
        }

        public static string GetBlenderMaterialScriptPath(string gltfPath)
        {
            var outDir = Path.GetDirectoryName(gltfPath) ?? Environment.CurrentDirectory;
            var baseName = Path.GetFileNameWithoutExtension(gltfPath);
            return Path.Combine(outDir, $"{baseName}_trinity_blender.py");
        }

        public static string GetTextureManifestPath(string gltfPath)
        {
            var outDir = Path.GetDirectoryName(gltfPath) ?? Environment.CurrentDirectory;
            var baseName = Path.GetFileNameWithoutExtension(gltfPath);
            return Path.Combine(outDir, $"{baseName}_texture_manifest.json");
        }

        private static void WriteBlenderMaterialScript(string gltfPath)
        {
            var scriptPath = GetBlenderMaterialScriptPath(gltfPath);
            var script = BuildBlenderMaterialScript(gltfPath);
            File.WriteAllText(scriptPath, script);
            DiagnosticLog.Write($"Blender material helper written: {scriptPath}");
        }

        private static string BuildBlenderMaterialScript(string gltfPath)
        {
            string gltfPathJson = JsonSerializer.Serialize(Path.GetFullPath(gltfPath));
            string script = @"
import bpy
import json
import os

# Generated by TrinityModelViewer.
# 1. Import the exported .gltf in Blender.
# 2. Run this script in Blender's Text Editor.
# Exported TEXCOORD_0 already matches ModelViewer's viewer UVs.
# Set USE_RAW_GAME_UV to True only when you intentionally want to inspect raw game UVs.
GLTF_PATH = __GLTF_PATH_JSON__
USE_RAW_GAME_UV = False

NON_COLOR_TEXTURES = (
    'normal', 'roughness', 'metallic', 'ao', 'mask', 'layer', 'sss',
    'detail', 'flow'
)

def get_script_dir():
    try:
        return os.path.dirname(os.path.abspath(__file__))
    except Exception:
        if bpy.data.filepath:
            return os.path.dirname(os.path.abspath(bpy.data.filepath))
        return os.getcwd()

def resolve_gltf_path():
    if os.path.exists(GLTF_PATH):
        return GLTF_PATH
    candidate = os.path.join(get_script_dir(), os.path.basename(GLTF_PATH))
    return candidate

def resolve_uri(base_dir, uri):
    if not uri:
        return None
    if uri.startswith('data:'):
        return None
    path = uri.replace('/', os.sep)
    if os.path.isabs(path):
        return path
    return os.path.normpath(os.path.join(base_dir, path))

def normalized_material_name(name):
    if not name:
        return ''
    if len(name) > 4 and name[-4] == '.' and name[-3:].isdigit():
        return name[:-4]
    return name

def blender_materials_for(gltf_name):
    wanted = normalized_material_name(gltf_name)
    return [
        mat for mat in bpy.data.materials
        if normalized_material_name(mat.name) == wanted
    ]

def find_principled(mat):
    nodes = mat.node_tree.nodes
    for node in nodes:
        if node.type == 'BSDF_PRINCIPLED':
            return node
    node = nodes.new('ShaderNodeBsdfPrincipled')
    node.location = (360, 0)
    return node

def find_output(mat):
    for node in mat.node_tree.nodes:
        if node.type == 'OUTPUT_MATERIAL':
            return node
    node = mat.node_tree.nodes.new('ShaderNodeOutputMaterial')
    node.location = (640, 0)
    return node

def input_by_names(node, names):
    for name in names:
        if name in node.inputs:
            return node.inputs[name]
    return None

def output_by_names(node, names):
    for name in names:
        if name in node.outputs:
            return node.outputs[name]
    return None

def link_replace(tree, output_socket, input_socket):
    if not output_socket or not input_socket:
        return
    for link in list(input_socket.links):
        tree.links.remove(link)
    tree.links.new(output_socket, input_socket)

def set_non_color(image):
    try:
        image.colorspace_settings.name = 'Non-Color'
    except Exception:
        pass

def load_image(path, semantic):
    if not path or not os.path.exists(path):
        print('Missing texture:', semantic, path)
        return None
    image = bpy.data.images.load(path, check_existing=True)
    lower = semantic.lower()
    if any(token in lower for token in NON_COLOR_TEXTURES) and 'basecolor' not in lower:
        set_non_color(image)
    return image

def make_texture_node(mat, texture, image, index):
    nodes = mat.node_tree.nodes
    semantic = texture.get('name') or 'Texture'
    node = nodes.new('ShaderNodeTexImage')
    node.name = 'TRINITY_' + semantic
    node.label = semantic + ' | slot ' + str(texture.get('slot', '?'))
    node.image = image
    node.location = (-850, 260 - index * 220)
    return node

def make_separate_red(tree, tex_node, x, y):
    nodes = tree.nodes
    try:
        sep = nodes.new('ShaderNodeSeparateColor')
        red = output_by_names(sep, ('Red', 'R'))
    except Exception:
        sep = nodes.new('ShaderNodeSeparateRGB')
        red = output_by_names(sep, ('R', 'Red'))
    sep.location = (x, y)
    link_replace(tree, output_by_names(tex_node, ('Color',)), input_by_names(sep, ('Color', 'Image')))
    return sep, red

def set_material_uv_flip_once():
    if not USE_RAW_GAME_UV:
        return
    for obj in bpy.context.scene.objects:
        if obj.type != 'MESH':
            continue
        mesh = obj.data
        if mesh.get('trinity_uv_raw_game'):
            continue
        for layer in mesh.uv_layers:
            for data in layer.data:
                data.uv.y = 1.0 - data.uv.y
        mesh['trinity_uv_raw_game'] = True

def apply_material(mat, material_json, base_dir):
    extras = material_json.get('extras') or {}
    textures = extras.get('trinityTextures') or []
    if not textures:
        return

    mat.use_nodes = True
    tree = mat.node_tree
    principled = find_principled(mat)
    output = find_output(mat)
    link_replace(tree, output_by_names(principled, ('BSDF',)), input_by_names(output, ('Surface',)))

    created = {}
    for i, tex in enumerate(textures):
        semantic = tex.get('name') or ''
        path = resolve_uri(base_dir, tex.get('uri'))
        image = load_image(path, semantic)
        if not image:
            continue
        node = make_texture_node(mat, tex, image, i)
        created[semantic.lower()] = node

    base = created.get('basecolormap')
    if base:
        link_replace(tree, output_by_names(base, ('Color',)), input_by_names(principled, ('Base Color',)))
        alpha_input = input_by_names(principled, ('Alpha',))
        alpha_output = output_by_names(base, ('Alpha',))
        if alpha_input and alpha_output:
            link_replace(tree, alpha_output, alpha_input)
            mat.blend_method = 'BLEND'
            mat.use_screen_refraction = True

    normal = created.get('normalmap')
    if normal:
        normal_node = tree.nodes.new('ShaderNodeNormalMap')
        normal_node.location = (-310, -260)
        if 'Strength' in normal_node.inputs:
            normal_node.inputs['Strength'].default_value = 1.0
        link_replace(tree, output_by_names(normal, ('Color',)), input_by_names(normal_node, ('Color',)))
        link_replace(tree, output_by_names(normal_node, ('Normal',)), input_by_names(principled, ('Normal',)))

    rough = created.get('roughnessmap')
    if rough:
        _, red = make_separate_red(tree, rough, -560, -20)
        link_replace(tree, red, input_by_names(principled, ('Roughness',)))

    metal = created.get('metallicmap')
    if metal:
        _, red = make_separate_red(tree, metal, -560, -160)
        link_replace(tree, red, input_by_names(principled, ('Metallic',)))

    sss = created.get('sssmaskmap')
    if sss:
        _, red = make_separate_red(tree, sss, -560, -420)
        target = input_by_names(principled, ('Subsurface Weight', 'Subsurface'))
        link_replace(tree, red, target)

    mat['trinity_shader'] = extras.get('trinityShader', '')
    mat['trinity_textures'] = json.dumps(textures, ensure_ascii=False)

def main():
    gltf_path = resolve_gltf_path()
    if not os.path.exists(gltf_path):
        raise FileNotFoundError(gltf_path)
    base_dir = os.path.dirname(gltf_path)
    with open(gltf_path, 'r', encoding='utf-8') as f:
        gltf = json.load(f)

    set_material_uv_flip_once()

    count = 0
    for material_json in gltf.get('materials', []):
        name = material_json.get('name') or ''
        for mat in blender_materials_for(name):
            apply_material(mat, material_json, base_dir)
            count += 1
    print('Trinity material helper applied to', count, 'Blender materials')

main()
";
            return script.Replace("__GLTF_PATH_JSON__", gltfPathJson).TrimStart();
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
            DiagnosticLog.Write($"Gltf animation export begin: name={animation.Name}, out={gltfPath}, bones={boneCount}, frames={frameCount}, fps={fps}, tracks={animation.TrackCount}");

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
            DiagnosticLog.Write($"Gltf animation export written: gltf={gltfPath}, bin={binPath}, binBytes={binBytes.Length}, nodes={gltf.Nodes.Count}, animations={gltf.Animations.Count}");
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

            var rootChildren = gltf.Nodes[rootNodeIndex].Children ??= new List<int>();
            rootChildren.Add(nodeIndex);
        }

        private static string WriteTextureManifest(string gltfPath, string modelName, IReadOnlyList<GltfTextureExportRecord> records)
        {
            var manifestPath = GetTextureManifestPath(gltfPath);
            var manifest = new GltfTextureManifest
            {
                Model = modelName,
                TextureCount = records.Count,
                ExportedCount = records.Count(r => string.Equals(r.Status, "Exported", StringComparison.OrdinalIgnoreCase) ||
                                                   string.Equals(r.Status, "Shared", StringComparison.OrdinalIgnoreCase)),
                Records = records.ToList()
            };

            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
            File.WriteAllText(manifestPath, JsonSerializer.Serialize(manifest, jsonOptions));
            return manifestPath;
        }

        private static TextureExportResult ExportAllTextures(GltfRoot gltf, string texDir, IReadOnlyList<Material> materials)
        {
            var cache = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var records = new List<GltfTextureExportRecord>();
            var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var mat in materials)
            {
                if (mat == null) continue;
                foreach (var tex in mat.Textures)
                {
                    if (tex == null) continue;
                    var key = GetTextureKey(tex);
                    string? sourcePath = null;
                    bool sourceExists = tex.TryGetResolvedSourcePath(out var resolvedPath) && File.Exists(resolvedPath);
                    if (sourceExists)
                    {
                        sourcePath = resolvedPath;
                    }

                    if (cache.TryGetValue(key, out int sharedTextureIndex))
                    {
                        records.Add(new GltfTextureExportRecord
                        {
                            Material = mat.Name,
                            Shader = mat.ShaderName,
                            Name = tex.Name,
                            SourceFile = tex.SourceFile,
                            Slot = tex.Slot,
                            SourcePath = sourcePath,
                            SourceExists = sourceExists,
                            Status = "Shared",
                            Texture = sharedTextureIndex,
                            Uri = TryGetTextureUri(gltf, sharedTextureIndex)
                        });
                        continue;
                    }

                    using var bmp = tex.LoadPreviewBitmap();
                    if (bmp == null)
                    {
                        DiagnosticLog.Write($"Gltf texture skipped: name={tex.Name}, source={tex.SourceFile}, reason=decode returned null");
                        records.Add(new GltfTextureExportRecord
                        {
                            Material = mat.Name,
                            Shader = mat.ShaderName,
                            Name = tex.Name,
                            SourceFile = tex.SourceFile,
                            Slot = tex.Slot,
                            SourcePath = sourcePath,
                            SourceExists = sourceExists,
                            Status = sourceExists ? "Decode failed" : "Missing"
                        });
                        continue;
                    }

                    string outName = MakeUniqueTextureFileName(usedNames, tex);
                    string outPath = Path.Combine(texDir, outName);
                    bmp.Save(outPath, ImageFormat.Png);
                    DiagnosticLog.Write($"Gltf texture exported: name={tex.Name}, source={tex.SourceFile}, out={outPath}, size={bmp.Width}x{bmp.Height}, pixelFormat={bmp.PixelFormat}");

                    int imgIndex = gltf.Images.Count;
                    gltf.Images.Add(new GltfImage { Uri = $"{Path.GetFileName(texDir)}/{outName}" });
                    int texIndex = gltf.Textures.Count;
                    gltf.Textures.Add(new GltfTexture { Sampler = 0, Source = imgIndex, Name = tex.Name });

                    cache[key] = texIndex;
                    records.Add(new GltfTextureExportRecord
                    {
                        Material = mat.Name,
                        Shader = mat.ShaderName,
                        Name = tex.Name,
                        SourceFile = tex.SourceFile,
                        Slot = tex.Slot,
                        SourcePath = sourcePath,
                        SourceExists = sourceExists,
                        Status = "Exported",
                        Width = bmp.Width,
                        Height = bmp.Height,
                        Texture = texIndex,
                        Uri = $"{Path.GetFileName(texDir)}/{outName}"
                    });
                }
            }

            return new TextureExportResult(cache, records);
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
            var uvSets = BuildUvSetsForExport(sub, vertexCount);
            DiagnosticLog.Write($"Gltf mesh UV export: submesh={sub.Name}, inputUVs={sub.UVs.Length}, uvSets={uvSets.Count}, vertices={vertexCount}, defaultMatchesViewerUv=true, includesRawHelper={uvSets.Any(set => !set.IsVFlipped)}");
            var tangents = sub.Tangents.Length == vertexCount ? sub.Tangents : Array.Empty<Vector4>();
            var joints = sub.BlendIndices.Length == vertexCount ? sub.BlendIndices : Enumerable.Repeat(Vector4.Zero, vertexCount).ToArray();
            var weights = sub.BlendWeights.Length == vertexCount ? sub.BlendWeights : Enumerable.Repeat(new Vector4(1, 0, 0, 0), vertexCount).ToArray();

            int posAcc = AddAccessorVec3(gltf, buffer, positions, target: 34962, includeMinMax: true);
            int nrmAcc = AddAccessorVec3(gltf, buffer, normals, target: 34962);
            var uvAccessors = uvSets
                .Select(uvs => AddAccessorVec2(gltf, buffer, uvs.Values, target: 34962))
                .ToArray();

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
                    ["NORMAL"] = nrmAcc
                },
                Indices = idxAcc,
                Material = materialIndex,
                Extras = new GltfPrimitiveExtras
                {
                    TrinityUvSets = uvSets
                        .Select((set, index) => new GltfTrinityUvSet
                        {
                            Attribute = $"TEXCOORD_{index}",
                            Name = set.Name,
                            SourceSet = set.SourceSet,
                            IsVFlipped = set.IsVFlipped
                        })
                        .ToList()
                }
            };

            for (int i = 0; i < uvAccessors.Length; i++)
            {
                prim.Attributes[$"TEXCOORD_{i}"] = uvAccessors[i];
            }

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

        private sealed class ExportUvSet
        {
            public ExportUvSet(string name, Vector2[] values, int sourceSet, bool isVFlipped)
            {
                Name = name;
                Values = values;
                SourceSet = sourceSet;
                IsVFlipped = isVFlipped;
            }

            public string Name { get; }
            public Vector2[] Values { get; }
            public int SourceSet { get; }
            public bool IsVFlipped { get; }
        }

        private static List<ExportUvSet> BuildUvSetsForExport(Model.ExportSubmesh sub, int vertexCount)
        {
            var result = new List<ExportUvSet>();
            var sourceSets = new List<Vector2[]>();

            if (sub.UVSets != null)
            {
                foreach (var set in sub.UVSets)
                {
                    if (set != null && set.Length == vertexCount)
                    {
                        sourceSets.Add(set.ToArray());
                    }
                }
            }

            if (sourceSets.Count == 0 && sub.UVs.Length == vertexCount)
            {
                sourceSets.Add(sub.UVs.ToArray());
            }

            if (sourceSets.Count == 0)
            {
                sourceSets.Add(Enumerable.Repeat(Vector2.Zero, vertexCount).ToArray());
            }

            // TEXCOORD_0 should match ModelViewer's shader sampling (u, 1-v).
            // Raw game UVs are kept as extra attributes for debugging.
            for (int i = 0; i < sourceSets.Count; i++)
            {
                result.Add(new ExportUvSet($"TRINITY_UV_{i}_VIEWER", FlipUvV(sourceSets[i]), i, isVFlipped: true));
            }

            for (int i = 0; i < sourceSets.Count; i++)
            {
                result.Add(new ExportUvSet($"TRINITY_UV_{i}_RAW", sourceSets[i], i, isVFlipped: false));
            }

            return result;
        }

        private static Vector2[] FlipUvV(Vector2[] source)
        {
            var flipped = new Vector2[source.Length];
            for (int i = 0; i < source.Length; i++)
            {
                flipped[i] = new Vector2(source[i].X, 1f - source[i].Y);
            }
            return flipped;
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
                DoubleSided = true,
                Extras = BuildMaterialExtras(gltf, mat, textureCache)
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
            DiagnosticLog.Write($"Gltf material export: name={gltfMat.Name}, shader={mat?.ShaderName ?? "<none>"}, pbrBaseColor={baseColorTex?.ToString() ?? "<none>"}, normal={normalTex?.ToString() ?? "<none>"}, ao={aoTex?.ToString() ?? "<none>"}, extraTextures={gltfMat.Extras?.TrinityTextures.Count ?? 0}");
            gltfMaterialIndex[materialName] = gltfIndex;
            return gltfIndex;
        }

        private static GltfMaterialExtras? BuildMaterialExtras(GltfRoot gltf, Material? mat, Dictionary<string, int> textureCache)
        {
            if (mat == null)
            {
                return null;
            }

            var extras = new GltfMaterialExtras
            {
                TrinityShader = mat.ShaderName,
                ShaderOptions = ToDictionary(mat.ShaderParameters),
                FloatParameters = ToDictionary(mat.FloatParameters, p => p.Value),
                Vec2Parameters = ToDictionary(mat.Vec2Parameters, p => new[] { p.Value.X, p.Value.Y }),
                Vec3Parameters = ToDictionary(mat.Vec3Parameters, p => new[] { p.Value.X, p.Value.Y, p.Value.Z }),
                Vec4Parameters = ToDictionary(mat.Vec4Parameters, p => new[] { p.Value.X, p.Value.Y, p.Value.Z, p.Value.W })
            };

            foreach (var tex in mat.Textures)
            {
                if (tex == null) continue;
                int? textureIndex = null;
                string? uri = null;
                if (textureCache.TryGetValue(GetTextureKey(tex), out int cachedIndex))
                {
                    textureIndex = cachedIndex;
                    uri = TryGetTextureUri(gltf, cachedIndex);
                }

                var item = new GltfTrinityTexture
                {
                    Name = tex.Name,
                    SourceFile = tex.SourceFile,
                    Slot = tex.Slot,
                    Texture = textureIndex,
                    Uri = uri
                };

                if (TryGetSampler(mat, tex.Slot, out var sampler))
                {
                    item.Sampler = new GltfTrinitySampler
                    {
                        RepeatU = sampler.RepeatU.ToString(),
                        RepeatV = sampler.RepeatV.ToString(),
                        RepeatW = sampler.RepeatW.ToString()
                    };
                }

                extras.TrinityTextures.Add(item);
            }

            return extras.TrinityTextures.Count > 0 || !string.IsNullOrWhiteSpace(extras.TrinityShader)
                ? extras
                : null;
        }

        private static Dictionary<string, string> ToDictionary(IReadOnlyList<(string Name, string Value)> values)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (values == null) return result;
            foreach (var value in values)
            {
                if (!string.IsNullOrWhiteSpace(value.Name))
                {
                    result[value.Name] = value.Value ?? string.Empty;
                }
            }
            return result;
        }

        private static Dictionary<string, T> ToDictionary<TParam, T>(IReadOnlyList<TParam> values, Func<TParam, T> getValue)
            where TParam : class
        {
            var result = new Dictionary<string, T>(StringComparer.OrdinalIgnoreCase);
            if (values == null) return result;
            foreach (var value in values)
            {
                if (value == null) continue;
                var nameProperty = value.GetType().GetProperty("Name");
                var name = nameProperty?.GetValue(value) as string;
                if (!string.IsNullOrWhiteSpace(name))
                {
                    result[name] = getValue(value);
                }
            }
            return result;
        }

        private static string? TryGetTextureUri(GltfRoot gltf, int textureIndex)
        {
            if (textureIndex < 0 || textureIndex >= gltf.Textures.Count)
            {
                return null;
            }

            int imageIndex = gltf.Textures[textureIndex].Source;
            if (imageIndex < 0 || imageIndex >= gltf.Images.Count)
            {
                return null;
            }

            return gltf.Images[imageIndex].Uri;
        }

        private static bool TryGetSampler(Material mat, uint slot, out TRSampler sampler)
        {
            var samplers = mat.Samplers;
            int index = (int)slot;
            if (samplers != null && index >= 0 && index < samplers.Count && samplers[index] != null)
            {
                sampler = samplers[index];
                return true;
            }

            sampler = null!;
            return false;
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

        private sealed class TextureExportResult
        {
            public TextureExportResult(Dictionary<string, int> cache, List<GltfTextureExportRecord> records)
            {
                Cache = cache;
                Records = records;
            }

            public Dictionary<string, int> Cache { get; }
            public List<GltfTextureExportRecord> Records { get; }
        }

        private sealed class GltfTextureManifest
        {
            [JsonPropertyName("model")] public string Model { get; set; } = string.Empty;
            [JsonPropertyName("textureCount")] public int TextureCount { get; set; }
            [JsonPropertyName("exportedCount")] public int ExportedCount { get; set; }
            [JsonPropertyName("records")] public List<GltfTextureExportRecord> Records { get; set; } = new List<GltfTextureExportRecord>();
        }

        private sealed class GltfTextureExportRecord
        {
            [JsonPropertyName("material")] public string? Material { get; set; }
            [JsonPropertyName("shader")] public string? Shader { get; set; }
            [JsonPropertyName("name")] public string? Name { get; set; }
            [JsonPropertyName("sourceFile")] public string? SourceFile { get; set; }
            [JsonPropertyName("slot")] public uint Slot { get; set; }
            [JsonPropertyName("sourcePath")] public string? SourcePath { get; set; }
            [JsonPropertyName("sourceExists")] public bool SourceExists { get; set; }
            [JsonPropertyName("status")] public string? Status { get; set; }
            [JsonPropertyName("width")] public int? Width { get; set; }
            [JsonPropertyName("height")] public int? Height { get; set; }
            [JsonPropertyName("texture")] public int? Texture { get; set; }
            [JsonPropertyName("uri")] public string? Uri { get; set; }
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
            [JsonPropertyName("extras")] public GltfPrimitiveExtras? Extras { get; set; }
        }

        private sealed class GltfPrimitiveExtras
        {
            [JsonPropertyName("trinityUvSets")] public List<GltfTrinityUvSet> TrinityUvSets { get; set; } = new List<GltfTrinityUvSet>();
        }

        private sealed class GltfTrinityUvSet
        {
            [JsonPropertyName("attribute")] public string Attribute { get; set; } = string.Empty;
            [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
            [JsonPropertyName("sourceSet")] public int SourceSet { get; set; }
            [JsonPropertyName("isVFlipped")] public bool IsVFlipped { get; set; }
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
            [JsonPropertyName("extras")] public GltfMaterialExtras? Extras { get; set; }
        }

        private sealed class GltfMaterialExtras
        {
            [JsonPropertyName("trinityShader")] public string? TrinityShader { get; set; }
            [JsonPropertyName("trinityTextures")] public List<GltfTrinityTexture> TrinityTextures { get; set; } = new List<GltfTrinityTexture>();
            [JsonPropertyName("shaderOptions")] public Dictionary<string, string>? ShaderOptions { get; set; }
            [JsonPropertyName("floatParameters")] public Dictionary<string, float>? FloatParameters { get; set; }
            [JsonPropertyName("vec2Parameters")] public Dictionary<string, float[]>? Vec2Parameters { get; set; }
            [JsonPropertyName("vec3Parameters")] public Dictionary<string, float[]>? Vec3Parameters { get; set; }
            [JsonPropertyName("vec4Parameters")] public Dictionary<string, float[]>? Vec4Parameters { get; set; }
        }

        private sealed class GltfTrinityTexture
        {
            [JsonPropertyName("name")] public string? Name { get; set; }
            [JsonPropertyName("sourceFile")] public string? SourceFile { get; set; }
            [JsonPropertyName("slot")] public uint Slot { get; set; }
            [JsonPropertyName("texture")] public int? Texture { get; set; }
            [JsonPropertyName("uri")] public string? Uri { get; set; }
            [JsonPropertyName("sampler")] public GltfTrinitySampler? Sampler { get; set; }
        }

        private sealed class GltfTrinitySampler
        {
            [JsonPropertyName("repeatU")] public string? RepeatU { get; set; }
            [JsonPropertyName("repeatV")] public string? RepeatV { get; set; }
            [JsonPropertyName("repeatW")] public string? RepeatW { get; set; }
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
