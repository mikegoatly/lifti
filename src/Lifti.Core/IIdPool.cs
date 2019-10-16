using System.Collections.Generic;

namespace Lifti
{
    public interface IIdPool<T>
    {
        int AllocatedIdCount { get; }

        IEnumerable<(T item, int itemId)> GetIndexedItems();

        int CreateIdFor(T item);
        T GetItemForId(int id);

        /// <summary>
        /// Returns the id associated to the given item back to the pool.
        /// </summary>
        /// <returns>
        /// The id that was associated to the item.
        /// </returns>
        int ReleaseItem(T item);
        void Add(int id, T item);
        bool Contains(T item);
    }
}