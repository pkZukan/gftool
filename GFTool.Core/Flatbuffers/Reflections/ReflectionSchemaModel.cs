using FlatSharp.Attributes;

namespace Trinity.Core.Flatbuffers.Reflections
{
    // FlatBuffers reflection schema bindings (reflection.fbs), sufficient for reading .bfbs.

    [FlatBufferEnum(typeof(byte))]
    public enum ReflectionBaseType : byte
    {
        None = 0,
        UType = 1,
        Bool = 2,
        Byte = 3,
        UByte = 4,
        Short = 5,
        UShort = 6,
        Int = 7,
        UInt = 8,
        Long = 9,
        ULong = 10,
        Float = 11,
        Double = 12,
        String = 13,
        Vector = 14,
        Obj = 15,
        Union = 16
    }

    [FlatBufferTable]
    public class ReflectionType
    {
        [FlatBufferItem(0)]
        public virtual ReflectionBaseType BaseType { get; set; }

        [FlatBufferItem(1)]
        public virtual ReflectionBaseType Element { get; set; }

        [FlatBufferItem(2)]
        public virtual int Index { get; set; }

        [FlatBufferItem(3)]
        public virtual ushort FixedLength { get; set; }
    }

    [FlatBufferTable]
    public class ReflectionKeyValue
    {
        [FlatBufferItem(0)]
        public virtual string? Key { get; set; }

        [FlatBufferItem(1)]
        public virtual string? Value { get; set; }
    }

    [FlatBufferTable]
    public class ReflectionField
    {
        [FlatBufferItem(0)]
        public virtual string? Name { get; set; }

        [FlatBufferItem(1)]
        public virtual ReflectionType? Type { get; set; }

        [FlatBufferItem(2)]
        public virtual ushort Id { get; set; }

        // Struct field offset (tables use vtable offsets instead).
        [FlatBufferItem(3)]
        public virtual ushort Offset { get; set; }

        [FlatBufferItem(4)]
        public virtual long DefaultInteger { get; set; }

        [FlatBufferItem(5)]
        public virtual double DefaultReal { get; set; }

        [FlatBufferItem(6)]
        public virtual bool Deprecated { get; set; }

        [FlatBufferItem(7)]
        public virtual bool Required { get; set; }

        [FlatBufferItem(8)]
        public virtual bool Key { get; set; }

        [FlatBufferItem(9)]
        public virtual ReflectionKeyValue[]? Attributes { get; set; }

        [FlatBufferItem(10)]
        public virtual string[]? Documentation { get; set; }
    }

    [FlatBufferTable]
    public class ReflectionObject
    {
        [FlatBufferItem(0)]
        public virtual string? Name { get; set; }

        [FlatBufferItem(1)]
        public virtual ReflectionField[]? Fields { get; set; }

        [FlatBufferItem(2)]
        public virtual bool IsStruct { get; set; }

        [FlatBufferItem(3)]
        public virtual int MinAlign { get; set; }

        [FlatBufferItem(4)]
        public virtual int Bytesize { get; set; }

        [FlatBufferItem(5)]
        public virtual ReflectionKeyValue[]? Attributes { get; set; }

        [FlatBufferItem(6)]
        public virtual string[]? Documentation { get; set; }
    }

    [FlatBufferTable]
    public class ReflectionEnumVal
    {
        [FlatBufferItem(0)]
        public virtual string? Name { get; set; }

        [FlatBufferItem(1)]
        public virtual long Value { get; set; }

        [FlatBufferItem(2)]
        public virtual ReflectionKeyValue[]? Attributes { get; set; }

        [FlatBufferItem(3)]
        public virtual string[]? Documentation { get; set; }
    }

    [FlatBufferTable]
    public class ReflectionEnum
    {
        [FlatBufferItem(0)]
        public virtual string? Name { get; set; }

        [FlatBufferItem(1)]
        public virtual ReflectionEnumVal[]? Values { get; set; }

        [FlatBufferItem(2)]
        public virtual ReflectionType? UnderlyingType { get; set; }

        [FlatBufferItem(3)]
        public virtual ReflectionKeyValue[]? Attributes { get; set; }

        [FlatBufferItem(4)]
        public virtual string[]? Documentation { get; set; }
    }

    [FlatBufferTable]
    public class ReflectionSchema
    {
        [FlatBufferItem(0)]
        public virtual ReflectionObject[]? Objects { get; set; }

        [FlatBufferItem(1)]
        public virtual ReflectionEnum[]? Enums { get; set; }

        [FlatBufferItem(2)]
        public virtual string? FileIdent { get; set; }

        [FlatBufferItem(3)]
        public virtual string? FileExt { get; set; }

        [FlatBufferItem(4)]
        public virtual ReflectionObject? RootTable { get; set; }

        // Services and advanced features are ignored for our usage.
    }
}
