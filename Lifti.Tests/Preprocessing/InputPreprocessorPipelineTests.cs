using FluentAssertions;
using Lifti.Preprocessing;
using System;
using System.Linq;
using Xunit;

namespace Lifti.Tests.Preprocessing
{
    public class InputPreprocessorPipelineTests
    {
        [Fact]
        public void WithMultiplePreprocessors_ShouldApplyAllProcessorsInOrder()
        {
            var input = 'Ч';
            var expectedOutput = "CH";

            var pipeline = new InputPreprocessorPipeline(
                new IInputPreprocessor[]
                {
                    new LatinCharacterNormalizer(),
                    new CaseInsensitiveNormalizer()
                });

            var actual = pipeline.Process(input);

            actual.ToArray().Should().BeEquivalentTo(expectedOutput);
        }

        [Fact]
        public void WithSinglePreprocessor_ShouldApplyProvidedPreprocesor()
        {
            var input = 'Ч';
            var expectedOutput = "Ch";

            var pipeline = new InputPreprocessorPipeline(
                new IInputPreprocessor[]
                {
                    new LatinCharacterNormalizer()
                });

            var actual = pipeline.Process(input);

            actual.ToArray().Should().BeEquivalentTo(expectedOutput);
        }

        [Fact]
        public void WithNoPreprocessors_ShouldReturnInput()
        {
            var input = 'Ч';

            var pipeline = new InputPreprocessorPipeline(Array.Empty<IInputPreprocessor>());

            var actual = pipeline.Process(input);

            actual.ToArray().Should().BeEquivalentTo(new[] { input });
        }
    }
}
