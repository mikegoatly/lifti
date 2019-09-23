using System;
using System.Collections.Generic;
using System.Linq;

namespace Lifti
{
    public struct IndexNavigator
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

        public IEnumerable<(int itemId, IReadOnlyList<IndexedWordLocation> indexedWordLocations)> GetExactMatches()
        {
            if (this.currentNode == null || this.HasIntraNodeTextLeftToProcess)
            {
                return Array.Empty<(int, IReadOnlyList<IndexedWordLocation>)>();
            }

            return this.currentNode.Matches.Select(m => (m.Key, (IReadOnlyList<IndexedWordLocation>)m.Value));
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
