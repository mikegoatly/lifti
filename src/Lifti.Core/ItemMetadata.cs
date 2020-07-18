namespace Lifti
{
    /// <inheritdoc />
    public class ItemMetadata<T> : IItemMetadata<T>
    {
        /// <summary>
        /// Constructs a new instance of <see cref="IItemMetadata{TKey}"/>.
        /// </summary>
        public ItemMetadata(int id, T item, DocumentStatistics documentStatistics)
        {
            this.Id = id;
            this.Item = item;
            this.DocumentStatistics = documentStatistics;
        }

        /// <inheritdoc />
        public T Item { get; }

        /// <inheritdoc />
        public int Id { get; }

        /// <inheritdoc />
        public DocumentStatistics DocumentStatistics { get; }
    }
}