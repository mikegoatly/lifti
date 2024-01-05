using FluentAssertions;
using Lifti.Querying;
using Xunit;

namespace Lifti.Tests.Querying
{
    public class FieldMatchTests : QueryTestBase
    {
        [Fact]
        public void MixOfCompositeAndTokenLocations_ShouldReturnUniqueLocationsInOrder()
        {
            var sut = ScoredFieldMatch(
                1D,
                1,
                [CompositeTokenLocation(4, 5, 6, 9), TokenLocation(6), CompositeTokenLocation(5, 6), TokenLocation(7)]);

            sut.GetTokenLocations().Should().BeEquivalentTo(
                new[]
                {
                    new TokenLocation(4, 4, 4),
                    new TokenLocation(5, 5, 5),
                    new TokenLocation(6, 6, 6),
                    new TokenLocation(7, 7, 7),
                    new TokenLocation(9, 9, 9)
                },
                options => options.WithStrictOrdering());
        }

        [Fact]
        public void TokenLocationsOnly_ShouldReturnExactListProvided()
        {
            var tokenLocations = TokenLocations(4, 7, 9, 13);
            var sut = Lifti.Querying.ScoredFieldMatch.CreateFromPresorted(1D, 1, tokenLocations);

            sut.GetTokenLocations().Should().BeSameAs(tokenLocations);
        }

        [Fact]
        public void Merging_BothWithTokenLists_ShouldCreateOrderedAndUniqueList()
        {
            var leftLocations = TokenLocations(4, 7, 9, 13);
            var rightLocations = TokenLocations(5, 9, 13, 17);
            var merged = Lifti.Querying.ScoredFieldMatch.Merge(
                Lifti.Querying.ScoredFieldMatch.CreateFromPresorted(1D, 1, leftLocations),
                Lifti.Querying.ScoredFieldMatch.CreateFromPresorted(1D, 1, rightLocations));

            merged.Locations.Should().BeEquivalentTo(
                TokenLocations(4, 7, 5, 9, 13, 17));
        }

        [Fact]
        public void Merging_BothWithMixedLists_ShouldCreateOrderedAndUniqueList()
        {
            var leftLocations = TokenLocations(4, 8);
            ITokenLocation[] rightLocations = [CompositeTokenLocation(4, 5, 6, 9), CompositeTokenLocation(24, 30)];
            var merged = Lifti.Querying.ScoredFieldMatch.Merge(
                Lifti.Querying.ScoredFieldMatch.CreateFromPresorted(1D, 1, leftLocations),
                Lifti.Querying.ScoredFieldMatch.CreateFromPresorted(1D, 1, rightLocations));

            merged.Locations.Should().BeEquivalentTo(
                new ITokenLocation[] 
                { 
                    TokenLocation(4),
                    CompositeTokenLocation(4, 5, 6, 9),
                    TokenLocation(8),
                    CompositeTokenLocation(24, 30) 
                });
        }
    }
}
