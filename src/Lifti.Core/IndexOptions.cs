using System;

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
        /// intra-node text completely, set this to an arbitrarily large value, e.g. <see cref="int.MaxValue"/>.
        /// The default value is <c>4</c>.
        /// </summary>
        public int SupportIntraNodeTextAfterIndexDepth { get; internal set; } = 4;

        /// <inheritdoc cref="DuplicateKeyBehavior"/>
        [Obsolete("Use DuplicateKeyBehavior property instead")]
        public DuplicateKeyBehavior DuplicateItemBehavior => this.DuplicateKeyBehavior;

        /// <summary>
        /// Gets the behavior the index should exhibit when key that already exists in the index is added again. 
        /// The default value is <see cref="DuplicateKeyBehavior.Replace"/>.
        /// </summary>
        public DuplicateKeyBehavior DuplicateKeyBehavior { get; internal set; } = DuplicateKeyBehavior.Replace;
    }
}
