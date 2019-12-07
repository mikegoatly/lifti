using System;
using System.Collections.Immutable;

namespace Lifti
{
    public interface IIndexNodeFactory : IConfiguredBy<IndexOptions>
    {
        IndexNode CreateRootNode();
        IndexNode CreateNode(
            ReadOnlyMemory<char> intraNodeText,
            ImmutableDictionary<char, IndexNode> childNodes,
            ImmutableDictionary<int, ImmutableList<IndexedWord>> matches);
        IndexSupportLevelKind GetIndexSupportLevelForDepth(int depth);
    }
}