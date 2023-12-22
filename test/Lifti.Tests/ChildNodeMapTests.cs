using FluentAssertions;
using System;
using Xunit;

namespace Lifti.Tests
{
    public class ChildNodeMapTests
    {
        [Fact]
        public void TryGetValue_WhenNoCharacters_ReturnsFalse()
        {
            var sut = new ChildNodeMap();
            sut.TryGetValue('A', out var nextNode).Should().BeFalse();
        }

        [Fact]
        public void TryGetValue_WithSingleMatchingCharacter_ReturnsMatch()
        {
            var sut = new ChildNodeMap([CreateTestIndexNodeMap('A')]);

            this.VerifySuccessfulMatch(sut, 'A');
            this.VerifyUnsuccessfulMatch(sut, 'B');
            this.VerifyUnsuccessfulMatch(sut, 'a');
        }

        [Fact]
        public void TryGetValue_WithTwoCharacters_ReturnsMatch()
        {
            var sut = new ChildNodeMap(
                [
                    CreateTestIndexNodeMap('A'),
                    CreateTestIndexNodeMap('Z')
                ]);

            this.VerifySuccessfulMatch(sut, 'A');
            this.VerifySuccessfulMatch(sut, 'Z');
            this.VerifyUnsuccessfulMatch(sut, 'B');
            this.VerifyUnsuccessfulMatch(sut, 'a');
        }

        [Fact]
        public void TryGetValue_WithFiveCharacters_ReturnsMatch()
        {
            var sut = new ChildNodeMap(
                [
                    CreateTestIndexNodeMap('E'),
                    CreateTestIndexNodeMap('H'),
                    CreateTestIndexNodeMap('L'),
                    CreateTestIndexNodeMap('N'),
                    CreateTestIndexNodeMap('P')
                ]);

            this.VerifySuccessfulMatch(sut, 'E');
            this.VerifySuccessfulMatch(sut, 'H');
            this.VerifySuccessfulMatch(sut, 'L');
            this.VerifySuccessfulMatch(sut, 'N');
            this.VerifySuccessfulMatch(sut, 'P');
            this.VerifyUnsuccessfulMatch(sut, 'M');
            this.VerifyUnsuccessfulMatch(sut, 'B');
            this.VerifyUnsuccessfulMatch(sut, 'Z');
            this.VerifyUnsuccessfulMatch(sut, 'a');
            this.VerifyUnsuccessfulMatch(sut, 'e');
        }

        private void VerifySuccessfulMatch(ChildNodeMap sut, char character)
        {
            sut.TryGetValue(character, out var nextNode).Should().BeTrue();
            nextNode!.IntraNodeText.ToString().Should().BeEquivalentTo(character.ToString());
        }

        private void VerifyUnsuccessfulMatch(ChildNodeMap sut, char character)
        {
            sut.TryGetValue(character, out var nextNode).Should().BeFalse();
        }

        private static ChildNodeMapEntry CreateTestIndexNodeMap(char character)
        {
            return new(
                character,
                new IndexNode(character.ToString().AsMemory(), new ChildNodeMap(), new DocumentTokenMatchMap()));
        }
    }
}
