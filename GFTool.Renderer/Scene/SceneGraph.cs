
namespace GFTool.Renderer.Scene
{
    public sealed class SceneGraph
    {
        private GraphicsObjects.Object rootScene;

        private static readonly Lazy<SceneGraph> lazy = new Lazy<SceneGraph>(() => new SceneGraph());
        public static SceneGraph Instance { get { return lazy.Value; } }
        private SceneGraph()
        {
            rootScene = new GraphicsObjects.Object();
        }

        public GraphicsObjects.Object GetRoot()
        {
            return rootScene;
        }
    }
}
