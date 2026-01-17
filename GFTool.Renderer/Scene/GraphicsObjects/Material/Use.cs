using GFTool.Core.Utils;
using GFTool.Renderer.Core;
using GFTool.Renderer.Core.Graphics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Globalization;
using System.Drawing;
using System.Linq;
using Trinity.Core.Flatbuffers.TR.Model;
using Trinity.Core.Flatbuffers.Utils;
using Trinity.Core.Assets;

namespace GFTool.Renderer.Scene.GraphicsObjects
{
    public partial class Material : IDisposable
    {
        public void Use(
            Matrix4 view,
            Matrix4 model,
            Matrix4 proj,
            bool hasVertexColors,
            bool hasTangents,
            bool hasBinormals,
            bool hasUv1,
            UvSetOverride layerMaskUvOverride = UvSetOverride.Material,
            UvSetOverride aoUvOverride = UvSetOverride.Material)
        {
            var activeShader = GetActiveShader();
            if (activeShader == null) return;

            PerfCounters.RecordMaterialUse();

            activeShader.Bind();
            activeShader.SetBoolIfExists("TransparentPass", RenderOptions.TransparentPass);
            ApplyTransparentBlendState();
            ResetCommonUniformDefaults(activeShader);
            var usedSlots = new HashSet<int>();
            var textureNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int nextSlot = 0;
            bool baseColorMapIsPlaceholder = false;
            for (int i = 0; i < textures.Count; i++)
            {
                textures[i].EnsureLoaded();
                if (RenderOptions.EnableAsyncResourceLoading)
                {
                    // When models/materials are swapped after the initial async load (e.g. changing TRMMT sets),
                    // textures can end up decoded on a worker thread but never uploaded since the async loader
                    // work item isn't running. Opportunistically upload here while we are on the GL thread.
                    if (!textures[i].IsAsyncLoadComplete)
                    {
                        textures[i].TryUploadDecodedOnGlThread();
                    }
                }
                textureNames.Add(textures[i].Name);
                if (!baseColorMapIsPlaceholder &&
                    string.Equals(textures[i].Name, "BaseColorMap", StringComparison.OrdinalIgnoreCase) &&
                    IsPlaceholderMaskTexturePath(textures[i].SourceFile))
                {
                    baseColorMapIsPlaceholder = true;
                }
                int slot = (int)textures[i].Slot;
                if (slot < 0 || slot > 31 || usedSlots.Contains(slot))
                {
                    while (usedSlots.Contains(nextSlot) && nextSlot < 32) nextSlot++;
                    slot = Math.Min(nextSlot, 31);
                }
                usedSlots.Add(slot);

                GL.ActiveTexture(TextureUnit.Texture0 + slot);
                GL.BindTexture(TextureTarget.Texture2D, textures[i].textureId);

                var aliases = GetTextureNameAliases(textures[i].Name);
                if (aliases.Count > 0)
                {
                    activeShader.SetIntIfExists(textures[i].Name, slot);
                }
                else
                {
                    activeShader.SetIntIfExists(textures[i].Name, slot);
                }

                foreach (var alias in aliases)
                {
                    textureNames.Add(alias);
                    activeShader.SetIntIfExists(alias, slot);
                }
            }

            PerfCounters.RecordTextureBind(textures.Count);

            ApplyShaderParams(activeShader, layerMaskUvOverride, aoUvOverride, baseColorMapIsPlaceholder);
            if (MessageHandler.Instance.DebugLogsEnabled &&
                string.Equals(shaderKey, "IkCharacter", StringComparison.OrdinalIgnoreCase) &&
                RenderOptions.UseBackupIkCharacterShader)
            {
                LogIkCharacterColorTableStateOnce();
            }
            bool layerMaskIsPlaceholder = textures.Any(t =>
                string.Equals(t.Name, "LayerMaskMap", StringComparison.OrdinalIgnoreCase) &&
                IsPlaceholderMaskTexturePath(t.SourceFile));
            SetTextureFlags(activeShader, textureNames, layerMaskIsPlaceholder);

            if (MessageHandler.Instance.DebugLogsEnabled &&
                string.Equals(shaderKey, "EyeClearCoat", StringComparison.OrdinalIgnoreCase) &&
                loggedEyeClearCoatParams.Add($"{modelpath}::{Name}"))
            {
                TryGetShaderParamIntWithOverrides("NumMaterialLayer", out int layers);
                TryGetShaderParamIntWithOverrides("UVIndexLayerMask", out int uvMask);
                bool? enableLayerMaskOpt = null;
                for (int i = 0; i < ShaderParams.Count; i++)
                {
                    if (!string.Equals(ShaderParams[i].Name, "EnableLayerMaskMap", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var v = ShaderParams[i].Value?.Trim();
                    if (string.Equals(v, "true", StringComparison.OrdinalIgnoreCase) || string.Equals(v, "1", StringComparison.OrdinalIgnoreCase))
                    {
                        enableLayerMaskOpt = true;
                    }
                    else if (string.Equals(v, "false", StringComparison.OrdinalIgnoreCase) || string.Equals(v, "0", StringComparison.OrdinalIgnoreCase))
                    {
                        enableLayerMaskOpt = false;
                    }
                    break;
                }

                var enableLayerMaskLabel = enableLayerMaskOpt.HasValue ? enableLayerMaskOpt.Value.ToString() : "(missing)";
                MessageHandler.Instance.AddMessage(
                    MessageType.LOG,
                    $"[EyeClearCoat] mat='{Name}' layers={layers} hasLayerMaskTex={textureNames.Contains("LayerMaskMap")} EnableLayerMaskMap={enableLayerMaskLabel} UVIndexLayerMask={uvMask}");
            }

            activeShader.SetBool("EnableVertexColor", RenderOptions.EnableVertexColors && hasVertexColors);
            activeShader.SetBool("HasTangents", hasTangents);
            activeShader.SetBool("HasBinormals", hasBinormals);
            activeShader.SetBoolIfExists("HasUv1", hasUv1);
            activeShader.SetBool("FlipNormalY", RenderOptions.FlipNormalY);
            activeShader.SetBool("ReconstructNormalZ", RenderOptions.ReconstructNormalZ);
            SetLightingUniforms(activeShader, view);
            ApplyUniformOverrides(activeShader);
            activeShader.SetMatrix4("model", model);
            activeShader.SetMatrix4("view", view);
            activeShader.SetMatrix4("projection", proj);
        }

        private static readonly HashSet<string> loggedIkCharacterColorTableStates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private void LogIkCharacterColorTableStateOnce()
        {
            var key = $"{modelpath}::{Name}";
            if (!loggedIkCharacterColorTableStates.Add(key))
            {
                return;
            }

            bool TryGetOpt(string name, out string value)
            {
                value = string.Empty;
                for (int i = 0; i < ShaderParams.Count; i++)
                {
                    if (!string.Equals(ShaderParams[i].Name, name, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                    value = ShaderParams[i].Value ?? string.Empty;
                    return true;
                }
                return false;
            }

            string enableStr = "(missing)";
            if (TryGetUniformOverride("EnableColorTableMap", out var enableOverride))
            {
                enableStr = $"override={enableOverride}";
            }
            else if (TryGetOpt("EnableColorTableMap", out var opt))
            {
                enableStr = $"trmtr={opt}";
            }

            int divide = 0;
            bool hasDivide = TryGetShaderParamIntEffective("ColorTableDivideNumber", out divide);
            int i1 = 0, i2 = 0, i3 = 0, i4 = 0;
            bool hasI1 = TryGetShaderParamIntEffective("BaseColorIndex1", out i1);
            bool hasI2 = TryGetShaderParamIntEffective("BaseColorIndex2", out i2);
            bool hasI3 = TryGetShaderParamIntEffective("BaseColorIndex3", out i3);
            bool hasI4 = TryGetShaderParamIntEffective("BaseColorIndex4", out i4);

            bool hasLayerColors =
                vec4Params.Any(p => string.Equals(p.Name, "BaseColorLayer1", StringComparison.OrdinalIgnoreCase)) ||
                vec4Params.Any(p => string.Equals(p.Name, "ShadowingColorLayer1", StringComparison.OrdinalIgnoreCase));

            MessageHandler.Instance.AddMessage(
                MessageType.LOG,
                $"[ColorTable] IkCharacter mat='{Name}' EnableColorTableMap={enableStr} Divide={(hasDivide ? divide.ToString() : "(missing)")} " +
                $"Idx={(hasI1 ? i1.ToString() : "?")},{(hasI2 ? i2.ToString() : "?")},{(hasI3 ? i3.ToString() : "?")},{(hasI4 ? i4.ToString() : "?")} " +
                $"HasLayerColors={hasLayerColors}");
        }

    }
}
