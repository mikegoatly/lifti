using FluentAssertions;
using Lifti.Preprocessing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace Lifti.Tests.Preprocessing
{
    public class InputPreprocessorPipelineTests
    {
        [Fact]
        public void ShouldApplyAllProcessorsInOrder()
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
    }
}
