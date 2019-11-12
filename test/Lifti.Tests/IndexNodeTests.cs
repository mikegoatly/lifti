using FluentAssertions;
using Lifti.Tokenization;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Lifti.Tests
{
    public class IndexNodeTests
    {
        private readonly Mock<IIndexNodeFactory> indexNodeFactoryMock;
        private readonly IndexNode sut;
        private readonly List<IndexNode> createdChildNodes = new List<IndexNode>();
        private readonly IndexedWord locations1 = CreateLocations(0, (0, 1, 2), (1, 5, 8));
        private readonly IndexedWord locations2 = CreateLocations(0, (2, 9, 2));
        private readonly IndexedWord locations3 = CreateLocations(0, (3, 14, 5));
        private readonly IndexedWord locations4 = CreateLocations(0, (4, 4, 5));
        private const int item1 = 1;
        private const int item2 = 2;
        private const int item3 = 3;
        private const int item4 = 4;
        private const byte fieldId1 = 0;

        public IndexNodeTests()
        {
            this.indexNodeFactoryMock = new Mock<IIndexNodeFactory>();
            this.sut = new IndexNode(this.indexNodeFactoryMock.Object, 0, IndexSupportLevelKind.IntraNodeText);
            this.indexNodeFactoryMock.Setup(
                x => x.CreateNode(It.IsAny<IndexNode>()))
                    .Returns((IndexNode parent) =>
                    {
                        var node = new IndexNode(this.indexNodeFactoryMock.Object, parent.Depth + 1, IndexSupportLevelKind.IntraNodeText);
                        this.createdChildNodes.Add(node);
                        return node;
                    });
        }

        [Fact]
        public void IndexingEmptyNode_ShouldResultInItemsDirectlyIndexedAtNode()
        {
            this.sut.Index(item1, fieldId1, new Token("test", this.locations1.Locations));

            VerifyResult(this.sut, "test", new[] { (item1, this.locations1) });
        }

        [Theory]
        [InlineData("test")]
        [InlineData("a")]
        public void IndexingAtNodeWithSameTextForDifferentItem_ShouldResultInItemsDirectlyIndexedAtNode(string word)
        {
            this.sut.Index(item1, fieldId1, new Token(word, this.locations1.Locations));
            this.sut.Index(item2, fieldId1, new Token(word, this.locations2.Locations));

            VerifyResult(this.sut, word, new[] { (item1, this.locations1), (item2, this.locations2) });
        }

        [Fact]
        public void IndexingWordEndingAtSplit_ShouldResultInItemIndexedWhereSplitOccurs()
        {
            this.sut.Index(item1, fieldId1, new Token("apple", this.locations1.Locations));
            this.sut.Index(item2, fieldId1, new Token("able", this.locations2.Locations));
            this.sut.Index(item3, fieldId1, new Token("banana", this.locations3.Locations));
            this.sut.Index(item4, fieldId1, new Token("a", this.locations4.Locations));

            VerifyResult(this.sut, null, expectedChildNodes: new[] { 'a', 'b' });
            VerifyResult(this.sut, new[] { 'a' }, null, new[] { (item4, this.locations4) }, new[] { 'p', 'b' });
            VerifyResult(this.sut, new[] { 'b' }, "anana", new[] { (item3, this.locations3) });
            VerifyResult(this.sut, new[] { 'a', 'b' }, "le", new[] { (item2, this.locations2) });
            VerifyResult(this.sut, new[] { 'a', 'p' }, "ple", new[] { (item1, this.locations1) });
        }

        [Fact]
        public void IndexingWhenChildNodeAlreadyExists_ShouldContinueIndexingAtExistingChild()
        {
            this.sut.Index(item1, fieldId1, new Token("freedom", this.locations1.Locations));
            this.sut.Index(item2, fieldId1, new Token("fred", this.locations2.Locations));
            this.sut.Index(item3, fieldId1, new Token("freddy", this.locations3.Locations));

            VerifyResult(this.sut, "fre", expectedChildNodes: new[] { 'e', 'd' });
            VerifyResult(this.sut, new[] { 'e' }, "dom", new[] { (item1, this.locations1) });
            VerifyResult(this.sut, new[] { 'd' }, null, new[] { (item2, this.locations2) }, new[] { 'd' });
            VerifyResult(this.sut, new[] { 'd', 'd' }, "y", new[] { (item3, this.locations3) });
        }

        [Fact]
        public void IndexingAtNodeWithTextWithSameSuffix_ShouldCreateNewChildNode()
        {
            this.sut.Index(item1, fieldId1, new Token("test", this.locations1.Locations));
            this.sut.Index(item2, fieldId1, new Token("testing", this.locations2.Locations));
            this.sut.Index(item3, fieldId1, new Token("tester", this.locations3.Locations));

            VerifyResult(this.sut, "test", new[] { (item1, this.locations1) }, new[] { 'i', 'e' });
            VerifyResult(this.sut, new[] { 'i' }, "ng", new[] { (item2, this.locations2) });
            VerifyResult(this.sut, new[] { 'e' }, "r", new[] { (item3, this.locations3) });
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
            this.sut.Index(item1, fieldId1, new Token("test", this.locations1.Locations));
            this.sut.Index(item2, fieldId1, new Token(indexText, this.locations2.Locations));

            VerifyResult(this.sut, remainingIntraText, expectedChildNodes: new[] { originalSplitChar, newSplitChar });
            VerifyResult(this.sut, new[] { originalSplitChar }, splitIntraText, new[] { (item1, this.locations1) });
            VerifyResult(this.sut, new[] { newSplitChar }, newIntraText, new[] { (item2, this.locations2) });
        }

        [Fact]
        public void IndexingAtNodeCausingSplitAtMiddleOfIntraNodeText_ShouldPlaceMatchAtSplit()
        {
            this.sut.Index(item1, fieldId1, new Token("NOITAZI", this.locations1.Locations));
            this.sut.Index(item2, fieldId1, new Token("NOITA", this.locations2.Locations));

            this.createdChildNodes.Should().HaveCount(1);

            VerifyResult(this.sut, "NOITA", new[] { (item2, this.locations2) }, expectedChildNodes: new[] { 'Z' });
            VerifyResult(this.sut, new[] { 'Z' }, "I", new[] { (item1, this.locations1) });
        }

        [Fact]
        public void IndexingAtNodeCausingSplitAtStartOfIntraNodeText_ShouldReturnInEntryAddedAtSplitNode()
        {
            this.sut.Index(item1, fieldId1, new Token("www", this.locations1.Locations));
            this.sut.Index(item2, fieldId1, new Token("w3c", this.locations2.Locations));
            this.sut.Index(item3, fieldId1, new Token("w3", this.locations3.Locations));

            VerifyResult(this.sut, "w", expectedChildNodes: new[] { 'w', '3' });
            VerifyResult(this.sut, new[] { 'w' }, "w", new[] { (item1, this.locations1) });
            VerifyResult(this.sut, new[] { '3' }, null, new[] { (item3, this.locations3) }, new[] { 'c' });
            VerifyResult(this.sut, new[] { '3', 'c' }, null, new[] { (item2, this.locations2) });
        }

        [Fact]
        public void RemovingItemId_ShouldCauseItemToBeRemovedFromIndexAndChildNodes()
        {
            this.sut.Index(item1, fieldId1, new Token("www", this.locations1.Locations));
            this.sut.Index(item1, fieldId1, new Token("wwwww", this.locations2.Locations));

            this.createdChildNodes.Should().HaveCount(1);

            this.sut.Remove(item1);

            VerifyResult(this.sut, "www", expectedChildNodes: new[] { 'w' }, expectedMatches: Array.Empty<(int, IndexedWord)>());
            VerifyResult(this.sut, new[] { 'w' }, "w", expectedMatches: Array.Empty<(int, IndexedWord)>());
        }

        private static IndexedWord CreateLocations(byte fieldId, params (int, int, ushort)[] locations)
        {
            return new IndexedWord(fieldId, locations.Select(r => new WordLocation(r.Item1, r.Item2, r.Item3)).ToArray());
        }

        //private static void VerifySutState(
        //    IndexNode node,
        //    string intraNodeText,
        //    (int, IndexedWord)[] expectedMatches = null,
        //    (char, IndexNode)[] expectedChildNodes = null)
        //{
        //    node.IntraNodeText.ToArray().Should().BeEquivalentTo(intraNodeText?.ToCharArray() ?? Array.Empty<char>());
        //    node.ChildNodes.Should().BeEquivalentTo(expectedChildNodes?.ToDictionary(x => x.Item1, x => x.Item2), o => o.Excluding(t => t.Value.ChildNodes).Excluding(t => t.Value.Matches));
        //    node.Matches.Should().BeEquivalentTo(expectedMatches?.ToDictionary(x => x.Item1, x => new[] { x.Item2 }));
        //}

        private static void VerifyResult(
            IndexNode node,
            string intraNodeText,
            (int, IndexedWord)[] expectedMatches = null,
            char[] expectedChildNodes = null)
        {
            node.IntraNodeText.ToArray().Should().BeEquivalentTo(intraNodeText?.ToCharArray() ?? Array.Empty<char>());
            node.ChildNodes?.Keys.Should().BeEquivalentTo(expectedChildNodes, o => o.WithoutStrictOrdering());
            node.Matches?.Should().BeEquivalentTo(expectedMatches?.ToDictionary(x => x.Item1, x => new[] { x.Item2 }));
        }

        private static void VerifyResult(
            IndexNode node,
            char[] navigationChars,
            string intraNodeText,
            (int, IndexedWord)[] expectedMatches = null,
            char[] expectedChildNodes = null)
        {
            foreach (var navigationChar in navigationChars)
            {
                node = node.ChildNodes[navigationChar];
            }

            VerifyResult(node, intraNodeText, expectedMatches, expectedChildNodes);
        }
    }
}
