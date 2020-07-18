using System.Collections.Generic;
using System.Collections.Immutable;

namespace Lifti
{
    /// <inheritdoc />
    public class ItemStore<T> : IItemStore<T>
    {
        internal ItemStore()
            : this(
                ImmutableDictionary<T, ItemMetadata<T>>.Empty,
                ImmutableDictionary<int, ItemMetadata<T>>.Empty,
                IndexStatistics.Empty)
        { 
        }

        private ItemStore(
            ImmutableDictionary<T, ItemMetadata<T>> itemLookup, 
            ImmutableDictionary<int, ItemMetadata<T>> itemIdLookup, 
            IndexStatistics indexStatistics)
        {
            this.ItemLookup = itemLookup;
            this.ItemIdLookup = itemIdLookup;
            this.IndexStatistics = indexStatistics;
        }

        /// <inheritdoc />
        public int Count => this.ItemLookup.Count;

        /// <inheritdoc />
        public IndexStatistics IndexStatistics { get; protected set; } = IndexStatistics.Empty;

        protected ImmutableDictionary<T, ItemMetadata<T>> ItemLookup { get; set; }

        protected ImmutableDictionary<int, ItemMetadata<T>> ItemIdLookup { get; set; }

        /// <inheritdoc />\
        public IEnumerable<ItemMetadata<T>> GetIndexedItems()
        {
            return this.ItemLookup.Values;
        }

        /// <inheritdoc />
        public IItemMetadata<T> GetMetadata(int id)
        {
            if (!this.ItemIdLookup.TryGetValue(id, out var itemMetadata))
            {
                throw new LiftiException(ExceptionMessages.ItemNotFound);
            }

            return itemMetadata;
        }

        /// <inheritdoc />
        public IItemMetadata<T> GetMetadata(T item)
        {
            if (!this.ItemLookup.TryGetValue(item, out var itemMetadata))
            {
                throw new LiftiException(ExceptionMessages.ItemNotFound);
            }

            return itemMetadata;
        }

        /// <inheritdoc />
        public bool Contains(T item)
        {
            return this.ItemLookup.ContainsKey(item);
        }

        /// <inheritdoc />
        public IItemStore<T> Snapshot()
        {
            return new ItemStore<T>(this.ItemLookup,
                this.ItemIdLookup,
                this.IndexStatistics
            );
        }

        IItemMetadata IItemStore.GetMetadata(int id)
        {
            return this.GetMetadata(id);
        }
    }
}
