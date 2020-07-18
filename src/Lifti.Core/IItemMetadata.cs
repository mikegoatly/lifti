namespace Lifti
{
    /// <summary>
    /// Describes metadata for an indexed item.
    /// </summary>
    /// <typeparam name="TKey">The type of the key in the index.</typeparam>
    public interface IItemMetadata<TKey> : IItemMetadata
    {
        /// <summary>
        /// Gets the indexed item.
        /// </summary>
        public TKey Item { get; }
    }

    /// <summary>
    /// Describes metadata for an indexed item.
    /// </summary>
    public interface IItemMetadata
    {
        /// <summary>
        /// Gets the reference ID of the indexed item used internally in the index.
        /// </summary>
        public int Id { get; }

        /// <summary>
        /// Gets the statistics for the indexed document, including token count.
        /// </summary>
        public DocumentStatistics DocumentStatistics { get; }
    }
}