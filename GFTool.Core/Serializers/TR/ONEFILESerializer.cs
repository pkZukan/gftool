using GFTool.Core.Flatbuffers.TR.ResourceDictionary;
using GFTool.Core.Structures.TR;
using GFTool.Core.Utils;
using GFTool.Core.Math.Hash;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GFTool.Core.Serializers.TR
{
    public static class ONEFILESerializer
    {
        public static FileSystem DeserializeFileSystem(BinaryReader br)
        {
            var header = br.ReadBytes(OneFileHeader.SIZE).ToStruct<OneFileHeader>();
            br.BaseStream.Position = header.offset;
            return FlatBufferConverter.DeserializeFrom<FileSystem>(br.ReadBytes((int)(br.BaseStream.Length - header.offset)));
        }

        public static FileSystem DeserializeFileSystem(String path)
        {
            BinaryReader binaryReader = new BinaryReader(File.OpenRead(path));
            return DeserializeFileSystem(binaryReader);
        }

        public static byte[] SplitTRPAK(BinaryReader br, long offset, long fileSize)
        {
            br.BaseStream.Position = offset;
            var bytes =  br.ReadBytes((int)fileSize);
            br.Close();
            return bytes;
        }

        public static byte[] SplitTRPAK(String path, long offset, long fileSize)
        {
            BinaryReader binaryReader = new BinaryReader(File.OpenRead(path));
            return SplitTRPAK(binaryReader, offset, fileSize);
        }
    }
}
