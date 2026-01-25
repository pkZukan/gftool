using System.Reflection;

namespace Trinity.Core.Flatbuffers.Reflections
{
    public enum PokeDocsGame
    {
        SV,
        LA
    }

    public enum PokeDocsModelSchema
    {
        Trmdl,
        Trmsh,
        Trmbf,
        Trskl,
        Trmtr,
        Trmmt
    }

    public static class PokeDocsBfbsRegistry
    {
        private static readonly object Gate = new();
        private static readonly Dictionary<(PokeDocsGame Game, PokeDocsModelSchema Schema), ReflectionSchemaContext> Cache = new();

        public static ReflectionSchemaContext GetModelSchema(PokeDocsGame game, PokeDocsModelSchema schema)
        {
            lock (Gate)
            {
                if (Cache.TryGetValue((game, schema), out var cached))
                {
                    return cached;
                }

                var ctx = LoadSchemaContext(game, schema);
                Cache[(game, schema)] = ctx;
                return ctx;
            }
        }

        public static bool TryGetDefaultModelSchemaForExtension(string extension, out (PokeDocsGame Game, PokeDocsModelSchema Schema) schema)
        {
            extension = extension?.TrimStart('.') ?? string.Empty;
            schema = default;

            if (string.Equals(extension, "trmdl", StringComparison.OrdinalIgnoreCase))
            {
                schema = (PokeDocsGame.SV, PokeDocsModelSchema.Trmdl);
                return true;
            }
            if (string.Equals(extension, "trmsh", StringComparison.OrdinalIgnoreCase))
            {
                schema = (PokeDocsGame.SV, PokeDocsModelSchema.Trmsh);
                return true;
            }
            if (string.Equals(extension, "trmbf", StringComparison.OrdinalIgnoreCase))
            {
                schema = (PokeDocsGame.SV, PokeDocsModelSchema.Trmbf);
                return true;
            }
            if (string.Equals(extension, "trskl", StringComparison.OrdinalIgnoreCase) || string.Equals(extension, "trskl1", StringComparison.OrdinalIgnoreCase))
            {
                schema = (PokeDocsGame.SV, PokeDocsModelSchema.Trskl);
                return true;
            }
            if (string.Equals(extension, "trmtr", StringComparison.OrdinalIgnoreCase))
            {
                schema = (PokeDocsGame.SV, PokeDocsModelSchema.Trmtr);
                return true;
            }
            if (string.Equals(extension, "trmmt", StringComparison.OrdinalIgnoreCase))
            {
                schema = (PokeDocsGame.SV, PokeDocsModelSchema.Trmmt);
                return true;
            }

            return false;
        }

        private static ReflectionSchemaContext LoadSchemaContext(PokeDocsGame game, PokeDocsModelSchema schema)
        {
            string basePath = game switch
            {
                PokeDocsGame.SV => "Flatbuffers.Reflections.Bfbs.PokeDocs.SV.model",
                PokeDocsGame.LA => "Flatbuffers.Reflections.Bfbs.PokeDocs.LA.model",
                _ => throw new ArgumentOutOfRangeException(nameof(game), game, null)
            };

            string file = schema switch
            {
                PokeDocsModelSchema.Trmdl => "trmdl.bfbs.b64",
                PokeDocsModelSchema.Trmsh => "trmsh.bfbs.b64",
                PokeDocsModelSchema.Trmbf => "trmbf.bfbs.b64",
                PokeDocsModelSchema.Trskl => "trskl.bfbs.b64",
                PokeDocsModelSchema.Trmtr => "trmtr.bfbs.b64",
                PokeDocsModelSchema.Trmmt => "trmmt.bfbs.b64",
                _ => throw new ArgumentOutOfRangeException(nameof(schema), schema, null)
            };

            string suffix = $"{typeof(PokeDocsBfbsRegistry).Assembly.GetName().Name}.{basePath}.{file}";

            byte[] bfbsBytes = ReadEmbeddedBase64(suffix);
            var reflectionSchema = Trinity.Core.Utils.FlatBufferConverter.DeserializeFrom<ReflectionSchema>(bfbsBytes);
            return ReflectionSchemaContext.Create(reflectionSchema);
        }

        private static byte[] ReadEmbeddedBase64(string expectedName)
        {
            var asm = typeof(PokeDocsBfbsRegistry).Assembly;

            // EmbeddedResource names can vary slightly between SDK-style project configs; search by suffix.
            string? resourceName = asm.GetManifestResourceNames()
                .FirstOrDefault(n => n.EndsWith(expectedName, StringComparison.OrdinalIgnoreCase));

            if (resourceName == null)
            {
                resourceName = asm.GetManifestResourceNames()
                    .FirstOrDefault(n => n.EndsWith(expectedName.Split('.', 2).Last(), StringComparison.OrdinalIgnoreCase));
            }

            if (resourceName == null)
            {
                throw new InvalidOperationException($"Embedded schema resource not found: {expectedName}");
            }

            using var s = asm.GetManifestResourceStream(resourceName);
            if (s == null)
            {
                throw new InvalidOperationException($"Failed to open embedded schema resource: {resourceName}");
            }

            using var reader = new StreamReader(s);
            string b64 = reader.ReadToEnd().Trim();
            return Convert.FromBase64String(b64);
        }
    }

    public sealed class ReflectionSchemaContext
    {
        public required ReflectionSchema Schema { get; init; }
        public required ReflectionObject RootTable { get; init; }
        public required IReadOnlyDictionary<string, ReflectionObject> ObjectsByName { get; init; }
        public required IReadOnlyDictionary<int, ReflectionObject> ObjectsByIndex { get; init; }
        public required IReadOnlyDictionary<int, ReflectionEnum> EnumsByIndex { get; init; }
        public required IReadOnlyDictionary<string, ReflectionEnum> EnumsByName { get; init; }

        public static ReflectionSchemaContext Create(ReflectionSchema schema)
        {
            var objects = schema.Objects ?? Array.Empty<ReflectionObject>();
            var enums = schema.Enums ?? Array.Empty<ReflectionEnum>();

            var objectsByName = new Dictionary<string, ReflectionObject>(StringComparer.Ordinal);
            var objectsByIndex = new Dictionary<int, ReflectionObject>();
            for (int i = 0; i < objects.Length; i++)
            {
                var o = objects[i];
                if (!string.IsNullOrWhiteSpace(o?.Name))
                {
                    objectsByName[o.Name!] = o;
                }
                objectsByIndex[i] = o;
            }

            var enumsByIndex = new Dictionary<int, ReflectionEnum>();
            var enumsByName = new Dictionary<string, ReflectionEnum>(StringComparer.Ordinal);
            for (int i = 0; i < enums.Length; i++)
            {
                var e = enums[i];
                enumsByIndex[i] = e;
                if (!string.IsNullOrWhiteSpace(e?.Name))
                {
                    enumsByName[e.Name!] = e;
                }
            }

            if (schema.RootTable == null)
            {
                throw new InvalidOperationException("Reflection schema missing RootTable.");
            }

            return new ReflectionSchemaContext
            {
                Schema = schema,
                RootTable = schema.RootTable,
                ObjectsByName = objectsByName,
                ObjectsByIndex = objectsByIndex,
                EnumsByIndex = enumsByIndex,
                EnumsByName = enumsByName
            };
        }
    }
}
