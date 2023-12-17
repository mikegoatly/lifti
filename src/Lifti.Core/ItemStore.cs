using Lifti.Tokenization.Objects;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Lifti
{
    /// <inheritdoc />
    internal class ItemStore<TKey> : IItemStore<TKey>
        where TKey : notnull
    {
        private readonly IdPool<TKey> idPool;

        internal ItemStore()
            : this(
                new IdPool<TKey>(),
                ImmutableDictionary<TKey, ItemMetadata<TKey>>.Empty,
                ImmutableDictionary<int, ItemMetadata<TKey>>.Empty,
                IndexStatistics.Empty)
        {
        }

        private ItemStore(
            IdPool<TKey> idPool,
            ImmutableDictionary<TKey, ItemMetadata<TKey>> itemLookup,
            ImmutableDictionary<int, ItemMetadata<TKey>> itemIdLookup,
            IndexStatistics indexStatistics)
        {
            this.idPool = idPool;
            this.ItemLookup = itemLookup;
            this.ItemIdLookup = itemIdLookup;
            this.IndexStatistics = indexStatistics;
        }

        /// <inheritdoc />
        public int Count => this.ItemLookup.Count;

        /// <inheritdoc />
        public IndexStatistics IndexStatistics { get; protected set; } = IndexStatistics.Empty;

        /// <summary>
        /// Gets or sets the lookup of item key to <see cref="ItemMetadata{T}"/> information.
        /// </summary>
        protected ImmutableDictionary<TKey, ItemMetadata<TKey>> ItemLookup { get; set; }

        /// <summary>
        /// Gets or sets the lookup of internal item id to <see cref="ItemMetadata{T}"/> information.
        /// </summary>
        protected ImmutableDictionary<int, ItemMetadata<TKey>> ItemIdLookup { get; set; }

        /// <inheritdoc />\
        public IEnumerable<ItemMetadata<TKey>> GetIndexedItems()
        {
            return this.ItemLookup.Values;
        }

        /// <summary>
        /// Adds the given item key to the item store. Used when indexing loose text not associated with an object.
        /// </summary>
        /// <param name="key">
        /// The item to add.
        /// </param>
        /// <param name="documentStatistics">
        /// The document statistics for the item.
        /// </param>
        /// <returns>
        /// The internal id of the item that was added.
        /// </returns>
        public int Add(TKey key, DocumentStatistics documentStatistics)
        {
            return this.Add(
                id => new ItemMetadata<TKey>(id, key, documentStatistics, null, null));
        }

        /// <summary>
        /// Adds the given key and item to the item store. Used when indexing objects.
        /// </summary>
        /// <inheritdoc cref="Add(TKey, DocumentStatistics)" />
        public int Add<TItem>(TKey key, TItem item, DocumentStatistics documentStatistics, IndexedObjectConfiguration<TItem, TKey> objectConfiguration)
        {
            // Get the score boosts for the item
            var scoreBoostOptions = objectConfiguration.ScoreBoostOptions;
            var freshnessDate = scoreBoostOptions.FreshnessProvider?.Invoke(item);
            var scoringMagnitude = scoreBoostOptions.MagnitudeProvider?.Invoke(item);

            return this.Add(
                id => new ItemMetadata<TKey>(id, key, documentStatistics, freshnessDate, scoringMagnitude));
        }

        /// <inheritdoc />
        public void Add(ItemMetadata<TKey> itemMetadata)
        {
            // Make the ID pool aware of the ID we are using
            this.idPool.RegisterUsedId(itemMetadata.Id);

            this.UpdateLookups(itemMetadata);
        }

        /// <summary>
        /// Removes the given item from the item store.
        /// </summary>
        /// <returns>
        /// The internal id of the item that was removed.
        /// </returns>
        public int Remove(TKey key)
        {
            var itemInfo = this.ItemLookup[key];
            var id = itemInfo.Id;
            this.ItemLookup = this.ItemLookup.Remove(key);
            this.ItemIdLookup = this.ItemIdLookup.Remove(id);
            this.IndexStatistics = this.IndexStatistics.Remove(itemInfo.DocumentStatistics);

            this.idPool.Return(id);

            return id;
        }

        /// <inheritdoc />
        public ItemMetadata<TKey> GetMetadata(int id)
        {
            if (!this.ItemIdLookup.TryGetValue(id, out var itemMetadata))
            {
                throw new LiftiException(ExceptionMessages.ItemNotFound);
            }

            return itemMetadata;
        }

        /// <inheritdoc />
        public ItemMetadata<TKey> GetMetadata(TKey key)
        {
            if (!this.ItemLookup.TryGetValue(key, out var itemMetadata))
            {
                throw new LiftiException(ExceptionMessages.ItemNotFound);
            }

            return itemMetadata;
        }

        /// <inheritdoc />
        public bool Contains(TKey key)
        {
            return this.ItemLookup.ContainsKey(key);
        }

        /// <inheritdoc />
        public IItemStore<TKey> Snapshot()
        {
            return new ItemStore<TKey>(
                this.idPool,
                this.ItemLookup,
                this.ItemIdLookup,
                this.IndexStatistics
            );
        }

        ItemMetadata IItemStore.GetMetadata(int id)
        {
            return this.GetMetadata(id);
        }

        private int Add(Func<int, ItemMetadata<TKey>> createItemMetadata)
        {
            var id = this.idPool.Next();
            var itemMetadata = createItemMetadata(id);

            this.Add(itemMetadata);

            return id;
        }

        private void UpdateLookups(ItemMetadata<TKey> itemMetadata)
        {
            var key = itemMetadata.Key;
            var id = itemMetadata.Id;
            if (this.ItemLookup.ContainsKey(key))
            {
                throw new LiftiException(ExceptionMessages.ItemAlreadyIndexed);
            }

            if (this.ItemIdLookup.ContainsKey(id))
            {
                throw new LiftiException(ExceptionMessages.IdAlreadyUsed, id);
            }

            this.ItemLookup = this.ItemLookup.Add(key, itemMetadata);
            this.ItemIdLookup = this.ItemIdLookup.Add(id, itemMetadata);
            this.IndexStatistics = this.IndexStatistics.Add(itemMetadata.DocumentStatistics);
        }
    }
}
