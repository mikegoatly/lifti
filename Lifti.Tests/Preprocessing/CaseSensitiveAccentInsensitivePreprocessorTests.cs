using FluentAssertions;
using Xunit;

namespace Lifti.Tests.Preprocessing
{
    public class CaseSensitiveAccentInsensitivePreprocessorTests
    {
        [Fact]
        public void ShouldSameCasingWithDiacriticsStripped()
        {
            var input = "Test šđčćž";

            var output = new CaseSensitiveAccentInsensitivePreprocessor().Preprocess(input);

            output.Should().Be("Test sdccz");
        }
    }
}
