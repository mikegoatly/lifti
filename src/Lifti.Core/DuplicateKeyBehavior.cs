namespace Lifti
{
    /// <summary>
    /// How an index should behave when adding a document for which the key is already present.
    /// </summary>
    public enum DuplicateKeyBehavior
    {
        /// <summary>
        /// When an document is added with a key already present in the index, the new document will replace the old.
        /// </summary>
        Replace = 0,

        /// <summary>
        /// If when adding a document to the index its key is already present, a <see cref="LiftiException"/> will be thrown.
        /// </summary>
        ThrowException = 1
    }
}