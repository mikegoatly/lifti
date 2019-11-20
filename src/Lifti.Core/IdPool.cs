using System;
using System.Collections.Generic;

namespace Lifti
{
    public class IdPool<T> : IdLookup<T>, IIdPool<T>
    {
        private readonly Queue<int> reusableIds = new Queue<int>();
        private int nextId;

        public int Add(T item)
        {
            if (this.ItemIdIndex.ContainsKey(item))
            {
                throw new LiftiException(ExceptionMessages.ItemAlreadyIndexed);
            }

            var id = this.reusableIds.Count == 0 ? this.nextId++ : this.reusableIds.Dequeue();
            this.ItemIdIndex = this.ItemIdIndex.Add(item, id);
            this.ItemIdLookup = this.ItemIdLookup.Add(id, item);
            return id;
        }

        public int ReleaseItem(T item)
        {
            var id = this.ItemIdIndex[item];
            this.ItemIdIndex = this.ItemIdIndex.Remove(item);
            this.ItemIdLookup = this.ItemIdLookup.Remove(id);
            this.reusableIds.Enqueue(id);
            return id;
        }

        public void Add(int id, T item)
        {
            if (this.ItemIdIndex.ContainsKey(item))
            {
                throw new LiftiException(ExceptionMessages.ItemAlreadyIndexed);
            }

            if (this.ItemIdLookup.ContainsKey(id))
            {
                throw new LiftiException(ExceptionMessages.IdAlreadyUsed, id);
            }

            this.ItemIdIndex = this.ItemIdIndex.Add(item, id);
            this.ItemIdLookup = this.ItemIdLookup.Add(id, item);
            this.nextId = Math.Max(this.nextId, id + 1);
        }

    }
}
