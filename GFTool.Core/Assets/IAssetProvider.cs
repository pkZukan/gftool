using System;
using System.Collections.Generic;
using System.IO;

namespace Trinity.Core.Assets
{
    public interface IAssetProvider : IDisposable
    {
        string DisplayName { get; }

        bool Exists(string path);
        Stream OpenRead(string path);
        byte[] ReadAllBytes(string path);

        IEnumerable<AssetEntry> EnumerateEntries();
    }

    public readonly record struct AssetEntry(ulong PathHash, string? Path);
}
