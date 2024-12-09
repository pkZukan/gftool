using GFTool.Renderer.Scene;
using GFTool.Renderer.Scene.GraphicsObjects;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using System.Drawing;

namespace GFTool.Renderer
{
    public class RenderContext : IDisposable
    {
        private IGraphicsContext viewport = null;
        private int Width, Height;

        GBuffer gbuffer;
        public Camera camera { get; private set; }

        public RenderContext(IGLFWGraphicsContext ctxt, int width, int height)
        {
            Width = width;
            Height = height;
            viewport = ctxt;
        }

        //Render init
        public void Setup()
        {
            //Create camera and add to root scene
            camera = new Camera(Width, Height);
            SceneGraph.Instance.GetRoot().AddChild(camera);

            GL.Enable(EnableCap.DepthTest);
            GL.ClearDepth(1.0f);
            GL.DepthFunc(DepthFunction.Lequal);
            GL.ClearColor(Color.Gray);

            //Set viewport size
            Resize(Width, Height);
        }

        //Render loop
        public void Update()
        {
            if (viewport == null) return;

            //Update VP mat
            camera.Update();

            //Bind viewport
            viewport.MakeCurrent();

            //Bind GBuf and clear it
            gbuffer.BindFBO();
            gbuffer.Clear();

            //Various passes
            GeometryPass();
            LightingPass();
            FinalPass();

            //Render
            gbuffer.BindDefaultFB();
            Render();
        }

        private void GeometryPass()
        {
            //TODO: Traverse scene and only draw geometry (eventually)
            foreach (var c in SceneGraph.Instance.GetRoot().children)
            {
                c.Draw(camera.viewMat, camera.projMat);
            }
        }

        private void LightingPass()
        { 
            //
        }

        private void FinalPass()
        {
            gbuffer.Draw();
        }

        public Model AddSceneModel(string file)
        {
            //TODO: Probably figure out how we're adding shit to child nodes (assuming necessary at this level)

            var mdl = new Model(file);
            SceneGraph.Instance.GetRoot().AddChild(mdl);

            return mdl;
        }

        public void RemoveSceneModel(Model mdl)
        {
            var root = SceneGraph.Instance.GetRoot();
            if (root.children.Contains(mdl)) 
                root.children.Remove(mdl);
        }

        public void ClearScene()
        {
            SceneGraph.Instance.GetRoot().children.Clear();
        }

        public void SetGBufferDisplayMode(GBuffer.DisplayType displayType)
        {
            gbuffer.DisplayMode = displayType;
        }

        public void SetWireframe(bool b)
        {
            //TODO: Solid wireframe shader
        }

        private void Render()
        {
            viewport.SwapBuffers();
        }

        public void Resize(int width, int height)
        {
            //Create GBuffer
            gbuffer = new GBuffer(width, height);

            GL.Viewport(0, 0, width, height);
        }

        public void Dispose()
        {
            gbuffer.Dispose();
        }
    }
}
