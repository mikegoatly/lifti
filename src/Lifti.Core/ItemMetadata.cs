namespace Lifti
{

    public class ItemMetadata<T> : IItemMetadata<T>
    {
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