using FlatSharp;
using System.Text.Json;
using System.Text.Json.Serialization;

using Trinity.Core.Flatbuffers.TR.Animation;
using Trinity.Core.Utils;

namespace Trinity.Core.Flatbuffers.Converters
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
            Dictionary<string, JsonElement>? dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(ref reader, options);
            if (dict == null ||
                !dict.TryGetValue("Discriminator", out var discEl) ||
                !dict.TryGetValue("Values", out var valuesEl))
            {
                return new FlatBufferUnion<FixedVectorTrack, FramedVectorTrack, Keyed16VectorTrack, Keyed8VectorTrack>();
            }

            byte discriminator = discEl.Deserialize<byte>();
            switch (discriminator)
            {
                case 1:
                    {
                        var value = valuesEl.Deserialize<FixedVectorTrack>(options);
                        return value == null
                            ? new FlatBufferUnion<FixedVectorTrack, FramedVectorTrack, Keyed16VectorTrack, Keyed8VectorTrack>()
                            : new FlatBufferUnion<FixedVectorTrack, FramedVectorTrack, Keyed16VectorTrack, Keyed8VectorTrack>(value);
                    }
                case 2:
                    {
                        var value = valuesEl.Deserialize<FramedVectorTrack>(options);
                        return value == null
                            ? new FlatBufferUnion<FixedVectorTrack, FramedVectorTrack, Keyed16VectorTrack, Keyed8VectorTrack>()
                            : new FlatBufferUnion<FixedVectorTrack, FramedVectorTrack, Keyed16VectorTrack, Keyed8VectorTrack>(value);
                    }
                case 3:
                    {
                        var value = valuesEl.Deserialize<Keyed16VectorTrack>(options);
                        return value == null
                            ? new FlatBufferUnion<FixedVectorTrack, FramedVectorTrack, Keyed16VectorTrack, Keyed8VectorTrack>()
                            : new FlatBufferUnion<FixedVectorTrack, FramedVectorTrack, Keyed16VectorTrack, Keyed8VectorTrack>(value);
                    }
                case 4:
                    {
                        var value = valuesEl.Deserialize<Keyed8VectorTrack>(options);
                        return value == null
                            ? new FlatBufferUnion<FixedVectorTrack, FramedVectorTrack, Keyed16VectorTrack, Keyed8VectorTrack>()
                            : new FlatBufferUnion<FixedVectorTrack, FramedVectorTrack, Keyed16VectorTrack, Keyed8VectorTrack>(value);
                    }
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
            Dictionary<string, JsonElement>? dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(ref reader, options);
            if (dict == null ||
                !dict.TryGetValue("Discriminator", out var discEl) ||
                !dict.TryGetValue("Values", out var valuesEl))
            {
                return new FlatBufferUnion<FixedRotationTrack, FramedRotationTrack, Keyed16RotationTrack, Keyed8RotationTrack>();
            }

            byte discriminator = discEl.Deserialize<byte>();
            switch (discriminator)
            {
                case 1:
                    {
                        var value = valuesEl.Deserialize<FixedRotationTrack>(options);
                        return value == null
                            ? new FlatBufferUnion<FixedRotationTrack, FramedRotationTrack, Keyed16RotationTrack, Keyed8RotationTrack>()
                            : new FlatBufferUnion<FixedRotationTrack, FramedRotationTrack, Keyed16RotationTrack, Keyed8RotationTrack>(value);
                    }
                case 2:
                    {
                        var value = valuesEl.Deserialize<FramedRotationTrack>(options);
                        return value == null
                            ? new FlatBufferUnion<FixedRotationTrack, FramedRotationTrack, Keyed16RotationTrack, Keyed8RotationTrack>()
                            : new FlatBufferUnion<FixedRotationTrack, FramedRotationTrack, Keyed16RotationTrack, Keyed8RotationTrack>(value);
                    }
                case 3:
                    {
                        var value = valuesEl.Deserialize<Keyed16RotationTrack>(options);
                        return value == null
                            ? new FlatBufferUnion<FixedRotationTrack, FramedRotationTrack, Keyed16RotationTrack, Keyed8RotationTrack>()
                            : new FlatBufferUnion<FixedRotationTrack, FramedRotationTrack, Keyed16RotationTrack, Keyed8RotationTrack>(value);
                    }
                case 4:
                    {
                        var value = valuesEl.Deserialize<Keyed8RotationTrack>(options);
                        return value == null
                            ? new FlatBufferUnion<FixedRotationTrack, FramedRotationTrack, Keyed16RotationTrack, Keyed8RotationTrack>()
                            : new FlatBufferUnion<FixedRotationTrack, FramedRotationTrack, Keyed16RotationTrack, Keyed8RotationTrack>(value);
                    }
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
