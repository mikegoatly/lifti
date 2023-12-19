using Lifti.Tokenization.Objects;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Lifti
{
    /// <inheritdoc />
    internal class ItemStore<TKey> : IItemStore<TKey>
        where TKey : notnull
    {
        private readonly Dictionary<byte, ScoreBoostMetadata> scoreBoostMetadata;
        private readonly IdPool<TKey> idPool;

        internal ItemStore(IEnumerable<IIndexedObjectConfiguration> configureObjectTypes)
        {
            this.idPool = new IdPool<TKey>();
            this.ItemLookup = ImmutableDictionary<TKey, ItemMetadata<TKey>>.Empty;
            this.ItemIdLookup = ImmutableDictionary<int, ItemMetadata<TKey>>.Empty;
            this.IndexStatistics = IndexStatistics.Empty;
            this.scoreBoostMetadata = configureObjectTypes.ToDictionary(o => o.Id, o => new ScoreBoostMetadata(o.ScoreBoostOptions));
        }

        private ItemStore(ItemStore<TKey> original)
        {
            this.idPool = original.idPool;
            this.ItemLookup = original.ItemLookup;
            this.ItemIdLookup = original.ItemIdLookup;
            this.IndexStatistics = original.IndexStatistics;
            this.scoreBoostMetadata = original.scoreBoostMetadata;
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
                id => ItemMetadata<TKey>.ForLooseText(id, key, documentStatistics));
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
                itemId =>
                {
                    var itemMetadata = ItemMetadata<TKey>.ForObject(
                        objectTypeId: objectConfiguration.Id,
                        itemId: itemId,
                        key,
                        documentStatistics,
                        freshnessDate,
                        scoringMagnitude);

                    this.GetObjectTypeScoreBoostMetadata(objectConfiguration.Id)
                        .Add(itemMetadata);

                    return itemMetadata;
                });
        }

        /// <inheritdoc />
        public void Add(ItemMetadata<TKey> itemMetadata)
        {
            // Make the ID pool aware of the ID we are using
            this.idPool.RegisterUsedId(itemMetadata.Id);

            if (itemMetadata.ObjectTypeId is byte objectTypeId)
            {
                // Add the item to the score boost metadata
                this.GetObjectTypeScoreBoostMetadata(objectTypeId)
                    .Add(itemMetadata);
            }

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

            if (itemInfo.ObjectTypeId is byte objectTypeId)
            {
                // Remove the item from the score boost metadata
                this.GetObjectTypeScoreBoostMetadata(objectTypeId)
                    .Remove(itemInfo);
            }

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
            return new ItemStore<TKey>(this);
        }

        /// <inheritdoc />
        public ScoreBoostMetadata GetObjectTypeScoreBoostMetadata(byte objectTypeId)
        {
            if (!this.scoreBoostMetadata.TryGetValue(objectTypeId, out var scoreBoostMetadata))
            {
                throw new LiftiException(ExceptionMessages.UnknownObjectTypeId, objectTypeId);
            }

            return scoreBoostMetadata;
        }

        /// <inheritdoc />
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
