using FluentAssertions;
using Lifti.Querying;
using Lifti.Querying.QueryParts;
using Lifti.Tests.Querying;
using Lifti.Tokenization;
using Lifti.Tokenization.TextExtraction;
using Moq;
using System;
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
        public void WithObjectConfiguration_ShouldConstructIndexWithSpecifiedConfig()
        {
            this.sut.WithObjectTokenization<TestObject1>(
                o => o
                    .WithKey(i => i.Id)
                    .WithField("TextField", i => i.Text))
                .WithObjectTokenization<TestObject2>(
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
        public void WithObjectConfiguration_ShouldUseDefaultTokenizationOptionsIfNotProvided()
        {
            this.sut.WithDefaultTokenization(o => o.CaseInsensitive(false))
                .WithObjectTokenization<TestObject2>(
                o => o
                    .WithKey(i => i.Id)
                    .WithField("Content", i => i.Content)
                    .WithField("Title", i => i.Title, s => s.CaseInsensitive(true)));

            var index = this.sut.Build();

            ((Tokenizer)index.FieldLookup.GetFieldInfo("Content").Tokenizer).Options.CaseInsensitive.Should().BeFalse();
            ((Tokenizer)index.FieldLookup.GetFieldInfo("Title").Tokenizer).Options.CaseInsensitive.Should().BeTrue();
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
            var indexScorer = new Mock<IScorer>();
            indexScorer.Setup(s => s.Score(It.IsAny<IReadOnlyList<QueryTokenMatch>>(), It.IsAny<double>()))
                .Returns((IReadOnlyList<QueryTokenMatch> t, double weight) => t.Select(m => new ScoredToken(m.ItemId, m.FieldMatches.Select(fm => new ScoredFieldMatch(score, fm)).ToList())).ToList());

            var scorer = new Mock<IIndexScorerFactory>();
            scorer.Setup(s => s.CreateIndexScorer(It.IsAny<IIndexSnapshot>())).Returns(indexScorer.Object);

            this.sut.WithScorerFactory(scorer.Object);

            var index = this.sut.Build();

            await index.AddAsync(1, "test");

            var results = index.Search("test");

            results.Should().HaveCount(1);
            results.Single().Score.Should().Be(score);
        }

        [Fact]
        public async Task WithConfiguredIntraNodeTextLength_ShouldPassValueAsConfigurationToIndexNodeFactory()
        {
            this.sut.WithIntraNodeTextSupportedAfterIndexDepth(0);
            var index = this.sut.Build();
            await index.AddAsync(1, "Testing");
            index.Root.IntraNodeText.ToString().Should().BeEquivalentTo("TESTING");

            this.sut.WithIntraNodeTextSupportedAfterIndexDepth(1);
            index = this.sut.Build();
            await index.AddAsync(1, "Testing");
            index.Root.IntraNodeText.ToString().Should().BeEquivalentTo("");
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
