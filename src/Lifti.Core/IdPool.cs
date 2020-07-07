using System;
using System.Collections.Generic;

namespace Lifti
{
    public class IdPool<T> : ItemStore<T>, IIdPool<T>
    {
        private readonly Queue<int> reusableIds = new Queue<int>();
        private int nextId;

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
