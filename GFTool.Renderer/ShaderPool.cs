
using System.Xml.Linq;

namespace GFTool.Renderer
{
    public class ShaderPool
    {
        private static readonly Lazy<ShaderPool> lazy = new Lazy<ShaderPool>(() => new ShaderPool());
        public static ShaderPool Instance { get { return lazy.Value; } }

        private Dictionary<string, Shader> shaders = new Dictionary<string, Shader>();

        private string shaderPath;

        private ShaderPool() 
        {
            shaderPath = "Shaders/";
        }

        private void AddShader(string name)
        {
            var vsh = shaderPath + name + ".vsh";
            var fsh = shaderPath + name + ".fsh";
            shaders[name] = new Shader(name, vsh, fsh);
            MessageHandler.Instance.AddMessage(MessageType.LOG, string.Format("Shader \"{0}\" loaded into pool.", name));
        }

        public Shader GetShader(string name)
        {
            if (!shaders.ContainsKey(name))
                AddShader(name);
            return shaders[name];
        }

        public void Bind(string name)
        { 
            var shader = GetShader(name);
            shader.Bind();
        }
    }
}
