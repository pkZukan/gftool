
using Trinity.Core.Cache;
using Trinity.Core.Compression;
using Trinity.Core.Flatbuffers.TR.ResourceDictionary;
using Trinity.Core.Math.Hash;
using Trinity.Core.Serializers.TR;
using Trinity.Core.Utils;
using System.Threading;
using System.Threading.Tasks;

namespace TrinityFileExplorer
{
    public partial class ExportProgressWindow : Form
    {
        private volatile bool isClosing = false;
        private long lastUiUpdateTicks = 0;
        private const int UiUpdateIntervalMs = 100;


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
            if (fileDescriptor == null || fileSystem == null || fileHashes.Length == 0)
            {
                return false;
            }

            var packMap = new Dictionary<string, HashSet<ulong>>(StringComparer.Ordinal);
            foreach (ulong hash in fileHashes.Distinct())
            {
                var packName = fileDescriptor.GetPackName(hash);
                if (string.IsNullOrEmpty(packName))
                {
                    continue;
                }

                if (!packMap.TryGetValue(packName, out var hashes))
                {
                    hashes = new HashSet<ulong>();
                    packMap.Add(packName, hashes);
                }
                hashes.Add(hash);
            }

            int processed = 0;
            int total = packMap.Sum(pair => pair.Value.Count);
            var options = new ParallelOptions
            {
                MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount - 1)
            };

            Parallel.ForEach(packMap, options, pair =>
            {
                if (isClosing)
                {
                    return;
                }

                var pack = GetPackByName(pair.Key);
                if (pack == null)
                {
                    return;
                }

                SavePackFiles(pack, pair.Value, outFolder, total, ref processed);
            });

            return true;
        }

        private void SavePackFiles(PackedArchive pack, HashSet<ulong> hashes, string outFolder, int total, ref int processed)
        {
            for (int i = 0; i < pack.FileEntry.Length; i++)
            {
                if (isClosing)
                {
                    break;
                }

                var hash = pack.FileHashes[i];

                if (!hashes.Contains(hash))
                {
                    continue;
                }

                var fileName = GFPakHashCache.GetName(hash);
                fileName ??= hash.ToString("X16") + ".bin";
                var filepath = string.Format("{0}\\{1}", outFolder, fileName);

                var current = Interlocked.Increment(ref processed);
                if (ShouldUpdateUi(current, total))
                {
                    UpdateProgressUi(fileName, filepath);
                }

                var dir = Path.GetDirectoryName(filepath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                var entry = pack.FileEntry[i];
                var buffer = entry.FileBuffer;

                if (entry.EncryptionType != -1)
                    buffer = Oodle.Decompress(buffer, (long)entry.FileSize);

                File.WriteAllBytes(filepath, buffer);
            }
        }

        private bool ShouldUpdateUi(int processed, int total)
        {
            if (processed == total)
            {
                return true;
            }

            long now = Environment.TickCount64;
            long last = Interlocked.Read(ref lastUiUpdateTicks);
            if (now - last < UiUpdateIntervalMs)
            {
                return false;
            }

            Interlocked.Exchange(ref lastUiUpdateTicks, now);
            return true;
        }

        private void UpdateProgressUi(string fileName, string filepath)
        {
            if (IsDisposed)
            {
                return;
            }

            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => UpdateProgressUi(fileName, filepath)));
                return;
            }

            NameValueLabel.Text = fileName;
            ToValueLabel.Text = filepath;
            Refresh();
        }

        private PackedArchive? GetPackByName(string packName)
        {
            if (fileDescriptor == null || fileSystem == null)
            {
                return null;
            }

            int packIndex = Array.IndexOf(fileDescriptor.PackNames, packName);
            if (packIndex == -1)
            {
                return null;
            }

            ulong packHash = GFFNV.Hash(packName);
            int fileIndex = Array.IndexOf(fileSystem.FileHashes, packHash);
            if (fileIndex == -1)
            {
                return null;
            }

            PackInfo? packInfo = fileDescriptor.PackInfo[packIndex];
            if (packInfo == null)
            {
                return null;
            }

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
