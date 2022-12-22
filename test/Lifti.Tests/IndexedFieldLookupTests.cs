using FluentAssertions;
using Lifti.Tokenization;
using Lifti.Tokenization.Objects;
using Lifti.Tokenization.TextExtraction;
using System.Linq;
using Xunit;

namespace Lifti.Tests
{
    public class IndexedFieldLookupTests
    {
        private readonly IndexedFieldLookup sut;

        public IndexedFieldLookupTests()
        {
            IObjectTokenization itemConfig = (new ObjectTokenizationBuilder<string, string>()
                .WithKey(i => i)
                .WithField("Field1", r => r)
                .WithField("Field2", r => r)
                .WithField("Field3", r => r)
                .WithField("FieldX", r => r, o => o.WithStemming())
                .WithField("FieldY", r => r) as IObjectTokenizationBuilder)
                .Build(IndexTokenizer.Default, new ThesaurusBuilder(), new PlainTextExtractor());

            this.sut = new IndexedFieldLookup(itemConfig.GetConfiguredFields());
        }

        [Fact]
        public void GettingIdsForFieldsShouldReturnCorrectIncrementalIds()
        {
            this.sut.GetFieldInfo("Field1").Id.Should().Be(1);
            this.sut.GetFieldInfo("Field2").Id.Should().Be(2);
            this.sut.GetFieldInfo("Field3").Id.Should().Be(3);
        }

        [Fact]
        public void GettingTokenizationOptionsShouldReturnCorrectlyConstructedInstances()
        {
            ((IndexTokenizer)this.sut.GetFieldInfo("FieldX").Tokenizer).Options.Stemming.Should().BeTrue();
            ((IndexTokenizer)this.sut.GetFieldInfo("FieldY").Tokenizer).Options.Stemming.Should().BeFalse();
        }

        [Fact]
        public void GettingNameForValidIdShouldReturnCorrectFieldName()
        {
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

            IObjectTokenization config = Build(itemConfigBuilder);

            Assert.Throws<LiftiException>(() => new IndexedFieldLookup(config.GetConfiguredFields()))
                .Message.Should().Be("Only 255 distinct fields can currently be indexed");
        }

        [Fact]
        public void UsingDuplicateFieldNameShouldThrowException()
        {
            IObjectTokenization config1 = Build(new ObjectTokenizationBuilder<string, string>()
                .WithField("Field1", o => o)
                .WithField("Field2", o => o)
                .WithKey(i => i));

            IObjectTokenization config2 = Build(new ObjectTokenizationBuilder<string, string>()
                .WithField("Field1", o => o)
                .WithKey(i => i));

            Assert.Throws<LiftiException>(
                () => new IndexedFieldLookup(config1.GetConfiguredFields().Concat(config2.GetConfiguredFields())))
                .Message.Should().Be("Duplicate field name used: Field1. Field names must be unique across all item types registered with an index.");
        }

        private IObjectTokenization Build(ObjectTokenizationBuilder<string, string> objectTokenizationBuilder)
        {
            return (objectTokenizationBuilder as IObjectTokenizationBuilder)
                .Build(IndexTokenizer.Default, new ThesaurusBuilder(), new PlainTextExtractor());
        }
    }
}
