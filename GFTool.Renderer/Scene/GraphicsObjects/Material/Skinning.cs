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
        public void ApplySkinning(bool enabled, int boneCount, Matrix4[] matrices)
        {
            var activeShader = GetActiveShader();
            if (activeShader == null)
            {
                return;
            }

            activeShader.Bind();
            activeShader.SetBoolIfExists("EnableSkinning", enabled);
            activeShader.SetIntIfExists("BoneCount", enabled ? boneCount : 0);
            activeShader.SetBoolIfExists("SwapBlendOrder", RenderOptions.SwapBlendOrder);
            if (enabled)
            {
                PerfCounters.RecordSkinMatrixUpload();
                if (MessageHandler.Instance.DebugLogsEnabled)
                {
                    bool hasEnable = activeShader.HasUniformFast("EnableSkinning");
                    bool hasBones = activeShader.HasUniformFast("Bones");
                    bool hasCount = activeShader.HasUniformFast("BoneCount");
                    if ((!hasEnable || !hasBones || !hasCount) && warnedMissingSkinningUniforms.Add($"{activeShader.DebugName}::{Name}"))
                    {
                        MessageHandler.Instance.AddMessage(
                            MessageType.WARNING,
                            $"[Skin] Shader missing skinning uniforms: shader='{activeShader.DebugName}' mat='{Name}' EnableSkinning={hasEnable} Bones={hasBones} BoneCount={hasCount}");
                    }
                }

                activeShader.SetMatrix4ArrayIfExists("Bones", matrices, RenderOptions.TransposeSkinMatrices);
            }
        }

    }
}
