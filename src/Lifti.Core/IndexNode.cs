using Lifti.Tokenization;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Lifti
{
    public enum IndexSupportLevelKind
    {
        CharacterByCharacter = 0,
        IntraNodeText = 1
    }

    public class IndexNode
    {
        private Dictionary<int, List<IndexedWord>> matches;
        private Dictionary<char, IndexNode> childNodes;
        private readonly IIndexNodeFactory indexNodeFactory;
        private readonly IndexSupportLevelKind indexSupportLevel;

        public IndexNode(IIndexNodeFactory indexNodeFactory, int depth, IndexSupportLevelKind indexSupportLevel)
        {
            this.indexNodeFactory = indexNodeFactory;
            this.Depth = depth;
            this.indexSupportLevel = indexSupportLevel;
        }

        public int Depth { get; }
        internal char[] IntraNodeText { get; private set; }
        internal IReadOnlyDictionary<char, IndexNode> ChildNodes => this.childNodes;
        internal IReadOnlyDictionary<int, List<IndexedWord>> Matches => this.matches;

        public void Index(int itemId, byte fieldId, Token word)
        {
            if (word is null)
            {
                throw new ArgumentNullException(nameof(word));
            }

            Debug.Assert(word.Locations.Select((l, i) => i > 0 && l.WordIndex > word.Locations[i - 1].WordIndex).All(v => v));

            this.Index(itemId, fieldId, word.Locations, word.Value.AsSpan());
        }

        private void Index(int itemId, byte fieldId, IReadOnlyList<WordLocation> locations, ReadOnlySpan<char> remainingWordText)
        {
            switch (this.indexSupportLevel)
            {
                case IndexSupportLevelKind.CharacterByCharacter:
                    this.IndexFromCharacter(itemId, fieldId, locations, remainingWordText);
                    break;
                case IndexSupportLevelKind.IntraNodeText:
                    this.IndexWithIntraNodeTextSupport(itemId, fieldId, locations, remainingWordText);
                    break;
                default:
                    throw new LiftiException(ExceptionMessages.UnsupportedIndexSupportLevel, this.indexSupportLevel);
            }

        }

        private void IndexWithIntraNodeTextSupport(int itemId, byte fieldId, IReadOnlyList<WordLocation> locations, ReadOnlySpan<char> remainingWordText)
        {
            if (this.IntraNodeText == null)
            {
                if (this.childNodes == null && this.matches == null)
                {
                    // Currently a leaf node
                    this.IntraNodeText = remainingWordText.Length == 0 ? null : remainingWordText.ToArray();
                    this.AddMatchedItem(itemId, fieldId, locations);
                }
                else
                {
                    this.IndexFromCharacter(itemId, fieldId, locations, remainingWordText);
                }
            }
            else
            {
                if (remainingWordText.Length == 0)
                {
                    // The indexing ends before the start of the intranode text so we need to split
                    this.SplitIntraNodeText(0);
                    this.AddMatchedItem(itemId, fieldId, locations);
                    return;
                }

                var testLength = Math.Min(remainingWordText.Length, this.IntraNodeText.Length);
                for (var i = 0; i < testLength; i++)
                {
                    if (remainingWordText[i] != this.IntraNodeText[i])
                    {
                        this.SplitIntraNodeText(i);
                        this.ContinueIndexingAtChild(itemId, fieldId, locations, remainingWordText, i);
                        return;
                    }
                }

                // No split occurred
                this.IndexFromCharacter(itemId, fieldId, locations, remainingWordText, testLength);
            }
        }

        private void IndexFromCharacter(int itemId, byte fieldId, IReadOnlyList<WordLocation> locations, ReadOnlySpan<char> remainingWordText, int testLength = 0)
        {
            if (remainingWordText.Length > testLength)
            {
                this.ContinueIndexingAtChild(itemId, fieldId, locations, remainingWordText, testLength);
            }
            else
            {
                // Remaining text == intraNodeText
                this.AddMatchedItem(itemId, fieldId, locations);
            }
        }

        private void ContinueIndexingAtChild(int itemId, byte fieldId, IReadOnlyList<WordLocation> locations, ReadOnlySpan<char> remainingWordText, int remainingTextSplitPosition)
        {
            this.EnsureChildNodesLookupCreated();

            var indexChar = remainingWordText[remainingTextSplitPosition];
            if (!this.childNodes.TryGetValue(indexChar, out var childNode))
            {
                childNode = this.indexNodeFactory.CreateNode(this);
                this.childNodes.Add(indexChar, childNode);
            }

            childNode.Index(itemId, fieldId, locations, remainingWordText.Slice(remainingTextSplitPosition + 1));
        }

        private void EnsureChildNodesLookupCreated()
        {
            if (this.childNodes == null)
            {
                this.childNodes = new Dictionary<char, IndexNode>();
            }
        }

        private void SplitIntraNodeText(int splitIndex)
        {
            var intraTextSpan = this.IntraNodeText.AsSpan();
            var splitChildNode = this.indexNodeFactory.CreateNode(this);
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

        private void AddMatchedItem(int itemId, byte fieldId, IReadOnlyList<WordLocation> locations)
        {
            if (this.matches == null)
            {
                this.matches = new Dictionary<int, List<IndexedWord>>();
            }

            if (!this.matches.TryGetValue(itemId, out var itemFieldLocations))
            {
                itemFieldLocations = new List<IndexedWord>(); // TODO Constrain list to size == number of fields for index?
                this.matches[itemId] = itemFieldLocations;
            }

            itemFieldLocations.Add(new IndexedWord(fieldId, locations));
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
