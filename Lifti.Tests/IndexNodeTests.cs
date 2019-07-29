using FluentAssertions;
using Moq;
using Lifti;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Lifti.Tests
{
    public class IndexNodeTests
    {
        private Mock<IIndexNodeFactory> indexNodeFactoryMock;
        private IndexNode sut;
        private List<IndexNode> createdChildNodes = new List<IndexNode>();

        public IndexNodeTests()
        {
            this.indexNodeFactoryMock = new Mock<IIndexNodeFactory>();
            this.sut = new IndexNode(this.indexNodeFactoryMock.Object);
            this.indexNodeFactoryMock.Setup(
                x => x.CreateChildNodeFor(It.IsAny<IndexNode>()))
                    .Returns((IndexNode i) =>
                    {
                        var node = new IndexNode(this.indexNodeFactoryMock.Object, i);
                        this.createdChildNodes.Add(node);
                        return node;
                    });
        }

        [Fact]
        public void IndexingEmptyNode_ShouldResultInItemsDirectlyIndexedAtNode()
        {
            var locations = CreateLocations((1, 2), (5, 8));
            this.sut.Index(4, new SplitWord("test".AsSpan(), locations));

            VerifySutState(this.sut, null, "test", new[] { (4, locations) });
        }

        [Fact]
        public void IndexingAtNodeWithSameTextForDifferentItem_ShouldResultInItemsDirectlyIndexedAtNode()
        {
            var locations1 = CreateLocations((1, 2), (5, 8));
            this.sut.Index(1, new SplitWord("test".AsSpan(), locations1));
            var locations2 = CreateLocations((9, 2));
            this.sut.Index(2, new SplitWord("test".AsSpan(), locations2));

            VerifySutState(this.sut, null, "test", new[] { (1, locations1), (2, locations2) });
        }

        [Fact]
        public void IndexingWhenChildNodeAlreadyExists_ShouldContinueIndexingAtExistingChild()
        {
            var locations1 = CreateLocations((1, 2), (5, 8));
            this.sut.Index(1, new SplitWord("freedom".AsSpan(), locations1));
            var locations2 = CreateLocations((9, 2));
            this.sut.Index(2, new SplitWord("fred".AsSpan(), locations2));
            var locations3 = CreateLocations((14, 5));
            this.sut.Index(3, new SplitWord("freddy".AsSpan(), locations3));

            this.createdChildNodes.Should().HaveCount(3);
            VerifySutState(this.sut, null, "fre", expectedChildNodes: new[] { ('e', createdChildNodes[0]), ('d', createdChildNodes[1]) });
            VerifySutState(createdChildNodes[0], this.sut, "dom", new[] { (1, locations1) });
            VerifySutState(createdChildNodes[1], this.sut, null, new[] { (2, locations2) }, new[] { ('d', createdChildNodes[2]) });
            VerifySutState(createdChildNodes[2], createdChildNodes[1], "y", new[] { (3, locations3) });
        }

        [Fact]
        public void IndexingAtNodeWithTextWithSameSuffix_ShouldCreateNewChildNode()
        {
            var locations1 = CreateLocations((1, 2), (5, 8));
            this.sut.Index(1, new SplitWord("test".AsSpan(), locations1));
            var locations2 = CreateLocations((9, 2));
            this.sut.Index(2, new SplitWord("testing".AsSpan(), locations2));
            var locations3 = CreateLocations((10, 2));
            this.sut.Index(3, new SplitWord("tester".AsSpan(), locations3));

            this.createdChildNodes.Should().HaveCount(2);
            VerifySutState(this.sut, null, "test", new[] { (1, locations1) }, new[] { ('i', createdChildNodes[0]), ('e', createdChildNodes[1]) });
            VerifySutState(this.createdChildNodes[0], this.sut, "ng", new[] { (2, locations2) });
            VerifySutState(this.createdChildNodes[1], this.sut, "r", new[] { (3, locations3) });
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
            var locations1 = CreateLocations((1, 2), (5, 8));
            this.sut.Index(1, new SplitWord("test".AsSpan(), locations1));
            var locations2 = CreateLocations((9, 2));
            this.sut.Index(2, new SplitWord(indexText.AsSpan(), locations2));

            createdChildNodes.Should().HaveCount(2);
            VerifySutState(this.sut, null, remainingIntraText, expectedChildNodes: new[] { (originalSplitChar, createdChildNodes[0]), (newSplitChar, createdChildNodes[1]) });
            VerifySutState(createdChildNodes[0], this.sut, splitIntraText, new[] { (1, locations1) });
            VerifySutState(createdChildNodes[1], this.sut, newIntraText, new[] { (2, locations2) });
        }

        private static Lifti.Range[] CreateLocations(params (int, int)[] locations)
        {
            return locations.Select(r => new Lifti.Range(r.Item1, r.Item2)).ToArray();
        }

        private static void VerifySutState(
            IndexNode node,
            IndexNode expectedParentNode,
            string intraNodeText,
            (int, Lifti.Range[])[] expectedMatches = null,
            (char, IndexNode)[] expectedChildNodes = null)
        {
            node.ParentNode.Should().BeEquivalentTo(expectedParentNode);
            node.IntraNodeText.Should().BeEquivalentTo(intraNodeText?.ToCharArray());
            node.ChildNodes.Should().BeEquivalentTo(expectedChildNodes?.ToDictionary(x => x.Item1, x => x.Item2));
            node.Matches.Should().BeEquivalentTo(expectedMatches?.ToDictionary(x => x.Item1, x => x.Item2));
        }
    }
}
