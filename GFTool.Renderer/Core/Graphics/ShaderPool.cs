using System.Linq;
using GFTool.Renderer.Core;
using OpenTK.Graphics.OpenGL4;

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

        public void Invalidate(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return;
            }

            if (!shaders.TryGetValue(name, out var shader) || shader == null)
            {
                return;
            }

            shaders.Remove(name);

            // Best-effort cleanup; requires a valid GL context.
            try
            {
                GL.DeleteProgram(shader.Handle);
            }
            catch
            {
                // Ignore.
            }
        }

        public void Clear()
        {
            foreach (var name in shaders.Keys.ToArray())
            {
                Invalidate(name);
            }
        }

        private bool AddShader(string name)
        {
            string vsh;
            string fsh;

            if (string.Equals(name, "IkCharacter", StringComparison.OrdinalIgnoreCase))
            {
                // Allow quick A/B comparisons while iterating on IkCharacter.
                // Uses the backed-up sources copied to output at `Shaders/_ShaderBackups/IkCharacter.*.bak`.
                if (RenderOptions.UseBackupIkCharacterShader)
                {
                    vsh = Path.Combine(shaderPath, "_ShaderBackups", "IkCharacter.vsh.bak");
                    fsh = Path.Combine(shaderPath, "_ShaderBackups", "IkCharacter.fsh.bak");
                    goto LoadShader;
                }

                // Default IkCharacter shader is the legacy highlights shader.
                vsh = Path.Combine(shaderPath, "IkCharacterLegacy.vsh");
                fsh = Path.Combine(shaderPath, "IkCharacterLegacy.fsh");
                goto LoadShader;
            }

            vsh = Path.Combine(shaderPath, name + ".vsh");
            fsh = Path.Combine(shaderPath, name + ".fsh");

        LoadShader:
            if (!File.Exists(vsh) || !File.Exists(fsh))
            {
                MessageHandler.Instance.AddMessage(MessageType.ERROR, string.Format("Shader \"{0}\" not supported.", name));
                return false;
            }
            var shader = new Shader(name, vsh, fsh);
            if (shader.Handle == 0)
            {
                MessageHandler.Instance.AddMessage(MessageType.ERROR, string.Format("Shader \"{0}\" failed to compile.", name));
                return false;
            }

            shaders[name] = shader;
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
