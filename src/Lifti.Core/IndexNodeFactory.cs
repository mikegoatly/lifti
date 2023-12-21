using System;

namespace Lifti
{
    /// <summary>
    /// A factory for <see cref="IndexNode"/> instances used by a <see cref="FullTextIndex{TKey}"/>
    /// when adding nodes to the index.
    /// </summary>
    public class IndexNodeFactory : IIndexNodeFactory
    {
        private readonly int supportIntraNodeTextAtDepth;

        /// <summary>
        /// Initializes a new instance of the <see cref="IndexNodeFactory"/> class.
        /// </summary>
        public IndexNodeFactory(IndexOptions options)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            this.supportIntraNodeTextAtDepth = options.SupportIntraNodeTextAfterIndexDepth;
        }

        /// <summary>
        /// Creates a new instance of <see cref="IndexNodeFactory"/>.
        /// </summary>
        public IndexNode CreateRootNode()
        {
            return new IndexNode(
                null,
                ChildNodeMap.Empty,
                DocumentTokenMatchMap.Empty);
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
            ChildNodeMap childNodes,
            DocumentTokenMatchMap matches)
        {
            return new IndexNode(intraNodeText, childNodes, matches);
        }
    }
}
