using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK.Mathematics;
using Trinity.Core.Flatbuffers.TR.Model;

namespace TrinityModelViewer
{
    internal static class TrmtrJsonConverter
    {
        public static TrmtrJsonDocument ToJsonDocument(TrmtrFile trmtr)
        {
            if (trmtr == null)
            {
                throw new ArgumentNullException(nameof(trmtr));
            }

            var doc = new TrmtrJsonDocument { Version = 1 };
            foreach (var mat in trmtr.Materials ?? Array.Empty<TrmtrFileMaterial>())
            {
                if (mat == null)
                {
                    continue;
                }

                var outItem = new TrmtrJsonMaterialItem
                {
                    Name = mat.Name ?? string.Empty,
                };

                foreach (var shader in mat.Shaders ?? Array.Empty<TrmtrFileShader>())
                {
                    if (shader == null)
                    {
                        continue;
                    }

                    var outTech = new TrmtrJsonTechnique { Name = shader.Name ?? string.Empty };
                    foreach (var p in shader.Values ?? Array.Empty<TrmtrFileStringParameter>())
                    {
                        if (p == null || string.IsNullOrWhiteSpace(p.Name))
                        {
                            continue;
                        }

                        outTech.ShaderOptions.Add(new TrmtrJsonShaderOption
                        {
                            Name = p.Name,
                            Choice = p.Value ?? string.Empty
                        });
                    }
                    outItem.Techniques.Add(outTech);
                }

                foreach (var tex in mat.Textures ?? Array.Empty<TrmtrFileTexture>())
                {
                    if (tex == null || string.IsNullOrWhiteSpace(tex.Name))
                    {
                        continue;
                    }

                    outItem.Textures.Add(new TrmtrJsonTextureParam
                    {
                        Name = tex.Name,
                        FilePath = tex.File ?? string.Empty,
                        SamplerId = (int)tex.Slot
                    });
                }

                foreach (var fp in mat.FloatParameters ?? Array.Empty<TrmtrFileFloatParameter>())
                {
                    if (fp == null || string.IsNullOrWhiteSpace(fp.Name))
                    {
                        continue;
                    }
                    outItem.Floats.Add(new TrmtrJsonFloatParam { Name = fp.Name, Value = fp.Value });
                }

                foreach (var ip in mat.IntParameters ?? Array.Empty<TrmtrFileIntParameter>())
                {
                    if (ip == null || string.IsNullOrWhiteSpace(ip.Name))
                    {
                        continue;
                    }
                    outItem.Ints.Add(new TrmtrJsonIntParam { Name = ip.Name, Value = ip.Value });
                }

                static void AddVec4(List<TrmtrJsonVec4Param> dst, TrmtrFileFloat4Parameter v4)
                {
                    if (v4 == null || string.IsNullOrWhiteSpace(v4.Name) || v4.Value == null)
                    {
                        return;
                    }

                    dst.Add(new TrmtrJsonVec4Param
                    {
                        Name = v4.Name,
                        Value = new TrmtrJsonVec4 { X = v4.Value.R, Y = v4.Value.G, Z = v4.Value.B, W = v4.Value.A }
                    });
                }

                foreach (var v4 in mat.Float4LightParameters ?? Array.Empty<TrmtrFileFloat4Parameter>())
                {
                    AddVec4(outItem.Vec4, v4);
                }

                foreach (var v4 in mat.Float4Parameters ?? Array.Empty<TrmtrFileFloat4Parameter>())
                {
                    AddVec4(outItem.Vec4, v4);
                }

                doc.Items.Add(outItem);
            }

            return doc;
        }

        public static void ApplyToRuntimeModel(
            TrmtrJsonDocument doc,
            GFTool.Renderer.Scene.GraphicsObjects.Model model,
            out int materialsTouched,
            out int overridesApplied)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));
            if (model == null) throw new ArgumentNullException(nameof(model));

            materialsTouched = 0;
            overridesApplied = 0;

            var runtimeByName = model.GetMaterials()
                .Where(m => m != null && !string.IsNullOrWhiteSpace(m.Name))
                .ToDictionary(m => m.Name, StringComparer.OrdinalIgnoreCase);

            foreach (var item in doc.Items ?? new List<TrmtrJsonMaterialItem>())
            {
                if (item == null || string.IsNullOrWhiteSpace(item.Name) || !runtimeByName.TryGetValue(item.Name, out var runtime))
                {
                    continue;
                }

                int appliedThisMat = 0;

                void Set(string name, object value)
                {
                    if (string.IsNullOrWhiteSpace(name))
                    {
                        return;
                    }

                    runtime.SetUniformOverride(name, value);
                    model.TrySetMaterialUniformOverride(runtime.Name, name, value);
                    appliedThisMat++;
                }

                foreach (var t in item.Techniques ?? new List<TrmtrJsonTechnique>())
                {
                    if (t?.ShaderOptions == null)
                    {
                        continue;
                    }
                    foreach (var opt in t.ShaderOptions)
                    {
                        if (opt == null || string.IsNullOrWhiteSpace(opt.Name))
                        {
                            continue;
                        }
                        Set(opt.Name, opt.Choice ?? string.Empty);
                    }
                }

                foreach (var fp in item.Floats ?? new List<TrmtrJsonFloatParam>())
                {
                    if (fp == null || string.IsNullOrWhiteSpace(fp.Name))
                    {
                        continue;
                    }
                    Set(fp.Name, fp.Value);
                }

                foreach (var ip in item.Ints ?? new List<TrmtrJsonIntParam>())
                {
                    if (ip == null || string.IsNullOrWhiteSpace(ip.Name))
                    {
                        continue;
                    }
                    Set(ip.Name, ip.Value);
                }

                foreach (var v2 in item.Vec2 ?? new List<TrmtrJsonVec2Param>())
                {
                    if (v2?.Value == null || string.IsNullOrWhiteSpace(v2.Name))
                    {
                        continue;
                    }
                    Set(v2.Name, new Vector2(v2.Value.X, v2.Value.Y));
                }

                foreach (var v3 in item.Vec3 ?? new List<TrmtrJsonVec3Param>())
                {
                    if (v3?.Value == null || string.IsNullOrWhiteSpace(v3.Name))
                    {
                        continue;
                    }
                    Set(v3.Name, new Vector3(v3.Value.X, v3.Value.Y, v3.Value.Z));
                }

                foreach (var v4 in item.Vec4 ?? new List<TrmtrJsonVec4Param>())
                {
                    if (v4?.Value == null || string.IsNullOrWhiteSpace(v4.Name))
                    {
                        continue;
                    }
                    Set(v4.Name, new Vector4(v4.Value.X, v4.Value.Y, v4.Value.Z, v4.Value.W));
                }

                if (appliedThisMat > 0)
                {
                    materialsTouched++;
                    overridesApplied += appliedThisMat;
                    runtime.RefreshColorTableOverridesFromUniformOverrides();
                }
            }
        }
    }
}
