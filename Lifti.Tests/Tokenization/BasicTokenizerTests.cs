using FluentAssertions;
using Lifti.Tokenization;
using System.Linq;
using Xunit;

namespace Lifti.Tests.Tokenization
{
    public abstract class BasicTokenizerTests
    {
        protected BasicTokenizer sut = new BasicTokenizer();

        [Fact]
        public void ShouldReturnNoTokensForEmptyString()
        {
            var output = this.sut.Process(string.Empty).ToList();

            output.Should().BeEmpty();
        }

        [Fact]
        public void ShouldReturnNoTokensForStringContainingJustWordBreakCharacters()
        {
            var output = this.sut.Process(" \t\r\n\u2028\u2029\u000C").ToList();

            output.Should().BeEmpty();
        }

        protected void WithConfiguration(bool splitOnPunctuation = true, char[] additionalSplitChars = null, bool caseInsensitive = false, bool accentInsensitive = false)
        {
            ((IConfiguredBy<TokenizationOptions>)this.sut).Configure(
                new TokenizationOptions(
                    TokenizerKind.Default,
                    splitOnPunctuation: splitOnPunctuation,
                    additionalSplitCharacters: additionalSplitChars,
                    caseInsensitive: caseInsensitive,
                    accentInsensitive: accentInsensitive));
        }

        public class WithNoPreprocessors : BasicTokenizerTests
        {
            [Fact]
            public void ShouldReturnSingleTokenForStringContainingOnlyOneWord()
            {
                var output = this.sut.Process("test").ToList();

                output.Should().BeEquivalentTo(new[]
                {
                new Token("test", new Range(0, 4))
            });
            }

            [Fact]
            public void ShouldReturnSingleTokenForStringContainingOnlyOneWordEnclosedWithWordBreaks()
            {
                var output = this.sut.Process(" test\r\n").ToList();

                output.Should().BeEquivalentTo(new[]
                {
                new Token("test", new Range(1, 4))
            });
            }

            [Fact]
            public void WhenSplittingAtPunctuation_ShouldTokenizeAtWordBreaksAndPunctuation()
            {
                this.WithConfiguration();

                var input = "Test string (with punctuation) with test spaces";

                var output = this.sut.Process(input).ToList();

                output.Should().BeEquivalentTo(new[]
                {
                    new Token("Test", new Range(0, 4)),
                    new Token("string", new Range(5, 6)),
                    new Token("with", new Range(13, 4), new Range(31, 4)),
                    new Token("punctuation", new Range(18, 11)),
                    new Token("test", new Range(36, 4)),
                    new Token("spaces", new Range(41, 6))
                });
            }

            [Fact]
            public void WhenNotSplittingAtPunctuation_ShouldTokenizeAtWordBreaksOnly()
            {
                this.WithConfiguration(splitOnPunctuation: false);

                var input = "Test string (with punctuation) with test spaces";

                var output = this.sut.Process(input).ToList();

                output.Should().BeEquivalentTo(new[]
                {
                    new Token("Test", new Range(0, 4)),
                    new Token("string", new Range(5, 6)),
                    new Token("(with", new Range(12, 5)),
                    new Token("punctuation)", new Range(18, 12)),
                    new Token("with", new Range(31, 4)),
                    new Token("test", new Range(36, 4)),
                    new Token("spaces", new Range(41, 6))
                });
            }

            [Fact]
            public void WhenSplittingOnAdditionalCharacters_ShouldTokenizeAtWordBreaksAndAdditionalCharacters()
            {
                this.WithConfiguration(splitOnPunctuation: false, additionalSplitChars: new[] { '@', '¬' });

                var input = "Test@string¬with custom\tsplits";

                var output = this.sut.Process(input).ToList();

                output.Should().BeEquivalentTo(new[]
                {
                    new Token("Test", new Range(0, 4)),
                    new Token("string", new Range(5, 6)),
                    new Token("with", new Range(12, 4)),
                    new Token("custom", new Range(17, 6)),
                    new Token("splits", new Range(24, 6))
                });
            }

            public class WithAllInsensitivityProcessors : BasicTokenizerTests
            {
                public WithAllInsensitivityProcessors()
                {
                    this.WithConfiguration(caseInsensitive: true, accentInsensitive: true);
                }

                [Fact]
                public void ShouldReturnSingleTokenForStringContainingOnlyOneWord()
                {
                    var output = this.sut.Process("test").ToList();

                    output.Should().BeEquivalentTo(new[]
                    {
                        new Token("TEST", new Range(0, 4))
                    });
                }

                [Fact]
                public void ShouldReturnSingleTokenForStringContainingOnlyOneWordEnclosedWithWordBreaks()
                {
                    var output = this.sut.Process(" test\r\n").ToList();

                    output.Should().BeEquivalentTo(new[]
                    {
                        new Token("TEST", new Range(1, 4))
                    });
                }

                [Fact]
                public void WhenSplittingAtPunctuation_ShouldTokenizeAtWordBreaksAndPunctuation()
                {
                    var input = "Træ træ moo moǑ";

                    var output = this.sut.Process(input).ToList();

                    output.OrderBy(o => o.Value[0]).Should().BeEquivalentTo(new[]
                    {
                        new Token("MOO", new Range(8, 3), new Range(12, 3)),
                        new Token("TRAE", new Range(0, 3), new Range(4, 3))
                    });
                }
            }
        }
    }
}
