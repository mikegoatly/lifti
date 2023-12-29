using FluentAssertions;
using Lifti.Tests.Querying;
using Lifti.Tokenization;
using Lifti.Tokenization.Stemming;
using System;
using System.Text;
using Xunit;

namespace Lifti.Tests
{
    public class TokenizerBuilderTests
    {
        private static readonly TokenizationOptions expectedDefaultOptions = new()
        {
            AccentInsensitive = true,
            AdditionalSplitCharacters = Array.Empty<char>(),
            CaseInsensitive = true,
            SplitOnPunctuation = true
        };

        [Fact]
        public void WithoutChangingTokenizerFactory_ShouldBuildTokenizer()
        {
            var builder = new TokenizerBuilder();
            builder.Build().Should().BeOfType<IndexTokenizer>();
        }

        [Fact]
        public void WithSpecifiedTokenizerFactory_ShouldReturnConfiguredType()
        {
            var builder = new TokenizerBuilder();
            builder.WithFactory(o => new FakeIndexTokenizer(o) );
            builder.Build().Should().BeOfType<FakeIndexTokenizer>().Subject
                .Options.Should().BeEquivalentTo(expectedDefaultOptions);
        }

        [Fact]
        public void WithoutApplyingAnyOptions_ShouldSetDefaultsCorrectly()
        {
            var builder = new TokenizerBuilder();
            builder.Build().Should().BeOfType<IndexTokenizer>().Subject
                .Options.Should().BeEquivalentTo(expectedDefaultOptions);
        }

        [Fact]
        public void WithoutStemming_ShouldLeaveStemmerNull()
        {
            var builder = new TokenizerBuilder();
            builder.Build().Should().BeOfType<IndexTokenizer>().Subject.Options.Stemmer.Should().BeNull();
        }

        [Fact]
        public void WithStemming_ShouldSetTheStemmerToAPorterStemmer()
        {
            var builder = new TokenizerBuilder();
            builder.WithStemming(true);
            builder.Build().Should().BeOfType<IndexTokenizer>().Subject.Options.Stemmer.Should().BeOfType<PorterStemmer>();
        }

        [Fact]
        public void WithCustomStemmer_ShouldSetTheStemmerToAProvidedStemmer()
        {
            var builder = new TokenizerBuilder();
            builder.WithStemming(new CustomStemmer(true, true));
            builder.Build().Should().BeOfType<IndexTokenizer>().Subject.Options.Stemmer.Should().BeOfType<CustomStemmer>();
        }

        [Fact]
        public void StemmerInsensitivityRequirements_ShouldAffectIndexInsensitivityOptions()
        {
            var builder = new TokenizerBuilder()
                .AccentInsensitive(false)
                .CaseInsensitive(false)
                .WithStemming(new CustomStemmer(true, false));

            var options = builder.Build().Should().BeOfType<IndexTokenizer>().Subject.Options;
            options.CaseInsensitive.Should().BeTrue();
            options.AccentInsensitive.Should().BeFalse();

            builder = new TokenizerBuilder()
                .AccentInsensitive(false)
                .CaseInsensitive(false)
                .WithStemming(new CustomStemmer(false, true));

            options = builder.Build().Should().BeOfType<IndexTokenizer>().Subject.Options;
            options.CaseInsensitive.Should().BeFalse();
            options.AccentInsensitive.Should().BeTrue();
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void WithCaseInsensitivity_ShouldSetTheCaseInsensitivityPropertyCorrectly(bool setting)
        {
            var builder = new TokenizerBuilder();
            builder.CaseInsensitive(setting);
            builder.Build().Should().BeOfType<IndexTokenizer>().Subject.Options.CaseInsensitive.Should().Be(setting);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void WithAccentInsensitivity_ShouldSetTheAccentInsensitivityPropertyCorrectly(bool setting)
        {
            var builder = new TokenizerBuilder();
            builder.AccentInsensitive(setting);
            builder.Build().Should().BeOfType<IndexTokenizer>().Subject.Options.AccentInsensitive.Should().Be(setting);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void WithSplittingOnPunctuation_ShouldSetTheSplitOnPunctuationPropertyCorrectly(bool setting)
        {
            var builder = new TokenizerBuilder();
            builder.SplitOnPunctuation(setting);
            builder.Build().Should().BeOfType<IndexTokenizer>().Subject.Options.SplitOnPunctuation.Should().Be(setting);
        }

        [Fact]
        public void WithAdditionalSplitCharacters_ShouldSetAdditionalSplitCharactersCorrectly()
        {
            var builder = new TokenizerBuilder();
            builder.SplitOnCharacters('$', '%', '|');
            builder.Build().Should().BeOfType<IndexTokenizer>().Subject.Options.AdditionalSplitCharacters.Should().BeEquivalentTo(new[] { '$', '%', '|' });
        }

        [Fact]
        public void WithIgnoreCharacters_ShouldSetUpIgnoreCharacterListCorrectly()
        {
            var builder = new TokenizerBuilder();
            builder.IgnoreCharacters('\'', '`');
            builder.Build().Should().BeOfType<IndexTokenizer>().Subject.Options.IgnoreCharacters.Should().BeEquivalentTo(new[] { '\'', '`' });
        }

        private class CustomStemmer : IStemmer
        {
            public CustomStemmer(bool requireCaseInsensitivity, bool requireAccentInsensitivity)
            {
                this.RequiresCaseInsensitivity = requireCaseInsensitivity;
                this.RequiresAccentInsensitivity = requireAccentInsensitivity;
            }

            public bool RequiresCaseInsensitivity { get; private set; }

            public bool RequiresAccentInsensitivity { get; private set; }

            public void Stem(StringBuilder builder)
            {
                builder.Length = 1;
            }
        }
    }
}
