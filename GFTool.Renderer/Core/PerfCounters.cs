using System.Threading;

namespace GFTool.Renderer.Core
{
    public static class PerfCounters
    {
        public static bool Enabled;

        private static int modelDraws;
        private static int submeshDraws;
        private static int drawCalls;
        private static long triangles;
        private static int materialUses;
        private static int textureBinds;
        private static int skinMatrixUploads;

        public static void BeginFrame()
        {
            if (!Enabled)
            {
                return;
            }

            Interlocked.Exchange(ref modelDraws, 0);
            Interlocked.Exchange(ref submeshDraws, 0);
            Interlocked.Exchange(ref drawCalls, 0);
            Interlocked.Exchange(ref triangles, 0);
            Interlocked.Exchange(ref materialUses, 0);
            Interlocked.Exchange(ref textureBinds, 0);
            Interlocked.Exchange(ref skinMatrixUploads, 0);
        }

        public static void RecordModelDraw()
        {
            if (!Enabled) return;
            Interlocked.Increment(ref modelDraws);
        }

        public static void RecordSubmeshDraw()
        {
            if (!Enabled) return;
            Interlocked.Increment(ref submeshDraws);
        }

        public static void RecordDrawCall(int indexCount)
        {
            if (!Enabled) return;
            Interlocked.Increment(ref drawCalls);
            if (indexCount > 0)
            {
                Interlocked.Add(ref triangles, indexCount / 3);
            }
        }

        public static void RecordMaterialUse()
        {
            if (!Enabled) return;
            Interlocked.Increment(ref materialUses);
        }

        public static void RecordTextureBind(int count)
        {
            if (!Enabled) return;
            if (count > 0)
            {
                Interlocked.Add(ref textureBinds, count);
            }
        }

        public static void RecordSkinMatrixUpload()
        {
            if (!Enabled) return;
            Interlocked.Increment(ref skinMatrixUploads);
        }

        public static Snapshot GetSnapshot()
        {
            return new Snapshot(
                ModelDraws: Volatile.Read(ref modelDraws),
                SubmeshDraws: Volatile.Read(ref submeshDraws),
                DrawCalls: Volatile.Read(ref drawCalls),
                Triangles: Volatile.Read(ref triangles),
                MaterialUses: Volatile.Read(ref materialUses),
                TextureBinds: Volatile.Read(ref textureBinds),
                SkinMatrixUploads: Volatile.Read(ref skinMatrixUploads));
        }

        public readonly struct Snapshot
        {
            public readonly int ModelDraws;
            public readonly int SubmeshDraws;
            public readonly int DrawCalls;
            public readonly long Triangles;
            public readonly int MaterialUses;
            public readonly int TextureBinds;
            public readonly int SkinMatrixUploads;

            public Snapshot(int ModelDraws, int SubmeshDraws, int DrawCalls, long Triangles, int MaterialUses, int TextureBinds, int SkinMatrixUploads)
            {
                this.ModelDraws = ModelDraws;
                this.SubmeshDraws = SubmeshDraws;
                this.DrawCalls = DrawCalls;
                this.Triangles = Triangles;
                this.MaterialUses = MaterialUses;
                this.TextureBinds = TextureBinds;
                this.SkinMatrixUploads = SkinMatrixUploads;
            }
        }
    }
}
