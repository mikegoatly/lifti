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
            new StringBuilder(a).SequenceEqual(b).Should().BeFalse();
        }

        [Fact]
        public void WhenBothStringsEmptyShouldReturnTrue()
        {
            new StringBuilder().SequenceEqual(string.Empty).Should().BeTrue();
        }

        [Fact]
        public void WhenBothStringsHaveSameCharactersShouldReturnTrue()
        {
            new StringBuilder("test").SequenceEqual("test").Should().BeTrue();
        }

        [Fact]
        public void WhenBothStringsHaveDifferentCharactersShouldReturnFalse()
        {
            new StringBuilder("best").SequenceEqual("test").Should().BeFalse();
        }
    }
}
