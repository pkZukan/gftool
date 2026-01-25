using System;
using System.Buffers.Binary;
using System.Text;

namespace TrinityModelViewer.Export
{
    internal sealed class FlatBufferBinary
    {
        private readonly byte[] buffer;
        private static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

        public FlatBufferBinary(byte[] buffer)
        {
            this.buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
        }

        public byte[] Buffer => buffer;

        public int GetRootTableOffset()
        {
            // Root table is stored as an offset from the start of the buffer.
            return ReadInt32(0);
        }

        public int GetVTableOffset(int tableOffset)
        {
            // At the start of every table is an int32 offset to the vtable (negative, relative).
            int vtableRel = ReadInt32(tableOffset);
            return tableOffset - vtableRel;
        }

        public int GetFieldAbsoluteOffset(int tableOffset, int fieldIndex)
        {
            int vtable = GetVTableOffset(tableOffset);
            ushort vtableLen = ReadUInt16(vtable);
            int entryPos = vtable + 4 + (fieldIndex * 2);
            if (entryPos + 2 > vtable + vtableLen)
            {
                return 0;
            }

            ushort fieldOff = ReadUInt16(entryPos);
            return fieldOff == 0 ? 0 : tableOffset + fieldOff;
        }

        public int ReadUOffset(int absolutePos)
        {
            return ReadInt32(absolutePos);
        }

        public int DerefUOffset(int uoffsetFieldPos)
        {
            int rel = ReadUOffset(uoffsetFieldPos);
            return rel == 0 ? 0 : uoffsetFieldPos + rel;
        }

        public string ReadStringAtUOffsetField(int uoffsetFieldPos)
        {
            int strPos = DerefUOffset(uoffsetFieldPos);
            if (strPos == 0)
            {
                return string.Empty;
            }

            int len = ReadInt32(strPos);
            if (len <= 0)
            {
                return string.Empty;
            }

            int bytesPos = strPos + 4;
            if (bytesPos < 0 || bytesPos + len > buffer.Length)
            {
                return string.Empty;
            }

            return Encoding.UTF8.GetString(buffer, bytesPos, len);
        }

        public bool TryOverwriteStringAtUOffsetField(int uoffsetFieldPos, string newValue)
        {
            if (uoffsetFieldPos == 0)
            {
                return false;
            }

            int strPos = DerefUOffset(uoffsetFieldPos);
            if (strPos == 0)
            {
                return false;
            }

            int oldLen = ReadInt32(strPos);
            if (oldLen < 0)
            {
                return false;
            }

            newValue ??= string.Empty;
            var newBytes = Utf8NoBom.GetBytes(newValue);
            if (newBytes.Length > oldLen)
            {
                return false;
            }

            int bytesPos = strPos + 4;
            int oldEnd = bytesPos + oldLen;
            if (bytesPos < 0 || oldEnd >= buffer.Length)
            {
                return false;
            }

            WriteInt32(strPos, newBytes.Length);

            if (newBytes.Length > 0)
            {
                System.Buffer.BlockCopy(newBytes, 0, buffer, bytesPos, newBytes.Length);
            }

            // FlatBuffer strings are length-prefixed and null-terminated.
            buffer[bytesPos + newBytes.Length] = 0;

            // Clear remaining bytes (optional, but helps keep diffs stable if the file is re-dumped).
            for (int i = newBytes.Length + 1; i <= oldLen; i++)
            {
                buffer[bytesPos + i] = 0;
            }

            return true;
        }

        public int GetVectorDataStartFromUOffsetField(int uoffsetFieldPos, out int length)
        {
            length = 0;
            int vecPos = DerefUOffset(uoffsetFieldPos);
            if (vecPos == 0)
            {
                return 0;
            }

            int len = ReadInt32(vecPos);
            if (len < 0)
            {
                return 0;
            }

            length = len;
            return vecPos + 4;
        }

        public int GetVectorElementTableOffset(int vectorDataStart, int index)
        {
            int elemField = vectorDataStart + (index * 4);
            int rel = ReadUOffset(elemField);
            return rel == 0 ? 0 : elemField + rel;
        }

        public void WriteInt32(int absolutePos, int value)
        {
            BinaryPrimitives.WriteInt32LittleEndian(buffer.AsSpan(absolutePos, 4), value);
        }

        public void WriteSingle(int absolutePos, float value)
        {
            BinaryPrimitives.WriteSingleLittleEndian(buffer.AsSpan(absolutePos, 4), value);
        }

        public byte ReadByte(int absolutePos)
        {
            return buffer[absolutePos];
        }

        public bool ReadBool(int absolutePos)
        {
            return ReadByte(absolutePos) != 0;
        }

        public int ReadInt32(int absolutePos)
        {
            return BinaryPrimitives.ReadInt32LittleEndian(buffer.AsSpan(absolutePos, 4));
        }

        public ushort ReadUInt16(int absolutePos)
        {
            return BinaryPrimitives.ReadUInt16LittleEndian(buffer.AsSpan(absolutePos, 2));
        }
    }
}
