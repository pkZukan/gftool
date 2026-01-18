using GFTool.Core.Utils;
using GFTool.Renderer.Core;
using GFTool.Renderer.Core.Graphics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Globalization;
using System.Drawing;
using System.Linq;
using Trinity.Core.Flatbuffers.TR.Model;
using Trinity.Core.Flatbuffers.Utils;
using Trinity.Core.Assets;

namespace GFTool.Renderer.Scene.GraphicsObjects
{
    public partial class Material : IDisposable
    {
        public bool TryGetUniformOverride(string name, out object value)
        {
            lock (overrideLock)
            {
                return uniformOverrides.TryGetValue(name, out value);
            }
        }

        public bool HasUniformOverrides
        {
            get
            {
                lock (overrideLock)
                {
                    return uniformOverrides.Count > 0;
                }
            }
        }

        public KeyValuePair<string, object>[] GetUniformOverridesSnapshot()
        {
            lock (overrideLock)
            {
                return uniformOverrides.ToArray();
            }
        }

        public bool TryGetShaderParamIntEffective(string name, out int value)
        {
            return TryGetShaderParamIntWithOverrides(name, out value);
        }

        public void SetUniformOverride(string name, object value)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return;
            }

            lock (overrideLock)
            {
                uniformOverrides[name] = value;
            }
        }

        public void ClearUniformOverride(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return;
            }

            lock (overrideLock)
            {
                uniformOverrides.Remove(name);
            }
        }

        private void ApplyUniformOverrides(Shader activeShader)
        {
            KeyValuePair<string, object>[] snapshot;
            lock (overrideLock)
            {
                if (uniformOverrides.Count == 0)
                {
                    return;
                }
                snapshot = uniformOverrides.ToArray();
            }

            for (int i = 0; i < snapshot.Length; i++)
            {
                var (name, value) = (snapshot[i].Key, snapshot[i].Value);
                if (string.IsNullOrWhiteSpace(name) || reservedOverrideUniformNames.Contains(name))
                {
                    continue;
                }

                activeShader.TryGetUniformType(name, out var uniformType);

                void SetIntOrFloat(int n)
                {
                    if (uniformType == ActiveUniformType.Float)
                    {
                        activeShader.SetFloatIfExists(name, n);
                        return;
                    }
                    activeShader.SetIntIfExists(name, n);
                }

                void SetFloatOrInt(float f)
                {
                    if (uniformType == ActiveUniformType.Int || uniformType == ActiveUniformType.Bool)
                    {
                        activeShader.SetIntIfExists(name, (int)MathF.Round(f));
                        return;
                    }
                    activeShader.SetFloatIfExists(name, f);
                }

                void SetBoolOrFloat(bool b)
                {
                    if (uniformType == ActiveUniformType.Float)
                    {
                        activeShader.SetFloatIfExists(name, b ? 1.0f : 0.0f);
                        return;
                    }
                    activeShader.SetBoolIfExists(name, b);
                }

                switch (value)
                {
                    case bool b:
                        SetBoolOrFloat(b);
                        break;
                    case int n:
                        SetIntOrFloat(n);
                        break;
                    case float f:
                        SetFloatOrInt(f);
                        break;
                    case Vector2 v2:
                        activeShader.SetVector2IfExists(name, v2);
                        break;
                    case Vector3 v3:
                        activeShader.SetVector3IfExists(name, v3);
                        break;
                    case Vector4 v4:
                        activeShader.SetVector4IfExists(name, v4);
                        break;
                    case string s:
                        {
                            var trimmed = s.Trim();
                            if (bool.TryParse(trimmed, out var sb))
                            {
                                SetBoolOrFloat(sb);
                                break;
                            }

                            if (int.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out var si))
                            {
                                SetIntOrFloat(si);
                                break;
                            }

                            if (float.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out var sf))
                            {
                                SetFloatOrInt(sf);
                            }

                            break;
                        }
                }
            }
        }

    }
}
