using Lifti.Tokenization;
using System;
using System.Diagnostics;
using System.Linq;

namespace Lifti
{
    internal class IndexMutation
    {
        private readonly IndexNodeMutation root;

        public IndexMutation(IndexNode root, IIndexNodeFactory indexNodeFactory)
        {
            this.root = new IndexNodeMutation(0, root, indexNodeFactory);
        }

        internal void Add(int itemId, byte fieldId, Token word)
        {
            if (word is null)
            {
                throw new ArgumentNullException(nameof(word));
            }

            Debug.Assert(word.Locations.Select((l, i) => i == 0 || l.WordIndex > word.Locations[i - 1].WordIndex).All(v => v));

            this.root.Index(itemId, fieldId, word.Locations, word.Value.AsMemory());
        }

        internal void Remove(int itemId)
        {
            this.root.Remove(itemId);
        }

        public IndexNode Apply()
        {
            return this.root.Apply();
        }

        public override string ToString()
        {
            return this.root.ToString();
        }
    }
}
