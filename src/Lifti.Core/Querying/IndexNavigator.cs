using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Lifti.Querying
{
    internal sealed class IndexNavigator : IIndexNavigator
    {
        private readonly StringBuilder navigatedWith = new StringBuilder(16);
        private IIndexNavigatorPool? pool;
        private IScorer? scorer;
        private IndexNode? currentNode;
        private int intraNodeTextPosition;
        private bool bookmarkApplied;

        internal void Initialize(IndexNode node, IIndexNavigatorPool pool, IScorer scorer)
        {
            this.pool = pool;
            this.scorer = scorer;
            this.currentNode = node;
            this.intraNodeTextPosition = 0;
            this.navigatedWith.Length = 0;
            this.bookmarkApplied = false;
        }

        private bool HasIntraNodeTextLeftToProcess => this.currentNode != null && this.intraNodeTextPosition < this.currentNode.IntraNodeText.Length;

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

        public IntermediateQueryResult GetExactMatches(double weighting = 1D)
        {
            if (this.currentNode == null || this.HasIntraNodeTextLeftToProcess || !this.currentNode.HasMatches)
            {
                return IntermediateQueryResult.Empty;
            }

            var matches = this.currentNode.Matches.Select(CreateQueryTokenMatch);

            return CreateIntermediateQueryResult(matches, weighting);
        }

        public IntermediateQueryResult GetExactAndChildMatches(double weighting = 1D)
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

            var queryTokenMatches = matches.Select(m => new QueryTokenMatch(
                    m.Key,
                    MergeItemMatches(m.Value).ToList()));

            return CreateIntermediateQueryResult(queryTokenMatches, weighting);
        }

        public bool Process(string text)
        {
            return this.Process(text.AsSpan());
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

            if (this.bookmarkApplied == false)
            {
                this.navigatedWith.Append(value);
            }

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

        public IEnumerable<string> EnumerateIndexedTokens()
        {
            if (this.bookmarkApplied)
            {
                throw new LiftiException(ExceptionMessages.UnableToEnumerateIndexedTokensAfterApplyingBookmark);
            }

            if (this.currentNode == null)
            {
                return Enumerable.Empty<string>();
            }

            if (this.intraNodeTextPosition > 0)
            {
                this.navigatedWith.Length -= this.intraNodeTextPosition;
            }

            var results = this.EnumerateIndexedTokens(this.currentNode).ToList();

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

        public IEnumerable<char> EnumerateNextCharacters()
        {
            if (this.currentNode != null)
            {
                if (this.HasIntraNodeTextLeftToProcess)
                {
                    yield return this.currentNode.IntraNodeText.Span[this.intraNodeTextPosition];
                }
                else if (this.currentNode.HasChildNodes)
                {
                    foreach (var character in this.currentNode.ChildNodes.Keys)
                    {
                        yield return character;
                    }
                }
            }
        }

        public IIndexNavigatorBookmark CreateBookmark()
        {
            return new IndexNavigatorBookmark(this);
        }

        public void Dispose()
        {
            if (this.pool == null)
            {
                Debug.Fail("No pool available when disposing IndexNavigator");
                return;
            }

            this.pool.Return(this);
        }

        private IntermediateQueryResult CreateIntermediateQueryResult(IEnumerable<QueryTokenMatch> matches, double weighting)
        {
            if (this.scorer == null)
            {
                throw new InvalidOperationException(ExceptionMessages.NoScorerInitialized);
            }

            var matchList = matches as IReadOnlyList<QueryTokenMatch> ?? matches.ToList();
            var scoredMatches = this.scorer.Score(matchList, weighting);
            return new IntermediateQueryResult(scoredMatches);
        }

        private IEnumerable<string> EnumerateIndexedTokens(IndexNode node)
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
                    foreach (var result in this.EnumerateIndexedTokens(childNode.Value))
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

        private static IEnumerable<FieldMatch> MergeItemMatches(List<FieldMatch> fieldMatches)
        {
            return fieldMatches.ToLookup(m => m.FieldId)
                .Select(m => new FieldMatch(
                    m.Key,
                    m.SelectMany(w => w.Locations)));
        }

        private static QueryTokenMatch CreateQueryTokenMatch(
            KeyValuePair<int, ImmutableList<IndexedToken>> match)
        {
            return new QueryTokenMatch(
                match.Key,
                match.Value.Select(v => new FieldMatch(v)).ToList());
        }

        internal readonly struct IndexNavigatorBookmark : IIndexNavigatorBookmark, IEquatable<IndexNavigatorBookmark>
        {
            private readonly IndexNavigator indexNavigator;
            private readonly IndexNode? currentNode;
            private readonly int intraNodeTextPosition;

            public IndexNavigatorBookmark(IndexNavigator indexNavigator)
            {
                this.currentNode = indexNavigator.currentNode;
                this.intraNodeTextPosition = indexNavigator.intraNodeTextPosition;
                this.indexNavigator = indexNavigator;
            }

            /// <inheritdoc />
            public void Apply()
            {
                this.indexNavigator.bookmarkApplied = true;
                this.indexNavigator.currentNode = this.currentNode;
                this.indexNavigator.intraNodeTextPosition = this.intraNodeTextPosition;
            }

            public override bool Equals(object? obj)
            {
                if (obj is IndexNavigatorBookmark other)
                {
                    return this.Equals(other);
                }

                return false;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(this.indexNavigator, this.currentNode, this.intraNodeTextPosition);
            }

            public bool Equals(IndexNavigatorBookmark bookmark)
            {
                return this.indexNavigator == bookmark.indexNavigator &&
                       this.currentNode == bookmark.currentNode &&
                       this.intraNodeTextPosition == bookmark.intraNodeTextPosition;
            }
        }
    }
}
