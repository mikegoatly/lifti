using System.Collections.Generic;

namespace Lifti
{
    public interface IItemStore
    {
        /// <summary>
        /// Gets the number of items managed by this instance.
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Gets the item metadata for the given id.
        /// </summary>
        /// <exception cref="LiftiException">
        /// Thrown when the id is not known.
        /// </exception>
        IItemMetadata GetMetadata(int id);

        /// <summary>
        /// Gets the aggregated statistics for all the indexed documents, including total token count.
        /// </summary>
        IndexStatistics IndexStatistics { get; }
    }

    public interface IItemStore<T> : IItemStore
    {
        /// <summary>
        /// Gets each of the items and their associated ids managed by this instance.
        /// </summary>
        IEnumerable<ItemMetadata<T>> GetIndexedItems();

        /// <summary>
        /// Gets a value indicating whether the given item is managed by this instance.
        /// </summary>
        bool Contains(T item);

        /// <summary>
        /// Gets the item metadata for the given id.
        /// </summary>
        /// <exception cref="LiftiException">
        /// Thrown when the id is not known.
        /// </exception>
        IItemMetadata<T> GetMetadata(int id);

        /// <summary>
        /// Gets the item metadata for the given item.
        /// </summary>
        /// <exception cref="LiftiException">
        /// Thrown when the item is not known.
        /// </exception>
        IItemMetadata<T> GetMetadata(T item);

        /// <summary>
        /// Creates a snapshot of this instance that can be used even if the index is subsequently mutated.
        /// </summary>
        IItemStore<T> Snapshot();
    }
}