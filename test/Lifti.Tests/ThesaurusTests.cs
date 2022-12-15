using FluentAssertions;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Lifti.Tests
{
    public class ThesaurusTests
    {
        private record TestObject(int Id, string Text);

        private static Dictionary<int, string> source = new()
        {
            { 1, "The big bird flew" },
            { 2, "The car crashed" },
            { 3, "The vehicle flew through the air" },
        };

        [Fact]
        public async Task LooseTextIndex_SearchingForSynonym_ShouldReturnMatch()
        {
            var sut = await CreateLooseTextIndexAsync();

            var results = sut.Search("massive");

            results.CreateMatchPhrases(x => source[x])
                .Should()
                .BeEquivalentTo(
                new[]
                {
                    new ItemPhrases<int>(
                        results.Single(x => x.Key == 1),
                        new[] { new FieldPhrases<int>(IndexedFieldLookup.DefaultFieldName , "big") })
                 });
        }

        [Fact]
        public async Task LooseTextIndex_SearchingForHyponym_ShouldOnlyReturnSearchedWord()
        {
            var sut = await CreateLooseTextIndexAsync();

            var results = sut.Search("vehicle");

            results.CreateMatchPhrases(x => source[x])
                .Should()
                .BeEquivalentTo(
                new[]
                {
                    new ItemPhrases<int>(
                        results.Single(x => x.Key == 3),
                        new[] { new FieldPhrases<int>(IndexedFieldLookup.DefaultFieldName, "vehicle") })
                 });
        }

        [Fact]
        public async Task LooseTextIndex_SearchingForHypernym_ShouldReturnMatch()
        {
            var sut = await CreateLooseTextIndexAsync();

            var results = sut.Search("car");

            results.CreateMatchPhrases(x => source[x])
                .Should()
                .BeEquivalentTo(
                new[]
                {
                    new ItemPhrases<int>(
                        results.Single(x => x.Key == 2),
                        new[] { new FieldPhrases<int>(IndexedFieldLookup.DefaultFieldName, "car") }),
                    new ItemPhrases<int>(
                        results.Single(x => x.Key == 3),
                        new[] { new FieldPhrases<int>(IndexedFieldLookup.DefaultFieldName, "vehicle") })
                 });
        }

        [Fact]
        public async Task ObjectIndex_SearchingForSynonym_ShouldReturnMatch()
        {
            var sut = await CreateObjectIndexAsync();

            var results = sut.Search("massive");

            (await results.CreateMatchPhrasesAsync(x => new TestObject(x, source[x])))
                .Should()
                .BeEquivalentTo(
                new[]
                {
                    new ItemPhrases<int>(
                        results.Single(x => x.Key == 1),
                        new[] { new FieldPhrases<int>("Field" , "big") })
                 });
        }

        [Fact]
        public async Task ObjectIndex_SearchingForHyponym_ShouldOnlyReturnSearchedWord()
        {
            var sut = await CreateObjectIndexAsync();

            var results = sut.Search("vehicle");

            (await results.CreateMatchPhrasesAsync(x => new TestObject(x, source[x])))
                .Should()
                .BeEquivalentTo(
                new[]
                {
                    new ItemPhrases<int>(
                        results.Single(x => x.Key == 3),
                        new[] { new FieldPhrases<int>("Field", "vehicle") })
                 });
        }

        [Fact]
        public async Task ObjectIndex_SearchingForHypernym_ShouldReturnMatch()
        {
            var sut = await CreateObjectIndexAsync();

            var results = sut.Search("car");

            (await results.CreateMatchPhrasesAsync(x => new TestObject(x, source[x])))
                .Should()
                .BeEquivalentTo(
                new[]
                {
                    new ItemPhrases<int>(
                        results.Single(x => x.Key == 2),
                        new[] { new FieldPhrases<int>("Field", "car") }),
                    new ItemPhrases<int>(
                        results.Single(x => x.Key == 3),
                        new[] { new FieldPhrases<int>("Field", "vehicle") })
                 });
        }

        private static async Task<IFullTextIndex<int>> CreateIndexAsync(bool useObjectTokenization)
        {
            if (useObjectTokenization)
            {
                return await CreateObjectIndexAsync();
            }

            return await CreateLooseTextIndexAsync();
        }

        private static async Task<FullTextIndex<int>> CreateObjectIndexAsync()
        {
            var sut = new FullTextIndexBuilder<int>()
                .WithObjectTokenization<TestObject>(
                    o => o.WithKey(x => x.Id)
                        .WithField(
                            "Field",
                            x => x.Text,
                            thesaurusOptions: b => b
                                .AddSynonyms("happy", "joyous", "delighted")
                                .AddSynonyms("large", "big", "massive")
                                .AddHypernyms("vehicle", "car", "truck", "motorcycle")
                                .AddHypernyms("animal", "mammal", "bird", "reptile")))
                .Build();

            sut.BeginBatchChange();
            foreach (var item in source)
            {
                await sut.AddAsync(new TestObject(item.Key, item.Value));
            }
            await sut.CommitBatchChangeAsync();

            return sut;
        }

        private static async Task<FullTextIndex<int>> CreateLooseTextIndexAsync()
        {
            var sut = new FullTextIndexBuilder<int>()
                .WithThesaurus(
                    b => b
                        .AddSynonyms("happy", "joyous", "delighted")
                        .AddSynonyms("large", "big", "massive")
                        .AddHypernyms("vehicle", "car", "truck", "motorcycle")
                        .AddHypernyms("animal", "mammal", "bird", "reptile"))
                .Build();

            sut.BeginBatchChange();
            foreach (var item in source)
            {
                await sut.AddAsync(item.Key, item.Value);
            }
            await sut.CommitBatchChangeAsync();

            return sut;
        }
    }
}
