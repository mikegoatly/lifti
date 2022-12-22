using FluentAssertions;
using Lifti.Tokenization;
using Lifti.Tokenization.Preprocessing;
using System;
using System.Linq;
using Xunit;

namespace Lifti.Tests.Tokenization.Preprocessing
{
    public class InputPreprocessorPipelineTests
    {
        [Fact]
        public void WithCaseInsensitiveAndAccentInsensitivePreprocessors_ShouldApplyAccentRulesThenCaseInsensitivity()
        {
            var input = 'Ч';
            var expectedOutput = "CH";

            var pipeline = CreatePipeline(caseInsensitive: true, accentInsensitive: true);

            var actual = pipeline.Process(input);

            actual.Should().BeEquivalentTo(expectedOutput);
        }

        [Fact]
        public void WithOnlyCaseInsensitivePreprocessor()
        {
            var input = 'Ч';
            var expectedOutput = "Ch";

            var pipeline = CreatePipeline(accentInsensitive: true);

            var actual = pipeline.Process(input);

            actual.Should().BeEquivalentTo(expectedOutput);
        }

        [Fact]
        public void WithNoPreprocessors_ShouldReturnInput()
        {
            var input = 'Ч';

            var pipeline = CreatePipeline();

            var actual = pipeline.Process(input);

            actual.Should().BeEquivalentTo(new[] { input });
        }

        [Fact]
        public void WithIgnoreCharacters_ShouldReturnEmptyForIgnoredCharacter()
        {
            var input = 'Ч';

            var pipeline = CreatePipeline(ignoreCharacters: new[] { input });

            pipeline.Process(input).Should().BeEmpty();

            pipeline.Process('p').Should().BeEquivalentTo(new[] { 'p' });
        }

        private static IInputPreprocessorPipeline CreatePipeline(bool caseInsensitive = false, bool accentInsensitive = false, char[]? ignoreCharacters = null)
        {
            return new InputPreprocessorPipeline(new TokenizationOptions()
            {
                CaseInsensitive = caseInsensitive,
                AccentInsensitive = accentInsensitive,
                IgnoreCharacters = ignoreCharacters ?? Array.Empty<char>()
            });
        }
    }
}
