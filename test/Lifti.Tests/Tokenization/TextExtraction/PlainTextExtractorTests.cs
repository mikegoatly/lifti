using System;
using System.Linq;
using FluentAssertions;
using Lifti.Tokenization.TextExtraction;
using Xunit;

namespace Lifti.Tests.Tokenization.TextExtraction
{
    public class PlainTextExtractorTests
    {
        [Fact]
        public void ShouldReturnAllTextAsOneFragment()
        {
            var sut = new PlainTextExtractor();
            var results = sut.Extract("Some text".AsMemory(), 10);

            results.Select(r => (r.Offset, r.Text.ToString()))
            .Should().BeEquivalentTo(
                (10, "Some text")
            );
        }
    }
}