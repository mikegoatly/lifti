using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Text;

namespace Lifti
{
    internal class IndexNodeMutation
    {
        private readonly int depth;
        private readonly IIndexNodeFactory indexNodeFactory;
        private IndexNode? original;

        private IndexNodeMutation(IndexNodeMutation parent)
            : this(parent.depth + 1, parent.indexNodeFactory)
        {
        }

        private IndexNodeMutation(int depth, IIndexNodeFactory indexNodeFactory)
        {
            this.depth = depth;
            this.indexNodeFactory = indexNodeFactory;
        }

        public IndexNodeMutation(int depth, IndexNode node, IIndexNodeFactory indexNodeFactory)
            : this(depth, indexNodeFactory)
        {
            this.original = node;
            this.IntraNodeText = node.IntraNodeText;

            this.HasMatches = node.HasMatches;
            this.HasChildNodes = node.HasChildNodes;
        }

        public bool IsEmpty => !this.HasChildNodes && !this.HasMatches;

        /// <remarks>
        /// Important note about <see cref="HasChildNodes"/> and <see cref="HasMatches"/> in an <see cref="IndexNodeMutation"/>:
        /// We can't easily derive the presence of child nodes or mutations from state, because it could be tracked in the 
        /// original unmodified node or the mutated state. To reduce compute effort, these flags are cached and manually updated. 
        /// </remarks>
        public bool HasChildNodes { get; private set; }
        public bool HasMatches { get; private set; }
        public ReadOnlyMemory<char> IntraNodeText { get; private set; }
        public ChildNodeMapMutation? ChildNodeMapMutation { get; private set; }

        public DocumentTokenMatchMapMutation? DocumentTokenMatchMapMutation { get; private set; }

        internal void Index(
            int documentId,
            byte fieldId,
            IReadOnlyList<TokenLocation> locations,
            ReadOnlyMemory<char> remainingTokenText)
        {
            var indexSupportLevel = this.indexNodeFactory.GetIndexSupportLevelForDepth(this.depth);
            switch (indexSupportLevel)
            {
                case IndexSupportLevelKind.CharacterByCharacter:
                    this.IndexFromCharacter(documentId, fieldId, locations, remainingTokenText);
                    break;
                case IndexSupportLevelKind.IntraNodeText:
                    this.IndexWithIntraNodeTextSupport(documentId, fieldId, locations, remainingTokenText);
                    break;
                default:
                    throw new LiftiException(ExceptionMessages.UnsupportedIndexSupportLevel, indexSupportLevel);
            }
        }

        internal IndexNode Apply()
        {
            ChildNodeMap childNodes;
            DocumentTokenMatchMap matches;

            if (this.original == null)
            {
                childNodes = this.ChildNodeMapMutation?.Apply() ?? ChildNodeMap.Empty;
                matches = this.DocumentTokenMatchMapMutation?.Apply() ?? DocumentTokenMatchMap.Empty;
            }
            else
            {
                childNodes = this.ChildNodeMapMutation?.Apply() ?? this.original.ChildNodes;
                matches = this.DocumentTokenMatchMapMutation?.Apply() ?? this.original.Matches;
            }

            return this.indexNodeFactory.CreateNode(this.IntraNodeText, childNodes, matches);
        }

        internal void Remove(int documentId)
        {
            if (this.HasChildNodes)
            {
                if (this.ChildNodeMapMutation != null)
                {
                    // First look through any already mutated child nodes
                    foreach (var (_, mutatedChild) in this.ChildNodeMapMutation.GetMutated())
                    {
                        mutatedChild.Remove(documentId);
                    }

                    // Then any unmutated children
                    foreach (var (childChar, childNode) in this.ChildNodeMapMutation.GetUnmutated())
                    {
                        if (this.TryRemove(childNode, documentId, this.depth + 1, out var mutatedChild))
                        {
                            this.ChildNodeMapMutation.Mutate(childChar, mutatedChild);
                        }
                    }
                }
                else if (this.original != null)
                {
                    // Then any unmutated children
                    foreach (var (childChar, childNode) in this.original.ChildNodes.CharacterMap)
                    {
                        if (this.TryRemove(childNode, documentId, this.depth + 1, out var mutatedChild))
                        {
                            var childNodeMapMutation = this.EnsureMutatedChildNodesCreated();
                            childNodeMapMutation.Mutate(childChar, mutatedChild);
                        }
                    }
                }
            }

            if (this.HasMatches)
            {
                if (this.DocumentTokenMatchMapMutation != null)
                {
                    this.DocumentTokenMatchMapMutation.Remove(documentId);
                }
                else
                {
                    if (this.original != null && this.original.Matches.HasDocument(documentId))
                    {
                        // Mutate and remove
                        var matchMutation = this.EnsureMutatedMatchesCreated();
                        matchMutation.Remove(documentId);
                    }
                }
            }
        }

        private bool TryRemove(IndexNode node, int documentId, int nodeDepth, [NotNullWhen(true)] out IndexNodeMutation? mutatedNode)
        {
            mutatedNode = null;

            if (node.HasChildNodes)
            {
                // Work through the child nodes and recursively determine whether removals are needed from 
                // them. If they are, then this instance will also become mutated.
                foreach (var (character, childNode) in node.ChildNodes.CharacterMap)
                {
                    if (this.TryRemove(childNode, documentId, nodeDepth + 1, out var mutatedChild))
                    {
                        if (mutatedNode == null)
                        {
                            mutatedNode = new IndexNodeMutation(nodeDepth, node, this.indexNodeFactory);
                            mutatedNode.EnsureMutatedChildNodesCreated();
                        }

                        mutatedNode.ChildNodeMapMutation!.Mutate(character, mutatedChild);
                    }
                }
            }

            if (node.HasMatches)
            {
                if (node.Matches.HasDocument(documentId))
                {
                    mutatedNode ??= new IndexNodeMutation(nodeDepth, node, this.indexNodeFactory);

                    var matchMutation = mutatedNode.EnsureMutatedMatchesCreated();
                    matchMutation.Remove(documentId);
                }
            }

            return mutatedNode != null;
        }

        private void IndexFromCharacter(
            int documentId,
            byte fieldId,
            IReadOnlyList<TokenLocation> locations,
            ReadOnlyMemory<char> remainingTokenText,
            int testLength = 0)
        {
            if (remainingTokenText.Length > testLength)
            {
                this.ContinueIndexingAtChild(documentId, fieldId, locations, remainingTokenText, testLength);
            }
            else
            {
                // Remaining text == intraNodeText
                this.AddMatchedDocument(documentId, fieldId, locations);
            }
        }

        private void ContinueIndexingAtChild(
            int documentId,
            byte fieldId,
            IReadOnlyList<TokenLocation> locations,
            ReadOnlyMemory<char> remainingTokenText,
            int remainingTextSplitPosition)
        {
            var indexChar = remainingTokenText.Span[remainingTextSplitPosition];

            var childNodeMutation = this.EnsureMutatedChildNodesCreated();
            var childNode = childNodeMutation.GetOrCreateMutation(
                indexChar,
                () => this.original?.ChildNodes.TryGetValue(indexChar, out var originalChildNode) == true
                    // the original had an unmutated child node that matched the index character - mutate it now
                    ? new IndexNodeMutation(this.depth + 1, originalChildNode, this.indexNodeFactory)
                    // This is a novel branch in the index
                    : new IndexNodeMutation(this));

            childNode.Index(documentId, fieldId, locations, remainingTokenText.Slice(remainingTextSplitPosition + 1));
        }

        private ChildNodeMapMutation EnsureMutatedChildNodesCreated()
        {
            if (this.ChildNodeMapMutation == null)
            {
                this.HasChildNodes = true;
                this.ChildNodeMapMutation = new ChildNodeMapMutation(this.original?.ChildNodes ?? ChildNodeMap.Empty);
            }

            return this.ChildNodeMapMutation;
        }

        private DocumentTokenMatchMapMutation EnsureMutatedMatchesCreated()
        {
            if (this.DocumentTokenMatchMapMutation == null)
            {
                this.HasMatches = true;
                this.DocumentTokenMatchMapMutation = new DocumentTokenMatchMapMutation(this.original?.Matches ?? DocumentTokenMatchMap.Empty);
            }

            return this.DocumentTokenMatchMapMutation;
        }

        private void IndexWithIntraNodeTextSupport(
            int documentId,
            byte fieldId,
            IReadOnlyList<TokenLocation> locations,
            ReadOnlyMemory<char> remainingTokenText)
        {
            if (this.IntraNodeText.Length == 0)
            {
                if (this.IsEmpty)
                {
                    // Currently a leaf node
                    this.IntraNodeText = remainingTokenText.Length == 0 ? null : remainingTokenText;
                    this.AddMatchedDocument(documentId, fieldId, locations);
                }
                else
                {
                    this.IndexFromCharacter(documentId, fieldId, locations, remainingTokenText);
                }
            }
            else
            {
                if (remainingTokenText.Length == 0)
                {
                    // The indexing ends before the start of the intranode text so we need to split
                    this.SplitIntraNodeText(0);
                    this.AddMatchedDocument(documentId, fieldId, locations);
                    return;
                }

                // Test the current intra-node text against the remaining token text to see if
                // we can index here or need to split
                var testLength = Math.Min(remainingTokenText.Length, this.IntraNodeText.Length);
                var intraNodeSpan = this.IntraNodeText.Span;
                var tokenSpan = remainingTokenText.Span;
                for (var i = 0; i < testLength; i++)
                {
                    if (tokenSpan[i] != intraNodeSpan[i])
                    {
                        this.SplitIntraNodeText(i);
                        this.ContinueIndexingAtChild(documentId, fieldId, locations, remainingTokenText, i);
                        return;
                    }
                }

                if (this.IntraNodeText.Length > testLength)
                {
                    // This token is indexed in the middle of intra-node text. Split it and index here
                    this.SplitIntraNodeText(testLength);
                }

                this.IndexFromCharacter(documentId, fieldId, locations, remainingTokenText, testLength);
            }
        }

        private void AddMatchedDocument(int documentId, byte fieldId, IReadOnlyList<TokenLocation> locations)
        {
            var indexedToken = new IndexedToken(fieldId, locations);
            var documentTokenMatchMutations = this.EnsureMutatedMatchesCreated();
            documentTokenMatchMutations.Add(documentId, indexedToken);
        }

        private void SplitIntraNodeText(int splitIndex)
        {
            var splitChildNode = new IndexNodeMutation(this)
            {
                HasMatches = this.HasMatches,
                HasChildNodes = this.HasChildNodes,
                ChildNodeMapMutation = this.ChildNodeMapMutation,
                DocumentTokenMatchMapMutation = this.DocumentTokenMatchMapMutation,
                IntraNodeText = splitIndex + 1 == this.IntraNodeText.Length ? null : this.IntraNodeText.Slice(splitIndex + 1),

                // Pass the original down to the child node - the only state that matters there is any unmutated child nodes/matches
                original = this.original
            };

            this.original = null;

            var splitChar = this.IntraNodeText.Span[splitIndex];

            // Reset the matches at this node
            this.DocumentTokenMatchMapMutation = null;
            this.HasMatches = false;

            // Replace any remaining intra node text
            this.IntraNodeText = splitIndex == 0 ? null : this.IntraNodeText.Slice(0, splitIndex);

            this.HasChildNodes = true;
            this.ChildNodeMapMutation = new(splitChar, splitChildNode);
        }

        [Pure]
        public override string ToString()
        {
            if (this.IsEmpty)
            {
                return "<EMPTY>";
            }

            var builder = new StringBuilder();
            this.FormatNodeText(builder);
            this.FormatChildNodeText(builder, 0);

            return builder.ToString();
        }

        internal void ToString(StringBuilder builder, char linkChar, int currentDepth)
        {
            builder.Append(' ', currentDepth * 2)
                .Append(linkChar)
                .Append('*')
                .Append(' ');

            this.FormatNodeText(builder);

            this.FormatChildNodeText(builder, currentDepth);
        }

        private void FormatChildNodeText(StringBuilder builder, int currentDepth)
        {
            if (this.HasChildNodes)
            {
                var nextDepth = currentDepth + 1;

                if (this.ChildNodeMapMutation is { } childNodeMutations)
                {
                    childNodeMutations.ToString(builder, currentDepth);
                }
                else
                {
                    this.original?.ToString(builder, nextDepth);
                }
            }
        }

        private void FormatNodeText(StringBuilder builder)
        {
            if (this.IntraNodeText.Length > 0)
            {
                builder.Append(this.IntraNodeText);
            }

            if (this.HasMatches)
            {
                builder.Append(
                    System.Globalization.CultureInfo.InvariantCulture,
                    $" [{this.original?.Matches.Count ?? 0} original match(es) - {this.DocumentTokenMatchMapMutation?.MutationCount ?? 0} mutated]");
            }
        }
    }
}
