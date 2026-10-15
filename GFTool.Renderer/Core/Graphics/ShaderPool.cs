

namespace GFTool.Renderer.Core.Graphics
{
    public class ShaderPool
    {
        private static readonly Lazy<ShaderPool> lazy = new Lazy<ShaderPool>(() => new ShaderPool());
        public static ShaderPool Instance { get { return lazy.Value; } }

        private Dictionary<string, Shader> shaders = new Dictionary<string, Shader>();

        private string shaderPath;
        private readonly HashSet<string> failedShaders = new(StringComparer.OrdinalIgnoreCase);

        private ShaderPool()
        {
            shaderPath = ShaderSource.DirectoryPath + Path.DirectorySeparatorChar;
        }

        private bool AddShader(string name)
        {
            if (!File.Exists(shaderPath + name + ".glsl"))
            {
                MessageHandler.Instance.AddMessage(MessageType.ERROR, string.Format("Shader \"{0}\" not supported.", name));
                return false;
            }
            try
            {
                var source = ShaderSource.GetProgram(name);
                shaders[name] = new Shader(name, source.Vertex, source.Fragment, sourceText: true);
            }
            catch { return false; }
            MessageHandler.Instance.AddMessage(MessageType.LOG, string.Format("Shader \"{0}\" loaded into pool.", name));

            return true;
        }

        public Shader GetShader(string name)
        {

            if (failedShaders.Contains(name)) return null;
            if (!shaders.ContainsKey(name))
            {
                if (!AddShader(name))
                {
                    failedShaders.Add(name);
                    return null;
                }
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
