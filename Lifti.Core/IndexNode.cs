using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lifti
{
    public class IndexNode
    {
        private Dictionary<int, IReadOnlyList<Range>> matches;
        private Dictionary<char, IndexNode> childNodes;
        private readonly IIndexNodeFactory indexNodeFactory;

        internal IndexNode ParentNode { get; }
        internal char[] IntraNodeText { get; private set; }
        internal IReadOnlyDictionary<char, IndexNode> ChildNodes => this.childNodes;
        internal IReadOnlyDictionary<int, IReadOnlyList<Range>> Matches => this.matches;

        public IndexNode(IIndexNodeFactory indexNodeFactory)
        {
            this.indexNodeFactory = indexNodeFactory;
        }

        public IndexNode(IIndexNodeFactory indexNodeFactory, IndexNode parent)
        {
            this.indexNodeFactory = indexNodeFactory;
            this.ParentNode = parent;
        }

        public void Index(int itemId, SplitWord word)
        {
            this.Index(itemId, word.Locations, word.Word);
        }

        private void Index(int itemId, IReadOnlyList<Range> locations, ReadOnlySpan<char> remainingWordText)
        {
            if (this.IntraNodeText == null)
            {
                if (this.childNodes == null && this.matches == null)
                {
                    // Currently a leaf node
                    IntraNodeText = remainingWordText.Length == 0 ? null : remainingWordText.ToArray();
                    AddMatchedItem(itemId, locations);
                }
                else
                {
                    this.ContinueIndexingAtChild(itemId, locations, remainingWordText, 0);
                }
            }
            else
            {
                if (remainingWordText.Length == 0)
                {
                    throw new InvalidOperationException("Remaining word text should not be empty at a leaf node that is not empty");
                }

                var testLength = Math.Min(remainingWordText.Length, this.IntraNodeText.Length);
                for (var i = 0; i < testLength; i++)
                {
                    if (remainingWordText[i] != this.IntraNodeText[i])
                    {
                        this.SplitIntraNodeText(itemId, i, locations, remainingWordText);
                        ContinueIndexingAtChild(itemId, locations, remainingWordText, i);
                        return;
                    }
                }

                // No split occurred
                if (remainingWordText.Length > testLength)
                {
                    this.ContinueIndexingAtChild(itemId, locations, remainingWordText, testLength);
                }
                else
                {
                    // Remaining text == intraNodeText
                    AddMatchedItem(itemId, locations);
                }
            }
        }

        private void ContinueIndexingAtChild(int itemId, IReadOnlyList<Range> locations, ReadOnlySpan<char> remainingWordText, int remainingTextSplitPosition)
        {
            EnsureChildNodesLookupCreated();

            var indexChar = remainingWordText[remainingTextSplitPosition];
            if (!this.childNodes.TryGetValue(indexChar, out var childNode))
            {
                childNode = this.indexNodeFactory.CreateChildNodeFor(this);
                this.childNodes.Add(indexChar, childNode);
            }

            childNode.Index(itemId, locations, remainingWordText.Slice(remainingTextSplitPosition + 1));
        }

        private void EnsureChildNodesLookupCreated()
        {
            if (this.childNodes == null)
            {
                this.childNodes = new Dictionary<char, IndexNode>();
            }
        }

        private void SplitIntraNodeText(int itemId, int splitIndex, IReadOnlyList<Range> locations, ReadOnlySpan<char> remainingWordText)
        {
            var intraTextSpan = this.IntraNodeText.AsSpan();
            var splitChildNode = this.indexNodeFactory.CreateChildNodeFor(this);
            splitChildNode.IntraNodeText = splitIndex + 1 == intraTextSpan.Length ? null : intraTextSpan.Slice(splitIndex + 1).ToArray();
            splitChildNode.childNodes = this.childNodes;
            splitChildNode.matches = this.matches;
            this.matches = null;
            this.childNodes = new Dictionary<char, IndexNode>();

            if (splitIndex == 0)
            {
                this.IntraNodeText = null;
            }
            else
            {
                this.IntraNodeText = intraTextSpan.Slice(0, splitIndex).ToArray();
            }

            this.childNodes.Add(intraTextSpan[splitIndex], splitChildNode);
        }

        private void AddMatchedItem(int itemId, IReadOnlyList<Range> locations)
        {
            if (this.matches == null)
            {
                this.matches = new Dictionary<int, IReadOnlyList<Range>>();
            }

            this.matches.Add(itemId, locations);
        }

        public override string ToString()
        {
            if (this.childNodes == null && this.matches == null)
            {
                return "<EMPTY>";
            }

            var builder = new StringBuilder();
            this.FormatNodeText(builder);
            this.FormatChildNodeText(builder, 0);

            return builder.ToString();
        }

        private void ToString(StringBuilder builder, char linkChar, int currentDepth)
        {
            builder.Append(' ', currentDepth * 2)
                .Append(linkChar)
                .Append(' ');

            this.FormatNodeText(builder);

            this.FormatChildNodeText(builder, currentDepth);
        }

        private void FormatChildNodeText(StringBuilder builder, int currentDepth)
        {
            if (this.childNodes != null)
            {
                var nextDepth = currentDepth + 1;
                foreach (var item in this.childNodes)
                {
                    builder.AppendLine();
                    item.Value.ToString(builder, item.Key, nextDepth);
                }
            }
        }

        private void FormatNodeText(StringBuilder builder)
        {
            if (this.IntraNodeText != null)
            {
                builder.Append(this.IntraNodeText);
            }

            if (this.matches != null)
            {
                builder.Append(" [").Append(this.matches.Count).Append(" matche(s)]");
            }
        }
    }
}
