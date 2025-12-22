
using Trinity.Core.Cache;
using Trinity.Core.Compression;
using Trinity.Core.Flatbuffers.TR.ResourceDictionary;
using Trinity.Core.Math.Hash;
using Trinity.Core.Serializers.TR;
using Trinity.Core.Utils;

namespace TrinityFileExplorer
{
    public partial class ExportProgressWindow : Form
    {
        private bool isClosing = false;


        public CustomFileDescriptor? fileDescriptor { get; private set; }
        public FileSystem? fileSystem { get; private set; }

        public ExportProgressWindow(CustomFileDescriptor fileDescriptor, FileSystem fileSystem)
        {
            this.fileDescriptor = fileDescriptor;
            this.fileSystem = fileSystem;
            InitializeComponent();
        }

        public bool SaveFiles(ulong[] fileHashes, string outFolder)
        {
            foreach (ulong hash in fileHashes)
            {
                if (isClosing) { break; }
                SaveFile(hash, outFolder);
            }

            return true;
        }

        private void SaveFile(ulong fileHash, string outFolder)
        {
            PackedArchive pack = GetPack(fileHash);

            for (int i = 0; i < pack.FileEntry.Length; i++)
            {
                var hash = pack.FileHashes[i];

                if (hash == fileHash)
                {
                    var fileName = GFPakHashCache.GetName(hash);
                    fileName ??= hash.ToString("X16") + ".bin";
                    var filepath = string.Format("{0}\\{1}", outFolder, fileName);

                    NameValueLabel.Text = fileName;
                    ToValueLabel.Text = filepath;

                    Console.WriteLine("pee");

                    Refresh();

                    if (!Directory.Exists(Path.GetDirectoryName(filepath)))
                        Directory.CreateDirectory(Path.GetDirectoryName(filepath));

                    var entry = pack.FileEntry[i];
                    var buffer = entry.FileBuffer;

                    if (entry.EncryptionType != -1)
                        buffer = Oodle.Decompress(buffer, (long)entry.FileSize);

                    File.WriteAllBytes(filepath, buffer);

                    break;
                }
            }
        }

        private PackedArchive GetPack(ulong fileHash)
        {
            ulong packHash = GFFNV.Hash(fileDescriptor.GetPackName(fileHash));
            int fileIndex = Array.IndexOf(fileSystem.FileHashes, packHash);

            PackInfo? packInfo = fileDescriptor.GetPackInfo(fileHash);
            byte[] fileBytes = ONEFILESerializer.SplitTRPAK(Path.Join(ExplorerSettings.GetRomFSPath(), FilepathSettings.trpfsRel), (long)fileSystem.FileOffsets[fileIndex], (long)packInfo.FileSize);

            PackedArchive pack = FlatBufferConverter.DeserializeFrom<PackedArchive>(fileBytes);
            return pack;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            isClosing = true;
        }

        private void CancelExportButton_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
