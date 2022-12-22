using System;
using System.Collections.Generic;

namespace Lifti
{
    /// <summary>
    /// Extends <see cref="ItemStore{T}"/> by adding additional methods for controlling
    /// the addition and removal of items, caching and reusing the item ids.
    /// </summary>
    public class IdPool<T> : ItemStore<T>, IIdPool<T>
        where T : notnull
    {
        private readonly Queue<int> reusableIds = new Queue<int>();
        private int nextId;

        /// <inheritdoc />
        public int Add(T item, DocumentStatistics documentStatistics)
        {
            if (this.ItemLookup.ContainsKey(item))
            {
                throw new LiftiException(ExceptionMessages.ItemAlreadyIndexed);
            }

            var id = this.reusableIds.Count == 0 ? this.nextId++ : this.reusableIds.Dequeue();
            this.Add(id, item, new ItemMetadata<T>(id, item, documentStatistics));
            return id;
        }

        /// <inheritdoc />
        public int ReleaseItem(T item)
        {
            var itemMetadata = this.ItemLookup[item];
            var id = itemMetadata.Id;

            this.ItemLookup = this.ItemLookup.Remove(item);
            this.ItemIdLookup = this.ItemIdLookup.Remove(id);
            this.IndexStatistics = this.IndexStatistics.Remove(itemMetadata.DocumentStatistics);

            this.reusableIds.Enqueue(id);
            return id;
        }

        /// <inheritdoc />
        public void Add(int id, T item, DocumentStatistics documentStatistics)
        {
            if (this.ItemLookup.ContainsKey(item))
            {
                throw new LiftiException(ExceptionMessages.ItemAlreadyIndexed);
            }

            if (this.ItemIdLookup.ContainsKey(id))
            {
                throw new LiftiException(ExceptionMessages.IdAlreadyUsed, id);
            }

            this.Add(id, item, new ItemMetadata<T>(id, item, documentStatistics));
            this.nextId = Math.Max(this.nextId, id + 1);
        }

        private void Add(int id, T item, ItemMetadata<T> itemMetadata)
        {
            this.ItemLookup = this.ItemLookup.Add(item, itemMetadata);
            this.ItemIdLookup = this.ItemIdLookup.Add(id, itemMetadata);
            this.IndexStatistics = this.IndexStatistics.Add(itemMetadata.DocumentStatistics);
        }
    }
}
