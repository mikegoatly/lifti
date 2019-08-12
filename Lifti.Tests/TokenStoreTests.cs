using FluentAssertions;
using System;
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
            this.sut.MergeOrAdd(new TokenHash(1234), "test".AsSpan(), new Range(4, 8));

            this.sut.ToList().Should().BeEquivalentTo(
                new[]
                {
                    new Token("test", new Range(4, 8))
                });
        }

        [Fact]
        public void ShouldCreateSeparateEntryForClashingHasWithDifferentText()
        {
            this.sut.MergeOrAdd(new TokenHash(1234), "test".AsSpan(), new Range(4, 8));
            this.sut.MergeOrAdd(new TokenHash(1234), "test2".AsSpan(), new Range(9, 2));

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
            this.sut.MergeOrAdd(new TokenHash(1234), "test".AsSpan(), new Range(4, 8));
            this.sut.MergeOrAdd(new TokenHash(12345), "test7".AsSpan(), new Range(9, 2));

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
            this.sut.MergeOrAdd(new TokenHash(1234), "test".AsSpan(), new Range(4, 8));
            this.sut.MergeOrAdd(new TokenHash(1234), "test".AsSpan(), new Range(9, 2));

            this.sut.ToList().Should().BeEquivalentTo(
                new[]
                {
                    new Token("test", new Range(4, 8), new Range(9, 2))
                });
        }
    }
}
