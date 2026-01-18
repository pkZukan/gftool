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
        private void ParseMaterial(string file)
        {
            ParseMaterial(file, preserveMaterialMetadata: false);
        }

	        private void ParseMaterial(string file, bool preserveMaterialMetadata)
	        {
	            currentMaterialFilePath = file;
	            currentMaterialSetName = null;

		            if (!preserveMaterialMetadata)
		            {
		                defaultMaterialFilePath = file;
		                materialMetadata = null;
		                materialMetadataPath = null;
		                materialMetadataSelections.Clear();
		                materialMetadataValueOverrides.Clear();
		                materialMetadataLastAppliedUniformNames.Clear();
		            }

	            if (materials != null)
	            {
	                foreach (var existing in materials)
                {
                    existing?.Dispose();
                }
            }

		            List<Material> matlist = new List<Material>();
		            var materialPath = new PathString(file);
		            var trmtrBytes = assetProvider.ReadAllBytes(file);
		            var trmtr = FlatBufferConverter.DeserializeFrom<TrmtrFile>(trmtrBytes);
		            TRMTR? legacyTrmtr = null;
		            try
		            {
		                legacyTrmtr = FlatBufferConverter.DeserializeFrom<TRMTR>(trmtrBytes);
		            }
		            catch
		            {
		                legacyTrmtr = null;
		            }
		            var legacySamplersByMaterialName = legacyTrmtr?.Materials?
		                .Where(m => m != null && !string.IsNullOrWhiteSpace(m.Name))
		                .ToDictionary(m => m.Name, m => m.Samplers ?? Array.Empty<TRSampler>(), StringComparer.OrdinalIgnoreCase)
		                ?? new Dictionary<string, TRSampler[]>(StringComparer.OrdinalIgnoreCase);
		            var shaderGame = ResolveEffectiveShaderGame(trmtr, assetProvider);
		            this.shaderGame = shaderGame;

		            if (trmtr?.Materials == null || trmtr.Materials.Length == 0)
		            {
		                throw new InvalidOperationException($"TRMTR has no materials: {file}");
		            }

		            for (int i = 0; i < trmtr.Materials.Length; i++)
		            {
		                var src = trmtr.Materials[i];
		                legacySamplersByMaterialName.TryGetValue(src?.Name ?? string.Empty, out var legacySamplers);
		                if ((legacySamplers == null || legacySamplers.Length == 0) && legacyTrmtr?.Materials != null && i < legacyTrmtr.Materials.Length)
		                {
		                    legacySamplers = legacyTrmtr.Materials[i]?.Samplers;
		                }
		                var trmat = ConvertTrmtrMaterial(src, shaderGame, legacySamplers);
		                matlist.Add(new Material(materialPath, trmat, assetProvider));
		            }
			            materials = matlist.ToArray();
			            BuildMaterialMap();
			            ApplyMaterialMetadataOverridesToRuntimeMaterials();
			            ApplyMaterialUniformOverridesToRuntimeMaterials();
			        }

	        private static TRMaterial ConvertTrmtrMaterial(TrmtrFileMaterial src, ShaderGame game, TRSampler[]? legacySamplers)
	        {
	            var shaderParams = new List<TRStringParameter>();

	            string techniqueName = src?.Shaders?.FirstOrDefault()?.Name ?? "Standard";
	            shaderParams.Add(new TRStringParameter { Name = "__TechniqueName", Value = techniqueName });

	            if (src?.Shaders != null)
	            {
	                foreach (var shader in src.Shaders)
	                {
	                    if (shader?.Values == null) continue;
	                    foreach (var p in shader.Values)
	                    {
	                        if (p == null || string.IsNullOrWhiteSpace(p.Name)) continue;
	                        shaderParams.Add(new TRStringParameter { Name = p.Name, Value = p.Value });
	                    }
	                }
	            }

	            if (src?.IntParameters != null)
	            {
	                foreach (var p in src.IntParameters)
	                {
	                    if (p == null || string.IsNullOrWhiteSpace(p.Name)) continue;
	                    shaderParams.Add(new TRStringParameter { Name = p.Name, Value = p.Value.ToString() });
	                }
	            }

	            string shaderName = MapTechniqueToShaderName(techniqueName, game);

	            var textures = src?.Textures?.Select(t => new TRTexture
	            {
	                Name = t?.Name ?? string.Empty,
	                File = t?.File ?? string.Empty,
	                Slot = t?.Slot ?? 0
	            }).ToArray() ?? Array.Empty<TRTexture>();

	            var samplers = src?.Samplers?.Select(s => new TRSampler
	            {
	                State0 = s?.State0 ?? 0,
	                State1 = s?.State1 ?? 0,
	                State2 = s?.State2 ?? 0,
	                State3 = s?.State3 ?? 0,
	                State4 = s?.State4 ?? 0,
	                State5 = s?.State5 ?? 0,
	                State6 = s?.State6 ?? 0,
	                State7 = s?.State7 ?? 0,
	                State8 = s?.State8 ?? 0,
	                RepeatU = s?.RepeatU ?? UVWrapMode.WRAP,
	                RepeatV = s?.RepeatV ?? UVWrapMode.WRAP,
	                RepeatW = s?.RepeatW ?? UVWrapMode.WRAP,
	                BorderColor = s?.BorderColor ?? new Trinity.Core.Flatbuffers.Utils.RGBA(),
	            }).Select(NormalizeSamplerWrapModes).ToArray() ?? Array.Empty<TRSampler>();
	            if (legacySamplers != null && legacySamplers.Length > 0)
	            {
	                bool shouldPreferLegacy = samplers.Length == 0;
	                if (!shouldPreferLegacy)
	                {
	                    int check = Math.Min(samplers.Length, legacySamplers.Length);
	                    int swapMatches = 0;
	                    int considered = 0;
	                    for (int i = 0; i < check; i++)
	                    {
	                        var a = samplers[i];
	                        var b = legacySamplers[i];
	                        if (a == null || b == null)
	                        {
	                            continue;
	                        }

	                        considered++;
	                        if (a.RepeatV == b.RepeatW && a.RepeatW == b.RepeatV)
	                        {
	                            swapMatches++;
	                        }
	                    }

	                    if (considered > 0 && swapMatches >= Math.Max(1, considered / 2))
	                    {
	                        shouldPreferLegacy = true;
	                    }
	                }

	                if (shouldPreferLegacy)
	                {
	                    // Use legacy samplers if present so wrap/filter state isn't silently lost or mis-decoded
	                    // (defaults to ClampToEdge looks like broken UVs / collapsed previews).
	                    samplers = legacySamplers.Select(NormalizeLegacySampler).Select(NormalizeSamplerWrapModes).ToArray();
	                }
	            }

	            static TRSampler NormalizeLegacySampler(TRSampler srcSampler)
	            {
	                if (srcSampler == null)
	                {
	                    return new TRSampler();
	                }

	                return new TRSampler
	                {
	                    State0 = srcSampler.State0,
	                    State1 = srcSampler.State1,
	                    State2 = srcSampler.State2,
	                    State3 = srcSampler.State3,
	                    State4 = srcSampler.State4,
	                    State5 = srcSampler.State5,
	                    State6 = srcSampler.State6,
	                    State7 = srcSampler.State7,
	                    State8 = srcSampler.State8,
	                    RepeatU = srcSampler.RepeatU,
	                    RepeatV = srcSampler.RepeatV,
	                    RepeatW = srcSampler.RepeatW,
	                    BorderColor = srcSampler.BorderColor ?? new Trinity.Core.Flatbuffers.Utils.RGBA(),
	                };
	            }

	            static TRSampler NormalizeSamplerWrapModes(TRSampler sampler)
	            {
	                if (sampler == null)
	                {
	                    return new TRSampler();
	                }

	                var repeatU = sampler.RepeatU;
	                var repeatV = sampler.RepeatV;
	                var repeatW = sampler.RepeatW;

	                // Heuristic: some TRMTR variants appear to deserialize sampler wrap modes with RepeatV/RepeatW swapped.
	                // Symptom: V is always CLAMP because W is commonly CLAMP, while W contains the real V mode (WRAP/MIRROR).
	                if (repeatV == UVWrapMode.CLAMP &&
	                    repeatW != UVWrapMode.CLAMP &&
	                    (repeatW == UVWrapMode.WRAP || repeatW == UVWrapMode.MIRROR || repeatW == UVWrapMode.MIRROR_ONCE))
	                {
	                    (repeatV, repeatW) = (repeatW, repeatV);
	                }

	                // Heuristic: some assets appear to clamp V unexpectedly while still mirroring/repeating U.
	                // This produces the "UV collapsed to an edge" look in previews and breaks layered masks.
	                // If U is explicitly mirrored and both V/W are clamp, assume V should also wrap.
	                if ((repeatU == UVWrapMode.MIRROR || repeatU == UVWrapMode.MIRROR_ONCE) &&
	                    repeatV == UVWrapMode.CLAMP &&
	                    repeatW == UVWrapMode.CLAMP)
	                {
	                    repeatV = UVWrapMode.WRAP;
	                }

	                sampler.RepeatU = repeatU;
	                sampler.RepeatV = repeatV;
	                sampler.RepeatW = repeatW;
	                return sampler;
	            }

	            var floatParams = src?.FloatParameters?.Select(p => new TRFloatParameter
	            {
	                Name = p?.Name ?? string.Empty,
	                Value = p?.Value ?? 0.0f
	            }).ToArray() ?? Array.Empty<TRFloatParameter>();

	            static IEnumerable<TRVec4fParameter> ConvertFloat4Params(TrmtrFileFloat4Parameter[]? srcParams)
	            {
	                if (srcParams == null) yield break;
	                foreach (var p in srcParams)
	                {
	                    if (p == null || string.IsNullOrWhiteSpace(p.Name) || p.Value == null) continue;
	                    yield return new TRVec4fParameter
	                    {
	                        Name = p.Name,
	                        Value = new Trinity.Core.Flatbuffers.Utils.Vector4f
	                        {
	                            W = p.Value.R,
	                            X = p.Value.G,
	                            Y = p.Value.B,
	                            Z = p.Value.A
	                        }
	                    };
	                }
	            }

	            var vec4 = ConvertFloat4Params(src?.Float4Parameters);
	            var vec4Light = ConvertFloat4Params(src?.Float4LightParameters);
	            var vec4Params = vec4.Concat(vec4Light)
	                .GroupBy(v => v.Name, StringComparer.OrdinalIgnoreCase)
	                .Select(g => g.First())
	                .OrderBy(v => v.Name, StringComparer.OrdinalIgnoreCase)
	                .ToArray();

	            return new TRMaterial
	            {
	                Name = src?.Name ?? "Material",
	                Shader = new[] { new TRMaterialShader { Name = shaderName, Values = shaderParams.ToArray() } },
	                Textures = textures,
	                Samplers = samplers,
	                FloatParams = floatParams,
	                Vec2fParams = Array.Empty<TRVec2fParameter>(),
	                Vec3fParams = Array.Empty<TRVec3fParameter>(),
	                Vec4fParams = vec4Params,
	            };
	        }

        public IReadOnlyList<Material> GetMaterials()
        {
            return materials ?? Array.Empty<Material>();
        }

        public void ReplaceMaterials(IReadOnlyList<Material> newMaterials)
        {
            if (newMaterials == null)
            {
                return;
            }

            materials = newMaterials.Where(m => m != null).ToArray();
            BuildMaterialMap();
        }

        public Armature? GetArmature()
        {
            return armature;
        }

        public IReadOnlyList<string> GetSubmeshNames()
        {
            return SubmeshNames;
        }

        public IReadOnlyList<string> GetSubmeshMaterials()
        {
            return MaterialNames;
        }

        public void SetSubmeshMaterialName(int submeshIndex, string materialName)
        {
            if (submeshIndex < 0)
            {
                return;
            }

            if (submeshIndex >= MaterialNames.Count)
            {
                return;
            }

            MaterialNames[submeshIndex] = materialName ?? string.Empty;
        }

        public IReadOnlyList<UvSet> GetUvSetsForMaterial(string materialName)
        {
            return GetUvSetsForMaterial(materialName, 0);
        }

        public IReadOnlyList<UvSet> GetUvSetsForMaterial(string materialName, int uvIndex)
        {
            var result = new List<UvSet>();
            if (string.IsNullOrWhiteSpace(materialName))
            {
                return result;
            }

            IReadOnlyList<Vector2[]> uvsSource = uvIndex == 1 ? UVs2 : UVs;

            var count = Math.Min(MaterialNames.Count, Math.Min(uvsSource.Count, Indices.Count));
            for (int i = 0; i < count; i++)
            {
                if (MatchesMaterial(MaterialNames[i], materialName))
                {
                    var submeshName = i < SubmeshNames.Count ? SubmeshNames[i] : $"Submesh {i}";
                    result.Add(new UvSet(uvsSource[i], Indices[i], submeshName));
                }
            }

            return result;
        }

        private static bool MatchesMaterial(string name, string target)
        {
            if (string.Equals(name, target, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(target))
            {
                return false;
            }

            if (name.Contains(':'))
            {
                name = name.Split(':')[0];
            }

            if (target.Contains(':'))
            {
                target = target.Split(':')[0];
            }

            return name.StartsWith(target, StringComparison.OrdinalIgnoreCase) ||
                   target.StartsWith(name, StringComparison.OrdinalIgnoreCase);
        }

        private void BuildMaterialMap()
        {
            materialMap.Clear();
            if (materials == null) return;
            foreach (var mat in materials)
            {
                if (mat == null || string.IsNullOrEmpty(mat.Name)) continue;
                if (!materialMap.ContainsKey(mat.Name))
                {
                    materialMap.Add(mat.Name, mat);
                }

                // Some DCC tools append numeric suffixes like ".001" when duplicating materials.
                // Add an alias without the suffix so imported meshes can still resolve materials
                // without requiring the user to manually rename them back.
                var baseName = StripDccNumericSuffix(mat.Name);
                if (!string.IsNullOrWhiteSpace(baseName) && !materialMap.ContainsKey(baseName))
                {
                    materialMap.Add(baseName, mat);
                }
            }
        }

        private static string StripDccNumericSuffix(string name)
        {
            if (string.IsNullOrWhiteSpace(name) || name.Length < 5)
            {
                return name;
            }

            // Matches ".000" .. ".999" suffixes.
            int dot = name.LastIndexOf('.');
            if (dot < 0 || dot >= name.Length - 4)
            {
                return name;
            }

            if (name.Length - dot != 4)
            {
                return name;
            }

            char c1 = name[dot + 1];
            char c2 = name[dot + 2];
            char c3 = name[dot + 3];
            if (!char.IsDigit(c1) || !char.IsDigit(c2) || !char.IsDigit(c3))
            {
                return name;
            }

            return name.Substring(0, dot);
        }
	    }
}
