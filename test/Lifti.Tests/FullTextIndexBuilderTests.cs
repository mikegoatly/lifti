using FluentAssertions;
using Lifti.Querying;
using Lifti.Querying.QueryParts;
using Lifti.Tests.Querying;
using Lifti.Tokenization;
using Moq;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        public void WithItemConfiguration_ShouldUseDefaultTokenizationOptionsIfNotProvided()
        {
            this.sut.WithDefaultTokenizationOptions(o => o.CaseInsensitive(false))
                .WithItemTokenization<TestObject2>(
                o => o
                    .WithKey(i => i.Id)
                    .WithField("Content", i => i.Content)
                    .WithField("Title", i => i.Title, s => s.CaseInsensitive(true)));

            var index = this.sut.Build();

            index.FieldLookup.GetFieldInfo("Content").Tokenizer.Process("teSt").Select(s => s.Value).Should().BeEquivalentTo("teSt");
            index.FieldLookup.GetFieldInfo("Title").Tokenizer.Process("teSt").Select(s => s.Value).Should().BeEquivalentTo("TEST");
        }

        [Fact]
        public void WithCustomIndexNodeFactory_ShouldPassCustomImplementationToIndex()
        {
            var factory = new Mock<IIndexNodeFactory>();
            var expectedRoot = new IndexNode(null, null, null);
            factory.Setup(f => f.CreateRootNode()).Returns(expectedRoot);

            this.sut.WithIndexNodeFactory(factory.Object);

            var index = this.sut.Build();

            index.Root.Should().Be(expectedRoot);
            factory.Verify(f => f.CreateRootNode(), Times.Once);
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
        public async Task WithScorer_ShouldPassImplementationToIndex()
        {
            var score = 999999999D;
            var indexScorer = new Mock<IIndexScorer>();
            indexScorer.Setup(s => s.Score(It.IsAny<IReadOnlyList<QueryTokenMatch>>()))
                .Returns((IReadOnlyList<QueryTokenMatch> t) => t.Select(m => new ScoredToken(m.ItemId, m.FieldMatches.Select(fm => new ScoredFieldMatch(score, fm)).ToList())).ToList());

            var scorer = new Mock<IScorer>();
            scorer.Setup(s => s.CreateIndexScorer(It.IsAny<IIndexSnapshot>())).Returns(indexScorer.Object);

            this.sut.WithScorer(scorer.Object);

            var index = this.sut.Build();

            await index.AddAsync(1, "test");

            var results = index.Search("test");

            results.Should().HaveCount(1);
            results.Single().Score.Should().Be(score);
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
            factory.Setup(f => f.CreateRootNode()).Returns(new IndexNode(null, null, null));

            this.sut.WithIndexNodeFactory(factory.Object)
                .WithIntraNodeTextSupportedAfterIndexDepth(89);

            var index = this.sut.Build();

            factory.Verify(f => f.Configure(It.Is<IndexOptions>(o => o.SupportIntraNodeTextAfterIndexDepth == 89)), Times.Once);
        }

        [Fact]
        public async Task WithIndexModifiedActions_ShouldPassActionsToIndex()
        {
            var action1 = new List<string>();
            var action2 = new List<int>();

            this.sut.WithIndexModificationAction(i => action1.Add(i.Items.Count.ToString()))
                .WithIndexModificationAction(i => action2.Add(i.Items.Count));

            var index = this.sut.Build();

            await index.AddAsync(9, "Test");

            action1.Should().BeEquivalentTo("1");
            action2.Should().BeEquivalentTo(1);
        }

        [Fact]
        public async Task WithDuplicateItemKeysThrowingExceptions_ShouldPassOptionToIndex()
        {
            var index = this.sut.WithDuplicateItemBehavior(DuplicateItemBehavior.ThrowException)
                .Build();

            await index.AddAsync(1, "Test");
            await index.AddAsync(2, "Test");

            await Assert.ThrowsAsync<LiftiException>(async () => await index.AddAsync(1, "Test"));
        }

        [Fact]
        public async Task WithDuplicateItemKeysReplacingItems_ShouldPassOptionToIndex()
        {
            var index = this.sut.WithDuplicateItemBehavior(DuplicateItemBehavior.ReplaceItem)
                .Build();

            await index.AddAsync(1, "Test");
            await index.AddAsync(2, "Test");
            await index.AddAsync(1, "Testing");

            index.Search("Testing").Should().HaveCount(1);
        }

        private Mock<IQueryParser> ConfigureQueryParserMock()
        {
            var parser = new Mock<IQueryParser>();
            parser.Setup(p => p.Parse(It.IsAny<IIndexedFieldLookup>(), It.IsAny<string>(), It.IsAny<ITokenizer>()))
                .Returns(new Query(EmptyQueryPart.Instance));

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
