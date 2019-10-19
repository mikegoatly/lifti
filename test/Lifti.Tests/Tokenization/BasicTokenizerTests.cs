using FluentAssertions;
using Lifti.Tokenization;
using System;
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
                new TokenizationOptions(TokenizerKind.Default)
                {
                    SplitOnPunctuation = splitOnPunctuation,
                    AdditionalSplitCharacters = additionalSplitChars ?? Array.Empty<char>(),
                    CaseInsensitive = caseInsensitive,
                    AccentInsensitive = accentInsensitive
                });
        }

        public class WithNoPreprocessors : BasicTokenizerTests
        {
            [Fact]
            public void ShouldReturnSingleTokenForStringContainingOnlyOneWord()
            {
                var output = this.sut.Process("test").ToList();

                output.Should().BeEquivalentTo(new[]
                {
                    new Token("test", new WordLocation(0, 0, 4))
                });
            }

            [Fact]
            public void ShouldReturnSingleTokenForStringContainingOnlyOneWordEnclosedWithWordBreaks()
            {
                var output = this.sut.Process(" test\r\n").ToList();

                output.Should().BeEquivalentTo(new[]
                {
                    new Token("test", new WordLocation(0, 1, 4))
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
                    new Token("Test", new WordLocation(0, 0, 4)),
                    new Token("string", new WordLocation(1, 5, 6)),
                    new Token("with", new WordLocation(2, 13, 4), new WordLocation(4, 31, 4)),
                    new Token("punctuation", new WordLocation(3, 18, 11)),
                    new Token("test", new WordLocation(5, 36, 4)),
                    new Token("spaces", new WordLocation(6, 41, 6))
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
                    new Token("Test", new WordLocation(0, 0, 4)),
                    new Token("string", new WordLocation(1, 5, 6)),
                    new Token("(with", new WordLocation(2, 12, 5)),
                    new Token("punctuation)", new WordLocation(3, 18, 12)),
                    new Token("with", new WordLocation(4, 31, 4)),
                    new Token("test", new WordLocation(5, 36, 4)),
                    new Token("spaces", new WordLocation(6, 41, 6))
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
                    new Token("Test", new WordLocation(0, 0, 4)),
                    new Token("string", new WordLocation(1, 5, 6)),
                    new Token("with", new WordLocation(2, 12, 4)),
                    new Token("custom", new WordLocation(3, 17, 6)),
                    new Token("splits", new WordLocation(4, 24, 6))
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
                        new Token("TEST", new WordLocation(0, 0, 4))
                    });
                }

                [Fact]
                public void ShouldReturnSingleTokenForStringContainingOnlyOneWordEnclosedWithWordBreaks()
                {
                    var output = this.sut.Process(" test\r\n").ToList();

                    output.Should().BeEquivalentTo(new[]
                    {
                        new Token("TEST", new WordLocation(0, 1, 4))
                    });
                }

                [Fact]
                public void WhenSplittingAtPunctuation_ShouldTokenizeAtWordBreaksAndPunctuation()
                {
                    var input = "Træ træ moo moǑ";

                    var output = this.sut.Process(input).ToList();

                    output.OrderBy(o => o.Value[0]).Should().BeEquivalentTo(new[]
                    {
                        new Token("MOO", new WordLocation(2, 8, 3), new WordLocation(3, 12, 3)),
                        new Token("TRAE", new WordLocation(0, 0, 3), new WordLocation(1, 4, 3))
                    });
                }
            }
        }
    }
}