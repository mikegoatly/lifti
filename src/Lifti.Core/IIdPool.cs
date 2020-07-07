namespace Lifti
{
    internal interface IIdPool<T> : IItemStore<T>
    {
        /// <summary>
        /// Returns the id associated to the given item back to the pool.
        /// </summary>
        /// <returns>
        /// The id that was associated to the item.
        /// </returns>
        int ReleaseItem(T item);

        /// <summary>
        /// Adds the given item with the pre-determined id. This is used when
        /// de-serializing an index and the ids are already known.
        /// </summary>
        /// <exception cref="LiftiException">
        /// Thrown when the id is already used or the item is already indexed.
        /// </exception>
        void Add(int id, T item, DocumentStatistics documentStatistics);

        /// <summary>
        /// Adds the given item, generating a new id for it as it is stored.
        /// </summary>
        /// <returns>
        /// The id for the item.
        /// </returns>
        /// <exception cref="LiftiException">
        /// Thrown when the item is already indexed.
        /// </exception>
        int Add(T item, DocumentStatistics documentStatistics);
    }
}