using System;
using Xunit;

namespace Lifti.Tests
{
    public class VirtualStringTests
    {
        private readonly VirtualString sut;

        public VirtualStringTests()
        {
            this.sut = new VirtualString(["test".AsMemory(), "string".AsMemory(), "test".AsMemory()]);
        }

        [Fact]
        public void Substring_WithValidInput_ReturnsCorrectSubstring()
        {
            var result = this.sut.Substring(3, 4);
            Assert.Equal("tstr", result);
        }

        [Fact]
        public void Substring_ShouldResetInternalStateBetweenExecutions()
        {
            var result = this.sut.Substring(3, 4);
            Assert.Equal("tstr", result);
            result = this.sut.Substring(6, 2);
            Assert.Equal("ri", result);
        }

        [Fact]
        public void Substring_WithStartIndexGreaterThanLength_ReturnsEmptyString()
        {
            var result = this.sut.Substring(20, 4);
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void Substring_WithLengthGreaterThanLength_ReturnsRemainingString()
        {
            var result = this.sut.Substring(3, 20);
            Assert.Equal("tstringtest", result);
        }

        [Fact]
        public void Substring_WithStartIndexAndLengthEqualToLength_ReturnsEntireString()
        {
            var result = this.sut.Substring(0, 15);
            Assert.Equal("teststringtest", result);
        }

        [Fact]
        public void Substring_WithStartIndexAndLengthEqualToZero_ReturnsEmptyString()
        {
            var result = this.sut.Substring(0, 0);
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void Substring_WithNegativeStartIndex_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => this.sut.Substring(-1, 10));
        }

        [Fact]
        public void Substring_WithEmptySourceText_ReturnsEmptyString()
        {
            var sut = new VirtualString([]);
            var result = sut.Substring(0, 10);
            Assert.Equal(string.Empty, result);
        }
    }
}
