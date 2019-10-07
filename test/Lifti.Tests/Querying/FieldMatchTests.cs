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
            var sut = new FieldMatch(1, CompositeMatch(4, 5, 6, 9), WordMatch(6), CompositeMatch(5, 6), WordMatch(7));

            sut.GetWordLocations().Should().BeEquivalentTo(
                new[]
                {
                    new WordLocation(4, 4, 4),
                    new WordLocation(5, 5, 5),
                    new WordLocation(6, 6, 6),
                    new WordLocation(7, 7, 7),
                    new WordLocation(9, 9, 9)
                }, 
                options => options.WithStrictOrdering());
        }
    }
}
