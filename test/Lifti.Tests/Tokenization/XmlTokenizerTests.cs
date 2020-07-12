using FluentAssertions;
using Lifti.Tokenization;
using System.Linq;
using Xunit;

namespace Lifti.Tests.Tokenization
{
    public class XmlTokenizerTests
    {
        private readonly XmlTokenizer sut;

        public XmlTokenizerTests()
        {
            this.sut = new XmlTokenizer();
            ((ITokenizer)this.sut).Configure(TokenizationOptions.Default);
        }

        [Fact]
        public void ShouldSkipAllTextInXmlTags()
        {
            var output = this.sut.Process("test <data tag='foo'>inside</data>outside").ToList();

            output.OrderBy(o => o.Value[0]).Should().BeEquivalentTo(new[]
            {
                new Token("INSIDE", new TokenLocation(1, 21, 6)),
                new Token("OUTSIDE", new TokenLocation(2, 34, 7)),
                new Token("TEST", new TokenLocation(0, 0, 4))
            });
        }

        [Fact]
        public void ShouldIgnorePresenceOfCloseAngleBracketsInAttributeValues()
        {
            var output = this.sut.Process("test <data tag='foo>'>inside</data>outside").ToList();

            output.OrderBy(o => o.Value[0]).Should().BeEquivalentTo(new[]
            {
                new Token("INSIDE", new TokenLocation(1, 22, 6)),
                new Token("OUTSIDE", new TokenLocation(2, 35, 7)),
                new Token("TEST", new TokenLocation(0, 0, 4))
            });
        }
    }
}
