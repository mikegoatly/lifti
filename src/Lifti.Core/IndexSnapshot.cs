using Lifti.Querying;
using System;

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
            IIndexMetadata<TKey> indexMetadata)
        {
            this.Metadata = indexMetadata;
            this.Root = rootNode;
            this.indexNavigatorPool = indexNavigatorPool;
            this.FieldLookup = fieldLookup;
        }

        /// <inheritdoc />
        [Obsolete("Use Metadata property instead")]
        public IIndexMetadata<TKey> Items => this.Metadata;

        /// <inheritdoc />
        public IIndexMetadata<TKey> Metadata { get; }

        /// <inheritdoc />
        public IndexNode Root { get; }

        /// <inheritdoc />
        public IIndexedFieldLookup FieldLookup { get; }

        IIndexMetadata IIndexSnapshot.Metadata => this.Metadata;

        IIndexMetadata IIndexSnapshot.Items => this.Metadata;

        /// <inheritdoc />
        public IIndexNavigator CreateNavigator()
        {
            return this.indexNavigatorPool.Create(this);
        }
    }
}
