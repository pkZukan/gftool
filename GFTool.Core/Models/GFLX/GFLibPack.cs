using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GFTool.Core.Models.GFLX
{
    public class GFLibFolder
    {
        public string path;
        public List<GFLibFile> files;

        public void AddFile(GFLibFile file)
        {
            files.Add(file);
        }
    }

    public class GFLibFile
    {
        public string name;
        public string fullname;
        public byte[] data;
    }

    public class GFLibPack
    {
        public string name;
        public List<GFLibFolder> folders;

        public int GetFileCount()
        {
            int count = 0;
            foreach (GFLibFolder folder in folders) {
                count += folder.files.Count;
            }
            return count;
        }
    }
}
