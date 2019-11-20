using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Lifti
{
    public class IdLookup<T> : IIdLookup<T>
    {
        public int Count => this.ItemIdIndex.Count;

        protected ImmutableDictionary<T, int> ItemIdIndex { get; set; } = ImmutableDictionary<T, int>.Empty;

        protected ImmutableDictionary<int, T> ItemIdLookup { get; set; } = ImmutableDictionary<int, T>.Empty;

        public IEnumerable<(T item, int itemId)> GetIndexedItems()
        {
            return this.ItemIdIndex.Select(p => (p.Key, p.Value));
        }

        public T GetItemForId(int id)
        {
            if (!this.ItemIdLookup.TryGetValue(id, out var item))
            {
                throw new LiftiException(ExceptionMessages.ItemNotFound);
            }

            return item;
        }

        public bool Contains(T item)
        {
            return this.ItemIdIndex.ContainsKey(item);
        }

        public IIdLookup<T> Snapshot()
        {
            return new IdLookup<T>
            {
                ItemIdIndex = this.ItemIdIndex,
                ItemIdLookup = this.ItemIdLookup
            };
        }
    }
}
