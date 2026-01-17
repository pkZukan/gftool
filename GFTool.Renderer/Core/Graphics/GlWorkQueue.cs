using System;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace GFTool.Renderer.Core.Graphics
{
    internal interface IGlWorkItem
    {
        bool Step();
    }

    internal sealed class GlWorkQueue
    {
        private readonly ConcurrentQueue<IGlWorkItem> pending = new ConcurrentQueue<IGlWorkItem>();
        private readonly object gate = new object();
        private IGlWorkItem? active;

        public void Enqueue(IGlWorkItem item)
        {
            if (item == null)
            {
                return;
            }

            pending.Enqueue(item);
        }

        public void Process(float budgetMs)
        {
            if (budgetMs <= 0.0f)
            {
                return;
            }

            long start = Stopwatch.GetTimestamp();
            double ticksPerMs = Stopwatch.Frequency / 1000.0;
            long budgetTicks = (long)(budgetMs * ticksPerMs);

            while (Stopwatch.GetTimestamp() - start < budgetTicks)
            {
                IGlWorkItem? current;
                lock (gate)
                {
                    if (active == null)
                    {
                        if (!pending.TryDequeue(out active))
                        {
                            return;
                        }
                    }

                    current = active;
                }

                bool done = false;
                try
                {
                    done = current.Step();
                }
                catch
                {
                    done = true;
                }

                if (done)
                {
                    lock (gate)
                    {
                        if (ReferenceEquals(active, current))
                        {
                            active = null;
                        }
                    }
                }
                else
                {
                    return;
                }
            }
        }
    }
}
