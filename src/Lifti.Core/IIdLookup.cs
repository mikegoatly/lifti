using System.Collections.Generic;

namespace Lifti
{
    public interface IIdLookup<T>
    {
        /// <summary>
        /// Gets the number of items managed by this instance.
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Gets each of the items and their associated ids managed by this instance.
        /// </summary>
        IEnumerable<(T item, int itemId)> GetIndexedItems();

        /// <summary>
        /// Gets a value indicating whether the given item is managed by this instance.
        /// </summary>
        bool Contains(T item);

        /// <summary>
        /// Gets the item for the given id.
        /// </summary>
        /// <exception cref="LiftiException">
        /// Thrown when the id is not known.
        /// </exception>
        T GetItemForId(int id);

        /// <summary>
        /// Creates a snapshot of this lookup that can be used even if the index is subsequently mutated.
        /// </summary>
        IIdLookup<T> Snapshot();
    }
}