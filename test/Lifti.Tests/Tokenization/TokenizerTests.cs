using FluentAssertions;
using Lifti.Tokenization;
using Lifti.Tokenization.TextExtraction;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Lifti.Tests.Tokenization
{
    public abstract class TokenizerTests
    {
        [Fact]
        public void ShouldReturnNoTokensForEmptyString()
        {
            var sut = WithConfiguration();

            var output = Execute(sut, string.Empty);

            output.Should().BeEmpty();
        }

        [Fact]
        public void ShouldReturnNoTokensForDefaultMemoryFromNullString()
        {
            var sut = WithConfiguration();

            var output = Execute(sut, new string[] { null });

            output.Should().BeEmpty();
        }

        [Fact]
        public void ShouldReturnNoTokensForStringContainingJustWordBreakCharacters()
        {
            var sut = WithConfiguration();

            var output = Execute(sut, " \t\r\n\u2028\u2029\u000C");

            output.Should().BeEmpty();
        }

        protected static Tokenizer WithConfiguration(bool splitOnPunctuation = true, char[] additionalSplitChars = null, bool caseInsensitive = false, bool accentInsensitive = false)
        {
            return new Tokenizer(
                new TokenizationOptions()
                {
                    SplitOnPunctuation = splitOnPunctuation,
                    AdditionalSplitCharacters = additionalSplitChars ?? Array.Empty<char>(),
                    CaseInsensitive = caseInsensitive,
                    AccentInsensitive = accentInsensitive
                });
        }

        protected static IReadOnlyList<Token> Execute(Tokenizer tokenizer, params string[] textParts)
        {
            var fragments = new List<DocumentTextFragment>();
            var offset = 0;
            foreach (var text in textParts)
            {
                fragments.Add(new DocumentTextFragment(offset, text.AsMemory()));
                offset += text?.Length ?? 0;
            }

            return tokenizer.Process(fragments);
        }

        public class WithNoPreprocessors : TokenizerTests
        {
            [Fact]
            public void ShouldReturnSingleTokenForStringContainingOnlyOneWord()
            {
                var sut = WithConfiguration();

                var output = Execute(sut, "test");

                output.Should().BeEquivalentTo(new[]
                {
                    new Token("test", new TokenLocation(0, 0, 4))
                });
            }

            [Fact]
            public void ShouldReturnSeparateTokensForWordsSeparatedByNonBreakSpace()
            {
                var sut = WithConfiguration();

                var output = Execute(sut, "test split");

                output.Should().BeEquivalentTo(new[]
                {
                    new Token("test", new TokenLocation(0, 0, 4)),
                    new Token("split", new TokenLocation(1, 5, 5))
                });
            }

            [Fact]
            public void ShouldReturnSingleTokenForStringContainingOnlyOneWordEnclosedWithWordBreaks()
            {
                var sut = WithConfiguration();

                var output = Execute(sut, " test\r\n");

                output.Should().BeEquivalentTo(new[]
                {
                    new Token("test", new TokenLocation(0, 1, 4))
                });
            }

            [Fact]
            public void WhenSplittingAtPunctuation_ShouldTokenizeAtWordBreaksAndPunctuation()
            {
                var sut = WithConfiguration();

                var input = "Test string (with punctuation) with test spaces?";

                var output = Execute(sut, input);

                output.Should().BeEquivalentTo(new[]
                {
                    new Token("Test", new TokenLocation(0, 0, 4)),
                    new Token("string", new TokenLocation(1, 5, 6)),
                    new Token("with", new TokenLocation(2, 13, 4), new TokenLocation(4, 31, 4)),
                    new Token("punctuation", new TokenLocation(3, 18, 11)),
                    new Token("test", new TokenLocation(5, 36, 4)),
                    new Token("spaces", new TokenLocation(6, 41, 6))
                });
            }

            [Fact]
            public void WhenNotSplittingAtPunctuation_ShouldTokenizeAtWordBreaksOnly()
            {
                var sut = WithConfiguration(splitOnPunctuation: false);

                var input = "Test string (with punctuation) with test spaces?";

                var output = Execute(sut, input);

                output.Should().BeEquivalentTo(new[]
                {
                    new Token("Test", new TokenLocation(0, 0, 4)),
                    new Token("string", new TokenLocation(1, 5, 6)),
                    new Token("(with", new TokenLocation(2, 12, 5)),
                    new Token("punctuation)", new TokenLocation(3, 18, 12)),
                    new Token("with", new TokenLocation(4, 31, 4)),
                    new Token("test", new TokenLocation(5, 36, 4)),
                    new Token("spaces?", new TokenLocation(6, 41, 7))
                });
            }

            [Fact]
            public void WhenSplittingOnAdditionalCharacters_ShouldTokenizeAtWordBreaksAndAdditionalCharacters()
            {
                var sut = WithConfiguration(splitOnPunctuation: false, additionalSplitChars: new[] { '@', '¬' });

                var input = "Test@string¬with custom\tsplits";

                var output = Execute(sut, input);

                output.Should().BeEquivalentTo(new[]
                {
                    new Token("Test", new TokenLocation(0, 0, 4)),
                    new Token("string", new TokenLocation(1, 5, 6)),
                    new Token("with", new TokenLocation(2, 12, 4)),
                    new Token("custom", new TokenLocation(3, 17, 6)),
                    new Token("splits", new TokenLocation(4, 24, 6))
                });
            }

            public class WithAllInsensitivityProcessors : TokenizerTests
            {
                private readonly Tokenizer sut;

                public WithAllInsensitivityProcessors()
                {
                    this.sut = WithConfiguration(caseInsensitive: true, accentInsensitive: true);
                }

                [Fact]
                public void ShouldReturnSingleTokenForStringContainingOnlyOneWord()
                {
                    var output = Execute(this.sut, "test");

                    output.Should().BeEquivalentTo(new[]
                    {
                        new Token("TEST", new TokenLocation(0, 0, 4))
                    });
                }

                [Fact]
                public void ProcessingWithNonZeroOffset_ShouldReturnTokensWithExistingOffsetApplied()
                {
                    var output = Execute(this.sut, "test test2 test3");

                    output.Should().BeEquivalentTo(new[]
                    {
                        new Token("TEST", new TokenLocation(0, 0, 4)),
                        new Token("TEST2", new TokenLocation(1, 5, 5)),
                        new Token("TEST3", new TokenLocation(2, 11, 5))
                    });
                }

                [Fact]
                public void ProcessingEnumerableContainingMultipleWordStrings_ShouldReturnTokensWithContinuingIndexAndOffset()
                {
                    var output = Execute(this.sut, "test", "test2 and test3", "test4");

                    output.Should().BeEquivalentTo(new[]
                    {
                        new Token("TEST", new TokenLocation(0, 0, 4)),
                        new Token("TEST2", new TokenLocation(1, 4, 5)),
                        new Token("AND", new TokenLocation(2, 10, 3)),
                        new Token("TEST3", new TokenLocation(3, 14, 5)),
                        new Token("TEST4", new TokenLocation(4, 19, 5))
                    });
                }

                [Fact]
                public void ShouldReturnSingleTokenForStringContainingOnlyOneWordEnclosedWithWordBreaks()
                {
                    var output = Execute(this.sut, " test\r\n");

                    output.Should().BeEquivalentTo(new[]
                    {
                        new Token("TEST", new TokenLocation(0, 1, 4))
                    });
                }

                [Fact]
                public void WhenSplittingAtPunctuation_ShouldTokenizeAtWordBreaksAndPunctuation()
                {
                    var input = "Træ træ moo moǑ";

                    var output = Execute(this.sut, input);

                    output.OrderBy(o => o.Value[0]).Should().BeEquivalentTo(new[]
                    {
                        new Token("MOO", new TokenLocation(2, 8, 3), new TokenLocation(3, 12, 3)),
                        new Token("TRAE", new TokenLocation(0, 0, 3), new TokenLocation(1, 4, 3))
                    });
                }
            }
        }
    }
}