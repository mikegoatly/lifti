using FluentAssertions;
using Lifti.Tokenization;
using Lifti.Tokenization.Objects;
using Lifti.Tokenization.TextExtraction;
using Xunit;

namespace Lifti.Tests
{
    public class IndexedFieldLookupTests
    {
        private readonly IndexedFieldLookup sut;

        public IndexedFieldLookupTests()
        {
            this.sut = new IndexedFieldLookup();
        }

        [Fact]
        public void GettingIdsForFieldsShouldReturnCorrectIncrementalIds()
        {
            this.WithBasicConfig();

            this.sut.GetFieldInfo("Field1").Id.Should().Be(1);
            this.sut.GetFieldInfo("Field2").Id.Should().Be(2);
            this.sut.GetFieldInfo("Field3").Id.Should().Be(3);
        }

        [Fact]
        public void GettingTokenizationOptionsShouldReturnCorrectlyConstructedInstances()
        {
            this.WithBasicConfig();

            ((IndexTokenizer)this.sut.GetFieldInfo("FieldX").Tokenizer).Options.Stemming.Should().BeTrue();
            ((IndexTokenizer)this.sut.GetFieldInfo("FieldY").Tokenizer).Options.Stemming.Should().BeFalse();
        }

        [Fact]
        public void GettingNameForValidIdShouldReturnCorrectFieldName()
        {
            this.WithBasicConfig();

            this.sut.GetFieldForId(2).Should().Be("Field2");
            this.sut.GetFieldForId(1).Should().Be("Field1");
        }

        [Fact]
        public void GettingNameForInvalidIdShouldThrowException()
        {
            Assert.Throws<LiftiException>(() => this.sut.GetFieldForId(99))
                .Message.Should().Be("Field id 99 has no associated field name");
        }

        [Fact]
        public void UsingMoreThan255IdsShouldThrowException()
        {
            var itemConfigBuilder = new ObjectTokenizationBuilder<string, string>()
                .WithKey(i => i);

            for (var i = 0; i < 256; i++)
            {
                itemConfigBuilder = itemConfigBuilder.WithField("Field" + i, r => r);
            }

            Assert.Throws<LiftiException>(() => this.Build(itemConfigBuilder, this.sut))
                .Message.Should().Be("Only 255 distinct fields can currently be indexed");
        }

        [Fact]
        public void UsingDuplicateFieldNameShouldThrowException()
        {
            var config1 = this.Build(new ObjectTokenizationBuilder<string, string>()
                .WithField("Field1", o => o)
                .WithField("Field2", o => o)
                .WithKey(i => i), this.sut);

            Assert.Throws<LiftiException>(
                () => this.Build(new ObjectTokenizationBuilder<string, string>()
                    .WithField("Field1", o => o)
                    .WithKey(i => i), this.sut))
                .Message.Should().Be("Duplicate field name used: Field1. Field names must be unique across all item types registered with an index.");
        }

        private void WithBasicConfig()
        {
            var itemConfig = (new ObjectTokenizationBuilder<string, string>()
                .WithKey(i => i)
                .WithField("Field1", r => r)
                .WithField("Field2", r => r)
                .WithField("Field3", r => r)
                .WithField("FieldX", r => r, o => o.WithStemming())
                .WithField("FieldY", r => r) as IObjectTokenizationBuilder)
                .Build(IndexTokenizer.Default, new ThesaurusBuilder(), new PlainTextExtractor(), this.sut);
        }

        private IObjectTokenization Build(ObjectTokenizationBuilder<string, string> objectTokenizationBuilder, IndexedFieldLookup fieldLookup)
        {
            return (objectTokenizationBuilder as IObjectTokenizationBuilder)
                .Build(IndexTokenizer.Default, new ThesaurusBuilder(), new PlainTextExtractor(), fieldLookup);
        }

        private class TestObject
        {
        }
    }
}
