namespace GFTool.Renderer;

/// <summary>Host callbacks for an existing OpenGL context. The host owns its lifetime and bindings.</summary>
public sealed class RenderSurface(Action makeCurrent, Action swapBuffers)
{
    public void MakeCurrent() => makeCurrent();
    public void SwapBuffers() => swapBuffers();
}
