using GFTool.Core.Utils;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Trinity.Core.Utils;

namespace GFTool.Core.Middleware
{
    public class BNTX
    {
        public string Name { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }
        public uint Format { get; private set; }

        public List<byte[]> Images { get; private set; }

        private BRTData brtd;
        private BRTInfo brti;

        private static Dictionary<int, int> bpps = new Dictionary<int, int>
        {
            { 0x0b, 0x04 }, { 0x07, 0x02 }, { 0x02, 0x01 }, { 0x09, 0x02 }, { 0x1a, 0x08 },
            { 0x1b, 0x10 }, { 0x1c, 0x10 }, { 0x1d, 0x08 }, { 0x1e, 0x10 }, { 0x1f, 0x10 },
            { 0x20, 0x10 }, { 0x2d, 0x10 }, { 0x2e, 0x10 }, { 0x2f, 0x10 }, { 0x30, 0x10 },
            { 0x31, 0x10 }, { 0x32, 0x10 }, { 0x33, 0x10 }, { 0x34, 0x10 }, { 0x35, 0x10 },
            { 0x36, 0x10 }, { 0x37, 0x10 }, { 0x38, 0x10 }, { 0x39, 0x10 }, { 0x3a, 0x10 }
        };

        private static Dictionary<int, (int, int)> blkDims = new Dictionary<int, (int, int)>
        {
            { 0x1a, (4, 4) }, { 0x1b, (4, 4) }, { 0x1c, (4, 4) }, { 0x1d, (4, 4) }, { 0x1e, (4, 4) },
            { 0x1f, (4, 4) }, { 0x20, (4, 4) }, { 0x2d, (4, 4) }, { 0x2e, (5, 4) }, { 0x2f, (5, 5) },
            { 0x30, (6, 5) }, { 0x31, (6, 6) }, { 0x32, (8, 5) }, { 0x33, (8, 6) }, { 0x34, (8, 8) },
            { 0x35, (10, 5) }, { 0x36, (10, 6) }, { 0x37, (10, 8) }, { 0x38, (10, 10) }, { 0x39, (12, 10) },
            { 0x3a, (12, 12) }
        };

        struct BNTXHeader
        {
            public string Magic;
            public uint Version;
            public ushort unk_1;
            public ushort Revision;
            public uint FilenameAddr;
            public ushort unk_2;
            public ushort StringAddr;
            public uint RelocAddr;
            public uint FileSize;

            public BNTXHeader(BinaryReader br)
            {
                Magic = br.ReadFixedString(8);
                Version = br.ReadUInt32();
                unk_1 = br.ReadUInt16();
                Revision = br.ReadUInt16();
                FilenameAddr = br.ReadUInt32();
                unk_2 = br.ReadUInt16();
                StringAddr = br.ReadUInt16();
                RelocAddr = br.ReadUInt32();
                FileSize = br.ReadUInt32();
            }
        }

        struct NXHeader
        {
            public string Magic;
            public uint Count;
            public ulong InfoPtrAddr;
            public ulong DataBlkAddr;
            public ulong DictAddr;
            public uint StrDictSize;

            public NXHeader(BinaryReader br)
            {
                Magic = br.ReadFixedString(4);
                Count = br.ReadUInt32();
                InfoPtrAddr = br.ReadUInt64();
                DataBlkAddr = br.ReadUInt64();
                DictAddr = br.ReadUInt64();
                StrDictSize = br.ReadUInt32();
            }
        }

        struct BRTInfo
        {
            public string Magic;
            public uint Size;
            public ulong OffsetToData;
            public byte TileMode;
            public byte DIM;
            public ushort Flags;
            public ushort Swizzle;
            public ushort MipsCount;
            public uint NumMultiSample;
            public uint Format;
            public uint GPUAccessFlags;
            public int Width;
            public int Height;
            public int Depth;
            public int ArrayLength;
            public int SizeRange;
            public uint unk38;
            public uint unk3C;
            public uint unk40;
            public uint unk44;
            public uint unk48;
            public uint unk4C;
            public int DataSize;
            public int Alignment;
            public int ChannelType;
            public int Type;
            public ulong NameOffset;
            public ulong ParentOffset;
            public ulong MipMapArrayPtr;
            public ulong UserDataPtr;
            public ulong TexturePtr;
            public ulong TextureViewPtr;
            public ulong UserDescriptorSlot;
            public ulong UserDataDicPtr;

            public BRTInfo(BinaryReader br)
            {
                Magic = br.ReadFixedString(4);
                Size = br.ReadUInt32();
                OffsetToData = br.ReadUInt64();
                TileMode = br.ReadByte();
                DIM = br.ReadByte();
                Flags = br.ReadUInt16();
                Swizzle = br.ReadUInt16();
                MipsCount = br.ReadUInt16();
                NumMultiSample = br.ReadUInt32();
                Format = br.ReadUInt32();
                GPUAccessFlags = br.ReadUInt32();
                Width = br.ReadInt32();
                Height = br.ReadInt32();
                Depth = br.ReadInt32();
                ArrayLength = br.ReadInt32();
                SizeRange = br.ReadInt32();
                unk38 = br.ReadUInt32();
                unk3C = br.ReadUInt32();
                unk40 = br.ReadUInt32();
                unk44 = br.ReadUInt32();
                unk48 = br.ReadUInt32();
                unk4C = br.ReadUInt32();
                DataSize = br.ReadInt32();
                Alignment = br.ReadInt32();
                ChannelType = br.ReadInt32();
                Type = br.ReadInt32();
                NameOffset = br.ReadUInt64();
                ParentOffset = br.ReadUInt64();
                MipMapArrayPtr = br.ReadUInt64();
                UserDataPtr = br.ReadUInt64();
                TexturePtr = br.ReadUInt64();
                TextureViewPtr = br.ReadUInt64();
                UserDescriptorSlot = br.ReadUInt64();
                UserDataDicPtr = br.ReadUInt64();
            }
        }

        struct BRTData
        {
            public string Magic;
            public ulong FileSize;

            public BRTData(BinaryReader br)
            {
                Magic = br.ReadFixedString(8);
                FileSize = br.ReadUInt64();
            }
        };

        private const int ALIGNMENT = 512;
        private int DivRoundUp(int n, int d) => (n + d - 1) / d;
        private int RoundUp(int x, int y) => ((x - 1) | (y - 1)) + 1;

        private byte[] Swizzle(BRTInfo info, byte[] data, bool toSwizzle)
        {
            int blockHeight = 1 << info.SizeRange;
            int bpp = bpps[(int)info.Format >> 8];
            (int blkWidth, int blkHeight) = blkDims[(int)info.Format >> 8];

            var width = DivRoundUp(info.Width, blkWidth);
            var height = DivRoundUp(info.Height, blkHeight);

            int pitch, surfSize;
            if (info.TileMode == 1)
            {
                pitch = RoundUp(width * bpp, 32);
                surfSize = RoundUp(pitch * height, ALIGNMENT);
            }
            else
            {
                pitch = RoundUp(width * bpp, 64);
                surfSize = RoundUp(pitch * RoundUp(height, blockHeight * 8), ALIGNMENT);
            }

            byte[] result = new byte[surfSize];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int pos = -1;
                    int pos_ = (y * width + x) * bpp;

                    if (info.TileMode == 1)
                    {
                        pos = y * pitch + x * bpp;
                    }
                    else
                    {
                        pos = GetAddrBlockLinear(x, y, width, bpp, 0, blockHeight);
                    }

                    if (pos + bpp <= surfSize)
                    {
                        if (toSwizzle)
                        {
                            Array.Copy(data, pos_, result, pos, bpp);
                        }
                        else
                        {
                            Array.Copy(data, pos, result, pos_, bpp);
                        }
                    }
                }
            }

            return result;
        }

        private int GetAddrBlockLinear(int x, int y, int imageWidth, int bytesPerPixel, int baseAddress, int blockHeight)
        {
            int imageWidthInGobs = DivRoundUp(imageWidth * bytesPerPixel, 64);

            int gobAddress = baseAddress
                + (y / (8 * blockHeight)) * 512 * blockHeight * imageWidthInGobs
                + (x * bytesPerPixel / 64) * 512 * blockHeight
                + (y % (8 * blockHeight) / 8) * 512;

            x *= bytesPerPixel;

            return gobAddress + ((x % 64) / 32) * 256 + ((y % 8) / 2) * 64 + ((x % 32) / 16) * 32 + (y % 2) * 16 + (x % 16);
        }

        public bool LoadFromFile(string file)
        {
            Images = new List<byte[]>();
            if (!File.Exists(file))
            {
                throw new InvalidOperationException($"Couldn't load BNTX file: {file}");
            }

            using (var br = new BinaryReader(new MemoryStream(File.ReadAllBytes(file))))
            {
                var bntxHead = new BNTXHeader(br);
                if (bntxHead.Magic != "BNTX")
                {
                    Trace.WriteLine("BNTX magic error: " + bntxHead.Magic);
                    return false;
                }

                var nxHead = new NXHeader(br);
                if (nxHead.Magic != "NX  ")
                {
                    Trace.WriteLine("NX magic error: " + nxHead.Magic);
                    return false;
                }

                for (int i = 0; i < nxHead.Count; i++)
                {
                    br.BaseStream.Position = (long)nxHead.InfoPtrAddr + (i * 8);
                    var infoPos = br.ReadUInt64();

                    //BRTI
                    br.BaseStream.Position = (long)infoPos;
                    brti = new BRTInfo(br);
                    if (brti.Magic != "BRTI")
                    {
                        Trace.WriteLine("BRTI magic error: " + brti.Magic);
                        return false;
                    }

                    var Depth = brti.Depth;
                    var MipsCount = brti.MipsCount;

                    //Name
                    br.BaseStream.Position = (long)brti.NameOffset;
                    Name = br.ReadPascalString();

                    //Mips
                    ulong[] Mips = new ulong[MipsCount];
                    br.BaseStream.Position = (long)brti.MipMapArrayPtr;
                    for (int j = 0; j < MipsCount; j++)
                    { 
                        Mips[j] = br.ReadUInt64();
                    }

                    //BRTD
                    br.BaseStream.Position = (long)nxHead.DataBlkAddr;
                    brtd = new BRTData(br);
                    if (brtd.Magic != "BRTD")
                    {
                        Trace.WriteLine("BRTD magic error: " + brtd.Magic);
                        return false;
                    }

                    //TODO: read all mips
                    br.BaseStream.Position = (long)Mips[0];
                    Images.Add(br.ReadBytes(brti.DataSize));

                    Format = brti.Format;
                }
            }

            return true;
        }

        public Bitmap ToBitmap()
        {
            var unswizz_data = Swizzle(brti, Images[0], false);

            // Create bitmap
            Bitmap img = new Bitmap(brti.Width, brti.Height);

            //TODO: decode image
            byte[] decoded_img = new byte[0];

            BitmapData bmp_data = img.LockBits(new Rectangle(0, 0, img.Width, img.Height), ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            Marshal.Copy(decoded_img, 0, bmp_data.Scan0, decoded_img.Length);

            img.UnlockBits(bmp_data);

            return img;
        }
    }
}
