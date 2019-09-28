using System;
using System.Collections.Generic;
using System.Linq;

namespace Lifti.Querying
{
    public class IndexNavigator : IIndexNavigator
    {
        private IndexNode currentNode;
        private int intraNodeTextPosition;

        internal IndexNavigator(IndexNode node)
        {
            this.currentNode = node;
            this.intraNodeTextPosition = 0;
        }

        private bool HasIntraNodeTextLeftToProcess => this.currentNode.IntraNodeText != null &&
                    this.intraNodeTextPosition < this.currentNode.IntraNodeText.Length;

        public IntermediateQueryResult GetExactMatches()
        {
            if (this.currentNode == null || this.HasIntraNodeTextLeftToProcess)
            {
                return IntermediateQueryResult.Empty;
            }

            return new IntermediateQueryResult(this.GetCurrentNodeMatches());
        }

        private IEnumerable<QueryWordMatch> GetCurrentNodeMatches()
        {
            return this.currentNode.Matches?.Select(CreateQueryWordMatch) ??
                Array.Empty<QueryWordMatch>();
        }

        public IntermediateQueryResult GetExactAndChildMatches()
        {
            if (this.currentNode == null)
            {
                return IntermediateQueryResult.Empty;
            }

            var matches = new List<QueryWordMatch>();
            var childNodeStack = new Queue<IndexNode>();
            childNodeStack.Enqueue(this.currentNode);

            while (childNodeStack.Count > 0)
            {
                var node = childNodeStack.Dequeue();
                if (node.Matches != null)
                {
                    matches.AddRange(node.Matches.Select(CreateQueryWordMatch));
                }

                if (node.ChildNodes != null)
                {
                    foreach (var childNode in node.ChildNodes.Values)
                    {
                        childNodeStack.Enqueue(childNode);
                    }
                }
            }
            
            return new IntermediateQueryResult(matches);
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

            if (this.HasIntraNodeTextLeftToProcess)
            {
                if (value == this.currentNode.IntraNodeText[this.intraNodeTextPosition])
                {
                    this.intraNodeTextPosition++;
                    return true;
                }

                this.currentNode = null;
                return false;
            }

            if (this.currentNode.ChildNodes != null && this.currentNode.ChildNodes.TryGetValue(value, out var nextNode))
            {
                this.currentNode = nextNode;
                this.intraNodeTextPosition = 0;
                return true;
            }

            this.currentNode = null;
            return false;
        }

        private static QueryWordMatch CreateQueryWordMatch(KeyValuePair<int, List<IndexedWord>> match)
        {
            return new QueryWordMatch(match.Key, match.Value.Select(v => new FieldMatch(v)));
        }
    }
}
