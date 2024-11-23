
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

        private bool AddShader(string name)
        {
            var vsh = shaderPath + name + ".vsh";
            var fsh = shaderPath + name + ".fsh";
            if (!File.Exists(vsh) || !File.Exists(fsh))
            {
                MessageHandler.Instance.AddMessage(MessageType.ERROR, string.Format("Shader \"{0}\" not supported.", name));
                return false;
            }
            shaders[name] = new Shader(name, vsh, fsh);
            MessageHandler.Instance.AddMessage(MessageType.LOG, string.Format("Shader \"{0}\" loaded into pool.", name));

            return true;
        }

        public Shader GetShader(string name)
        {
            if (!shaders.ContainsKey(name))
            {
                if (!AddShader(name))
                    return null;
            }
               
            return shaders[name];
        }

        public void Bind(string name)
        { 
            var shader = GetShader(name);
            shader.Bind();
        }
    }
}
