using System;
using System.Collections.Generic;
using System.Linq;

namespace Lifti
{
    public class IdPool<T> : IIdPool<T>
    {
        private readonly Queue<int> reusableIds = new Queue<int>();
        private readonly Dictionary<T, int> itemIdIndex = new Dictionary<T, int>();
        private readonly Dictionary<int, T> itemIdLookup = new Dictionary<int, T>();
        private int nextId;

        public int Count => this.itemIdIndex.Count;

        public IEnumerable<(T item, int itemId)> GetIndexedItems()
        {
            return this.itemIdIndex.Select(p => (p.Key, p.Value));
        }

        public int Add(T item)
        {
            if (this.itemIdIndex.ContainsKey(item))
            {
                throw new LiftiException(ExceptionMessages.ItemAlreadyIndexed);
            }

            var id = reusableIds.Count == 0 ? nextId++ : reusableIds.Dequeue();
            itemIdIndex[item] = id;
            itemIdLookup[id] = item;
            return id;
        }

        public T GetItemForId(int id)
        {
            if (!this.itemIdLookup.TryGetValue(id, out var item))
            {
                throw new LiftiException(ExceptionMessages.ItemNotFound);
            }

            return item;
        }

        public int ReleaseItem(T item)
        {
            var id = this.itemIdIndex[item];
            this.itemIdIndex.Remove(item);
            this.itemIdLookup.Remove(id);
            this.reusableIds.Enqueue(id);
            return id;
        }

        public void Add(int id, T item)
        {
            if (this.itemIdIndex.ContainsKey(item))
            {
                throw new LiftiException(ExceptionMessages.ItemAlreadyIndexed);
            }

            if (this.itemIdLookup.ContainsKey(id))
            {
                throw new LiftiException(ExceptionMessages.IdAlreadyUsed, id);
            }

            itemIdIndex[item] = id;
            itemIdLookup[id] = item;
            this.nextId = Math.Max(this.nextId, id + 1);
        }

        public bool Contains(T item)
        {
            return this.itemIdIndex.ContainsKey(item);
        }
    }
}
