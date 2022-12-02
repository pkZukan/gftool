using FlatSharp;

namespace Trinity.Core.Utils
{
        public static class FlatBufferConverter
        {
            public static T DeserializeFrom<T>(string file) where T : class
            {
                var data = File.ReadAllBytes(file);
                return DeserializeFrom<T>(data);
            }
            public static T DeserializeFrom<T>(byte[] data) where T : class
            {
                return FlatBufferSerializer.Default.Parse<T>(data);
            }
            public static byte[] SerializeFrom<T>(T obj) where T : class
            {
                var size = FlatBufferSerializer.Default.GetMaxSize(obj);
                var data = new byte[size];
                var result = FlatBufferSerializer.Default.Serialize(obj, data);
                if (result != data.Length)
                    Array.Resize(ref data, result);
                return data;
            }
        }

}
