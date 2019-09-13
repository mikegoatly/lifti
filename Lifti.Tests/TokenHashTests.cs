﻿using FluentAssertions;
using Lifti.Preprocessing;
using System.Linq;
using Xunit;

namespace Lifti.Tests
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

            tests.Select(t => new TokenHash(t).HashValue).ToHashSet()
                .Should().HaveCount(tests.Length);
        }

        [Fact]
        public void GeneratingHashFromConstructorShouldBeSameAsCombiningMultipleInstances()
        {
            var testString = "lifti";

            var constructorHash = new TokenHash(testString);

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
            var first = new TokenHash("test");
            var second = new TokenHash("test");

            first.Equals(second).Should().BeTrue();
        }

        [Fact]
        public void EqualsForHashesWithDifferentValueReturnsFalse()
        {
            var first = new TokenHash("test");
            var second = new TokenHash("tesf");

            first.Equals(second).Should().BeFalse();
        }

        [Fact]
        public void GetHashCodeShouldJustReturnHashValue()
        {
            var sut = new TokenHash("test");

            sut.HashValue.Should().Be(sut.GetHashCode());
        }
    }
}
