using GFTool.Core.Middleware;
using GFTool.Core.Utils;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using Trinity.Core.Flatbuffers.TR.Model;

namespace GFTool.Renderer.Scene.GraphicsObjects
{
    public class Material
    {
        private Shader shader;
        private List<Texture> textures;

        private PathString modelpath;

        private List<Tuple<string, string>> ShaderParams;

        public Material(PathString modelPath, TRMaterial trmat)
        {
            modelpath = modelPath;
            //I hope we dont actually have more than one shader per material
            shader = ShaderPool.Instance.GetShader(trmat.Shader[0].Name);
            ShaderParams = new List<Tuple<string, string>>();
            foreach (var param in trmat.Shader[0].Values)
            {
                ShaderParams.Add(new Tuple<string, string>(param.Name, param.Value));
            }
            textures = new List<Texture>();
            foreach (var tex in trmat.Textures)
            {
                var bntx = new BNTX();
                bntx.LoadFromFile(modelpath.Combine(tex.File));
                textures.Add(new Texture(tex.Name, bntx));
            }
        }

        public void SetUniforms(Matrix4 view, Matrix4 model, Matrix4 proj)
        {
            shader.Bind();
            for (int i = 0; i < textures.Count; i++)
            {
                GL.ActiveTexture(TextureUnit.Texture0 + i);
                GL.BindTexture(TextureTarget.Texture2D, textures[i].textureId);
                shader.SetInt(textures[i].Name, textures[i].textureId);
            }
            shader.SetMatrix4("model", model);
            shader.SetMatrix4("view", view);
            shader.SetMatrix4("projection", proj);
        }
    }
}
