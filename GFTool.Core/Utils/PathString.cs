using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace GFTool.Core.Utils
{
    public class PathString
    {
        private readonly string rootPath;

        public PathString(string root)
        {
            rootPath = Path.GetDirectoryName(root) ?? string.Empty;
        }

        public string Combine(string str)
        {
            return Path.Combine(rootPath, str);
        }
    }
}
