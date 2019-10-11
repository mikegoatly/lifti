using FluentAssertions;
using Lifti.Tokenization;
using Moq;
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

            VerifySutState(this.sut, "test", new[] { (item1, this.locations1) });
        }

        [Theory]
        [InlineData("test")]
        [InlineData("a")]
        public void IndexingAtNodeWithSameTextForDifferentItem_ShouldResultInItemsDirectlyIndexedAtNode(string word)
        {
            this.sut.Index(item1, fieldId1, new Token(word, this.locations1.Locations));
            this.sut.Index(item2, fieldId1, new Token(word, this.locations2.Locations));

            VerifySutState(this.sut, word, new[] { (item1, this.locations1), (item2, this.locations2) });
        }

        [Fact]
        public void IndexingWordEndingAtSplit_ShouldResultInItemIndexedWhereSplitOccurs()
        {
            this.sut.Index(item1, fieldId1, new Token("apple", this.locations1.Locations));
            this.sut.Index(item2, fieldId1, new Token("able", this.locations2.Locations));
            this.sut.Index(item3, fieldId1, new Token("banana", this.locations3.Locations));
            this.sut.Index(item4, fieldId1, new Token("a", this.locations4.Locations));

            this.createdChildNodes.Should().HaveCount(4);
            VerifySutState(this.sut, null, expectedChildNodes: new[] { ('a', this.createdChildNodes[2]), ('b', this.createdChildNodes[3]) });
            VerifySutState(this.createdChildNodes[2], null, new[] { (item4, this.locations4) }, new[] { ('p', this.createdChildNodes[0]), ('b', this.createdChildNodes[1]) });
            VerifySutState(this.createdChildNodes[3], "anana", new[] { (item3, this.locations3) });
            VerifySutState(this.createdChildNodes[1], "le", new[] { (item2, this.locations2) });
            VerifySutState(this.createdChildNodes[0], "ple", new[] { (item1, this.locations1) });
        }

        [Fact]
        public void IndexingWhenChildNodeAlreadyExists_ShouldContinueIndexingAtExistingChild()
        {
            this.sut.Index(item1, fieldId1, new Token("freedom", this.locations1.Locations));
            this.sut.Index(item2, fieldId1, new Token("fred", this.locations2.Locations));
            this.sut.Index(item3, fieldId1, new Token("freddy", this.locations3.Locations));

            this.createdChildNodes.Should().HaveCount(3);
            VerifySutState(this.sut, "fre", expectedChildNodes: new[] { ('e', this.createdChildNodes[0]), ('d', this.createdChildNodes[1]) });
            VerifySutState(this.createdChildNodes[0], "dom", new[] { (item1, this.locations1) });
            VerifySutState(this.createdChildNodes[1], null, new[] { (item2, this.locations2) }, new[] { ('d', this.createdChildNodes[2]) });
            VerifySutState(this.createdChildNodes[2], "y", new[] { (item3, this.locations3) });
        }

        [Fact]
        public void IndexingAtNodeWithTextWithSameSuffix_ShouldCreateNewChildNode()
        {
            this.sut.Index(item1, fieldId1, new Token("test", this.locations1.Locations));
            this.sut.Index(item2, fieldId1, new Token("testing", this.locations2.Locations));
            this.sut.Index(item3, fieldId1, new Token("tester", this.locations3.Locations));

            this.createdChildNodes.Should().HaveCount(2);
            VerifySutState(this.sut, "test", new[] { (item1, this.locations1) }, new[] { ('i', this.createdChildNodes[0]), ('e', this.createdChildNodes[1]) });
            VerifySutState(this.createdChildNodes[0], "ng", new[] { (item2, this.locations2) });
            VerifySutState(this.createdChildNodes[1], "r", new[] { (item3, this.locations3) });
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

            this.createdChildNodes.Should().HaveCount(2);
            VerifySutState(this.sut, remainingIntraText, expectedChildNodes: new[] { (originalSplitChar, this.createdChildNodes[0]), (newSplitChar, this.createdChildNodes[1]) });
            VerifySutState(this.createdChildNodes[0], splitIntraText, new[] { (item1, this.locations1) });
            VerifySutState(this.createdChildNodes[1], newIntraText, new[] { (item2, this.locations2) });
        }

        [Fact]
        public void IndexingAtNodeCausingSplitAtMiddleOfIntraNodeText_ShouldPlaceMatchAtSplit()
        {
            this.sut.Index(item1, fieldId1, new Token("NOITAZI", this.locations1.Locations));
            this.sut.Index(item2, fieldId1, new Token("NOITA", this.locations2.Locations));

            this.createdChildNodes.Should().HaveCount(1);

            VerifySutState(this.sut, "NOITA", new[] { (item2, this.locations2) }, expectedChildNodes: new[] { ('Z', this.createdChildNodes[0]) });
            VerifySutState(this.createdChildNodes[0], "I", new[] { (item1, this.locations1) });
        }

        [Fact]
        public void IndexingAtNodeCausingSplitAtStartOfIntraNodeText_ShouldReturnInEntryAddedAtSplitNode()
        {
            this.sut.Index(item1, fieldId1, new Token("www", this.locations1.Locations));
            this.sut.Index(item2, fieldId1, new Token("w3c", this.locations2.Locations));
            this.sut.Index(item3, fieldId1, new Token("w3", this.locations3.Locations));

            this.createdChildNodes.Should().HaveCount(3);
            VerifySutState(this.sut, "w", expectedChildNodes: new[] { ('w', this.createdChildNodes[0]), ('3', this.createdChildNodes[1]) });
            VerifySutState(this.createdChildNodes[0], "w", new[] { (item1, this.locations1) });
            VerifySutState(this.createdChildNodes[1], null, new[] { (item3, this.locations3) }, new[] { ('c', this.createdChildNodes[2]) });
            VerifySutState(this.createdChildNodes[2], null, new[] { (item2, this.locations2) });
        }

        private static IndexedWord CreateLocations(byte fieldId, params (int, int, int)[] locations)
        {
            return new IndexedWord(fieldId, locations.Select(r => new WordLocation(r.Item1, r.Item2, r.Item3)).ToArray());
        }

        private static void VerifySutState(
            IndexNode node,
            string intraNodeText,
            (int, IndexedWord)[] expectedMatches = null,
            (char, IndexNode)[] expectedChildNodes = null)
        {
            node.IntraNodeText.Should().BeEquivalentTo(intraNodeText?.ToCharArray());
            node.ChildNodes.Should().BeEquivalentTo(expectedChildNodes?.ToDictionary(x => x.Item1, x => x.Item2), o => o.Excluding(t => t.Value.ChildNodes).Excluding(t => t.Value.Matches));

            node.Matches.Should().BeEquivalentTo(expectedMatches?.ToDictionary(x => x.Item1, x => new[] { x.Item2 }));
        }
    }
}
