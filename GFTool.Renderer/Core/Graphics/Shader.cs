using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System.Runtime.CompilerServices;

namespace GFTool.Renderer.Core.Graphics
{
    public class Shader
    {
        public readonly int Handle;
        private readonly string Name;

        private readonly Dictionary<string, int> uniformLocations = null;

        public Shader(string name, string vertPath, string fragPath)
        {
            Name = name;
            try
            {
                //Create shader
                var shaderSource = File.ReadAllText(vertPath);
                var vertexShader = GL.CreateShader(ShaderType.VertexShader);

                GL.ShaderSource(vertexShader, shaderSource);
                CompileShader(vertexShader);

                shaderSource = File.ReadAllText(fragPath);
                var fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
                GL.ShaderSource(fragmentShader, shaderSource);
                CompileShader(fragmentShader);

                Handle = GL.CreateProgram();

                GL.AttachShader(Handle, vertexShader);
                GL.AttachShader(Handle, fragmentShader);

                LinkProgram(Handle);

                GL.DetachShader(Handle, vertexShader);
                GL.DetachShader(Handle, fragmentShader);
                GL.DeleteShader(fragmentShader);
                GL.DeleteShader(vertexShader);

                GL.GetProgram(Handle, GetProgramParameterName.ActiveUniforms, out var numberOfUniforms);

                uniformLocations = new Dictionary<string, int>();

                for (var i = 0; i < numberOfUniforms; i++)
                {
                    var key = GL.GetActiveUniform(Handle, i, out _, out _);
                    var location = GL.GetUniformLocation(Handle, key);
                    uniformLocations.Add(key, location);
                }
            }
            catch (Exception ex)
            {
                //
            }
        }

        private static void CompileShader(int shader)
        {
            GL.CompileShader(shader);

            GL.GetShader(shader, ShaderParameter.CompileStatus, out var code);
            if (code != (int)All.True)
            {
                var infoLog = GL.GetShaderInfoLog(shader);
                throw new Exception($"Error occurred whilst compiling Shader({shader}).\n\n{infoLog}");
            }
        }

        private static void LinkProgram(int program)
        {
            GL.LinkProgram(program);

            GL.GetProgram(program, GetProgramParameterName.LinkStatus, out var code);
            if (code != (int)All.True)
            {
                var err = GL.GetProgramInfoLog(program);
                throw new Exception($"Error occurred whilst linking Program({program}): {err}");
            }
        }

        private bool HasUniform(string name)
        {
            if (uniformLocations == null) return false; ;

            bool ret = uniformLocations.ContainsKey(name);
            if (!ret)
                MessageHandler.Instance.AddMessage(MessageType.WARNING, string.Format("Uniform {0} does not exist in shader {1}.", name, Name));
            return ret;
        }

        public void Bind()
        {
            GL.UseProgram(Handle);
        }

        public void Unbind()
        {
            GL.UseProgram(0);
        }

        public void SetBool(string name, bool data)
        {
            if (HasUniform(name))
                GL.Uniform1(uniformLocations[name], data ? 1 : 0);
        }

        public void SetInt(string name, int data)
        {
            if (HasUniform(name))
                GL.Uniform1(uniformLocations[name], data);
        }

        public void SetFloat(string name, float data)
        {
            if (HasUniform(name))
                GL.Uniform1(uniformLocations[name], data);
        }

        public void SetMatrix4(string name, Matrix4 data)
        {
            if (HasUniform(name))
                GL.UniformMatrix4(uniformLocations[name], false, ref data);
        }

        public void SetVector3(string name, Vector3 data)
        {
            if (HasUniform(name))
                GL.Uniform3(uniformLocations[name], data);
        }
    }
}
