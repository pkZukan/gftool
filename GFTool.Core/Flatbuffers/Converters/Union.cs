using FlatSharp;
using System.Text.Json;
using System.Text.Json.Serialization;

using GFTool.Core.Flatbuffers.TR.Animation;
using GFTool.Core.Utils;

namespace GFTool.Core.Flatbuffers.Converters
{
    public class UnionConverterFactory : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert)
        {
            return typeToConvert == typeof(IFlatBufferUnion);
        }

        public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }
    }

    public class VectorTrackUnionConverter : JsonConverter<FlatBufferUnion<FixedVectorTrack, FramedVectorTrack, Keyed16VectorTrack, Keyed8VectorTrack>>
    {
        public override FlatBufferUnion<FixedVectorTrack, FramedVectorTrack, Keyed16VectorTrack, Keyed8VectorTrack> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            Dictionary<string, JsonElement> dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(ref reader, options);
            byte discriminator = dict["Discriminator"].Deserialize<byte>();
            switch (discriminator)
            {
                case 1:
                    return new FlatBufferUnion<FixedVectorTrack, FramedVectorTrack, Keyed16VectorTrack, Keyed8VectorTrack>(dict["Values"].Deserialize<FixedVectorTrack>());
                case 2:
                    return new FlatBufferUnion<FixedVectorTrack, FramedVectorTrack, Keyed16VectorTrack, Keyed8VectorTrack>(dict["Values"].Deserialize<FramedVectorTrack>());
                case 3:
                    return new FlatBufferUnion<FixedVectorTrack, FramedVectorTrack, Keyed16VectorTrack, Keyed8VectorTrack>(dict["Values"].Deserialize<Keyed16VectorTrack>());
                case 4:
                    return new FlatBufferUnion<FixedVectorTrack, FramedVectorTrack, Keyed16VectorTrack, Keyed8VectorTrack>(dict["Values"].Deserialize<Keyed8VectorTrack>());
                default:
                    return new FlatBufferUnion<FixedVectorTrack, FramedVectorTrack, Keyed16VectorTrack, Keyed8VectorTrack>();
            }
        }

        public override void Write(Utf8JsonWriter writer, FlatBufferUnion<FixedVectorTrack, FramedVectorTrack, Keyed16VectorTrack, Keyed8VectorTrack> value, JsonSerializerOptions options)
        {
            Dictionary<string, Object> dict = new Dictionary<string, Object>();
            dict["Discriminator"] = value.Discriminator;
            value.Switch(
                defaultCase: () => { },
                case1: (FixedVectorTrack v) => { dict["Values"] = v; },
                case2: (FramedVectorTrack v) => { dict["Values"] = v; },
                case3: (Keyed16VectorTrack v) => { dict["Values"] = v; },
                case4: (Keyed8VectorTrack v) => { dict["Values"] = v; }
                );
            JsonSerializer.Serialize(writer, dict);

        }
    }

    public class RotationTrackUnionConverter : JsonConverter<FlatBufferUnion<FixedRotationTrack, FramedRotationTrack, Keyed16RotationTrack, Keyed8RotationTrack>>
    {
        public override FlatBufferUnion<FixedRotationTrack, FramedRotationTrack, Keyed16RotationTrack, Keyed8RotationTrack> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            Dictionary<string, JsonElement> dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(ref reader, options);
            byte discriminator = dict["Discriminator"].Deserialize<byte>();
            switch (discriminator)
            {
                case 1:
                    return new FlatBufferUnion<FixedRotationTrack, FramedRotationTrack, Keyed16RotationTrack, Keyed8RotationTrack>(dict["Values"].Deserialize<FixedRotationTrack>());
                case 2:
                    return new FlatBufferUnion<FixedRotationTrack, FramedRotationTrack, Keyed16RotationTrack, Keyed8RotationTrack>(dict["Values"].Deserialize<FramedRotationTrack>());
                case 3:
                    return new FlatBufferUnion<FixedRotationTrack, FramedRotationTrack, Keyed16RotationTrack, Keyed8RotationTrack>(dict["Values"].Deserialize<Keyed16RotationTrack>());
                case 4:
                    return new FlatBufferUnion<FixedRotationTrack, FramedRotationTrack, Keyed16RotationTrack, Keyed8RotationTrack>(dict["Values"].Deserialize<Keyed8RotationTrack>());
                default:
                    return new FlatBufferUnion<FixedRotationTrack, FramedRotationTrack, Keyed16RotationTrack, Keyed8RotationTrack>();
            }
        }

        public override void Write(Utf8JsonWriter writer, FlatBufferUnion<FixedRotationTrack, FramedRotationTrack, Keyed16RotationTrack, Keyed8RotationTrack> value, JsonSerializerOptions options)
        {
            Dictionary<string, Object> dict = new Dictionary<string, Object>();
            dict["Discriminator"] = value.Discriminator;
            value.Switch(
                defaultCase: () => { },
                case1: (FixedRotationTrack v) => { dict["Values"] = v; },
                case2: (FramedRotationTrack v) => { dict["Values"] = v; },
                case3: (Keyed16RotationTrack v) => { dict["Values"] = v; },
                case4: (Keyed8RotationTrack v) => { dict["Values"] = v; }
            );
            JsonSerializer.Serialize(writer, dict);
        }
    }
}
