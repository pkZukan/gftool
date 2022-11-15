using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GFTool.Core.Models.TR
{
    public class TRPackFile
    {
        public string path;
        public byte[] data;
    }
    public class TRPack
    {
        public List<TRPackFile> files;

        public TRPack(List<TRPackFile> files)
        {
            this.files = files;
        }
    }
}
