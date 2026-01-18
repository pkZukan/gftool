using System.Runtime.InteropServices;

namespace Trinity.Core.Utils
{
    public static class StructConverter
    {
        public static T ToStruct<T>(this byte[] bytearray) where T : struct
        {
            var handle = GCHandle.Alloc(bytearray, GCHandleType.Pinned);
            try
            {
                return Marshal.PtrToStructure<T>(handle.AddrOfPinnedObject())!;
            }
            finally
            {
                handle.Free();
            }
        }

        public static byte[] ToBytes<T>(this T obj) where T : struct
        {
            int size = Marshal.SizeOf(obj);
            byte[] arr = new byte[size];

            IntPtr ptr = Marshal.AllocHGlobal(size);
            try
            {
                Marshal.StructureToPtr(obj, ptr, true);
                Marshal.Copy(ptr, arr, 0, size);
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
            return arr;
        }
    }
}
