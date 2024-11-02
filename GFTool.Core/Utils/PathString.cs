using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GFTool.Core.Utils
{
    public class PathString
    {
        private string rootPath;

        public PathString(string root)
        { 
            rootPath = Path.GetDirectoryName(root);
        }

        public string Combine(string str)
        {
            return Path.Combine(rootPath, str);
        }
    }
}
