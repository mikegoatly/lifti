using FluentAssertions;
using Lifti.Tokenization.TextExtraction;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Lifti.Tests.Tokenization.TextExtraction
{
    public class XmlTextExtractorTests
    {
        private readonly XmlTextExtractor sut;

        public XmlTextExtractorTests()
        {
            this.sut = new XmlTextExtractor();
        }

        [Fact]
        public void ShouldReturnAllTextIfNoXmlPresent()
        {
            var output = this.sut.Extract("test data".AsMemory(), 0).ToList();

            output.Select(o => (o.Offset, o.Text.ToString()))
                .Should().BeEquivalentTo(new[]
            {
                (0, "test data")
            });
        }

        [Fact]
        public void ShouldSkipAllTextInXmlTags()
        {
            var output = this.sut.Extract("test <data tag='foo'>inside</data>outside".AsMemory(), 0).ToList();

            output.Select(o => (o.Offset, o.Text.ToString()))
                .Should().BeEquivalentTo(new[]
            {
                (0, "test "),
                (21, "inside"),
                (34, "outside")
            });
        }

        [Fact]
        public void ShouldApplyOffsetToExtractedFragments()
        {
            var output = this.sut.Extract("test <data tag='foo'>inside</data>outside".AsMemory(), 10).ToList();

            output.Select(o => (o.Offset, o.Text.ToString()))
                .Should().BeEquivalentTo(new[]
            {
                (10, "test "),
                (31, "inside"),
                (44, "outside")
            });
        }

        [Fact]
        public void ShouldIgnorePresenceOfCloseAngleBracketsInAttributeValues()
        {
            var output = this.sut.Extract("test <data tag='foo>'>inside</data>outside".AsMemory(), 0).ToList();

            output.Select(o => (o.Offset, o.Text.ToString()))
                .Should().BeEquivalentTo(new[]
            {
                (0, "test "),
                (22, "inside"),
                (35, "outside")
            });
        }

        [Fact]
        public async Task IndexingXmlText_ShouldResultInWordsWithCorrectLocationsAndIndexes()
        {
            var index = new FullTextIndexBuilder<int>()
                .WithDefaultTokenization(t => t.WithStemming())
                .WithTextExtractor<XmlTextExtractor>()
                .Build();

            await index.AddAsync(1, "<div>test <b>test</b> <i>testing</i> test tested</div>");

            var results = index.Search("test");

            results.Single().FieldMatches.Single().Locations.Should().BeEquivalentTo(new[]
            {
                new TokenLocation(0, 5, 4),
                new TokenLocation(1, 13, 4),
                new TokenLocation(2, 25, 7),
                new TokenLocation(3, 37, 4),
                new TokenLocation(4, 42, 6)
            });
        }
    }
}
