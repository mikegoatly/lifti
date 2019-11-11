using System;
using System.Collections.Immutable;

namespace Lifti
{
    public class IndexNodeFactory : ConfiguredBy<AdvancedOptions>, IIndexNodeFactory
    {
        private int supportIntraNodeTextAtDepth;

        public IndexNode CreateRootNode()
        {
            return new IndexNode(
                null,
                ImmutableDictionary<char, IndexNode>.Empty,
                ImmutableDictionary<int, ImmutableList<IndexedWord>>.Empty);
        }

        public IndexSupportLevelKind GetIndexSupportLevelForDepth(int depth)
        {
            return depth >= this.supportIntraNodeTextAtDepth ?
                IndexSupportLevelKind.IntraNodeText :
                IndexSupportLevelKind.CharacterByCharacter;
        }

        public IndexNode CreateNode(
            ReadOnlyMemory<char> intraNodeText,
            ImmutableDictionary<char, IndexNode> childNodes,
            ImmutableDictionary<int, ImmutableList<IndexedWord>> matches)
        {
            return new IndexNode(intraNodeText, childNodes, matches);
        }

        protected override void OnConfiguring(AdvancedOptions options)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            this.supportIntraNodeTextAtDepth = options.SupportIntraNodeTextAfterIndexDepth;
        }
    }
}
