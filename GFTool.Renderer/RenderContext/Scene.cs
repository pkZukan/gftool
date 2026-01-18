using GFTool.Renderer.Core;
using GFTool.Renderer.Core.Graphics;
using GFTool.Renderer.Scene;
using GFTool.Renderer.Scene.GraphicsObjects;
using OpenTK;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System.Collections.Generic;
using System.Drawing;
using System.Diagnostics;
using System.Linq;
using Trinity.Core.Assets;
using GFTool.Renderer.Core;

namespace GFTool.Renderer
{
    public partial class RenderContext : IDisposable
    {
        internal void AddSceneModelDeferred(Model model)
        {
            if (model == null)
            {
                return;
            }

            SceneGraph.Instance.GetRoot().children.Add(model);
        }

        public Model AddSceneModel(string file, bool loadAllLods = false)
        {
            //TODO: Probably figure out how we're adding shit to child nodes (assuming necessary at this level)

            var mdl = new Model(file, loadAllLods);
            SceneGraph.Instance.GetRoot().AddChild(mdl);

            return mdl;
        }

        public Model AddSceneModel(IAssetProvider assetProvider, string file, bool loadAllLods = false)
        {
            var mdl = new Model(assetProvider, file, loadAllLods);
            SceneGraph.Instance.GetRoot().AddChild(mdl);
            return mdl;
        }

        public void RemoveSceneModel(Model mdl)
        {
            var root = SceneGraph.Instance.GetRoot();
            if (root.children.Contains(mdl))
                root.children.Remove(mdl);
        }

        public void ClearScene()
        {
            var root = SceneGraph.Instance.GetRoot();
            root.children.RemoveAll(child => child is Model);
        }

    }
}
