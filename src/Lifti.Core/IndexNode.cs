using System;
using System.Diagnostics.Contracts;
using System.Text;

namespace Lifti
{
    /// <summary>
    /// A node that forms part of the structure of an index.
    /// </summary>
    public class IndexNode
    {
        internal IndexNode(
            ReadOnlyMemory<char> intraNodeText,
            ChildNodeMap childNodes,
            DocumentTokenMatchMap matches)
        {
            this.IntraNodeText = intraNodeText;
            this.ChildNodes = childNodes;
            this.Matches = matches;
        }

        /// <summary>
        /// Gets the continuous sequence of characters that are matched at this node. When traversing the index,
        /// any matches found at this node will not be considered an exact match until this 
        /// text has been completely processed.
        /// </summary>
        public ReadOnlyMemory<char> IntraNodeText { get; }

        /// <summary>
        /// Gets any child nodes that can be navigated to from this instance, once the intra-node text has 
        /// been processed.
        /// </summary>
        public ChildNodeMap ChildNodes { get; }

        /// <summary>
        /// Gets the set of matches that are found at this location in the index (once all the <see cref="IntraNodeText"/>
        /// has been processed.)
        /// </summary>
        public DocumentTokenMatchMap Matches { get; }

        /// <summary>
        /// Gets a value indicating whether this node is empty. A node is considered empty if it doesn't have 
        /// any child nodes, and it doesn't have any matches.
        /// </summary>
        public bool IsEmpty => !this.HasChildNodes && !this.HasMatches;

        /// <summary>
        /// Gets a value indicating whether this instance has any child nodes.
        /// </summary>
        public bool HasChildNodes => this.ChildNodes.Count > 0;

        /// <summary>
        /// Gets a value indicating whether this instance has any items matched at it.
        /// </summary>
        public bool HasMatches => this.Matches.Count > 0;

        /// <inheritdoc />
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

        /// <summary>
        /// Formats a single child node linked from this instance to the given <paramref name="builder"/>.
        /// </summary>
        internal void ToString(StringBuilder builder, char linkChar, int currentDepth)
        {
            builder.Append(' ', currentDepth * 2)
                .Append(linkChar)
                .Append(' ');

            this.FormatNodeText(builder);

            this.FormatChildNodeText(builder, currentDepth);
        }

        /// <summary>
        /// Formats all the child nodes of this instance to the given <paramref name="builder"/>.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="nextDepth"></param>
        internal void ToString(StringBuilder builder, int nextDepth)
        {
            foreach (var (character, childNode) in this.ChildNodes.CharacterMap)
            {
                builder.AppendLine();
                childNode.ToString(builder, character, nextDepth);
            }
        }

        private void FormatChildNodeText(StringBuilder builder, int currentDepth)
        {
            if (this.HasChildNodes)
            {
                var nextDepth = currentDepth + 1;

                foreach (var (character, childNode) in this.ChildNodes.CharacterMap)
                {
                    builder.AppendLine();
                    childNode.ToString(builder, character, nextDepth);
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
                builder.Append(" [").Append(this.Matches.Count).Append(" matche(s)]");
            }
        }
    }
}
