using System;
using System.Collections.Generic;
using System.Linq;

namespace Lifti
{
    public partial class FullTextIndex<TKey>
    {
        internal class SearchContext
        {
            private readonly FullTextIndex<TKey> index;
            private readonly Dictionary<int, List<IndexedWordLocation>> results = new Dictionary<int, List<IndexedWordLocation>>(); // TODO ugly - helper class?

            public SearchContext(FullTextIndex<TKey> index)
            {
                this.index = index;
            }

            // Exact match only for now
            public void Match(ReadOnlySpan<char> remainingMatchText)
            {
                var currentNode = this.index.Root;
                while (currentNode != null)
                {
                    if (remainingMatchText.Length == 0)
                    {
                        this.AddMatches(currentNode.Matches);
                        break;
                    }

                    if (currentNode.IntraNodeText != null)
                    {
                        var intraNodeTextLength = currentNode.IntraNodeText.Length;
                        if (intraNodeTextLength > remainingMatchText.Length)
                        {
                            // Exact match only for now - intra node text is too long to match
                            break;
                        }

                        if (currentNode.IntraNodeText.AsSpan().SequenceEqual(remainingMatchText.Slice(0, intraNodeTextLength))) // Optimize?
                        {
                            if (intraNodeTextLength == remainingMatchText.Length)
                            {
                                // Exact match on intra node text - match on any direct matches
                                this.AddMatches(currentNode.Matches);
                                break;
                            }

                            // Reduce the remaining text to match by the matched intra node text
                            remainingMatchText = remainingMatchText.Slice(intraNodeTextLength);
                        }
                        else
                        {
                            // Intra node text doesn't match - no match
                            break;
                        }

                        if (currentNode.ChildNodes == null)
                        {
                            // No children to continue matching with - not a match
                            break;
                        }
                    }

                    currentNode.ChildNodes.TryGetValue(remainingMatchText[0], out currentNode);

                    // Reduce the remaining text to match by the character just looked up against the child nodes
                    remainingMatchText = remainingMatchText.Slice(1);
                }
            }

            private void AddMatches(IReadOnlyDictionary<int, List<IndexedWordLocation>> matches)
            {
                foreach (var matchedItem in matches)
                {
                    if (!this.results.TryGetValue(matchedItem.Key, out var itemResults))
                    {
                        itemResults = new List<IndexedWordLocation>();
                        this.results[matchedItem.Key] = itemResults;
                    }

                    itemResults.AddRange(matchedItem.Value);
                }
            }

            public IEnumerable<SearchResult<TKey>> Results()
            {
                foreach (var itemResults in this.results)
                {
                    var item = this.index.idPool.GetItemForId(itemResults.Key);
                    yield return new SearchResult<TKey>(
                        item,
                        itemResults.Value.Select(m => new MatchedLocation(this.index.fieldLookup.GetFieldForId(m.FieldId), m.Locations)).ToList());
                }
            }
        }
    }
}
