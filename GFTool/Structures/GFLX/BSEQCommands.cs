
using System.Runtime.InteropServices;
using GFTool.Utils;

namespace GFTool.Structures.GFLX
{
    public static class BSEQCommands
    {
        private static Dictionary<ulong, String> GetBSEQCommandName() => new Dictionary<ulong, String>
        {
            { 0xd72313dcea8f6312, "CreateModel"}, //"CreateModel"
            { 0xf78744ce3ad97ce0, "CreateModelAnimation"}, //"CreateModelAnimation"
            { 0xaf8d93f685c2ff1c, "CameraAnimation"}, //"CameraAnimation"
            { 0x4938ee1ef8c2a2a8, "SoundSetSwitch"}, //"SoundSetSwitch"
        };

        private static Dictionary<ulong, BSEQReader> GetBSEQCommandArguments() => new Dictionary<ulong, BSEQReader>
        {
            { 0xd72313dcea8f6312, new BSEQReader(b => b.ToStruct<BSEQCreateModel>())}, //"CreateModel"
            { 0xf78744ce3ad97ce0, new BSEQReader(b => b.ToStruct<BSEQCreateModelAnimation>())}, //"CreateModelAnimation"
            { 0xaf8d93f685c2ff1c, new BSEQReader(b => b.ToStruct<BSEQCameraAnimation>())}, //"CameraAnimation"
            { 0x4938ee1ef8c2a2a8, new BSEQReader(b => b.ToStruct<BSEQSoundSetSwitch>())}, //"SoundSetSwitch"
        };

        public interface IBSEQArgReader
        {

        }

        [StructLayout(LayoutKind.Sequential)]
        public struct BSEQCreateModel : IBSEQArgReader
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string ModelPath;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct BSEQCreateModelAnimation : IBSEQArgReader
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string ModelAnimationPath;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct BSEQCameraAnimation : IBSEQArgReader
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string CameraAnimationPath;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct BSEQSoundSetSwitch : IBSEQArgReader
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string SoundSwitchFlag;
        }
        public static IBSEQArgReader? Parse(ulong hash, byte[] buffer)
        {
            Dictionary<ulong, BSEQReader> parser = GetBSEQCommandArguments();
            if (!parser.ContainsKey(hash))
            {
                return new BSEQUnknownCommand(buffer);
            }
            return parser[hash].Reader(buffer);
        }

        public class BSEQUnknownCommand : IBSEQArgReader
        {
            public byte[] Buffer { get; }
            public BSEQUnknownCommand(byte[] buffer)
            {
                Buffer = buffer;
            }
        }

        private class BSEQReader
        {
            public Func<byte[], IBSEQArgReader> Reader { get; }
            public BSEQReader(Func<byte[], IBSEQArgReader> reader)
            {
                Reader = reader;
            }
        }
    }

}
