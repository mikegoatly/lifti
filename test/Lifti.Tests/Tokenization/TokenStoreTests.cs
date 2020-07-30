using FluentAssertions;
using Lifti.Tokenization;
using System.Text;
using Xunit;

namespace Lifti.Tests.Tokenization
{
    public class TokenStoreTests
    {
        private readonly TokenStore sut;
        private readonly TokenLocation location1 = new TokenLocation(0, 4, 8);
        private readonly TokenLocation location2 = new TokenLocation(1, 9, 2);

        public TokenStoreTests()
        {
            this.sut = new TokenStore();
        }

        [Fact]
        public void ShouldCreateEntryForFirstCall()
        {
            this.sut.MergeOrAdd(new StringBuilder("test"), this.location1);

            this.sut.ToList().Should().BeEquivalentTo(
                new[]
                {
                    new Token("test", this.location1)
                });
        }

        [Fact]
        public void ShouldCreateSeparateEntryForNovelHash()
        {
            this.sut.MergeOrAdd(new StringBuilder("test"), this.location1);
            this.sut.MergeOrAdd(new StringBuilder("test7"), this.location2);

            this.sut.ToList().Should().BeEquivalentTo(
                new[]
                {
                    new Token("test", this.location1),
                    new Token("test7", this.location2)
                });
        }

        [Fact]
        public void ShouldCombineEntriesForMatchingHashAndText()
        {
            this.sut.MergeOrAdd(new StringBuilder("test"), this.location1);
            this.sut.MergeOrAdd(new StringBuilder("test"), this.location2);

            this.sut.ToList().Should().BeEquivalentTo(
                new[]
                {
                    new Token("test", this.location1, this.location2)
                });
        }
    }
}
