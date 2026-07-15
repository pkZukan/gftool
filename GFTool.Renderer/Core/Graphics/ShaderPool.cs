namespace GFTool.Renderer.Core.Graphics
{
    public class ShaderPool
    {
        private static readonly Lazy<ShaderPool> lazy = new Lazy<ShaderPool>(() => new ShaderPool());
        public static ShaderPool Instance { get { return lazy.Value; } }

        private Dictionary<string, Shader> shaders = new Dictionary<string, Shader>();

        private string shaderPath;

        private ShaderPool()
        {
            shaderPath = Path.Combine(AppContext.BaseDirectory, "Shaders");
        }

        private bool AddShader(string name)
        {
            var vsh = Path.Combine(shaderPath, name + ".vsh");
            var fsh = Path.Combine(shaderPath, name + ".fsh");
            if (!File.Exists(vsh) || !File.Exists(fsh))
            {
                MessageHandler.Instance.AddMessage(MessageType.ERROR, $"Shader \"{name}\" not found in \"{shaderPath}\".");
                return false;
            }
            var shader = new Shader(name, vsh, fsh);
            if (!shader.IsValid)
            {
                return false;
            }

            shaders[name] = shader;
            MessageHandler.Instance.AddMessage(MessageType.LOG, string.Format("Shader \"{0}\" loaded into pool.", name));

            return true;
        }

        public Shader? GetShader(string name)
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
            if (shader == null)
            {
                return;
            }

            shader.Bind();
        }
    }
}
