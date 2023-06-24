using System;
using System.IO;

namespace Lifti.Serialization.Binary
{
    internal static class BinaryReaderExtensions
    {
        public static ushort ReadVarUInt16(this BinaryReader reader)
        {
            ushort result = 0;
            var shift = 0;
            while (shift < 16)
            {
                var b = reader.ReadByte();
                result |= (ushort)((b & 0x7F) << shift);
                if ((b & 0x80) == 0)
                {
                    return result;
                }

                shift += 7;
            }

            throw new FormatException(ExceptionMessages.BadlyFormattedVariableLengthValueEncountered);
        }

        public static int ReadVarInt32(this BinaryReader reader)
        {
            var result = ReadVarUInt32(reader);

            // Zig-zag decoding
            return (int)(result >> 1) ^ -(int)(result & 1);
        }

        public static int ReadNonNegativeVarInt32(this BinaryReader reader)
        {
            var value = 0;
            for (var shift = 0; shift < 32; shift += 7)
            {
                var b = reader.ReadByte();
                value |= (b & 0x7F) << shift;
                if ((b & 0x80) == 0)
                {
                    return value;
                }
            }

            throw new LiftiException(ExceptionMessages.BadlyFormattedVariableLengthValueEncountered);
        }

        public static uint ReadVarUInt32(this BinaryReader reader)
        {
            uint value = 0;
            for (var shift = 0; shift < 32; shift += 7)
            {
                var b = reader.ReadByte();
                value |= (uint)(b & 0x7F) << shift;
                if ((b & 0x80) == 0)
                {
                    return value;
                }
            }

            throw new LiftiException(ExceptionMessages.BadlyFormattedVariableLengthValueEncountered);
        }
    }
}
