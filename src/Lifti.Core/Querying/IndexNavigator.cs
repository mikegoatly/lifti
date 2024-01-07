using Lifti.Querying.Lifti.Querying;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace Lifti.Querying
{
    internal sealed class IndexNavigator : IIndexNavigator
    {
        private readonly Queue<IndexNavigatorBookmark> bookmarkPool = new(10);
        private readonly StringBuilder navigatedWith = new(16);
        private IIndexNavigatorPool? pool;
        private IScorer? scorer;
        private IIndexSnapshot? snapshot;

        private IndexNode? currentNode;
        private int intraNodeTextPosition;
        private bool bookmarkApplied;

        internal void Initialize(IIndexSnapshot indexSnapshot, IIndexNavigatorPool pool, IScorer scorer)
        {
            this.pool = pool;
            this.scorer = scorer;
            this.snapshot = indexSnapshot;
            this.currentNode = indexSnapshot.Root;
            this.intraNodeTextPosition = 0;
            this.navigatedWith.Length = 0;
            this.bookmarkApplied = false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool HasIntraNodeTextLeftToProcess(int intraNodeTextPosition, IndexNode node) => intraNodeTextPosition < node.IntraNodeText.Length;

        public int ExactMatchCount()
        {
            return this.HasExactMatches ? this.currentNode!.Matches.Count : 0;
        }

        /// <inheritdoc />
        public IIndexSnapshot Snapshot
        {
            get
            {
                return this.snapshot ?? throw new LiftiException(ExceptionMessages.NoSnapshotInitialized);
            }
        }

        public bool HasExactMatches
        {
            get
            {
                if (this.currentNode == null || HasIntraNodeTextLeftToProcess(this.intraNodeTextPosition, this.currentNode) || !this.currentNode.HasMatches)
                {
                    return false;
                }

                return this.currentNode.HasMatches;
            }
        }

        public IntermediateQueryResult GetExactMatches(double weighting = 1D)
        {
            return this.GetExactMatches(QueryContext.Empty, weighting);
        }

        public IntermediateQueryResult GetExactMatches(QueryContext queryContext, double weighting = 1D)
        {
            if (this.currentNode == null || HasIntraNodeTextLeftToProcess(this.intraNodeTextPosition, this.currentNode) || !this.currentNode.HasMatches)
            {
                return IntermediateQueryResult.Empty;
            }

            var collector = new DocumentMatchCollector();

            this.AddExactMatches(this.currentNode, queryContext, collector, weighting);

            return collector.ToIntermediateQueryResult();
        }

        public void AddExactMatches(QueryContext queryContext, DocumentMatchCollector documentMatchCollector, double weighting = 1D)
        {
            if (this.currentNode == null || HasIntraNodeTextLeftToProcess(this.intraNodeTextPosition, this.currentNode) || !this.currentNode.HasMatches)
            {
                return;
            }

            this.AddExactMatches(this.currentNode, queryContext, documentMatchCollector, weighting);
        }

        public IntermediateQueryResult GetExactAndChildMatches(double weighting = 1D)
        {
            return this.GetExactAndChildMatches(QueryContext.Empty, weighting);
        }

        public IntermediateQueryResult GetExactAndChildMatches(QueryContext queryContext, double weighting = 1D)
        {
            if (this.currentNode == null)
            {
                return IntermediateQueryResult.Empty;
            }

            var collector = new DocumentMatchCollector();

            this.AddExactAndChildMatches(this.currentNode, queryContext, collector, weighting);

            return collector.ToIntermediateQueryResult();
        }

        public void AddExactAndChildMatches(QueryContext queryContext, DocumentMatchCollector documentMatchCollector, double weighting = 1)
        {
            if (this.currentNode != null)
            {
                this.AddExactAndChildMatches(this.currentNode, queryContext, documentMatchCollector, weighting);
            }
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

            if (HasIntraNodeTextLeftToProcess(this.intraNodeTextPosition, this.currentNode))
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
                if (HasIntraNodeTextLeftToProcess(this.intraNodeTextPosition, this.currentNode))
                {
                    yield return this.currentNode.IntraNodeText.Span[this.intraNodeTextPosition];
                }
                else if (this.currentNode.HasChildNodes)
                {
                    var childChars = this.currentNode.ChildNodes.CharacterMap;
                    for (var i = 0; i < childChars.Count; i++)
                    {
                        yield return childChars[i].ChildChar;
                    }
                }
            }
        }

        public IIndexNavigatorBookmark CreateBookmark()
        {
            var bookmark = this.GetCachedBookmarkOrCreate();
            bookmark.Capture();
            return bookmark;
        }

        private IndexNavigatorBookmark GetCachedBookmarkOrCreate()
        {
            return this.bookmarkPool.Count == 0
                ? new IndexNavigatorBookmark(this)
                : this.bookmarkPool.Dequeue();
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
                foreach (var (character, childNode) in node.ChildNodes.CharacterMap)
                {
                    this.navigatedWith.Append(character);
                    foreach (var result in this.EnumerateIndexedTokens(childNode))
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

        private void AddExactAndChildMatches(IndexNode startNode, QueryContext queryContext, DocumentMatchCollector documentMatchCollector, double weighting)
        {
            var childNodeStack = new Queue<IndexNode>();
            childNodeStack.Enqueue(startNode);

            while (childNodeStack.Count > 0)
            {
                var node = childNodeStack.Dequeue();
                if (node.HasMatches)
                {
                    AddExactMatches(node, queryContext, documentMatchCollector, weighting);
                }

                if (node.HasChildNodes)
                {
                    foreach (var (_, childNode) in node.ChildNodes.CharacterMap)
                    {
                        childNodeStack.Enqueue(childNode);
                    }
                }
            }
        }

        private void AddExactMatches(IndexNode node, QueryContext queryContext, DocumentMatchCollector documentMatchCollector, double weighting)
        {
            if (this.scorer == null)
            {
                throw new InvalidOperationException(ExceptionMessages.NoScorerInitialized);
            }

            var documentMatches = node.Matches.Enumerate();
            if (queryContext.FilterToDocumentIds != null)
            {
                documentMatches = documentMatches.Where(m => queryContext.FilterToDocumentIds.Contains(m.documentId));
            }

            var filterToFieldId = queryContext.FilterToFieldId;
            var matchedDocumentCount = node.Matches.Count;
            var scorer = this.scorer;
            foreach (var (documentId, indexedTokens) in documentMatches)
            {
                foreach (var indexedToken in indexedTokens)
                {
                    var fieldId = indexedToken.FieldId;
                    if (filterToFieldId.HasValue && filterToFieldId.GetValueOrDefault() != fieldId)
                    {
                        continue;
                    }

                    var score = scorer.CalculateScore(
                        matchedDocumentCount,
                        documentId,
                        fieldId,
                        indexedToken.Locations,
                        weighting);

                    documentMatchCollector.Add(documentId, fieldId, indexedToken.Locations, score);
                }
            }
        }

        internal sealed class IndexNavigatorBookmark : IIndexNavigatorBookmark, IEquatable<IndexNavigatorBookmark>
        {
            private readonly IndexNavigator indexNavigator;
            private IndexNode? currentNode;
            private int intraNodeTextPosition;
            private bool disposed;

            public IndexNavigatorBookmark(IndexNavigator indexNavigator)
            {
                this.indexNavigator = indexNavigator;
            }

            public void Capture()
            {
                this.currentNode = indexNavigator.currentNode;
                this.intraNodeTextPosition = indexNavigator.intraNodeTextPosition;
                this.disposed = false;
            }

            /// <inheritdoc />
            public void Apply()
            {
                if (this.disposed)
                {
                    throw new LiftiException(ExceptionMessages.BookmarkDisposed);
                }

                var indexNavigator = this.indexNavigator;
                indexNavigator.bookmarkApplied = true;
                indexNavigator.currentNode = this.currentNode;
                indexNavigator.intraNodeTextPosition = this.intraNodeTextPosition;
            }

            public void Dispose()
            {
                if (this.indexNavigator.bookmarkPool.Count < 10)
                {
                    this.indexNavigator.bookmarkPool.Enqueue(this);
                }

                this.disposed = true;
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
                return HashCode.Combine(this.currentNode, this.intraNodeTextPosition);
            }

            public bool Equals(IndexNavigatorBookmark? bookmark)
            {
                return bookmark != null &&
                       this.currentNode == bookmark.currentNode &&
                       this.intraNodeTextPosition == bookmark.intraNodeTextPosition;
            }
        }
    }
}
