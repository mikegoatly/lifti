using Lifti.Tokenization;
using System;

namespace Lifti
{
    internal class IndexMutation<TKey>
        where TKey : notnull
    {
        private readonly IndexNodeMutation root;

        public IndexMutation(
            IndexNode root,
            ItemStore<TKey> originalItemStore,
            IIndexNodeFactory indexNodeFactory)
        {
            this.root = new IndexNodeMutation(0, root, indexNodeFactory);
            this.ItemStore = new(originalItemStore);
        }

        public ItemStore<TKey> ItemStore { get; }

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
