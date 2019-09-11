using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Lifti.Tests.Preprocessing
{
    public class StringBuilderExtensionTests_SequenceEqual
    {
        [Theory]
        [InlineData("", "1")]
        [InlineData("1", "")]
        [InlineData("11", "1")]
        public void WhenStringsDifferentLengthsShouldReturnFalse(string a, string b)
        {
            new StringBuilder(a).SequenceEqual(b.ToCharArray()).Should().BeFalse();
        }

        [Fact]
        public void WhenBothStringsEmptyShouldReturnTrue()
        {
            new StringBuilder().SequenceEqual(Array.Empty<char>()).Should().BeTrue();
        }

        [Fact]
        public void WhenBothStringsHaveSameCharactersShouldReturnTrue()
        {
            new StringBuilder("test").SequenceEqual("test".ToCharArray()).Should().BeTrue();
        }

        [Fact]
        public void WhenBothStringsHaveDifferentCharactersShouldReturnFalse()
        {
            new StringBuilder("best").SequenceEqual("test".ToCharArray()).Should().BeFalse();
        }
    }
}
