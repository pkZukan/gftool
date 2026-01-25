using System.Diagnostics;
using FlatSharp;
using Trinity.Core.Flatbuffers.Reflections;
using Trinity.Core.Flatbuffers.TR.Model;

namespace Trinity.Core.Utils
{
    public static class FlatSharpWarmup
    {
        private static readonly object Gate = new();
        private static Task? warmupTask;

        public static Task EnsureTrinityModelSerializersWarmedUp(Action<string>? log = null, CancellationToken cancellationToken = default)
        {
            lock (Gate)
            {
                warmupTask ??= Task.Run(() => WarmUpTrinityModelSerializers(log, cancellationToken), cancellationToken);
                return warmupTask;
            }
        }

        private static void WarmUpTrinityModelSerializers(Action<string>? log, CancellationToken cancellationToken)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Common Trinity model stack (SV/ZA):
                // - TRMDL references TRMSH/TRMBF/TRSKL/TRMTR/TRMMT
                // - TRMMT has two flavors (set mapping + metadata/variation)
                // - Reflection schemas (.bfbs) are used for flatc-like JSON viewing
                // FlatSharp's serializer generation happens lazily on first Parse/Serialize.
                // To avoid relying on internal APIs, we intentionally trigger Parse<T> with a dummy buffer and
                // ignore the inevitable failure; by then the typed serializer has been compiled and cached.
                var dummy = new byte[32];
                WarmUpByParsing<TRMDL>(dummy, cancellationToken);
                WarmUpByParsing<TRMSH>(dummy, cancellationToken);
                WarmUpByParsing<TRMBF>(dummy, cancellationToken);
                WarmUpByParsing<TRSKL>(dummy, cancellationToken);
                WarmUpByParsing<TrmtrFile>(dummy, cancellationToken);
                WarmUpByParsing<TrmmtFile>(dummy, cancellationToken);
                WarmUpByParsing<TrmmtMetadataFile>(dummy, cancellationToken);
                WarmUpByParsing<ReflectionSchema>(dummy, cancellationToken);

                log?.Invoke($"[Warmup] FlatSharp serializers ready in {sw.ElapsedMilliseconds}ms");
            }
            catch (OperationCanceledException)
            {
                log?.Invoke("[Warmup] FlatSharp warmup canceled");
            }
            catch (Exception ex)
            {
                log?.Invoke($"[Warmup] FlatSharp warmup failed: {ex.Message}");
            }
        }

        private static void WarmUpByParsing<T>(byte[] dummy, CancellationToken cancellationToken) where T : class
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                _ = FlatBufferSerializer.Default.Parse<T>(dummy);
            }
            catch
            {
                // Expected: dummy buffers don't represent valid FlatBuffers.
            }
        }
    }
}
