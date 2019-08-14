using FluentAssertions;
using System;
using System.Linq;
using Xunit;

namespace Lifti.Tests.Preprocessing
{
    public abstract class BasicTokenizerTests
    {
        protected BasicTokenizer sut;

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

        public class WithNoPreprocessors : BasicTokenizerTests
        {
            public WithNoPreprocessors()
            {
                this.sut = new BasicTokenizer(new InputPreprocessorPipeline(Array.Empty<IInputPreprocessor>()));
            }

            [Fact]
            public void WhenSplittingAtPunctuation_ShouldTokenizeAtWordBreaksAndPunctuation()
            {
                this.sut.ConfigureWith(new FullTextIndexOptions { TokenizationOptions = { SplitOnPunctuation = true } });

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
                this.sut.ConfigureWith(new FullTextIndexOptions { TokenizationOptions = { SplitOnPunctuation = false } });

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
        }
    }
}
