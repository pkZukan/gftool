using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace GFTool.Renderer.Core.Graphics
{
    public class Shader
    {
        public readonly int Handle;
        private readonly string Name;

        private readonly Dictionary<string, int> uniformLocations = null;
        private readonly Dictionary<string, ActiveUniformType> uniformTypes = null;
        private Dictionary<string, string>? uniformArray0NameCache;
        private readonly HashSet<string> missingUniforms = new HashSet<string>();

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
                uniformTypes = new Dictionary<string, ActiveUniformType>();

                for (var i = 0; i < numberOfUniforms; i++)
                {
                    var key = GL.GetActiveUniform(Handle, i, out _, out ActiveUniformType type);
                    var location = GL.GetUniformLocation(Handle, key);
                    uniformLocations.Add(key, location);
                    uniformTypes[key] = type;
                }
            }
            catch (Exception ex)
            {
                MessageHandler.Instance.AddMessage(MessageType.ERROR, $"Shader \"{name}\" failed to compile: {ex.Message}");
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
            if (uniformLocations == null)
            {
                return false;
            }

            if (TryGetUniformLocation(name, out _))
            {
                return true;
            }

            if (!missingUniforms.Contains(name))
            {
                missingUniforms.Add(name);
                MessageHandler.Instance.AddMessage(MessageType.WARNING, string.Format("Uniform {0} does not exist in shader {1}.", name, Name));
            }

            return false;
        }

        private bool TryGetUniformLocation(string name, out int location)
        {
            location = -1;
            if (uniformLocations == null)
            {
                return false;
            }

            if (uniformLocations.TryGetValue(name, out location))
            {
                return true;
            }

            // Some drivers expose single samplers as `name[0]` in the active uniform list.
            // Treat array[0] as equivalent for uniform lookups.
            return uniformLocations.TryGetValue(GetUniformArray0Name(name), out location);
        }

        public bool TryGetUniformType(string name, out ActiveUniformType type)
        {
            type = default;
            if (uniformTypes == null)
            {
                return false;
            }

            if (uniformTypes.TryGetValue(name, out type))
            {
                return true;
            }

            return uniformTypes.TryGetValue(GetUniformArray0Name(name), out type);
        }

        private string GetUniformArray0Name(string name)
        {
            uniformArray0NameCache ??= new Dictionary<string, string>(StringComparer.Ordinal);
            if (uniformArray0NameCache.TryGetValue(name, out var cached))
            {
                return cached;
            }

            var arrayName = name + "[0]";
            uniformArray0NameCache[name] = arrayName;
            return arrayName;
        }

        public void Bind()
        {
            GL.UseProgram(Handle);
        }

        public string DebugName => Name;

        public bool HasUniformFast(string name)
        {
            if (uniformLocations == null)
            {
                return false;
            }

            return uniformLocations.ContainsKey(name) || uniformLocations.ContainsKey(GetUniformArray0Name(name));
        }

        public void Unbind()
        {
            GL.UseProgram(0);
        }

        public void SetBool(string name, bool data)
        {
            if (TryGetUniformLocation(name, out int location))
            {
                GL.Uniform1(location, data ? 1 : 0);
            }
            else
            {
                HasUniform(name);
            }
        }

        public void SetBoolIfExists(string name, bool data)
        {
            if (TryGetUniformLocation(name, out int location))
            {
                GL.Uniform1(location, data ? 1 : 0);
            }
        }

        public void SetInt(string name, int data)
        {
            if (TryGetUniformLocation(name, out int location))
            {
                GL.Uniform1(location, data);
            }
            else
            {
                HasUniform(name);
            }
        }

        public void SetIntIfExists(string name, int data)
        {
            if (TryGetUniformLocation(name, out int location))
            {
                GL.Uniform1(location, data);
            }
        }

        public void SetFloat(string name, float data)
        {
            if (TryGetUniformLocation(name, out int location))
            {
                GL.Uniform1(location, data);
            }
            else
            {
                HasUniform(name);
            }
        }

        public void SetFloatIfExists(string name, float data)
        {
            if (TryGetUniformLocation(name, out int location))
            {
                GL.Uniform1(location, data);
            }
        }

        public void SetMatrix4(string name, Matrix4 data)
        {
            if (TryGetUniformLocation(name, out int location))
            {
                GL.UniformMatrix4(location, false, ref data);
            }
            else
            {
                HasUniform(name);
            }
        }

        public void SetMatrix4ArrayIfExists(string name, Matrix4[] data)
        {
            SetMatrix4ArrayIfExists(name, data, false);
        }

        public void SetMatrix4ArrayIfExists(string name, Matrix4[] data, bool transpose)
        {
            if (data == null || data.Length == 0)
            {
                return;
            }

            if (TryGetUniformLocation(name, out int location) || TryGetUniformLocation(GetUniformArray0Name(name), out location))
            {
                GL.UniformMatrix4(location, data.Length, transpose, ref data[0].Row0.X);
            }
        }

        public void SetVector3(string name, Vector3 data)
        {
            if (TryGetUniformLocation(name, out int location))
            {
                GL.Uniform3(location, data);
            }
            else
            {
                HasUniform(name);
            }
        }

        public void SetVector3IfExists(string name, Vector3 data)
        {
            if (TryGetUniformLocation(name, out int location))
                GL.Uniform3(location, data);
        }

        public void SetVector2(string name, Vector2 data)
        {
            if (TryGetUniformLocation(name, out int location))
            {
                GL.Uniform2(location, data);
            }
            else
            {
                HasUniform(name);
            }
        }

        public void SetVector2IfExists(string name, Vector2 data)
        {
            if (TryGetUniformLocation(name, out int location))
                GL.Uniform2(location, data);
        }

        public void SetVector4(string name, Vector4 data)
        {
            if (TryGetUniformLocation(name, out int location))
            {
                GL.Uniform4(location, data);
            }
            else
            {
                HasUniform(name);
            }
        }

        public void SetVector4IfExists(string name, Vector4 data)
        {
            if (TryGetUniformLocation(name, out int location))
                GL.Uniform4(location, data);
        }
    }
}
