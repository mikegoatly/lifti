using System;
using System.Collections.Generic;
using System.Linq;

namespace Lifti.Querying
{
    public struct IndexNavigator : IIndexNavigator
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
                return new IntermediateQueryResult(Array.Empty<(int, IEnumerable<IndexedWordLocation>)>());
            }

            return new IntermediateQueryResult(
                this.currentNode.Matches.Select(m => (m.Key, (IEnumerable<IndexedWordLocation>)m.Value)));
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

        public bool Process(char next)
        {
            if (this.currentNode == null)
            {
                return false;
            }

            if (this.HasIntraNodeTextLeftToProcess)
            {
                if (next == this.currentNode.IntraNodeText[this.intraNodeTextPosition])
                {
                    this.intraNodeTextPosition++;
                    return true;
                }

                this.currentNode = null;
                return false;
            }

            if (this.currentNode.ChildNodes != null && this.currentNode.ChildNodes.TryGetValue(next, out var nextNode))
            {
                this.currentNode = nextNode;
                this.intraNodeTextPosition = 0;
                return true;
            }

            return false;
        }
    }
}
