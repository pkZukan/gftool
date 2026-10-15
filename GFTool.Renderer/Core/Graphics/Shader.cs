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
        public string ProgramName => Name;

        private readonly Dictionary<string, int> uniformLocations = null;
        private readonly HashSet<string> missingUniforms = new HashSet<string>();
        private readonly Dictionary<string, ActiveUniformType> uniformTypes = new();
        private readonly Dictionary<string, (ActiveUniformType Type, float[] Values, int Integer)> defaults = new();
        private readonly Dictionary<string, int> samplerBindings = new();
        public IReadOnlyDictionary<string, int> SamplerBindings => samplerBindings;
        public object? MaterialOwner { get; set; }

        public Shader(string name, string vertPath, string fragPath, bool sourceText = false)
        {
            Name = name;
            int vertexShader = 0, fragmentShader = 0;
            try
            {
                //Create shader
                var shaderSource = sourceText ? vertPath : ShaderSource.Expand(vertPath);
                vertexShader = GL.CreateShader(ShaderType.VertexShader);

                GL.ShaderSource(vertexShader, shaderSource);
                CompileShader(vertexShader);

                shaderSource = sourceText ? fragPath : ShaderSource.Expand(fragPath);
                fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
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
                fragmentShader = vertexShader = 0;

                GL.GetProgram(Handle, GetProgramParameterName.ActiveUniforms, out var numberOfUniforms);

                uniformLocations = new Dictionary<string, int>();

                for (var i = 0; i < numberOfUniforms; i++)
                {
                    var key = GL.GetActiveUniform(Handle, i, out _, out var type);
                    var location = GL.GetUniformLocation(Handle, key);
                    uniformLocations.Add(key, location);
                    uniformTypes[key] = type;
                    if (type is ActiveUniformType.Float or ActiveUniformType.FloatVec2 or ActiveUniformType.FloatVec3 or ActiveUniformType.FloatVec4)
                    {
                        var values = new float[4];
                        GL.GetUniform(Handle, location, values);
                        defaults[key] = (type, values, 0);
                    }
                    else if (type is ActiveUniformType.Int or ActiveUniformType.Bool or ActiveUniformType.Sampler2D)
                    {
                        GL.GetUniform(Handle, location, out int value);
                        defaults[key] = (type, Array.Empty<float>(), value);
                        if (type == ActiveUniformType.Sampler2D) samplerBindings.Add(key, value);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageHandler.Instance.AddMessage(MessageType.ERROR, $"Shader \"{name}\" failed to compile: {ex.Message}");
                if (Handle != 0) GL.DeleteProgram(Handle);
                if (vertexShader != 0) GL.DeleteShader(vertexShader);
                if (fragmentShader != 0) GL.DeleteShader(fragmentShader);
                throw;
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
            if (!ret && !missingUniforms.Contains(name))
            {
                missingUniforms.Add(name);
                MessageHandler.Instance.AddMessage(MessageType.WARNING, string.Format("Uniform {0} does not exist in shader {1}.", name, Name));
            }
            return ret;
        }

        private bool TryGetUniformLocation(string name, out int location)
        {
            location = -1;
            if (uniformLocations == null)
            {
                return false;
            }

            if (!uniformLocations.TryGetValue(name, out location))
            {
                location = GL.GetUniformLocation(Handle, name);
                uniformLocations[name] = location;
            }
            return location >= 0;
        }

        public void ResetMaterialUniforms()
        {
            foreach (var (name, value) in defaults)
            {
                int location = uniformLocations[name];
                switch (value.Type)
                {
                    case ActiveUniformType.Float: GL.Uniform1(location, value.Values[0]); break;
                    case ActiveUniformType.FloatVec2: GL.Uniform2(location, value.Values[0], value.Values[1]); break;
                    case ActiveUniformType.FloatVec3: GL.Uniform3(location, value.Values[0], value.Values[1], value.Values[2]); break;
                    case ActiveUniformType.FloatVec4: GL.Uniform4(location, value.Values[0], value.Values[1], value.Values[2], value.Values[3]); break;
                    default: GL.Uniform1(location, value.Integer); break;
                }
            }
        }

        public void SetOption(string name, string value)
        {
            if (!uniformTypes.TryGetValue(name, out var type)) return;
            if (type == ActiveUniformType.Float && float.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var number)) SetFloatIfExists(name, number);
            else if (MaterialShaderCatalog.OptionValue(name, value) is { } option) SetIntIfExists(name, option);
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

        public void SetBoolIfExists(string name, bool data)
        {
            if (TryGetUniformLocation(name, out int location))
                GL.Uniform1(location, data ? 1 : 0);
        }

        public void SetInt(string name, int data)
        {
            if (HasUniform(name))
                GL.Uniform1(uniformLocations[name], data);
        }

        public void SetIntIfExists(string name, int data)
        {
            if (TryGetUniformLocation(name, out int location))
            {
                if (uniformTypes.GetValueOrDefault(name) == ActiveUniformType.Float) GL.Uniform1(location, (float)data);
                else GL.Uniform1(location, data);
            }
        }

        public void SetFloat(string name, float data)
        {
            if (HasUniform(name))
                GL.Uniform1(uniformLocations[name], data);
        }

        public void SetFloatIfExists(string name, float data)
        {
            if (TryGetUniformLocation(name, out int location))
                GL.Uniform1(location, data);
        }

        public void SetMatrix4(string name, Matrix4 data)
        {
            if (HasUniform(name))
                GL.UniformMatrix4(uniformLocations[name], false, ref data);
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

            if (TryGetUniformLocation(name, out int location) || TryGetUniformLocation($"{name}[0]", out location))
            {
                GL.UniformMatrix4(location, data.Length, transpose, ref data[0].Row0.X);
            }
        }

        public void SetVector3(string name, Vector3 data)
        {
            if (HasUniform(name))
                GL.Uniform3(uniformLocations[name], data);
        }

        public void SetVector3IfExists(string name, Vector3 data)
        {
            if (TryGetUniformLocation(name, out int location))
                GL.Uniform3(location, data);
        }

        public void SetVector2(string name, Vector2 data)
        {
            if (HasUniform(name))
                GL.Uniform2(uniformLocations[name], data);
        }

        public void SetVector2IfExists(string name, Vector2 data)
        {
            if (TryGetUniformLocation(name, out int location))
                GL.Uniform2(location, data);
        }

        public void SetVector4(string name, Vector4 data)
        {
            if (HasUniform(name))
                GL.Uniform4(uniformLocations[name], data);
        }

        public void SetVector4IfExists(string name, Vector4 data)
        {
            if (TryGetUniformLocation(name, out int location))
            {
                if (uniformTypes.GetValueOrDefault(name) == ActiveUniformType.FloatVec3) GL.Uniform3(location, data.X, data.Y, data.Z);
                else GL.Uniform4(location, data);
            }
        }
    }
}
