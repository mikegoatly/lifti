using FluentAssertions;
using Lifti.Tokenization;
using Lifti.Tokenization.Preprocessing;
using System.Linq;
using Xunit;

namespace Lifti.Tests.Tokenization.Preprocessing
{
    public class InputPreprocessorPipelineTests
    {
        [Fact]
        public void WithMultiplePreprocessors_ShouldApplyAllProcessorsInOrder()
        {
            var input = 'Ч';
            var expectedOutput = "CH";

            var pipeline = CreatePipeline(caseInsensitive: true, accentInsensitive: true);

            var actual = pipeline.Process(input);

            actual.ToArray().Should().BeEquivalentTo(expectedOutput);
        }

        [Fact]
        public void WithSinglePreprocessor_ShouldApplyProvidedPreprocesor()
        {
            var input = 'Ч';
            var expectedOutput = "Ch";

            var pipeline = CreatePipeline(accentInsensitive: true);

            var actual = pipeline.Process(input);

            actual.ToArray().Should().BeEquivalentTo(expectedOutput);
        }

        [Fact]
        public void WithNoPreprocessors_ShouldReturnInput()
        {
            var input = 'Ч';

            var pipeline = CreatePipeline();

            var actual = pipeline.Process(input);

            actual.ToArray().Should().BeEquivalentTo(new[] { input });
        }

        private static IInputPreprocessorPipeline CreatePipeline(bool caseInsensitive = false, bool accentInsensitive = false)
        {
            var pipeline = new InputPreprocessorPipeline();
            ((IConfiguredBy<TokenizationOptions>)pipeline).Configure(
                new TokenizationOptions(TokenizerKind.PlainText)
                {
                    CaseInsensitive = caseInsensitive,
                    AccentInsensitive = accentInsensitive
                });
            return pipeline;
        }
    }
}
