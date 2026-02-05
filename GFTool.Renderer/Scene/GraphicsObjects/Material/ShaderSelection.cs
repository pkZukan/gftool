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
        private Shader GetActiveShader()
        {
            // Only EyeClearCoat has a dedicated forward shader. Do not force it for all transparent materials
            // (it expects a different uniform set and can make unrelated transparent materials render invisible).
            if (RenderOptions.TransparentPass &&
                isTransparent &&
                string.Equals(shaderKey, "EyeClearCoat", StringComparison.OrdinalIgnoreCase))
            {
                var forwardShader = ShaderPool.Instance.GetShader("EyeClearCoatForward");
                if (forwardShader != null)
                {
                    return forwardShader;
                }

                if (MessageHandler.Instance.DebugLogsEnabled && warnedMissingEyeClearCoatForward.Add($"{modelpath}::{Name}"))
                {
                    MessageHandler.Instance.AddMessage(
                        MessageType.WARNING,
                        $"[Shader] EyeClearCoatForward missing; using deferred shader for transparent pass: mat='{Name}' model='{modelpath}'");
                }
            }

            if (RenderOptions.LegacyMode)
            {
                shader ??= ShaderPool.Instance.GetShader(shaderKey);
                return shader ?? ShaderPool.Instance.GetShader("Standard");
            }

            shader ??= ShaderPool.Instance.GetShader(shaderKey);
            // If the desired shader failed to compile/load, fall back to Standard so the mesh
            // still renders (and skinning uniforms are applied) instead of reusing stale GL state.
            return shader ?? ShaderPool.Instance.GetShader("Standard");
        }

    }
}
