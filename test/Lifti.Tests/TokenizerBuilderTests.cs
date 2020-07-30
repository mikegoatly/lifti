using FluentAssertions;
using Lifti.Tests.Querying;
using Lifti.Tokenization;
using System;
using Xunit;

namespace Lifti.Tests
{
    public class TokenizerBuilderTests
    {
        private static readonly TokenizationOptions expectedDefaultOptions = new TokenizationOptions()
        {
            AccentInsensitive = true,
            Stemming = false,
            AdditionalSplitCharacters = Array.Empty<char>(),
            CaseInsensitive = true,
            SplitOnPunctuation = true
        };

        [Fact]
        public void WithoutChangingTokenizerFactory_ShouldBuildTokenizer()
        {
            var builder = new TokenizerBuilder();
            builder.Build().Should().BeOfType<Tokenizer>();
        }

        [Fact]
        public void WithSpecifiedTokenizerFactory_ShouldReturnConfiguredType()
        {
            var builder = new TokenizerBuilder();
            builder.WithFactory(o => new FakeTokenizer(o) );
            builder.Build().Should().BeOfType<FakeTokenizer>().Subject
                .Options.Should().BeEquivalentTo(expectedDefaultOptions);
        }

        [Fact]
        public void WithoutApplyingAnyOptions_ShouldSetDefaultsCorrectly()
        {
            var builder = new TokenizerBuilder();
            builder.Build().Should().BeOfType<Tokenizer>().Subject
                .Options.Should().BeEquivalentTo(expectedDefaultOptions);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void WithStemming_ShouldSetTheStemmingPropertyCorrectly(bool setting)
        {
            var builder = new TokenizerBuilder();
            builder.WithStemming(setting);
            builder.Build().Should().BeOfType<Tokenizer>().Subject.Options.Stemming.Should().Be(setting);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void WithCaseInsensitivity_ShouldSetTheCaseInsensitivityPropertyCorrectly(bool setting)
        {
            var builder = new TokenizerBuilder();
            builder.CaseInsensitive(setting);
            builder.Build().Should().BeOfType<Tokenizer>().Subject.Options.CaseInsensitive.Should().Be(setting);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void WithAccentInsensitivity_ShouldSetTheAccentInsensitivityPropertyCorrectly(bool setting)
        {
            var builder = new TokenizerBuilder();
            builder.AccentInsensitive(setting);
            builder.Build().Should().BeOfType<Tokenizer>().Subject.Options.AccentInsensitive.Should().Be(setting);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void WithSplittingOnPunctuation_ShouldSetTheSplitOnPunctuationPropertyCorrectly(bool setting)
        {
            var builder = new TokenizerBuilder();
            builder.SplitOnPunctuation(setting);
            builder.Build().Should().BeOfType<Tokenizer>().Subject.Options.SplitOnPunctuation.Should().Be(setting);
        }

        [Fact]
        public void WithAdditionalSplitCharacters_ShouldSetAdditionalSplitCharactersCorrectly()
        {
            var builder = new TokenizerBuilder();
            builder.SplitOnCharacters('$', '%', '|');
            builder.Build().Should().BeOfType<Tokenizer>().Subject.Options.AdditionalSplitCharacters.Should().BeEquivalentTo(new[] { '$', '%', '|' });
        }
    }
}
