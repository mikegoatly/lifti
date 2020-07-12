using FluentAssertions;
using Lifti.Querying;
using Xunit;

namespace Lifti.Tests.Querying
{
    public class FieldMatchTests : QueryTestBase
    {
        [Fact]
        public void ShouldReturnUniqueLocationsInOrder()
        {
            var sut = new FieldMatch(1, CompositeMatch(4, 5, 6, 9), TokenMatch(6), CompositeMatch(5, 6), TokenMatch(7));

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
    }
}
