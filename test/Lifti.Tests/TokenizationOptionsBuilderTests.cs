using FluentAssertions;
using Lifti.Tokenization;
using System;
using Xunit;

namespace Lifti.Tests
{
    public class TokenizationOptionsBuilderTests
    {
        [Fact]
        public void WithoutApplyingAnyOptions_ShouldSetDefaultsCorrectly()
        {
            var builder = new TokenizationOptionsBuilder();
            builder.Build().Should().BeEquivalentTo(new TokenizationOptions()
            {
                AccentInsensitive = true,
                Stemming = false,
                AdditionalSplitCharacters = Array.Empty<char>(),
                CaseInsensitive = true,
                SplitOnPunctuation = true
            });
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void WithStemming_ShouldSetTheStemmingPropertyCorrectly(bool setting)
        {
            var builder = new TokenizationOptionsBuilder();
            builder.WithStemming(setting);
            builder.Build().Stemming.Should().Be(setting);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void WithCaseInsensitivity_ShouldSetTheCaseInsensitivityPropertyCorrectly(bool setting)
        {
            var builder = new TokenizationOptionsBuilder();
            builder.CaseInsensitive(setting);
            builder.Build().CaseInsensitive.Should().Be(setting);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void WithAccentInsensitivity_ShouldSetTheAccentInsensitivityPropertyCorrectly(bool setting)
        {
            var builder = new TokenizationOptionsBuilder();
            builder.AccentInsensitive(setting);
            builder.Build().AccentInsensitive.Should().Be(setting);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void WithSplittingOnPunctuation_ShouldSetTheSplitOnPunctuationPropertyCorrectly(bool setting)
        {
            var builder = new TokenizationOptionsBuilder();
            builder.SplitOnPunctuation(setting);
            builder.Build().SplitOnPunctuation.Should().Be(setting);
        }

        [Fact]
        public void WithAdditionalSplitCharacters_ShouldSetAdditionalSplitCharactersCorrectly()
        {
            var builder = new TokenizationOptionsBuilder();
            builder.SplitOnCharacters('$', '%', '|');
            builder.Build().AdditionalSplitCharacters.Should().BeEquivalentTo(new[] { '$', '%', '|' });
        }
    }
}
