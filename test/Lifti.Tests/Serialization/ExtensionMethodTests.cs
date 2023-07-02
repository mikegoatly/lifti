using FluentAssertions;
using Lifti.Serialization.Binary;
using System.IO;
using Xunit;

namespace Lifti.Tests.Serialization
{
    public class ExtensionMethodTests
    {
        [Theory]
        [InlineData(0, 1)]
        [InlineData(127, 1)]
        [InlineData(128, 2)]
        [InlineData(16383, 2)]
        [InlineData(16384, 3)]
        [InlineData(ushort.MaxValue, 3)]
        public void ShouldReadAndWriteCompressedUInt16s(ushort value, int expectedLength)
        {
            using var memoryStream = new MemoryStream();
            using var writer = new BinaryWriter(memoryStream);
            writer.WriteVarUInt16(value);

            memoryStream.Length.Should().Be(expectedLength);

            memoryStream.Position = 0;
            using var reader = new BinaryReader(memoryStream);
            var readValue = reader.ReadVarUInt16();

            readValue.Should().Be(value);
        }

        [Theory]
        [InlineData(0, 1)]
        [InlineData(127, 1)]
        [InlineData(128, 2)]
        [InlineData(16383, 2)]
        [InlineData(16384, 3)]
        [InlineData(2097151, 3)]
        [InlineData(2097152, 4)]
        [InlineData(int.MaxValue, 5)]
        public void ShouldReadAndWriteCompressedNonNegativeInt32s(int value, int expectedLength)
        {
            using var memoryStream = new MemoryStream();
            using var writer = new BinaryWriter(memoryStream);
            writer.WriteNonNegativeVarInt32(value);

            memoryStream.Length.Should().Be(expectedLength);

            memoryStream.Position = 0;
            using var reader = new BinaryReader(memoryStream);
            var readValue = reader.ReadNonNegativeVarInt32();

            readValue.Should().Be(value);
        }

        [Theory]
        [InlineData(0, 1)]
        [InlineData(-1, 1)]
        [InlineData(63, 1)]
        [InlineData(-64, 1)]
        [InlineData(64, 2)]
        [InlineData(-65, 2)]
        [InlineData(8191, 2)]
        [InlineData(-8192, 2)]
        [InlineData(8192, 3)]
        [InlineData(-8193, 3)]
        [InlineData(1048575, 3)]
        [InlineData(-1048576, 3)]
        [InlineData(1048576, 4)]
        [InlineData(-1048577, 4)]
        [InlineData(134217727, 4)]
        [InlineData(-134217728, 4)]
        [InlineData(134217728, 5)]
        [InlineData(-134217729, 5)]
        [InlineData(int.MinValue, 5)]
        [InlineData(int.MaxValue, 5)]
        public void ShouldReadAndWriteCompressedInt32s(int value, int expectedLength)
        {
            using var memoryStream = new MemoryStream();
            using var writer = new BinaryWriter(memoryStream);
            writer.WriteVarInt32(value);

            memoryStream.Length.Should().Be(expectedLength);

            memoryStream.Position = 0;
            using var reader = new BinaryReader(memoryStream);
            var readValue = reader.ReadVarInt32();

            readValue.Should().Be(value);
        }

        [Theory]
        [InlineData(0, 1)]
        [InlineData(127, 1)]
        [InlineData(128, 2)]
        [InlineData(16383, 2)]
        [InlineData(16384, 3)]
        [InlineData(2097151, 3)]
        [InlineData(2097152, 4)]
        [InlineData(268435455, 4)]
        [InlineData(268435456, 5)]
        [InlineData(uint.MaxValue, 5)]
        public void ShouldReadAndWriteCompressedUInt32s(uint value, int expectedLength)
        {
            using var memoryStream = new MemoryStream();
            using var writer = new BinaryWriter(memoryStream);
            writer.WriteVarUInt32(value);

            memoryStream.Length.Should().Be(expectedLength);

            memoryStream.Position = 0;
            using var reader = new BinaryReader(memoryStream);
            var readValue = reader.ReadVarUInt32();

            readValue.Should().Be(value);
        }
    }
}
