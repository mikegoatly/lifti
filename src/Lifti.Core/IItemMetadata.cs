namespace Lifti
{
    public interface IItemMetadata<T> : IItemMetadata
    {
        /// <summary>
        /// Gets the indexed item.
        /// </summary>
        public T Item { get; }
    }

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