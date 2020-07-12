using Lifti.Tokenization;
using System;

namespace Lifti
{
    internal class IndexMutation
    {
        private readonly IndexNodeMutation root;

        public IndexMutation(IndexNode root, IIndexNodeFactory indexNodeFactory)
        {
            this.root = new IndexNodeMutation(0, root, indexNodeFactory);
        }

        internal void Add(int itemId, byte fieldId, Token token)
        {
            if (token is null)
            {
                throw new ArgumentNullException(nameof(token));
            }

            this.root.Index(itemId, fieldId, token.Locations, token.Value.AsMemory());
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
