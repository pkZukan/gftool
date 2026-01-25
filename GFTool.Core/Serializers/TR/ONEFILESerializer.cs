using Trinity.Core.Flatbuffers.TR.ResourceDictionary;
using Trinity.Core.Structures.TR;
using Trinity.Core.Utils;
using Trinity.Core.Math.Hash;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Trinity.Core.Serializers.TR
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
            if (fileSize < 0 || fileSize > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(fileSize),
                    fileSize,
                    "TRPAK section is too large to load into a single byte[]; use an extracted RomFS on disk or implement streamed extraction.");
            }

            br.BaseStream.Position = offset;
            var bytes = br.ReadBytes((int)fileSize);
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
