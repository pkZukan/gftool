using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrinityModLoader
{
    public static class TreeBuilder
    {
        public static TreeNode MakeTreeFromPaths(List<string> paths, TreeNode rootNode, Action progUpdate, bool used = true)
        {
            foreach (var path in paths)
            {
                var currentNode = rootNode;
                var pathItems = path.Split('/');
                foreach (var item in pathItems)
                {
                    var tmp = currentNode.Nodes.Cast<TreeNode>().Where(x => x.Text.Equals(item));
                    TreeNode tn = new TreeNode();
                    if (tmp.Count() == 0)
                    {
                        tn.Text = item;
                        if (!used) tn.BackColor = Color.Red;
                        currentNode.Nodes.Add(tn);
                        currentNode = tn;
                    }
                    else
                        currentNode = tmp.Single();
                }
                progUpdate();
            }
            return rootNode;
        }
    }
}
