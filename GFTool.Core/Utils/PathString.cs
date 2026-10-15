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
        private readonly string sourceFile;
        private readonly AssetPathResolver resolver;

        public PathString(string root, string? assetRoot = null)
        { 
            rootPath = Path.GetDirectoryName(root);
            sourceFile = root;
            resolver = new AssetPathResolver(assetRoot);
        }

        public string Combine(string str)
        {
            var local = Path.Combine(rootPath, str.Replace('/', Path.DirectorySeparatorChar));
            return File.Exists(local) ? local : resolver.Resolve(str, sourceFile);
        }
    }
}
