
namespace TrinityModLoader
{
    internal interface IExplorerViewer
    {
        public void Disable();
        public void Enable();
        public string GetCwd();
        public void ParseFileDescriptor(CustomFileDescriptor fileDescriptor);
        public void NavigateTo(string path);
        public string? GetPathAtIndex(int index);

        public IEnumerable<ulong> GetFolderPaths(string path);
        public IEnumerable<ulong> GetFiles();
        public IEnumerable<ulong> GetUnhashedFiles();
    }
}