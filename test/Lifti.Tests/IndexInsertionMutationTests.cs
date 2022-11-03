using Lifti.Tokenization;
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

            VerifyResult(result, "test", new[] { (Item1, this.Locations1) });
        }

        [Theory]
        [InlineData("test")]
        [InlineData("a")]
        public void IndexingAtNodeWithSameTextForDifferentItem_ShouldResultInItemsDirectlyIndexedAtNode(string word)
        {
            this.Sut.Add(Item1, FieldId1, new Token(word, this.Locations1.Locations));
            this.Sut.Add(Item2, FieldId1, new Token(word, this.Locations2.Locations));
            var result = this.Sut.Apply();

            VerifyResult(result, word, new[] { (Item1, this.Locations1), (Item2, this.Locations2) });
        }

        [Fact]
        public void IndexingWordEndingAtSplit_ShouldResultInItemIndexedWhereSplitOccurs()
        {
            this.Sut.Add(Item1, FieldId1, new Token("apple", this.Locations1.Locations));
            this.Sut.Add(Item2, FieldId1, new Token("able", this.Locations2.Locations));
            this.Sut.Add(Item3, FieldId1, new Token("banana", this.Locations3.Locations));
            this.Sut.Add(Item4, FieldId1, new Token("a", this.Locations4.Locations));
            var result = this.Sut.Apply();

            VerifyResult(result, null, expectedChildNodes: new[] { 'a', 'b' });
            VerifyResult(result, new[] { 'a' }, null, new[] { (Item4, this.Locations4) }, new[] { 'p', 'b' });
            VerifyResult(result, new[] { 'b' }, "anana", new[] { (Item3, this.Locations3) });
            VerifyResult(result, new[] { 'a', 'b' }, "le", new[] { (Item2, this.Locations2) });
            VerifyResult(result, new[] { 'a', 'p' }, "ple", new[] { (Item1, this.Locations1) });
        }

        [Fact]
        public void IndexingWhenChildNodeAlreadyExists_ShouldContinueIndexingAtExistingChild()
        {
            this.Sut.Add(Item1, FieldId1, new Token("freedom", this.Locations1.Locations));
            this.Sut.Add(Item2, FieldId1, new Token("fred", this.Locations2.Locations));
            this.Sut.Add(Item3, FieldId1, new Token("freddy", this.Locations3.Locations));
            var result = this.Sut.Apply();

            VerifyResult(result, "fre", expectedChildNodes: new[] { 'e', 'd' });
            VerifyResult(result, new[] { 'e' }, "dom", new[] { (Item1, this.Locations1) });
            VerifyResult(result, new[] { 'd' }, null, new[] { (Item2, this.Locations2) }, new[] { 'd' });
            VerifyResult(result, new[] { 'd', 'd' }, "y", new[] { (Item3, this.Locations3) });
        }

        [Fact]
        public void IndexingAtNodeWithTextWithSameSuffix_ShouldCreateNewChildNode()
        {
            this.Sut.Add(Item1, FieldId1, new Token("test", this.Locations1.Locations));
            this.Sut.Add(Item2, FieldId1, new Token("testing", this.Locations2.Locations));
            this.Sut.Add(Item3, FieldId1, new Token("tester", this.Locations3.Locations));
            var result = this.Sut.Apply();

            VerifyResult(result, "test", new[] { (Item1, this.Locations1) }, new[] { 'i', 'e' });
            VerifyResult(result, new[] { 'i' }, "ng", new[] { (Item2, this.Locations2) });
            VerifyResult(result, new[] { 'e' }, "r", new[] { (Item3, this.Locations3) });
        }

        [Fact]
        public void IndexingAtNodeAlreadySplit_ShouldMaintainMatchesAtFirstSplitNode()
        {
            this.Sut.Add(Item1, FieldId1, new Token("broker", this.Locations1.Locations));
            this.Sut.Add(Item2, FieldId1, new Token("broken", this.Locations2.Locations));
            this.Sut.Add(Item3, FieldId1, new Token("brokerage", this.Locations3.Locations));
            var result = this.Sut.Apply();

            VerifyResult(result, "broke", expectedChildNodes: new[] { 'r', 'n' });
            VerifyResult(result, new[] { 'r' }, "", new[] { (Item1, this.Locations1) }, new[] { 'a' });
            VerifyResult(result, new[] { 'n' }, "", new[] { (Item2, this.Locations2) });
            VerifyResult(result, new[] { 'r', 'a' }, "ge", new[] { (Item3, this.Locations3) });
        }

        [Theory]
        [InlineData("pest", 't', 'p', null, "est", "est")]
        [InlineData("taste", 'e', 'a', "t", "st", "ste")]
        [InlineData("tesa", 't', 'a', "tes", null, null)]
        public void IndexingAtNodeWithIntraNodeTextWithDifferentText_ShouldResultInSplitNodes(
            string indexText,
            char originalSplitChar,
            char newSplitChar,
            string remainingIntraText,
            string splitIntraText,
            string newIntraText)
        {
            this.Sut.Add(Item1, FieldId1, new Token("test", this.Locations1.Locations));
            this.Sut.Add(Item2, FieldId1, new Token(indexText, this.Locations2.Locations));
            var result = this.Sut.Apply();

            VerifyResult(result, remainingIntraText, expectedChildNodes: new[] { originalSplitChar, newSplitChar });
            VerifyResult(result, new[] { originalSplitChar }, splitIntraText, new[] { (Item1, this.Locations1) });
            VerifyResult(result, new[] { newSplitChar }, newIntraText, new[] { (Item2, this.Locations2) });
        }

        [Fact]
        public void IndexingAtNodeCausingSplitAtMiddleOfIntraNodeText_ShouldPlaceMatchAtSplit()
        {
            this.Sut.Add(Item1, FieldId1, new Token("NOITAZI", this.Locations1.Locations));
            this.Sut.Add(Item2, FieldId1, new Token("NOITA", this.Locations2.Locations));
            var result = this.Sut.Apply();

            VerifyResult(result, "NOITA", new[] { (Item2, this.Locations2) }, expectedChildNodes: new[] { 'Z' });
            VerifyResult(result, new[] { 'Z' }, "I", new[] { (Item1, this.Locations1) });
        }

        [Fact]
        public void IndexingAtNodeCausingSplitAtStartOfIntraNodeText_ShouldReturnInEntryAddedAtSplitNode()
        {
            this.Sut.Add(Item1, FieldId1, new Token("www", this.Locations1.Locations));
            this.Sut.Add(Item2, FieldId1, new Token("w3c", this.Locations2.Locations));
            this.Sut.Add(Item3, FieldId1, new Token("w3", this.Locations3.Locations));
            var result = this.Sut.Apply();

            VerifyResult(result, "w", expectedChildNodes: new[] { 'w', '3' });
            VerifyResult(result, new[] { 'w' }, "w", new[] { (Item1, this.Locations1) });
            VerifyResult(result, new[] { '3' }, null, new[] { (Item3, this.Locations3) }, new[] { 'c' });
            VerifyResult(result, new[] { '3', 'c' }, null, new[] { (Item2, this.Locations2) });
        }        
    }
}
