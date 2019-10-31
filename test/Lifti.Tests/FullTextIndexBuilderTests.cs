using FluentAssertions;
using Lifti.Querying;
using Lifti.Tests.Querying;
using Lifti.Tokenization;
using Moq;
using Xunit;

namespace Lifti.Tests
{
    public class FullTextIndexBuilderTests
    {
        private readonly FullTextIndexBuilder<int> sut;

        public FullTextIndexBuilderTests()
        {
            this.sut = new FullTextIndexBuilder<int>();
        }

        [Fact]
        public void WithItemConfiguration_ShouldConstructIndexWithSpecifiedConfig()
        {
            this.sut.WithItemTokenization<TestObject1>(
                o => o
                    .WithKey(i => i.Id)
                    .WithField("TextField", i => i.Text))
                .WithItemTokenization<TestObject2>(
                o => o
                    .WithKey(i => i.Id)
                    .WithField("Content", i => i.Content)
                    .WithField("Title", i => i.Title));

            var index = this.sut.Build();

            index.FieldLookup.GetFieldInfo("TextField").Id.Should().Be(1);
            index.FieldLookup.GetFieldInfo("Content").Id.Should().Be(2);
            index.FieldLookup.GetFieldInfo("Title").Id.Should().Be(3);
        }

        [Fact]
        public void WithCustomIndexNodeFactory_ShouldPassCustomImplementationToIndex()
        {
            var factory = new Mock<IIndexNodeFactory>();
            factory.Setup(f => f.CreateNode()).Returns(new IndexNode(factory.Object, 999, IndexSupportLevelKind.CharacterByCharacter));

            this.sut.WithIndexNodeFactory(factory.Object);

            var index = this.sut.Build();

            index.Root.Depth.Should().Be(999);
            factory.Verify(f => f.CreateNode(), Times.Once);
        }

        [Fact]
        public void WithCustomQueryParser_ShouldPassCustomImplementationToIndex()
        {
            var parser = this.ConfigureQueryParserMock();

            var index = this.sut.Build();

            index.Search("test").Should().BeEmpty();

            parser.Verify(p => p.Parse(It.IsAny<IIndexedFieldLookup>(), "test", It.IsAny<ITokenizer>()), Times.Once);
        }

        [Fact]
        public void WithCustomTokenizerFactory_ShouldPassCustomImplementationToIndex()
        {
            var tokenizer = new FakeTokenizer();
            var factory = new Mock<ITokenizerFactory>();
            factory.Setup(f => f.Create(It.IsAny<TokenizationOptions>())).Returns(tokenizer);
            var parser = this.ConfigureQueryParserMock();

            this.sut.WithTokenizerFactory(factory.Object);

            var index = this.sut.Build();

            index.Search("test").Should().BeEmpty();

            parser.Verify(p => p.Parse(It.IsAny<IIndexedFieldLookup>(), It.IsAny<string>(), tokenizer), Times.Once);
        }

        [Fact]
        public void WithDefaultTokenizationOptions_ShouldUseOptionsWhenSearchingWithNoTokenizerOptions()
        {
            var factory = new Mock<ITokenizerFactory>();
            var tokenizer = new FakeTokenizer();
            var xmlTokenizer = new XmlTokenizer();
            factory.Setup(f => f.Create(It.Is<TokenizationOptions>(o => o.TokenizerKind == TokenizerKind.XmlContent))).Returns(xmlTokenizer);
            factory.Setup(f => f.Create(It.Is<TokenizationOptions>(o => o.TokenizerKind == TokenizerKind.PlainText))).Returns(tokenizer);
            var parser = this.ConfigureQueryParserMock();

            this.sut.WithTokenizerFactory(factory.Object);
            this.sut.WithDefaultTokenizationOptions(o => o.XmlContent());

            var index = this.sut.Build();

            index.Search("test").Should().BeEmpty();
            index.Search("test with tokenization options", TokenizationOptions.Default).Should().BeEmpty();
            parser.Verify(p => p.Parse(It.IsAny<IIndexedFieldLookup>(), "test", xmlTokenizer), Times.Once);
            parser.Verify(p => p.Parse(It.IsAny<IIndexedFieldLookup>(), "test with tokenization options", tokenizer), Times.Once);
        }

        [Fact]
        public void WithConfiguredIntraNodeTextLength_ShouldPassValueAsConfigurationToIndexNodeFactory()
        {
            var factory = new Mock<IIndexNodeFactory>();
            factory.Setup(f => f.CreateNode()).Returns(new IndexNode(factory.Object, 999, IndexSupportLevelKind.CharacterByCharacter));

            this.sut.WithIndexNodeFactory(factory.Object)
                .WithIntraNodeTextSupportedAfterIndexDepth(89);

            var index = this.sut.Build();

            factory.Verify(f => f.Configure(It.Is<AdvancedOptions>(o => o.SupportIntraNodeTextAfterIndexDepth == 89)), Times.Once);
        }

        private Mock<IQueryParser> ConfigureQueryParserMock()
        {
            var parser = new Mock<IQueryParser>();
            parser.Setup(p => p.Parse(It.IsAny<IIndexedFieldLookup>(), It.IsAny<string>(), It.IsAny<ITokenizer>()))
                .Returns(new Query(null));

            this.sut.WithQueryParser(parser.Object);

            return parser;
        }

        private class TestObject1
        {
            public int Id { get; set; }
            public string Text { get; set; }
        }

        private class TestObject2
        {
            public int Id { get; set; }
            public string[] Content { get; set; }
            public string Title { get; set; }
        }
    }
}
