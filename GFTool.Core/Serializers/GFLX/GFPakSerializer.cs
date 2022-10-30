using GFToolCore.Compression;
using GFToolCore.Math.Hash;
using GFToolCore.Models.GFLX;
using GFToolCore.Structures.GFLX;
using GFToolCore.Utils;
using GFToolCore.Cache;

namespace GFToolCore.Serializers.GFLX
{
    public static class GFPakSerializer
    {
        public static GFLibPack Deserialize(BinaryReader br)
        {
            GFPakHeader header = br.ReadBytes(GFPakHeader.SIZE).ToStruct<GFPakHeader>();

            UInt64 embeddedFileOff = br.ReadUInt64();
            UInt64 embeddedFileHashOff = br.ReadUInt64();

            List<Int64> folderOffsets = new List<Int64>();
            for (int i = 0; i < header.FolderNumber; i++)
            {
                Int64 folderOffset = br.ReadInt64();
                folderOffsets.Add(folderOffset);
            }

            br.BaseStream.Position = (long)embeddedFileHashOff;
            List<UInt64> fileHashes = new List<UInt64>();

            for (int i = 0; i < header.FileNumber; i++)
            {
                UInt64 fileHash = br.ReadUInt64();
                fileHashes.Add(fileHash);
            }


            br.BaseStream.Position = (long)embeddedFileOff;
            List<GFPakFileHeader> embeddedFiles = new List<GFPakFileHeader>();
            for (int i = 0; i < header.FileNumber; i++)
            {
                GFPakFileHeader file = br.ReadBytes(GFPakFileHeader.SIZE).ToStruct<GFPakFileHeader>();
                embeddedFiles.Add(file);
            }

            List<byte[]> files = new List<byte[]>();
            for (int i = 0; i < embeddedFiles.Count; i++)
            {
                GFPakFileHeader file = embeddedFiles[i];
                byte[] fileBytes = br.ReadBytes((int)file.FileSize);
                byte[]? decompressed = new byte[file.BufferSize];
                switch (file.CompressionType)
                {
                    case GFPakCompressionType.LZ4:
                        decompressed = LZ4.Decompress(fileBytes, (int)file.BufferSize);
                        files.Add(decompressed);
                        break;
                    case GFPakCompressionType.OODLE:
                        decompressed = Oodle.Decompress(fileBytes, (int)file.BufferSize);
                        if (decompressed == null)
                        {
                            decompressed = fileBytes;
                        }
                        files.Add(decompressed);
                        break;
                    case GFPakCompressionType.NONE:
                    default:
                        files.Add(fileBytes);
                        break;
                }
                files.Add(fileBytes);
            }

            br.BaseStream.Position = folderOffsets[0];
            List<GFLibFolder> folders = new List<GFLibFolder>();
            for (int i = 0; i < header.FolderNumber; i++)
            {
                br.BaseStream.Position = folderOffsets[i];
                GFPakFolderHeader tFolder = br.ReadBytes(GFPakFolderHeader.SIZE).ToStruct<GFPakFolderHeader>();
                var folderName = GFPakHashCache.GetName(tFolder.Hash);
                if (folderName == null) 
                {
                    folderName = tFolder.Hash.ToString();
                }
                GFLibFolder folder = new GFLibFolder() { path = folderName, files = new List<GFLibFile>()};
                for (int j = 0; j < tFolder.ContentNumber; j++)
                {
                    GFPakFolderIndex content = br.ReadBytes(GFPakFolderIndex.SIZE).ToStruct<GFPakFolderIndex>();
                    var name = GFPakHashCache.GetName(content.Hash);
                    if (name == null)
                    {
                        name = content.Hash.ToString();
                    }
                    
                    var path = GFPakHashCache.GetName(fileHashes[(int)content.Index]);
                    if (path == null)
                    {
                        //Need to rethink this one here because I'm an idiot
                        if (name != null && folderName != null) {
                            path = folderName + name;
                            GFPakHashCache.AddHashName(fileHashes[(int)content.Index], path);
                        }
                        else
                        {
                            path = fileHashes[(int)content.Index].ToString();
                        }
                    }
                    GFLibFile file = new GFLibFile()
                    {
                        name = name,
                        path = path,
                        data = files[(int)content.Index],
                    };
                    folder.AddFile(file);
                }
                folders.Add(folder);
            }

            return new GFLibPack() { folders = folders };

        }
        public static void Serialize(BinaryWriter bw, GFLibPack pack)
        {
            GFPakHeader header = new GFPakHeader()
            {
                magic = GFPakHeader.MAGIC,
                Version = 0x1000,
                Relocated = 0,
                FolderNumber = (uint)pack.folders.Count,
                FileNumber = (uint)pack.GetFileCount(),
            };

            bw.Write(header.ToBytes());

            //We'll need to come back here later
            long filePosPtr = bw.BaseStream.Position;
            //Offset to File Pointer
            bw.Write((UInt64)0);

            //Offset to Absolute Hashes
            long fileHashPtr = bw.BaseStream.Position;
            bw.Write((UInt64)0);

            //Offset to Folder Pointers
            long folderPosPtr = bw.BaseStream.Position;
            for (int i = 0; i < pack.folders.Count; i++)
            {
                bw.Write((UInt64)0);
            }

            //Now we go back and add the fileHashPtr
            var pos = bw.BaseStream.Position;
            bw.BaseStream.Position = fileHashPtr;
            bw.Write(pos);
            bw.BaseStream.Position = pos;

            //Write the Absolute Path Hashes
            foreach (GFLibFolder folder in pack.folders)
            {
                foreach (GFLibFile file in folder.files)
                {
                    bw.Write(GFFNV.Hash(file.path));
                }
            }

            //Write the Folder Info
            int fileCount = 0;
            for (int i = 0; i < pack.folders.Count; i++)
            {
                GFLibFolder folder = pack.folders[i];

                //We need to go back and add the folder position
                pos = bw.BaseStream.Position;
                bw.BaseStream.Position = folderPosPtr + GFPakFolderHeader.SIZE * i;
                bw.Write(pos);
                bw.BaseStream.Position = pos;

                bw.Write(
                    new GFPakFolderHeader()
                    {
                        Hash = GFFNV.Hash(folder.path),
                        ContentNumber = (uint)folder.files.Count,
                        Reserved = 0xCC,
                    }.ToBytes());
                foreach (GFLibFile file in folder.files)
                {
                    GFPakFolderIndex folderIndex = new GFPakFolderIndex()
                    {
                        //The relative hash name goes here
                        Hash = GFFNV.Hash(file.name),
                        Index = (uint)fileCount,
                        Reserved = 0xCC,
                    };
                    fileCount++;
                }
            }

            List<long> filePosPtrs = new List<long>();
            List<byte[]> fileData = new List<byte[]>();
            //Write the file info
            foreach (GFLibFolder folder in pack.folders)
            {
                foreach (GFLibFile file in folder.files)
                {
                    //TODO: Compression
                    bw.Write(
                        new GFPakFileHeader()
                        {
                            Level = 9,
                            CompressionType = GFPakCompressionType.NONE,
                            FileSize = (uint)file.data.Length,
                            BufferSize = (uint)file.data.Length,
                            Reserved = 0xCC,
                            FilePointer = 0,
                        }.ToBytes());
                    fileData.Add(file.data);
                    filePosPtrs.Add(bw.BaseStream.Position - 0x8);
                }
            }

            //Go back and write the File Pointer position
            pos = bw.BaseStream.Position;
            bw.BaseStream.Position = filePosPtr;
            bw.Write(pos);
            bw.BaseStream.Position = pos;

            //Now we write the files
            for (int i = 0; i < fileCount; i++)
            {
                //First we update the file info
                pos = bw.BaseStream.Position;
                bw.BaseStream.Position = filePosPtrs[i];
                bw.Write(pos);
                bw.BaseStream.Position = pos;
                //Now we write the filedata
                bw.Write(fileData[i]);
            }

        }
    }
}

