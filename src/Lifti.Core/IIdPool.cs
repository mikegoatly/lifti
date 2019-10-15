using System.Collections.Generic;

namespace Lifti
{
    public interface IIdPool<T>
    {
        int AllocatedIdCount { get; }

        IEnumerable<(T item, int itemId)> GetIndexedItems();

        int CreateIdFor(T item);
        T GetItemForId(int id);
        int ReleaseItem(T item);
        void Add(int id, T item);
    }
}