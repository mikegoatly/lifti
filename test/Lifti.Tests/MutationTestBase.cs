using FluentAssertions;
using System;
using System.Collections.Immutable;
using System.Linq;

namespace Lifti.Tests
{
    public abstract class MutationTestBase
    {
        private readonly IndexNodeFactory indexNodeFactory;

        protected readonly IndexedToken Locations1 = CreateLocations(0, (0, 1, 2), (1, 5, 8));
        protected readonly IndexedToken Locations2 = CreateLocations(0, (2, 9, 2));
        protected readonly IndexedToken Locations3 = CreateLocations(0, (3, 14, 5));
        protected readonly IndexedToken Locations4 = CreateLocations(0, (4, 4, 5));
        protected const int Item1 = 1;
        protected const int Item2 = 2;
        protected const int Item3 = 3;
        protected const int Item4 = 4;
        protected const byte FieldId1 = 0;

        protected MutationTestBase()
        {
            this.indexNodeFactory = new IndexNodeFactory(new IndexOptions { SupportIntraNodeTextAfterIndexDepth = 0 });
            this.RootNode = this.indexNodeFactory.CreateRootNode();
            this.Sut = new IndexMutation(this.RootNode, this.indexNodeFactory);
        }

        protected IndexNode RootNode { get; }
        internal IndexMutation Sut { get; set; }

        protected void ApplyMutationsToNewSut()
        {
            this.Sut = new IndexMutation(this.Sut.Apply(), this.indexNodeFactory);
        }

        protected static void VerifyResult(
            IndexNode node,
            string? intraNodeText,
            (int, IndexedToken)[]? expectedMatches = null,
            char[]? expectedChildNodes = null)
        {
            expectedChildNodes ??= Array.Empty<char>();
            expectedMatches ??= Array.Empty<(int, IndexedToken)>();

            node.HasChildNodes.Should().Be(expectedChildNodes.Length > 0);
            node.HasMatches.Should().Be(expectedMatches.Length > 0);
            node.IntraNodeText.ToArray().Should().BeEquivalentTo(intraNodeText?.ToCharArray() ?? Array.Empty<char>());
            node.ChildNodes.Keys.Should().BeEquivalentTo(expectedChildNodes, o => o.WithoutStrictOrdering());
            node.Matches.Should().BeEquivalentTo(expectedMatches.ToImmutableDictionary(x => x.Item1, x => new[] { x.Item2 }));
        }

        protected static void VerifyResult(
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

        private static IndexedToken CreateLocations(byte fieldId, params (int, int, ushort)[] locations)
        {
            return new IndexedToken(fieldId, locations.Select(r => new TokenLocation(r.Item1, r.Item2, r.Item3)).ToArray());
        }
    }
}
