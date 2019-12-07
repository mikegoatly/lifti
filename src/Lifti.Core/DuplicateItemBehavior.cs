namespace Lifti
{
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