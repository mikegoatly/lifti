using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FluentAssertions;
using Lifti;
using Xunit;

namespace Lifti.Tests
{
    public class SplitWordHashTests
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

            tests.Select(t => new SplitWordHash(t).HashValue).ToHashSet()
                .Should().HaveCount(tests.Length);
        }

        [Fact]
        public void GeneratingHashFromConstructorShouldBeSameAsCombiningMultipleInstances()
        {
            var testString = "lifti";

            var constructorHash = new SplitWordHash(testString);

            var combinedHash = new SplitWordHash();
            foreach (var character in testString)
            {
                combinedHash = combinedHash.Combine(character);
            }

            constructorHash.HashValue.Should().Be(combinedHash.HashValue);
        }

        [Fact]
        public void EqualsForHashesWithSameValueReturnsTrue()
        {
            var first = new SplitWordHash("test");
            var second = new SplitWordHash("test");

            first.Equals(second).Should().BeTrue();
        }

        [Fact]
        public void EqualsForHashesWithDifferentValueReturnsFalse()
        {
            var first = new SplitWordHash("test");
            var second = new SplitWordHash("tesf");

            first.Equals(second).Should().BeFalse();
        }

        [Fact]
        public void GetHashCodeShouldJustReturnHashValue()
        {
            var sut = new SplitWordHash("test");

            sut.HashValue.Should().Be(sut.GetHashCode());
        }
    }
}
