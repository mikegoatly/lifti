using System.Collections.Generic;

namespace Lifti
{
    /// <summary>
    /// Describes methods for accessing information about items stored in an index.
    /// </summary>
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

    /// <summary>
    /// Describes methods for accessing information about items stored in an index.
    /// </summary>
    /// <typeparam name="TKey">
    /// The type of the key in the index.
    /// </typeparam>
    public interface IItemStore<TKey> : IItemStore
    {
        /// <summary>
        /// Gets each of the items and their associated ids managed by this instance.
        /// </summary>
        IEnumerable<ItemMetadata<TKey>> GetIndexedItems();

        /// <summary>
        /// Gets a value indicating whether the given item is managed by this instance.
        /// </summary>
        bool Contains(TKey item);

        /// <summary>
        /// Gets the item metadata for the given id.
        /// </summary>
        /// <exception cref="LiftiException">
        /// Thrown when the id is not known.
        /// </exception>
        IItemMetadata<TKey> GetMetadata(int id);

        /// <summary>
        /// Gets the item metadata for the given item.
        /// </summary>
        /// <exception cref="LiftiException">
        /// Thrown when the item is not known.
        /// </exception>
        IItemMetadata<TKey> GetMetadata(TKey item);

        /// <summary>
        /// Creates a snapshot of this instance that can be used even if the index is subsequently mutated.
        /// </summary>
        IItemStore<TKey> Snapshot();
    }
}