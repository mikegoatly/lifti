using FluentAssertions;
using Lifti.Tests.Querying;
using Lifti.Tokenization.Objects;
using Lifti.Tokenization.TextExtraction;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Lifti.Tests.Tokenization.Objects
{
    public class DictionaryDynamicFieldReaderTests
    {
        private static readonly Dictionary<string, string> fieldValues = new()
        {
            { "Foo", "Bar" },
            { "Baz", "Bam" }
        };

        [Fact]
        public async Task GettingDynamicFields_ShouldReturnAllFields()
        {
            var sut = CreateSut();

            var result = await sut.ReadAsync(new TestObject(fieldValues), default);

            result.Should().BeEquivalentTo(new (string, IEnumerable<ReadOnlyMemory<char>>)[]
            {
                ("Foo", ["Bar".AsMemory()]),
                ("Baz", ["Bam".AsMemory()])
            });
        }

        [Fact]
        public async Task WithPrefix_GettingDynamicFields_ShouldReturnAllFieldsWithPrefix()
        {
            var sut = CreateSut("Test_");

            var result = await sut.ReadAsync(new TestObject(fieldValues), default);

            result.Should().BeEquivalentTo(new (string, IEnumerable<ReadOnlyMemory<char>>)[]
            {
                ("Test_Foo", ["Bar".AsMemory()]),
                ("Test_Baz", ["Bam".AsMemory()])
            });
        }

        [Fact]
        public async Task GettingFieldValue_ShouldReturnTextAssociatedToSpecificField()
        {
            var sut = CreateSut();

            await sut.ReadAsync(new TestObject(fieldValues), default);

            (await sut.ReadAsync(new TestObject(fieldValues), "Foo", default)).Should().BeEquivalentTo(["Bar".AsMemory()]);
            (await sut.ReadAsync(new TestObject(fieldValues), "Baz", default)).Should().BeEquivalentTo(["Bam".AsMemory()]);
        }

        [Fact]
        public async Task WithPrefix_GettingFieldValue_ShouldReturnTextAssociatedToSpecificField()
        {
            var sut = CreateSut("Test_");

            await sut.ReadAsync(new TestObject(fieldValues), default);

            (await sut.ReadAsync(new TestObject(fieldValues), "Test_Foo", default)).Should().BeEquivalentTo(["Bar".AsMemory()]);
            (await sut.ReadAsync(new TestObject(fieldValues), "Test_Baz", default)).Should().BeEquivalentTo(["Bam".AsMemory()]);
        }

        [Fact]
        public async Task GettingFieldValue_WhenNotFound_ShouldReturnEmptyResults()
        {
            var sut = CreateSut();

            // Make sure the reader has encountered the field against a different objet
            await sut.ReadAsync(new TestObject(new Dictionary<string, string> { { "Zod", "Doz" } }), default);

            // Attempting to read that field from an object that doesn't have the field should not error
            (await sut.ReadAsync(new TestObject(fieldValues), "Zod", default)).Should().BeEmpty();
        }

        private static StringDictionaryDynamicFieldReader<TestObject> CreateSut(string? fieldPrefix = null)
        {
            return new StringDictionaryDynamicFieldReader<TestObject>(
                x => x.Fields,
                "Test",
                fieldPrefix,
                new FakeIndexTokenizer(),
                new PlainTextExtractor(),
                new ThesaurusBuilder().Build(new FakeIndexTokenizer()),
                1D);
        }

        private class TestObject
        {
            public TestObject(Dictionary<string, string> fields)
            {
                this.Fields = fields;
            }

            public Dictionary<string, string> Fields { get; set; }
        }
    }
}
