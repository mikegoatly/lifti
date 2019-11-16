using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace Lifti.Querying
{

    internal sealed class IndexNavigator : IIndexNavigator
    {
        private readonly StringBuilder navigatedWith = new StringBuilder(16);
        private IIndexNavigatorPool pool;
        private IndexNode currentNode;
        private int intraNodeTextPosition;

        internal void Initialize(IndexNode node, IIndexNavigatorPool pool)
        {
            this.pool = pool;
            this.currentNode = node;
            this.intraNodeTextPosition = 0;
            this.navigatedWith.Length = 0;
        }

        private bool HasIntraNodeTextLeftToProcess => this.intraNodeTextPosition < this.currentNode.IntraNodeText.Length;

        public bool HasExactMatches
        {
            get
            {
                if (this.currentNode == null || this.HasIntraNodeTextLeftToProcess || !this.currentNode.HasMatches)
                {
                    return false;
                }

                return this.currentNode.HasMatches;
            }
        }

        public IntermediateQueryResult GetExactMatches()
        {
            if (this.currentNode == null || this.HasIntraNodeTextLeftToProcess || !this.currentNode.HasMatches)
            {
                return IntermediateQueryResult.Empty;
            }

            return new IntermediateQueryResult(this.currentNode.Matches.Select(CreateQueryWordMatch));
        }

        public IntermediateQueryResult GetExactAndChildMatches()
        {
            if (this.currentNode == null)
            {
                return IntermediateQueryResult.Empty;
            }

            var matches = new Dictionary<int, List<FieldMatch>>();
            var childNodeStack = new Queue<IndexNode>();
            childNodeStack.Enqueue(this.currentNode);

            while (childNodeStack.Count > 0)
            {
                var node = childNodeStack.Dequeue();
                if (node.HasMatches)
                {
                    foreach (var match in node.Matches)
                    {
                        var fieldMatches = match.Value.Select(v => new FieldMatch(v));
                        if (!matches.TryGetValue(match.Key, out var mergedItemResults))
                        {
                            mergedItemResults = new List<FieldMatch>(fieldMatches);
                            matches[match.Key] = mergedItemResults;
                        }
                        else
                        {
                            mergedItemResults.AddRange(fieldMatches);
                        }
                    }
                }

                if (node.HasChildNodes)
                {
                    foreach (var childNode in node.ChildNodes.Values)
                    {
                        childNodeStack.Enqueue(childNode);
                    }
                }
            }

            return new IntermediateQueryResult(matches.Select(m => new QueryWordMatch(m.Key, this.MergeItemMatches(m.Value))));
        }

        public bool Process(ReadOnlySpan<char> text)
        {
            foreach (var next in text)
            {
                if (!this.Process(next))
                {
                    return false;
                }
            }

            return true;
        }

        public bool Process(char value)
        {
            if (this.currentNode == null)
            {
                return false;
            }

            this.navigatedWith.Append(value);

            if (this.HasIntraNodeTextLeftToProcess)
            {
                if (value == this.currentNode.IntraNodeText.Span[this.intraNodeTextPosition])
                {
                    this.intraNodeTextPosition++;
                    return true;
                }

                this.currentNode = null;
                return false;
            }

            if (this.currentNode.HasChildNodes && this.currentNode.ChildNodes.TryGetValue(value, out var nextNode))
            {
                this.currentNode = nextNode;
                this.intraNodeTextPosition = 0;
                return true;
            }

            this.currentNode = null;
            return false;
        }

        public IEnumerable<string> EnumerateIndexedWords()
        {
            if (this.currentNode == null)
            {
                return Enumerable.Empty<string>();
            }

            if (this.intraNodeTextPosition > 0)
            {
                this.navigatedWith.Length -= this.intraNodeTextPosition;
            }

            var results = this.EnumerateIndexedWords(this.currentNode).ToList();

            if (this.intraNodeTextPosition > 0)
            {
                var span = this.currentNode.IntraNodeText.Span;
                for (var i = 0; i < this.intraNodeTextPosition; i++)
                {
                    this.navigatedWith.Append(span[i]);
                }
            }

            return results;
        }

        public void Dispose()
        {
            this.pool.Return(this);
        }

        private IEnumerable<string> EnumerateIndexedWords(IndexNode node)
        {
            if (node.IntraNodeText.Length > 0)
            {
                this.navigatedWith.Append(node.IntraNodeText);
            }

            if (node.HasMatches)
            {
                yield return this.navigatedWith.ToString();
            }

            if (node.HasChildNodes)
            {
                foreach (var childNode in node.ChildNodes)
                {
                    this.navigatedWith.Append(childNode.Key);
                    foreach (var result in this.EnumerateIndexedWords(childNode.Value))
                    {
                        yield return result;
                    }

                    this.navigatedWith.Length -= 1;
                }
            }

            if (node.IntraNodeText.Length > 0)
            {
                this.navigatedWith.Length -= node.IntraNodeText.Length;
            }
        }

        private IEnumerable<FieldMatch> MergeItemMatches(List<FieldMatch> fieldMatches)
        {
            return fieldMatches.ToLookup(m => m.FieldId)
                .Select(m => new FieldMatch(
                    m.Key,
                    m.SelectMany(w => w.Locations).OrderBy(w => w.MinWordIndex).ToList()));
        }

        private static QueryWordMatch CreateQueryWordMatch(KeyValuePair<int, ImmutableList<IndexedWord>> match)
        {
            return new QueryWordMatch(match.Key, match.Value.Select(v => new FieldMatch(v)));
        }
    }
}
