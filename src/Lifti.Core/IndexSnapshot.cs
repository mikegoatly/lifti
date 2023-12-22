using Lifti.Querying;

namespace Lifti
{
    /// <inheritdoc />
    public class IndexSnapshot<TKey> : IIndexSnapshot<TKey>, IIndexSnapshot
        where TKey : notnull
    {
        private readonly IIndexNavigatorPool indexNavigatorPool;

        internal IndexSnapshot(
            IIndexNavigatorPool indexNavigatorPool,
            IIndexedFieldLookup fieldLookup,
            IndexNode rootNode,
            IItemStore<TKey> itemStore)
        {
            this.Items = itemStore;
            this.Root = rootNode;
            this.indexNavigatorPool = indexNavigatorPool;
            this.FieldLookup = fieldLookup;
        }

        /// <inheritdoc />
        public IItemStore<TKey> Items { get; }

        /// <inheritdoc />
        public IndexNode Root { get; }

        /// <inheritdoc />
        public IIndexedFieldLookup FieldLookup { get; }

        IItemStore IIndexSnapshot.Items => this.Items;

        /// <inheritdoc />
        public IIndexNavigator CreateNavigator()
        {
            return this.indexNavigatorPool.Create(this);
        }
    }
}
