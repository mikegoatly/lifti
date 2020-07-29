using FluentAssertions;
using Lifti.Tokenization;
using System.Linq;
using System.Text;
using Xunit;

namespace Lifti.Tests.Tokenization
{
    public class TokenHashTests
    {
        [Fact]
        public void ShouldGenerateUniqueHashForDifferentStrings()
        {
            var tests = new[]
            {
                "a",
                "b",
                "\\",
                "test",
                "rumplestiltskin",
                "the",
                "she",
                "foo",
                "loo"
            };

            tests.Select(t => CreateTokenHash(t).HashValue).ToHashSet()
                .Should().HaveCount(tests.Length);
        }

        [Fact]
        public void GeneratingHashFromConstructorShouldBeSameAsCombiningMultipleInstances()
        {
            var testString = "lifti";

            var constructorHash = CreateTokenHash(testString);

            var combinedHash = new TokenHash();
            foreach (var character in testString)
            {
                combinedHash = combinedHash.Combine(character);
            }

            constructorHash.HashValue.Should().Be(combinedHash.HashValue);
        }

        [Fact]
        public void EqualsForHashesWithSameValueReturnsTrue()
        {
            var first = CreateTokenHash("test");
            var second = CreateTokenHash("test");

            first.Equals(second).Should().BeTrue();
        }

        [Fact]
        public void EqualsForHashesWithDifferentValueReturnsFalse()
        {
            var first = CreateTokenHash("test");
            var second = CreateTokenHash("tesf");

            first.Equals(second).Should().BeFalse();
        }

        [Fact]
        public void GetHashCodeShouldJustReturnHashValue()
        {
            var sut = CreateTokenHash("test");

            sut.HashValue.Should().Be(sut.GetHashCode());
        }

        private TokenHash CreateTokenHash(string text)
        {
            return new TokenHash(new StringBuilder(text));
        }
    }
}
