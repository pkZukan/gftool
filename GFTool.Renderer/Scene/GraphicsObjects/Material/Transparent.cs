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
        public static void ResetTransparentBlendStateCache()
        {
            lastTransparentBlendMode = null;
        }

        private void ApplyTransparentBlendState()
        {
            if (!RenderOptions.TransparentPass || !isTransparent)
            {
                return;
            }

            if (lastTransparentBlendMode.HasValue && lastTransparentBlendMode.Value == transparentBlendMode)
            {
                return;
            }

            switch (transparentBlendMode)
            {
                case TransparentBlendMode.PremultipliedAlpha:
                    GL.BlendFunc(BlendingFactor.One, BlendingFactor.OneMinusSrcAlpha);
                    break;
                case TransparentBlendMode.Additive:
                    GL.BlendFunc(BlendingFactor.One, BlendingFactor.One);
                    break;
                case TransparentBlendMode.Alpha:
                default:
                    GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
                    break;
            }

            lastTransparentBlendMode = transparentBlendMode;
        }

    }
}
