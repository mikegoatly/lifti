using Lifti.Querying;

namespace Lifti
{
    public class IndexSnapshot<TKey> : IIndexSnapshot<TKey>
    {
        private readonly IIndexNavigatorPool indexNavigatorPool;

        internal IndexSnapshot(IIndexNavigatorPool indexNavigatorPool, FullTextIndex<TKey> index)
        {
            this.Items = index.Items.Snapshot();
            this.Root = index.Root;
            this.indexNavigatorPool = indexNavigatorPool;

            // Field lookup is read-only once the index is constructed
            this.FieldLookup = index.FieldLookup;
        }

        /// <inheritdoc />
        public IItemStore<TKey> Items { get; }

        /// <inheritdoc />
        public IndexNode Root { get; }

        /// <inheritdoc />
        public IIndexedFieldLookup FieldLookup { get; }

        /// <inheritdoc />
        public IIndexNavigator CreateNavigator()
        {
            return this.indexNavigatorPool.Create(this.Root);
        }
    }
}
