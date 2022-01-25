using FluentAssertions;
using Lifti.Tokenization;
using System;
using System.Collections.Immutable;
using System.Linq;
using Xunit;

namespace Lifti.Tests
{
    public class IndexInsertionMutationTests
    {
        private readonly IndexNodeFactory nodeFactory;
        private readonly IndexNode rootNode;
        private readonly IndexMutation sut;
        private readonly IndexedToken locations1 = CreateLocations(0, (0, 1, 2), (1, 5, 8));
        private readonly IndexedToken locations2 = CreateLocations(0, (2, 9, 2));
        private readonly IndexedToken locations3 = CreateLocations(0, (3, 14, 5));
        private readonly IndexedToken locations4 = CreateLocations(0, (4, 4, 5));
        private const int item1 = 1;
        private const int item2 = 2;
        private const int item3 = 3;
        private const int item4 = 4;
        private const byte fieldId1 = 0;

        public IndexInsertionMutationTests()
        {
            this.nodeFactory = new IndexNodeFactory(new IndexOptions { SupportIntraNodeTextAfterIndexDepth = 0 } );
            this.rootNode = this.nodeFactory.CreateRootNode();
            this.sut = new IndexMutation(this.rootNode, this.nodeFactory);
        }

        [Fact]
        public void IndexingEmptyNode_ShouldResultInItemsDirectlyIndexedAtNode()
        {
            this.sut.Add(item1, fieldId1, new Token("test", this.locations1.Locations));
            var result = this.sut.Apply();

            VerifyResult(result, "test", new[] { (item1, this.locations1) });
        }

        [Theory]
        [InlineData("test")]
        [InlineData("a")]
        public void IndexingAtNodeWithSameTextForDifferentItem_ShouldResultInItemsDirectlyIndexedAtNode(string word)
        {
            this.sut.Add(item1, fieldId1, new Token(word, this.locations1.Locations));
            this.sut.Add(item2, fieldId1, new Token(word, this.locations2.Locations));
            var result = this.sut.Apply();

            VerifyResult(result, word, new[] { (item1, this.locations1), (item2, this.locations2) });
        }

        [Fact]
        public void IndexingWordEndingAtSplit_ShouldResultInItemIndexedWhereSplitOccurs()
        {
            this.sut.Add(item1, fieldId1, new Token("apple", this.locations1.Locations));
            this.sut.Add(item2, fieldId1, new Token("able", this.locations2.Locations));
            this.sut.Add(item3, fieldId1, new Token("banana", this.locations3.Locations));
            this.sut.Add(item4, fieldId1, new Token("a", this.locations4.Locations));
            var result = this.sut.Apply();

            VerifyResult(result, null, expectedChildNodes: new[] { 'a', 'b' });
            VerifyResult(result, new[] { 'a' }, null, new[] { (item4, this.locations4) }, new[] { 'p', 'b' });
            VerifyResult(result, new[] { 'b' }, "anana", new[] { (item3, this.locations3) });
            VerifyResult(result, new[] { 'a', 'b' }, "le", new[] { (item2, this.locations2) });
            VerifyResult(result, new[] { 'a', 'p' }, "ple", new[] { (item1, this.locations1) });
        }

        [Fact]
        public void IndexingWhenChildNodeAlreadyExists_ShouldContinueIndexingAtExistingChild()
        {
            this.sut.Add(item1, fieldId1, new Token("freedom", this.locations1.Locations));
            this.sut.Add(item2, fieldId1, new Token("fred", this.locations2.Locations));
            this.sut.Add(item3, fieldId1, new Token("freddy", this.locations3.Locations));
            var result = this.sut.Apply();

            VerifyResult(result, "fre", expectedChildNodes: new[] { 'e', 'd' });
            VerifyResult(result, new[] { 'e' }, "dom", new[] { (item1, this.locations1) });
            VerifyResult(result, new[] { 'd' }, null, new[] { (item2, this.locations2) }, new[] { 'd' });
            VerifyResult(result, new[] { 'd', 'd' }, "y", new[] { (item3, this.locations3) });
        }

        [Fact]
        public void IndexingAtNodeWithTextWithSameSuffix_ShouldCreateNewChildNode()
        {
            this.sut.Add(item1, fieldId1, new Token("test", this.locations1.Locations));
            this.sut.Add(item2, fieldId1, new Token("testing", this.locations2.Locations));
            this.sut.Add(item3, fieldId1, new Token("tester", this.locations3.Locations));
            var result = this.sut.Apply();

            VerifyResult(result, "test", new[] { (item1, this.locations1) }, new[] { 'i', 'e' });
            VerifyResult(result, new[] { 'i' }, "ng", new[] { (item2, this.locations2) });
            VerifyResult(result, new[] { 'e' }, "r", new[] { (item3, this.locations3) });
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
            this.sut.Add(item1, fieldId1, new Token("test", this.locations1.Locations));
            this.sut.Add(item2, fieldId1, new Token(indexText, this.locations2.Locations));
            var result = this.sut.Apply();

            VerifyResult(result, remainingIntraText, expectedChildNodes: new[] { originalSplitChar, newSplitChar });
            VerifyResult(result, new[] { originalSplitChar }, splitIntraText, new[] { (item1, this.locations1) });
            VerifyResult(result, new[] { newSplitChar }, newIntraText, new[] { (item2, this.locations2) });
        }

        [Fact]
        public void IndexingAtNodeCausingSplitAtMiddleOfIntraNodeText_ShouldPlaceMatchAtSplit()
        {
            this.sut.Add(item1, fieldId1, new Token("NOITAZI", this.locations1.Locations));
            this.sut.Add(item2, fieldId1, new Token("NOITA", this.locations2.Locations));
            var result = this.sut.Apply();

            VerifyResult(result, "NOITA", new[] { (item2, this.locations2) }, expectedChildNodes: new[] { 'Z' });
            VerifyResult(result, new[] { 'Z' }, "I", new[] { (item1, this.locations1) });
        }

        [Fact]
        public void IndexingAtNodeCausingSplitAtStartOfIntraNodeText_ShouldReturnInEntryAddedAtSplitNode()
        {
            this.sut.Add(item1, fieldId1, new Token("www", this.locations1.Locations));
            this.sut.Add(item2, fieldId1, new Token("w3c", this.locations2.Locations));
            this.sut.Add(item3, fieldId1, new Token("w3", this.locations3.Locations));
            var result = this.sut.Apply();

            VerifyResult(result, "w", expectedChildNodes: new[] { 'w', '3' });
            VerifyResult(result, new[] { 'w' }, "w", new[] { (item1, this.locations1) });
            VerifyResult(result, new[] { '3' }, null, new[] { (item3, this.locations3) }, new[] { 'c' });
            VerifyResult(result, new[] { '3', 'c' }, null, new[] { (item2, this.locations2) });
        }

        // TODO Move to new test suite
        //[Fact]
        //public void RemovingItemId_ShouldCauseItemToBeRemovedFromIndexAndChildNodes()
        //{
        //    this.sut.Index(item1, fieldId1, new Token("www", this.locations1.Locations));
        //    this.sut.Index(item1, fieldId1, new Token("wwwww", this.locations2.Locations));
        //    this.sut.Remove(item1);

        //    var result = this.sut.ApplyMutations();

        //    VerifyResult(result, "www", expectedChildNodes: new[] { 'w' }, expectedMatches: Array.Empty<(int, IndexedWord)>());
        //    VerifyResult(result, new[] { 'w' }, "w", expectedMatches: Array.Empty<(int, IndexedWord)>());
        //}

        private static IndexedToken CreateLocations(byte fieldId, params (int, int, ushort)[] locations)
        {
            return new IndexedToken(fieldId, locations.Select(r => new TokenLocation(r.Item1, r.Item2, r.Item3)).ToArray());
        }

        private static void VerifyResult(
            IndexNode node,
            string? intraNodeText,
            (int, IndexedToken)[]? expectedMatches = null,
            char[]? expectedChildNodes = null)
        {
            expectedChildNodes = expectedChildNodes ?? Array.Empty<char>();
            expectedMatches = expectedMatches ?? Array.Empty<(int, IndexedToken)>();

            node.IntraNodeText.ToArray().Should().BeEquivalentTo(intraNodeText?.ToCharArray() ?? Array.Empty<char>());
            node.ChildNodes.Keys.Should().BeEquivalentTo(expectedChildNodes, o => o.WithoutStrictOrdering());
            node.Matches.Should().BeEquivalentTo(expectedMatches.ToImmutableDictionary(x => x.Item1, x => new[] { x.Item2 }));
        }

        private static void VerifyResult(
            IndexNode node,
            char[] navigationChars,
            string? intraNodeText,
            (int, IndexedToken)[]? expectedMatches = null,
            char[]? expectedChildNodes = null)
        {
            foreach (var navigationChar in navigationChars)
            {
                node = node.ChildNodes[navigationChar];
            }

            VerifyResult(node, intraNodeText, expectedMatches, expectedChildNodes);
        }
    }
}
