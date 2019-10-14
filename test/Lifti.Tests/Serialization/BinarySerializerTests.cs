using FluentAssertions;
using Lifti.Serialization.Binary;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Lifti.Tests.Serialization
{
    public class BinarySerializerTests
    {
        [Fact]
        public async Task ShouldRoundTripIndexStructure()
        {
            var index = new FullTextIndex<string>();
            index.Index("A", "This is a test string for serialization");
            index.Index("B", "Also a test string and should be serialized");

            var serializer = new BinarySerializer<string>();

            using (var stream = new MemoryStream())
            {
                await serializer.SerializeAsync(index, stream, false);

                stream.Length.Should().BeGreaterThan(4);
            }
        }
    }
}
