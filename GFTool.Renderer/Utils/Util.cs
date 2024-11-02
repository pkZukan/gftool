using Microsoft.CodeAnalysis.CSharp.Syntax;
using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GFTool.Renderer.Utils
{
    public static class Util
    {
        public static Tuple<PixelInternalFormat, PixelFormat> BntxFormatToGL4(uint fmt)
        {
            byte channel = (byte)((fmt >> 8) & 0xFF);
            byte type = (byte)(fmt & 0xFF);

            var outChan = new PixelInternalFormat[] { 
                0,
                PixelInternalFormat.Rg8,    //R4_G4
                PixelInternalFormat.R8,     //R8
                PixelInternalFormat.Rgba16, //R4_G4_B4_A4
                PixelInternalFormat.Rgba16,  //A4_B4_G4_R4
                PixelInternalFormat.Rgb5A1, //R5_G5_B5_A1
                PixelInternalFormat.Rgb5A1, //A1_B5_G5_R5
                PixelInternalFormat.R5G6B5IccSgix,  //B5_G6_R5
                PixelInternalFormat.Rg16,  //R8_G8
                PixelInternalFormat.R16,    //R16
                PixelInternalFormat.Rgba32i,  //R8_G8_B8_A8
                0, //B8_G8_R8_A8
                0, //R9_G9_B9_E5
                0, //R10_G10_B10_A2
                0, //R11_G11_B10
                0, //B10_G11_R11
                0, //R10_G11_B11
                0, //R16_G16
                0, //R24_G8
                0, //R32
                0, //R16_G16_B16_A16
                0, //R32_G8_X24
                0, //R32_G32
                0, //R32_G32_B32
                0, //R32_G32_B32_A32
                PixelInternalFormat.CompressedRgbaS3tcDxt1Ext, //BC1
                PixelInternalFormat.CompressedRgbaS3tcDxt3Ext, //BC2
                PixelInternalFormat.CompressedRgbaS3tcDxt5Ext, //BC3
                PixelInternalFormat.CompressedRedRgtc1, //BC4
                PixelInternalFormat.CompressedRgRgtc2, //BC5
                PixelInternalFormat.CompressedRgbBptcUnsignedFloat, //BC6
                PixelInternalFormat.CompressedRgbaBptcUnorm, //BC7
                0, 
                0, 
                0, 
                0
            }[channel];

            var outFmt = new PixelFormat[] { 
                0,
                PixelFormat.UnsignedInt,    //unorm int
                PixelFormat.UnsignedInt,    //snorm int
                PixelFormat.UnsignedInt,    //uint
                PixelFormat.UnsignedInt,    //sint
                PixelFormat.UnsignedInt,    //float
                PixelFormat.UnsignedInt,    //unorm srgb
                PixelFormat.DepthStencil,   //depth stencil
                PixelFormat.UnsignedInt,    //uint to float
                PixelFormat.UnsignedInt,    //sint to float
                PixelFormat.UnsignedInt,    //ufloat
            }[type];

            return new Tuple<PixelInternalFormat, PixelFormat>(outChan, outFmt);

        }
    }
}
