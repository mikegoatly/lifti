using System.Collections.Generic;

namespace Lifti
{
    public class IdPool<T> : IIdPool<T>
    {
        private readonly Queue<int> reusableIds = new Queue<int>();
        private readonly Dictionary<T, int> itemIdIndex = new Dictionary<T, int>();
        private readonly Dictionary<int, T> itemIdLookup = new Dictionary<int, T>();
        private int nextId;

        public int CreateIdFor(T item)
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
    }
}
