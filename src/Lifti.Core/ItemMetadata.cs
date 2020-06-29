namespace Lifti
{
    public class ItemMetadata<T>
    {
        public ItemMetadata(int id, T item, DocumentStatistics documentStatistics)
        {
            this.Id = id;
            this.Item = item;
            this.DocumentStatistics = documentStatistics;
        }

        /// <summary>
        /// Gets the reference ID of the indexed item used internally in the index.
        /// </summary>
        public int Id { get; }

        /// <summary>
        /// Gets the indexed item.
        /// </summary>
        public T Item { get; }

        /// <summary>
        /// Gets the statistics for the indexed document, including word count.
        /// </summary>
        public DocumentStatistics DocumentStatistics { get; }
    }
}