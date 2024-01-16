using FluentAssertions;
using Lifti.Querying;
using System.Collections.Generic;
using Xunit;

namespace Lifti.Tests.Querying
{
    public class CompositeTokenLocationTests : QueryTestBase
    {
        [Fact]
        public void ComposingTwoTokenLocations_ShouldBuildAppropriately()
        {
            var composite = ((ITokenLocation)TokenLocation(3)).ComposeWith(TokenLocation(2));
            composite.MinTokenIndex.Should().Be(2);
            composite.MaxTokenIndex.Should().Be(3);

            // And the same if we compose the other way around
            composite = ((ITokenLocation)TokenLocation(2)).ComposeWith(TokenLocation(3));
            composite.MinTokenIndex.Should().Be(2);
            composite.MaxTokenIndex.Should().Be(3);

            // Both tokens should be returned
            CompositeTokenLocationTests.VerifyCollectedTokens(composite, TokenLocations(2, 3));
        }

        [Fact]
        public void ComposingCompositeWithNewMaxTokenLocation_ShouldBuildAppropriately()
        {
            var existingComposite = new CompositeTokenLocation([.. TokenLocations(6, 2)], 2, 6);

            var composite = existingComposite.ComposeWith(TokenLocation(9));

            composite.MinTokenIndex.Should().Be(2);
            composite.MaxTokenIndex.Should().Be(9);

            // All tokens should be returned
            CompositeTokenLocationTests.VerifyCollectedTokens(composite, TokenLocations(2, 6, 9));
        }

        [Fact]
        public void ComposingCompositeWithNewMinTokenLocation_ShouldBuildAppropriately()
        {
            var existingComposite = new CompositeTokenLocation([.. TokenLocations(6, 2)], 2, 6);

            var composite = existingComposite.ComposeWith(TokenLocation(1));

            composite.MinTokenIndex.Should().Be(1);
            composite.MaxTokenIndex.Should().Be(6);

            // All tokens should be returned
            CompositeTokenLocationTests.VerifyCollectedTokens(composite, TokenLocations(1, 2, 6));
        }

        [Fact]
        public void ComposingCompositeWithComposite_ShouldBuildAppropriately()
        {
            var composite1 = new CompositeTokenLocation([.. TokenLocations(6, 2)], 2, 6);
            var composite2 = new CompositeTokenLocation([.. TokenLocations(1, 9)], 1, 9);

            var composite = composite1.ComposeWith(composite2);

            composite.MinTokenIndex.Should().Be(1);
            composite.MaxTokenIndex.Should().Be(9);

            CompositeTokenLocationTests.VerifyCollectedTokens(composite, TokenLocations(1, 2, 6, 9));
        }

        private static void VerifyCollectedTokens(CompositeTokenLocation composite, List<TokenLocation> tokenLocations)
        {
            var collectedTokens = new HashSet<TokenLocation>();
            composite.AddTo(collectedTokens);
            collectedTokens.Should().BeEquivalentTo(tokenLocations);
        }
    }
}
