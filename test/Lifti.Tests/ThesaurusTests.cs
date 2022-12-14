using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Lifti.Tests
{
    public class ThesaurusTests : IAsyncLifetime
    {
        private static Dictionary<int, string> source = new()
        {
            { 1, "The big bird flew" },
            { 2, "The car crashed" },
            { 3, "The vehicle flew through the air" },
        };

        private FullTextIndex<int> sut;

        public ThesaurusTests()
        {
            this.sut = new FullTextIndexBuilder<int>()
                .WithThesaurus(
                    b => b
                        .AddSynonyms("happy", "joyous", "delighted")
                        .AddSynonyms("large", "big", "massive")
                        .AddHypernyms("vehicle", "car", "truck", "motorcycle")
                        .AddHypernyms("animal", "mammal", "bird", "reptile"))
                .Build();
        }

        [Fact]
        public void SearchingForSynonym_ShouldReturnMatch()
        {
            var results = this.sut.Search("massive");
            
            results.CreateMatchPhrases(x => source[x])
                .Should()
                .BeEquivalentTo(
                new[]
                {
                    new ItemPhrases<int>(
                        results.Single(x => x.Key == 1),
                        new[] { new FieldPhrases<int>(IndexedFieldLookup.DefaultFieldName, "big") })
                 });
        }

        [Fact]
        public void SearchingForHyponym_ShouldOnlyReturnSearchedWord()
        {
            var results = this.sut.Search("vehicle");

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
        public void SearchingForHypernum_ShouldReturnMatch()
        {
            var results = this.sut.Search("car");

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

        public async Task InitializeAsync()
        {
            foreach (var item in source)
            {
                await this.sut.AddAsync(item.Key, item.Value);
            }
        }

        public Task DisposeAsync()
        {
            this.sut.Dispose();
            return Task.CompletedTask;
        }
    }
}
