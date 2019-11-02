using FluentAssertions;
using Lifti.ItemTokenization;
using Lifti.Tokenization;
using Moq;
using Xunit;

namespace Lifti.Tests
{
    public class IndexedFieldLookupTests
    {
        private static readonly ITokenizer defaultTokenizer = new Mock<ITokenizer>().Object;
        private static readonly ITokenizer stemmingTokenizer = new Mock<ITokenizer>().Object;
        private readonly IndexedFieldLookup sut;
        private readonly Mock<ITokenizerFactory> tokenizerFactoryMock;

        public IndexedFieldLookupTests()
        {
            var itemConfig = new ItemTokenizationOptionsBuilder<string, string>()
                .WithKey(i => i)
                .WithField("Field1", r => r)
                .WithField("Field2", r => r)
                .WithField("Field3", r => r)
                .WithField("FieldX", r => r, o => o.WithStemming())
                .WithField("FieldY", r => r)
                .Build();

            this.tokenizerFactoryMock = new Mock<ITokenizerFactory>();
            this.tokenizerFactoryMock.Setup(f => f.Create(TokenizationOptions.Default)).Returns(defaultTokenizer);
            this.tokenizerFactoryMock.Setup(f => f.Create(It.Is<TokenizationOptions>(o => o.Stemming))).Returns(stemmingTokenizer);

            this.sut = new IndexedFieldLookup(itemConfig.FieldTokenization, this.tokenizerFactoryMock.Object, TokenizationOptions.Default);
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
            this.sut.GetFieldInfo("FieldX").Tokenizer.Should().Be(stemmingTokenizer);
            this.sut.GetFieldInfo("FieldY").Tokenizer.Should().Be(defaultTokenizer);
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
            var itemConfigBuilder = new ItemTokenizationOptionsBuilder<string, string>()
                .WithKey(i => i);

            for (var i = 0; i < 256; i++)
            {
                itemConfigBuilder = itemConfigBuilder.WithField("Field" + i, r => r);
            }

            Assert.Throws<LiftiException>(() => new IndexedFieldLookup(itemConfigBuilder.Build().FieldTokenization, this.tokenizerFactoryMock.Object, TokenizationOptions.Default))
                .Message.Should().Be("Only 255 distinct fields can currently be indexed");
        }

        [Fact]
        public void UsingDuplicateFieldNameShouldThrowException()
        {
            var itemConfigBuilder = new ItemTokenizationOptionsBuilder<string, string>()
                .WithField("Field1", o => o)
                .WithField("Field2", o => o)
                .WithField("Field1", o => o)
                .WithKey(i => i);

            Assert.Throws<LiftiException>(() => new IndexedFieldLookup(itemConfigBuilder.Build().FieldTokenization, this.tokenizerFactoryMock.Object, TokenizationOptions.Default))
                .Message.Should().Be("Duplicate field name used: Field1. Field names must be unique across all item types registered with an index.");
        }
    }
}
