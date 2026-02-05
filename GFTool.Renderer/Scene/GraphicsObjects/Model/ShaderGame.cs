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
        private ShaderGame shaderGame = ShaderGame.Auto;

        private static ShaderGame ResolveEffectiveShaderGame(TrmtrFile? trmtr, IAssetProvider assetProvider)
        {
            // GFPAK contents are LA-only in our usage.
            if (assetProvider is GfpakAssetProvider)
            {
                return ShaderGame.LA;
            }

            if (RenderOptions.ShaderGame != ShaderGame.Auto)
            {
                return RenderOptions.ShaderGame;
            }

            // If we can't find techniques, default to SCVI (shared technique set is closer).
            if (trmtr?.Materials != null)
            {
                foreach (var item in trmtr.Materials)
                {
                    if (item?.Shaders == null)
                    {
                        continue;
                    }

                    foreach (var shader in item.Shaders)
                    {
                        var name = shader?.Name;
                        if (string.IsNullOrWhiteSpace(name))
                        {
                            continue;
                        }

                        if (name.StartsWith("Ik", StringComparison.OrdinalIgnoreCase))
                        {
                            return ShaderGame.ZA;
                        }
                    }
                }
            }

            return ShaderGame.SCVI;
        }

        private static string MapTechniqueToShaderName(string techniqueName, ShaderGame game)
        {
            if (string.IsNullOrWhiteSpace(techniqueName))
            {
                return "Standard";
            }

            // Keep mapping minimal and focused: handle known non-identity mappings that affect runtime shader selection.
            switch (game)
            {
                case ShaderGame.ZA:
                    {
                        // ZA technique list maps these to the IkCharacter archive.
                        if (techniqueName.Equals("IkCharacterTransparent", StringComparison.OrdinalIgnoreCase) ||
                            techniqueName.Equals("IkCharacterTransparentInner", StringComparison.OrdinalIgnoreCase))
                        {
                            return "IkCharacter";
                        }

                        // ZA technique list maps these to the SSS shader.
                        if (techniqueName.Equals("TransparentSSS", StringComparison.OrdinalIgnoreCase) ||
                            techniqueName.Equals("TransparentInnerSSS", StringComparison.OrdinalIgnoreCase))
                        {
                            return "SSS";
                        }

                        return techniqueName;
                    }
                case ShaderGame.LA:
                    {
                        // LA technique list maps a few techniques onto the haStandard shader archive.
                        if (techniqueName.Equals("Standard", StringComparison.OrdinalIgnoreCase) ||
                            techniqueName.Equals("DepthFade", StringComparison.OrdinalIgnoreCase) ||
                            techniqueName.Equals("TransparentInner", StringComparison.OrdinalIgnoreCase))
                        {
                            return "haStandard";
                        }

                        // LA also maps ImplicitTech -> ImplicitVolume. Keep the name for now; we can implement it later.
                        if (techniqueName.Equals("ImplicitTech", StringComparison.OrdinalIgnoreCase))
                        {
                            return "ImplicitVolume";
                        }

                        return techniqueName;
                    }
                case ShaderGame.SCVI:
                case ShaderGame.Auto:
                default:
                    return techniqueName;
            }
        }
	    }
}
