using FluentAssertions;
using Lifti.Querying;
using Lifti.Tests.Fakes;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Lifti.Tests.Querying
{
    public class CompositeTokenMatchLocationTests : QueryTestBase
    {
        private readonly FakeTokenLocation match2;
        private readonly FakeTokenLocation match1;
        private readonly CompositeTokenLocation sut1;
        private readonly CompositeTokenLocation sut2;
        private readonly TokenLocation[] match1Locations;
        private readonly TokenLocation[] match2Locations;

        public CompositeTokenMatchLocationTests()
        {
            this.match1Locations =
            [
                new TokenLocation(100, 1, 2),
                new TokenLocation(200, 1, 2)
            ];

            this.match1 = new FakeTokenLocation(100, 200, this.match1Locations);

            this.match2Locations =
            [
                new TokenLocation(110, 1, 2),
                new TokenLocation(150, 1, 2),
                new TokenLocation(180, 1, 2)
            ];

            this.match2 = new FakeTokenLocation(110, 180, this.match2Locations);

            this.sut1 = new CompositeTokenLocation(this.match1, this.match2);
            this.sut2 = new CompositeTokenLocation(this.match2, this.match1);
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
            var locations = new HashSet<TokenLocation>();
            this.sut1.AddTo(locations);
            locations.Should().BeEquivalentTo(this.match1Locations.Concat(this.match2Locations).ToList());

            locations.Clear();

            this.sut2.AddTo(locations);
            locations.Should().BeEquivalentTo(this.match2Locations.Concat(this.match1Locations).ToList());
        }
    }
}
