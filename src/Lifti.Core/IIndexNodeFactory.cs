using System;
using System.Collections.Immutable;

namespace Lifti
{
    /// <summary>
    /// Implemented by classes that can create new <see cref="IndexNode"/> instances for use in
    /// an index.
    /// </summary>
    public interface IIndexNodeFactory
    {
        /// <summary>
        /// Constructs the root node of the index.
        /// </summary>
        IndexNode CreateRootNode();

        /// <summary>
        /// Constructs a new node for an index.
        /// </summary>
        /// <param name="intraNodeText">
        /// The continuous sequence of characters that are matched at this node. When traversing the index,
        /// the matches provided to <paramref name="matches"/> will not be considered an exact match until this 
        /// text has been completely processed.
        /// </param>
        /// <param name="childNodes">
        /// The set of child nodes at this instance, keyed by matching character.
        /// </param>
        /// <param name="matches">
        /// The tokens that are matched at this instance, keyed by the internal item id.
        /// </param>
        IndexNode CreateNode(
            ReadOnlyMemory<char> intraNodeText,
            ImmutableDictionary<char, IndexNode> childNodes,
            ImmutableDictionary<int, ImmutableList<IndexedToken>> matches);

        /// <summary>
        /// Gets the <see cref="IndexSupportLevelKind"/> for the given <paramref name="depth"/> into the index.
        /// </summary>
        IndexSupportLevelKind GetIndexSupportLevelForDepth(int depth);
    }
}