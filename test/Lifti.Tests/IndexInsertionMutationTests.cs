using Lifti.Tokenization;
using System;
using Xunit;

namespace Lifti.Tests
{
    public class IndexInsertionMutationTests : MutationTestBase
    {

        [Fact]
        public void IndexingEmptyNode_ShouldResultInItemsDirectlyIndexedAtNode()
        {
            this.Sut.Add(Item1, FieldId1, new Token("test", this.Locations1.Locations));
            var result = this.Sut.Apply();

            VerifyResult(result, "test", [(Item1, this.Locations1)]);
        }

        [Theory]
        [InlineData("test")]
        [InlineData("a")]
        public void IndexingAtNodeWithSameTextForDifferentItem_ShouldResultInItemsDirectlyIndexedAtNode(string word)
        {
            this.Sut.Add(Item1, FieldId1, new Token(word, this.Locations1.Locations));
            this.Sut.Add(Item2, FieldId1, new Token(word, this.Locations2.Locations));
            var result = this.Sut.Apply();

            VerifyResult(result, word, [(Item1, this.Locations1), (Item2, this.Locations2)]);
        }

        [Fact]
        public void IndexingWordEndingAtSplit_ShouldResultInItemIndexedWhereSplitOccurs()
        {
            this.Sut.Add(Item1, FieldId1, new Token("apple", this.Locations1.Locations));
            this.Sut.Add(Item2, FieldId1, new Token("able", this.Locations2.Locations));
            this.Sut.Add(Item3, FieldId1, new Token("banana", this.Locations3.Locations));
            this.Sut.Add(Item4, FieldId1, new Token("a", this.Locations4.Locations));
            var result = this.Sut.Apply();

            VerifyResult(result, null, expectedChildNodes: ['a', 'b']);
            VerifyResult(result, ['a'], null, [(Item4, this.Locations4)], ['b', 'p']);
            VerifyResult(result, ['b'], "anana", [(Item3, this.Locations3)]);
            VerifyResult(result, ['a', 'b'], "le", [(Item2, this.Locations2)]);
            VerifyResult(result, ['a', 'p'], "ple", [(Item1, this.Locations1)]);
        }

        [Fact]
        public void IndexingWhenChildNodeAlreadyExists_ShouldContinueIndexingAtExistingChild()
        {
            this.Sut.Add(Item1, FieldId1, new Token("freedom", this.Locations1.Locations));
            this.Sut.Add(Item2, FieldId1, new Token("fred", this.Locations2.Locations));
            this.Sut.Add(Item3, FieldId1, new Token("freddy", this.Locations3.Locations));
            var result = this.Sut.Apply();

            VerifyResult(result, "fre", expectedChildNodes: ['d', 'e']);
            VerifyResult(result, ['e'], "dom", [(Item1, this.Locations1)]);
            VerifyResult(result, ['d'], null, [(Item2, this.Locations2)], ['d']);
            VerifyResult(result, ['d', 'd'], "y", [(Item3, this.Locations3)]);
        }

        [Fact]
        public void IndexingAtNodeWithTextWithSameSuffix_ShouldCreateNewChildNode()
        {
            this.Sut.Add(Item1, FieldId1, new Token("test", this.Locations1.Locations));
            this.Sut.Add(Item2, FieldId1, new Token("testing", this.Locations2.Locations));
            this.Sut.Add(Item3, FieldId1, new Token("tester", this.Locations3.Locations));
            var result = this.Sut.Apply();

            VerifyResult(result, "test", [(Item1, this.Locations1)], ['e', 'i']);
            VerifyResult(result, ['i'], "ng", [(Item2, this.Locations2)]);
            VerifyResult(result, ['e'], "r", [(Item3, this.Locations3)]);
        }

        [Fact]
        public void IndexingAtNodeAlreadySplit_ShouldMaintainMatchesAtFirstSplitNode()
        {
            this.Sut.Add(Item1, FieldId1, new Token("broker", this.Locations1.Locations));
            this.Sut.Add(Item2, FieldId1, new Token("broken", this.Locations2.Locations));
            this.Sut.Add(Item3, FieldId1, new Token("brokerage", this.Locations3.Locations));
            var result = this.Sut.Apply();

            VerifyResult(result, "broke", expectedChildNodes: ['n', 'r']);
            VerifyResult(result, ['r'], "", [(Item1, this.Locations1)], ['a']);
            VerifyResult(result, ['n'], "", [(Item2, this.Locations2)]);
            VerifyResult(result, ['r', 'a'], "ge", [(Item3, this.Locations3)]);
        }

        [Theory]
        [InlineData("pest", 't', 'p', null, "est", "est")]
        [InlineData("taste", 'e', 'a', "t", "st", "ste")]
        [InlineData("tesa", 't', 'a', "tes", null, null)]
        public void IndexingAtNodeWithIntraNodeTextWithDifferentText_ShouldResultInSplitNodes(
            string indexText,
            char originalSplitChar,
            char newSplitChar,
            string? remainingIntraText,
            string? splitIntraText,
            string? newIntraText)
        {
            this.Sut.Add(Item1, FieldId1, new Token("test", this.Locations1.Locations));
            this.Sut.Add(Item2, FieldId1, new Token(indexText, this.Locations2.Locations));
            var result = this.Sut.Apply();

            var expectedChildNodes = new[] { originalSplitChar, newSplitChar };

            // The order of child nodes *must* be ascending, so we'll order the array
            Array.Sort(expectedChildNodes);

            VerifyResult(result, remainingIntraText, expectedChildNodes: expectedChildNodes);
            VerifyResult(result, [originalSplitChar], splitIntraText, [(Item1, this.Locations1)]);
            VerifyResult(result, [newSplitChar], newIntraText, [(Item2, this.Locations2)]);
        }

        [Fact]
        public void IndexingAtNodeCausingSplitAtMiddleOfIntraNodeText_ShouldPlaceMatchAtSplit()
        {
            this.Sut.Add(Item1, FieldId1, new Token("NOITAZI", this.Locations1.Locations));
            this.Sut.Add(Item2, FieldId1, new Token("NOITA", this.Locations2.Locations));
            var result = this.Sut.Apply();

            VerifyResult(result, "NOITA", [(Item2, this.Locations2)], expectedChildNodes: ['Z']);
            VerifyResult(result, ['Z'], "I", [(Item1, this.Locations1)]);
        }

        [Fact]
        public void IndexingAtNodeCausingSplitAtStartOfIntraNodeText_ShouldReturnInEntryAddedAtSplitNode()
        {
            this.Sut.Add(Item1, FieldId1, new Token("www", this.Locations1.Locations));
            this.Sut.Add(Item2, FieldId1, new Token("w3c", this.Locations2.Locations));
            this.Sut.Add(Item3, FieldId1, new Token("w3", this.Locations3.Locations));
            var result = this.Sut.Apply();

            VerifyResult(result, "w", expectedChildNodes: ['3', 'w']);
            VerifyResult(result, ['w'], "w", [(Item1, this.Locations1)]);
            VerifyResult(result, ['3'], null, [(Item3, this.Locations3)], ['c']);
            VerifyResult(result, ['3', 'c'], null, [(Item2, this.Locations2)]);
        }
    }
}
