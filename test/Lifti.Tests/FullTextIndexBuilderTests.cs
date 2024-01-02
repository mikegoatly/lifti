using FluentAssertions;
using Lifti.Querying;
using Lifti.Tests.Fakes;
using Lifti.Tokenization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Lifti.Tests
{
    public partial class FullTextIndexBuilderTests
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
        public void WithObjectConfiguration_ShouldAllocateUniqueObjectTypeIds()
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

            index.ObjectTypeConfiguration.AllConfigurations.Select(x => x.Id)
                .Should().BeEquivalentTo(new[] { 1, 2 });
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
        public void WithObjectConfiguration_ShouldUseDefaultThesaurusIfNotProvided()
        {
            this.sut.WithDefaultThesaurus(o => o.WithSynonyms("a", "b", "c"))
                .WithDefaultTokenization(o => o.CaseInsensitive(false))
                .WithObjectTokenization<TestObject2>(
                o => o
                    .WithKey(i => i.Id)
                    .WithField("Content", i => i.Content, tokenizationOptions: o => o.CaseInsensitive(true))
                    .WithField("Title", i => i.Title, thesaurusOptions: s => s.WithSynonyms("A", "b")));

            var index = this.sut.Build();

            var defaultThesaurus = (Thesaurus)index.DefaultThesaurus;

            // Thesaurus should have been built without case insensitivity - casing should be unchanged
            defaultThesaurus.WordLookup.Should().BeEquivalentTo(
                new Dictionary<string, IReadOnlyList<string>>
                {
                    { "a", new[] { "a", "b", "c" } },
                    { "b", new[] { "a", "b", "c" } },
                    { "c", new[] { "a", "b", "c" } },
                });

            // Thesaurus should contain the same words as the default thesaurus, but have been built WITH case insensitivity,
            // so casing should be uppercase
            ((Thesaurus)index.FieldLookup.GetFieldInfo("Content").Thesaurus).WordLookup.Should().BeEquivalentTo(
                new Dictionary<string, IReadOnlyList<string>>
                {
                    { "A", new[] { "A", "B", "C" } },
                    { "B", new[] { "A", "B", "C" } },
                    { "C", new[] { "A", "B", "C" } },
                });

            // Thesaurus should have been built using the default tokenizer, but only contain the thesaurus words specified
            // at the field level
            ((Thesaurus)index.FieldLookup.GetFieldInfo("Title").Thesaurus).WordLookup.Should().BeEquivalentTo(
                new Dictionary<string, IReadOnlyList<string>>
                {
                    { "A", new[] { "A", "b" } },
                    { "b", new[] { "A", "b" } },
                });
        }

        [Fact]
        public void WithConfiguredExplicitFuzzyMatchParameters_ShouldPassParametersToConstructedQueryParsers()
        {
            var passedOptions = this.BuildSutAndGetPassedOptions(o => o.WithFuzzySearchDefaults(10, 4));

            passedOptions.Should().NotBeNull();
            passedOptions!.FuzzySearchMaxEditDistance(1000).Should().Be(10);
            passedOptions.FuzzySearchMaxSequentialEdits(1000).Should().Be(4);
        }

        [Fact]
        public void WithNoDefaultJoiningOperatorConfigured_ShouldPassAndOperatorToIndex()
        {
            var passedOptions = this.BuildSutAndGetPassedOptions(o => o);
            passedOptions.Should().NotBeNull();
            passedOptions!.DefaultJoiningOperator.Should().Be(QueryTermJoinOperatorKind.And);
        }

        [Fact]
        public void WithDefaultJoiningOperatorConfigured_ShouldPassDefaultOperatorToIndex()
        {
            var passedOptions = this.BuildSutAndGetPassedOptions(o => o.WithDefaultJoiningOperator(QueryTermJoinOperatorKind.Or));
            passedOptions.Should().NotBeNull();
            passedOptions!.DefaultJoiningOperator.Should().Be(QueryTermJoinOperatorKind.Or);
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
            passedOptions!.FuzzySearchMaxEditDistance(1000).Should().Be(100);
            passedOptions.FuzzySearchMaxSequentialEdits(1000).Should().Be(10);
        }

        [Fact]
        public void WithCustomQueryParser_ShouldPassCustomImplementationToIndex()
        {
            var parser = this.ConfigureQueryParser();

            var index = this.sut.Build();

            index.Search("test").Should().BeEmpty();

            parser.ParsedQueries.Should().BeEquivalentTo(new[] { "test" });
        }

        [Fact]
        public void WithQueryParserOptions_ShouldPassOptionsToQueryParser()
        {
            QueryParserOptions? providedOptions = null;

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

            var scorerFactory = new FakeScorerFactory(new FakeScorer(score));

            this.sut.WithScorerFactory(scorerFactory);

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

            this.sut.WithIndexModificationAction(i => action1.Add(i.Metadata.DocumentCount.ToString()))
                .WithIndexModificationAction(i => action2.Add(i.Metadata.DocumentCount));

            var index = this.sut.Build();

            await index.AddAsync(9, "Test");

            action1.Should().BeEquivalentTo(new[] { "1" });
            action2.Should().BeEquivalentTo(new[] { 1 });
        }

        [Fact]
        public async Task WithDuplicateItemKeysThrowingExceptions_ShouldPassOptionToIndex()
        {
            var index = this.sut.WithDuplicateKeyBehavior(DuplicateKeyBehavior.ThrowException)
                .Build();

            await index.AddAsync(1, "Test");
            await index.AddAsync(2, "Test");

            await Assert.ThrowsAsync<LiftiException>(async () => await index.AddAsync(1, "Test"));
        }

        [Fact]
        public async Task WithDuplicateItemKeysReplacingItems_ShouldPassOptionToIndex()
        {
            var index = this.sut.WithDuplicateKeyBehavior(DuplicateKeyBehavior.Replace)
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

            index.DefaultTokenizer.Process("O'Reilly".AsSpan()).Should().BeEquivalentTo(new[] { new Token("OREILLY", new TokenLocation(0, 0, 8)) });

            var results = index.Search("O'Reilly").ToList();
            results.Should().HaveCount(1);
            results[0].Key.Should().Be(12);
        }

        private QueryParserOptions? BuildSutAndGetPassedOptions(Func<QueryParserBuilder, QueryParserBuilder> optionsBuilder)
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

        private FakeQueryParser ConfigureQueryParser()
        {
            var parser = new FakeQueryParser(Query.Empty);

            this.sut.WithQueryParser(parser);

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
