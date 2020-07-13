using FluentAssertions;
using Lifti.Querying;
using Moq;
using System.Linq;
using Xunit;

namespace Lifti.Tests.Querying
{
    public class CompositeTokenMatchLocationTests : QueryTestBase
    {
        private readonly Mock<ITokenLocationMatch> match2;
        private readonly Mock<ITokenLocationMatch> match1;
        private readonly CompositeTokenMatchLocation sut1;
        private readonly CompositeTokenMatchLocation sut2;
        private readonly TokenLocation[] match1Locations;
        private readonly TokenLocation[] match2Locations;

        public CompositeTokenMatchLocationTests()
        {
            this.match1 = new Mock<ITokenLocationMatch>();
            this.match1.SetupGet(t => t.MinTokenIndex).Returns(100);
            this.match1.SetupGet(t => t.MaxTokenIndex).Returns(200);
            this.match1Locations = new[] {
                new TokenLocation(100, 1, 2),
                new TokenLocation(200, 1, 2)
            };

            this.match1.Setup(m => m.GetLocations()).Returns(this.match1Locations);

            this.match2 = new Mock<ITokenLocationMatch>();
            this.match2.SetupGet(t => t.MinTokenIndex).Returns(110);
            this.match2.SetupGet(t => t.MaxTokenIndex).Returns(180);
            this.match2Locations = new[] {
                new TokenLocation(110, 1, 2),
                new TokenLocation(150, 1, 2),
                new TokenLocation(180, 1, 2)
            };

            this.match2.Setup(m => m.GetLocations()).Returns(this.match2Locations);

            this.sut1 = new CompositeTokenMatchLocation(this.match1.Object, this.match2.Object);
            this.sut2 = new CompositeTokenMatchLocation(this.match2.Object, this.match1.Object);
        }

        [Fact]
        public void MinLocationShouldBeMinimumOfTheLeftAndRightMinimumValues()
        {
            this.sut1.MinTokenIndex.Should().Be(100);
            this.sut2.MinTokenIndex.Should().Be(100);
        }

        [Fact]
        public void MaxLocationShouldBeMinimumOfTheLeftAndRightMinimumValues()
        {
            this.sut1.MaxTokenIndex.Should().Be(200);
            this.sut2.MaxTokenIndex.Should().Be(200);
        }

        [Fact]
        public void GetLocationsShouldReturnAllLocations()
        {
            this.sut1.GetLocations().Should().BeEquivalentTo(this.match1Locations.Concat(this.match2Locations).ToList());
            this.sut2.GetLocations().Should().BeEquivalentTo(this.match2Locations.Concat(this.match1Locations).ToList());
        }
    }
}
