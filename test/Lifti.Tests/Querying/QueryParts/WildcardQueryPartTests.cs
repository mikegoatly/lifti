using FluentAssertions;
using Lifti.Querying;
using Lifti.Querying.QueryParts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Lifti.Tests.Querying.QueryParts
{
    public class WildcardQueryPartTests : IAsyncLifetime
    {
        private FullTextIndex<int> index;

        public WildcardQueryPartTests()
        {
        }

        [Fact]
        public void Evaluating_WithSingleTextFragment_ShouldReturnOnlyExactMatches()
        {
            var part = new WildcardQueryPart(new[] { WildcardQueryFragment.CreateText("ALSO") });
            var results = this.index.Search(new Query(part)).ToList();

            results.Should().HaveCount(1);

            results[0].FieldMatches.Should().SatisfyRespectively(
                x => x.Locations.Should().BeEquivalentTo(new[]
                {
                    new TokenLocation(2, 16, 4)
                }));
        }

        [Fact]
        public void Evaluating_WithSingleCharacterReplacement_ShouldReturnCorrectResults()
        {
            var part = new WildcardQueryPart(new[] 
            { 
                WildcardQueryFragment.CreateText("TH"),
                WildcardQueryFragment.SingleCharacter,
                WildcardQueryFragment.CreateText("S")
            });

            var results = this.index.Search(new Query(part)).ToList();

            results.Should().HaveCount(2);

            results[0].FieldMatches.Should().SatisfyRespectively(
                x => x.Locations.Should().BeEquivalentTo(new[]
                {
                    new TokenLocation(1, 11, 4)
                }));

            results[1].FieldMatches.Should().SatisfyRespectively(
                x => x.Locations.Should().BeEquivalentTo(new[]
                {
                    new TokenLocation(4, 30, 4)
                }));
        }

        [Fact]
        public void Evaluating_WithConsecutiveSingleCharacterReplacement_ShouldReturnCorrectResults()
        {
            var part = new WildcardQueryPart(new[]
            {
                WildcardQueryFragment.CreateText("T"),
                WildcardQueryFragment.SingleCharacter,
                WildcardQueryFragment.SingleCharacter,
                WildcardQueryFragment.CreateText("S")
            });

            var results = this.index.Search(new Query(part)).ToList();

            results.Should().HaveCount(2);

            results[0].FieldMatches.Should().SatisfyRespectively(
                x => x.Locations.Should().BeEquivalentTo(new[]
                {
                    new TokenLocation(1, 11, 4)
                }));

            results[1].FieldMatches.Should().SatisfyRespectively(
                x => x.Locations.Should().BeEquivalentTo(new[]
                {
                    new TokenLocation(4, 30, 4)
                }));
        }

        public Task DisposeAsync()
        {
            return Task.CompletedTask;
        }

        public async Task InitializeAsync()
        {
            this.index = new FullTextIndexBuilder<int>()
                .Build();

            await this.index.AddAsync(1, "Apparently this also applies");
            await this.index.AddAsync(2, "Angry alternatives to apples, thus");
        }
    }
}
