using System;
using System.IO;

namespace Lifti.Serialization.Binary
{
    internal static class BinaryWriterExtensions
    {
        internal static void WriteSpanAsVarInt16s(this BinaryWriter writer, ReadOnlySpan<char> span)
        {
            // Write out as shorts avoiding surrogate character serialization errors
            for (var i = 0; i < span.Length; i++)
            {
                writer.WriteVarUInt16(span[i]);
            }
        }

        public static void WriteVarUInt32(this BinaryWriter writer, uint value)
        {
            while (value > 0x7F)
            {
                writer.Write((byte)((value & 0x7F) | 0x80));
                value >>= 7;
            }

            writer.Write((byte)value);
        }

        public static void WriteVarUInt16(this BinaryWriter writer, ushort value)
        {
            while (value > 0x7F)
            {
                writer.Write((byte)((value & 0x7F) | 0x80));
                value >>= 7;
            }

            writer.Write((byte)value);
        }

        public static void WriteVarInt32(this BinaryWriter writer, int value)
        {
            var zigZagEncoded = (uint)((value << 1) ^ (value >> 31));

            WriteVarUInt32(writer, zigZagEncoded);
        }

        public static void WriteNonNegativeVarInt32(this BinaryWriter writer, int value)
        {
            while (value > 0x7F)
            {
                writer.Write((byte)((value & 0x7F) | 0x80));
                value >>= 7;
            }

            writer.Write((byte)value);
        }
    }
}
