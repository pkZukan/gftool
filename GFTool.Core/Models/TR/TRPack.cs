using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Trinity.Core.Models.TR
{
    public class TRPackFile
    {
        public string path = string.Empty;
        public byte[] data = Array.Empty<byte>();
    }
    public class TRPack
    {
        public List<TRPackFile> files = new List<TRPackFile>();

        public TRPack(List<TRPackFile> files)
        {
            this.files = files ?? new List<TRPackFile>();
        }
    }
}
