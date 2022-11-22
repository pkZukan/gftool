using System;
using System.Runtime.InteropServices;

namespace GFTool.Core.Compression

{
    //Note: Stolen from PkNX....
    //Figure out licensing later

    public static class Oodle
    {
        public const string OodleLibraryPath = "oo2core_8_win64";

        [DllImport(OodleLibraryPath, CallingConvention = CallingConvention.Cdecl)]
        private static extern long OodleLZ_Decompress(ref byte buffer, long bufferSize, ref byte result, long outputBufferSize,
            OodleFuzzSafe fuzz = OodleFuzzSafe.Yes,
            OodleCheckCrc crc = OodleCheckCrc.No,
            OodleVerbosity verbosity = OodleVerbosity.None,
            long context = 0, long e = 0, long callback = 0, long callback_ctx = 0, long scratch = 0, long scratch_size = 0,
            OodleThreadPhase threadPhase = OodleThreadPhase.Unthreaded);

        [DllImport(OodleLibraryPath)]
        private static extern long OodleLZ_Compress(OodleFormat format, ref byte buffer, long bufferSize, ref byte result, OodleCompressionLevel level,
            long opts = 0, long context = 0, long unused = 0, long scratch = 0, long scratch_size = 0);

        public static byte[]? Decompress(ReadOnlySpan<byte> input, long decompressedLength)
        {
            var result = new byte[decompressedLength];
            return Decompress(input, result);
        }

        private static byte[]? Decompress(ReadOnlySpan<byte> input, byte[] result)
        {
            var dest = result.AsSpan();
            long decodedSize = OodleLZ_Decompress(ref MemoryMarshal.GetReference(input), input.Length, ref MemoryMarshal.GetReference(dest), result.Length);
            if (decodedSize == 0)
                return null; // failed
            return result;
        }

        public static Span<byte> Compress(ReadOnlySpan<byte> input, out int compressedSize,
            OodleFormat format = OodleFormat.Kraken, OodleCompressionLevel level = OodleCompressionLevel.Optimal2)
        {
            var maxSize = GetCompressedBufferSizeNeeded(input.Length);
            var result = new byte[maxSize].AsSpan();
            return Compress(input, result, out compressedSize, format, level);
        }

        private static Span<byte> Compress(ReadOnlySpan<byte> input, Span<byte> result, out int compressedSize, OodleFormat format, OodleCompressionLevel level)
        {
            var encodedSize = OodleLZ_Compress(format, ref MemoryMarshal.GetReference(input), input.Length, ref MemoryMarshal.GetReference(result), level);

            // Oodle's compressed result leaves data after the "compressed length" return index.
            // Return an aligned span (ensuring length is a multiple of 4).
            // Retaining these unused bytes matches the behavior observed in New Pokémon Snap DRPF files.
            compressedSize = (int)encodedSize;
            var align = (compressedSize + 3) & ~3;
            return result[..align];
        }
        private static long GetCompressedBufferSizeNeeded(long inputSize)
        {
            return inputSize + (274 * ((inputSize + 0x3FFFF) / 0x40000));
        }
    }

    public enum OodleFormat : uint
    {
        LZH = 0,
        LZHLW = 1,
        LZNIB = 2,
        None = 3,
        LZB16 = 4,
        LZBLW = 5,
        LZA = 6,
        LZNA = 7,
        Kraken = 8,
        Mermaid = 9,
        BitKnit = 10,
        Selkie = 11,
        Hydra = 12,
        Leviathan = 13,
    }

    public enum OodleCompressionLevel : ulong
    {
        None = 0,
        SuperFast = 1,
        VeryFast = 2,
        Fast = 3,
        Normal = 4,
        Optimal1 = 5,
        Optimal2 = 6,
        Optimal3 = 7,
        Optimal4 = 8,
        Optimal5 = 9,
    }

    public enum OodleFuzzSafe
    {
        No = 0,
        Yes = 1,
    }

    public enum OodleCheckCrc
    {
        No = 0,
        Yes = 1,
    }

    public enum OodleVerbosity
    {
        None = 0,
        Max = 3,
    }

    [Flags]
    public enum OodleThreadPhase
    {
        Invalid = 0,
        ThreadPhase1 = 1,
        ThreadPhase2 = 2,

        Unthreaded = ThreadPhase1 | ThreadPhase2, // 3
    }
}