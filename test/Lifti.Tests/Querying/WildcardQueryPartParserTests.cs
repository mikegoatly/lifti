using FluentAssertions;
using Lifti.Querying;
using Lifti.Querying.QueryParts;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

using static Lifti.Querying.QueryParts.WildcardQueryFragment;

namespace Lifti.Tests.Querying
{
    public class WildcardQueryPartParserTests
    {
        [Fact]
        public void TextOnly_ShouldReturnFalse()
        {
            RunTest("Foo", null!, false);
        }

        [Fact]
        public void TextWithWildcard_ShouldNormalizeText()
        {
            RunTest("Foo*", new WildcardQueryPart(CreateText("FOO"), MultiCharacter));
        }

        [Fact]
        public void MultipleWildcards_ShouldBeCollapsed()
        {
            RunTest("****", new WildcardQueryPart(MultiCharacter));
        }

        [Fact]
        public void SingleWildcardFollowingMultiple_ShouldThrowException()
        {
            Assert.Throws<QueryParserException>(() => RunTest("****%", null!));
        }

        [Fact]
        public void SingleWildcardPrecedingMultiple_ShouldReturnValidPart()
        {
            RunTest("%*", new WildcardQueryPart(SingleCharacter, MultiCharacter));
        }

        [Fact]
        public void MultipleSingleWildcard_ShouldReturnValidPart()
        {
            RunTest("%%%", new WildcardQueryPart(SingleCharacter, SingleCharacter, SingleCharacter));
        }

        [Fact]
        public void MixOfFragments_ShouldReturnValidPart()
        {
            RunTest("%%foo*bar", new WildcardQueryPart(SingleCharacter, SingleCharacter, CreateText("FOO"), MultiCharacter, CreateText("BAR")));
        }

        private static void RunTest(string text, WildcardQueryPart? expectedQueryPart, bool expectedResult = true)
        {
            var result = WildcardQueryPartParser.TryParse(text.AsSpan(), new FakeIndexTokenizer(normalizeToUppercase: true), out var part);

            result.Should().Be(expectedResult);
            if (expectedQueryPart != null)
            {
                part!.Fragments.Should().BeEquivalentTo(expectedQueryPart.Fragments);
            }
        }
    }
}
