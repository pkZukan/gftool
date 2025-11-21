
using GFTool.Renderer.Scene.GraphicsObjects;

namespace GFTool.Renderer.Scene
{
    public sealed class SceneGraph
    {
        private RefObject rootScene;

        private static readonly Lazy<SceneGraph> lazy = new Lazy<SceneGraph>(() => new SceneGraph());
        public static SceneGraph Instance { get { return lazy.Value; } }
        private SceneGraph()
        {
            rootScene = new RefObject();
        }

        public RefObject GetRoot()
        {
            return rootScene;
        }
    }
}
