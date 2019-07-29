using System;
using FluentAssertions;
using Lifti;
using Xunit;

namespace Lifti.Tests
{
    public class SplitWordStoreTests
    {
        private SplitWordStore sut;

        public SplitWordStoreTests()
        {
            this.sut = new SplitWordStore();
        }

        [Fact]
        public void ShouldCreateEntryForFirstCall()
        {
            this.sut.MergeOrAdd(new SplitWordHash(1234), "test".AsSpan(), new Range(4, 8));

            this.sut.ToList().Should().BeEquivalentTo(
                new[]
                {
                    new SplitWord("test", new Range(4, 8))
                });
        }

        [Fact]
        public void ShouldCreateSeparateEntryForClashingHasWithDifferentText()
        {
            this.sut.MergeOrAdd(new SplitWordHash(1234), "test".AsSpan(), new Range(4, 8));
            this.sut.MergeOrAdd(new SplitWordHash(1234), "test2".AsSpan(), new Range(9, 2));

            this.sut.ToList().Should().BeEquivalentTo(
                new[]
                {
                    new SplitWord("test", new Range(4, 8)),
                    new SplitWord("test2", new Range(9, 2))
                });
        }

        [Fact]
        public void ShouldCreateSeparateEntryForNovelHash()
        {
            this.sut.MergeOrAdd(new SplitWordHash(1234), "test".AsSpan(), new Range(4, 8));
            this.sut.MergeOrAdd(new SplitWordHash(12345), "test7".AsSpan(), new Range(9, 2));

            this.sut.ToList().Should().BeEquivalentTo(
                new[]
                {
                    new SplitWord("test", new Range(4, 8)),
                    new SplitWord("test7", new Range(9, 2))
                });
        }

        [Fact]
        public void ShouldCombineEntriesForMatchingHashAndText()
        {
            this.sut.MergeOrAdd(new SplitWordHash(1234), "test".AsSpan(), new Range(4, 8));
            this.sut.MergeOrAdd(new SplitWordHash(1234), "test".AsSpan(), new Range(9, 2));

            this.sut.ToList().Should().BeEquivalentTo(
                new[]
                {
                    new SplitWord("test", new Range(4, 8), new Range(9, 2))
                });
        }
    }
}
