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
            IndexMetadata<TKey> originalMetadata,
            IIndexNodeFactory indexNodeFactory)
        {
            this.root = new IndexNodeMutation(0, root, indexNodeFactory);
            this.Metadata = new(originalMetadata);
        }

        /// <summary>
        /// A mutating copy of the index metadata.
        /// </summary>
        public IndexMetadata<TKey> Metadata { get; }

        internal void Add(int documentId, byte fieldId, Token token)
        {
            if (token is null)
            {
                throw new ArgumentNullException(nameof(token));
            }

            this.root.Index(documentId, fieldId, token.Locations, token.Value.AsMemory());
        }

        internal void Remove(int documentId)
        {
            this.root.Remove(documentId);
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
