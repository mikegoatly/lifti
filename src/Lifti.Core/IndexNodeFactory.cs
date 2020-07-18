using System;
using System.Collections.Immutable;

namespace Lifti
{
    /// <summary>
    /// A factory for <see cref="IndexNode"/> instances used by a <see cref="FullTextIndex{TKey}"/>
    /// when adding nodes to the index.
    /// </summary>
    public class IndexNodeFactory : ConfiguredBy<IndexOptions>, IIndexNodeFactory
    {
        private int supportIntraNodeTextAtDepth;

        /// <summary>
        /// Creates a new instance of <see cref="IndexNodeFactory"/>.
        /// </summary>
        public IndexNode CreateRootNode()
        {
            return new IndexNode(
                null,
                ImmutableDictionary<char, IndexNode>.Empty,
                ImmutableDictionary<int, ImmutableList<IndexedToken>>.Empty);
        }

        /// <inheritdoc/>
        public IndexSupportLevelKind GetIndexSupportLevelForDepth(int depth)
        {
            return depth >= this.supportIntraNodeTextAtDepth ?
                IndexSupportLevelKind.IntraNodeText :
                IndexSupportLevelKind.CharacterByCharacter;
        }

        /// <inheritdoc/>
        public IndexNode CreateNode(
            ReadOnlyMemory<char> intraNodeText,
            ImmutableDictionary<char, IndexNode> childNodes,
            ImmutableDictionary<int, ImmutableList<IndexedToken>> matches)
        {
            return new IndexNode(intraNodeText, childNodes, matches);
        }

        /// <inheritdoc/>
        protected override void OnConfiguring(IndexOptions options)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            this.supportIntraNodeTextAtDepth = options.SupportIntraNodeTextAfterIndexDepth;
        }
    }
}
