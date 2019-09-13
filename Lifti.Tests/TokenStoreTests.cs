using FluentAssertions;
using Lifti.Preprocessing;
using System.Text;
using Xunit;

namespace Lifti.Tests
{
    public class TokenStoreTests
    {
        private readonly TokenStore sut;

        public TokenStoreTests()
        {
            this.sut = new TokenStore();
        }

        [Fact]
        public void ShouldCreateEntryForFirstCall()
        {
            this.sut.MergeOrAdd(new TokenHash(1234), new StringBuilder("test"), new Range(4, 8));

            this.sut.ToList().Should().BeEquivalentTo(
                new[]
                {
                    new Token("test", new Range(4, 8))
                });
        }

        [Fact]
        public void ShouldCreateSeparateEntryForClashingHasWithDifferentText()
        {
            this.sut.MergeOrAdd(new TokenHash(1234), new StringBuilder("test"), new Range(4, 8));
            this.sut.MergeOrAdd(new TokenHash(1234), new StringBuilder("test2"), new Range(9, 2));

            this.sut.ToList().Should().BeEquivalentTo(
                new[]
                {
                    new Token("test", new Range(4, 8)),
                    new Token("test2", new Range(9, 2))
                });
        }

        [Fact]
        public void ShouldCreateSeparateEntryForNovelHash()
        {
            this.sut.MergeOrAdd(new TokenHash(1234), new StringBuilder("test"), new Range(4, 8));
            this.sut.MergeOrAdd(new TokenHash(12345), new StringBuilder("test7"), new Range(9, 2));

            this.sut.ToList().Should().BeEquivalentTo(
                new[]
                {
                    new Token("test", new Range(4, 8)),
                    new Token("test7", new Range(9, 2))
                });
        }

        [Fact]
        public void ShouldCombineEntriesForMatchingHashAndText()
        {
            this.sut.MergeOrAdd(new TokenHash(1234), new StringBuilder("test"), new Range(4, 8));
            this.sut.MergeOrAdd(new TokenHash(1234), new StringBuilder("test"), new Range(9, 2));

            this.sut.ToList().Should().BeEquivalentTo(
                new[]
                {
                    new Token("test", new Range(4, 8), new Range(9, 2))
                });
        }
    }
}
