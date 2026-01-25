using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Trinity.Core.Models.GFLX
{
    public class GFLibFolder
    {
        public string path = string.Empty;
        public List<GFLibFile> files = new List<GFLibFile>();

        public void AddFile(GFLibFile file)
        {
            files.Add(file);
        }
    }

    public class GFLibFile
    {
        public string name = string.Empty;
        public string fullname = string.Empty;
        public byte[] data = Array.Empty<byte>();
    }

    public class GFLibPack
    {
        public string name = string.Empty;
        public List<GFLibFolder> folders = new List<GFLibFolder>();

        public int GetFileCount()
        {
            int count = 0;
            foreach (GFLibFolder folder in folders)
            {
                count += folder.files.Count;
            }
            return count;
        }
    }
}
