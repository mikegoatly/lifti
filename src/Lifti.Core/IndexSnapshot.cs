using Lifti.Querying;

namespace Lifti
{
    public class IndexSnapshot<TKey> : IIndexSnapshot<TKey>
    {
        private readonly IIndexNavigatorPool indexNavigatorPool;

        internal IndexSnapshot(IIndexNavigatorPool indexNavigatorPool, FullTextIndex<TKey> index)
        {
            this.IdLookup = index.IdLookup.Snapshot();
            this.Root = index.Root;
            this.indexNavigatorPool = indexNavigatorPool;

            // Field lookup is read-only once the index is constructed
            this.FieldLookup = index.FieldLookup;
        }

        public IIdLookup<TKey> IdLookup { get; }
        public IndexNode Root { get; }

        public IIndexedFieldLookup FieldLookup { get; }

        public IIndexNavigator CreateNavigator()
        {
            return this.indexNavigatorPool.Create(this.Root);
        }
    }
}
