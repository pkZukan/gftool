using System;
using System.Collections.Generic;
using System.IO;

namespace Trinity.Core.Assets
{
    public sealed class DiskAssetProvider : IAssetProvider
    {
        public string DisplayName => "Disk";

        public bool Exists(string path) => File.Exists(path);

        public Stream OpenRead(string path) => File.OpenRead(path);

        public byte[] ReadAllBytes(string path) => File.ReadAllBytes(path);

        public IEnumerable<AssetEntry> EnumerateEntries()
        {
            yield break;
        }

        public void Dispose()
        {
        }
    }
}
