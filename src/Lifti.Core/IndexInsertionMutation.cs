using Lifti.Tokenization;
using System;
using System.Diagnostics;
using System.Linq;

namespace Lifti
{
    internal class IndexInsertionMutation : IndexMutation
    {
        public IndexInsertionMutation(IndexNode root, IIndexNodeFactory indexNodeFactory)
            : base(root, indexNodeFactory)
        {
        }

        internal void Index(int itemId, byte fieldId, Token word)
        {
            if (word is null)
            {
                throw new ArgumentNullException(nameof(word));
            }

            Debug.Assert(word.Locations.Select((l, i) => i == 0 || l.WordIndex > word.Locations[i - 1].WordIndex).All(v => v));

            this.Root.Index(itemId, fieldId, word.Locations, word.Value.AsMemory(), this);
        }

        public override string ToString()
        {
            return this.Root.ToString();
        }
    }
}
