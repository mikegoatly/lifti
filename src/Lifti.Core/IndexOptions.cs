namespace Lifti
{
    /// <summary>
    /// Options that are passed to the index at construction time.
    /// </summary>
    public class IndexOptions
    {
        internal IndexOptions()
        {
        }

        /// <summary>
        /// Gets the depth of the index tree after which intra-node text is supported.
        /// A value of zero indicates that intra-node text is always supported. To disable
        /// intra-node text completely, set this to an arbitrarily large value, e.g. <see cref="System.Int32.MaxValue"/>.
        /// The default value is <c>4</c>.
        /// </summary>
        public int SupportIntraNodeTextAfterIndexDepth { get; internal set; } = 4;

        /// <summary>
        /// Gets the behavior the index should exhibit when an item that already exists in the index is indexed again. 
        /// The default value is <see cref="DuplicateItemBehavior.ReplaceItem"/>.
        /// </summary>
        public DuplicateItemBehavior DuplicateItemBehavior { get; internal set; } = DuplicateItemBehavior.ReplaceItem;
    }
}
