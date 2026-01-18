using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Trinity.Core.Flatbuffers.Utils;
using Trinity.Core.Utils;

namespace Trinity.Core.Flatbuffers.Converters
{
    public class QuaternionConverter : JsonConverter<PackedQuaternion>
    {
        public override PackedQuaternion Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var quatdict = JsonSerializer.Deserialize<Dictionary<string, float>>(ref reader, options);
            if (quatdict == null ||
                !quatdict.TryGetValue("X", out var x) ||
                !quatdict.TryGetValue("Y", out var y) ||
                !quatdict.TryGetValue("Z", out var z) ||
                !quatdict.TryGetValue("W", out var w))
            {
                throw new JsonException("Invalid quaternion JSON; expected {X,Y,Z,W}.");
            }

            Quaternion quaternion = new Quaternion(x, y, z, w);
            return quaternion.Pack();
        }

        public override void Write(Utf8JsonWriter writer, PackedQuaternion value, JsonSerializerOptions options)
        {
            Quaternion quaternion = value.Unpack();
            JsonSerializer.Serialize(writer, quaternion.ToDictionary(), options);
        }
    }

}
