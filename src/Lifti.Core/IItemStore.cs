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
        ItemMetadata GetMetadata(int id);

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
        bool Contains(TKey key);

        /// <summary>
        /// Gets the item metadata for the given id.
        /// </summary>
        /// <exception cref="LiftiException">
        /// Thrown when the id is not known.
        /// </exception>
#pragma warning disable CS0108 // Member hides inherited member; missing new keyword
        ItemMetadata<TKey> GetMetadata(int id);
#pragma warning restore CS0108 // Member hides inherited member; missing new keyword

        /// <summary>
        /// Gets the item metadata for the given item.
        /// </summary>
        /// <exception cref="LiftiException">
        /// Thrown when the item is not known.
        /// </exception>
        ItemMetadata<TKey> GetMetadata(TKey key);

        /// <summary>
        /// Creates a snapshot of this instance that can be used even if the index is subsequently mutated.
        /// </summary>
        IItemStore<TKey> Snapshot();

        /// <summary>
        /// Adds the given item metadata to the item store. This should only be used by deserializers as they 
        /// rebuild the index.
        /// </summary>
        /// <param name="itemMetadata"></param>
        void Add(ItemMetadata<TKey> itemMetadata);
    }
}