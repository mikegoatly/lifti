using Lifti.Tokenization.Objects;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Lifti
{
    /// <inheritdoc />
    internal sealed class ItemStore<TKey> : IItemStore<TKey>
        where TKey : notnull
    {
        private readonly Dictionary<byte, ScoreBoostMetadata> scoreBoostMetadata;
        private readonly IdPool<TKey> idPool;

        internal ItemStore(IEnumerable<IIndexedObjectConfiguration> configureObjectTypes)
        {
            this.idPool = new IdPool<TKey>();
            this.ItemLookup = [];
            this.ItemIdLookup = [];
            this.IndexStatistics = new();
            this.scoreBoostMetadata = configureObjectTypes.ToDictionary(o => o.Id, o => new ScoreBoostMetadata(o.ScoreBoostOptions));
        }

        /// <summary>
        /// Creates a new <see cref="ItemStore{TKey}"/> instance that is a copy of the given instance and is safe to mutate.
        /// </summary>
        /// <param name="original"></param>
        internal ItemStore(ItemStore<TKey> original)
        {
            this.idPool = original.idPool;
            this.ItemLookup = new(original.ItemLookup);
            this.ItemIdLookup = new(original.ItemIdLookup);
            this.IndexStatistics = new(original.IndexStatistics);
            this.scoreBoostMetadata = original.scoreBoostMetadata;
        }

        /// <inheritdoc />
        public int Count => this.ItemLookup.Count;

        /// <inheritdoc />
        public IndexStatistics IndexStatistics { get; }

        /// <summary>
        /// Gets or sets the lookup of item key to <see cref="ItemMetadata{T}"/> information.
        /// </summary>
        private Dictionary<TKey, ItemMetadata<TKey>> ItemLookup { get; set; }

        /// <summary>
        /// Gets or sets the lookup of internal item id to <see cref="ItemMetadata{T}"/> information.
        /// </summary>
        private Dictionary<int, ItemMetadata<TKey>> ItemIdLookup { get; set; }

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
        /// Tries to get the internal id for the given key.
        /// </summary>
        public bool TryGetDocumentId(TKey key, out int documentId)
        {
            if (this.ItemLookup.TryGetValue(key, out var itemMetadata))
            {
                documentId = itemMetadata.Id;
                return true;
            }

            documentId = -1;
            return false;
        }

        /// <summary>
        /// Removes the given item from the item store.
        /// </summary>
        /// <returns>
        /// The internal id of the item that was removed.
        /// </returns>
        public int Remove(TKey key)
        {
            var documentInfo = this.ItemLookup[key];
            var documentId = documentInfo.Id;
            this.ItemLookup.Remove(key);
            this.ItemIdLookup.Remove(documentId);
            this.IndexStatistics.Remove(documentInfo.DocumentStatistics);

            if (documentInfo.ObjectTypeId is byte objectTypeId)
            {
                // Remove the item from the score boost metadata
                this.GetObjectTypeScoreBoostMetadata(objectTypeId)
                    .Remove(documentInfo);
            }

            this.idPool.Return(documentId);

            return documentId;
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

            this.ItemLookup.Add(key, itemMetadata);
            this.ItemIdLookup.Add(id, itemMetadata);
            this.IndexStatistics.Add(itemMetadata.DocumentStatistics);
        }
    }
}
