using GFTool.Renderer.Core;
using GFTool.Renderer.Core.Graphics;

namespace GFTool.Renderer
{
    public partial class RenderContext
    {
        private readonly GlWorkQueue glWorkQueue = new GlWorkQueue();

        internal void EnqueueGlWork(IGlWorkItem item)
        {
            glWorkQueue.Enqueue(item);
        }

        private void ProcessAsyncGlWork()
        {
            if (!RenderOptions.EnableAsyncResourceLoading)
            {
                return;
            }

            glWorkQueue.Process(RenderOptions.AsyncGpuWorkBudgetMs);
        }
    }
}
