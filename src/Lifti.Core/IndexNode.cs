using System;
using System.Collections.Immutable;
using System.Diagnostics.Contracts;
using System.Text;

namespace Lifti
{
    public class IndexNode
    {
        internal IndexNode(
            ReadOnlyMemory<char> intraNodeText,
            ImmutableDictionary<char, IndexNode> childNodes,
            ImmutableDictionary<int, ImmutableList<IndexedWord>> matches)
        {
            this.IntraNodeText = intraNodeText;
            this.ChildNodes = childNodes;
            this.Matches = matches;
        }

        public ReadOnlyMemory<char> IntraNodeText { get; }
        public ImmutableDictionary<char, IndexNode> ChildNodes { get; }
        public ImmutableDictionary<int, ImmutableList<IndexedWord>> Matches { get; }
        public bool IsEmpty => !this.HasChildNodes && !this.HasMatches;
        public bool HasChildNodes => this.ChildNodes?.Count > 0;
        public bool HasMatches => this.Matches?.Count > 0;

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
                .Append(' ');

            this.FormatNodeText(builder);

            this.FormatChildNodeText(builder, currentDepth);
        }

        private void FormatChildNodeText(StringBuilder builder, int currentDepth)
        {
            if (this.HasChildNodes)
            {
                var nextDepth = currentDepth + 1;
                foreach (var item in this.ChildNodes)
                {
                    builder.AppendLine();
                    item.Value.ToString(builder, item.Key, nextDepth);
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
