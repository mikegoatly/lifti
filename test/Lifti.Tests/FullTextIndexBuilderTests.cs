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

            ((IndexTokenizer)index.FieldLookup.GetFieldInfo("Content").Tokenizer).Options.CaseInsensitive.Should().BeFalse();
            ((IndexTokenizer)index.FieldLookup.GetFieldInfo("Title").Tokenizer).Options.CaseInsensitive.Should().BeTrue();
        }

        [Fact]
        public void WithConfiguredExplicitFuzzyMatchParameters_ShouldPassParametersToConstructedQueryParsers()
        {
            var passedOptions = BuildSutAndGetPassedOptions(o => o.WithFuzzySearchDefaults(10, 4));

            passedOptions.Should().NotBeNull();
            passedOptions.FuzzySearchMaxEditDistance(1000).Should().Be(10);
            passedOptions.FuzzySearchMaxSequentialEdits(1000).Should().Be(4);
        }

        [Fact]
        public void WithNoDefaultJoiningOperatorConfigured_ShouldPassAndOperatorToIndex()
        {
            var passedOptions = BuildSutAndGetPassedOptions(o => o);
            passedOptions.Should().NotBeNull();
            passedOptions.DefaultJoiningOperator.Should().Be(QueryTermJoinOperatorKind.And);
        }

        [Fact]
        public void WithDefaultJoiningOperatorConfigured_ShouldPassDefaultOperatorToIndex()
        {
            var passedOptions = BuildSutAndGetPassedOptions(o => o.WithDefaultJoiningOperator(QueryTermJoinOperatorKind.Or));
            passedOptions.Should().NotBeNull();
            passedOptions.DefaultJoiningOperator.Should().Be(QueryTermJoinOperatorKind.Or);
        }

        [Fact]
        public void WithSimpleQueryParserConfigured_ShouldUseSimpleQueryParserInIndex()
        {
            var index = this.sut.WithSimpleQueryParser(o => o.WithDefaultJoiningOperator(QueryTermJoinOperatorKind.Or)).Build();

            index.QueryParser.Should().BeOfType<SimpleQueryParser>();
        }

        [Fact]
        public void WithConfigureDynamicFuzzyMatchParameters_ShouldPassParametersToConstructedQueryParsers()
        {
            QueryParserOptions? passedOptions = null;
            this.sut.WithQueryParser(o => o.WithFuzzySearchDefaults(x => (ushort)(x / 10), x => (ushort)(x / 100)).WithQueryParserFactory(x =>
            {
                passedOptions = x;
                return new QueryParser(x);
            }));

            var index = this.sut.Build();

            index.Search("test");

            passedOptions.Should().NotBeNull();
            passedOptions.FuzzySearchMaxEditDistance(1000).Should().Be(100);
            passedOptions.FuzzySearchMaxSequentialEdits(1000).Should().Be(10);
        }

        [Fact]
        public void WithCustomQueryParser_ShouldPassCustomImplementationToIndex()
        {
            var parser = this.ConfigureQueryParserMock();

            var index = this.sut.Build();

            index.Search("test").Should().BeEmpty();

            parser.Verify(p => p.Parse(It.IsAny<IIndexedFieldLookup>(), "test", It.IsAny<IIndexTokenizer>()), Times.Once);
        }

        [Fact]
        public void WithQueryParserOptions_ShouldPassOptionsToQueryParser()
        {
            QueryParserOptions providedOptions = null;

            this.sut.WithQueryParser(o => o
                .AssumeFuzzySearchTerms()
                .WithQueryParserFactory((QueryParserOptions f) =>
                {
                    providedOptions = f;
                    return new QueryParser(f);
                }));

            var index = this.sut.Build();

            providedOptions.Should().BeEquivalentTo(
                new QueryParserOptions
                {
                    AssumeFuzzySearchTerms = true
                });
        }

        [Fact]
        public void WithQueryParserOptions_AndDefaultFactory_ShouldNotError()
        {
            this.sut.WithQueryParser(o => o.AssumeFuzzySearchTerms());

            var index = this.sut.Build();

            index.Should().NotBeNull();
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

        [Fact]
        public async Task IgnoredCharacters_ShouldBeAppliedToBothIndexedTermsAndSearchTerms()
        {
            var index = this.sut.WithDefaultTokenization(o => o.IgnoreCharacters('\''))
                .Build();

            await index.AddAsync(12, "O'Reilly Books");
            await index.AddAsync(24, "O Reilly Books");

            index.DefaultTokenizer.Process("O'Reilly").Should().BeEquivalentTo(new[] { new Token("OREILLY", new TokenLocation(0, 0, 8)) });

            var results = index.Search("O'Reilly").ToList();
            results.Should().HaveCount(1);
            results[0].Key.Should().Be(12);
        }

        private QueryParserOptions BuildSutAndGetPassedOptions(Func<QueryParserBuilder, QueryParserBuilder> optionsBuilder)
        {
            QueryParserOptions? passedOptions = null;

            this.sut.WithQueryParser(o => optionsBuilder(o).WithQueryParserFactory(x =>
            {
                passedOptions = x;
                return new QueryParser(x);
            }));

            var index = this.sut.Build();

            return passedOptions;
        }

        private Mock<IQueryParser> ConfigureQueryParserMock()
        {
            var parser = new Mock<IQueryParser>();
            parser.Setup(p => p.Parse(It.IsAny<IIndexedFieldLookup>(), It.IsAny<string>(), It.IsAny<IIndexTokenizer>()))
                .Returns(new Query(EmptyQueryPart.Instance));

            this.sut.WithQueryParser(parser.Object);

            return parser;
        }

        private class TestObject1
        {
            public TestObject1(int id, string text)
            {
                this.Id = id;
                this.Text = text;
            }

            public int Id { get; }
            public string Text { get; }
        }

        private class TestObject2
        {
            public TestObject2(int id, string[] content, string title)
            {
                this.Id = id;
                this.Content = content;
                this.Title = title;
            }

            public int Id { get; }
            public string[] Content { get; }
            public string Title { get; }
        }
    }
}
