namespace Lifti
{
    /// <summary>
    /// Describes the behavior of the index when indexing an item that is already present in the index.
    /// </summary>
    public enum DuplicateItemBehavior
    {
        /// <summary>
        /// When an item is indexed and it already exists in the index, the text associated to the new item should replace the old.
        /// </summary>
        ReplaceItem = 0,

        /// <summary>
        /// When an item is indexed and it already exists in the index, a <see cref="LiftiException"/> should be thrown.
        /// </summary>
        ThrowException = 1
    }
}