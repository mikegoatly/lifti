using FluentAssertions;
using Lifti.Tokenization;
using System.Text;
using Xunit;

namespace Lifti.Tests.Tokenization
{
    public class TokenStoreTests
    {
        private readonly TokenStore sut;
        private readonly WordLocation location1 = new WordLocation(0, 4, 8);
        private readonly WordLocation location2 = new WordLocation(1, 9, 2);

        public TokenStoreTests()
        {
            this.sut = new TokenStore();
        }

        [Fact]
        public void ShouldCreateEntryForFirstCall()
        {
            this.sut.MergeOrAdd(new TokenHash(1234), new StringBuilder("test"), this.location1);

            this.sut.ToList().Should().BeEquivalentTo(
                new[]
                {
                    new Token("test", this.location1)
                });
        }

        [Fact]
        public void ShouldCreateSeparateEntryForClashingHasWithDifferentText()
        {
            this.sut.MergeOrAdd(new TokenHash(1234), new StringBuilder("test"), this.location1);
            this.sut.MergeOrAdd(new TokenHash(1234), new StringBuilder("test2"), this.location2);

            this.sut.ToList().Should().BeEquivalentTo(
                new[]
                {
                    new Token("test", this.location1),
                    new Token("test2", this.location2)
                });
        }

        [Fact]
        public void ShouldCreateSeparateEntryForNovelHash()
        {
            this.sut.MergeOrAdd(new TokenHash(1234), new StringBuilder("test"), this.location1);
            this.sut.MergeOrAdd(new TokenHash(12345), new StringBuilder("test7"), this.location2);

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
            this.sut.MergeOrAdd(new TokenHash(1234), new StringBuilder("test"), this.location1);
            this.sut.MergeOrAdd(new TokenHash(1234), new StringBuilder("test"), this.location2);

            this.sut.ToList().Should().BeEquivalentTo(
                new[]
                {
                    new Token("test", this.location1, this.location2)
                });
        }
    }
}
