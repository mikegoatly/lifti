using FluentAssertions;
using Xunit;

namespace Lifti.Tests.Preprocessing
{
    public class CaseInsensitiveAccentInsensitivePreprocessorTests
    {
        [Fact]
        public void ShouldReturnUpperCaseTextWithDiacriticsStripped()
        {
            var input = "Test šđčćž";

            var output = new CaseInsensitiveAccentInsensitivePreprocessor().Preprocess(input);

            output.Should().Be("TEST SDCCZ");
        }
    }
}
